using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/* GameManager
 * 
 * Manages the game of life, including operations and UI.
 * 
 */
public class GameManager : MonoBehaviour
{
    // --- Rules ---
    [Header("Rules")]
    [SerializeField] private Rules[] rules; // Array of rules players can choose to run
    private Rules currentRules; // The current rule

    // --- Tilemap Data ---
    [Header("Tilemap Data")]
    [SerializeField] private Tilemap ground; // Tile map for alive tiles
    [SerializeField] private Tilemap water; // Tile map for dead tiles
    [SerializeField] private RuleTile ALIVE; // Alive rule tiles
    [SerializeField] private RuleTile DEAD; // Dead rule tiles

    // --- UI References ---
    [Header("UI References Left Side")]
    [SerializeField] private Button pause; // pause button reference
    [SerializeField] private Button start; // Start button reference
    [SerializeField] private Button next; // Next step button reference
    [SerializeField] private Button reset; // Reset button reference
    [SerializeField] private Button randomize; // Randomize button reference
    [SerializeField] private Slider chanceSlider; // Chance slider reference
    [SerializeField] private TMP_Dropdown stamp; // Stamp option dropdown reference
    [SerializeField] private TMP_Dropdown ruleDropdown; // Rule option dropdown reference
    [SerializeField] private Toggle updateAlive; // Toggle to update alive cells reference
    [SerializeField] private Toggle updateDead; // Toggle to update dead cells reference

    [Header("UI References Right Side")]
    [SerializeField] private TMP_Text generation; // Text for generation number reference
    [SerializeField] private Slider widthSlider; // Grid width slider reference
    [SerializeField] private Slider heightSlider; // Grid height slider reference
    [SerializeField] private Slider intervalSlider; // Update interval slider reference
    [SerializeField] private TMP_Dropdown presetDropdown; // Preset options dropdown reference
    [SerializeField] private Toggle wrap; // Toggle to wrap border cells reference
    [SerializeField] private Toggle wallsAlive; // Toggle if walls should be considered alive reference
    [SerializeField] private Toggle runSequence; // Toggle to run rule sequence reference

    [Header("Text UI")]
    [SerializeField] private TMP_Text chanceText; // Text to display current chance value reference
    [SerializeField] private TMP_Text widthText; // Text to display current width value reference
    [SerializeField] private TMP_Text heightText; // Text to display current height value reference
    [SerializeField] private TMP_Text intervalText; // Text to display current update interval value reference

    // --- Camera ---
    [Header("Camera Reference")]
    [SerializeField] private Camera cam; // Camera reference to update size

    // --- Grid Data ---
    private int[,] grid; // Current grid, state is displayed
    private int[,] nextGrid; // Next grid state, once determined based on rule and grid values transferred to grid
    private int width = 32; // Grid width
    private int height = 32; // Grid height
    private Vector2Int center = Vector2Int.zero; // Center of grid for centering values on tilemap

    // --- Interval Timer ---
    private float timer = 0f; // Timer value for updating state when runnning
    private float timerMax = 0.25f; // Timer max value based on interval slider value
    
    // --- Operation Variables ---
    private bool running = false; // Game is running or not
    private int gen = 0; // Current generation of update

    // --- Presets ---
    [Header("Pattern Presets")]
    [SerializeField] private Pattern[] patterns; // Preset patterns to choose from
    private Pattern preset; // The current preset
    private Pattern stampBrush; // The preset to use as a brush

    // --- Rule Sequence ---
    [Header("Rule Sequence")]
    [SerializeField] private RuleSequence ruleSequence; // The rule sequence to run
    private int ruleSquenceIndex = 0; // Index in rule sequence
    private int currentRuleGenerations = 0; // Generations to run rule for

    // --- Testing Variables ---
    [Header("Testing")]
    [SerializeField] private bool DEBUG_MODE = false; // Switch to turn debug on and off

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
        // Get width and height values from sliders
        width = (int)widthSlider.value;
        height = (int)heightSlider.value;

        // Create grid 
        InitGridData();

        // Set camera size accordingly to width and height of grid
        cam.orthographicSize = Mathf.Max(width, height) / 2;

        // Set current rule and stamp brush
        stampBrush = patterns[0];
        currentRules = rules[0];

        // Set text of various UI elements to their corresponding slider values
        widthText.text = "Width: " + widthSlider.value.ToString();
        heightText.text = "Height: " + heightSlider.value.ToString();
        intervalText.text = "Interval: " + intervalSlider.value.ToString() + " s";
        chanceText.text = "Chance: " + (chanceSlider.value * 100).ToString() + " %";

        // Change colour of pause and start buttons to reflect running state
        pause.GetComponent<Image>().color = Color.red;
        start.GetComponent<Image>().color = Color.white;
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
            if (!runSequence.isOn)
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
            else
            {
                if (running)
                {
                    UpdateIntervalTimer();
                }
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
            if (!runSequence.isOn)
            {
                UpdateState();
            }
            else
            {
                RunSequence();
            }

        }
    }

    /* ListenForInput
     * 
     * Listens for input from the left mouse button. Draws stamp pattern if clicked
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void ListenForInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Convert position position to world position

            // Make sure mouse position is within tilemap area
            if (position.x >= -width / 2 && position.x <= width / 2 && position.y >= -height / 2 && position.y <= height / 2)
            {
                // Stamp onto grid
                Stamp((int)position.x, (int)position.y, stampBrush);

                // Set pattern
                SetPattern();
            }
        }
    }

    /* InitGridData
     * 
     * Initializes grids to width and height size and values to 0 then finds center of grid.
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void InitGridData()
    {
        // Initialize grids
        grid = new int[width, height];
        nextGrid = new int[width, height];

        // Loop through grids
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                // Set all values to 0
                grid[i, j] = 0;
                nextGrid[i, j] = 0;
            }
        }
        // Determine center
        center = new Vector2Int(width / 2, height / 2);
    }

    /* UpdateState
     * 
     * Updates the state of the grid.
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void UpdateState()
    {
        // Update next grid
        GetNextGrid();

        // Update grid to next grid values
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = nextGrid[i, j];
            }
        }
        // Set pattern to grid
        SetPattern();

        // Update generation
        gen++;
        UpdateGenText();
    }

    /* SetPattern
     * 
     * Draws rule tiles to tilemaps based on alive (1) or dead (0) status.
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void SetPattern()
    {
        // Clear tilemaps
        ClearTiles();

        // Loop through grid
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                // If alive
                if (grid[i, j] == 1)
                {
                    // Set ground tilemap to alive rule tile at position adjusted to center
                    ground.SetTile(new Vector3Int(i - center.x, j - center.y, 0), ALIVE);
                }
                else // If dead
                {
                    // Set water tilemap to dead rule tile at position adjusted to center
                    water.SetTile(new Vector3Int(i - center.x, j - center.y, 0), DEAD);
                }
            }
        }
    }

    /* ClearTiles
     * 
     * Clear all tilemaps tiles to have fresh slate
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void ClearTiles()
    {
        ground.ClearAllTiles();
        water.ClearAllTiles();
    }

    /* GetNextGrid
     * 
     * Loops through nextGrid and updates cells
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void GetNextGrid()
    {
        // Loop through next grid
        for (int i = 0; i < nextGrid.GetLength(0); i++)
        {
            for (int j = 0; j < nextGrid.GetLength(1); j++)
            {
                nextGrid[i, j] = UpdateCell(i, j); // Update the cell at positon (i, j)
            }
        }
    }

    /* UpdateCell
     * 
     * Updates a cell based on its alive neighbours and the current rule set
     * 
     * Parameters: int x, the x position of the cell to look at
     *             int y, the y position of the cell to look at
     *             
     * Return: int result, 0 if cell is now dead, 1 if cell is now alive
     * 
     */
    private int UpdateCell(int x, int y)
    {
        int result = 0;
        int aliveNeighbours = AliveNeighbours(x, y); // Get count of alive neighbours

        // If alive
        if (grid[x, y] == 1)
        {
            // Apply alive rules to alive tiles
            if (updateAlive.isOn)
            {
                // Get value based on current rules for alive
                result = currentRules.aliveRules[aliveNeighbours];
            }
        }
        else // If dead
        {
            // Apply dead rules to dead tiles
            if (updateDead.isOn)
            {
                // Get value based on current rules for dead
                result = result = currentRules.deadRules[aliveNeighbours];
            }
        }
        return result;
    }

    /* AliveNeighbours
     * 
     * Gets a count of the number of alive cells surrounding the cell at (x, y)
     * 
     * Parameters: int x, the x position of the cell to look at
     *             int y, the y position of the cell to look at
     *             
     * Return: int result, the number of alive neighbours
     * 
     */
    private int AliveNeighbours(int x, int y)
    {
        int result = 0;

        // Check if grid should be wrapped
        if (!wrap.isOn)
        {
            // Loop through surrounding cells
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    // Skip over center since we want this cells neighbours
                    if (i == x && j == y)
                    {
                        continue;
                    }
                    
                    // If a cell is a wall cell
                    if (i < 0 || j < 0 || i >= grid.GetLength(0) || j >= grid.GetLength(1))
                    {
                        // If we consider walls alive
                        if (wallsAlive.isOn)
                        {
                            result++;
                        }
                        else // If we consider walls dead
                        {
                            continue;
                        }
                    }
                    else if (grid[i, j] == 1) // Check if cell is alive
                    {
                        result++;
                    }

                }
            }
        }
        else // Wrapping is on
        {
            // Loop through surrounding cells
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    // Cell coordinates to check
                    int xCheck = i;
                    int yCheck = j;

                    // Skip itself
                    if (i == x && j == y)
                    {
                        continue;
                    }
                    // Wrap to other side if wall
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
                    // Check cell at xCheck, yCheck
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

    /* Pause
     * 
     * Pauses operation of simulation
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void Pause()
    {
        running = false; // Pause running

        // Update button colours to display current state
        pause.GetComponent<Image>().color = Color.red;
        start.GetComponent<Image>().color = Color.white;
    }

    /* Run
     * 
     * Starts operation of simulation
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void Run()
    {
        running = true; // Start running

        // Update button colours to display current state
        pause.GetComponent<Image>().color = Color.white;
        start.GetComponent<Image>().color = Color.green;
    }

    /* Next
     * 
     * Pauses operation and processes the next state
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void Next()
    {
        Pause();
        UpdateState();
    }

    /* Randomize
     * 
     * Randomizes the grid
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void Randomize()
    {
        Pause(); // Pause sim
        ClearTiles(); // Clear tilemaps

        // Loop through grid
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                // Set cell to alive if random is less than chanceSlider valule
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

        // Set tile maps
        SetPattern();
    }

    /* ResetSim
     * 
     * Resets the simulation to a blank slate
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void ResetSim()
    {
        ClearTiles(); // Clear tiles
        Pause(); // Pause
        gen = 0; // Set gen back to 0
        UpdateGenText(); // Update gen text
        InitGridData(); // Re init grids
    }

    /* UpdateGenText
     * 
     * Updates the generation text
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void UpdateGenText()
    {
        generation.text = "Generation: " + gen.ToString();
    }

    /* ChangedGridSize
     * 
     * Updates grids size, camera, and text values upon slider change
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void ChangedGridSize()
    {
        ResetSim(); // Reset sim

        // Get new values
        width = (int)widthSlider.value;
        height = (int)heightSlider.value;

        // Update camera size
        cam.orthographicSize = Mathf.Max(width, height) / 2;

        // Re init grid
        InitGridData();

        // Update text UI for width and height
        widthText.text = "Width: " + widthSlider.value.ToString();
        heightText.text = "Height: " + heightSlider.value.ToString();
    }

    /* ChangedInterval
     * 
     * Updates interval timer max and interval UI text
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void ChangedInterval()
    {
        timerMax = intervalSlider.value; // Update value to new slider value

        // Update text UI
        intervalText.text = "Interval: " + intervalSlider.value.ToString() + " s";
    }

    /* ChangedRandomSlider
     * 
     * Updates the Random slider UI text
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void ChangedRandomSlider()
    {
        chanceText.text = "Chance: " + (chanceSlider.value * 100).ToString() + " %";
    }

    /* ChangedStampBrush
     * 
     * Updates the stamp brush to the chosen pattern from the dropdown value
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void ChangedStampBrush()
    {
        stampBrush = patterns[stamp.value];
        if (DEBUG_MODE)
        {
            Debug.Log(stampBrush);
        }
    }

    /* ChangedPreset
     * 
     * Updates the sim to the new preset, updates width and height to ensure centering of preset
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void ChangedPreset()
    {
        ResetSim(); // Reset
        ChangedGridSize(); // Update grid

        // Updates preset, value 0 means no preset
        if (presetDropdown.value != 0)
        {
            preset = patterns[presetDropdown.value];

            if (DEBUG_MODE)
            {
                Debug.Log(preset);
            }

            // Draw pattern
            DrawPattern(center.x, center.y, preset);
        }
        else
        {
            preset = null;
        }

        // Set pattern
        SetPattern();
    }

    /* ChangedRules
     * 
     * Update rules to current value of dropdown menu
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void ChangedRules()
    {
        currentRules = rules[ruleDropdown.value];
    }

    /* Stamp
     * 
     * Stamps the pattern onto the grid and tilemap
     * 
     * Parameters: int x, x coordinate of where to place the pattern
     *             int y, y coordinate of where to place the pattern
     *             Pattern pattern, pattern to place
     *             
     * Return: None
     * 
     */
    private void Stamp(int x, int y, Pattern pattern)
    {
        Vector2Int patternCenter = pattern.GetCenter(); // Get center of pattern

        // Loop through cells in pattern
        for (int i = 0; i < pattern.cells.Length; i++)
        {
            // Calculate coordinate in grid for pattern
            // Where we want to place it + half the grid size + cell x y - pattern center
            int xCoor = x + width / 2 + pattern.cells[i].x - patternCenter.x;
            int yCoor = y + height / 2 + pattern.cells[i].y - patternCenter.y;

            // Make sure position is not out of bounds
            if (xCoor >= grid.GetLength(0) || xCoor < 0 || yCoor >= grid.GetLength(1) || yCoor < 0)
            {
                continue;
            }

            // Set cell to alive
            grid[xCoor, yCoor] = 1;
        }
    }

    /* DrawPattern
     * 
     * Draws a pattern centered on the grid
     * 
     * Parameters: int x, x coordinate of where to place the pattern
     *             int y, y coordinate of where to place the pattern
     *             Pattern pattern, pattern to place
     *             
     * Return: None
     * 
     */
    private void DrawPattern(int x, int y, Pattern pattern)
    {
        // Get pattern center
        Vector2Int patternCenter = pattern.GetCenter();

        // Loop through pattern cells and adjust position to center on grid
        for (int i = 0; i < pattern.cells.Length; i++)
        {
            int xCoor = x + pattern.cells[i].x - patternCenter.x;
            int yCoor = y + pattern.cells[i].y - patternCenter.y;

            grid[xCoor, yCoor] = 1;
        }
    }

    /* SequenceUpdate
     * 
     * Sets up to run a sequence of rules when run sequence toggle is enabled. Called when sequence is toggled
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    public void SequenceUpdate()
    {
        // If run Sequence toggle is on
        if (runSequence.isOn)
        {
            // Reset sim
            ResetSim();

            // Set chance slider value to chance from rule sequence
            chanceSlider.value = ruleSequence.aliveChance;

            // Randomize
            Randomize();

            // Set current rules and number of generations to run
            currentRules = ruleSequence.steps[ruleSquenceIndex].rule;
            currentRuleGenerations = ruleSequence.steps[ruleSquenceIndex].generations;
        }
    }

    /* RunSequence
     * 
     * Updates the state of the board and updates the sequence variables
     * 
     * Parameters: None
     * 
     * Return: None
     * 
     */
    private void RunSequence()
    {
        UpdateState(); // Update state
        currentRuleGenerations--; // Reduce generation count

        // If generations to run rule for is less than or equal to zero move on
        if (currentRuleGenerations <= 0)
        {
            // Update sequence index
            ruleSquenceIndex++;

            // Check if index in bounds still 
            if (ruleSquenceIndex < ruleSequence.steps.Count)
            {
                // Updates sequence variables
                currentRules = ruleSequence.steps[ruleSquenceIndex].rule;
                currentRuleGenerations = ruleSequence.steps[ruleSquenceIndex].generations;
            }
            else // Sequence out of bound sequence is now over
            {
                // Pause
                Pause();

                // Disable run sequence
                runSequence.isOn = false;

                // Revert rule back to whatever dropdown menu selected is
                ChangedRules();
            }
        }
    }
}
