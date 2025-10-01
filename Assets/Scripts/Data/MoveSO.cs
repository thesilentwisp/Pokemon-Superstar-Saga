using UnityEngine;

/// <summary>
/// Move definition: cost, power, attack QTE timings, status %, and defense target timing.
/// </summary>

[CreateAssetMenu(menuName = "Battle/Move")]
public class MoveSO : ScriptableObject
{
    public string moveID = "ember";
    public string moveName = "Ember";

    public int manaCost = 1;
    public float power = 6f;

    public MoveStep[] steps;

    public StatusSpec status;

    public float defenseTargetMinSeconds = 0.90f;
    public float defenseTargetMaxSeconds = 1.00f;
    public float defenseGoodWindow = 0.12f;
    public float defensePerfectWindow = 0.06f;
    [Range(0f, 1f)] public float defenseGoodDamageMult = 0.5f;
    public bool defensePerfectNegates = true;
}
