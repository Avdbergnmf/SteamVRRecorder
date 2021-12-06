using SteamVRRecorder.Replay;
using UnityEditor;

namespace SteamVRRecorder.Recorders.Replay.Editor
{
    [CustomEditor(typeof(ReplayTimeManager))]
    public class ReplayTimeManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (!(target is ReplayTimeManager t)) return;

            t.isPlaying = EditorGUILayout.Toggle("Plays", t.isPlaying);
            t.speed = EditorGUILayout.Slider("Speed", t.speed, 0.0f, 10.0f);
            t.time = EditorGUILayout.FloatField("Time", t.time);

            //EditorGUILayout.LabelField(t.GetTimeSecond().ToString("G9"));
        }
    }
}
