using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BombAnimator : MonoBehaviour
{
    [Header("Bomb Sprites")]
    [SerializeField] private Sprite bombBlack;
    [SerializeField] private Sprite bombMahogany;
    [SerializeField] private Sprite bombRed;

    [Header("UI Reference")]
    [SerializeField] private Image bombImage;

    [Header("Breathing Animation")]
    [SerializeField] private float breatheMinScale = 0.95f;
    [SerializeField] private float breatheMaxScale = 1.15f;
    [SerializeField] private float urgentRedMaxScale = 1.28f;

    [Header("Color cycle Timing")]
    [SerializeField] private float normalCycleSpeed = 0.6f; // Black <-> Mahogany
    [SerializeField] private float urgentCycleSpeed = 0.3f; // Black <-> Mahogany <-> Red (fast)

    private float gameStartTime;
    private float gameDuration;
    private bool isAnimating = false;
    private Coroutine animationCoroutine;

    void Awake()
    {
        if (!bombImage)
        {
            bombImage = GetComponent<Image>();
        }

    }

    /// <summary>
    /// Start the bomb animation
    /// </summary>
    /// <param name="duration">Total game duration in seconds</param>
    public void StartAnimation(float duration)
    {
        if (isAnimating)
        {
            StopAnimation();
        }

        gameStartTime = Time.time;
        gameDuration = duration;
        isAnimating = true;

        if (bombImage)
        {
            bombImage.gameObject.SetActive(true);
        }
        
        animationCoroutine = StartCoroutine(AnimateBomb());

        Debug.Log($"Bomb animation started for {duration}");
    }

    /// <summary>
    /// Stop all animations
    /// </summary>
    public void StopAnimation()
    {
        isAnimating = false;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        //reset to default
        if (bombImage)
        {
            bombImage.transform.localScale = Vector3.one;
            bombImage.sprite = bombBlack;
        }
    }

    /// <summary>
    /// Main animation loop - handles both breathing and color cycling
    /// </summary>
    IEnumerator AnimateBomb()
    {
        while (isAnimating)
        {
            float timeElapsed = Time.time - gameStartTime;
            float timeRemaining = gameDuration - timeElapsed;
            float percentageRemaining = Mathf.Clamp01(timeRemaining / gameDuration);

            // Color Cycling based on time remaining
            if (percentageRemaining > 0.5f)
            {
                // Normal phase: one shared pulse drives both scale and color.
                float pulse = Mathf.PingPong(Time.time / normalCycleSpeed, 1f);
                float scale = Mathf.Lerp(breatheMinScale, breatheMaxScale, pulse);

                if (bombImage)
                {
                    bombImage.transform.localScale = Vector3.one * scale;
                    bombImage.sprite = pulse < 0.5f ? bombBlack : bombMahogany;
                }
            } else
            {
                // Urgent phase: Black -> Mahogany -> Red, with red peaking larger.
                float cycle = Time.time / urgentCycleSpeed % 3f;
                int phase = Mathf.FloorToInt(cycle);
                float phaseT = cycle - phase;

                if (bombImage)
                {
                    if (phase == 0)
                    {
                        bombImage.transform.localScale =
                            Vector3.one * Mathf.Lerp(breatheMinScale, breatheMaxScale, phaseT);
                        bombImage.sprite = bombBlack;
                    } else if (phase == 1)
                    {
                        bombImage.transform.localScale =
                            Vector3.one * Mathf.Lerp(breatheMaxScale, urgentRedMaxScale, phaseT);
                        bombImage.sprite = bombMahogany;
                    } else
                    {
                        bombImage.transform.localScale =
                            Vector3.one * Mathf.Lerp(urgentRedMaxScale, breatheMinScale, phaseT);
                        bombImage.sprite = bombRed;
                    }
                }
            }
            yield return null;
        }

        if (bombImage)
        {
            bombImage.gameObject.SetActive(false);
        }
    }

    //show bomb
    public void Show()
    {
        if (bombImage)
        {
            bombImage.gameObject.SetActive(true);
        }
    }

    //hide bomb
    public void Hide()
    {
        StopAnimation();
        if (bombImage)
        {
            bombImage.gameObject.SetActive(false);
        }
    }

    public void SnapTo(Transform target)
    {
        if (target != null && bombImage != null)
        {
            RectTransform bombRect = bombImage.rectTransform;
            RectTransform targetRect = target as RectTransform;

            if (bombRect != null && targetRect != null)
            {
                // Exact UI snap: use target local space and reset to center.
                bombRect.SetParent(targetRect, false);
                bombRect.anchorMin = new Vector2(0.5f, 0.5f);
                bombRect.anchorMax = new Vector2(0.5f, 0.5f);
                bombRect.pivot = new Vector2(0.5f, 0.5f);
                bombRect.anchoredPosition = Vector2.zero;
                return;
            }

            bombImage.transform.position = target.position;
        }
    }

    /// <summary>
    /// check if bomb is currently animating
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }
}
