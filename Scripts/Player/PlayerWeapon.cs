using System;
using UnityEngine;

[Serializable]
public class PlayerWeapon
{
    public string _name = "M4";
    public int _damage = 10;
    public float _range = 100f; // 射击距离

    public float _shootRate = 10f; // 每秒10发
    public float _shootCoolDownTime = 0.25f; // 射击后冷却时间，仅限单发
    public float _recoilForce = 2f; // 后坐力

    public int _maxBullets = 30; // 弹匣容量
    public int _bullets = 30; // 当前弹匣子弹数
    public float _reloadTime = 1.5f; // 换弹时间

    [HideInInspector] public Coroutine _reloadCoroutine; // 装弹协程
    [HideInInspector] public bool _isReloading = false; // 隐藏该变量，不在Inspector面板显示

    public GameObject _graphics;
}
