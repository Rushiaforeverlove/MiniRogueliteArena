using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text timerText;

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        healthText.text = "HP: " + currentHealth + " / " + maxHealth;
    }

    public void UpdateLevel(int level)
    {
        levelText.text = "Level: " + level;
    }

    public void UpdateExperience(int currentExperience, int experienceToNextLevel)
    {
        experienceText.text = "EXP: " + currentExperience + " / " + experienceToNextLevel;
    }

    public void UpdateTimer(float elapsedTime)
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        timerText.text = "Time: " + minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}