using UnityEngine;
using System.Collections;

public class AnimalWander : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;

    [Header("Behavior Settings")]
    public bool isHostile = false; // Set to true for the Bear, false for the Human!

    [Header("Animation Names")]
    public string idleAnimation = "Idle";
    public string walkAnimation = "Run";
    public string hitAnimation = "Bear_GetHitFromFront";

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float wanderRadius = 15f;

    [Header("Aggro Settings")]
    public Transform player;
    public float chaseRange = 8f;
    public float attackRange = 2f;
    public string attackAnimation = "Bear_Attack1";
    public float attackCooldown = 1.5f;

    [Header("Physics")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    private bool isMoving = false;
    private Vector3 startPosition;
    private bool isChasing = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        startPosition = transform.position;
        StartCoroutine(WanderRoutine());
    }

    void Update()
    {


        // 2. Auto-find player (only if hostile)
        if (isHostile)
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Main_Player");
                if (playerObj != null) player = playerObj.transform;
            }

            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                if (distance <= chaseRange)
                {
                    if (!isChasing)
                    {
                        isChasing = true;
                        StopAllCoroutines(); // Stop the wandering routine
                    }

                    if (isChasing)
                    {
                        HandleChaseAndAttack(distance);
                        if (isAttacking || distance <= attackRange)
                        {
                            ApplyGravity(Vector3.zero);
                        }
                    }
                    return; // Stop normal wandering behavior
                }
                else if (isChasing)
                {
                    // Player got away, resume wandering
                    isChasing = false;
                    isMoving = false;
                    StartCoroutine(WanderRoutine());
                }
            }
        }

        // 3. Wander movement using CharacterController
        Vector3 moveVelocity = Vector3.zero;
        if (isMoving)
        {
            moveVelocity = transform.forward * moveSpeed;
        }
        
        ApplyGravity(moveVelocity);
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

    void HandleChaseAndAttack(float distance)
    {
        if (isAttacking) return;

        // Turn to face player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToPlayer), 5f * Time.deltaTime);
        }

        if (distance <= attackRange)
        {
            // Attack player
            isMoving = false;
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(BearAttackRoutine());
            }
            else
            {
                // Play idle between attacks
                anim.CrossFade(idleAnimation, 0.2f);
            }
        }
        else
        {
            // Run/Walk towards player
            anim.CrossFade(walkAnimation, 0.2f);
            
            // Move using CharacterController via Update (this method just sets horizontal motion)
            Vector3 chaseVelocity = transform.forward * moveSpeed;
            ApplyGravity(chaseVelocity);
        }
    }

    IEnumerator BearAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        anim.CrossFade(attackAnimation, 0.1f);
        
        // Wait briefly for paw swipe to connect
        yield return new WaitForSeconds(0.6f);

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackRange + 0.5f)
            {
                CharacterHealth playerHealth = player.GetComponent<CharacterHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(15); // Bear deals 15 damage
                }
            }
        }

        yield return new WaitForSeconds(0.9f);
        isAttacking = false;
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            // 1. Stop and play Idle
            isMoving = false;
            anim.CrossFade(idleAnimation, 0.2f);
            yield return new WaitForSeconds(Random.Range(3f, 6f));

            // 2. Check distance from start
            float distanceFromStart = Vector3.Distance(transform.position, startPosition);

            if (distanceFromStart > wanderRadius)
            {
                Vector3 directionToStart = (startPosition - transform.position).normalized;
                directionToStart.y = 0;
                transform.rotation = Quaternion.LookRotation(directionToStart);
            }
            else
            {
                float randomAngle = Random.Range(0f, 360f);
                transform.rotation = Quaternion.Euler(0, randomAngle, 0);
            }

            // 3. Start walking
            anim.CrossFade(walkAnimation, 0.2f);
            isMoving = true;
            yield return new WaitForSeconds(Random.Range(2f, 5f));
        }
    }
}
