using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using Valve.VR;

namespace SteamVRRecorder.Record
{
    public class MovementRecorder : Recorder {
        
        [SerializeField] private string _filename = "data";

        private Camera _hmd=null;
        private SteamVR_TrackedObject _upperArm = null;
        private SteamVR_TrackedObject _lowerArm = null;
        private SteamVR_TrackedObject _hand = null;
        private Transform _transformUpperArm = null;
        private Transform _transformLowerArm = null;
        private Transform _transformHand = null;
        private Transform _transformHmd = null;

        private string _filePath;
        private Thread _recordingThread = null;
        private Thread _savingThread = null;
        
        private static string Filename = "";
        private ThreadSafeCircularQueue<float[]> _buffer;
        private bool _isEnding = false;
        
        private RecorderTimeManager _recorderTimeManager;

        public bool recordHmd;
        public TrackerConfigurationLoader[] trackedDevices;
        private SteamVR_TrackedObject[] _trackedObjects;

        private void Start()
        {
            if (_filename.Length > 0) Filename = _filename;

            TrackerConfigurationLoader[] loaders=FindObjectsOfType<TrackerConfigurationLoader>();
            
            _recorderTimeManager = FindObjectOfType<RecorderTimeManager>();
            
            _buffer = new ThreadSafeCircularQueue<float[]>(2000, null);
            _trackedObjects = new SteamVR_TrackedObject[trackedDevices.Length];
            for(int i=0; i<trackedDevices.Length; i++)
            {
                _trackedObjects[i] = trackedDevices[i].GetComponent<SteamVR_TrackedObject>();
            }

            if (File.Exists(GetFilePath()))
            {
                Debug.LogError("File already exists. The application will won't be launched in order to avoid to erase these data. Please change the filename on the ParticipantInformation component attached to " + gameObject.name + " game object.");
                //this.ApplicationQuit();// QuitGame();
                Filename += "0";
            }
            else
            {
                _hmd = Camera.main;

                if (recordHmd && _hmd == null)
                {
                    Debug.LogError("No HMD found. It is needed, that is why the application was quit.");
                    this.ApplicationQuit();
                }

                _filePath = GetFilePath();

                _recordingThread = new Thread(Record);
                _savingThread = new Thread(WriteToFile);
                _recordingThread.Start();
                _savingThread.Start();
            }
        }

        private void OnApplicationQuit()
        {
            if (_recordingThread!=null)
            {
                _isEnding = true;
                _recordingThread.Join();
                _savingThread.Join();
            }
        }

        private void WriteToFile()
        {
            StringBuilder content = new StringBuilder();
            using (StreamWriter file = new StreamWriter(_filePath))
            {
                content.Append("systemTime[s]" + Separator);
                if (recordHmd) {
                    content.Append("hmd.pos.x" + Separator + "hmd.pos.y" + Separator + "hmd.pos.z" + Separator
                                   + "hmd.rot.w" + Separator + "hmd.rot.x" + Separator + "hmd.rot.y" + Separator + "hmd.rot.z" + Separator);
                }
                for(int i=0; i<trackedDevices.Length; i++)
                {
                    string trackerName = trackedDevices[i].configuredName;
                    content.Append(trackerName + ".pos.x" + Separator + trackerName + ".pos.y" + Separator + trackerName + ".pos.z" + Separator
                                   + trackerName + ".rot.w" + Separator + trackerName + ".rot.x" + Separator + trackerName + ".rot.y" + Separator + trackerName + ".rot.z" + Separator);
                }
                file.WriteLine(content);
                content.Length = 0;
                content.Capacity = 0;
            }

            while (!_buffer.IsEmpty() || !_isEnding)
            {
                using (StreamWriter file = new StreamWriter(_filePath, true))
                {
                    while (!_buffer.IsEmpty())
                    {
                        foreach (float value in _buffer.Pop())
                        {
                            content.Append(value + "" + Separator);
                        }
                        file.WriteLine(content);
                        content.Length=0;
                        content.Capacity = 0;
                    }
                }

                if (!_isEnding)
                {
                    Thread.Sleep(1000);
                }
            }
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

        private void Record()
        {
            int deviceSize = ((recordHmd) ? 1 : 0) + trackedDevices.Length;
        
            //OpenVR.System.ResetSeatedZeroPose();
            while (!_isEnding)
            {
                TrackedDevicePose_t[] trackedPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
                OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, trackedPoses);

                float[] record = new float[1 + GetDeviceNumber() * 7];
                record[0] = RecorderTimeManager.SystemTime;
                for (int i= 0; i < GetDeviceNumber(); i++)
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
                Thread.Sleep(1);
            }
        }

        public static string GetFilePath()
        {
            return GetFolder() + Filename + ".csv";
        }
    }
}
