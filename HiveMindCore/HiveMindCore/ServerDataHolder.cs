using System.Net.Security;
using System.Net.Sockets;
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
        [JsonInclude] public bool RequestTile;
        [JsonInclude] public int X;
        [JsonInclude] public int Y;
        [JsonInclude] public string Name;
        [JsonInclude] public string Ip;
        [JsonInclude] public int Port;
        [JsonInclude] public string OwnerId;
        [JsonInclude] public string PublicKey;
        [JsonInclude] public string LastContact;

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

        //string query = "INSERT INTO servers (x,y,name,ip,port,ownerID,publicKey) VALUES (@x,@y,@name,@ip, @port,@ownerID,@publicKey)";

        //For testing, this allows for repeated relaunching of same tile while maintainer is disabled
        string query = "REPLACE INTO servers (x,y,name,ip,port,ownerID,publicKey) VALUES (@x,@y,@name,@ip, @port,@ownerID,@publicKey)";

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

    public List<string> GetServerRangeJsonData(int x, int y, int range)
    {
        using MySqlConnection mySqlConnection = new MySqlConnection(ConnStr);

        string query = "SELECT * FROM servers WHERE x > @x1 AND x < @x2 AND y > @y1 AND y < @y2";

        try
        {
            mySqlConnection.Open();

            using MySqlCommand cmd = new MySqlCommand(query, mySqlConnection);
            cmd.Parameters.AddWithValue("@x1", x - range);
            cmd.Parameters.AddWithValue("@x2", x + range);
            cmd.Parameters.AddWithValue("@y1", y - range);
            cmd.Parameters.AddWithValue("@y2", y + range);

            MySqlDataReader rdr = cmd.ExecuteReader();

            Console.WriteLine("Success!");

            //Read result into ServerData object;
            //if (!rdr.HasRows)
            //  return "DoesNotExist";

            List<string> rows = new();

            while (rdr.Read())
            {
                
                ServerData data = new ServerData();
                
                int i = 0;

                data.X = (int)rdr[i++];
                data.Y = (int)rdr[i++];
                data.Name = (string)rdr[i++];
                data.Ip = (string)rdr[i++];
                data.Port = (int)rdr[i++];
                data.OwnerId = (string)rdr[i++];
                data.PublicKey = (string)rdr[i++];

                rows.Add(JsonConvert.SerializeObject(data));
            }

            rdr.Close();

            return rows;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return new();
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

            data.X = (int)rdr[i++];
            data.Y = (int)rdr[i++];
            data.Name = (string)rdr[i++];
            data.Ip = (string)rdr[i++];
            data.Port = (int)rdr[i++];
            data.OwnerId = (string)rdr[i++];
            data.PublicKey = (string)rdr[i++];

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

    public void PruneServers()
    {
        using MySqlConnection mySqlConnection = new MySqlConnection(ConnStr);
        string query = "SELECT x, y, ip, port, publicKey FROM servers";

        try
        {
            mySqlConnection.Open();

            using MySqlCommand cmd = new MySqlCommand(query, mySqlConnection);
            using MySqlDataReader rdr = cmd.ExecuteReader(); // Automatically closes the reader

            while (rdr.Read())
            {
                ServerData data = new ServerData();
                int i = 0;

                // Populate server data from the database
                data.X = (int)rdr[i++];
                data.Y = (int)rdr[i++];
                data.Ip = (string)rdr[i++];
                data.Port = (int)rdr[i++];
                data.PublicKey = (string)rdr[i];

                try
                {
                    // Create and activate the TCP connection
                    using (TcpClient tcpClient = new TcpClient(data.Ip, data.Port))
                    {
                        using (TileStream tileStream = new TileStream(tcpClient))
                        {
                            tileStream.ActivateStream(data.PublicKey);
                            tileStream.SendStringToStream("Ping");

                            if (tileStream.GetStringFromStream() != "Pong")
                                throw new Exception("Did not receive pong from server " + data.X + ", " + data.Y + ".");
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log the error and proceed with deleting the server from the database
                    Console.WriteLine($"Error connecting to server {data.X}, {data.Y}: {e.Message}");
                    Console.WriteLine($"Goodbye server at {data.X}, {data.Y}.");

                    try
                    {
                        // Try deleting the server if the connection failed
                        DeleteServer(data.X, data.Y);
                    }
                    catch (Exception deleteException)
                    {
                        // Log if the deletion failed
                        Console.WriteLine($"Error deleting server {data.X}, {data.Y}: {deleteException.Message}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            // Log any errors that occur during the database connection or command execution
            Console.WriteLine($"An error occurred while accessing the database: {e.Message}");
        }
    }


    //Returns true if command sent; false otherwise.
    public bool DeleteServer(int x, int y)
    {
        using MySqlConnection connection = new MySqlConnection(ConnStr);

        try
        {
            connection.Open();
            string query = "DELETE FROM servers WHERE x = @x AND y = @y";

            MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@x", x);
            command.Parameters.AddWithValue("@y", y);

            command.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }
}