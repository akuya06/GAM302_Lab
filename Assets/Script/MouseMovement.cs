
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Quản lý phép quay camera và nhân vật theo chuyển động chuột
/// </summary>
public class MouseMovement : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalRotationClampMax = 90f;
    [SerializeField] private float verticalRotationClampMin = -90f;
    [Header("Mobile Look Area")]
    [Tooltip("Chỉ dùng touch ở bên phải màn hình (0-1). Ví dụ 0.5 = nửa phải.")]
    [Range(0f, 1f)] [SerializeField] private float lookAreaMinXNormalized = 0.5f;
    [SerializeField] private float tpsDistance = 3f; // Khoảng cách camera từ nhân vật khi ở TPS
    [SerializeField] private float tpsHeight = 1.5f; // Độ cao camera khi ở TPS
    [SerializeField] private Animator animator;
    
    private Transform cameraTransform; // Reference đến Camera con
    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;
    private const float MOUSE_DELTA_THRESHOLD = 0.01f;
    private bool isFPSMode = true; // true = FPS, false = TPS
    private Vector3 fpsCameraLocalPos; // Lưu vị trí camera FPS ban đầu
    private int lookFingerId = -1; // finger dùng để nhìn xung quanh trên mobile

    private void Start()
    {
        // Tìm Camera con của Player
        cameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("Không tìm thấy Camera con của Player!");
            return;
        }

        // Tự động lấy Animator nếu chưa được assign
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Không tìm thấy Animator component trên Player!");
            }
        }

        LockAndHideCursor();
        // Lưu vị trí camera FPS ban đầu
        fpsCameraLocalPos = cameraTransform.localPosition;
    }

    private void Update()
    {
        // Xử lý cả chuột (PC test) và touch (Android) cùng lúc
        HandleMouseLook();
        HandleTouchLook();
        HandleCameraToggle();
    }

    /// <summary>
    /// Xử lý nhìn bằng chuột - quay camera theo trục X (lên/xuống) và quay nhân vật theo trục Y (trái/phải)
    /// </summary>
    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        // Sử dụng Input System cũ: các axis "Mouse X" và "Mouse Y"
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Bỏ qua nhiễu rất nhỏ
        if (Mathf.Abs(mouseX) + Mathf.Abs(mouseY) < MOUSE_DELTA_THRESHOLD)
            return;

        // Quay camera theo trục X (nhìn lên/xuống)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, verticalRotationClampMin, verticalRotationClampMax);
        // Tính giá trị aimPitch từ 0 đến 1 dựa trên góc nhìn lên/xuống
        float aimPitch = Mathf.InverseLerp(verticalRotationClampMin, verticalRotationClampMax, verticalRotation);
        aimPitch = 1f - aimPitch; // đảo ngược giá trị
        animator.SetFloat("AimPitch", aimPitch);
        Debug.Log("AimPitch: " + aimPitch);
        // Quay nhân vật theo trục Y (nhân vật xoay trái/phải)
        horizontalRotation += mouseX;
        ApplyRotation();
    }

    /// <summary>
    /// Xử lý nhìn bằng cảm ứng khi ở chế độ Mobile: kéo trên vùng không có UI để quay camera
    /// </summary>
    private void HandleTouchLook()
    {
        if (cameraTransform == null) return;

        if (Input.touchCount == 0)
        {
            lookFingerId = -1;
            return;
        }

        // Nếu chưa có finger để look, chọn một finger không đè lên UI
        if (lookFingerId == -1)
        {
            foreach (Touch t in Input.touches)
            {
                if (t.phase == TouchPhase.Began)
                {
                    bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
                    // Chỉ cho phép dùng vùng nhìn ở bên phải màn hình (tránh joystick bên trái)
                    bool inLookArea = t.position.x / Screen.width >= lookAreaMinXNormalized;

                    if (!overUI && inLookArea)
                    {
                        lookFingerId = t.fingerId;
                        break;
                    }
                }
            }
        }

        // Tìm touch tương ứng finger đang dùng để look
        Touch? maybeTouch = null;
        foreach (Touch t in Input.touches)
        {
            if (t.fingerId == lookFingerId)
            {
                maybeTouch = t;
                break;
            }
        }

        if (maybeTouch.HasValue)
        {
            Touch touch = maybeTouch.Value;

            // Dùng deltaPosition để quay camera
            Vector2 delta = touch.deltaPosition;
            float sensitivity = mouseSensitivity * 0.1f; // giảm bớt cho cảm ứng
            float lookX = delta.x * sensitivity;
            float lookY = delta.y * sensitivity;

            if (Mathf.Abs(lookX) + Mathf.Abs(lookY) > MOUSE_DELTA_THRESHOLD)
            {
                verticalRotation -= lookY;
                verticalRotation = Mathf.Clamp(verticalRotation, verticalRotationClampMin, verticalRotationClampMax);

                float aimPitch = Mathf.InverseLerp(verticalRotationClampMin, verticalRotationClampMax, verticalRotation);
                aimPitch = 1f - aimPitch;
                if (animator != null)
                {
                    animator.SetFloat("AimPitch", aimPitch);
                }

                horizontalRotation += lookX;
                ApplyRotation();
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lookFingerId = -1;
            }
        }
        else
        {
            // finger hiện tại không còn, reset
            lookFingerId = -1;
        }
    }

    /// <summary>
    /// Áp dụng phép quay cho camera (trục X) và nhân vật (trục Y)
    /// </summary>
    private void ApplyRotation()
    {
        if (cameraTransform == null) return;

        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }

    /// <summary>
    /// Khóa và ẩn con trỏ chuột
    /// </summary>
    private void LockAndHideCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    /// <summary>
    /// Mở khóa con trỏ chuột (dùng khi tạm dừng game)
    /// </summary>
    public void UnlockCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Confined;
        UnityEngine.Cursor.visible = true;
    }

    /// <summary>
    /// Xử lý chuyển đổi giữa FPS và TPS khi bấm F5
    /// </summary>
    private void HandleCameraToggle()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            isFPSMode = !isFPSMode;
            
            if (isFPSMode)
            {
                SwitchToFPS();
            }
            else
            {
                SwitchToTPS();
            }
        }
    }

    /// <summary>
    /// Chuyển sang chế độ FPS (camera tại vị trí đầu nhân vật)
    /// </summary>
    private void SwitchToFPS()
    {
        if (cameraTransform == null) return;
        cameraTransform.localPosition = fpsCameraLocalPos;
    }

    /// <summary>
    /// Chuyển sang chế độ TPS (camera nhìn từ phía sau và trên nhân vật)
    /// </summary>
    private void SwitchToTPS()
    {
        if (cameraTransform == null) return;

        // Tính vị trí camera TPS: phía sau Player, cách xa một khoảng
        Vector3 tpsOffset = -transform.forward * tpsDistance + Vector3.up * tpsHeight;
        cameraTransform.localPosition = tpsOffset;
    }
}