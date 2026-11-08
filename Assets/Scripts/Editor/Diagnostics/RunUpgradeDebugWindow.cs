#if UNITY_EDITOR
using System.Linq;
using AbyssBar.Upgrades;
using UnityEditor;
using UnityEngine;

public sealed class RunUpgradeDebugWindow : EditorWindow
{
    private int selected;
    private bool ignoreUnlocks;
    private Vector2 scroll;
    [MenuItem("Abyss Bar/Run Upgrades/4 - Battle Test Window")]
    private static void Open() => GetWindow<RunUpgradeDebugWindow>("局內升級測試");
    private void OnGUI()
    {
        var db = Resources.Load<UpgradeDatabase>("RunUpgradeDatabase");
        var runtime = Object.FindAnyObjectByType<RunUpgradeRuntime>();
        EditorGUILayout.HelpBox("進入 Main 的 Play Mode 後測試。此視窗只修改目前這一局，不扣貨幣，也不修改永久培養。", MessageType.Info);
        if (db == null || db.cards == null || db.cards.Length == 0) { EditorGUILayout.LabelField("請先 Prepare Data Assets。"); return; }
        var cards = db.cards.Where(c => c != null).OrderBy(c => c.id).ToArray();
        selected = Mathf.Clamp(selected, 0, cards.Length - 1);
        selected = EditorGUILayout.Popup("升級卡", selected, cards.Select(c => c.id + " " + c.title).ToArray());
        EditorGUILayout.LabelField(cards[selected].description, EditorStyles.wordWrappedLabel);
        ignoreUnlocks = EditorGUILayout.Toggle("忽略選角／Q／節點門檻", ignoreUnlocks);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || runtime == null || !runtime.Active))
        {
            if (GUILayout.Button("套用所選卡片一級"))
            {
                bool ok = ignoreUnlocks ? runtime.ApplyForTesting(cards[selected]) : runtime.Apply(cards[selected]);
                if (!ok) Debug.LogWarning("未套用：角色／Q／節點門檻不符，或此卡已滿級。");
            }
            if (GUILayout.Button("本局預覽 Q（不存檔）")) runtime.Skills.SetQUnlocked(true);
            if (GUILayout.Button("打開一次四選一")) Object.FindAnyObjectByType<UpgradeManager>()?.ShowUpgradeChoices();
        }
        if (runtime != null)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var pair in runtime.Ranks) EditorGUILayout.LabelField(pair.Key, "Lv." + pair.Value);
            EditorGUILayout.EndScrollView();
        }
    }
    private void OnInspectorUpdate() { Repaint(); }
}
#endif
