using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading.Tasks;

public class ServerController : MonoBehaviour
{

    public IPAddress ipAddress;
    public int port;


    // Start is called before the first frame update
    private void Start()
    {
        ipAddress = IPAddress.Parse("127.0.0.1");
        ClientConnectListener();
    }

    void ClientConnectListener()
    {
        StartCoroutine(ListenForClients());
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
