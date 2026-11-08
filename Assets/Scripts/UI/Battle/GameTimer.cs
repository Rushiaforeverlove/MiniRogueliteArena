using UnityEngine;

// Time is a result statistic, never a victory condition.
public class GameTimer : MonoBehaviour
{
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private GameResultUI gameResultUI;
    private float elapsedTime;
    private bool stopped;
    public float ElapsedTime => elapsedTime;
    private void Awake()
    {
        if (gameResultUI == null) gameResultUI = FindAnyObjectByType<GameResultUI>();
        if (gameResultUI != null) gameResultUI.BattleFinished += OnBattleFinished;
    }
    private void Update()
    {
        if (stopped || Time.timeScale <= 0 || gameResultUI != null && gameResultUI.HasSettled) return;
        elapsedTime += Time.deltaTime;
        gameHUD?.UpdateTimer(elapsedTime);
    }
    private void OnBattleFinished(bool victory) { stopped = true; }
    private void OnDisable()
    { if (gameResultUI != null) gameResultUI.BattleFinished -= OnBattleFinished; stopped = true; }
}
