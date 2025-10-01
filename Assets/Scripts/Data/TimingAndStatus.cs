using UnityEngine;

/// <summary>
/// Shared enums and structs for timing, status, and multipliers.
/// </summary>


public enum TimingResult { Miss, Good, Perfect }
public enum StatusType { None, Burn, Poison, Slow, Stun }
public enum StepKind {Tap, HoldRelease}

[System.Serializable]
public struct StatusSpec
{
    public StatusType type;
    [Range(0f, 1f)] public float baseChance;
    [Range(0f, 1f)] public float perfectBonusPP;
    public int durationTurns;
}

[System.Serializable]
public struct TimingMultipliers
{
    public float missMult;
    public float goodMult;
    public float perfectMult;
}

[System.Serializable]
public struct MoveStep
{
    public StepKind kind;
    public float targetSeconds;
    public float goodWindow;
    public float perfectWindow;
    public TimingMultipliers multipliers;
}

