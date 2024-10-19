using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor.Sprites;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public delegate void handleMessage(NetworkMessage message);
    public static List<PlayerData> ServerPlayers;
    public string playerID;
    public string username;
    public TileStream tileStream;
    public UdpClient udpClient;
    public IPEndPoint remoteEP;
    public bool verified = false;
    ServerController serverController;

    Thread udpThread, tcpThread;

    public BlockingCollection<NetworkMessage> serverPipeOut;

    public static void SpawnNewPlayer(TileStream clientStream)
    {
        var newPlayer = Instantiate(Resources.Load("TemplatePlayerPref"), GameObject.FindGameObjectWithTag("PlayerContainer").transform);
        newPlayer.GetComponent<PlayerData>().InitializePlayerData(clientStream);
    }

    public void InitializePlayerData(TileStream client)
    {        
        serverController = GameObject.FindWithTag("ServerController").GetComponent<ServerController>();

        tileStream = client;
        serverPipeOut = new BlockingCollection<NetworkMessage>();

        var verification = new Thread(() => HandleVerification());
        verification.Start();

        tcpThread = new Thread(() => TCPThread(HandleIncomingClientMessage));
    }

    private void HandleIncomingClientMessage(NetworkMessage packet)
    {
        var type = packet.messageType;

        switch (type) {
            case "GetAssets":
                GetAssets(packet.message);
                break;
            case "KillMe":
                HandleKillSwitch();
                break;
            case "UpdateTransform":
                UpdatePlayerPosAndRot(packet.message);

                var newPacket = new NetworkMessage(type, ConcatByteArrays(Encoding.UTF8.GetBytes($"{playerID}ENDPID"), packet.message));

                HiveServerEvents.callGlobalPlayerMessage(this, newPacket);
                break;
            default:
                break;
        }
    }

    static byte[] ConcatByteArrays(params byte[][] arrays)
    {
        // Calculate the total length of all arrays
        int totalLength = 0;
        foreach (byte[] array in arrays)
        {
            totalLength += array.Length;
        }

        // Create a new byte array to hold all concatenated arrays
        byte[] result = new byte[totalLength];

        // Copy each array into the result array
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }

        return result;
    }

    private void HandleKillSwitch()
    {
        Debug.Log($"Killing connection with {playerID}");
        Destroy(gameObject);
    }

    private void HandleVerification()
    {
        playerID = tileStream.GetStringFromStream();

        if (!VerifyPlayer())
        {
            //serverController.KillPlayer(tileStream);
            Debug.Log("Client rejected");
            return;
        }

        tileStream.SendStringToStream("ACK");

        username = playerID;

        var osType = tileStream.GetStringFromStream();
        GetAssets(Encoding.UTF8.GetBytes(osType));

        tcpThread.Start();
    }

    private void Start()
    {
        HiveServerEvents.OnGlobalPlayerMessage += HandleGlobalMessage;
    }

    private void HandleGlobalMessage(PlayerData originator, NetworkMessage msg)
    {
        if(originator != this)
            serverPipeOut.Add(msg);
    }

    private void OnDestroy()
    {
        tcpThread.Abort();
        tileStream.Close();
        serverPipeOut.Dispose();
        HiveServerEvents.OnGlobalPlayerMessage -= HandleGlobalMessage;
    }

    private void OnDisable()
    {
        tcpThread.Abort();
        tileStream.Close();
        serverPipeOut.Dispose();
        HiveServerEvents.OnGlobalPlayerMessage -= HandleGlobalMessage;
    }

    void UpdatePlayerPosAndRot(byte[] transformInfo)
    {
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

    void TCPThread(handleMessage handleFunc)
    {

        while (true)
        {
            while(serverPipeOut.TryTake(out NetworkMessage newObject))
            {
                tileStream.SendStringToStream(newObject.messageType);
                tileStream.SendBytesToStream(newObject.message);
            }

            while(tileStream.Available > 0)
            {
                string messageType = tileStream.GetStringFromStream();

                byte[] message = tileStream.GetBytesFromStream();

                NetworkMessage netMessage = new NetworkMessage(messageType, message);
                ServerController.mainThreadContext.Post(_ => handleFunc(netMessage), null);
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

    private void GetAssets(byte[] msg)
    {
        var typeOS = Encoding.UTF8.GetString(msg);
        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles";

        //string typeOS = CoreCommunication.GetStringFromStream(client);

        if (!(typeOS.Equals("w") || typeOS.Equals("m") || typeOS.Equals("l")))
        {
            Debug.Log("Client sent invalid OS type.");
            return;
        }

        string[] fileNames = Directory.GetFiles(assetBundleDirectoryPath + "/" + typeOS);

        //client.Write(Encoding.ASCII.GetBytes(fileNames.Length.ToString() + (char) 0x00));

        tileStream.SendBytesToStream(BitConverter.GetBytes(fileNames.Length));

        foreach (string fileName in fileNames)
        {

            tileStream.SendStringToStream(new System.IO.FileInfo(fileName).Name);
            //client.SendBytesToStream(BitConverter.GetBytes(new System.IO.FileInfo(fileName).Length));

            //client.Write(Encoding.ASCII.GetBytes(new System.IO.FileInfo(fileName).Name + (char) 0x00));
            //client.Write(Encoding.ASCII.GetBytes(((int) new System.IO.FileInfo(fileName).Length).ToString() + (char) 0x00));

            //SendFile() had issues with files above ~16 KB in size on macOS, so this solution was implemented instead.
            /*byte[] buffer = new byte[8192]; // 8 KB buffer size
            int bytesRead;

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    client.Write(buffer, 0, bytesRead);*/

            byte[] buffer = new byte[new System.IO.FileInfo(fileName).Length];

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                fileStream.Read(buffer);

            tileStream.SendBytesToStream(buffer);
        }
    }
}
