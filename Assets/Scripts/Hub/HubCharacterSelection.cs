using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HubCharacterSelection : MonoBehaviour
{
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private TMP_Text selectedName;
    [SerializeField] private TMP_Text profileDetails;
    [SerializeField] private AbyssBar.Showcase.HubSelectionShowcase showcase;
    [SerializeField] private HubPlayerMovement hubPlayer;

    [Header("Character Interaction")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject talkButton;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text interactionHint;
    [SerializeField] private HubCharacterSeat[] seats;
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;

    [Header("Battle Departure")]
    [SerializeField] private Transform exitDoor;
    [SerializeField] private GameObject departurePanel;
    [SerializeField] private TMP_Text departureText;
    [SerializeField] private string battleSceneName = "Main";

    [Header("Return From Battle")]
    [Tooltip("Only X is used because the player stays on the horizontal walking lane.")]
    [SerializeField] private Transform returnSpawnPoint;

    [Header("Bartender Interaction")]
    [SerializeField] private Transform bartender;
    [SerializeField] private HubTrainingUI trainingUI;

    [Header("Bulletin Board")]
    [SerializeField] private Transform bulletinBoard;
    [SerializeField] private HubBulletinUI bulletinUI;

    [Header("Wardrobe")]
    [SerializeField] private Transform wardrobe;
    [SerializeField] private HubWardrobeUI wardrobeUI;

    [Header("Skin Draw")]
    [SerializeField] private Transform skinDrawStation;
    [SerializeField] private HubSkinDrawUI skinDrawUI;

    private AbyssBar.Hub.HubInteractionController hubInteractions;
    private AbyssBar.Hub.HubSelectionPanController selectionPan;
    public bool CanPanBeforeSelection=>ready&&!isLoadingBattle&&activeSeat==null&&!HasOpenPanel&&!AbyssBar.Inputs.GameInput.Blocked;
    public bool HasOpenPanel => selectionPanel != null && selectionPanel.activeSelf
        || departurePanel != null && departurePanel.activeSelf
        || trainingUI != null && trainingUI.IsOpen || bulletinUI != null && bulletinUI.IsOpen
        || wardrobeUI != null && wardrobeUI.IsOpen || skinDrawUI != null && skinDrawUI.IsOpen;
    public bool CanUseHubInteraction => ready && !isLoadingBattle && activeSeat != null && !HasOpenPanel && !AbyssBar.Inputs.GameInput.Blocked;

    private SpriteRenderer playerRenderer;
    private Rigidbody2D playerBody;
    private TMP_Text confirmButtonLabel;
    private HubCharacterSeat previewSeat;
    private HubCharacterSeat activeSeat;
    private bool ready;
    private bool isLoadingBattle;

    private void Start()
    {
        // Screen-space UI is not a physical actor. Ignore an accidental Rigidbody2D
        // on its Canvas, without removing a serialized component or touching world bodies.
        var uiCanvas=GetComponent<Canvas>();var uiBody=GetComponent<Rigidbody2D>();
        if(uiCanvas!=null&&uiCanvas.renderMode!=RenderMode.WorldSpace&&uiBody!=null)uiBody.simulated=false;
        if (selectionPanel == null || selectedName == null || hubPlayer == null
            || confirmButton == null || talkButton == null || dialogueText == null
            || interactionHint == null || seats == null || seats.Length == 0
            || exitDoor == null || departurePanel == null || departureText == null
            || bartender == null || trainingUI == null || !trainingUI.IsConfigured
            || bulletinBoard == null || bulletinUI == null || !bulletinUI.IsConfigured
            || wardrobe == null || wardrobeUI == null || !wardrobeUI.IsConfigured
            || skinDrawStation == null || skinDrawUI == null || !skinDrawUI.IsConfigured)
        {
            Debug.LogError("Assign all HubCharacterSelection and hub panel references, including Skin Draw Station and Skin Draw UI.", this);
            return;
        }

        playerRenderer = hubPlayer.GetComponent<SpriteRenderer>();
        playerBody = hubPlayer.GetComponent<Rigidbody2D>();
        confirmButtonLabel = confirmButton.GetComponentInChildren<TMP_Text>(true);
        if (playerRenderer == null || playerBody == null || confirmButtonLabel == null)
        {
            Debug.LogError("Check HubPlayer components and ConfirmButton's TMP label.", this);
            return;
        }

        Time.timeScale = 1f;
        hubPlayer.SetCanMove(false);
        hubPlayer.gameObject.SetActive(false);
        selectionPanel.SetActive(false);
        departurePanel.SetActive(false);
        trainingUI.Hide();
        bulletinUI.Hide();
        wardrobeUI.Hide();
        skinDrawUI.Hide();
        interactionHint.gameObject.SetActive(false);
        talkButton.SetActive(false);
        dialogueText.gameObject.SetActive(false);
        ready = true;
        SetupInteractionTargets();

        // Stable IDs make duplicate seat references harmless: migration runs once per role.
        foreach (HubCharacterSeat seat in seats)
            if (seat != null) CharacterProgressionSave.MigrateLegacyTraining(seat.Character);

        RestoreSelectedCharacter();
        var cameraFollow=Camera.main!=null?Camera.main.GetComponent<HubCameraFollow>():null;
        if(activeSeat==null&&cameraFollow!=null)
        {
            var anchors=new Transform[seats.Length];
            for(int i=0;i<seats.Length;i++)anchors[i]=seats[i]!=null?seats[i].transform:null;
            cameraFollow.ShowSeatOverview(anchors);
        }
        else cameraFollow?.SnapToPlayer();
        selectionPan=GetComponent<AbyssBar.Hub.HubSelectionPanController>()??gameObject.AddComponent<AbyssBar.Hub.HubSelectionPanController>();
        selectionPan.Configure(this,cameraFollow);
    }

    public void RequestSeatPointerSelection(HubCharacterSeat seat)
    {
        if(selectionPan==null)selectionPan=GetComponent<AbyssBar.Hub.HubSelectionPanController>();
        selectionPan?.QueueSeatClick(seat);
    }

    private void SetupInteractionTargets()
    {
        hubInteractions=GetComponent<AbyssBar.Hub.HubInteractionController>()??gameObject.AddComponent<AbyssBar.Hub.HubInteractionController>();
        hubInteractions.Configure(interactionHint);
        BindTarget(exitDoor,"出戰準備",AbyssBar.Hub.InteractionVerb.Enter,OpenDeparture);
        BindTarget(bartender,"吧檯管理員",AbyssBar.Hub.InteractionVerb.With,OpenTraining);
        BindTarget(bulletinBoard,"公告欄",AbyssBar.Hub.InteractionVerb.With,OpenBulletin);
        BindTarget(wardrobe,"換裝間",AbyssBar.Hub.InteractionVerb.Enter,OpenWardrobe);
        BindTarget(skinDrawStation,"轉蛋機",AbyssBar.Hub.InteractionVerb.With,OpenSkinDraw);
        foreach(var entry in seats)
        {
            var seat=entry;if(seat==null)continue;
            var target=seat.GetComponent<AbyssBar.Hub.HubInteractionTarget>()??seat.gameObject.AddComponent<AbyssBar.Hub.HubInteractionTarget>();
            target.Configure("角色",AbyssBar.Hub.InteractionVerb.With,()=>OpenPreview(seat),
                ()=>CanUseHubInteraction&&seat!=activeSeat&&seat.Character!=null,()=>seat.Character!=null?seat.Character.DisplayName:"角色");
        }
    }
    private void BindTarget(Transform target,string title,AbyssBar.Hub.InteractionVerb verb,System.Action callback)
    {
        if(target==null)return;
        var interaction=target.GetComponent<AbyssBar.Hub.HubInteractionTarget>()??target.gameObject.AddComponent<AbyssBar.Hub.HubInteractionTarget>();
        interaction.Configure(title,verb,callback,()=>CanUseHubInteraction);
    }
    public void RegisterSeat(HubCharacterSeat seat)
    {
        if(seat==null||seats!=null&&System.Array.IndexOf(seats,seat)>=0)return;
        var updated=new HubCharacterSeat[(seats!=null?seats.Length:0)+1];if(seats!=null)System.Array.Copy(seats,updated,seats.Length);updated[updated.Length-1]=seat;seats=updated;
        if(ready)SetupInteractionTargets();
    }
    private void Update()
    {
        if(!ready||isLoadingBattle)return;
        // Unity's fast Play / script reload can retain serialized scene state while
        // clearing runtime delegates. Reconnect adapters once after that transition.
        var exitInteraction=exitDoor!=null?exitDoor.GetComponent<AbyssBar.Hub.HubInteractionTarget>():null;
        if(exitInteraction==null||!exitInteraction.HasRuntimeBinding)SetupInteractionTargets();
        if(AbyssBar.Inputs.GameInput.Blocked){hubInteractions?.Clear();return;}
        if(HasOpenPanel)
        {
            hubInteractions?.Clear();
            if(!AbyssBar.Inputs.GameInput.Down(AbyssBar.Inputs.GameAction.Menu))return;
            if(skinDrawUI.IsOpen)CloseSkinDraw();else if(wardrobeUI.IsOpen)CloseWardrobe();
            else if(bulletinUI.IsOpen)CloseBulletin();else if(trainingUI.IsOpen)CloseTraining();
            else if(departurePanel.activeSelf)CancelDeparture();else if(selectionPanel.activeSelf)ClosePreview();
            return;
        }
        hubInteractions?.Tick(playerBody.position,CanUseHubInteraction,AbyssBar.Inputs.GameInput.Down(AbyssBar.Inputs.GameAction.Interact));
    }

    public void OpenPreview(HubCharacterSeat seat)
    {
        if (!ready || isLoadingBattle || departurePanel.activeSelf || trainingUI.IsOpen || bulletinUI.IsOpen || wardrobeUI.IsOpen || skinDrawUI.IsOpen
            || seat == null || seat.Character == null
            || !seat.gameObject.activeInHierarchy || seat == activeSeat)
            return;

        bool isFirstSelection = activeSeat == null;
        if (!isFirstSelection)
        {
            float distance = Mathf.Abs(seat.transform.position.x - playerBody.position.x);
            if (selectionPanel.activeSelf || distance > interactionDistance)
                return;
        }

        previewSeat = seat;
        CharacterData character = seat.Character;
        string overview = AbyssBar.Characters.CharacterProfileText.Overview(character);
        string details = AbyssBar.Characters.CharacterProfileText.Details(character);
        selectedName.text = profileDetails != null ? overview : overview + "\n\n" + details;
        if (profileDetails != null) profileDetails.text = details;

        confirmButtonLabel.text = isFirstSelection ? "確認角色" : "更換成此角色";
        if (!AbyssBar.Mio.CharacterUnlockSave.IsUnlocked(character)) confirmButtonLabel.text = $"解鎖角色 · {character.UnlockCost} 補給幣";
        talkButton.SetActive(!isFirstSelection);
        dialogueText.gameObject.SetActive(!isFirstSelection);
        dialogueText.text = seat.Greeting;
        hubPlayer.SetCanMove(false);
        interactionHint.gameObject.SetActive(false);
        selectionPanel.SetActive(true);
        if (showcase != null) showcase.Show(seat);
    }

    public void StartDailyDialogue()
    {
        if (!ready || activeSeat == null || previewSeat == null || !selectionPanel.activeSelf)
            return;

        dialogueText.text = previewSeat.DailyDialogue;
    }

    public void ConfirmSelection()
    {
        if (!ready || previewSeat == null || !selectionPanel.activeSelf)
            return;

        bool isFirstSelection = activeSeat == null;
        if (!AbyssBar.Mio.CharacterUnlockSave.IsUnlocked(previewSeat.Character))
        {
            if (!AbyssBar.Mio.CharacterUnlockSave.TryPurchase(previewSeat.Character)) { confirmButtonLabel.text = "補給幣不足"; return; }
        }
        if (showcase != null) showcase.Hide();
        ActivateCharacter(previewSeat);

        // Initial entry starts at the seat; later swaps preserve the walking position.
        if (isFirstSelection)
            playerBody.position = new Vector2(activeSeat.transform.position.x, playerBody.position.y);

        Camera.main?.GetComponent<HubCameraFollow>()?.SnapToPlayer();

        selectionPanel.SetActive(false);
        previewSeat = null;
        hubPlayer.SetCanMove(true);
    }

    // Share the same visual and seat changes for manual selection and returning.
    private void ActivateCharacter(HubCharacterSeat seat)
    {
        CharacterData character = seat.Character;
        ApplyPlayerAppearance(seat);

        if (activeSeat != null)
            activeSeat.gameObject.SetActive(true);

        CharacterSelection.Current = character;
        activeSeat = seat;
        hubPlayer.gameObject.SetActive(true);
        activeSeat.gameObject.SetActive(false);
    }

    private void ApplyPlayerAppearance(HubCharacterSeat seat)
    {
        CharacterData character = seat.Character;
        SpriteRenderer seatRenderer = seat.GetComponent<SpriteRenderer>();
        Sprite sprite = CharacterSkinSave.GetGameplaySprite(character);
        playerRenderer.sprite = sprite != null ? sprite : seatRenderer.sprite;
        playerRenderer.color = CharacterSkinSave.GetEquippedSkin(character) != null
            ? CharacterSkinSave.GetTint(character)
            : sprite != null ? Color.white : seatRenderer.color;
        AbyssBar.Mio.MioBootstrap.Configure(hubPlayer.gameObject, character, true);
        (hubPlayer.GetComponent<AbyssBar.Hub.CharacterSizePresenter>()??hubPlayer.gameObject.AddComponent<AbyssBar.Hub.CharacterSizePresenter>()).Configure(character,true);
    }

    private void RestoreSelectedCharacter()
    {
        CharacterData previousCharacter = CharacterSelection.Current;
        if (previousCharacter == null) return;
        if (!AbyssBar.Mio.CharacterUnlockSave.IsUnlocked(previousCharacter)) { CharacterSelection.Current = null; return; }

        // Match the saved choice to this newly loaded scene's seat objects.
        foreach (HubCharacterSeat seat in seats)
        {
            if (seat == null || seat.Character == null) continue;

            bool sameAsset = seat.Character == previousCharacter;
            bool sameId = !string.IsNullOrWhiteSpace(previousCharacter.CharacterId)
                && seat.Character.CharacterId == previousCharacter.CharacterId;
            if (!sameAsset && !sameId) continue;

            ActivateCharacter(seat);
            if (returnSpawnPoint != null)
                playerBody.position = new Vector2(returnSpawnPoint.position.x, playerBody.position.y);

            hubPlayer.SetCanMove(true);
            return;
        }

        // Missing/removed characters fall back to the normal first-choice flow.
        CharacterSelection.Current = null;
        Debug.LogWarning("No matching seat was found. Select a character again.", this);
    }

    public void ClosePreview()
    {
        if (!ready || isLoadingBattle || departurePanel.activeSelf || trainingUI.IsOpen || bulletinUI.IsOpen || wardrobeUI.IsOpen || skinDrawUI.IsOpen) return;

        selectionPanel.SetActive(false);
        previewSeat = null;
        hubPlayer.SetCanMove(activeSeat != null);
        if (showcase != null) showcase.Hide();
    }

    private void OpenDeparture()
    {
        if (activeSeat == null || selectionPanel.activeSelf || trainingUI.IsOpen || bulletinUI.IsOpen || wardrobeUI.IsOpen || skinDrawUI.IsOpen) return;

        departureText.text = $"出戰角色：{activeSeat.Character.DisplayName}\n\n"
            + "準備好離開酒吧，前往地面了嗎？";
        hubPlayer.SetCanMove(false);
        interactionHint.gameObject.SetActive(false);
        departurePanel.SetActive(true);
    }

    // Wire this to DepartureConfirmButton's On Click event.
    public void ConfirmDeparture()
    {
        if (!ready || isLoadingBattle || activeSeat == null || !departurePanel.activeSelf)
            return;

        if (string.IsNullOrWhiteSpace(battleSceneName)
            || !Application.CanStreamedLevelBeLoaded(battleSceneName))
        {
            departureText.text = "找不到戰鬥場景。\n請將 Main 加入 Build Profiles 的 Scene List，"
                + "並確認 Battle Scene Name 與場景名稱相同。";
            Debug.LogError($"Cannot load battle scene '{battleSceneName}'. Check the active build profile's scene list.", this);
            return;
        }

        // Combat's PlayerCharacter reads this selection when Main starts.
        CharacterSelection.Current = activeSeat.Character;
        isLoadingBattle = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(battleSceneName);
    }

    // Wire this to DepartureCancelButton's On Click event.
    public void CancelDeparture()
    {
        if (!ready || isLoadingBattle || !departurePanel.activeSelf) return;

        departurePanel.SetActive(false);
        hubPlayer.SetCanMove(activeSeat != null);
    }

    private void OpenTraining()
    {
        if (activeSeat == null || selectionPanel.activeSelf || departurePanel.activeSelf || bulletinUI.IsOpen || wardrobeUI.IsOpen || skinDrawUI.IsOpen)
            return;

        if (!trainingUI.Show(activeSeat.Character)) return;

        hubPlayer.SetCanMove(false);
        interactionHint.gameObject.SetActive(false);
    }

    // Wire TrainingCloseButton to Canvas > HubCharacterSelection > CloseTraining().
    public void CloseTraining()
    {
        if (!ready || isLoadingBattle || !trainingUI.IsOpen) return;

        trainingUI.Hide();
        hubPlayer.SetCanMove(activeSeat != null);
    }

    private void OpenBulletin()
    {
        if (activeSeat == null || selectionPanel.activeSelf || departurePanel.activeSelf || trainingUI.IsOpen || wardrobeUI.IsOpen || skinDrawUI.IsOpen)
            return;

        if (!bulletinUI.Show()) return;

        hubPlayer.SetCanMove(false);
        interactionHint.gameObject.SetActive(false);
    }

    // Wire BulletinCloseButton to this method so closing also restores walking.
    public void CloseBulletin()
    {
        if (!ready || isLoadingBattle || !bulletinUI.IsOpen) return;

        bulletinUI.Hide();
        hubPlayer.SetCanMove(activeSeat != null);
    }

    private void OpenWardrobe()
    {
        if (activeSeat == null || selectionPanel.activeSelf || departurePanel.activeSelf
            || trainingUI.IsOpen || bulletinUI.IsOpen || skinDrawUI.IsOpen)
            return;

        if (!wardrobeUI.Show(activeSeat.Character)) return;

        hubPlayer.SetCanMove(false);
        interactionHint.gameObject.SetActive(false);
    }

    // Apply the saved choice to the current player and seats when the modal closes.
    public void CloseWardrobe()
    {
        if (!ready || isLoadingBattle || !wardrobeUI.IsOpen) return;

        wardrobeUI.Hide();
        RefreshCharacterAppearances();
        hubPlayer.SetCanMove(activeSeat != null);
    }

    public void RefreshCharacterAppearances()
    {
        foreach (HubCharacterSeat seat in seats)
        {
            if (seat != null) seat.RefreshAppearance();
        }
        if (activeSeat != null) ApplyPlayerAppearance(activeSeat);
    }

    private void OpenSkinDraw()
    {
        if (activeSeat == null || selectionPanel.activeSelf || departurePanel.activeSelf
            || trainingUI.IsOpen || bulletinUI.IsOpen || wardrobeUI.IsOpen)
            return;

        if (!skinDrawUI.Show()) return;

        hubPlayer.SetCanMove(false);
        interactionHint.gameObject.SetActive(false);
    }

    public void CloseSkinDraw()
    {
        if (!ready || isLoadingBattle || !skinDrawUI.IsOpen) return;

        skinDrawUI.Hide();
        hubPlayer.SetCanMove(activeSeat != null);
    }
}
