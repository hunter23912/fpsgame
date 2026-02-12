using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using TMPro;
public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button _refreshButton;
    [SerializeField] private Button _buildButton;

    [SerializeField] private Canvas _menuUI;
    [SerializeField] private GameObject _roomButtonPrefab;

    private List<Button> _rooms = new List<Button>();

    private string _serverIP = "113.44.43.227";

    private int _buildRoomPort = -1;
    
    void Start()
    {
        SetConfig();
        InitButtons();
        RefreshRoomList();
    }

    private void OnApplicationQuit()
    {
        if(_buildRoomPort != -1) // 是房主，则退出时自动移除房间
        {
            RemoveRoom();
        }    
    }

    private void SetConfig()
    {
        var args = Environment.GetCommandLineArgs();
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        ushort usePort = 7777;
        bool isDecicatedServer = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-launch-as-server") isDecicatedServer = true;
            if (args[i] == "-port" && i + 1 < args.Length) ushort.TryParse(args[i + 1], out usePort);
        }
        if (isDecicatedServer)
        {
            transport.SetConnectionData("0.0.0.0", usePort);
            var utp = transport as UnityTransport;
            if (utp != null)
            {
                utp.MaxPacketQueueSize = 2048; // 设置更大的数据包队列大小以处理更多的传入数据2048个
            }
            NetworkManager.Singleton.StartServer();
            Debug.Log($"服务器已启动，当前监听端口: {transport.ConnectionData.Port}"); // 添加这行
        }
    }

    private void InitButtons()
    {
        _refreshButton.onClick.AddListener(() =>
        {
            RefreshRoomList();
        });
        _buildButton.onClick.AddListener(() =>
        {
            BuildRoom();
        });
    }

    private void ConnectedToServer(string ip, ushort port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);
        NetworkManager.Singleton.StartClient();
        Debug.Log($"客户端已启动，连接到服务器 {ip}:{port}，客户端ID: {NetworkManager.Singleton.LocalClientId}");
        DestroyAllButtons();
    }

    private void RefreshRoomList()
    {
        StartCoroutine(RefreshRoomListRequest($"http://{_serverIP}:8085/fps/get_room_list/"));
    }

    IEnumerator RefreshRoomListRequest(string uri)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        yield return uwr.SendWebRequest();

        if(uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            var resp = JsonUtility.FromJson<GetRoomListResponse>(uwr.downloadHandler.text);
            foreach(var room in _rooms)
            {
                room.onClick.RemoveAllListeners();
                Destroy(room.gameObject);
            }
            _rooms.Clear();

            int k = 0;
            foreach(var room in resp.rooms)
            {
                GameObject buttonObj = Instantiate(_roomButtonPrefab, _menuUI.transform);
                buttonObj.transform.localPosition = new Vector3(0, 80 - k * 100, 0);
                Button button = buttonObj.GetComponent<Button>();
                button.GetComponentInChildren<TextMeshProUGUI>().text = room.name;

                // 为每个按钮捕获当前room.port
                ushort port = (ushort)room.port;
                button.onClick.AddListener(() =>
                {
                    ConnectedToServer(_serverIP,port);
                });
                _rooms.Add(button);
                print(room.name + " - " + room.port.ToString());
                k++;
            }
        }
    }

    private void BuildRoom()
    {
        StartCoroutine(BuildRoomRequest($"http://{_serverIP}:8085/fps/build_room/"));
    }

    IEnumerator BuildRoomRequest(string uri)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        yield return uwr.SendWebRequest();

        if(uwr.result != UnityWebRequest.Result.ConnectionError)
        {
            var resp = JsonUtility.FromJson<BuildRoomResponse>(uwr.downloadHandler.text);
            if(resp.error_message == "success")
            {
                Debug.Log("build成功返回了success");
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                ushort port = (ushort)resp.port;
                transport.SetConnectionData(_serverIP, port);
                _buildRoomPort = port;
                NetworkManager.Singleton.StartClient();
                DestroyAllButtons();
                Debug.Log("已经创建成功并启动了客户端");
            }
        }
    }

    private void RemoveRoom()
    {
        StartCoroutine(RemoveRoomRequest($"http://{_serverIP}:8085/fps/remove_room/?port={_buildRoomPort.ToString()}"));
    }

    IEnumerator RemoveRoomRequest(string uri)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.ConnectionError)
        {
            var resp = JsonUtility.FromJson<RemoveRoomResponse>(uwr.downloadHandler.text);
            if(resp.error_message == "success")
            {

            }
        }
    }

    private void DestroyAllButtons()
    {
        _refreshButton.onClick.RemoveAllListeners();
        _buildButton.onClick.RemoveAllListeners();

        Destroy(_refreshButton.gameObject);
        Destroy(_buildButton.gameObject);

        foreach (var room in _rooms)
        {
            room.onClick.RemoveAllListeners();
            Destroy(room.gameObject);
        }
    }
}
