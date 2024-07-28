using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle;

public class TileStream : TcpClient
{

    private RSACryptoServiceProvider serverRSA;

    private Aes aes;
    ICryptoTransform encryptor;
    ICryptoTransform decryptor;

    public TileStream(TcpClient tcpClient)
    {
        // Initialize the TileStream with the TcpClient's socket
        Client = tcpClient.Client;
    }

    public int ActivateStream(string publicKey)
    {
        serverRSA = new RSACryptoServiceProvider();

        serverRSA.FromXmlString(publicKey);

        aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        byte[] buffer = aes.Key;
        SendBytesToStreamRSA(buffer);
        buffer = aes.IV;
        SendBytesToStreamRSA(buffer);

        if (GetStringFromStream() != "ACK")
            return 1;

        return 0;
    }
    public void ActivateStream(RSACryptoServiceProvider localRSA)
    {
        serverRSA = localRSA;
        aes = Aes.Create();

        byte[] buffer = GetBytesFromStreamRSA();
        aes.Key = buffer;
        buffer = GetBytesFromStreamRSA();
        aes.IV = buffer;

        encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        SendStringToStream("ACK");
    }

    public void SendStringToStream(string payload)
    {
        SendBytesToStream(Encoding.ASCII.GetBytes(payload));
    }

    public string GetStringFromStream()
    {
        return Encoding.ASCII.GetString(GetBytesFromStream());
    }

    public void SendBytesToStream(byte[] payload)
    {
        //payload = remoteRSA.Encrypt(payload, false);
        using (MemoryStream ms = new MemoryStream())
        {
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(payload, 0, payload.Length);
                cs.FlushFinalBlock();
            }
            GetStream().Write(BitConverter.GetBytes(ms.ToArray().Length));
            GetStream().Write(ms.ToArray());
        }
    }

    public byte[] GetBytesFromStream()
    {

        byte[] payloadLength = new byte[4];

        while (Available < 4) { }

        GetStream().Read(payloadLength, 0, 4);

        int length = BitConverter.ToInt32(payloadLength);

        byte[] payload = new byte[length];

        while (Available < length) { }
        GetStream().Read(payload, 0, length);

        //return localRSA.Decrypt(payload, false);

        using (MemoryStream ms = new MemoryStream(payload))
        {
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    cs.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }

    void SendBytesToStreamRSA(byte[] payload)
    {

        payload = serverRSA.Encrypt(payload, false);
        GetStream().Write(BitConverter.GetBytes(payload.Length));

        GetStream().Write(payload);
    }

    byte[] GetBytesFromStreamRSA()
    {

        byte[] payloadLength = new byte[4];

        while (Available < 4) { }

        GetStream().Read(payloadLength, 0, 4);

        int length = BitConverter.ToInt32(payloadLength);

        byte[] payload = new byte[length];

        while (Available < length) { }
        GetStream().Read(payload, 0, length);

        return serverRSA.Decrypt(payload, false);
    }
}
