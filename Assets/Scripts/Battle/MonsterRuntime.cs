using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Runtime wrapper for a monster: current HP, mana, and statuses.
/// </summary>

public class MonsterRuntime
{
    public MonsterSO data;
    public int HP;
    public int Mana;
    public List<StatusInstance> statuses = new List<StatusInstance>();

    public int ATK => data.ATK;
    public int DEF => data.DEF;
    public int SPD => data.SPD;
    public int LUK => data.LUK;
    public int MaxHP => data.maxHP;

    public MonsterRuntime(MonsterSO so)
    {
        data = so;
        HP = data.maxHP;
        Mana = Mathf.Clamp(data.startMana, 0, 10);
    }
}
