using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ConfirmPanel : MonoBehaviour {

    [Header ("Level Information")]
    public string levelToLoad;
    public int level;
    private GameData gameData;
    private int starsActive;
    private int highscore;

    [Header ("UI Stuff")]
    public Image[] stars;
    public Text highScoreText;
    public Text starText;

    // Start is called before the first frame update
    void OnEnable() {
        gameData = FindObjectOfType<GameData>();
        LoadData();
        ActivateStars();
        SetText();
    }

    void LoadData() {
        if (gameData != null) {
            starsActive = gameData.saveData.stars[level - 1];
            highscore = gameData.saveData.highScores[level - 1];
        }
    }

    void SetText() {
        highScoreText.text = highscore.ToString();
        starText.text = starsActive.ToString() + "/3";
    }

    void ActivateStars() {
        //TODO when binary file is done, change this!
        for (int i = 0; i < starsActive; i++) {
            stars[i].enabled = true;
        }
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void Cancel() {
        this.gameObject.SetActive(false);
    }

    public void Play() {
        PlayerPrefs.SetInt("Current Level", level - 1);
        SceneManager.LoadScene(levelToLoad);
    }

}
