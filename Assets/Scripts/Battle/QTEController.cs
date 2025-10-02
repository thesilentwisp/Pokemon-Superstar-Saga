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
        if (attackCircle)
        {
            attackCircle.gameObject.SetActive(true);
            attackCircle.rectTransform.localScale = Vector3.one;
        }
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
        while (TapHeld())
            yield return null;
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
        while (TapHeld())
            yield return null;
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
        float timeout = target + 0.6f;
        float t = 0f;
        while (t < timeout)
        {
            t += Time.deltaTime;
            if (attackCircle)
            {
                float norm = target > Mathf.Epsilon ? Mathf.Clamp01(t / target) : 1f;
                float scale = Mathf.Clamp01(1f - norm);
                attackCircle.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
            if (TapDownThisFrame())
            {
                float delta = t - target;
                float dt = Mathf.Abs(delta);
                TimingResult timing = dt <= perfectWin ? TimingResult.Perfect : dt <= goodWin ? TimingResult.Good : TimingResult.Miss;
                Debug.Log($"Defense input {delta:+0.000;-0.000;0.000}s from perfect ({delta * 1000f:+0;-0;0}ms). Result: {timing}");
                onDefenseComplete?.Invoke(timing);
                if (attackCircle) attackCircle.gameObject.SetActive(false);
                running = false;
                yield break;
            }
            yield return null;
        }
        running = false;
        if (attackCircle) attackCircle.gameObject.SetActive(false);
        Debug.Log("Defense input missed: no tap detected.");
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

    bool TapHeld()
    {
        if (Input.GetMouseButton(0)) return true;
        if (Input.touchCount > 0)
        {
            var phase = Input.GetTouch(0).phase;
            if (phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary) return true;
        }
        return false;
    }
}
