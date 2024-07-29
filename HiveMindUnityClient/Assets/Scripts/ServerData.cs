using System.Net;
public class ServerData
{
    public bool RequestTile;
    public int X;
    public int Y;
    public string Name;
    public string Ip;
    public int Port;
    public string OwnerID;
    public string PublicKey;
    public string LastContact;

    public ServerData(bool requestTile, int x, int y, string name, string ip, int port, string ownerID)
    {
        this.RequestTile = requestTile;
        this.X = x;
        this.Y = y;
        this.Name = name;
        this.Ip = ip;
        this.Port = port;
        this.OwnerID = ownerID;

        LastContact = "";
    }
}