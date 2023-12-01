using SteamVRRecorder;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using System.Threading;
using Valve.VR;
using SteamVRRecorder.Record;
using System.Text;

public class UXFSteamVRTracker : Tracker
{
    public bool recordingActive = false;
    public override string MeasurementDescriptor => "SteamVRData";
    public override IEnumerable<string> CustomHeader => new string[] { "systemTime", "hmd.pos.x", "hmd.pos.y", "hmd.pos.z", "hmd.rot.w", "hmd.rot.x", "hmd.rot.y", "hmd.rot.z", "tracker1.pos.x", "tracker1.pos.y", "tracker1.pos.z", "tracker1.rot.w", "tracker1.rot.x", "tracker1.rot.y", "tracker1.rot.z" };
        

    private Thread _recordingThread = null;

    private ThreadSafeCircularQueue<float[]> _buffer;
    private bool _isEnding = false;

    //private RecorderTimeManager _recorderTimeManager;

    public bool recordHmd;
    private Camera _hmd = null;
    public TrackerConfigurationLoader[] trackedDevices;
    private SteamVR_TrackedObject[] _trackedObjects;

    public float[] record;



    public void Init()
    {
        TrackerConfigurationLoader[] loaders = FindObjectsOfType<TrackerConfigurationLoader>();

        //_recorderTimeManager = FindObjectOfType<RecorderTimeManager>();

        _buffer = new ThreadSafeCircularQueue<float[]>(2000, null);

        _trackedObjects = new SteamVR_TrackedObject[trackedDevices.Length];
        for (int i = 0; i < trackedDevices.Length; i++)
        {
            _trackedObjects[i] = trackedDevices[i].GetComponent<SteamVR_TrackedObject>();
        }


        _hmd = Camera.main;

        if (recordHmd && _hmd == null)
        {
            Debug.LogError("No HMD found. It is needed, that is why the application was quit.");
            this.ApplicationQuit();
        }

        CreateHeader();

        _recordingThread = new Thread(Record);
        _recordingThread.Start();
    }

    private void Update()
    {
        // Check if there is data in the buffer to process
        while (!_buffer.IsEmpty() && !_isEnding)
        {
            // Dequeue data for processing
            record = _buffer.Pop();
            //GetCurrentValues();
            RecordRow(); // Process data on the main thread
        }
    }

    private void Record()
    {
        //int deviceSize = ((recordHmd) ? 1 : 0) + trackedDevices.Length;

        //OpenVR.System.ResetSeatedZeroPose();
        while (!_isEnding)
        {
            if (recordingActive)
            {
                Debug.Log("Recording");

                TrackedDevicePose_t[] trackedPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
                OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, trackedPoses);

                record = new float[1 + GetDeviceNumber() * 7];
                record[0] = RecorderTimeManager.SystemTime;
                for (int i = 0; i < GetDeviceNumber(); i++)
                {
                    SteamVR_TrackedObject.EIndex deviceIndex = GetDeviceIndex(i);
                    if (deviceIndex != SteamVR_TrackedObject.EIndex.None)
                    {
                        TrackedDevicePose_t trackedPose = trackedPoses[(int)deviceIndex];
                        SteamVR_Utils.RigidTransform rigidTransform = new SteamVR_Utils.RigidTransform(trackedPose.mDeviceToAbsoluteTracking);
                        record[1 + i * 7] = rigidTransform.pos.x;
                        record[2 + i * 7] = rigidTransform.pos.y;
                        record[3 + i * 7] = rigidTransform.pos.z;
                        record[4 + i * 7] = rigidTransform.rot.w;
                        record[5 + i * 7] = rigidTransform.rot.x;
                        record[6 + i * 7] = rigidTransform.rot.y;
                        record[7 + i * 7] = rigidTransform.rot.z;
                    }
                    else
                    {
                        record[1 + i * 7] = 0.0f;
                        record[2 + i * 7] = 0.0f;
                        record[3 + i * 7] = 0.0f;
                        Quaternion noRot = Quaternion.identity;
                        record[4 + i * 7] = noRot.w;
                        record[5 + i * 7] = noRot.x;
                        record[6 + i * 7] = noRot.y;
                        record[7 + i * 7] = noRot.z;
                    }
                }
                _buffer.Put(record);
            }
            else
            {
                Debug.Log("Recording paused");
            }

            Thread.Sleep(1); // Sleep to prevent high CPU usage
        }
    }

    private void CreateHeader() // This will just output the header, which you can copy paste into the CustomHeader variable above
    {
        StringBuilder headerBuilder = new StringBuilder();

        // Add system time
        headerBuilder.Append("\"systemTime\",");

        // Add HMD headers if recording HMD
        if (recordHmd)
        {
            headerBuilder.Append("\"hmd.pos.x\",\"hmd.pos.y\",\"hmd.pos.z\",\"hmd.rot.w\",\"hmd.rot.x\",\"hmd.rot.y\",\"hmd.rot.z\",");
        }

        // Add headers for each tracked device
        foreach (var device in trackedDevices)
        {
            string trackerName = device.configuredName;
            headerBuilder.AppendFormat("\"{0}.pos.x\",\"{0}.pos.y\",\"{0}.pos.z\",\"{0}.rot.w\",\"{0}.rot.x\",\"{0}.rot.y\",\"{0}.rot.z\",", trackerName);
        }

        // Convert StringBuilder to a string array split by the comma separator
        string[] customHeader = headerBuilder.ToString().TrimEnd(',').Split(',');
        Debug.Log(string.Join(", ", customHeader)); // This will print the header string array to the Unity console
    }

    private SteamVR_TrackedObject.EIndex GetDeviceIndex(int trackedIndex)
    {
        if (recordHmd)
        {
            if (trackedIndex == 0)
            {
                return 0;
            }
            else
            {
                return _trackedObjects[trackedIndex - 1].index;
            }
        }
        else
        {
            return _trackedObjects[trackedIndex].index;
        }
    }

    private int GetDeviceNumber()
    {
        int deviceNumber = _trackedObjects.Length;
        if (recordHmd) { deviceNumber++; }
        return deviceNumber;
    }


    protected override UXFDataRow GetCurrentValues()
    {
        var values = new UXFDataRow();

        // Add system time
        values.Add(("systemTime", record[0]));

        int deviceNumber = GetDeviceNumber();
        int dataIndex = 1; // Start after system time

        // Add HMD data if recording HMD
        if (recordHmd)
        {
            values.Add(("hmd.pos.x", record[dataIndex++]));
            values.Add(("hmd.pos.y", record[dataIndex++]));
            values.Add(("hmd.pos.z", record[dataIndex++]));
            values.Add(("hmd.rot.w", record[dataIndex++]));
            values.Add(("hmd.rot.x", record[dataIndex++]));
            values.Add(("hmd.rot.y", record[dataIndex++]));
            values.Add(("hmd.rot.z", record[dataIndex++]));
        }

        // Add data for each tracked device
        for (int i = recordHmd ? 1 : 0; i < deviceNumber; i++)
        {
            string trackerName = trackedDevices[i - (recordHmd ? 1 : 0)].configuredName;
            values.Add(($"{trackerName}.pos.x", record[dataIndex++]));
            values.Add(($"{trackerName}.pos.y", record[dataIndex++]));
            values.Add(($"{trackerName}.pos.z", record[dataIndex++]));
            values.Add(($"{trackerName}.rot.w", record[dataIndex++]));
            values.Add(($"{trackerName}.rot.x", record[dataIndex++]));
            values.Add(($"{trackerName}.rot.y", record[dataIndex++]));
            values.Add(($"{trackerName}.rot.z", record[dataIndex++]));
        }

        return values;
    }

    private void OnDestroy()
    {
        // Signal the recording thread to stop
        _isEnding = true;

        // If the recording thread is running, wait for it to finish
        if (_recordingThread != null && _recordingThread.IsAlive)
        {
            _recordingThread.Join(); // This waits for the thread to complete
        }
    }

}
