﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using SteamVRRecorder;
using UnityEngine;
using Valve.VR;

namespace SteamVRRecorder
{
    /// <summary>
    /// This class can be used on a GameObject which has a SteamVR_TrackedObject component in order
    /// to avoid to specify the SteamVR device's index (which depends on the order of activation).
    /// Instead, it uses a configured tracker name that is defined in <see cref="TRACKER_CONFIG_FILENAME"/>
    /// to match it with it's correct serial number. This file should be present in the executable folder.
    /// It could be generated by hand or using the "tracker configuration tool" (if serial number are not known).
    /// The "tracker configuration tool" can be found here: https://github.com/Artorg-MCLab/ViveTrackerConfigurationTool
    /// </summary>
    [RequireComponent(typeof(SteamVR_TrackedObject))]
    public class TrackerConfigurationLoader : MonoBehaviour {

        /// <summary>
        /// The (case-sensitive) name that is defined in tracker configuration file.
        /// </summary>
        public string configuredName="";

        public const string TRACKER_CONFIG_FILENAME= "tracker_config.txt";
        private string serialNumber;
        private SteamVR_TrackedObject.EIndex index;
        private SteamVR_TrackedObject trackedObject;

        private static Dictionary<string, uint> indexByNames = new Dictionary<string, uint>();
        private Vector3 initPos;

        private void Awake()
        {
            initPos = this.transform.position;
        }

        void Start ()
        {
            LoadID();
            LoadIndexFromID();
            trackedObject = GetComponent<SteamVR_TrackedObject>();
            trackedObject.index = (SteamVR_TrackedObject.EIndex) this.index;
            if (indexByNames.ContainsKey(configuredName))
            {
                Debug.LogError("Tracker with name " + configuredName + " already defined.");
                return;
            }
            indexByNames.Add(configuredName, (uint)trackedObject.index);
        }

        public float GetTriggerState()
        {
            return TrackerConfigurationLoader.GetTriggerState(configuredName);
        }

        public static float GetTriggerState(string name)
        {
            if (indexByNames.ContainsKey(name))
            {
                VRControllerState_t controllerState = new VRControllerState_t();
                OpenVR.System.GetControllerState(indexByNames[name], ref controllerState, (uint)System.Runtime.InteropServices.Marshal.SizeOf(controllerState));

                return controllerState.rAxis1.x;
            }
            else
            {
                return -1;
            }
        }

        public bool IsTracked
        {
            get { return this.transform.position != initPos; ; }
        }

        private void LoadID()
        {
            string trackerConfigurationFile=Application.dataPath + "/StreamingAssets/"+TRACKER_CONFIG_FILENAME;
            string[] lines = System.IO.File.ReadAllLines(trackerConfigurationFile);
            foreach (string line in lines)
            {
                //TODO: Check the file structure and make some security check.
                string[] lineParts=line.Split(';');
                string name = lineParts[0];
                string id = lineParts[1];
                if (name.Equals(configuredName))
                {
                    serialNumber = id;
                    return;
                }
            }

            Debug.LogError("No tracker with configured name \"" + configuredName + "\" were found in tracker configuration file. Please make sure that the file \""+ trackerConfigurationFile + "\" contains a line like this one: \"" + configuredName + "SERIAL_NUMBER\" or launch the tracker configuration tool.\nWithout configured tracker, The game will use the first tracker found in SteamVR device list.");

            for(int i=0; i<16; i++)
            {
                if (IsTracker((SteamVR_TrackedObject.EIndex)i))
                {
                    index = (SteamVR_TrackedObject.EIndex)i;
                    Debug.LogWarning("Tracker with configured name \"" + configuredName + "\" was assigned to SteamVRIndex " + ((SteamVR_TrackedObject.EIndex)i) + " because no corresponding entry were found in the tracker configuration file.");
                    LoadIDFromIndex();
                    return;
                }
            }
        }

        private void LoadIDFromIndex()
        {
            ETrackedPropertyError error = new ETrackedPropertyError();
            StringBuilder result = new StringBuilder();
            OpenVR.System.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_SerialNumber_String, result, OpenVR.k_unMaxPropertyStringSize, ref error);
            this.serialNumber = result.ToString();
        }

        private void LoadIndexFromID()
        {
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            bool trackerFound = false;
            for (uint i = 0; i < 16; i++)
            {
                StringBuilder result = new System.Text.StringBuilder((int)64);
                OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String, result, 64, ref error);
                if (result.ToString().Equals(serialNumber))
                {
                    index = (SteamVR_TrackedObject.EIndex)i;
                    trackerFound = true;
                }
            }
            if (!trackerFound)
            {
                index = SteamVR_TrackedObject.EIndex.None;
            }
        }

        public static bool IsTracker(SteamVR_TrackedObject.EIndex index)
        {
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            StringBuilder result = new System.Text.StringBuilder((int)64);
            OpenVR.System.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);
            Debug.Log(result);
            return result.ToString().Contains("tracker");
        }
    }
}