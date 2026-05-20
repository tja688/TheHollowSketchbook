using UnityEngine;

public sealed class PaperControlPointSpring : MonoBehaviour
{
    [SerializeField] private float stiffness = 28f;
    [SerializeField] private float damping = 7f;
    [SerializeField] private float idleForce = 0.03f;
    [SerializeField] private Vector3 axis = Vector3.right;
    [SerializeField] private float phase;

    private Vector3 initialLocalPosition;
    private Vector3 velocity;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        axis.Normalize();
    }

    private void Update()
    {
        Vector3 target = initialLocalPosition + axis * (Mathf.Sin(Time.time * 2.2f + phase) * idleForce);
        Vector3 displacement = transform.localPosition - target;
        velocity += (-displacement * stiffness - velocity * damping) * Time.deltaTime;
        transform.localPosition += velocity * Time.deltaTime;
    }
}
