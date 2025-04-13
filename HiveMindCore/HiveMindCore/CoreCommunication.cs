using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HiveMindCore;

public static class CoreCommunication
{

    //Any calls to GetStringFromStream() MUST NOT be called
    //from the main thread to prevent hanging in the case of
    //a lagging \n character.

    public static void SendStringToStream(SslStream client, string payload)
    {
        SendBytesToStream(client, Encoding.ASCII.GetBytes(payload));
    }

    public static string GetStringFromStream(SslStream client)
    {
        return Encoding.ASCII.GetString(GetBytesFromStream(client));
    }

    public static void SendBytesToStream(SslStream client, byte[] payload)
    {

        // Send the length of the encrypted data
        byte[] lengthBytes = BitConverter.GetBytes(payload.Length);
        client.Write(lengthBytes, 0, lengthBytes.Length);

        // Send the encrypted data in chunks
        int chunkSize = 8192;
        for (int i = 0; i < payload.Length; i += chunkSize)
        {
            int size = Math.Min(chunkSize, payload.Length - i);
            client.Write(payload, i, size);
        }
    }

    public static byte[] GetBytesFromStream(SslStream client)
    {
        // Read the length of the encrypted data
        int length = GetIntFromStream(client);

        // Define chunk size and buffer
        int chunkSize = 8192;
        byte[] buffer = new byte[chunkSize];

        // Use a MemoryStream to store the received encrypted data
        using MemoryStream encryptedStream = new MemoryStream();
        int totalBytesRead = 0;
        while (totalBytesRead < length)
        {
            int bytesToRead = Math.Min(chunkSize, length - totalBytesRead);
            int bytesRead = client.Read(buffer, 0, bytesToRead);
            //if (bytesRead == 0)
            //  break; // Connection closed

            encryptedStream.Write(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
        }

        return encryptedStream.ToArray();
    }

    public static void SendIntToStream(SslStream stream, int payload)
    {
        stream.Write(BitConverter.GetBytes(payload));
    }

    public static int GetIntFromStream(SslStream stream)
    {
        return BitConverter.ToInt32(ReadExactly(stream, 4));
    }

    public static byte[] ReadExactly(SslStream stream, int length)
    {
        byte[] buffer = new byte[length];

        int offset = 0;
        while (offset < length)
        {
            int bytesRead = stream.Read(buffer, offset, length - offset);
            //if (bytesRead == 0)
            //  throw new IOException("Unexpected end of stream");
            offset += bytesRead;
        }

        return buffer;
    }

    public static SslStream EstablishSslStreamFromTcpAsClient(TcpClient client)
    {


        SslStream sslStream = new SslStream(
            client.GetStream(),
            false,
            new RemoteCertificateValidationCallback(ValidateServerCertificate),
            null
        );
        // The server name must match the name on the server certificate

        try
        {
            sslStream.AuthenticateAsClient("honeydragonproductions.com");
            return sslStream;
        }
        catch (AuthenticationException e)
        {
            Console.WriteLine("Exception: {0}", e.Message);
            if (e.InnerException != null)
            {
                Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
            }
            Console.WriteLine("Authentication failed - closing the connection.");
            client.Close();
            return null;
        }
    }

    public static SslStream EstablishSslStreamFromTcpAsServer(TcpClient client)
    {


        SslStream sslStream = new SslStream(
            client.GetStream(),
            false,
            new RemoteCertificateValidationCallback(ValidateServerCertificate),
            null
        );
        // The server name must match the name on the server certificate

        try
        {
            sslStream.AuthenticateAsServer(new X509Certificate());
            return sslStream;
        }
        catch (AuthenticationException e)
        {
            Console.WriteLine("Exception: {0}", e.Message);
            if (e.InnerException != null)
            {
                Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
            }
            Console.WriteLine("Authentication failed - closing the connection.");
            client.Close();
            return null;
        }
    }

    public static bool ValidateServerCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

        // Do not allow this client to communicate with unauthenticated servers.
        return false;
    }
}