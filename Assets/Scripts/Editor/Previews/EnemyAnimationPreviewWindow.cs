#if UNITY_EDITOR
using AbyssBar.EnemyAnimations;
using UnityEditor;
using UnityEngine;

public sealed class EnemyAnimationPreviewWindow : EditorWindow
{
    private EnemyAnimationData data;
    private int role, motion;
    private bool playing = true;
    private double started;
    private float shownSeconds;
    [MenuItem("Abyss Bar/Enemy Animations/4 - Preview Animations")]
    private static void Open() { GetWindow<EnemyAnimationPreviewWindow>("Enemy Animation Preview").Show(); }
    private void OnEnable() { started = EditorApplication.timeSinceStartup; LoadRole(); }
    private void Update() { if (playing) Repaint(); }
    private void LoadRole()
    {
        string[] names = { "Wanderer", "Runner", "Riftmaw" };
        data = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>("Assets/Data/EnemyAnimations/" + names[role] + "Animations.asset");
        started = EditorApplication.timeSinceStartup;
        shownSeconds = 0;
    }
    private void OnGUI()
    {
        int selected = EditorGUILayout.Popup("Enemy", role, new[] { "普通敵人", "快速敵人", "裂口射手" });
        if (selected != role) { role = selected; LoadRole(); }
        data = (EnemyAnimationData)EditorGUILayout.ObjectField("Animation Data", data, typeof(EnemyAnimationData), false);
        int action = EditorGUILayout.Popup("Motion", motion, new[] { "Idle 待機", "Walk 移動", "Attack 攻擊", "Hurt 受傷", "Death 死亡", "Stun 暈眩" });
        if (action != motion) { motion = action; started = EditorApplication.timeSinceStartup; shownSeconds = 0; }
        if (GUILayout.Button(playing ? "Pause / 暫停" : "Replay / 重播")) { playing = !playing; if (playing) started = EditorApplication.timeSinceStartup; }
        if (data == null || !data.IsValid) { EditorGUILayout.HelpBox("Run Enemy Animations / 1 to prepare assets.", MessageType.Info); return; }
        var clips = new[] { data.idle, data.walk, data.attack, data.hurt, data.death, data.stun };
        var clip = clips[motion];
        if (playing) shownSeconds = (float)(EditorApplication.timeSinceStartup - started);
        int frame = clip.FrameAt(shownSeconds, motion != 4);
        Sprite sprite = clip.frames[frame];
        EditorGUILayout.LabelField("Frame", (frame + 1) + " / " + clip.frames.Length);
        Rect area = GUILayoutUtility.GetRect(300, 360, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(area, new Color(.13f, .15f, .2f));
        float side = Mathf.Min(area.width, area.height);
        Rect preview = new Rect(area.center.x - side * .5f, area.center.y - side * .5f, side, side);
        Rect r = sprite.rect;
        GUI.DrawTextureWithTexCoords(preview, sprite.texture, new Rect(r.x / sprite.texture.width,
            r.y / sprite.texture.height, r.width / sprite.texture.width, r.height / sprite.texture.height), true);
        EditorGUILayout.HelpBox("This previews sprite frames. Attack timing, collision and death/wave behavior must also be tested in Play Mode.", MessageType.Info);
    }
}
#endif
