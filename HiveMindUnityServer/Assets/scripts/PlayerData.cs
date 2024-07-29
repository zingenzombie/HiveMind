using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
//using UnityEditor.PackageManager;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerData : MonoBehaviour
{
    public string playerID;
    public string username;
    public TileStream tileStream;
    public UdpClient udpClient;
    public IPEndPoint remoteEP;
    ServerController serverController;

    Thread udpThread, tcpThread;

    public BlockingCollection<NetworkMessage> serverPipeIn, serverPipeOut;

    public void InitializePlayerData(TileStream client)
    {        serverController = GameObject.FindWithTag("ServerController").GetComponent<ServerController>();

        tileStream = client;

        //Get playerID and verify player
        playerID = tileStream.GetStringFromStream();
        
        if (!VerifyPlayer())
        {
            serverController.KillPlayer(tileStream);
            Debug.Log("Client rejected");
            return;
        }

        Debug.Log("Client verified");

        tileStream.SendStringToStream("ACK");

        username = tileStream.GetStringFromStream();

        foreach (var player in serverController.players)
        {
            //This is temporary and must be switched out with a new playerdata object.

            NetworkMessage newObject = new NetworkMessage("newPlayer", ASCIIEncoding.ASCII.GetBytes(((GameObject) player).GetComponent<PlayerData>().playerID));

            tileStream.SendStringToStream(newObject.messageType);
            tileStream.SendBytesToStream(newObject.message);

            //This should send the other clients positions at time of joining
            /*
            NetworkMessage newObject = new NetworkMessage("newPlayer", ASCIIEncoding.ASCII.GetBytes(((GameObject)player).GetComponent<PlayerData>().playerID));

            tileStream.SendStringToStream(newObject.messageType);
            tileStream.SendBytesToStream(newObject.message);*/
        }

        serverPipeIn = new BlockingCollection<NetworkMessage>(); //Will need to be separated into udp and tcp pipes!
        serverPipeOut = new BlockingCollection<NetworkMessage>();

        StartCoroutine(HandleMessages());

        tcpThread = new Thread(() => TCPThread());
        tcpThread.Start();

        //udpThread = new Thread(() => UDPThread());
        //udpThread.Start();
    }

    IEnumerator HandleMessages()
    {
        while (true)
        {
            if (serverPipeIn.TryTake(out NetworkMessage message))
            {
                switch (message.messageType)
                {
                    case "PlayerPos":
                        Debug.Log(message.message.ToString());
                        UpdatePlayerPosAndRot(message);
                        break;
                    case "killMe":
                        serverController.KillPlayer(tileStream);
                        break;
                    default: break;
                }
            }
            yield return null;
        }
    }

    void UpdatePlayerPosAndRot(NetworkMessage message)
    {
        Debug.Log(message.message);
        //***CHECK THAT MESSAGE TIME IS NEWER THAN CURRENT UPDATE***

        float posX = BitConverter.ToSingle(message.message, 0);
        float posY = BitConverter.ToSingle(message.message, 4);
        float posZ = BitConverter.ToSingle(message.message, 8);

        float rotX = BitConverter.ToSingle(message.message, 12);
        float rotY = BitConverter.ToSingle(message.message, 16);
        float rotZ = BitConverter.ToSingle(message.message, 20);
        float rotW = BitConverter.ToSingle(message.message, 24);

        transform.SetPositionAndRotation(new Vector3(posX,posY,posZ), new Quaternion(rotX, rotY, rotZ, rotW));


    }

    void TCPThread()
    {

        while (true)
        {
            if (!tileStream.Connected) 
            {
                tileStream.Close();
                HandleMessage(new NetworkMessage("killMe", new byte[0]));

                break;
            }

            //Send outgoing TCP messages
            if (serverPipeOut.TryTake(out NetworkMessage newObject))
            {

                tileStream.SendStringToStream(newObject.messageType);
                tileStream.SendBytesToStream(newObject.message);
            }

            if(tileStream.Available > 0)
            {

                string messageType = tileStream.GetStringFromStream();
                byte[] message = tileStream.GetBytesFromStream();

                NetworkMessage netMessage = new NetworkMessage(messageType, message);

                HandleMessage(netMessage);

            }
        }
    }
    public static bool IsConnected(TileStream _tcpClient)
    {
        try
        {
            if (_tcpClient != null && _tcpClient.Client != null && _tcpClient.Client.Connected)
            {
                /* pear to the documentation on Poll:
                    * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                    * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                    * -or- true if data is available for reading; 
                    * -or- true if the connection has been closed, reset, or terminated; 
                    * otherwise, returns false
                    */

                // Detect if client disconnected
                if (_tcpClient.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (_tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /*
    void UDPThread()
    {

        UdpClient listener = new UdpClient(3622);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Parse(ip), 3622);

        while (true)
        {


            //var data = udpClient.Receive(ref remoteEP);

        }
    }*/

    private void HandleMessage(NetworkMessage message)
    {
        serverPipeIn.Add(message);
    }

    private bool VerifyPlayer()
    {

        //Generate challenge
        RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        byte[] challengeKey = new byte[100];
        rng.GetBytes(challengeKey);

        //Encrypt challenge with playerKey and send

        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(playerID);

        tileStream.SendBytesToStream(rsa.Encrypt(challengeKey, false));

        byte[] response = tileStream.GetBytesFromStream();

        for(int i = 0; i < challengeKey.Length; i++)
            if (challengeKey[i] != response[i])
                return false;

        return true;
    }
}
