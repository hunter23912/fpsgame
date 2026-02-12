using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;

public class PlayerShooting : NetworkBehaviour
{
    private const string PLAYER_TAG = "Player";

    //[SerializeField] private PlayerWeapon _gun;
    private WeaponManager _weaponManager;
    private PlayerWeapon _currentWeapon;
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _mask;
    private float _leftShootCoolDownTime = 0f; // 射击冷却时间计时器
    private int _autoShootCount = 0; // 连发计数器，用于调整后坐力

    private PlayerController _playerController;
    private InputSystem_Actions _actions;

    private Coroutine _shootingCoroutine; // 存储连发协程

    enum HitEffectMaterial
    {
        Metal,
        Stone,
    }

    private void Awake()
    {
        _actions = new InputSystem_Actions();
        // 订阅fire的按下和松开事件
        _actions.Player.Fire.started += OnFireStarted;
        _actions.Player.Fire.canceled += OnFireCanceled;

        _actions.Player.Reload.performed += OnReloadPerformed;
    }

    private void OnEnable()
    {
        _actions.Player.Fire.Enable();
        _actions.Player.Reload.Enable();
    }
    private void OnDisable()
    {
        _actions.Player.Fire.Disable();
        _actions.Player.Reload.Disable();
    }

    private void Start()
    {
        _camera = GetComponentInChildren<Camera>();
        _weaponManager = GetComponent<WeaponManager>();
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if(_leftShootCoolDownTime > 0f)
        {
            _leftShootCoolDownTime = Math.Max(_leftShootCoolDownTime - Time.deltaTime, 0);
        }
        if (IsLocalPlayer && Keyboard.current.kKey.wasPressedThisFrame)
        {
            ShootServerRpc(NetworkObjectId, 20);
        }
    }

    private void OnFireStarted(CallbackContext context)
    {
        _currentWeapon = _weaponManager.GetCurrentWeapon();
        if (_currentWeapon == null || _currentWeapon._isReloading) return;
        if (_currentWeapon._shootRate <= 0) // 单发武器
        {
            if(_leftShootCoolDownTime > 0f) return; // 冷却中，不能射击
            Shoot();
            _leftShootCoolDownTime = _currentWeapon._shootCoolDownTime;
        }
        else
        {
            if (_shootingCoroutine == null)
            {
                _autoShootCount = 0;
                _shootingCoroutine = StartCoroutine(AutoFireRoutine());
            }
        }
    }

    private void OnFireCanceled(CallbackContext context)
    {
        if(_shootingCoroutine != null)
        {
            StopCoroutine(_shootingCoroutine);
            _shootingCoroutine = null;
        }
    }

    private void OnReloadPerformed(CallbackContext context)
    {
        _currentWeapon = _weaponManager.GetCurrentWeapon();
        if (_currentWeapon == null || _currentWeapon._isReloading || _currentWeapon._bullets == _currentWeapon._maxBullets) return;
        _currentWeapon._reloadCoroutine = StartCoroutine(ReloadCoroutine(_currentWeapon));
    }


    private IEnumerator ReloadCoroutine(PlayerWeapon weapon)
    {
        weapon._isReloading = true;
        print("positive reload...!");
        yield return new WaitForSeconds(weapon._reloadTime);
        weapon._bullets = weapon._maxBullets;
        weapon._isReloading = false;
        weapon._reloadCoroutine = null;
        print($"reload success! {weapon._name}");
    }

    // 玩家死亡时及时清理连发协程，需要在外部中触发
    public void OnPlayerDeath()
    {
        if(_shootingCoroutine != null)
        {
            StopCoroutine(_shootingCoroutine); 
            _shootingCoroutine = null;
        }
        foreach(var weapon in _weaponManager.GetAllWeapons())
        {
            if(weapon._reloadCoroutine != null)
            {
                StopCoroutine(weapon._reloadCoroutine);
                weapon._isReloading = false;
                weapon._reloadCoroutine = null;
            }
        }
    }

    // 连发协程，控制射击节奏
    private IEnumerator AutoFireRoutine()
    {
        Shoot();
        // 等待射速间隔
        yield return new WaitForSeconds(1f /  _currentWeapon._shootRate);
        while (true)
        {
            Shoot();
            yield return new WaitForSeconds(1f / _currentWeapon._shootRate);
        }
    }

    #region OnHit 击中特效网络同步事件
    [ClientRpc]
    private void OnHitClientRpc(Vector3 pos, Vector3 normal, HitEffectMaterial material)
    {
        OnHit(pos, normal, material);
    }

    [ServerRpc]
    private void OnHitServerRpc(Vector3 pos, Vector3 normal, HitEffectMaterial material)
    {
        if (!IsHost) OnHit(pos, normal, material);
        OnHitClientRpc(pos, normal, material);
    }

    private void OnHit(Vector3 pos, Vector3 normal, HitEffectMaterial material) // 击中特效
    {
        GameObject hitEffectPrefab;
        if(material == HitEffectMaterial.Metal)
        {
            hitEffectPrefab = _weaponManager.GetCurrentWeaponGraphics()._metalHitEffectPrefab;
        }
        else
        {
            hitEffectPrefab = _weaponManager.GetCurrentWeaponGraphics()._stoneHitEffectPrefab;
        }
        GameObject hitEffectObject = Instantiate(hitEffectPrefab, pos, Quaternion.LookRotation(normal));
        ParticleSystem particleSystem = hitEffectObject.GetComponent<ParticleSystem>();
        particleSystem.Emit(1);
        particleSystem.Play();
        Destroy(hitEffectObject, particleSystem.main.duration * 5);
    }
    #endregion

    #region OnShoot 射击特效网络同步事件
    public void OnShoot(float recoilForce) // 每次射击执行的逻辑，包括特效、音效
    {
        _weaponManager.GetCurrentWeaponGraphics()._muzzleFlash.Play();
        _weaponManager.GetCurrentAudioSource().Play();

        if (IsLocalPlayer)
        {
            _playerController.AddRecoilForce(recoilForce);
        }
    }

    [ServerRpc]
    private void OnShootServerRpc(float recoilForce)
    {
        if(!IsHost) OnShoot(recoilForce);
        OnShootClientRpc(recoilForce);
    }

    [ClientRpc]
    private void OnShootClientRpc(float recoilForce)
    {
        OnShoot(recoilForce);
    }
    #endregion

    private void Shoot()
    {
        _currentWeapon = _weaponManager.GetCurrentWeapon();
        if (_currentWeapon._bullets <= 0 || _currentWeapon._isReloading) return;

        _currentWeapon._bullets--;

        if(_currentWeapon._bullets <= 0)
        {
            _weaponManager.Reload(_currentWeapon);
        }

        _autoShootCount++;
        float recoilForce = _currentWeapon._recoilForce;
        if (_autoShootCount <= 3)
        {
            recoilForce *= 0.2f;
        }
        OnShootServerRpc(recoilForce);


        // 摄像机的位置，正前方，也就是屏幕中心指向的物体
        if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _currentWeapon._range, _mask))
        {
            PlayerSetup playerSetup = hit.collider.GetComponentInParent<PlayerSetup>();
            var targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.CompareTag(PLAYER_TAG))
            {
                ShootServerRpc(targetNetObj.NetworkObjectId, _currentWeapon._damage);
                OnHitServerRpc(hit.point, hit.normal, HitEffectMaterial.Metal); // 击中玩家，渲染金属特效
            }
            else
            {
                OnHitServerRpc(hit.point, hit.normal, HitEffectMaterial.Stone); // 否则，渲染石头特效
            }
        }
    }

    [ServerRpc]// 从客户端向服务器发送射击信息
    private void ShootServerRpc(ulong networkId, int damage, ServerRpcParams rpcParams = default)
    {

        // 使用 Netcode 内置的查找表，效率高且绝对同步
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out var netObj))
        {
            var player = netObj.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
            else
            {
                Debug.LogError($"[Server] 找到物体 {networkId} 但没有 Player 组件！");
            }
        }
        else
        {
            Debug.LogWarning($"Server: Try to damage object {networkId}, but it was not found.");
        }
    }
}
