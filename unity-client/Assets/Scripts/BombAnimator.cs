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
    [SerializeField] private float breatheSpeed = 1.5f;
    [SerializeField] private float breatheMinScale = 0.95f;
    [SerializeField] private float breatheMaxScale = 1.15f;

    [Header("Color cycle Timing")]
    [SerializeField] private float normalCycleSpeed = 0.6f; // Black <-> Mahogany
    [SerializeField] private float urgentCycleSpeed = 0.3f; // Black <-> Mahogany <-> Red (fast)

    private float gameStartTime;
    private float gameDuration;
    private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private Transform originalParent;

    void Awake()
    {
        if (!bombImage)
        {
            bombImage = GetComponent<Image>();
        }

        if (bombImage != null)
        {
            originalParent = bombImage.transform.parent;
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

            //breathing animation (scale pulsing)
            float breathe = Mathf.Sin(Time.time * breatheSpeed);
            float scale = Mathf.Lerp(breatheMinScale, breatheMaxScale, (breathe + 1f) / 2f);

            if (bombImage)
            {
                bombImage.transform.localScale = Vector3.one * scale;
            }

            // Color Cycling based on time remaining
            if (percentageRemaining > 0.5f)
            {
                //phase 1 cycle
                float cycle = Mathf.PingPong(Time.time / normalCycleSpeed, 1f);

                if (bombImage)
                {
                    bombImage.sprite = cycle < 0.5f ? bombBlack : bombMahogany;
                }
            } else
            {
                //phase 2 cycle
                float cycle = Time.time / urgentCycleSpeed % 3f;

                if (bombImage)
                {
                    if (cycle < 1f)
                    {
                        bombImage.sprite = bombBlack;
                    } else if (cycle < 2f)
                    {
                        bombImage.sprite = bombMahogany;
                    } else
                    {
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
            // Keep bomb under its original UI hierarchy to avoid accidental reparent drift.
            if (originalParent != null && bombImage.transform.parent != originalParent)
            {
                bombImage.transform.SetParent(originalParent, true);
            }

            RectTransform bombRect = bombImage.rectTransform;
            RectTransform targetRect = target as RectTransform;

            if (bombRect != null && targetRect != null)
            {
                Canvas canvas = bombImage.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    RectTransform canvasRect = canvas.transform as RectTransform;
                    Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, targetRect.position);

                    if (canvasRect != null &&
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvasRect,
                            screenPoint,
                            cam,
                            out Vector2 localPoint))
                    {
                        bombRect.SetParent(canvasRect, false);
                        bombRect.anchoredPosition = localPoint;
                        return;
                    }
                }
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
