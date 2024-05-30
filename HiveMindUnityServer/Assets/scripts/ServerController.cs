using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System.Linq;

public class ServerController : MonoBehaviour
{

    public IPAddress coreAddress;
    public int corePort;

    public bool requestTile;
    public IPAddress ipAddress;
    public int port;
    public int x, y;
    public string serverName;
    public string ownerID;


    // Start is called before the first frame update
    private void Start()
    {
        ipAddress = IPAddress.Parse("127.0.0.1");

        //Delete this later and fix
        coreAddress = IPAddress.Parse("127.0.0.1");

        ContactCore();

        ClientConnectListener();
    }

    void ClientConnectListener()
    {
        StartCoroutine(ListenForClients());
    }

    void ContactCore()
    {
        ServerData tmp = new ServerData(requestTile, x, y, serverName, ipAddress.ToString(), port, ownerID);
        string jsonString = JsonUtility.ToJson(tmp) + '\n';
        byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

        using TcpClient tcpClient = new TcpClient(coreAddress.ToString(), corePort);

        if (!tcpClient.Connected)
        {
            Console.WriteLine("Failed to connect to core!");
            return;
        }

        byte[] buffer = new byte[6];
        buffer[0] = (byte)'s';
        buffer[1] = (byte)'e';
        buffer[2] = (byte)'r';
        buffer[3] = (byte)'v';
        buffer[4] = (byte)'e';
        buffer[5] = (byte)'r';
        tcpClient.GetStream().Write(buffer, 0, buffer.Length);

        buffer = Encoding.ASCII.GetBytes("newServer\n");
        tcpClient.GetStream().Write(buffer, 0, buffer.Length);

        tcpClient.GetStream().Write(bytes, 0, bytes.Length);
        Debug.Log("Wrote " + bytes.Length + " bytes.");

        foreach(var character in bytes)
            Console.WriteLine((char)character);

    }

    IEnumerator ListenForClients()
    {
        TcpListener server = new TcpListener(IPAddress.Any, port);
        // we set our IP address as server's address, and we also set the port

        server.Start();  // this will start the server

        while (true)   //we wait for a connection
        {
            TcpClient client;
            if (server.Pending())
            {
                client = server.AcceptTcpClient();  //if a connection exists, the server will accept it

                if (client.Connected)  //while the client is connected, we look for incoming messages
                {
                    StartCoroutine(HandleNewClient(client));
                    yield return null;
                }
                else
                {
                    Debug.Log("Error! TCP client connection attempt received, but connection failed before handling!");
                }
            }
            yield return null;
        }
    }

    IEnumerator HandleNewClient(TcpClient client)
    {
        while (true)
        {
            if (!client.Connected)
                yield break;

            if (client.Available > 0)
            {
                byte[] buffer = new byte[client.Available];

                client.GetStream().Read(buffer, 0, client.Available);

                Debug.Log(System.Text.Encoding.UTF8.GetString(buffer));
            }
            yield return null;
        }
    }
}
