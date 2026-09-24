using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAutoAttack playerAutoAttack;
    [SerializeField] private PlayerHealth playerHealth;

    private bool isChoosingUpgrade;

    private void Awake()
    {
        upgradePanel.SetActive(false);
    }

    private void Update()
    {
        if (isChoosingUpgrade == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChooseMoveSpeed();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChooseAttackSpeed();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChooseMaxHealth();
        }
    }

    public void ShowUpgradeChoices()
    {
        isChoosingUpgrade = true;
        upgradePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ChooseMoveSpeed()
    {
        playerController.AddMoveSpeed(0.5f);
        CloseUpgradeChoices();
    }

    public void ChooseAttackSpeed()
    {
        playerAutoAttack.ReduceAttackInterval(0.08f);
        CloseUpgradeChoices();
    }

    public void ChooseMaxHealth()
    {
        playerHealth.AddMaxHealth(1);
        CloseUpgradeChoices();
    }

    private void CloseUpgradeChoices()
    {
        isChoosingUpgrade = false;
        upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }
}