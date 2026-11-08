using System.Collections.Generic;
using TMPro;
using UnityEngine;
using AbyssBar.Upgrades;

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private GameObject upgradePanel; // Old panel is hidden; new cards own their UI.
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private UpgradeChoiceUI cardUI;
    [SerializeField] private TMP_FontAsset chineseFont;
    private readonly List<UpgradeCardData> offers = new List<UpgradeCardData>();
    private readonly System.Random random = new System.Random();
    private RunUpgradeRuntime run;
    private int pending;
    private float previousTimeScale = 1;
    private bool choosing, ended;
    private int lastSelectionFrame = -1;
    private readonly UpgradeSelectionGate selectionGate = new UpgradeSelectionGate(.2f);
    private int offerVersion;
    private bool applicationPaused;
    private int authorizedSlot = -1;
    public bool IsSelectionInputReady => !AbyssBar.Inputs.GameInput.Blocked && isActiveAndEnabled && choosing && !ended && !applicationPaused && Application.isFocused && selectionGate.Ready;
    public bool IsChoosing => choosing;
    public bool BlocksPrimaryAttack => choosing || pending > 0 || lastSelectionFrame == Time.frameCount;
    public int PendingChoices => pending;
    private void Awake() { if (upgradePanel != null) upgradePanel.SetActive(false); if (cardUI != null) cardUI.gameObject.SetActive(false); }
    private void Update()
    {
        if (ended) return;
        if (!choosing && pending > 0) Open();
        if (!choosing) return;
        if (!Application.isFocused || applicationPaused || AbyssBar.Inputs.GameInput.Blocked)
        { selectionGate.Suspend(Time.frameCount); cardUI.SetInputReady(false); return; }
        selectionGate.Tick(Time.unscaledDeltaTime, Time.frameCount, SelectionInputsNeutral());
        cardUI.SetInputReady(IsSelectionInputReady);
        if (!IsSelectionInputReady) return;
        for(int index=0;index<4;index++)
            if(AbyssBar.Inputs.GameInput.Down(SelectionActions[index])){RequestChoice(index);break;}
    }
    public void ShowUpgradeChoices() { if (!ended) pending++; }
    private void Open()
    {
        run = playerHealth != null ? playerHealth.GetComponent<RunUpgradeRuntime>() : FindAnyObjectByType<RunUpgradeRuntime>();
        if (run == null || !run.Active) return;
        if (run.Database.Validate().Count > 0) { Debug.LogError("Invalid upgrade database. Run database validation.", this); CancelForBattleEnd(); return; }
        offers.Clear(); offers.AddRange(UpgradeDraft.Draw(run.Database, run.Character.CharacterId, run.Skills.IsQUnlocked,
            CharacterProgressionSave.IsNodeFiveComplete(run.Character), run.Ranks, run.Database.IsRoleUnlocked, random));
        if (offers.Count == 0) { Debug.LogError("No eligible upgrades: check the database.", this); CancelForBattleEnd(); return; }
        if (cardUI == null)
        {
            var canvas = upgradePanel != null ? upgradePanel.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("Upgrade cards need a Canvas.", this); CancelForBattleEnd(); return; }
            if (chineseFont == null)
            {
                foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true))
                    if (label.font != null && label.font.name.ToLowerInvariant().Contains("noto")) { chineseFont = label.font; break; }
                if (chineseFont == null) chineseFont = canvas.GetComponentInChildren<TMP_Text>(true)?.font;
            }
            cardUI = UpgradeChoiceUI.Build(canvas, chineseFont);
        }
        choosing = true; selectionGate.Begin(Time.frameCount); int currentOffer = ++offerVersion;
        if (Time.timeScale > 0) previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        cardUI.Show(offers, run.Ranks, index => { if (currentOffer == offerVersion) RequestChoice(index); },
            () => IsSelectionInputReady, () => selectionGate.Epoch, run.DescribeUpgrade);
        cardUI.transform.SetAsLastSibling();
    }
    private void RequestChoice(int index)
    {
        if (!IsSelectionInputReady) return;
        authorizedSlot = index;
        try { Choose(index); } finally { authorizedSlot = -1; }
    }
    public void Choose(int index)
    {
        if (authorizedSlot != index || !IsSelectionInputReady || index < 0 || index >= offers.Count || lastSelectionFrame == Time.frameCount) return;
        // Lock before applying: duplicate pointer/keyboard/UnityEvent calls cannot buy twice.
        choosing = false;
        if (!run.Apply(offers[index])) { choosing = true; return; }
        lastSelectionFrame = Time.frameCount;
        pending = Mathf.Max(0, pending - 1); selectionGate.Close(); cardUI.gameObject.SetActive(false); offers.Clear();
        if (pending > 0) Open(); else Time.timeScale = previousTimeScale;
    }
    public void CancelForBattleEnd()
    { ended = true; pending = 0; choosing = false; offerVersion++; selectionGate.Close(); offers.Clear(); if (cardUI != null) cardUI.gameObject.SetActive(false); }
    private static readonly AbyssBar.Inputs.GameAction[] SelectionActions = { AbyssBar.Inputs.GameAction.Upgrade1,AbyssBar.Inputs.GameAction.Upgrade2,AbyssBar.Inputs.GameAction.Upgrade3,AbyssBar.Inputs.GameAction.Upgrade4,AbyssBar.Inputs.GameAction.Confirm,AbyssBar.Inputs.GameAction.Primary };
    private static bool SelectionInputsNeutral()
    {
        for (int button = 0; button < 3; button++)
            if (Input.GetMouseButton(button) || Input.GetMouseButtonDown(button) || Input.GetMouseButtonUp(button)) return false;
        foreach(var action in SelectionActions)for(int slot=0;slot<2;slot++)
        {
            var key=AbyssBar.Inputs.GameInput.Router.Profile.Get(action,slot);var input=AbyssBar.Inputs.GameInput.Router.Input;
            if(input.Held(key)||input.Down(key)||input.Up(key))return false;
        }
        return true;
    }
    private void OnApplicationFocus(bool focused)
    { if (!focused && choosing) { selectionGate.Suspend(Time.frameCount); cardUI.SetInputReady(false); } }
    private void OnApplicationPause(bool paused)
    { applicationPaused = paused; if (paused && choosing) { selectionGate.Suspend(Time.frameCount); cardUI.SetInputReady(false); } }
    private void OnDisable()
    { selectionGate.Suspend(Time.frameCount); if (cardUI != null) cardUI.SetInputReady(false); }
    // Obsolete scene button bindings remain harmless until the old panel is removed.
    public void ChooseMoveSpeed() { }
    public void ChooseAttackSpeed() { }
    public void ChooseMaxHealth() { }
}

