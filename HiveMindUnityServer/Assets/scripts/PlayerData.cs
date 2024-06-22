using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerData : MonoBehaviour
{
    public string ip;
    public string clientName;
    public TcpClient tcpClient;
    public UdpClient udpClient;
    public IPEndPoint remoteEP;

    Thread udpThread, tcpThread;

    public BlockingCollection<NetworkMessage> serverPipeIn, serverPipeOut;

    public void InitializePlayerData(TcpClient client)
    {
        ip = ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString();
        tcpClient = client;
        clientName = CoreCommunication.GetStringFromStream(client);
        
        byte[] buffer = Encoding.ASCII.GetBytes("ACK\n");
        tcpClient.GetStream().Write(buffer);

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
            if(serverPipeOut.TryTake(out NetworkMessage message))
            {
                switch (message.messageType)
                {
                    case "PlayerPos":
                        //Debug.Log(message.message.ToString());
                        UpdatePlayerPosAndRot(message);
                        break;
                    case "killMe":
                        Destroy(this.gameObject);
                        break;
                    default: break;
                }
            }

            yield return null;
        }
    }

    void UpdatePlayerPosAndRot(NetworkMessage message)
    {
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

            //This is an incredibly inefficient hack that should be replaced asap
            if (!CoreCommunication.IsConnected(tcpClient))
            {
                tcpClient.Close();

                HandleMessage(new NetworkMessage("killMe", new byte[0]));

                //Destroy(this.gameObject);
                break;
            }

            //Send outgoing TCP messages
            if (serverPipeIn.TryTake(out NetworkMessage newObject))
            {
                tcpClient.GetStream().Write(ASCIIEncoding.ASCII.GetBytes(newObject.messageType + '\n'));
                tcpClient.GetStream().Write(BitConverter.GetBytes(newObject.numBytes));
                tcpClient.GetStream().Write(newObject.message);
            }

            //Receive incoming TCP messages
            if (tcpClient.Available > 0)
            {
                string messageType = CoreCommunication.GetStringFromStream(tcpClient);

                byte[] tmpMessageLength = new byte[4];

                while (tcpClient.Available < 4) { }

                tcpClient.GetStream().Read(tmpMessageLength, 0, tmpMessageLength.Length);
                int messageLength = BitConverter.ToInt32(tmpMessageLength, 0);

                while (tcpClient.Available < messageLength) { }

                byte[] message = new byte[messageLength];
                tcpClient.GetStream().Read(message, 0, messageLength);

                NetworkMessage netMessage = new NetworkMessage(messageType, message);

                HandleMessage(netMessage);
            }
        }
    }

    void UDPThread()
    {

        UdpClient listener = new UdpClient(3622);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Parse(ip), 3622);

        while (true)
        {


            //var data = udpClient.Receive(ref remoteEP);

        }
    }

    private void HandleMessage(NetworkMessage message)
    {
        serverPipeOut.Add(message);
    }
}
