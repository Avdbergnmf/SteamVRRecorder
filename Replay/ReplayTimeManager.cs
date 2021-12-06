using UnityEngine;

namespace SteamVRRecorder.Replay
{
    public class ReplayTimeManager : MonoBehaviour
    {
        public float speed;
        public bool isPlaying = true;
        public float time = 0;

        public float MaxTime { get; set; }

        private void FixedUpdate()
        {
            if (isPlaying)
            {
                time += Time.deltaTime * speed;
            }
        }

        public float GetTimeSecond()
        {
            return time;
        }
    }
}
