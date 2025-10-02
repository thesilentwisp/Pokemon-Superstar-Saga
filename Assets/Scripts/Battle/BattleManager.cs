using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Unity.VisualScripting;
using UnityEngine.AI;

public class BattleManager : MonoBehaviour
{
    public MonsterSO playerMonsterSO;
    public MonsterSO enemyMonsterSO;

    public Text playerNameText;
    public Slider playerHPBar;
    public Text playerHPText;
    public Text playerManaText;
    public Image playerSpriteImage;

    public Text enemyNameText;
    public Slider enemyHPBar;
    public Text enemyHPText;
    public Text enemyManaText;
    public Image enemySpriteImage;

    public Button move1Btn, move2Btn, move3Btn;
    public QTEController qte;
    public Text logText;
    public Button retryButton;

    MonsterRuntime P, E;
    System.Random rng = new System.Random();
    int playerChain = 0;
    bool playerFlowReady = false;
    bool playerFlowActiveThisTurn = false;

    MoveSO playerChosen, enemyChosen;

    void Start()
    {
        retryButton.gameObject.SetActive(false);
        P = new MonsterRuntime(playerMonsterSO);
        E = new MonsterRuntime(enemyMonsterSO);

        ApplyBattleSprite(playerSpriteImage, P.data.battleSprite);
        ApplyBattleSprite(enemySpriteImage, E.data.battleSprite);

        playerNameText.text = P.data.displayName;
        enemyNameText.text = E.data.displayName;

        playerHPBar.maxValue = P.MaxHP;
        playerHPBar.value = P.HP;
        enemyHPBar.maxValue = E.MaxHP;
        enemyHPBar.value = E.HP;
        UpdateHPManaUI();

        move1Btn.onClick.AddListener(() => OnClickMove(0));
        move2Btn.onClick.AddListener(() => OnClickMove(1));
        move3Btn.onClick.AddListener(() => OnClickMove(2));

        retryButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        StartCoroutine(BattleLoop());
    }

    IEnumerator BattleLoop()
    {
        while (P.HP > 0 && E.HP > 0)
        {
            yield return StartCoroutine(StartTurnPhase());
            yield return StartCoroutine(SelectionPhase());
            yield return StartCoroutine(ResolvePhase());
            yield return StartCoroutine(EndTurnPhase());
        }

        if (P.HP <= 0 && E.HP <= 0) Log("Draw!");
        else if (P.HP <= 0) Log("You lose!");
        else Log("You win!");

        retryButton.gameObject.SetActive(true);
        move1Btn.interactable = move2Btn.interactable = move3Btn.interactable = false;
    }

    IEnumerator StartTurnPhase()
    {
        P.Mana = Mathf.Min(10, P.Mana + 1);
        E.Mana = Mathf.Min(10, E.Mana + 1);
        UpdateHPManaUI();
        yield return null;
    }

    IEnumerator SelectionPhase()
    {
        playerFlowActiveThisTurn = false;
        ShowMoves(true);
        playerChosen = null;
        while (playerChosen == null) yield return null;
        enemyChosen = ChooseAIMove();
        Log("Enemy chose a move.");
    }

    IEnumerator ResolvePhase()
    {
        int pSPD = P.SPD - (P.statuses.Has(StatusType.Slow) ? Mathf.CeilToInt(P.SPD * 0.25f) : 0);
        int eSPD = E.SPD - (E.statuses.Has(StatusType.Slow) ? Mathf.CeilToInt(E.SPD * 0.25f) : 0);
        bool playerFirst = pSPD >= eSPD;

        int effCost = Mathf.Max(0, playerChosen.manaCost - (playerFlowReady ? 1 : 0));
        playerFlowActiveThisTurn = playerFlowReady;
        playerFlowReady = false;

        P.Mana = Mathf.Max(0, P.Mana - effCost);

        int enemyBaseIndex = 0;
        int enemyCost = (enemyChosen == E.data.moves[enemyBaseIndex]) ? 0 : enemyChosen.manaCost;
        E.Mana = Mathf.Max(0, E.Mana - enemyCost);

        UpdateHPManaUI();

        if (playerFirst)
        {
            if (!IsStunned(P)) yield return StartCoroutine(ResolvePlayerAttack(playerChosen));
            else Log($"{P.data.displayName} is stunned!");
            if (E.HP > 0)
            {
                if (!IsStunned(E)) yield return StartCoroutine(ResolveEnemyAttack(enemyChosen));
                else Log($"{E.data.displayName} is stunned!");
            }
        }

        else
        {
            if (!IsStunned(E)) yield return StartCoroutine(ResolveEnemyAttack(enemyChosen));
            else Log($"{E.data.displayName} is stunned!");
            if (P.HP > 0)
            {
                if (!IsStunned(P)) yield return StartCoroutine(ResolvePlayerAttack(playerChosen));
                else Log($"{P.data.displayName} is stunned!");
            }
        }
    }

    IEnumerator EndTurnPhase()
    {
        ApplyEndOfTurnStatuses(P);
        ApplyEndOfTurnStatuses(E);
        UpdateHPManaUI();
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator ResolvePlayerAttack(MoveSO move)
    {
        Log($"You use {move.moveName}.");
        TimingResult[] stepResults = null;
        bool done = false;

        qte.StartAttackSequence(move.steps, (res) => { stepResults = res; done = true; });
        while (!done) yield return null;

        int perfectCount = 0;
        float timingMultiplier = ComputeStepTimingMultiplier(move, stepResults, ref perfectCount);

        int damage = ComputePlayerAttackDamage(P, E, move, timingMultiplier, perfectCount > 0);
        E.HP = Mathf.Max(0, E.HP - damage);
        UpdateHPManaUI();

        if (damage > 0 && move.status.type != StatusType.None)
        {
            float perfectRatio = move.steps.Length > 0 ? (float)perfectCount / move.steps.Length : 0f;
            float chance = move.status.baseChance + move.status.perfectBonusPP * perfectRatio + (0.005f * P.LUK);
            chance = Mathf.Clamp01(Mathf.Min(0.95f, chance));
            if (rng.NextDouble() < chance) E.statuses.Add(new StatusInstance(move.status.type, move.status.durationTurns));
        }

        // Rule 1: any Miss breaks the chain
        bool anyMiss = false;
        for (int i = 0; i < stepResults.Length; i++)
        {
            if (stepResults[i] == TimingResult.Miss) { anyMiss = true; break; }
        }

        if (anyMiss)
        {
            playerChain = 0; // whiff breaks streak; no rewards
        }
        else
        {
            // Rule 2: reward only if ALL steps are Perfect (no Good, no Miss)
            bool allPerfect = (stepResults.Length > 0) && (perfectCount == stepResults.Length);

            if (allPerfect)
            {
                P.Mana = Mathf.Min(10, P.Mana + 1);
                playerChain++;

                if (playerChain >= 3)
                {
                    playerFlowReady = true;
                    playerChain = 0;
                    Log("Flow ready: next attack −1 cost, +10% dmg.");
                }
            }
            // Else = only Goods (or zero steps): chain unchanged, no rewards
        }
        UpdateHPManaUI();
        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator ResolveEnemyAttack(MoveSO move)
    {
        Log($"Enemy Attacks With {move.moveName}.");
        int baseDamage = ComputeBaseDamage(E.ATK, move.power, P.DEF);

        TimingResult defTiming = TimingResult.Miss;
        bool done = false;

        qte.StartDefenseQTE(move.defenseTargetMinSeconds, move.defenseTargetMaxSeconds, move.defenseGoodWindow, move.defensePerfectWindow, (res) => { defTiming = res; done = true; });
        while (!done) yield return null;

        int incoming = baseDamage;

        if (defTiming == TimingResult.Good)
        {
            incoming = Mathf.FloorToInt(incoming * move.defenseGoodDamageMult);
            Log("Good guard.");
        }
        else if (defTiming == TimingResult.Perfect)
        {
            if (move.defensePerfectNegates) incoming = 0;
            int counter = ComputeCounterDamage(P.ATK, P.DEF);
            E.HP = Mathf.Max(0, E.HP - counter);
            P.Mana = Mathf.Min(10, P.Mana + 1);
            Log($"Perfect counter: {counter} dmg, +1 mana.");
        }

        if (incoming > 0)
        {
            P.HP = Mathf.Max(0, P.HP - incoming);
            E.Mana = Mathf.Min(10, E.Mana + 1);
            Log($"You took {incoming} dmg.");
        }

        UpdateHPManaUI();
        yield return new WaitForSeconds(0.1f);
    }

    float ComputeStepTimingMultiplier(MoveSO move, TimingResult[] results, ref int perfectCount)
    {
        if (move.steps == null || move.steps.Length == 0) return 1f;
        float sum = 0f;
        perfectCount = 0;
        int n = Mathf.Min(move.steps.Length, results.Length);
        for (int i = 0; i < n; i++)
        {
            var step = move.steps[i];
            var r = results[i];
            float m = step.multipliers.missMult;
            if (r == TimingResult.Good) m = step.multipliers.goodMult;
            else if (r == TimingResult.Perfect)
            {
                m = step.multipliers.perfectMult;
                perfectCount++;
            }
            sum += m;
        }
        return sum / n;
    }

    int ComputeBaseDamage(int attackerATK, float movePower, int defenderDEF)
    {
        float effectiveATK = attackerATK * movePower;
        float mitigation = defenderDEF / (defenderDEF + 50f);
        int baseDamage = Mathf.FloorToInt(effectiveATK * (1f - mitigation));
        return Mathf.Max(0, baseDamage);
    }

    int ComputePlayerAttackDamage(MonsterRuntime attacker, MonsterRuntime defender, MoveSO move, float timingMultiplier, bool anyPerfect)
    {
        int baseDamage = ComputeBaseDamage(attacker.ATK, move.power, defender.DEF);
        float chainMult = 1f + 0.05f * Mathf.Clamp(playerChain, 0, 6);
        bool crit = anyPerfect && (rng.NextDouble() < (0.05 + 0.005 * attacker.LUK));
        float luckMult = crit ? 1.5f : 1f;
        float flowMult = playerFlowActiveThisTurn ? 1.10f : 1f;
        int damage = Mathf.FloorToInt(baseDamage * timingMultiplier * chainMult * luckMult * flowMult);
        return Mathf.Max(0, damage);
    }

    int ComputeCounterDamage(int attackerATK, int defenderDEF)
    {
        float mitigation = defenderDEF / (defenderDEF + 50f);
        return Mathf.FloorToInt(attackerATK * 0.6f * (1f - mitigation));
    }

    bool IsStunned(MonsterRuntime M)
    {
        return M.statuses.Has(StatusType.Stun);
    }

    void ApplyEndOfTurnStatuses(MonsterRuntime M)
    {
        if (M.statuses.Has(StatusType.Burn))
        {
            int dot = Mathf.Max(1, Mathf.FloorToInt(M.MaxHP * 0.05f));
            M.HP = Mathf.Max(0, M.HP - dot);
            Log($"{M.data.displayName} burned for {dot}.");
        }

        if (M.statuses.Has(StatusType.Poison))
        {
            int dot = Mathf.Max(1, Mathf.FloorToInt(M.MaxHP * 0.07f));
            M.HP = Mathf.Max(0, M.HP - dot);
            Log($"{M.data.displayName} took {dot} poison.");
        }
        StatusRuntime.TickEndOfTurn(M.statuses);
    }

    void ShowMoves(bool interactable)
    {
        SetupButton(move1Btn, P.data.moves.Length > 0 ? P.data.moves[0] : null, interactable);
        SetupButton(move2Btn, P.data.moves.Length > 1 ? P.data.moves[1] : null, interactable);
        SetupButton(move3Btn, P.data.moves.Length > 2 ? P.data.moves[2] : null, interactable);
    }

    void SetupButton(Button btn, MoveSO move, bool interactable)
    {
        if (!btn) return;
        var txt = btn.GetComponentInChildren<Text>();
        if (move == null)
        {
            btn.interactable = false;
            if (txt) txt.text = "-";
            return;
        }
        int effCost = Mathf.Max(0, move.manaCost - (playerFlowReady ? 1 : 0));
        if (txt) txt.text = $"{move.moveName} ({effCost})";
        btn.interactable = interactable && (P.Mana >= effCost);
    }

    void OnClickMove(int idx)
    {
        var m = P.data.moves;
        if (idx < 0 || idx >= m.Length) return;
        int effCost = Mathf.Max(0, m[idx].manaCost - (playerFlowReady ? 1 : 0));
        if (P.Mana >= effCost)
        {
            playerChosen = m[idx];
            move1Btn.interactable = move2Btn.interactable = move3Btn.interactable = false;
        }
    }

    MoveSO ChooseAIMove()
    {
        List<MoveSO> options = new List<MoveSO>();
        for (int i = 0; i < E.data.moves.Length; i++)
        {
            var mv = E.data.moves[i];
            if (!mv) continue;
            if (i == 0 || E.Mana >= mv.manaCost) options.Add(mv);
        }
        if (options.Count == 0) options.Add(E.data.moves[0]);
        options.Sort((a, b) => b.manaCost.CompareTo(a.manaCost));
        if (rng.NextDouble() < 0.7) return options[0];
        return options[rng.Next(0, options.Count)];
    }

    void UpdateHPManaUI()
    {
        playerHPBar.value = P.HP;
        enemyHPBar.value = E.HP;
        playerHPText.text = $"{P.HP}/{P.MaxHP}";
        enemyHPText.text = $"{E.HP}/{E.MaxHP}";
        playerManaText.text = $"MP: {P.Mana}/10 (Chain {playerChain})";
        enemyManaText.text = $"MP: {E.Mana}/10";
    }

    void Log(string s)
    {
        if (logText) logText.text = s;
        Debug.Log(s);
    }

    void ApplyBattleSprite(Image target, Sprite sprite)
    {
        if (!target) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
    }
}
