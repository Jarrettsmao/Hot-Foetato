using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownManager : MonoBehaviour
{
    public static CountdownManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private CanvasGroup countdownCanvasGroup;

    [Header("Countdown Settings")]
    [SerializeField] private float numberDisplayTime = 1f; //how long number stays
    [SerializeField] private float fadeInTime = 0.2f; //fade in duration
    [SerializeField] private float fadeOutTime = 0.3f; //fade out duration

    [Header("Animation Settings")]
    [SerializeField] private float scaleStart = 0.5f; //start small
    [SerializeField] private float scaleEnd = 1.2f; //end size
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    //add audio later
    public System.Action OnCountdownFinished;
    private bool isCountingDown = false;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        //hide countdown at start
        if (countdownCanvasGroup)
        {
            countdownCanvasGroup.alpha = 0;    
        }
        if (countdownText)
        {
        countdownText?.gameObject.SetActive(false);    
        }
        
    }

    /// <summary>
    /// Start the countdown sequence
    /// </summary>
    public void StartCountdown()
    {
        if (isCountingDown)
        {
            Debug.LogWarning("Countdown already in progress!");
            return;
        }

        StartCoroutine(CountdownSequence(GetDefaultDurationSeconds()));
    }

    public void StartCountdownTo(long countdownEndUnixMs)
    {
        if (isCountingDown)
        {
            Debug.LogWarning("Countdown already in progress!");
            return;
        }

        long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float secondsRemaining = Mathf.Max(0.25f, (countdownEndUnixMs - now) / 1000f);
        StartCoroutine(CountdownSequence(secondsRemaining));
    }

    private float GetDefaultDurationSeconds()
    {
        float defaultStep = numberDisplayTime + 0.1f;
        return defaultStep * 3f + numberDisplayTime;
    }

    private IEnumerator CountdownSequence(float totalDurationSeconds)
    {
        isCountingDown = true;
        float stepDuration = Mathf.Max(0.1f, totalDurationSeconds / 4f);

        // Activate countdown UI
        countdownText?.gameObject.SetActive(true);

        //show 3
        yield return ShowNumber("3", stepDuration);

        //show 2
        yield return ShowNumber("2", stepDuration);

        //show 1
        yield return ShowNumber("1", stepDuration);

        //show go
        yield return ShowNumber("GO!", stepDuration, Color.green);

        countdownText?.gameObject.SetActive(false);
        isCountingDown = false;

        //notify that the game should start
        OnCountdownFinished?.Invoke();
    }

    private IEnumerator ShowNumber(string text, float segmentDuration, Color? color = null)
    {
        if (countdownText)
        {
            countdownText.text = text;
            countdownText.color = color ?? Color.white;
        }

        float localFadeIn = Mathf.Min(fadeInTime, segmentDuration * 0.35f);
        float localFadeOut = Mathf.Min(fadeOutTime, segmentDuration * 0.35f);
        float holdTime = Mathf.Max(0f, segmentDuration - localFadeIn - localFadeOut);

        //animate fade in + scale up
        float elapsed = 0f;

        while (elapsed < localFadeIn)
        {
            elapsed += Time.deltaTime;
            float t = localFadeIn > 0f ? elapsed / localFadeIn : 1f;

            //Fade in
            if (countdownCanvasGroup)
            {
                countdownCanvasGroup.alpha = Mathf.Lerp(0, 1, t);
            }

            //Scale up
            if (countdownText)
            {
                float scale = Mathf.Lerp(scaleStart, scaleEnd, scaleCurve.Evaluate(t));
                countdownText.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        //Hold at full visibility
        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        // Animate: Fade out
        elapsed = 0f;
        while (elapsed < localFadeOut)
        {
            elapsed += Time.deltaTime;
            float t = localFadeOut > 0f ? elapsed / localFadeOut : 1f;

            // Fade out
            if (countdownCanvasGroup)
            {
                countdownCanvasGroup.alpha = Mathf.Lerp(1, 0, t);
            }

            yield return null;
        }

        // Reset scale
        if (countdownText != null)
        {
            countdownText.transform.localScale = Vector3.one;
        }
    }
}
