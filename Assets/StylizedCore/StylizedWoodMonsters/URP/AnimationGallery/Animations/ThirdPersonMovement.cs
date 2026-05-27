using UnityEngine;
using System.Collections;

public class ThirdPersonMovement : MonoBehaviour
{
    private CharacterController controller;
    private Transform cam;
    private Animator animator;

    [Header("Movement Speeds")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Physics")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    private string currentAnimState;

    [Header("Combat Settings")]
    public string attackAnimation = "PunchLeft";
    public float attackDuration = 0.8f;
    private float attackCooldown = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        // Lock mouse cursor to screen so it doesn't wander off window
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Press Escape to free the mouse cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        // Re-lock mouse if clicking back on screen
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetMouseButtonDown(0) && attackCooldown <= 0f)
        {
            StartCoroutine(AttackRoutine());
        }

        if (attackCooldown > 0f)
        {
            attackCooldown -= Time.deltaTime;
            // Apply gravity and return so they can't move during the attack
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            return;
        }



        // 1. Simple Gravity
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Read Keyboard Inputs (WASD)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // 3. Move and Rotate Player relative to Camera direction
        if (direction.magnitude >= 0.1f)
        {
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Calculate angle to face based on camera angle
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Move player
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);

            // Play Walk/Run animation (Matches names in Animator exactly)
            ChangeAnimationState(isRunning ? "Running" : "Walking");
        }
        else
        {
            // Play Idle animation if standing still
            ChangeAnimationState("Idle");
        }

        // Apply Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    IEnumerator AttackRoutine()
    {
        attackCooldown = attackDuration;
        ChangeAnimationState(attackAnimation);
        
        // Wait briefly for the punch/slash swing to connect
        yield return new WaitForSeconds(0.4f);

        // Find enemies in a sphere in front of the player
        Vector3 sphereCenter = transform.position + transform.forward * 1.5f + Vector3.up * 1f;
        Collider[] hitColliders = Physics.OverlapSphere(sphereCenter, 1.2f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;

            CharacterHealth targetHealth = hitCollider.GetComponent<CharacterHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(25); // Deals 25 damage per punch
            }
        }

        yield return new WaitForSeconds(attackDuration - 0.4f);
    }

    void ChangeAnimationState(string newState)
    {
        if (currentAnimState == newState) return;

        if (animator != null)
        {
            animator.CrossFade(newState, 0.1f);
        }
        currentAnimState = newState;
    }
}
