using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace HiveMindCore;

public class ServerDataHolder
{

    private ConcurrentBag<ServerData> servers = new ConcurrentBag<ServerData>();
    
    public class ServerData
    {
        [JsonInclude] 
        public bool OequestTile;
        [JsonInclude]
        public int X;
        [JsonInclude]
        public int Y;
        [JsonInclude]
        public string Name;
        [JsonInclude]
        public string Ip;
        [JsonInclude]
        public int Port;
        [JsonInclude]
        public string OwnerID;
        [JsonInclude]
        public string LastContact;

        public ServerData(int x, int y, string name, IPAddress ip, int port, string ownerID)
        {
            this.X = x;
            this.Y = y;
            this.Name = name;
            this.Ip = ip.ToString();
            this.Port = port;
            this.OwnerID = ownerID;
            
            LastContact = DateTime.Now.ToString();
        }
    }

    public void CreateServer(int x, int y, string name, IPAddress ip, int port, string ownerID)
    {
        servers.Add(new ServerData(x, y, name, ip, port, ownerID));
    }

    public string GetServerJsonData(int x, int y)
    {
        foreach(ServerData server in servers)
        {
            if (server.X == x && server.Y == y)
                return JsonSerializer.Serialize(server);
        }
        throw new Exception("No server exists at " + x + ", " + y + ".");
    }

    public string GetServerJsonData(string name)
    {
        foreach(ServerData server in servers)
        {
            if (server.Name == name)
                return JsonSerializer.Serialize(server);
        }
        throw new Exception("No server of name " + name + " exists.");
    }
    
    
}