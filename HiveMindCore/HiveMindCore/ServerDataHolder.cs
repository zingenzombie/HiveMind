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
        public string PublicKey;
        [JsonInclude]
        public string LastContact;

        /*
        public ServerData(int x, int y, string name, string ip, int port, string ownerID)
        {
            this.X = x;
            this.Y = y;
            this.Name = name;
            this.Ip = ip;
            this.Port = port;
            this.OwnerID = ownerID;
            
            LastContact = DateTime.Now.ToString();
        }*/

        public string GetServerJsonData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    
    //Returns 1 if successfully creates a new server and 0 if not.
    public bool CreateServer(string JSONObject)
    {
        ServerData newServerData;
        try
        {
            newServerData = JsonConvert.DeserializeObject<ServerData>(JSONObject);
        }
        catch (Exception)
        {
            Console.WriteLine("ERROR: failed to represent server with given JSON.");
            return false;
        }

        if (newServerData == null)
            return false;

        if (servers.ContainsKey(new Key(newServerData.X, newServerData.Y)))
        {
            //**REMOVE THIS LINE AFTER TESTING!!!**
            //return true;
            
            
            Console.WriteLine("ERROR: Server already exists at " + newServerData.X + ", " + newServerData.Y + ".");
            return false;
        }

        if (servers.TryAdd(new Key(newServerData.X, newServerData.Y), newServerData))
        {
            Console.WriteLine("Server successfully created at " + newServerData.X + ", " + newServerData.Y + ".");
            return true;
        }
        
        Console.WriteLine("Server failed to be created at " + newServerData.X + ", " + newServerData.Y + ". This should never occur.");
        return false;
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

        return JsonConvert.SerializeObject(data);
    }

    //This function would only be use in user queries for servers by name.
    public string GetServerJsonData(string name)
    {

        IEnumerator enumerator = servers.GetEnumerator();

        while (enumerator.MoveNext())
            if (((ServerData)enumerator.Current).Name == name)
                return JsonConvert.SerializeObject((ServerData)enumerator.Current);
        
        throw new Exception("No server of name " + name + " exists.");
    }
    
    
}