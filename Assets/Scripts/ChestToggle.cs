using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChestToggle : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private static readonly int OpenHash = Animator.StringToHash("Open");
    private bool isOpen;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        animator.SetBool(OpenHash, false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isOpen = !isOpen;
            animator.SetBool(OpenHash, isOpen);
        }
    }
}
