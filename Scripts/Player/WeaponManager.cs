using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;

public class WeaponManager : NetworkBehaviour
{
    [SerializeField] private PlayerWeapon _primaryWeapon; // 主武器
    [SerializeField] private PlayerWeapon _secondaryWeapon; // 副武器
    [SerializeField] private Transform _weaponHolder;

    private PlayerWeapon _currentWeapon;
    private GameObject _currentWeaponInstance; // 武器贴图资源
    private WeaponGraphics _currentGraphics;
    private AudioSource _currentAudioSource;

    private InputSystem_Actions _inputActions;

    // 用于网络同步当前武器索引，0表示主武器，1表示副武器
    private NetworkVariable<int> _currentWeaponIndex = new NetworkVariable<int>(0);

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
        _inputActions.Player.SwitchWeapon.started += OnSwitchWeapon;
    }
    private void OnEnable() => _inputActions.Enable();
    private void OnDisable() => _inputActions.Disable();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _currentWeaponIndex.OnValueChanged += OnWeaponChanged;
        EquipWeapon(_primaryWeapon);
    }

    public override void OnDestroy()
    {
        _currentWeaponIndex.OnValueChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(int oldIndex, int newIndex)
    {
        EquipWeapon(newIndex == 0 ? _primaryWeapon : _secondaryWeapon);
    }

    public void EquipWeapon(PlayerWeapon weapon)
    {
        if (weapon == null || weapon._graphics == null || _weaponHolder == null)
        {
            Debug.LogError("WeaponManager: Missing weapon graphics or weapon holder!");
            return;
        }
        // 销毁当前持有的武器实例
        if(_currentWeaponInstance != null)
        {
            Destroy(_currentWeaponInstance);
            _currentWeaponInstance = null;
        }


        _currentWeapon = weapon;

        _currentWeaponInstance = Instantiate(
            _currentWeapon._graphics,
            _weaponHolder,
            false
        );
        _currentGraphics = _currentWeaponInstance.GetComponent<WeaponGraphics>();
        _currentAudioSource = _currentWeaponInstance.GetComponent<AudioSource>();

        if (IsLocalPlayer)
        {
            _currentAudioSource.spatialBlend = 0f; // 设置为2D音效
        }
    }

    public PlayerWeapon GetCurrentWeapon()
    {
        return _currentWeapon;
    }

    public WeaponGraphics GetCurrentWeaponGraphics()
    {
        return _currentGraphics;
    }

    public AudioSource GetCurrentAudioSource()
    {
        return _currentAudioSource;
    }

    private void OnSwitchWeapon(CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            RequestToggleWeaponServerRpc();
        }

    }

    [ServerRpc]
    private void RequestToggleWeaponServerRpc(ServerRpcParams rpcParams = default)
    {
        _currentWeaponIndex.Value = _currentWeaponIndex.Value == 0 ? 1 : 0;
    }

    public void Reload(PlayerWeapon playerWeapon)
    {
        if(playerWeapon._isReloading) return;
        playerWeapon._isReloading = true;
        print("Reload...");
        StartCoroutine(ReloadCoroutine(playerWeapon));
    }

    public IEnumerable<PlayerWeapon> GetAllWeapons()
    {
        if(_primaryWeapon != null) yield return _primaryWeapon;
        if(_secondaryWeapon != null) yield return _secondaryWeapon;
    }

    private IEnumerator ReloadCoroutine(PlayerWeapon playerWeapon)
    {
        yield return new WaitForSeconds(playerWeapon._reloadTime); // 等待若干时间再执行后面逻辑

        playerWeapon._bullets = playerWeapon._maxBullets;

        playerWeapon._isReloading = false;
    }
}
