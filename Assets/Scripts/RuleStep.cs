using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* RuleStep
 * 
 * A step in a sequence of steps. Contains which rule to run and for how many generations.
 * 
 */
[System.Serializable]
public class RuleStep
{
    [SerializeField] public Rules rule; // Rule to run
    [SerializeField] public int generations; // Number of generations to run rule for
}
