using UnityEngine;
using TMPro;

public class PlayerInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _playerName;
    [SerializeField] private Transform _playerHealth;
    [SerializeField] private Transform _infoUI;
    private float _baseDist = 4f; // 基础距离，用于缩放UI

    private Player _player;
    private Vector3 _initialScale; // 初始缩放值
    private void Start()
    {
        _player = GetComponent<Player>();
        _initialScale = _infoUI.localScale;
    }

    void Update()
    {
        _playerName.text = transform.name;
        _playerHealth.localScale = new Vector3(_player.GetHealth() / 100f, 1f, 1f);

        var camera = Camera.main;
        if (camera == null) return;

        _infoUI.transform.LookAt(
            _infoUI.transform.position + camera.transform.rotation * Vector3.forward, 
            camera.transform.rotation * Vector3.up
        );

        float dist = Vector3.Distance(_infoUI.position, camera.transform.position);
        float rawScaleFactor = dist / _baseDist;
        float smoothedFactor = Mathf.Lerp(1.0f, rawScaleFactor, 0.5f); // 使用 Lerp 平滑：1.0f 是基准比例，0.5f 是平滑系数（越小缩放越慢）
        _infoUI.localScale = _initialScale * smoothedFactor;
    }
}
