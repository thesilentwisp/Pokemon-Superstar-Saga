# Architecture Overview

## Core Loops
- **BattleManager**: Orchestrates Start/Selection/Resolve/End phases as coroutines.
- **QTEController**: Runs step-based attack QTE and blind defense QTE.

## Data
- **MoveSO (ScriptableObject)**: name, cost, power, QTE step windows, status spec, defense windows.
- **MonsterSO (ScriptableObject)**: base stats, maxHP, startMana, moves[].

## Runtime State
- **MonsterRuntime**: HP, Mana, Status list; proxies to base stats from MonsterSO.
- **StatusInstance** + **StatusRuntime**: ticking per turn; helpers like `Has(StatusType)`.

## Damage & Chance
- **Base Damage**: `ATK * power * 50 / (DEF + 50)` (diminishing returns), floored.
- **Timing Multiplier**: average of per-step multipliers based on Miss/Good/Perfect.
- **Chain/Flow**: +5% per chain (cap 6), Flow +10% dmg and −1 MP (consumed once).
- **Crit**: requires timing flag (e.g., all-Perfect if chosen); chance = 5% + 0.5% * LUK; 1.5×.
- **Statuses**: chance = base + perfectBonusPP * (PerfectRatio) + 0.5% * LUK; capped ≤ 95%.

## Turn Order
- SPD reduced by Slow (ceil 25%); higher acts first. KO checks prevent post-death actions.

## AI
- Builds options list (base move always allowed, others if affordable).
- Sorts by manaCost desc; 70% pick strongest; else random among options.

