using UnityEngine;
using Fusion;

/// <summary>
/// Handles player movement and camera control for a networked player.
/// It reads input for the local player and moves the character controller.
/// For remote players, it relies on Fusion's network transform synchronization.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5.0f;

    [Header("Camera")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private float _mouseSensitivity = 2.0f;

    private CharacterController _cc;
    private float _pitch = 0f;

    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();

        if (_playerCamera != null)
        {
            bool isLocal = Object.HasInputAuthority;
            _playerCamera.gameObject.SetActive(isLocal);

            var audioListener = _playerCamera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = isLocal;
            }
        }

        if (Object.HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("Local player spawned. Camera and input enabled.");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || _cc == null)
        {
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            // Di chuyển theo hướng player (local space)
            Vector3 move = transform.right * data.MoveDirection.x + transform.forward * data.MoveDirection.z;
            _cc.Move(_moveSpeed * move.normalized * Runner.DeltaTime);

            // Mouse look (networked)
            if (_playerCamera != null && _cameraHolder != null)
            {
                float mouseX = data.LookDelta.x * _mouseSensitivity;
                float mouseY = data.LookDelta.y * _mouseSensitivity;

                // Rotate player horizontally
                transform.Rotate(Vector3.up * mouseX);

                // Rotate camera vertically
                _pitch -= mouseY;
                _pitch = Mathf.Clamp(_pitch, -80f, 80f);
                _cameraHolder.localRotation = Quaternion.Euler(_pitch, 0, 0);
            }
        }
    }
}
