﻿using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace HiveMindCore;

public class ServerDataHolder
{
    public struct Key
    {
        public readonly int Dimension1;
        public readonly int Dimension2;
        public Key(int x, int y)
        {
            Dimension1 = x;
            Dimension2 = y;
        }
        // Equals and GetHashCode ommitted
    }

    public ConcurrentDictionary<Key, ServerData> servers = new ConcurrentDictionary<Key, ServerData>();
    
    public class ServerData
    {
        [JsonInclude] 
        public bool requestTile;
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

        public ServerData(int x, int y, string name, string ip, int port, string ownerID)
        {
            this.X = x;
            this.Y = y;
            this.Name = name;
            this.Ip = ip;
            this.Port = port;
            this.OwnerID = ownerID;
            
            LastContact = DateTime.Now.ToString();
        }
    }
    
    /*
    public void CreateServer(int x, int y, string name, IPAddress ip, int port, string ownerID)
    {
        servers.Add(new ServerData(x, y, name, ip.ToString(), port, ownerID));
    }
    */
    public void CreateServer(string JSONObject)
    {
        ServerData newServerData;
        try
        {
            newServerData = JsonConvert.DeserializeObject<ServerData>(JSONObject);
        }
        catch (Exception)
        {
            Console.WriteLine("ERROR: failed to represent server with given JSON.");
            return;
        }

        if (newServerData == null)
            return;
        
        servers.TryAdd(new Key(newServerData.X, newServerData.Y),newServerData);
    }

    public void DeleteServer(int x, int y)
    {
        servers.TryRemove(new KeyValuePair<Key, ServerData>(new Key(x, y), servers[new Key(x, y)]));
    }

    public string GetServerJsonData(int x, int y)
    {
        ServerData data;
        
        if(servers.TryGetValue(new Key(x, y), out data))
            throw new Exception("No server exists at " + x + ", " + y + ".");
        
    }

    public string GetServerJsonData(string name)
    {
        foreach(ServerData server in servers)
        {
            if (server.Name == name)
                return System.Text.Json.JsonSerializer.Serialize(server);
        }
        throw new Exception("No server of name " + name + " exists.");
    }
    
    
}