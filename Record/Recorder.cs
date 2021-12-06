using System.IO;
using UnityEngine;

namespace SteamVRRecorder.Record
{
    public abstract class Recorder : MonoBehaviour {

        public const char Separator=',';

        protected virtual void Awake()
        {
            // Display the timer frequency and resolution.
            Debug.Log(System.Diagnostics.Stopwatch.IsHighResolution
                ? "Operations timed using the system's high-resolution performance counter."
                : "Operations timed using the DateTime class.");
        }

        private static string GetParentFolder()
        {
            return Application.dataPath + "/../Recordings/";
        }

        protected static string GetFolder(bool createIfNotExists=true)
        {
            string directoryPath = GetParentFolder(); // add subfolder here if needed. e.g.: + _subFolder;
            if (!Directory.Exists(directoryPath) && createIfNotExists)
            {
                Directory.CreateDirectory(directoryPath);
            }
            return directoryPath;
        }
    }
}
