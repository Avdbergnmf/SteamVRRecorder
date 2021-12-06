using System.Collections.Generic;
using System.IO;
using SteamVRRecorder.Record;
using UnityEngine;

namespace SteamVRRecorder.Replay
{
    public class MovementPlayer : MonoBehaviour {
        private float[] _times;
        private Vector3[] _positions;
        private Quaternion[] _rotations;
        private int _index = 0;
        public string deviceName;
        private ReplayTimeManager _timeManager;

        private void Awake()
        {
            _timeManager = FindObjectOfType<ReplayTimeManager>();
        }

        private void Start () {
            string[] lines = File.ReadAllLines(MovementRecorder.GetFilePath());
            _times = new float[lines.Length];
            _positions = new Vector3[lines.Length];
            _rotations = new Quaternion[lines.Length];

            string[] parts = lines[0].Split(new char[] { Recorder.Separator });
            Dictionary<string, int> offsetPerName = new Dictionary<string, int>();

            for(int i=0; i<(parts.Length-1)/7; i++)
            {
                int offset = 1 + i * 7;
                string varName = parts[offset];
                varName = varName.Split(new char[] { '.' })[0];
                offsetPerName.Add(varName, offset);
            }

            if (!offsetPerName.ContainsKey(deviceName))
            {
                Debug.LogWarning(deviceName + " tracked object not found in the movement file.");
            }
            else
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    parts = lines[i].Split(new char[] { Recorder.Separator });
                    _times[i] = float.Parse(parts[0]);
                    int offset = offsetPerName[deviceName];
                    _positions[i] = new Vector3(float.Parse(parts[offset]), float.Parse(parts[offset + 1]), float.Parse(parts[offset + 2]));
                    _rotations[i] = new Quaternion(float.Parse(parts[offset + 4]), float.Parse(parts[offset + 5]), float.Parse(parts[offset + 6]), float.Parse(parts[offset + 3]));
                }
            }

            if (_timeManager.MaxTime < _times[_times.Length - 1])
            {
                _timeManager.MaxTime = (long) _times[_times.Length - 1];
            }
        }
	
        // Update is called once per frame
        private void Update ()
        {
            var t = transform;
            while(_index < _times.Length && _times[_index] < _timeManager.GetTimeSecond())
            {
                t.localPosition = _positions[_index];
                t.localRotation = _rotations[_index];
                _index++;
            }
        }
    }
}
