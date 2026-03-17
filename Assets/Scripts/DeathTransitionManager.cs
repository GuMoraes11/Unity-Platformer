using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeathTransitionManager : MonoBehaviour
{
    public static DeathTransitionManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeOutDuration = 0.45f;
    [SerializeField] private float blackHoldDuration = 0.20f;
    [SerializeField] private float fadeInDuration = 0.45f;

    [Header("Pop Timing")]
    [SerializeField] private float delayBeforeSecondPop = 0.16f;
    [SerializeField] private float popOutDuration = 0.32f;
    [SerializeField] private float popInDuration = 0.36f;
    [SerializeField] private float delayBeforeFade = 0.06f;
    [SerializeField] private float delayBeforePopIn = 0.08f;

    private bool isRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    public void BeginDeathSequence(PlayerHealth firstDeadPlayer)
    {
        if (isRunning) return;
        StartCoroutine(DeathSequence(firstDeadPlayer));
    }

    private IEnumerator DeathSequence(PlayerHealth firstDeadPlayer)
    {
        isRunning = true;

        if (CouchCoopSpawner.Instance == null)
        {
            Debug.LogError("No CouchCoopSpawner found in scene.");
            isRunning = false;
            yield break;
        }

        GameObject p1Obj = CouchCoopSpawner.Instance.Player1Instance;
        GameObject p2Obj = CouchCoopSpawner.Instance.Player2Instance;

        PlayerHealth p1 = p1Obj != null ? p1Obj.GetComponent<PlayerHealth>() : null;
        PlayerHealth p2 = p2Obj != null ? p2Obj.GetComponent<PlayerHealth>() : null;

        PlayerHealth secondPlayer = null;
        if (firstDeadPlayer == p1) secondPlayer = p2;
        else if (firstDeadPlayer == p2) secondPlayer = p1;
        else secondPlayer = p1 != null ? p1 : p2;

        if (CouchCoopSpawner.Instance.TetherInstance != null)
            CouchCoopSpawner.Instance.TetherInstance.SetFrozen(true);

        if (firstDeadPlayer != null)
            yield return StartCoroutine(firstDeadPlayer.PlayPopOut(popOutDuration));

        yield return new WaitForSecondsRealtime(delayBeforeSecondPop);

        if (secondPlayer != null)
            yield return StartCoroutine(secondPlayer.PlayPopOut(popOutDuration));

        yield return new WaitForSecondsRealtime(delayBeforeFade);
        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));

        if (p1 != null)
        {
            p1.transform.position = CouchCoopSpawner.Instance.Player1SpawnPosition;
            p1.PrepareHiddenAtRespawn();
        }

        if (p2 != null)
        {
            p2.transform.position = CouchCoopSpawner.Instance.Player2SpawnPosition;
            p2.PrepareHiddenAtRespawn();
        }

        if (SharedHealthManager.Instance != null)
            SharedHealthManager.Instance.ResetHealthToMax();

        yield return new WaitForSecondsRealtime(blackHoldDuration);
        yield return StartCoroutine(FadeTo(0f, fadeInDuration));
        yield return new WaitForSecondsRealtime(delayBeforePopIn);

        if (p1 != null) StartCoroutine(p1.PlayPopIn(popInDuration));
        if (p2 != null) StartCoroutine(p2.PlayPopIn(popInDuration));

        yield return new WaitForSecondsRealtime(popInDuration);

        if (CouchCoopSpawner.Instance.TetherInstance != null)
            CouchCoopSpawner.Instance.TetherInstance.SetFrozen(false);

        isRunning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}