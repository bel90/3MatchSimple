using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour {

    public GameObject startPanel;
    public GameObject levelPanel;
    public GameData gameData;

    void Start() {
        gameData = FindObjectOfType<GameData>();
        if (!gameData.wasStarted) {
            startPanel.SetActive(true);
            levelPanel.SetActive(false);
        } else {
            startPanel.SetActive(false);
            levelPanel.SetActive(true);
        }
    }

    public void PlayGame() {
        startPanel.SetActive(false);
        levelPanel.SetActive(true);
        gameData.wasStarted = true;
        Debug.Log("play game");
    }

    public void Home() {
        startPanel.SetActive(true);
        levelPanel.SetActive(false);
        Debug.Log("go home");
    }
}
