using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private Behaviour[] _componentsToDisable;
    private bool[] _componentsEnabled; // 保存组件启用现场

    private Dictionary<Collider, bool> _colliderMap = new Dictionary<Collider, bool>(); // 存放碰撞体信息

    private NetworkVariable<int> _currentHealth = new();
    private NetworkVariable<bool> _isDead = new();

    public override void OnNetworkSpawn()
    {
        // 订阅变量改变事件，这样服务器和客户端都能同步表现
        _isDead.OnValueChanged += OnDeathStateChanged;
        Setup();
    }

    public override void OnNetworkDespawn()
    {
        _isDead.OnValueChanged -= OnDeathStateChanged;
    }

    public void Setup()
    {
        _componentsEnabled = new bool[_componentsToDisable.Length];
        for (int i = 0; i < _componentsToDisable.Length; i++)
        {
            _componentsEnabled[i] = _componentsToDisable[i].enabled;
        }
        _colliderMap.Clear();
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach(var collider in colliders)
        {
            _colliderMap.Add(collider, collider.enabled);
        }
        SetDefault();
    }

    private void SetDefault() // 玩家重生时的状态
    {
        for (int i = 0; i < _componentsEnabled.Length; i++)
        {
            _componentsToDisable[i].enabled = _componentsEnabled[i];
        }

        foreach(var item in _colliderMap)
        {
            item.Key.enabled = item.Value;
        }

        GetComponent<Rigidbody>().useGravity = true;
        if (IsServer)
        {
            _currentHealth.Value = _maxHealth;
            _isDead.Value = false;
        }
    }

    public bool IsDead()
    {
        return _isDead.Value;
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || _isDead.Value) return;

        _currentHealth.Value -= damage;

        if (_currentHealth.Value <= 0)
        {
            _currentHealth.Value = 0;
            _isDead.Value = true;
        }
    }

    // 当 _isDead 变量在服务器改变时，所有客户端都会执行此方法
    private void OnDeathStateChanged(bool oldVal, bool newVal)
    {
        if (newVal) // 变成死亡状态
        {
            PerformDie();
        }
        else // 变成存活状态（重生）
        {
            SetDefault();
        }
    }

    private void PerformDie()
    {
        GetComponentInChildren<Animator>().SetInteger("direction", -1);
        GetComponent<Rigidbody>().useGravity = false;
        // 执行死亡表现
        foreach (var comp in _componentsToDisable) comp.enabled = false;
        foreach (var col in _colliderMap.Keys) col.enabled = false;

        if (IsServer)
        {
            StartCoroutine(Respawn());
        }
    }

    private IEnumerator Respawn() // 重生，服务器端执行
    {
        yield return new WaitForSeconds(GameManager.Singleton.matchingSettings.respawnTime);
        // 只在服务器上修改位置
        GetComponentInChildren<Animator>().SetInteger("direction", 0);
        //GetComponent<Rigidbody>().useGravity = true;

        Vector3 spawnPos = new Vector3(0f, 10f, 0f); // 从天上掉下来

        // 通知该玩家的客户端自己去改坐标
        UpdatePositionClientRpc(spawnPos);

        transform.position = spawnPos;

        yield return new WaitForFixedUpdate(); // 等待一个物理帧，确保位置更新
        _isDead.Value = false;
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPos)
    {
        // 只有该对象的拥有者需要手动执行位移，以更新其权威坐标
        if (IsLocalPlayer)
        {
            transform.position = newPos;
        }
    }

    public int GetHealth()
    {
        return _currentHealth.Value;
    }
}
