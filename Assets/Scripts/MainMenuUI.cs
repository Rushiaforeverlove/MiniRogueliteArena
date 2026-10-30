using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private CharacterData selectedCharacter;
    [SerializeField] private TMP_Text selectedCharacterText;
    public void StartGame()
    {
        if (selectedCharacter == null) return;
        CharacterSelection.Current = selectedCharacter;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }

    private void Start()
    {
        SelectCharacter(selectedCharacter);
    }

    public void SelectCharacter(CharacterData character)
    {
        if (character == null) return;

        selectedCharacter = character;

        string weaponName = character.StartingWeapon != null
            ? character.StartingWeapon.DisplayName
            : "未指定";

        selectedCharacterText.text =
            $"{character.DisplayName}\n"
            + $"初始生命值：{character.BaseMaxHealth}\n"
            + $"移動速度：{character.BaseMoveSpeed:0.#}\n"
            + $"初始武器：{weaponName}";
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}