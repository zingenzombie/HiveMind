using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public string playerID;
    public string privatePlayerID;
    public string username = "Unnamed";

    RSACryptoServiceProvider rsa;

    AssetBundle avatar;

    public PlayerInfo()
    {
        rsa = new RSACryptoServiceProvider();
        playerID = rsa.ToXmlString(true);

        if (!Directory.Exists("PlayerInfo"))
            Directory.CreateDirectory("PlayerInfo");

        if (!File.Exists("PlayerInfo/PlayerInfo.txt"))
            GeneratePlayerKeys();

        rsa.FromXmlString(File.ReadAllText("PlayerInfo/PlayerInfo.txt"));
    }

    private void Start()
    {
        this.GetComponent<PlayerDebug>().name = username;
        playerID = rsa.ToXmlString(true);
    }

    private void GeneratePlayerKeys()
    {
        File.Create("PlayerInfo/PlayerInfo.txt");

        File.WriteAllText("PlayerInfo/PlayerInfo.txt", rsa.ToXmlString(true));
    }

    public string GetPlayerPublicRSA()
    {
        return rsa.ToXmlString(false);
    }

    public byte[] VerifyPlayer(byte[] payload)
    {
        return rsa.Decrypt(payload, false);
    }

}
