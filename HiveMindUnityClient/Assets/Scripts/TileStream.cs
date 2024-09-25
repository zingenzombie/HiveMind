using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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
        byte[] encryptedData;
        using (MemoryStream ms = new MemoryStream())
        {
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(payload, 0, payload.Length);
            }
            encryptedData = ms.ToArray();
        }

        // Send the length of the encrypted data
        byte[] lengthBytes = BitConverter.GetBytes(encryptedData.Length);
        GetStream().Write(lengthBytes, 0, lengthBytes.Length);

        // Send the encrypted data in chunks
        int chunkSize = 8192;
        for (int i = 0; i < encryptedData.Length; i += chunkSize)
        {
            int size = Math.Min(chunkSize, encryptedData.Length - i);
            GetStream().Write(encryptedData, i, size);
        }
    }

    public byte[] GetBytesFromStream()
    {
        // Read the length of the encrypted data
        byte[] lengthBytes = new byte[4];
        ReadExactly(GetStream(), lengthBytes, 4);
        int length = BitConverter.ToInt32(lengthBytes);

        // Define chunk size and buffer
        int chunkSize = 8192;
        byte[] buffer = new byte[chunkSize];

        // Use a MemoryStream to store the received encrypted data
        using (MemoryStream encryptedStream = new MemoryStream())
        {
            int totalBytesRead = 0;
            while (totalBytesRead < length)
            {
                int bytesToRead = Math.Min(chunkSize, length - totalBytesRead);
                int bytesRead = GetStream().Read(buffer, 0, bytesToRead);
                if (bytesRead == 0)
                    break; // Connection closed

                encryptedStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
            }

            // Decrypt and return the payload
            using (MemoryStream payloadStream = new MemoryStream(encryptedStream.ToArray()))
            using (CryptoStream cs = new CryptoStream(payloadStream, decryptor, CryptoStreamMode.Read))
            using (MemoryStream output = new MemoryStream())
            {
                cs.CopyTo(output);
                return output.ToArray();
            }
        }
    }

    static void ReadExactly(NetworkStream stream, byte[] buffer, int length)
    {
        int offset = 0;
        while (offset < length)
        {
            int bytesRead = stream.Read(buffer, offset, length - offset);
            if (bytesRead == 0)
                throw new IOException("Unexpected end of stream");
            offset += bytesRead;
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
