using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState {
    wait,
    move,
    win,
    lose,
    pause
}

public enum TileKind {
    Breakable,
    Blank,
    Lock,
    Concrete,
    Slime,
    Normal
}

[System.Serializable]
public class MatchType {
    public int type;
    public string color;
}

[System.Serializable] //Damit Unity die Klasse kennt
public class TileType {
    public int x;
    public int y;
    public TileKind tileKind;
}

public class Board : MonoBehaviour {

    [Header("Scriptable Object Stuff")]
    public World world;
    public int level;

    [Header("Board Dimensions")]
    public GameState currentState = GameState.move;
    public int width;
    public int height;
    public int offSet;

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject breakableTilePrefab;
    public GameObject lockTilePrefab;
    public GameObject concreteTilePrefab;
    public GameObject slimeTilePrefab;
    public GameObject[] dots;
    public GameObject[,] allDots;
    public Dot currentDot;
    public GameObject destroyEffect;

    [Header("Layout")]
    public TileType[] boardLayout;

    private bool[,] blankSpaces;
    private BackgroundTile[,] breakableTiles;
    public BackgroundTile[,] lockTiles;
    private BackgroundTile[,] concreteTiles;
    private BackgroundTile[,] slimeTiles;

    [Header("Match Stuff")]
    public MatchType matchType;
    private FindMatches findMatches;
    public int basePieceValue = 20;
    public int streakValue = 1;
    private ScoreManager scoreManager;
    private SoundManager soundManger;
    private GoalManager goalManager;
    public float refillDelay = 0.5f;
    public int[] scoreGoals;
    private bool makeSlime = true;

    private void Awake() {
        if (PlayerPrefs.HasKey("Current Level")) {
            level = PlayerPrefs.GetInt("Current Level");
        }
        if (world != null && (level < world.levels.Length)) {
            if (world.levels[level] != null) {
                width = world.levels[level].width;
                height = world.levels[level].height;
                dots = world.levels[level].dots;
                scoreGoals = world.levels[level].scoreGoals;
                boardLayout = world.levels[level].boardLayout;
            }
        }
    }

    // Start is called before the first frame update
    void Start() {
        scoreManager = FindObjectOfType<ScoreManager>();
        soundManger = FindObjectOfType<SoundManager>();
        goalManager = FindObjectOfType<GoalManager>();

        breakableTiles = new BackgroundTile[width, height];
        lockTiles = new BackgroundTile[width, height];
        concreteTiles = new BackgroundTile[width, height];
        slimeTiles = new BackgroundTile[width, height];

        findMatches = FindObjectOfType<FindMatches>();
        blankSpaces = new bool[width, height];
        allDots = new GameObject[width, height];
        SetUp();
        currentState = GameState.pause;
    }

    public void GenerateBlankSpaces() {
        for (int i = 0; i < boardLayout.Length; i++) {
            if (boardLayout[i].tileKind == TileKind.Blank) {
                blankSpaces[boardLayout[i].x, boardLayout[i].y] = true;
            }
        }
    }

    public void GenerateBreakableTiles() {
        for (int i = 0; i < boardLayout.Length; i++) {
            if (boardLayout[i].tileKind == TileKind.Breakable) {
                Vector2 tempPosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                GameObject tile = Instantiate(breakableTilePrefab, tempPosition, Quaternion.identity);
                breakableTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
            }
        }
    }

    private void GenerateLockTiles() {
        for (int i = 0; i < boardLayout.Length; i++) {
            if (boardLayout[i].tileKind == TileKind.Lock) {
                Vector2 tempPosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                GameObject tile = Instantiate(lockTilePrefab, tempPosition, Quaternion.identity);
                lockTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
            }
        }
    }

    private void GenerateConcreteTiles() {
        for (int i = 0; i < boardLayout.Length; i++) {
            if (boardLayout[i].tileKind == TileKind.Concrete) {
                Vector2 tempPosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                GameObject tile = Instantiate(concreteTilePrefab, tempPosition, Quaternion.identity);
                concreteTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
            }
        }
    }

    private void GenerateSlimeTiles() {
        for (int i = 0; i < boardLayout.Length; i++) {
            if (boardLayout[i].tileKind == TileKind.Slime) {
                Vector2 tempPosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                GameObject tile = Instantiate(slimeTilePrefab, tempPosition, Quaternion.identity);
                slimeTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
            }
        }
    }

    private void SetUp() {
        GenerateBlankSpaces();
        GenerateBreakableTiles();
        GenerateLockTiles();
        GenerateConcreteTiles();
        GenerateSlimeTiles();
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (!blankSpaces[i, j] && !concreteTiles[i, j] && !slimeTiles[i, j]) {
                    Vector2 tempPosition = new Vector2(i, j + offSet);
                    Vector2 tilePosition = new Vector2(i, j);
                    GameObject backgroundTile = Instantiate(tilePrefab, tilePosition, Quaternion.identity) as GameObject;
                    backgroundTile.transform.parent = this.transform;
                    backgroundTile.name = "( " + i + ", " + j + " )";

                    int dotToUse = Random.Range(0, dots.Length);

                    int maxIterations = 0;
                    while (MatchesAt(i, j, dots[dotToUse]) && maxIterations < 100) {
                        dotToUse = Random.Range(0, dots.Length);
                        maxIterations++;
                    }
                    maxIterations = 0;

                    GameObject dot = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);
                    dot.GetComponent<Dot>().row = j;
                    dot.GetComponent<Dot>().column = i;
                    dot.transform.parent = this.transform;
                    dot.name = "( " + i + ", " + j + " )";

                    allDots[i, j] = dot;
                }
            }
        }
    }

    private bool MatchesAt(int column, int row, GameObject piece) {
        if (column > 1 && row > 1) {
            if (allDots[column - 1, row] != null && allDots[column - 2, row] != null) {
                if (allDots[column - 1, row].tag == piece.tag && allDots[column - 2, row].tag == piece.tag) {
                    return true;
                }
            }
            if (allDots[column, row - 1] != null && allDots[column, row - 2] != null) {
                if (allDots[column, row - 1].tag == piece.tag && allDots[column, row - 2].tag == piece.tag) {
                    return true;
                }
            }
        } else if (column <= 1 || row <= 1) {
            if (row > 1) {
                if (allDots[column, row - 1] != null && allDots[column, row - 2] != null) {
                    if (allDots[column, row - 1].tag == piece.tag && allDots[column, row - 2].tag == piece.tag) {
                        return true;
                    }
                }
            }
            if (column > 1) {
                if (allDots[column - 1, row] != null && allDots[column - 2, row] != null) {
                    if (allDots[column - 1, row].tag == piece.tag && allDots[column - 2, row].tag == piece.tag) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private MatchType ColumnOrRow() {

        List<GameObject> matchCopy = findMatches.currentMatches as List<GameObject>;

        matchType.type = 0;
        matchType.color = "";

        for (int i = 0; i < matchCopy.Count; i++) {
            Dot thisDot = matchCopy[i].GetComponent<Dot>();
            string color = matchCopy[i].tag;
            int column = thisDot.column;
            int row = thisDot.row;
            int columnMatch = 0;
            int rowMatch = 0;

            for (int j = 0; j < matchCopy.Count; j++) {
                Dot nextDot = matchCopy[j].GetComponent<Dot>();
                if (nextDot == thisDot) {
                    continue;
                }
                if (nextDot.column == thisDot.column && nextDot.tag == color) { // oder nextDot.CompareTag(thisDot.tag)
                    columnMatch++;
                }
                if (nextDot.row == thisDot.row && nextDot.tag == color) {
                    rowMatch++;
                }
            }
            //Return 3 if column or row
            // 2 adjacent
            // 1 color
            if (columnMatch == 4 || rowMatch == 4) {
                matchType.type = 1;
                matchType.color = color;
                return matchType;
            }
            else if (columnMatch == 2 || rowMatch == 2) {
                matchType.type = 2;
                matchType.color = color;
                return matchType;
            }
            else if (columnMatch == 3 || rowMatch == 3) {
                matchType.type = 3;
                matchType.color = color;
                return matchType;
            }
        }
        matchType.type = 0;
        matchType.color = "";
        return matchType;
    }

    private void CheckToMakeBombs() {
        if (findMatches.currentMatches.Count > 3) {
            MatchType typeOfMatch = ColumnOrRow();
            if (typeOfMatch.type == 1) {
                if (currentDot != null && currentDot.isMatched && currentDot.tag == typeOfMatch.color) {
                    currentDot.isMatched = false;
                    currentDot.MakeColorBomb();
                }
                else if (currentDot.otherDot != null) {
                    Dot otherDot = currentDot.otherDot.GetComponent<Dot>();
                    if (otherDot.isMatched && otherDot.tag == typeOfMatch.color) {
                        otherDot.isMatched = false;
                        otherDot.MakeColorBomb();
                    }
                }
            }
            else if (typeOfMatch.type == 2) {
                if (currentDot != null && currentDot.isMatched && currentDot.tag == typeOfMatch.color) {
                    currentDot.isMatched = false;
                    currentDot.MakeAdjacentBomb();
                } else if (currentDot.otherDot != null) {
                    Dot otherDot = currentDot.otherDot.GetComponent<Dot>();
                    if (otherDot.isMatched && otherDot.tag == typeOfMatch.color) {
                        otherDot.isMatched = false;
                        otherDot.MakeAdjacentBomb();
                    }
                }
            }
            else if (typeOfMatch.type == 3) {
                findMatches.CheckBombs(matchType);
            }
        }
    }

    public void BombRow(int row) {
        for (int i = 0; i < width; i++) {
            if (concreteTiles[i, row]) {
                concreteTiles[i, row].TakeDamage(1);
                if (concreteTiles[i, row].hitPoints <= 0) {
                    concreteTiles[i, row] = null;
                }
            }
        }
    }

    public void BombColumn(int column) {
        for (int i = 0; i < width; i++) {
            if (concreteTiles[column, i]) {
                concreteTiles[column, i].TakeDamage(1);
                if (concreteTiles[column, i].hitPoints <= 0) {
                    concreteTiles[column, i] = null;
                }
            }
        }
    }

    private void DestroyMatchesAt(int column, int row) {
        if (allDots[column, row].GetComponent<Dot>().isMatched) {
            
            if (breakableTiles[column, row] != null) {
                breakableTiles[column, row].TakeDamage(1);
                if (breakableTiles[column, row].hitPoints <= 0) {
                    breakableTiles[column, row] = null;
                }
            }
            if (lockTiles[column, row] != null) {
                lockTiles[column, row].TakeDamage(1);
                if (lockTiles[column, row].hitPoints <= 0) {
                    lockTiles[column, row] = null;
                }
            }
            DamageConcrete(column, row);
            DamageSlime(column, row);

            if (goalManager != null) {
                goalManager.CompareGoal(allDots[column, row].tag.ToString());
                goalManager.UpdateGoals();
            }

            if (soundManger != null) {
                soundManger.PlayRandomDestroyNoise();
            }
            /*
            GameObject particle = Instantiate(destroyEffect, allDots[column, row].transform.position, Quaternion.identity);
            Destroy(particle, .5f);*/
            allDots[column, row].GetComponent<Dot>().GetComponent<SpriteRenderer>().sortingOrder = 15; //Setze den Dot nach ganz vorne, damit die Animation Sichtbar ist

            allDots[column, row].GetComponent<Dot>().PopAnimation();
            Destroy(allDots[column, row], .5f);
            scoreManager.IncreaseScore(basePieceValue * streakValue);
            allDots[column, row] = null;
        }
    }

    public void DestroyMatches() {
        if (findMatches.currentMatches.Count > 3) {
            CheckToMakeBombs();
        }
        findMatches.currentMatches.Clear();
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    DestroyMatchesAt(i, j);
                }
            }
        }
        
        StartCoroutine(DecreaseRowCo2());
    }

    private void DamageConcrete(int column, int row) {
        if (column > 0) {
            if (concreteTiles[column - 1, row]) {
                concreteTiles[column - 1, row].TakeDamage(1);
                if (concreteTiles[column - 1, row].hitPoints <= 0) {
                    concreteTiles[column - 1, row] = null;
                }
            }
        }
        if (column < width - 1) {
            if (concreteTiles[column + 1, row]) {
                concreteTiles[column + 1, row].TakeDamage(1);
                if (concreteTiles[column + 1, row].hitPoints <= 0) {
                    concreteTiles[column + 1, row] = null;
                }
            }
        }
        if (row > 0) {
            if (concreteTiles[column, row - 1]) {
                concreteTiles[column, row - 1].TakeDamage(1);
                if (concreteTiles[column, row - 1].hitPoints <= 0) {
                    concreteTiles[column, row - 1] = null;
                }
            }
        }
        if (row < height - 1) {
            if (concreteTiles[column, row + 1]) {
                concreteTiles[column, row + 1].TakeDamage(1);
                if (concreteTiles[column, row + 1].hitPoints <= 0) {
                    concreteTiles[column, row + 1] = null;
                }
            }
        }
    }

    private void DamageSlime(int column, int row) {
        if (column > 0) {
            if (slimeTiles[column - 1, row]) {
                slimeTiles[column - 1, row].TakeDamage(1);
                if (slimeTiles[column - 1, row].hitPoints <= 0) {
                    slimeTiles[column - 1, row] = null;
                }
                makeSlime = false;
            }
        }
        if (column < width - 1) {
            if (slimeTiles[column + 1, row]) {
                slimeTiles[column + 1, row].TakeDamage(1);
                if (slimeTiles[column + 1, row].hitPoints <= 0) {
                    slimeTiles[column + 1, row] = null;
                }
                makeSlime = false;
            }
        }
        if (row > 0) {
            if (slimeTiles[column, row - 1]) {
                slimeTiles[column, row - 1].TakeDamage(1);
                if (slimeTiles[column, row - 1].hitPoints <= 0) {
                    slimeTiles[column, row - 1] = null;
                }
                makeSlime = false;
            }
        }
        if (row < height - 1) {
            if (slimeTiles[column, row + 1]) {
                slimeTiles[column, row + 1].TakeDamage(1);
                if (slimeTiles[column, row + 1].hitPoints <= 0) {
                    slimeTiles[column, row + 1] = null;
                }
                makeSlime = false;
            }
        }
    }

    private IEnumerator DecreaseRowCo2() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (!blankSpaces[i, j] && allDots[i, j] == null && !concreteTiles[i, j] && !slimeTiles[i, j]) {
                    for (int k = j; k < height; k++) {
                        if (allDots[i, k] != null) {
                            allDots[i, k].GetComponent<Dot>().row = j;
                            allDots[i, k] = null;
                            break;
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(refillDelay * 0.5f);
        StartCoroutine(FillBoardCo());
    }

    private IEnumerator DecreaseRowCo() {
        int nullCount = 0;

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] == null) {
                    nullCount++;
                } else if (nullCount > 0) {
                    allDots[i, j].GetComponent<Dot>().row -= nullCount;
                    allDots[i, j] = null;
                }
            }
            nullCount = 0;
        }
        yield return new WaitForSeconds(refillDelay * 0.5f);
        StartCoroutine(FillBoardCo());
    }

    private void RefillBoard() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] == null && !blankSpaces[i, j] && !concreteTiles[i, j] && !slimeTiles[i, j]) {
                    Vector2 tempPosition = new Vector2(i, j + offSet);
                    int dotToUse = Random.Range(0, dots.Length);

                    int maxIterations = 0;
                    while (MatchesAt(i, j, dots[dotToUse]) && maxIterations < 100) {
                        maxIterations++;
                        dotToUse = Random.Range(0, dots.Length);
                    }

                    GameObject piece = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);
                    allDots[i, j] = piece;
                    piece.GetComponent<Dot>().row = j;
                    piece.GetComponent<Dot>().column = i;
                }
            }
        }
    }

    private bool MatchesOnBoard() {
        findMatches.FindAllMatches();
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    if (allDots[i, j].GetComponent<Dot>().isMatched) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator FillBoardCo() {
        
        yield return new WaitForSeconds(refillDelay);
        RefillBoard();
        yield return new WaitForSeconds(refillDelay);
        while (MatchesOnBoard()) {
            streakValue++;
            DestroyMatches();
            yield break;
        }
        currentDot = null;
        CheckToMakeSlime();
        //yield return new WaitForSeconds(.5f);
        if (IsDeadlocked()) ShuffleBoard();

        yield return new WaitForSeconds(refillDelay);

        if (currentState != GameState.pause) currentState = GameState.move;

        makeSlime = true;
        streakValue = 1;
    }

    private void CheckToMakeSlime() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (slimeTiles[i, j] != null && makeSlime) {
                    MakeNewSlime();
                }
            }
        }
    }

    private Vector2 CheckForAdjacent(int column, int row) {
        //TODO Falls hier nicht zurück gesprungen wird, ändere die Reihenfolge der boolschen Ausdrücke!
        if (column < width - 1 && allDots[column + 1, row]) {
            return Vector2.right;
        }
        if (column > 0 && allDots[column - 1, row]) {
            return Vector2.left;
        }
        if (row < height - 1 && allDots[column, row + 1]) {
            return Vector2.up;
        }
        if (row > 0 && allDots[column, row - 1]) {
            return Vector2.down;
        }
        return Vector2.zero;
    }

    private void MakeNewSlime() {
        //TODO Ändere diesen dummen Shit!!! Sobald die Reihe durchgearbeitet ist
        bool slime = false;
        int loops = 0;
        while (!slime && loops < 200) {
            int newX = Random.Range(0, width);
            int newY = Random.Range(0, height);
            if (slimeTiles[newX, newY]) {
                Vector2 adjacent = CheckForAdjacent(newX, newY);
                if (adjacent != Vector2.zero) {
                    Destroy(allDots[newX + (int)adjacent.x, newY + (int)adjacent.y]);
                    Vector2 tempPosition = new Vector2(newX + (int)adjacent.x, newY + (int)adjacent.y);
                    GameObject tile = Instantiate(slimeTilePrefab, tempPosition, Quaternion.identity);
                    slimeTiles[newX + (int)adjacent.x, newY + (int)adjacent.y] = tile.GetComponent<BackgroundTile>();
                    slime = true;
                    makeSlime = false; //Hatte er im Video nicht gemacht, hat aber double Slimes gefixt
                }
            }
            loops++;
        }
    }

    private void SwitchPieces(int column, int row, Vector2 direction) {
        if (allDots[column + (int)direction.x, row + (int)direction.y] != null) {
            GameObject holder = allDots[column + (int)direction.x, row + (int)direction.y] as GameObject;
            allDots[column + (int)direction.x, row + (int)direction.y] = allDots[column, row];

            allDots[column, row] = holder;
        }
    }

    private bool CheckForMatches() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    if (i < width - 2) {
                        if (allDots[i + 1, j] != null && allDots[i + 2, j] != null) {
                            if (allDots[i + 1, j].tag == allDots[i, j].tag && allDots[i, j].tag == allDots[i + 2, j].tag) {
                                return true;
                            }
                        }
                    }
                    if (j < height - 2) {
                        if (allDots[i, j + 1] != null && allDots[i, j + 2] != null) {
                            if (allDots[i, j + 1].tag == allDots[i, j].tag && allDots[i, j].tag == allDots[i, j + 2].tag) {
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    public bool SwitchAndCheck(int column, int row, Vector2 direction) {
        SwitchPieces(column, row, direction);
        if (CheckForMatches()) {
            SwitchPieces(column, row, direction);
            return true;
        }
        SwitchPieces(column, row, direction);
        return false;
    }

    private bool IsDeadlocked() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    if (i < width - 1) {
                        if (SwitchAndCheck(i, j, Vector2.right)) return false;
                    }
                    if (j < height - 1) {
                        if (SwitchAndCheck(i, j, Vector2.up)) return false;
                    }
                }
            }
        }
        return true;
    }

    private void ShuffleBoard() {
        List<GameObject> newBoard = new List<GameObject>();
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    newBoard.Add(allDots[i, j]);
                }
            }
        }

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (!blankSpaces[i, j] && !concreteTiles[i, j] && !slimeTiles[i, j]) {
                    int pieceToUse = Random.Range(0, newBoard.Count);

                    int maxIterations = 0;
                    while (MatchesAt(i, j, newBoard[pieceToUse]) && maxIterations < 100) {
                        pieceToUse = Random.Range(0, newBoard.Count);
                        maxIterations++;
                    }
                    maxIterations = 0;

                    Dot piece = newBoard[pieceToUse].GetComponent<Dot>();
                    piece.column = i;
                    piece.row = j;
                    allDots[i, j] = newBoard[pieceToUse];
                    newBoard.Remove(newBoard[pieceToUse]);
                }
            }
        }
        if (IsDeadlocked()) {
            ShuffleBoard();
        }
    }

}
