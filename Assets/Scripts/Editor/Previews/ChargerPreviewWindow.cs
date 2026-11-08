#if UNITY_EDITOR
using AbyssBar.EnemyAnimations;
using UnityEditor;
using UnityEngine;

public sealed class ChargerPreviewWindow : EditorWindow
{
    private ChargeAnimationSet data;
    private int motion;
    private double started;
    private float shown;
    private bool playing = true;
    [MenuItem("Abyss Bar/Charger/4 - Preview All Nine Motions")]
    private static void Open() { GetWindow<ChargerPreviewWindow>("Charger Animation Preview").Show(); }
    private void OnEnable()
    { data = AssetDatabase.LoadAssetAtPath<ChargeAnimationSet>("Assets/Data/Enemies/Charger/ChargerAnimations.asset"); started = EditorApplication.timeSinceStartup; }
    private void Update() { if (playing) Repaint(); }
    private void OnGUI()
    {
        data = (ChargeAnimationSet)EditorGUILayout.ObjectField("Animation Set", data, typeof(ChargeAnimationSet), false);
        int next = EditorGUILayout.Popup("Motion", motion, new[] { "待機 Idle", "走路 Walk", "蓄勢 Windup", "衝刺 Rush", "撞牆 Wall Impact", "收招 Recovery", "受傷 Hurt", "暈眩 Stun", "死亡 Death" });
        if (next != motion) { motion = next; started = EditorApplication.timeSinceStartup; shown = 0; }
        if (GUILayout.Button(playing ? "Pause / 暫停" : "Replay / 重播")) { playing = !playing; if (playing) started = EditorApplication.timeSinceStartup; }
        if (data == null || !data.IsValid) { EditorGUILayout.HelpBox("Run Charger / 1 first.", MessageType.Info); return; }
        if (playing) shown = (float)(EditorApplication.timeSinceStartup - started);
        var clips = new[] { data.common.idle, data.common.walk, data.windup, data.rush, data.wallImpact, data.recovery, data.common.hurt, data.common.stun, data.common.death };
        var clip = clips[motion]; int frame = clip.FrameAt(shown, motion == 0 || motion == 1 || motion == 3 || motion == 7);
        EditorGUILayout.LabelField("Frame", (frame + 1) + " / " + clip.frames.Length);
        Rect area = GUILayoutUtility.GetRect(300, 360, GUILayout.ExpandWidth(true)); EditorGUI.DrawRect(area, new Color(.13f, .15f, .2f));
        float side = Mathf.Min(area.width, area.height); var sprite = clip.frames[frame]; var r = sprite.rect;
        GUI.DrawTextureWithTexCoords(new Rect(area.center.x - side * .5f, area.center.y - side * .5f, side, side), sprite.texture,
            new Rect(r.x / sprite.texture.width, r.y / sprite.texture.height, r.width / sprite.texture.width, r.height / sprite.texture.height), true);
    }
}
#endif
