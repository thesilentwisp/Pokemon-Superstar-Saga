using UnityEngine;

/// <summary>
/// Monster base stats, staring mana, and move list.
/// </summary>

[CreateAssetMenu(menuName = "Battle/Monster")]
public class MonsterSO : ScriptableObject
{
    public string monsterId = "embercub";
    public string displayName = "Embercub";

    public int ATK = 9;
    public int DEF = 6;
    public int SPD = 7;
    public int LUK = 3;

    public int maxHP = 100;
    public int startMana = 1;

    public MoveSO[] moves = new MoveSO[3];

    [Header("Visuals")]
    public Sprite battleSprite;
}
