using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jump = 2f;
    [SerializeField] private float gravity = 5f;
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float topClamp = 90.0f;
    [SerializeField] private float bottomClamp = -90.0f;
    private bool isGrounded;
    private Vector3 velocity;
    private float mouseX;
    private float mouseY;
    private float cinemachineTargetPitch;
    private float rotationVelocity;
    private CharacterController controller;
    float currentSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        GetInput();
        DoRotation();
        velocity.y += gravity * Time.deltaTime;controller.Move(velocity * Time.deltaTime);
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

    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
