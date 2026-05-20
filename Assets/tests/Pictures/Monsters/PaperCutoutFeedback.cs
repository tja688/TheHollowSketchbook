using UnityEngine;

public sealed class PaperCutoutFeedback : MonoBehaviour
{
    [SerializeField] private float swayAngle = 4f;
    [SerializeField] private float swaySpeed = 1.8f;
    [SerializeField] private float popHeight = 0.18f;
    [SerializeField] private float popSpeed = 2.4f;
    [SerializeField] private float leanAngle = -6f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        float t = Time.time;
        float sway = Mathf.Sin(t * swaySpeed) * swayAngle;
        float pop = Mathf.Abs(Mathf.Sin(t * popSpeed)) * popHeight;
        transform.localPosition = initialLocalPosition + Vector3.up * pop;
        transform.localRotation = initialLocalRotation * Quaternion.Euler(leanAngle, 0f, sway);
    }
}
