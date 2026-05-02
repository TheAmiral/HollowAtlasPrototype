using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 12f;
    public Transform cameraTransform;

    [Header("Ground")]
    public float groundedStickForce = -2f;
    public float spawnGroundSnapDistance = 2f;
    public float groundProbeDistance = 0.18f;
    public float groundedCoyoteTime = 0.18f;
    public float fallAnimationAirborneDelay = 0.24f;
    public float fallAnimationMinVerticalSpeed = -1.25f;
    public float resumeFallSuppressTime = 0.45f;

    [Header("Dash Settings")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.75f;

    [Header("Dash Damage")]
    public int dashDamage = 12;
    public float dashHitRadius = 1.2f;
    public float dashHitYOffset = 0.6f;

    [Header("Dash Trail")]
    public float dashTrailTime = 0.18f;
    public float dashTrailWidth = 0.7f;

    public Vector3 CurrentMoveDirection => currentMoveDirection;
    public bool IsDashing => isDashing;
    public float VerticalVelocity => verticalVelocity;
    public bool StableGrounded => stableGrounded;
    public bool ShouldPlayFallAnimation =>
        airborneAnimationSuppressTimer <= 0f &&
        !stableGrounded &&
        airborneTimer >= fallAnimationAirborneDelay &&
        verticalVelocity <= fallAnimationMinVerticalSpeed;

    private CharacterController controller;
    private float verticalVelocity;
    private float timeSinceGrounded;
    private float airborneTimer;
    private float airborneAnimationSuppressTimer;
    private bool stableGrounded = true;
    private bool gameplayBlockedLastFrame;

    private Vector3 currentMoveDirection;
    private Vector3 dashDirection;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    private TrailRenderer dashTrail;
    private readonly HashSet<EnemyHealth> enemiesHitThisDash = new();

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        SetupDashTrail();
    }

    void Start()
    {
        SnapToGroundOnSpawn();

        verticalVelocity = controller.isGrounded ? groundedStickForce : 0f;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (IsGameplayBlocked())
        {
            gameplayBlockedLastFrame = true;
            SuppressFallAnimation(resumeFallSuppressTime);
            return;
        }

        if (gameplayBlockedLastFrame)
        {
            gameplayBlockedLastFrame = false;
            SuppressFallAnimation(resumeFallSuppressTime);

            if (controller != null && (controller.isGrounded || ProbeGrounded()))
                RegisterGrounded();
        }

        if (airborneAnimationSuppressTimer > 0f)
            airborneAnimationSuppressTimer -= Time.deltaTime;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        Vector2 input = ReadInput();
        Vector3 move = CalculateMoveDirection(input);

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        currentMoveDirection = move;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

        if (!isDashing && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && dashCooldownTimer <= 0f)
            StartDash();

        Vector3 horizontalVelocity;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            horizontalVelocity = dashDirection * dashSpeed;

            if (dashTrail != null)
                dashTrail.emitting = true;

            if (dashTimer <= 0f)
                EndDash();
        }
        else
        {
            horizontalVelocity = currentMoveDirection * moveSpeed;

            if (dashTrail != null)
                dashTrail.emitting = false;
        }

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        CollisionFlags collisionFlags = controller.Move(finalVelocity * Time.deltaTime);

        if ((collisionFlags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;

        UpdateGroundingState((collisionFlags & CollisionFlags.Below) != 0 || controller.isGrounded || ProbeGrounded());

        if (isDashing)
            ProcessDashDamage();

        RotateCharacter(horizontalVelocity);
    }

    Vector2 ReadInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current == null)
            return input;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            input.x -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            input.x += 1f;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        return input;
    }

    Vector3 CalculateMoveDirection(Vector2 input)
    {
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            return camForward * input.y + camRight * input.x;
        }

        return new Vector3(input.x, 0f, input.y);
    }

    void RotateCharacter(Vector3 horizontalVelocity)
    {
        Vector3 flatMove = horizontalVelocity;
        flatMove.y = 0f;

        if (flatMove.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(flatMove.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (currentMoveDirection.sqrMagnitude > 0.001f)
            dashDirection = currentMoveDirection.normalized;
        else
            dashDirection = transform.forward;

        dashDirection.y = 0f;
        dashDirection.Normalize();

        enemiesHitThisDash.Clear();

        AudioManager.Instance?.PlayDash();

        if (dashTrail != null)
        {
            dashTrail.time = dashTrailTime;
            dashTrail.Clear();
            dashTrail.emitting = true;
        }
    }

    void EndDash()
    {
        isDashing = false;

        if (dashTrail != null)
            dashTrail.emitting = false;
    }

    void ProcessDashDamage()
    {
        Vector3 hitCenter = transform.position + Vector3.up * dashHitYOffset;
        Collider[] hits = Physics.OverlapSphere(hitCenter, dashHitRadius);

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            if (enemy == null)
                enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null || enemy.IsDead)
                continue;

            if (enemiesHitThisDash.Contains(enemy))
                continue;

            enemiesHitThisDash.Add(enemy);
            enemy.TakeDamage(dashDamage);
        }
    }

    void SetupDashTrail()
    {
        dashTrail = GetComponent<TrailRenderer>();

        if (dashTrail == null)
            dashTrail = gameObject.AddComponent<TrailRenderer>();

        dashTrail.enabled = true;
        dashTrail.emitting = false;
        dashTrail.time = dashTrailTime;
        dashTrail.minVertexDistance = 0.05f;
        dashTrail.widthMultiplier = dashTrailWidth;
        dashTrail.alignment = LineAlignment.View;
        dashTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dashTrail.receiveShadows = false;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.15f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );
        dashTrail.colorGradient = gradient;

        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 1f);
        widthCurve.AddKey(1f, 0f);
        dashTrail.widthCurve = widthCurve;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            Material trailMaterial = new Material(shader);
            trailMaterial.color = new Color(1f, 0.8f, 0.25f, 1f);
            dashTrail.material = trailMaterial;
        }
    }

    public void SuppressFallAnimation(float duration)
    {
        airborneAnimationSuppressTimer = Mathf.Max(airborneAnimationSuppressTimer, duration);
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

    void UpdateGroundingState(bool groundedContact)
    {
        if (groundedContact)
        {
            RegisterGrounded();
            return;
        }

        timeSinceGrounded += Time.deltaTime;
        stableGrounded = timeSinceGrounded <= groundedCoyoteTime;

        if (stableGrounded)
            airborneTimer = 0f;
        else
            airborneTimer += Time.deltaTime;
    }

    void RegisterGrounded()
    {
        timeSinceGrounded = 0f;
        airborneTimer = 0f;
        stableGrounded = true;
    }

    bool ProbeGrounded()
    {
        if (controller == null || groundProbeDistance <= 0f)
            return false;

        float radius = Mathf.Max(0.05f, controller.radius - controller.skinWidth);
        float bottomSphereOffset = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        Vector3 center = transform.position + controller.center;
        Vector3 sphereOrigin = center + Vector3.down * bottomSphereOffset + Vector3.up * groundProbeDistance;
        float castDistance = groundProbeDistance + controller.skinWidth + 0.03f;

        return Physics.SphereCast(
            sphereOrigin,
            radius,
            Vector3.down,
            out _,
            castDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    void SnapToGroundOnSpawn()
    {
        if (spawnGroundSnapDistance <= 0f)
            return;

        float capsuleBottom = transform.position.y + controller.center.y - (controller.height * 0.5f);
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, spawnGroundSnapDistance + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        float targetBottom = hit.point.y + controller.skinWidth;
        float delta = targetBottom - capsuleBottom;

        if (Mathf.Abs(delta) > 0.001f)
            controller.Move(Vector3.up * delta);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 hitCenter = transform.position + Vector3.up * dashHitYOffset;
        Gizmos.DrawWireSphere(hitCenter, dashHitRadius);
    }
}
