using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public class ServerPlayer : MonoBehaviour
{
    public static Dictionary<string, ServerPlayer> gamePlayers;

    public string playerID;


    public static void makeNewPlayer(string PID)
    {
        var newplayer = Instantiate(Resources.Load("PlayerPref"));
        newplayer.GetComponent<ServerPlayer>().playerID = PID;
        gamePlayers.Add(PID, newplayer.GetComponent<ServerPlayer>());
    }

    public void UpdateTransform(byte[] transformInfo)
    {
        Debug.Log(Encoding.UTF8.GetString(transformInfo));
        //***CHECK THAT MESSAGE TIME IS NEWER THAN CURRENT UPDATE***

        float posX = BitConverter.ToSingle(transformInfo, 0);
        float posY = BitConverter.ToSingle(transformInfo, 4);
        float posZ = BitConverter.ToSingle(transformInfo, 8);

        float rotX = BitConverter.ToSingle(transformInfo, 12);
        float rotY = BitConverter.ToSingle(transformInfo, 16);
        float rotZ = BitConverter.ToSingle(transformInfo, 20);

        transform.position = new Vector3(posX, posY, posZ);
        transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
    }

}
