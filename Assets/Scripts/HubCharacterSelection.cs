using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HubCharacterSelection : MonoBehaviour
{
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private TMP_Text selectedName;
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

    private enum InteractionTarget
    {
        None,
        Character,
        Exit,
        Bartender,
        BulletinBoard,
        Wardrobe,
        SkinDraw
    }

    private SpriteRenderer playerRenderer;
    private Rigidbody2D playerBody;
    private TMP_Text confirmButtonLabel;
    private HubCharacterSeat previewSeat;
    private HubCharacterSeat activeSeat;
    private bool ready;
    private bool isLoadingBattle;

    private void Start()
    {
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

        // Stable IDs make duplicate seat references harmless: migration runs once per role.
        foreach (HubCharacterSeat seat in seats)
            if (seat != null) CharacterProgressionSave.MigrateLegacyTraining(seat.Character);

        RestoreSelectedCharacter();
    }

    private void Update()
    {
        if (!ready || isLoadingBattle) return;

        if (skinDrawUI.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseSkinDraw();
            return;
        }

        if (wardrobeUI.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseWardrobe();
            return;
        }

        if (bulletinUI.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseBulletin();
            return;
        }

        if (trainingUI.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseTraining();
            return;
        }

        if (departurePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CancelDeparture();
            return;
        }

        if (selectionPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePreview();
            return;
        }

        if (activeSeat == null || selectionPanel.activeSelf)
        {
            interactionHint.gameObject.SetActive(false);
            return;
        }

        InteractionTarget target = GetNearbyInteraction(out HubCharacterSeat nearbySeat);
        interactionHint.gameObject.SetActive(target != InteractionTarget.None);
        if (target == InteractionTarget.None) return;

        switch (target)
        {
            case InteractionTarget.Character:
                interactionHint.text = $"按 E：與{nearbySeat.Character.DisplayName}互動";
                break;
            case InteractionTarget.Exit:
                interactionHint.text = "按 E：前往地面出戰";
                break;
            case InteractionTarget.Bartender:
                interactionHint.text = "按 E：與吧檯管理員交談";
                break;
            case InteractionTarget.BulletinBoard:
                interactionHint.text = "按 E：查看酒吧情報";
                break;
            case InteractionTarget.Wardrobe:
                interactionHint.text = "按 E：更換角色造型";
                break;
            case InteractionTarget.SkinDraw:
                interactionHint.text = "按 E：造型抽選";
                break;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            switch (target)
            {
                case InteractionTarget.Character:
                    OpenPreview(nearbySeat);
                    break;
                case InteractionTarget.Exit:
                    OpenDeparture();
                    break;
                case InteractionTarget.Bartender:
                    OpenTraining();
                    break;
                case InteractionTarget.BulletinBoard:
                    OpenBulletin();
                    break;
                case InteractionTarget.Wardrobe:
                    OpenWardrobe();
                    break;
                case InteractionTarget.SkinDraw:
                    OpenSkinDraw();
                    break;
            }
        }
    }

    // Choose one nearest target so a single E press never opens two panels.
    private InteractionTarget GetNearbyInteraction(out HubCharacterSeat nearbySeat)
    {
        nearbySeat = GetNearbySeat();
        InteractionTarget target = nearbySeat != null
            ? InteractionTarget.Character : InteractionTarget.None;
        float nearestDistance = nearbySeat != null
            ? Mathf.Abs(nearbySeat.transform.position.x - playerBody.position.x)
            : interactionDistance;

        float doorDistance = Mathf.Abs(exitDoor.position.x - playerBody.position.x);
        if (exitDoor.gameObject.activeInHierarchy && doorDistance <= nearestDistance)
        {
            nearestDistance = doorDistance;
            target = InteractionTarget.Exit;
        }

        // The bartender stands behind the bar, so use X distance along the walking lane.
        float bartenderDistance = Mathf.Abs(bartender.position.x - playerBody.position.x);
        if (bartender.gameObject.activeInHierarchy && bartenderDistance <= nearestDistance)
        {
            nearestDistance = bartenderDistance;
            target = InteractionTarget.Bartender;
        }

        float boardDistance = Mathf.Abs(bulletinBoard.position.x - playerBody.position.x);
        if (bulletinBoard.gameObject.activeInHierarchy && boardDistance <= nearestDistance)
        {
            nearestDistance = boardDistance;
            target = InteractionTarget.BulletinBoard;
        }

        float wardrobeDistance = Mathf.Abs(wardrobe.position.x - playerBody.position.x);
        if (wardrobe.gameObject.activeInHierarchy && wardrobeDistance <= nearestDistance)
        {
            nearestDistance = wardrobeDistance;
            target = InteractionTarget.Wardrobe;
        }

        float drawDistance = Mathf.Abs(skinDrawStation.position.x - playerBody.position.x);
        if (skinDrawStation.gameObject.activeInHierarchy && drawDistance <= nearestDistance)
            target = InteractionTarget.SkinDraw;

        return target;
    }

    private HubCharacterSeat GetNearbySeat()
    {
        HubCharacterSeat nearestSeat = null;
        float nearestDistance = interactionDistance;

        foreach (HubCharacterSeat seat in seats)
        {
            if (seat == null || seat == activeSeat || seat.Character == null
                || !seat.gameObject.activeInHierarchy)
                continue;

            // The lobby uses one horizontal walking lane.
            float distance = Mathf.Abs(seat.transform.position.x - playerBody.position.x);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestSeat = seat;
            }
        }

        return nearestSeat;
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
        string weaponName = character.StartingWeapon != null
            ? character.StartingWeapon.DisplayName : "未指定";

        selectedName.text = $"{character.DisplayName}\n"
            + $"初始生命值：{CurrencySave.GetStartingMaxHealth(character)}\n"
            + $"移動速度：{character.BaseMoveSpeed:0.#}\n"
            + $"初始武器：{weaponName}";

        confirmButtonLabel.text = isFirstSelection ? "確認角色" : "更換成此角色";
        talkButton.SetActive(!isFirstSelection);
        dialogueText.gameObject.SetActive(!isFirstSelection);
        dialogueText.text = seat.Greeting;
        hubPlayer.SetCanMove(false);
        interactionHint.gameObject.SetActive(false);
        selectionPanel.SetActive(true);
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
        ActivateCharacter(previewSeat);

        // Initial entry starts at the seat; later swaps preserve the walking position.
        if (isFirstSelection)
            playerBody.position = new Vector2(activeSeat.transform.position.x, playerBody.position.y);

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
    }

    private void RestoreSelectedCharacter()
    {
        CharacterData previousCharacter = CharacterSelection.Current;
        if (previousCharacter == null) return;

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
        foreach (HubCharacterSeat seat in seats)
        {
            if (seat != null) seat.RefreshAppearance();
        }
        if (activeSeat != null) ApplyPlayerAppearance(activeSeat);
        hubPlayer.SetCanMove(activeSeat != null);
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
