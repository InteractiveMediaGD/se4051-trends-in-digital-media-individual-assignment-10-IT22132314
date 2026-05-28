using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DogWander : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private Vector3 startPosition;
    private Vector3 velocity;

    [Header("Movement Settings")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4.0f;
    public float turnSpeedLeft = -60f; // Degrees per second when turning left
    public float turnSpeedRight = 60f; // Degrees per second when turning right
    public float wanderRadius = 15f;
    public float gravity = -9.81f;

    void Start()
    {
        anim = GetComponent<Animator>();
        
        // Auto-add CharacterController if missing
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            // Configure suitable bounds for a German Shepherd
            controller.height = 1.0f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0, 0.5f, 0);
        }

        startPosition = transform.position;
    }

    void Update()
    {
        if (anim == null || controller == null) return;

        // 1. Get the current active animation state
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        
        float currentSpeed = 0f;
        float currentTurn = 0f;

        // Determine speed and turn based on the playing animation state name
        if (stateInfo.IsName("1 type_Run Loop"))
        {
            currentSpeed = runSpeed;
        }
        else if (stateInfo.IsName("1 type_Run Lean Left"))
        {
            currentSpeed = runSpeed;
            currentTurn = turnSpeedLeft;
        }
        else if (stateInfo.IsName("1 type_Run Lean Right"))
        {
            currentSpeed = runSpeed;
            currentTurn = turnSpeedRight;
        }
        else if (stateInfo.IsName("1 type_Walk_Turn_Left"))
        {
            currentSpeed = walkSpeed;
            currentTurn = turnSpeedLeft * 0.6f;
        }
        else if (stateInfo.IsName("1 type_Walk_Turn_Right"))
        {
            currentSpeed = walkSpeed;
            currentTurn = turnSpeedRight * 0.6f;
        }

        // 2. Handle Rotation
        // If outside wander radius, force-rotate back towards start position
        Vector3 toStart = startPosition - transform.position;
        toStart.y = 0;
        if (toStart.magnitude > wanderRadius && currentSpeed > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toStart);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 3f * Time.deltaTime);
        }
        else if (currentTurn != 0)
        {
            // Apply turning from the animation state
            transform.Rotate(0, currentTurn * Time.deltaTime, 0);
        }

        // 3. Move forward
        Vector3 moveDirection = transform.forward * currentSpeed;

        // 4. Apply Gravity
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
        
        moveDirection.y = velocity.y;

        // Move dog
        controller.Move(moveDirection * Time.deltaTime);
    }
}
