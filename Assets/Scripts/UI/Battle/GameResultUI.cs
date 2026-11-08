using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResultUI : MonoBehaviour
{
    public event System.Action<bool> BattleFinished;
    public bool HasSettled => hasSettled;
    public bool IsConfigured => ready && !isLoading;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;

    [Header("Battle Rewards")]
    [SerializeField] private TMP_Text rewardText;
    [SerializeField, Min(0)] private int victoryReward = 100;
    [SerializeField, Min(0)] private int defeatReward = 20;

    [Header("Skin Ticket Rewards")]
    [SerializeField, Min(0)] private int victorySkinTicketReward = 3;
    [SerializeField, Min(0)] private int defeatSkinTicketReward = 1;

    private bool isLoading;
    private bool ready;
    private bool hasSettled;

    private void Awake()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultPanel == null || resultTitleText == null || rewardText == null)
        {
            Debug.LogError("Assign Result Panel, Result Title Text and Reward Text on GameResultUI.", this);
            return;
        }

        ready = true;
    }

    public void ShowVictory()
    {
        ShowResult(true);
    }

    public void ShowGameOver()
    {
        ShowResult(false);
    }

    private void ShowResult(bool isVictory)
    {
        // Both victory and defeat enter here; only the first outcome pays out.
        if (!ready || hasSettled || isLoading) return;

        hasSettled = true;
        BattleFinished?.Invoke(isVictory);
        FindAnyObjectByType<UpgradeManager>()?.CancelForBattleEnd();
        FindAnyObjectByType<AbyssBar.Upgrades.RunUpgradeRuntime>()?.EndBattle();
        resultPanel.SetActive(true);
        resultTitleText.text = isVictory ? "Victory" : "Game Over";
        Time.timeScale = 0f;

        int reward = Mathf.Max(0, isVictory ? victoryReward : defeatReward);
        int ticketReward = Mathf.Max(0, isVictory ? victorySkinTicketReward : defeatSkinTicketReward);
        CurrencySave.GrantBattleRewards(reward, ticketReward, out int grantedAmount, out int grantedTickets);
        rewardText.text = $"本次獲得：補給幣 +{grantedAmount}\n"
            + $"本次獲得：造型券 +{grantedTickets}\n\n"
            + $"持有補給幣：{CurrencySave.SupplyCoins}\n"
            + $"持有造型券：{CurrencySave.SkinTickets}";
    }

    public void RestartGame()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToHub()
    {
        // Keep CharacterSelection.Current so the hub restores the chosen role.
        LoadScene("BarHub");
    }

    public void ReturnToMainMenu()
    {
        LoadScene("MainMenu");
    }

    private void LoadScene(string sceneName)
    {
        if (!ready || isLoading) return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Add '{sceneName}' to the active build profile's scene list.", this);
            return;
        }

        Time.timeScale = 1f;
        isLoading = true;
        SceneManager.LoadScene(sceneName);
    }
}


