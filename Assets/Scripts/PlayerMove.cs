using Unity.Netcode;
using UnityEngine;

public class PlayerMove : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jump = 2f;
    [SerializeField] private float gravity = 5f;

    [Header("Camera & Rotation")]
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float topClamp = 90.0f;
    [SerializeField] private float bottomClamp = -90.0f;

    [Header("Pickup & Throw")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float pickupRange = 3.5f;
    [SerializeField] private float holdSmoothSpeed = 15f;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private LayerMask pickupMask = ~0;

    private bool isGrounded;
    private Vector3 velocity;
    private float mouseX;
    private float mouseY;
    private float cinemachineTargetPitch;
    private float rotationVelocity;
    private CharacterController controller;
    private float currentSpeed;

    private Rigidbody heldRb;
    private Transform prevParent;
    private bool prevUseGravity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null && cameraTarget != null)
        {
            playerCamera = cameraTarget.GetComponentInChildren<Camera>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }

            if (holdPoint == null && cameraTarget != null)
            {
                GameObject hp = new GameObject("HoldPoint");
                hp.transform.SetParent(cameraTarget);
                hp.transform.localPosition = new Vector3(0f, -0.15f, 1.8f);
                holdPoint = hp.transform;
            }
        }
        else
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        GetInput();
        DoRotation();
        HandlePickupInput();
        UpdateHeldPosition();

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void GetInput()
    {
        float hor = Input.GetAxis("Horizontal");
        float ver = Input.GetAxis("Vertical");

        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }

        Vector3 move = transform.right * hor + transform.forward * ver;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jump * -2f * gravity);
        }
    }

    private void DoRotation()
    {
        cinemachineTargetPitch += mouseY * rotationSpeed * -1;
        rotationVelocity = mouseX * rotationSpeed;

        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, bottomClamp, topClamp);

        if (cameraTarget != null)
        {
            cameraTarget.localRotation = Quaternion.Euler(cinemachineTargetPitch, 0.0f, 0.0f);
        }

        transform.Rotate(Vector3.up * rotationVelocity);
    }

    private void HandlePickupInput()
    {
        if (Input.GetMouseButtonDown(0) && heldRb == null)
        {
            TryPickupObject();
        }

        if (Input.GetMouseButtonUp(0) && heldRb != null)
        {
            DropObject();
        }

        if (Input.GetMouseButtonDown(1) && heldRb != null)
        {
            ThrowObject();
        }
    }

    private void TryPickupObject()
    {
        Transform rayOrigin = playerCamera != null ? playerCamera.transform : cameraTarget;
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.yellow, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                heldRb = rb;
                prevParent = heldRb.transform.parent;
                prevUseGravity = heldRb.useGravity;

                heldRb.useGravity = false;
                heldRb.isKinematic = true;
                heldRb.transform.SetParent(holdPoint);
            }
        }
    }

    private void UpdateHeldPosition()
    {
        if (heldRb == null || holdPoint == null) return;

        heldRb.transform.localPosition = Vector3.Lerp(
            heldRb.transform.localPosition,
            Vector3.zero,
            holdSmoothSpeed * Time.deltaTime
        );
    }

    private void DropObject()
    {
        if (heldRb == null) return;

        heldRb.transform.SetParent(prevParent);
        heldRb.isKinematic = false;
        heldRb.useGravity = prevUseGravity;
        heldRb = null;
        prevParent = null;
    }

    private void ThrowObject()
    {
        if (heldRb == null) return;

        Transform dirSource = playerCamera != null ? playerCamera.transform : cameraTarget;
        Rigidbody rbToThrow = heldRb;
        DropObject();

        Vector3 dir = dirSource != null
            ? (dirSource.forward + Vector3.up * 0.15f).normalized
            : (transform.forward + Vector3.up * 0.15f).normalized;

        rbToThrow.AddForce(dir * throwForce, ForceMode.Impulse);
    }

    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}