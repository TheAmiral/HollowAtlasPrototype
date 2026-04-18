using UnityEngine;

public class CharacterAnimatorBridge : MonoBehaviour
{
    [Header("Animator Parameters")]
    public string horParam = "Hor";
    public string vertParam = "Vert";
    public string stateParam = "State";
    public string isJumpParam = "IsJump";

    [Header("Smoothing")]
    public float dampTime = 0.08f;

    private Animator animator;
    private PlayerMovement playerMovement;
    private CharacterController controller;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        controller = GetComponentInParent<CharacterController>();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        if (animator == null || playerMovement == null)
            return;

        Vector3 move = playerMovement.CurrentMoveDirection;

        float hor = move.x;
        float vert = move.z;
        float state = move.sqrMagnitude > 0.001f ? 1f : 0f;
        bool isJump = controller != null && !controller.isGrounded;

        animator.SetFloat(horParam, hor, dampTime, Time.deltaTime);
        animator.SetFloat(vertParam, vert, dampTime, Time.deltaTime);
        animator.SetFloat(stateParam, state, dampTime, Time.deltaTime);

        if (HasParameter(isJumpParam))
            animator.SetBool(isJumpParam, isJump);
    }

    bool HasParameter(string paramName)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName)
                return true;
        }

        return false;
    }
}