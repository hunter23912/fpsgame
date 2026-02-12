using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class GetRoomListResponse
{
    public string error_message;
    public Room[] rooms;
}
