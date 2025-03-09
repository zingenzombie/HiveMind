using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
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

        public string GetServerJsonData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    
    //Returns 1 if successfully creates a new server and 0 if not.
    public bool CreateServer(string JSONObject, SslStream client)
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
        
        //CoreCommunication.SendStringToStream(client, "Parsing data was a success");
        //CoreCommunication.SendStringToStream(client, "Setting tile up...");
        
        Console.Write($"New Server: {newServerData.Name} \n");
        if (newServerData.requestTile)
        {
            //CoreCommunication.SendStringToStream(client, $"You are requesting a tile @ {newServerData.X},{newServerData.Y}...");
            Console.Write($"        is requesting a tile at ({newServerData.X},{newServerData.Y})...");
            AssignTile(newServerData.X, newServerData.Y, newServerData, client);
        }
        else
        {
            //CoreCommunication.SendStringToStream(client, "You are asking for a random tile location... ");
            Console.Write($"        Server is requesting any available tile...");
        }

        return true;
    }

    public bool AssignTile(int X, int Y, ServerData newServerData, SslStream client)
    {
        
        //These four lines are a debug patch and should be removed. See below comment.
            servers[new Key(X, Y)] = newServerData;
            Console.WriteLine("GRANTED");
            CoreCommunication.SendStringToStream(client, $"   GRANTED @ ({X},{Y})");
            return true;
        
        //This is the original function that should be used in prod to prevent new servers from overriding existing ones. It's temporarily replaced for testing.
        /*
        if (servers.TryAdd(new Key(X, Y), newServerData))
        {
            Console.WriteLine("GRANTED");
            CoreCommunication.SendStringToStream(client, $"   GRANTED @ ({X},{Y})");
            return true;
        }*/
        
        
        Console.Write("FAILED");
        CoreCommunication.SendStringToStream(client, $"   FAILED @ ({X},{Y})");
        return false;
    }

    public void FindAvailableTile(ServerData newServerData)
    {
        
    }

    public void DeleteServer(int x, int y)
    {
        Console.Write($"Removing Server at Tile ({x},{y})...");
        if (servers.TryRemove(new KeyValuePair<Key, ServerData>(new Key(x, y), servers[new Key(x, y)])))
        {
            Console.WriteLine(" SUCCESS");
            return;
        }

        Console.WriteLine("FAILED, server does not exist at that tile location");
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

    public void PrintServers()
    {
        if(servers.IsEmpty)
            Console.WriteLine("The server is empty :(");
        foreach (var VARIABLE in servers)
        {
            var val = VARIABLE.Value;
            Console.WriteLine($"Server: {val.OwnerID} ({val.Name}) is at {val.X}:{val.Y}");
        }
    }
    
    
}