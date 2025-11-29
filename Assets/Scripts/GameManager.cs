using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/* GameManager
 * 
 * Manages the game of life.
 * 
 */
public class GameManager : MonoBehaviour
{
    // --- Rules ---
    [Header("Rules")]
    [SerializeField] private Rules[] rules;
    private Rules currentRules;

    // --- Tilemap Data ---
    [Header("Tilemap Data")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Tile ALIVE;
    [SerializeField] private Tile DEAD;

    // --- UI References ---
    [Header("UI References Left Side")]
    [SerializeField] private Button pause;
    [SerializeField] private Button start;
    [SerializeField] private Button next;
    [SerializeField] private Button reset;
    [SerializeField] private Button randomize;
    [SerializeField] private Slider chanceSlider;
    [SerializeField] private TMP_Dropdown stamp;
    [SerializeField] private TMP_Dropdown ruleDropdown;

    [Header("UI References Right Side")]
    [SerializeField] private TMP_Text generation;
    [SerializeField] private Slider widthSlider;
    [SerializeField] private Slider heightSlider;
    [SerializeField] private Slider intervalSlider;
    [SerializeField] private TMP_Dropdown presetDropdown;
    [SerializeField] private Toggle wrap;
    [SerializeField] private Toggle wallsAlive;

    [Header("Text UI")]
    [SerializeField] private TMP_Text chanceText;
    [SerializeField] private TMP_Text widthText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text intervalText;

    // --- Camera ---
    [Header("Camera Reference")]
    [SerializeField] private Camera cam;

    // --- Grid Data ---
    private int[,] grid;
    private int[,] nextGrid;
    private int width = 32;
    private int height = 32;
    private Vector2Int center = Vector2Int.zero;

    // --- Interval Timer ---
    private float timer = 0f;
    private float timerMax = 0.25f;
    
    // --- Operation Variables ---
    private bool running = false;
    private int gen = 0;

    // --- Presets ---
    [Header("Pattern Presets")]
    [SerializeField] private Pattern[] patterns;
    private Pattern preset;
    private Pattern stampBrush;

    // --- Testing Variables ---
    [Header("Testing")]
    [SerializeField] private bool DEBUG_MODE = false;

    /* Start
     * 
     * Called once before the first frame of update
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void Start()
    {
        InitGridData();
        width = (int)widthSlider.value;
        height = (int)heightSlider.value;
        cam.orthographicSize = Mathf.Max(width, height) / 2;
        stampBrush = patterns[0];
        currentRules = rules[0];

        widthText.text = "Width: " + widthSlider.value.ToString();
        heightText.text = "Height: " + heightSlider.value.ToString();
        intervalText.text = "Interval: " + intervalSlider.value.ToString() + " s";
        chanceText.text = "Chance: " + (chanceSlider.value * 100).ToString() + " %";
    }

    /* Update
     * 
     * Called once per frame
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void Update()
    {
        if (!DEBUG_MODE)
        {
            if (running)
            {
                UpdateIntervalTimer();
            }
            else
            {
                ListenForInput();
            }
        }
    }

    /* UpdateIntervalTimer
     * 
     * A timer that upon timeout updates the grids state
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void UpdateIntervalTimer()
    {
        // Update timer
        timer += Time.deltaTime;
        
        // Check for timeout
        if (timer > timerMax)
        {
            // Reduce by timerMax
            timer -= timerMax;

            // Check that timer is not below 0 if so set to 0
            if (timer < 0f)
            {
                timer = 0f;
            }

            // Update grid
            UpdateState();
        }
    }

    private void ListenForInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //Debug.Log("x: " + position.x + " y: " + position.y);
            if (position.x >= -width / 2 && position.x <= width / 2 && position.y >= -height / 2 && position.y <= height / 2)
            {
                Stamp((int)position.x, (int)position.y, stampBrush);
                SetPattern();
            }
        }
    }

    private void InitGridData()
    {
        grid = new int[width, height];
        nextGrid = new int[width, height];

        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = 0;
                nextGrid[i, j] = 0;
            }
        }
        center = new Vector2Int(width / 2, height / 2);
    }

    private void UpdateState()
    {
        GetNextGrid();
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = nextGrid[i, j];
            }
        }
        SetPattern();
        gen++;
        UpdateGenText();
    }

    private void SetPattern()
    {
        ClearTiles();

        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                if (grid[i, j] == 1)
                {
                    tilemap.SetTile(new Vector3Int(i - center.x, j - center.y, 0), ALIVE);
                }
                else
                {
                    tilemap.SetTile(new Vector3Int(i - center.x, j - center.y, 0), DEAD);
                }
            }
        }
    }

    private void ClearTiles()
    {
        tilemap.ClearAllTiles();
    }

    private void GetNextGrid()
    {
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                nextGrid[i, j] = UpdateCell(i, j);
            }
        }
    }

    private int UpdateCell(int x, int y)
    {
        int result = 0;
        int aliveNeighbours = AliveNeighbours(x, y);
        if (grid[x, y] == 1)
        {
            result = currentRules.aliveRules[aliveNeighbours];
        }
        else
        {
            result = result = currentRules.deadRules[aliveNeighbours];
        }
        return result;
    }

    private int AliveNeighbours(int x, int y)
    {
        int result = 0;
        if (!wrap.isOn)
        {
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i == x && j == y)
                    {
                        continue;
                    }
                    if (i < 0 || j < 0 || i >= grid.GetLength(0) || j >= grid.GetLength(1))
                    {
                        if (wallsAlive.isOn)
                        {
                            result++;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (grid[i, j] == 1)
                    {
                        result++;
                    }

                }
            }
        }
        else
        {
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    int xCheck = i;
                    int yCheck = j;
                    if (i == x && j == y)
                    {
                        continue;
                    }
                    if (xCheck < 0)
                    {
                        xCheck = grid.GetLength(0) - 1;
                    }
                    if (yCheck < 0)
                    {
                        yCheck = grid.GetLength(1) - 1;
                    }
                    if (xCheck >= grid.GetLength(0))
                    {
                        xCheck = 0;
                    }
                    if (yCheck >= grid.GetLength(1))
                    {
                        yCheck = 0;
                    }
                    if (grid[xCheck, yCheck] == 1)
                    {
                        result++;
                    }
                }
            }
        }
        return result;
    }

    // --- UI Functions ---

    public void Pause()
    {
        running = false;
    }

    public void Run()
    {
        running = true;
    }

    public void Next()
    {
        Pause();
        UpdateState();
    }

    public void Randomize()
    {
        Pause();
        ClearTiles();
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                if (Random.Range(0f, 1f) < chanceSlider.value)
                {
                    grid[i, j] = 1;
                }
                else
                {
                    grid[i, j] = 0;
                }
            }
        }
        SetPattern();
    }

    public void Reset()
    {
        ClearTiles();
        Pause();
        gen = 0;
        UpdateGenText();
        InitGridData();
    }

    private void UpdateGenText()
    {
        generation.text = "Generation: " + gen.ToString();
    }

    public void ChangedGridSize()
    {
        Reset();
        width = (int)widthSlider.value;
        height = (int)heightSlider.value;
        cam.orthographicSize = Mathf.Max(width, height) / 2;
        InitGridData();
        widthText.text = "Width: " + widthSlider.value.ToString();
        heightText.text = "Height: " + heightSlider.value.ToString();
    }

    public void ChangedInterval()
    {
        timerMax = intervalSlider.value;
        intervalText.text = "Interval: " + intervalSlider.value.ToString() + " s";
    }

    public void ChangedRandomSlider()
    {
        chanceText.text = "Chance: " + (chanceSlider.value * 100).ToString() + " %";
    }

    public void ChangedStampBrush()
    {
        stampBrush = patterns[stamp.value];
        Debug.Log(stampBrush);
    }

    public void ChangedPreset()
    {
        Reset();
        width = (int)widthSlider.value;
        height = (int)heightSlider.value;
        InitGridData();
        if (presetDropdown.value != 0)
        {
            preset = patterns[presetDropdown.value];
            Debug.Log(preset);
            DrawPattern(center.x, center.y, preset);
        }
        else
        {
            preset = null;
        }
        SetPattern();
    }

    public void ChangedRules()
    {
        currentRules = rules[ruleDropdown.value];
    }

    private void Stamp(int x, int y, Pattern pattern)
    {
        Vector2Int patternCenter = pattern.GetCenter();

        for (int i = 0; i < pattern.cells.Length; i++)
        {
            int xCoor = x + width / 2 + pattern.cells[i].x - patternCenter.x;
            int yCoor = y + height / 2 + pattern.cells[i].y - patternCenter.y;

            if (xCoor >= grid.GetLength(0) || xCoor < 0 || yCoor >= grid.GetLength(1) || yCoor < 0)
            {
                continue;
            }

            grid[xCoor, yCoor] = 1;
        }
    }

    private void DrawPattern(int x, int y, Pattern pattern)
    {
        Vector2Int patternCenter = pattern.GetCenter();

        for (int i = 0; i < pattern.cells.Length; i++)
        {
            int xCoor = x + pattern.cells[i].x - patternCenter.x;
            int yCoor = y + pattern.cells[i].y - patternCenter.y;

            grid[xCoor, yCoor] = 1;
        }
    }
}
