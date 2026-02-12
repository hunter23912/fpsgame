using UnityEngine;
using TMPro;
using System;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Singleton;

    private Player _player;
    [SerializeField] private TextMeshProUGUI _bulletsText;
    [SerializeField] private GameObject _bulletsObject;

    private WeaponManager _weaponManager;

    [SerializeField] private Transform _healthBarFill;
    [SerializeField] private GameObject _healthBarObject;

    public void Awake()
    {
        Singleton = this;
    }

    public void SetPlayer(Player localPlayer)
    {
        _player = localPlayer;
        _weaponManager = _player.GetComponent<WeaponManager>();
        _bulletsObject.SetActive(true);
        _healthBarObject.SetActive(true);
    }

    void Update()
    {
        if (_player == null) return;

        var currentWeapon = _weaponManager.GetCurrentWeapon();
        if (currentWeapon._isReloading)
        {
            _bulletsText.text = "Reloading...";
        }
        else
        {
            _bulletsText.text = $"Bullets: {currentWeapon._bullets}/{currentWeapon._maxBullets}";
        }
        _healthBarFill.localScale = new Vector3(_player.GetHealth() / 100f, 1f, 1f);
    }
}
