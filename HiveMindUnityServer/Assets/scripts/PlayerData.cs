using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PlayerData
{
    public string ip;
    public string name;
    public TcpClient tcpClient;
    public UdpClient udpClient;
    public IPEndPoint remoteEP;

    private Thread udpThread;

    public PlayerData(TcpClient client)
    {
        ip = ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString();
        tcpClient = client;
        name = CoreCommunication.GetStringFromStream(client);
        
        byte[] buffer = Encoding.ASCII.GetBytes("ACK\n");
        tcpClient.GetStream().Write(buffer);

        UdpClient udpClient = new UdpClient(3621);
        remoteEP = new IPEndPoint(IPAddress.Parse(ip), 0);

        udpThread = new Thread(() => UDPThread());
    }

    private void UDPThread()
    {

        while (true)
            handleUDPMessage();
    }

    private void handleUDPMessage()
    {
        var data = udpClient.Receive(ref remoteEP);

    }
}
