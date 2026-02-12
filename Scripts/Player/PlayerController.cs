using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Camera _cameraPivot;
    [SerializeField] private float _cameraRotationTotal = 0f;
    [SerializeField] private float _cameraRotationLimit = 85f;

    private Vector3 _velocity = Vector3.zero; // 速度：每秒钟移动的距离
    private Vector3 _xRotation = Vector3.zero; // 旋转角色
    private Vector3 _yRotation = Vector3.zero; // 旋转视角
    private Vector3 _thrusterForce = Vector3.zero; // 向上推力
    private float _recoilForce = 0f;

    private float _epsilon = 0.01f;
    private Vector3 _lastFramePosition = Vector3.zero; // 上一帧位置
    private Animator _animator;
    private float _distToGround = 0f;
    private void Start()
    {
        _lastFramePosition = transform.position;
        _animator = GetComponentInChildren<Animator>();
        _distToGround = GetComponentInChildren<Collider>().bounds.extents.y; // 计算到地面的距离
    }

    //private float lastY = 0f;
    public void Move(Vector3 velocity)
    {
        _velocity = velocity;
    }

    public void Rotate(Vector3 yRotation, Vector3 xRotation)
    {
        _xRotation = xRotation;
        _yRotation = yRotation;
    }

    public void AddRecoilForce(float newRecoilForce)
    {
        _recoilForce += newRecoilForce;
    }

    public void Thrust(Vector3 thrustForce)
    {
        _rb.AddForce(thrustForce, ForceMode.Impulse);
    }

    private void PerformMovement()
    {
        Vector3 currentVelocity = _rb.linearVelocity; 
        Vector3 targetHorizontalVelocity = _velocity;
        _rb.linearVelocity = new Vector3(targetHorizontalVelocity.x, currentVelocity.y, targetHorizontalVelocity.z);

        if (_thrusterForce != Vector3.zero)
        {
            _rb.AddForce(_thrusterForce, ForceMode.Acceleration);
            _thrusterForce = Vector3.zero;
        }
    }

    private void PerformRotation()
    {
        if(_recoilForce < 0.1)
        {
            _recoilForce = 0f;
        }
        if(_yRotation != Vector3.zero || _recoilForce > 0) // 左右视角旋转
        {
            _rb.transform.Rotate(_yRotation + _rb.transform.up * Random.Range(-1.2f * _recoilForce, 1.2f * _recoilForce));
        }
        if (_xRotation != Vector3.zero || _recoilForce > 0) // 上下视角旋转
        {
            _cameraRotationTotal += _xRotation.x - _recoilForce;
            _cameraRotationTotal = Mathf.Clamp(_cameraRotationTotal, -_cameraRotationLimit, _cameraRotationLimit);
            _cameraPivot.transform.localEulerAngles = new Vector3(_cameraRotationTotal, 0f, 0f);
        }

        _recoilForce *= 0.5f; // 每帧衰减一半,模拟后坐力效果
    }

    private void PerformAnimation()
    {

        Vector3 deltaPosition = transform.position - _lastFramePosition;
        _lastFramePosition = transform.position;

        // 与单位向量的点积，计算在前后和左右方向上的分量
        float forward = Vector3.Dot(deltaPosition, transform.forward);
        float right = Vector3.Dot(deltaPosition, transform.right);

        int direction = 0;
        if (forward > _epsilon)
        {
            direction = 1; // 前，左前，右前
        }
        else if(forward < -_epsilon)
        {
            if(right > _epsilon) direction = 4; // 后右
            else if (right < -_epsilon) direction = 6; // 后左
            else direction = 5; // 后
        }
        else
        {
            if (right > _epsilon) direction = 3; // 右
            else if (right < -_epsilon) direction = 7; // 左
            else direction = 0; // 静止
        }

        // 如果处于空中状态
        if (!Physics.Raycast(transform.position, -Vector3.up, _distToGround + 0.5f))
        {
            direction = 8;
        }

        if (GetComponent<Player>().IsDead())
        {
            direction = -1;
        }
        _animator.SetInteger("direction", direction);
    }

    private void FixedUpdate() // 相比update，是均匀的时间间隔调用，更适合物理计算
    {
        if (IsLocalPlayer)
        {
            var player = GetComponent<Player>();
            if (player != null && player.IsDead()) return;

            PerformMovement();
            PerformRotation();
            PerformAnimation();
        }
    }
    private void Update()
    {
        if (!IsLocalPlayer)
        {
            PerformAnimation();
        }
        //if (Keyboard.current.kKey.wasPressedThisFrame)
        //{
        //    GetComponentInChildren<Animator>().SetInteger("direction", -1);
        //}
    }
}
