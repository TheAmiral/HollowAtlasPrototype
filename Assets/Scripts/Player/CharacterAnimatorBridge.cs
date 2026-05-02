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

    private float resumeGraceTimer;
    private float prevTimeScale = 1f;
    private bool gameplayBlockedLastFrame;
    private const float RESUME_GRACE = 0.45f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        if (animator == null || playerMovement == null)
            return;

        bool gameplayBlocked = IsGameplayBlocked();
        bool resumedFromTimeScale = prevTimeScale <= 0f && Time.timeScale > 0f;
        prevTimeScale = Time.timeScale;

        if (gameplayBlocked)
        {
            gameplayBlockedLastFrame = true;
            resumeGraceTimer = RESUME_GRACE;
            playerMovement.SuppressFallAnimation(RESUME_GRACE);
            SetJump(false);
            return;
        }

        if (gameplayBlockedLastFrame || resumedFromTimeScale)
        {
            gameplayBlockedLastFrame = false;
            resumeGraceTimer = RESUME_GRACE;
            playerMovement.SuppressFallAnimation(RESUME_GRACE);
        }

        if (resumeGraceTimer > 0f)
            resumeGraceTimer -= Time.deltaTime;

        Vector3 move = transform.InverseTransformDirection(playerMovement.CurrentMoveDirection);

        float hor = move.x;
        float vert = move.z;
        float state = move.sqrMagnitude > 0.001f ? 1f : 0f;
        bool isJump = resumeGraceTimer <= 0f && playerMovement.ShouldPlayFallAnimation;

        animator.SetFloat(horParam, hor, dampTime, Time.deltaTime);
        animator.SetFloat(vertParam, vert, dampTime, Time.deltaTime);
        animator.SetFloat(stateParam, state, dampTime, Time.deltaTime);

        SetJump(isJump);
    }

    bool IsGameplayBlocked()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return true;

        if (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending)
            return true;

        if (BossRewardSystem.Instance != null && BossRewardSystem.Instance.RewardPending)
            return true;

        return Time.timeScale <= 0f;
    }

    void SetJump(bool isJump)
    {
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
