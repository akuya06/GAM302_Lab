using System.Collections;
using UnityEngine;
using Fusion;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private PlayerStun playerStun;
    public float speed = 5f;
    public float jumpForce = 5f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    [Header("Input")]
    public JoystickController joystick; // drag joystick UI here (optional)
    Vector3 velocity;
    bool isGrounded;
    bool isMoving;
    private Vector3 lastPosition = Vector3.zero;
    private Vector2 moveInput;
    private bool jumpInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (ShouldDisableForNetwork())
        {
            enabled = false;
            return;
        }

        controller = GetComponent<CharacterController>();
        playerStun = GetComponent<PlayerStun>();
    }

    private bool ShouldDisableForNetwork()
    {
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
            return false;

        var runner = FindFirstObjectByType<NetworkRunner>();
        return runner != null && runner.IsRunning;
    }

    // Update is called once per frame
    void Update()
    {
        // Nếu đang bị stun thì không cho di chuyển
        if (playerStun != null && playerStun.IsStunned())
        {
            // Có thể thêm hiệu ứng hoặc animation bị choáng ở đây
            return;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, 0.4f, groundLayer);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Read input: use joystick if available, otherwise keyboard
        // Luôn cho phép WASD cả mobile và PC
        Vector2 joystickInput = joystick != null ? joystick.Direction : Vector2.zero;
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        // Nếu joystick có input thì ưu tiên, nếu không thì dùng WASD
        moveInput = joystickInput != Vector2.zero ? joystickInput : new Vector2(moveX, moveY);
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        // PC: nhảy bằng phím Jump, Mobile: nhảy bằng nút UI (jumpInput)
        if ((Input.GetButtonDown("Jump") || jumpInput) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            jumpInput = false;
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Check if the player is moving
        if (transform.position != lastPosition)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
        lastPosition = transform.position;
    }

    // Gọi từ nút Jump Mobile (UI Button OnClick)
    public void OnMobileJumpButtonPressed()
    {
        jumpInput = true;
    }
}
