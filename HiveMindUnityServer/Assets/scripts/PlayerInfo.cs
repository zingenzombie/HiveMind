using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public string playerID;
    string privatePlayerID;
    public string username = "Unnamed";

    RSACryptoServiceProvider rsa;

    AssetBundle avatar;

    private void Start()
    {

        rsa = new RSACryptoServiceProvider();

        if (!Directory.Exists("PlayerInfo"))
            Directory.CreateDirectory("PlayerInfo");

        if (!File.Exists("PlayerInfo/PlayerInfo.txt"))
            GeneratePlayerKeys();

        rsa.FromXmlString(File.ReadAllText("PlayerInfo/PlayerInfo.txt"));

        playerID = rsa.ToXmlString(false);
        Debug.Log(playerID);

        this.GetComponent<PlayerDebug>().name = username;
    }

    private void GeneratePlayerKeys()
    {
        //File.Create("PlayerInfo/PlayerInfo.txt");
        //File.WriteAllText("PlayerInfo/PlayerInfo.txt", rsa.ToXmlString(true));

        FileStream fs = File.Create("PlayerInfo/PlayerInfo.txt");

        fs.Write(Encoding.UTF8.GetBytes(rsa.ToXmlString(true)));
        fs.Close();
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
