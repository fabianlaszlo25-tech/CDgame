using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GridCrawlerController : MonoBehaviour
{
    [Header("Dependencies")]
    public GridManager gridManager;
    public SmoothCameraFollow cameraScript; // Drag your Main Camera here!

    [Header("Movement Settings")]
    public float moveDuration = 0.3f;
    public float rotateDuration = 0.25f;
    public float bumpDuration = 0.2f;
    public float bumpDistance = 0.3f;
    public float postActionCooldown = 0.05f;

    [Header("Crosshair & UI Settings")]
    public Image crosshairImage;
    public Sprite defaultCrosshair;
    public Sprite interactCrosshair;
    public float interactReach = 3f;

    private bool isActing = false;
    private InputAction moveAction;

    void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
    }

    void Start()
    {
        // Instantly snap to the closest grid node when the game starts
        if (gridManager != null)
        {
            transform.position = gridManager.GetClosestNodePosition(transform.position);
        }
    }

    void OnEnable() => moveAction.Enable();
    void OnDisable() => moveAction.Disable();

    void Update()
    {
        HandleCrosshair();

        if (isActing) return;
        HandleInput();
    }

    private void HandleInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        if (input.sqrMagnitude > 0)
        {
            // W / Up Arrow -> Move Forward (or bump)
            if (input.y > 0.5f)
            {
                Vector3 forwardDir = transform.forward;
                Vector3? targetPos = gridManager.GetValidMovePosition(transform.position, forwardDir);
                StartCoroutine(PerformMove(forwardDir, targetPos));
            }
            // S / Down Arrow -> Turn 180 Around
            else if (input.y < -0.5f)
            {
                StartCoroutine(PerformTurn(180f));
            }
            // D / Right Arrow -> Turn 90 Right
            else if (input.x > 0.5f)
            {
                StartCoroutine(PerformTurn(90f));
            }
            // A / Left Arrow -> Turn 90 Left
            else if (input.x < -0.5f)
            {
                StartCoroutine(PerformTurn(-90f));
            }
        }
    }

    private IEnumerator PerformMove(Vector3 targetDirection, Vector3? targetPos)
    {
        isActing = true;
        // Notice: isAutoCentering has been removed from here so you can look around while moving/bumping

        if (targetPos.HasValue) // Valid space, move forward
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                transform.position = Vector3.Lerp(startPos, targetPos.Value, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos.Value;
        }
        else // Invalid space, play bump animation
        {
            Vector3 startPos = transform.position;
            Vector3 bumpPos = startPos + (targetDirection * bumpDistance);
            float elapsed = 0f;
            float halfBump = bumpDuration / 2f;

            while (elapsed < halfBump) // Forward part of bump
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfBump);
                transform.position = Vector3.Lerp(startPos, bumpPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfBump) // Backward part of bump
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfBump);
                transform.position = Vector3.Lerp(bumpPos, startPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = startPos;
        }

        yield return new WaitForSeconds(postActionCooldown);
        isActing = false;
    }

    private IEnumerator PerformTurn(float angle)
    {
        isActing = true;
        if (cameraScript != null) cameraScript.isAutoCentering = true; // Center camera on turns

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0f, angle, 0f);
        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / rotateDuration);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRot;

        if (cameraScript != null) cameraScript.isAutoCentering = false; // Release camera
        yield return new WaitForSeconds(postActionCooldown);
        isActing = false;
    }

    private void HandleCrosshair()
    {
        if (crosshairImage == null) return;

        // Use the camera's actual forward direction so the crosshair follows mouse-look
        Transform rayOrigin = cameraScript != null ? cameraScript.transform : transform;
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactReach))
        {
            if (hit.collider.CompareTag("HideHair")) crosshairImage.enabled = false;
            else if (hit.collider.CompareTag("Interactable"))
            {
                crosshairImage.enabled = true;
                crosshairImage.sprite = interactCrosshair;
            }
            else
            {
                crosshairImage.enabled = true;
                crosshairImage.sprite = defaultCrosshair;
            }
        }
        else
        {
            crosshairImage.enabled = true;
            crosshairImage.sprite = defaultCrosshair;
        }
    }
}