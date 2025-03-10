using System.Net.Security;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace HiveMindCore;

public class ServerDataHolder
{
    const string ConnStr = "server=localhost;user=core;database=Hive;port=3306;password=1234";
    public ServerDataHolder()
    {
        using MySqlConnection mySqlConnection = new MySqlConnection(ConnStr);
        
        try
        {
            Console.WriteLine("Connecting to MySQL...");
            mySqlConnection.Open();

            string sql = "SELECT ip FROM servers";
            using MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Console.WriteLine(rdr[0]);
            }
            rdr.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw new Exception("Failed to establish connection with database!");
        }
    }
    public class ServerData
    {
        [JsonInclude] 
        public bool RequestTile;
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
        public string OwnerId;
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
    public bool CreateServer(string jsonObject, SslStream client)
    {
        ServerData newServerData;
        try
        {
            newServerData = JsonConvert.DeserializeObject<ServerData>(jsonObject);
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
        if (newServerData.RequestTile)
        {
            //CoreCommunication.SendStringToStream(client, $"You are requesting a tile @ {newServerData.X},{newServerData.Y}...");
            Console.Write($"        is requesting a tile at ({newServerData.X},{newServerData.Y})...");
            AssignTile(newServerData, client);
        }
        else
        {
            //CoreCommunication.SendStringToStream(client, "You are asking for a random tile location... ");
            Console.Write($"        Server is requesting any available tile...");
        }

        return true;
    }

    bool AssignTile(ServerData newServerData, SslStream client)
    {
        using MySqlConnection mySqlConnection = new MySqlConnection(ConnStr);
        
        string query = "INSERT INTO servers (x,y,name,ip,port,ownerID,publicKey) VALUES (@x,@y,@name,@ip, @port,@ownerID,@publicKey)";

        try
        {
            mySqlConnection.Open();

            using MySqlCommand cmd = new MySqlCommand(query, mySqlConnection);
            cmd.Parameters.AddWithValue("@x", newServerData.X);
            cmd.Parameters.AddWithValue("@y", newServerData.Y);
            cmd.Parameters.AddWithValue("@name", newServerData.Name);
            cmd.Parameters.AddWithValue("@ip", newServerData.Ip);
            cmd.Parameters.AddWithValue("@port", newServerData.Port);
            cmd.Parameters.AddWithValue("@ownerID", newServerData.OwnerId);
            cmd.Parameters.AddWithValue("@publicKey", newServerData.PublicKey);
                
            cmd.ExecuteNonQuery();
                
            Console.WriteLine("Success!");
                
            CoreCommunication.SendStringToStream(client, $"   GRANTED @ ({newServerData.X},{newServerData.Y})");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        CoreCommunication.SendStringToStream(client, $"   FAILED @ ({newServerData.X},{newServerData.Y})");
        return false;
    }

    public void FindAvailableTile(ServerData newServerData)
    {
        
    }

    public void DeleteServer(int x, int y)
    {
        Console.WriteLine("NOT IMPLEMENTED!!!!!");
        
        //Console.Write($"Removing Server at Tile ({x},{y})...");
        
        //...
        
        //Console.WriteLine("FAILED, server does not exist at that tile location");
    }

    public string GetServerJsonData(int x, int y)
    {
        //Use of the ServerData object is based on old solution and should be removed.
        //ServerData middle-man is no-longer necessary.
        
        using MySqlConnection mySqlConnection = new MySqlConnection(ConnStr);
        
        string query = "SELECT * FROM servers WHERE x = @x AND y = @y";

        try
        {
            mySqlConnection.Open();

            using MySqlCommand cmd = new MySqlCommand(query, mySqlConnection);
            cmd.Parameters.AddWithValue("@x", x);
            cmd.Parameters.AddWithValue("@y", y);
                
            MySqlDataReader rdr = cmd.ExecuteReader();
                
            Console.WriteLine("Success!");
                
            rdr.Read();
                
            //Read result into ServerData object;
            if (!rdr.HasRows)
                return "DoesNotExist";
                    
            ServerData data = new ServerData();
                
            int i = 0;

            data.X = (int) rdr[i++];
            data.Y = (int) rdr[i++];
            data.Name = (string) rdr[i++];
            data.Ip = (string) rdr[i++];
            data.Port = (int) rdr[i++];
            data.OwnerId = (string) rdr[i++];
            data.PublicKey = (string) rdr[i++];

            return JsonConvert.SerializeObject(data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        return "DoesNotExist";
    }

    //This function would only be use in user queries for servers by name.
    public string GetServerJsonData(string name)
    {
        throw new Exception("GetServerJsonData not implemented!");
    }

    public void PrintServers()
    {
        throw new Exception("PrintServers not implemented!");
    }
    
    
}