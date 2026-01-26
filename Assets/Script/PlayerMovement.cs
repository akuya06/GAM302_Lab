using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private PlayerStun playerStun;
    public float speed = 5f;
    public float jumpForce = 5f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    Vector3 velocity;
    bool isGrounded;
    bool isMoving;
    private Vector3 lastPosition = Vector3.zero;
    private Vector2 moveInput;
    private bool jumpInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerStun = GetComponent<PlayerStun>();
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

        // Old Input Manager axes (Horizontal/Vertical)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        
        moveInput = new Vector2(moveX, moveY);
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
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
}
