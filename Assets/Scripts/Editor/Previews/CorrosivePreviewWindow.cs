#if UNITY_EDITOR
using AbyssBar.EnemyAnimations;
using UnityEditor;
using UnityEngine;

public sealed class CorrosivePreviewWindow : EditorWindow
{
    private CorrosiveAnimationSet data;
    private int motion;
    private bool playing = true;
    private double started;
    private float shown;
    [MenuItem("Abyss Bar/Corrosive/4 - Preview All Ten Motions")]
    private static void Open() { GetWindow<CorrosivePreviewWindow>("Corrosive Animation Preview").Show(); }
    private void OnEnable()
    { data = AssetDatabase.LoadAssetAtPath<CorrosiveAnimationSet>("Assets/Data/Enemies/Corrosive/CorrosiveAnimations.asset"); started = EditorApplication.timeSinceStartup; }
    private void Update() { if (playing) Repaint(); }
    private void OnGUI()
    {
        data = (CorrosiveAnimationSet)EditorGUILayout.ObjectField("Animation Set", data, typeof(CorrosiveAnimationSet), false);
        int next = EditorGUILayout.Popup("Motion", motion, new[] { "待機 Idle", "移動 Walk", "近身攻擊 Attack", "受傷 Hurt", "暈眩 Stun", "死亡膨脹 Inflate", "爆裂 Burst", "腐液生成 Formation", "腐液持續 Bubbling", "腐液消散 Dissolve" });
        if (next != motion) { motion = next; started = EditorApplication.timeSinceStartup; shown = 0; }
        if (GUILayout.Button(playing ? "Pause / 暫停" : "Replay / 重播")) { playing = !playing; if (playing) started = EditorApplication.timeSinceStartup; }
        if (data == null || !data.IsValid) { EditorGUILayout.HelpBox("Run Corrosive / 1 first.", MessageType.Info); return; }
        if (playing) shown = (float)(EditorApplication.timeSinceStartup - started);
        var clips = new[] { data.common.idle, data.common.walk, data.common.attack, data.common.hurt, data.common.stun, data.inflate, data.burst, data.formation, data.bubbling, data.dissolve };
        var clip = clips[motion]; int frame = clip.FrameAt(shown, motion == 0 || motion == 1 || motion == 4 || motion == 8);
        EditorGUILayout.LabelField("Frame", (frame + 1) + " / " + clip.frames.Length);
        Rect area = GUILayoutUtility.GetRect(300, 360, GUILayout.ExpandWidth(true)); EditorGUI.DrawRect(area, new Color(.13f, .15f, .2f));
        float side = Mathf.Min(area.width, area.height); var sprite = clip.frames[frame]; Rect r = sprite.rect;
        GUI.DrawTextureWithTexCoords(new Rect(area.center.x - side * .5f, area.center.y - side * .5f, side, side), sprite.texture,
            new Rect(r.x / sprite.texture.width, r.y / sprite.texture.height, r.width / sprite.texture.width, r.height / sprite.texture.height), true);
    }
}
#endif
