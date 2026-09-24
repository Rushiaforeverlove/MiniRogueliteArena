using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;

    private void Awake()
    {
        resultPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        resultPanel.SetActive(true);
        resultTitleText.text = "Victory";
        Time.timeScale = 0f;
    }

    public void ShowGameOver()
    {
        resultPanel.SetActive(true);
        resultTitleText.text = "Game Over";
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}