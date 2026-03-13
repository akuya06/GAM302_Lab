using UnityEngine;
using Fusion;

/// <summary>
/// Fusion NetworkBehaviour that drives player movement in multiplayer.
/// Bridges NetworkInputData → CharacterController, replacing direct Input reading
/// from PlayerMovement and MouseMovement for the local player.
/// Remote players are moved by Fusion's state sync.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float verticalClampMax = 80f;
    [SerializeField] private float verticalClampMin = -80f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;   // empty transform at eye level
    [SerializeField] private Camera playerCamera;

    // Synced over network
    [Networked] private Vector3 _syncedPosition { get; set; }
    [Networked] private Vector3 _syncedVelocity { get; set; }
    [Networked] private float    _syncedYaw     { get; set; }
    [Networked] private float    _syncedPitch   { get; set; }

    // Local-only
    private CharacterController _cc;
    private float   _verticalVelocity;
    private bool    _isGrounded;

    // Disable old input-reading scripts so they don't fight with us
    private PlayerMovement   _oldMovement;
    private MouseMovement    _oldLook;

    public override void Spawned()
    {
        try
        {
            _cc = GetComponent<CharacterController>();

            // Disable the legacy MonoBehaviour scripts so they don't read Input independently
            _oldMovement = GetComponent<PlayerMovement>();
            _oldLook     = GetComponent<MouseMovement>();
            if (_oldMovement != null) _oldMovement.enabled = false;
            if (_oldLook     != null) _oldLook.enabled     = false;

            if (Object.HasInputAuthority)
            {
                // This is the local player
                EnableLocalPlayerSetup();
            }
            else
            {
                // Remote player – disable camera and audio listener
                DisableRemotePlayerSetup();
            }

            if (Object.HasStateAuthority)
            {
                _syncedPosition = transform.position;
                _syncedYaw = transform.eulerAngles.y;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in NetworkPlayerController.Spawned: {e.Message}");
        }
    }

    // Called every network tick (default 60 Hz on Fusion Shared/Client-Server)
    public override void FixedUpdateNetwork()
    {
        try
        {
            if (Runner == null)
                return;

            if (!Object.HasStateAuthority)
                return;

            if (!GetInput(out NetworkInputData input))
                return;

            // --- Grounded check ---
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            _isGrounded = Physics.SphereCast(origin, 0.3f, Vector3.down, out _, 0.35f);

            if (_isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            // --- Horizontal movement ---
            Vector3 move = transform.right   * input.MoveDirection.x
                         + transform.forward * input.MoveDirection.y;

            // --- Jump ---
            if (input.Buttons.IsSet(NetworkInputData.BUTTON_JUMP) && _isGrounded)
                _verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);

            _verticalVelocity += gravity * Runner.DeltaTime;

            Vector3 horizontalMove = move * moveSpeed;
            Vector3 finalMove = new Vector3(horizontalMove.x, _verticalVelocity, horizontalMove.z);
            _cc.Move(finalMove * Runner.DeltaTime);

            _syncedVelocity = finalMove;
            _syncedPosition = transform.position;

            // --- Look (yaw on body, pitch on camera) ---
            _syncedYaw   += input.LookDelta.x * lookSensitivity;
            _syncedPitch  = Mathf.Clamp(_syncedPitch - input.LookDelta.y * lookSensitivity,
                                        verticalClampMin, verticalClampMax);

            // Apply rotation to body and camera holder
            transform.rotation           = Quaternion.Euler(0f,      _syncedYaw,   0f);
            if (cameraHolder != null)
                cameraHolder.localRotation = Quaternion.Euler(_syncedPitch, 0f, 0f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in FixedUpdateNetwork: {e.Message}");
        }
    }

    // Called every Unity frame for visual smoothing
    public override void Render()
    {
        if (!Object.HasStateAuthority)
        {
            float lerpSpeed = Runner != null ? Runner.DeltaTime * 20f : Time.deltaTime * 20f;
            transform.position = Vector3.Lerp(transform.position, _syncedPosition, lerpSpeed);
            transform.rotation = Quaternion.Euler(0f, _syncedYaw, 0f);
        }

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(_syncedPitch, 0f, 0f);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private void EnableLocalPlayerSetup()
    {
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);

            // There should only be one AudioListener in the scene
            var listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void DisableRemotePlayerSetup()
    {
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);
    }
}
