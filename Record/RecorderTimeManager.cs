using UnityEngine;

namespace SteamVRRecorder.Record
{
    public class RecorderTimeManager : MonoBehaviour
    {
        private static System.Diagnostics.Stopwatch _timer;
        private static long _nanoSecPerTick;
        private static float? _initTimeShift = null;

        private void Awake()
        {
            if (_timer == null)
            {
                long frequency = System.Diagnostics.Stopwatch.Frequency;
                Debug.Log("Timer frequency in ticks per second = " + frequency);
                _nanoSecPerTick = (1000L * 1000L * 1000L) / frequency;
                Debug.Log("Timer is accurate within " + _nanoSecPerTick + " nanoseconds");

                _timer = System.Diagnostics.Stopwatch.StartNew();
            }
        }

        /// <summary>
        /// Return the system time in seconds based on the number of elapsed ticks.
        /// </summary>
        public static float SystemTime => _timer.ElapsedTicks * _nanoSecPerTick / 1000000000.0f;
    }
}
