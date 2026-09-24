using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float victoryTime = 180f;
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private GameResultUI gameResultUI;

    private float elapsedTime;
    private bool isGameOver;

    private void Update()
    {
        if (isGameOver)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        gameHUD.UpdateTimer(elapsedTime);

        if (elapsedTime >= victoryTime)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        isGameOver = true;
        gameResultUI.ShowVictory();
    }
}