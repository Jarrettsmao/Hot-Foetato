using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 180f;
    [SerializeField] private bool clockwise = true;

    void Update()
    {
        float dir = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, dir * degreesPerSecond * Time.deltaTime);
    }
}
