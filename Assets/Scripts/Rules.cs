using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Rules
 * 
 * Rule set for the game of life.
 * 
 */
[CreateAssetMenu(menuName = "Game of Life/Rules")]
public class Rules : ScriptableObject
{
    // What to do based on number of alive neighbours (alive neighbours will be index in array) if currently alive
    [SerializeField] public int[] aliveRules = new int[9];

    // What to do based on number of alive neighbours (alive neighbours will be index in array) if currently dead
    [SerializeField] public int[] deadRules = new int[9]; 
}
