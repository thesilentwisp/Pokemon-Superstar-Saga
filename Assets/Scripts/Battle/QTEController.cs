/// Attack QTE runner for step sequences (tap and hold-release) and blind defense with variable timing.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QTEController : MonoBehaviour
{
    public Image attackCircle;

    System.Action<TimingResult[]> onAttackComplete;
    System.Action<TimingResult> onDefenseComplete;
    bool running;

    public void StartAttackSequence(MoveStep[] steps, System.Action<TimingResult[]> callback)
    {
        if (running) StopAllCoroutines();
        onAttackComplete = callback;
        if (attackCircle) attackCircle.gameObject.SetActive(true);
        gameObject.SetActive(true);
        running = true;
        StartCoroutine(AttackSequenceRoutine(steps));
    }

    public void StartDefenseQTE(float targetMin, float targetMax, float goodWindow, float perfectWindow, System.Action<TimingResult> callback)
    {
        if (running) StopAllCoroutines();
        onDefenseComplete = callback;
        if (attackCircle) attackCircle.gameObject.SetActive(false);
        gameObject.SetActive(true);
        running = true;
        StartCoroutine(DefenseRoutine(targetMin, targetMax, goodWindow, perfectWindow));
    }

    IEnumerator AttackSequenceRoutine(MoveStep[] steps)
    {
        var results = new TimingResult[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            TimingResult stepRes = TimingResult.Miss;
            var s = steps[i];
            if (s.kind == StepKind.Tap)
                yield return StartCoroutine(TapStepRoutine(s.targetSeconds, s.goodWindow, s.perfectWindow, r => stepRes = r));
            else
                yield return StartCoroutine(HoldReleaseRoutine(s.targetSeconds, s.goodWindow, s.perfectWindow, r => stepRes = r));
            results[i] = stepRes;
        }
        running = false;
        if (attackCircle) attackCircle.gameObject.SetActive(false);
        onAttackComplete?.Invoke(results);
    }

    IEnumerator TapStepRoutine(float target, float goodWin, float perfectWin, System.Action<TimingResult> cb)
    {
        float t = 0f;
        if (attackCircle) { attackCircle.gameObject.SetActive(true); attackCircle.rectTransform.localScale = Vector3.one; }
        while (t < target)
        {
            t += Time.deltaTime;
            float s = Mathf.Clamp01(1f - t / target);
            if (attackCircle) attackCircle.rectTransform.localScale = new Vector3(s, s, 1f);
            if (TapDownThisFrame())
            {
                float dt = Mathf.Abs(target - t);
                cb(dt <= perfectWin ? TimingResult.Perfect : dt <= goodWin ? TimingResult.Good : TimingResult.Miss);
                yield break;
            }
            yield return null;
        }
        cb(TimingResult.Miss);
    }

    IEnumerator HoldReleaseRoutine(float target, float goodWin, float perfectWin, System.Action<TimingResult> cb)
    {
        float held = 0f;
        bool holding = false;
        if (attackCircle) { attackCircle.gameObject.SetActive(true); attackCircle.rectTransform.localScale = Vector3.one; }
        float timeout = target + 0.6f;
        float t = 0f;
        while (t < timeout)
        {
            t += Time.deltaTime;
            if (!holding && TapDownThisFrame()) holding = true;
            if (holding) held += Time.deltaTime;
            float s = Mathf.Clamp01(1f - (holding ? held : 0f) / target);
            if (attackCircle) attackCircle.rectTransform.localScale = new Vector3(s, s, 1f);
            if (holding && TapUpThisFrame())
            {
                float dt = Mathf.Abs(held - target);
                cb(dt <= perfectWin ? TimingResult.Perfect : dt <= goodWin ? TimingResult.Good : TimingResult.Miss);
                yield break;
            }
            yield return null;
        }
        cb(TimingResult.Miss);
    }

    IEnumerator DefenseRoutine(float targetMin, float targetMax, float goodWin, float perfectWin)
    {
        float target = Random.Range(targetMin, targetMax);
        float t = 0f;
        while (t < target + 0.6f)
        {
            t += Time.deltaTime;
            if (TapDownThisFrame())
            {
                float dt = Mathf.Abs(t - target);
                onDefenseComplete?.Invoke(dt <= perfectWin ? TimingResult.Perfect : dt <= goodWin ? TimingResult.Good : TimingResult.Miss);
                running = false;
                yield break;
            }
            yield return null;
        }
        running = false;
        onDefenseComplete?.Invoke(TimingResult.Miss);
    }

    bool TapDownThisFrame()
    {
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    bool TapUpThisFrame()
    {
        if (Input.GetMouseButtonUp(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) return true;
        return false;
    }
}
