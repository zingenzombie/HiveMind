using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;

public static class CoreCommunication
{

    //Any calls to GetStringFromStream() MUST NOT be called
    //from the main thread to prevent hanging in the case of
    //a lagging \n character.

    public static void SendStringToStream(SslStream client, string payload)
    {
        byte[] buffer = new byte[payload.Length + 1];


        int i = 0;
        foreach (char c in payload)
            buffer[i++] = (byte)c;

        client.Write(buffer);
    }

    public static string GetStringFromStream(SslStream client)
    {
        string request = "";

        while (true)
        {
            int read;

            try
            {
                read = client.ReadByte();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return "";
            }

            if (read.Equals(-1))
                continue;

            if (read.Equals(0x00))
                return request;

            request += (char)read;
        }
    }

    //This is most-certainly broken
    public static byte[] GetBytesFromStream(SslStream sslStream, int numBytes)
    {
        // Read the  message sent by the client.
        // The client signals the end of the message using the

        byte[] fileBuffer = new byte[numBytes];

        int bytesRead = 0;
        while (bytesRead < numBytes)
        {
            int read = sslStream.Read(fileBuffer, bytesRead, numBytes - bytesRead);
            //BROKEN AND SHOULDN'T RELY ON IsConnected()
            if(read == 0 /*&& !CoreCommunication.IsConnected(sslStream)*/)
            //Handle stream closed or error connection
                break;

            bytesRead += read;
        }

        return fileBuffer;
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
            Debug.Log(e);
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
