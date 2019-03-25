using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour {

    public Text scoreText;
    public int score;
    public Image scoreBar;

    private Board board;
    private GameData gameData;
    private int numberStars;

    // Start is called before the first frame update
    void Start() {
        board = FindObjectOfType<Board>();
        gameData = FindObjectOfType<GameData>();
        if (scoreBar != null) scoreBar.fillAmount = 0;
    }

    // Update is called once per frame
    void Update() {
        scoreText.text = score.ToString();
    }

    public void IncreaseScore(int amountToIncrease) {
        score += amountToIncrease;
        for (int i = 0; i < board.scoreGoals.Length; i++) {
            if (score > board.scoreGoals[i] && numberStars < i + 1) {
                numberStars++;
            }
        }

        if (gameData != null) {
            int highscore = gameData.saveData.highScores[board.level];

            if (score > highscore) gameData.saveData.highScores[board.level] = score;

            int currentStars = gameData.saveData.stars[board.level];

            if (numberStars > currentStars) gameData.saveData.stars[board.level] = numberStars;

            gameData.Save();
        }

        /*
        if (gameData != null && score > gameData.saveData.highScores[board.level]) {
            gameData.saveData.highScores[board.level] = score;
            gameData.saveData.stars[board.level] = numberStars;
            gameData.Save();
        }
        */
        if (board != null && scoreBar != null) {
            scoreBar.fillAmount = (float)score / (float)board.scoreGoals[board.scoreGoals.Length - 1];
        }
    }
}
