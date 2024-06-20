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

        udpThread = new Thread(() => UDPThread());
        udpThread.Start();
    }

    private void UDPThread()
    {

        UdpClient listener = new UdpClient(3622);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Parse(ip), 3622);

        while (true)
            handleUDPMessage();
    }

    private void handleUDPMessage()
    {
        var data = udpClient.Receive(ref remoteEP);

    }
}
