using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private bool isLoading;

    public void StartGame()
    {
        if (isLoading) return;

        if (!Application.CanStreamedLevelBeLoaded("BarHub"))
        {
            Debug.LogError("Add BarHub to the active build profile's scene list.", this);
            return;
        }

        // Starting from the title screen always begins with choosing a seat.
        CharacterSelection.Current = null;
        Time.timeScale = 1f;
        isLoading = true;
        SceneManager.LoadScene("BarHub");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
