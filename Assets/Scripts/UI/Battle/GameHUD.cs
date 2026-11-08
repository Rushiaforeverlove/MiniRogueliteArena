using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text waveDetailText;
    private int shownHealth, shownMaxHealth;
    private float shownShield;

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        shownHealth = currentHealth; shownMaxHealth = maxHealth; RefreshHealth();
    }
    public void UpdateShield(float amount) { shownShield = amount; RefreshHealth(); }
    private void RefreshHealth()
    {
        if (healthText != null) healthText.text = $"HP: {shownHealth:N0} / {shownMaxHealth:N0}"
            + (shownShield > 0 ? $"\n屏障：{Mathf.CeilToInt(shownShield):N0}" : "");
    }

    public void UpdateLevel(int level)
    {
        levelText.text = "Level: " + level;
    }

    public void UpdateExperience(int currentExperience, int experienceToNextLevel)
    {
        experienceText.text = $"EXP: {currentExperience} / {experienceToNextLevel}";
    }

    public void UpdateTimer(float elapsedTime)
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = "Time: " + minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    public void UpdateWave(int wave)
    {
        waveText.text = "Wave: " + wave;
    }
    public void UpdateWaveProgress(AbyssBar.Waves.WaveBattleProgress progress)
    {
        if (waveText != null) waveText.text = $"Wave: {progress.CurrentWave} / {progress.TotalWaves}"
            + (waveDetailText == null ? $" | Left: {progress.Remaining}" : "");
        if (waveDetailText != null) waveDetailText.text = AbyssBar.Waves.WaveStatusFormatter.Details(progress);
    }
}
