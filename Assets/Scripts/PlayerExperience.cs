using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience;
    [SerializeField] private int experienceToNextLevel = 5;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private GameHUD gameHUD;

    private void Start()
    {
        gameHUD.UpdateLevel(currentLevel);
        gameHUD.UpdateExperience(currentExperience, experienceToNextLevel);
    }

    public void AddExperience(int amount)
    {
        currentExperience += amount;
        Debug.Log("Player EXP: " + currentExperience + " / " + experienceToNextLevel);

        while (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }

        gameHUD.UpdateExperience(currentExperience, experienceToNextLevel);
    }

    private void LevelUp()
    {
        currentExperience -= experienceToNextLevel;
        currentLevel++;

        experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.4f);

        Debug.Log("Level Up! Current Level: " + currentLevel);
        Debug.Log("Next Level EXP: " + experienceToNextLevel);

        gameHUD.UpdateLevel(currentLevel);
        upgradeManager.ShowUpgradeChoices();
    }
}