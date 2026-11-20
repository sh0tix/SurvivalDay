using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jump = 2f;
    [SerializeField] private float gravity = 5f;

    [Header("Camera & Rotation")]
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float topClamp = 90.0f;
    [SerializeField] private float bottomClamp = -90.0f;

    [Header("Pickup & Throw")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float pickupRange = 3.5f;
    [SerializeField] private float holdFollowSpeed = 14f;
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
    private bool prevUseGravity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (holdPoint == null && cameraTarget != null)
        {
            GameObject hp = new GameObject("HoldPoint");
            hp.transform.SetParent(cameraTarget);
            hp.transform.localPosition = new Vector3(0f, -0.15f, 1.8f);
            holdPoint = hp.transform;
        }
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        GetInput();
        DoRotation();
        HandlePickupInput();

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void FixedUpdate()
    {
        MoveHeldObject();
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

        cameraTarget.localRotation = Quaternion.Euler(cinemachineTargetPitch, 0.0f, 0.0f);

        transform.Rotate(Vector3.up * rotationVelocity);
    }

    private void HandlePickupInput()
    {
        // ЛКМ: взять или отпустить предмет
        if (Input.GetMouseButtonDown(0))
        {
            if (heldRb != null)
            {
                DropObject();
            }
            else
            {
                TryPickupObject();
            }
        }

        // ПКМ: кинуть предмет
        if (Input.GetMouseButtonDown(1) && heldRb != null)
        {
            ThrowObject();
        }
    }

    private void TryPickupObject()
    {
        if (cameraTarget == null) return;

        Ray ray = new Ray(cameraTarget.position, cameraTarget.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb != null && !rb.isKinematic)
            {
                heldRb = rb;
                prevUseGravity = heldRb.useGravity;
                heldRb.useGravity = false;
                heldRb.linearDamping = 10f;
                heldRb.angularDamping = 5f;
            }
        }
    }

    private void MoveHeldObject()
    {
        if (heldRb == null || holdPoint == null) return;

        if (Vector3.Distance(heldRb.position, holdPoint.position) > pickupRange * 1.35f)
        {
            DropObject();
            return;
        }

        Vector3 toTarget = holdPoint.position - heldRb.position;
        heldRb.linearVelocity = toTarget * holdFollowSpeed;
    }

    private void DropObject()
    {
        if (heldRb == null) return;

        heldRb.useGravity = prevUseGravity;
        heldRb.linearDamping = 0.05f;
        heldRb.angularDamping = 0.05f;
        heldRb = null;
    }

    private void ThrowObject()
    {
        if (heldRb == null || cameraTarget == null) return;

        Rigidbody rbToThrow = heldRb;
        DropObject();

        Vector3 throwDir = (cameraTarget.forward + Vector3.up * 0.15f).normalized;
        rbToThrow.AddForce(throwDir * throwForce, ForceMode.Impulse);
    }

    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}