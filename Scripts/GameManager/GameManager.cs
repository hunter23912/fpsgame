using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singleton;
    [SerializeField] public MatchingSettings matchingSettings;

    private static Dictionary<string, Player> players = new Dictionary<string, Player>();

    private void Awake()
    {
        Singleton = this;
    }

    public void RegisterPlayer(string playerName, Player player)
    {
        player.transform.name = playerName;
        players.Add(playerName, player);
    }

    public void UnRegisterPlayer(string playerName)
    {
        players.Remove(playerName);
    }

    // 供外部访问玩家实例
    public Player GetPlayer(string playerName)
    {
        return players[playerName];
    }

}
