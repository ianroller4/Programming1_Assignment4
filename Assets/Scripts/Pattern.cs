using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Pattern
 * 
 * Holds a preset pattern of cells to start a game with or stamp an area.
 * 
 */
[CreateAssetMenu(menuName = "Game of Life/Pattern")]
public class Pattern : ScriptableObject
{
    // --- Pattern Cells ---
    public Vector2Int[] cells; // Cells that make up the pattern

    /* GetCenter
     * 
     * Gets the center of the cells that make up the pattern.
     * Used for centering the pattern at the desired position.
     * 
     * Parameters: None
     * 
     * Return: Vectro2Int, the center of the cells
     * 
     */
    public Vector2Int GetCenter()
    {
        // Make sure cells array is not empty or null
        if (cells != null && cells.Length > 0)
        {
            Vector2Int min = Vector2Int.zero; // Minimum cell of pattern
            Vector2Int max = Vector2Int.zero; // Maximum cell of pattern

            // Loop through cells to determine range of cells
            for (int i = 0; i < cells.Length; i++)
            {
                min.x = Mathf.Min(cells[i].x, min.x);
                min.y = Mathf.Min(cells[i].y, min.y);

                max.x = Mathf.Max(cells[i].x, max.x);
                max.y = Mathf.Max(cells[i].y, max.y);
            }

            // Return average of min and max to get center of pattern
            return (min + max) / 2;
        }
        else // If cells was empty or null log a warning and return zero vector
        {
            Debug.LogWarning("No cells found in pattern!");
            return Vector2Int.zero;
        }
    }
}
