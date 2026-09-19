using UnityEngine;
using UnityEngine.InputSystem;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.6f, 0);

    [Tooltip("Higher = tighter follow. 35 gives a very tiny organic floatback.")]
    public float positionLerpSpeed = 35f;
    public float bodyRotationLerpSpeed = 15f;

    [Header("Free Look Settings")]
    public float mouseSensitivity = 0.15f;
    public float maxHorizontalLook = 45f;
    public float maxVerticalLook = 45f;

    [Tooltip("How aggressively the camera centers when moving")]
    public float recenterSpeed = 25f;

    [HideInInspector] public bool isAutoCentering = false;

    private float mouseX = 0f;
    private float mouseY = 0f;
    private Quaternion currentBodyRotation;

    private InputAction lookAction;

    void Awake()
    {
        lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        lookAction.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            currentBodyRotation = target.rotation;
        }
    }

    void OnDisable() => lookAction.Disable();

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Tightly follow the player. The player capsule already moves smoothly, 
        // so we don't want the camera fighting it with slow lag.
        transform.position = Vector3.Lerp(transform.position, target.position + offset, Time.deltaTime * positionLerpSpeed);

        // 2. Aggressive Auto-Centering Lockout
        if (isAutoCentering)
        {
            // Swiftly snap the local mouse look back to perfectly straight
            mouseX = Mathf.Lerp(mouseX, 0f, Time.deltaTime * recenterSpeed);
            mouseY = Mathf.Lerp(mouseY, 0f, Time.deltaTime * recenterSpeed);

            // Force exact zero if we get close enough to prevent lingering decimals
            if (Mathf.Abs(mouseX) < 0.1f) mouseX = 0f;
            if (Mathf.Abs(mouseY) < 0.1f) mouseY = 0f;
        }
        else
        {
            // Normal free look (only accepted when not moving)
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>() * mouseSensitivity;
            mouseX = Mathf.Clamp(mouseX + mouseDelta.x, -maxHorizontalLook, maxHorizontalLook);
            mouseY = Mathf.Clamp(mouseY - mouseDelta.y, -maxVerticalLook, maxVerticalLook);
        }

        // 3. Smoothly track the player's underlying body rotation
        currentBodyRotation = Quaternion.Slerp(currentBodyRotation, target.rotation, Time.deltaTime * bodyRotationLerpSpeed);

        // 4. Combine the smooth body rotation with the local mouse rotation
        Quaternion localLookRotation = Quaternion.Euler(mouseY, mouseX, 0f);
        transform.rotation = currentBodyRotation * localLookRotation;
    }
}