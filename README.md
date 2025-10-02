# <Your Game Name>

Unity 6 prototype of a 1v1 turn-based battle system with QTE attacks/defense, statuses, chain/flow, and simple AI.

## Requirements
- Unity 6 (2025.x)
- macOS/Windows

## Project Structure
- `Assets/`
  - `Scripts/`
    - `Battle/`
      - `BattleManager.cs` — full 1v1 loop
      - `QTEController.cs` — attack/defense QTEs
      - `MonsterRuntime.cs` — runtime HP/Mana/Status
      - `StatusRuntime.cs` — status ticking/helpers
    - `Scriptables/`
      - `MoveSO.cs` — move data
      - `MonsterSO.cs` — monster data
  - `Data/`
    - `Moves/` — Move ScriptableObjects
    - `Monsters/` — Monster ScriptableObjects
- `ProjectSettings/`
- `Packages/`

## How to Run
1. Open the project in Unity 6.
2. Open the battle scene (e.g., `Assets/Scenes/Battle.unity`).
3. Press Play.

## Gameplay Notes
- Multi-step attack QTE (Tap / Hold-Release).
- Defense is blind timing window.
- Chain: +5% damage per link (capped); Flow: −1 MP & +10% damage for next attack.
- Status chance scales with Perfects and LUK.

## Contributing
See [CONTRIBUTING.md](./CONTRIBUTING.md).

## License
TBD
