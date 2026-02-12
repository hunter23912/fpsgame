using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _speed = 8f;
    [SerializeField] private float _lookSensitivity = 0.2f;
    [SerializeField] private float _thrusterForce = 30f;

    [Header("References")]
    [SerializeField] private PlayerController _controller;
    private float _distToGround = 0f;
    private InputSystem_Actions _inputActions;

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
    }

    private void OnEnable() => _inputActions.Enable();

    private void OnDisable() => _inputActions.Disable();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // 锁定并隐藏鼠标
        _distToGround = GetComponentInChildren<Collider>().bounds.extents.y; // 计算到地面的距离
    }

    private void HandleMovement()
    {
        Vector2 inputVector = _inputActions.Player.Move.ReadValue<Vector2>();
        // 计算移动方向： transform.right 对应 X 轴，transform.forward 对应 Y 轴
        Vector3 velocity = (transform.right * inputVector.x + transform.forward * inputVector.y).normalized * _speed;
        _controller.Move(velocity);
    }

    private void HandleRotation()
    {
        Vector2 lookDelta = _inputActions.Player.Look.ReadValue<Vector2>();
        Vector3 yRotation = new Vector3(0f, lookDelta.x, 0f) * _lookSensitivity; // 水平旋转
        Vector3 xRotation = new Vector3(-lookDelta.y, 0f, 0f) * _lookSensitivity; // 竖直旋转
        _controller.Rotate(yRotation, xRotation);
    }

    private void HandleJump()
    {
        if (_inputActions.Player.Jump.WasPressedThisFrame())
        {
            // 从玩家中心位置向下发射一条射线，检测是否接触地面
            if (Physics.Raycast(transform.position, -Vector3.up, _distToGround + 0.5f))
            {
                Vector3 force = Vector3.up * _thrusterForce;
                _controller.Thrust(force);
            }
        }
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleJump();
    }
}
