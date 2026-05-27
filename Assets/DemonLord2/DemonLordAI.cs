using UnityEngine;
using System.Collections;

public class DemonLordAI : MonoBehaviour
{
    public Transform player; // Reference to player
    private Animator animator;
    private CharacterController controller;

    [Header("Ranges")]
    public float chaseRange = 12f;
    public float attackRange = 2.5f;

    [Header("Speeds")]
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 5f;

    [Header("Combat Settings")]
    public float attackCooldown = 2f;
    public string hitAnimation = "Taking_Damage1";
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    [Header("Physics")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    private string currentAnimState;

    // Idle Cycling variables to keep the Demon Lord animated
    private float nextIdleChangeTime = 0f;
    private string[] idleStates = { "Idle1", "Idle2", "Idle3" };
    private int currentIdleIndex = 0;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();

        // Disable root motion and force AlwaysAnimate culling mode to prevent freezing at distance
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        
        // Find player automatically if not set
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Main_Player");
            }
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void Update()
    {
        if (player == null)
        {
            PlayIdleCycle();
            ApplyGravity(Vector3.zero);
            return;
        }

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isAttacking)
        {
            // Rotate to face player even while attacking
            FacePlayer();
            ApplyGravity(Vector3.zero);
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            // Within attack range
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                // Wait during cooldown
                FacePlayer();
                PlayIdleCycle();
                ApplyGravity(Vector3.zero);
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // Chase player
            FacePlayer();
            
            // Move towards player using CharacterController
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            Vector3 horizontalMove = direction * moveSpeed;
            
            ApplyGravity(horizontalMove);
            ChangeAnimationState("Walk");
            
            // Reset idle timer so transitions are smooth next time
            nextIdleChangeTime = 0f;
        }
        else
        {
            // Idle when player is far away
            PlayIdleCycle();
            ApplyGravity(Vector3.zero);
        }
    }

    void PlayIdleCycle()
    {
        if (Time.time >= nextIdleChangeTime)
        {
            // Choose a random idle animation variation (Idle1, Idle2, Idle3)
            int newIndex = Random.Range(0, idleStates.Length);
            currentIdleIndex = newIndex;
            
            // Pick a random interval between 5 and 10 seconds for the next variation
            nextIdleChangeTime = Time.time + Random.Range(5f, 10f);
        }
        
        ChangeAnimationState(idleStates[currentIdleIndex]);
    }

    void ApplyGravity(Vector3 horizontalVelocity)
    {
        if (controller != null)
        {
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
            
            Vector3 finalVelocity = horizontalVelocity;
            finalVelocity.y = velocity.y;
            
            controller.Move(finalVelocity * Time.deltaTime);
        }
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep rotation strictly horizontal
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Play random attack animation (Attack1, Attack2, or Attack3)
        int attackIndex = Random.Range(1, 4); // 1, 2, or 3
        string attackState = "Attack" + attackIndex;

        ChangeAnimationState(attackState);

        // Wait briefly for the hammer to strike
        yield return new WaitForSeconds(0.7f);

        // Deal damage if player is still in range
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange + 0.5f)
            {
                CharacterHealth playerHealth = player.GetComponent<CharacterHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(20); // Demon Lord deals 20 damage
                }
            }
        }

        // Wait for the rest of the attack animation to play
        yield return new WaitForSeconds(0.8f);

        isAttacking = false;
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
