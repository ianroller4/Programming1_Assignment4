using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* RuleSequence
 * 
 * Contains a list of type RuleStep to run in the game of life for tilemap generation.
 * 
 */
[CreateAssetMenu(menuName = "Game of Life/Rule Sequence")]
public class RuleSequence : ScriptableObject
{
    [SerializeField] public float aliveChance; // Each sequence starts with a random field this is its chance
    [SerializeField] public List<RuleStep> steps; // List of RuleStep
}
