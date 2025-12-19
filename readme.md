* Hive Mind
Welcome to Hive Mind. Hive Mind is an open-source shared experience where all players share one world (the hive). The hive is defined as an infinite grid of hexagonal tiles. Each tile can be “possessed” by a “tile server” and given its own physics, assets, rules, etc. As a player, one can walk between tiles seamlessly.


Hive Mind is a distributed platform composed of three distinct component types:
* Core Server
* Tile Servers
* Clients

** Core Server
There is one core server for the entire platform. It hosts the map of tiles.
Responsible for:
* Assigning tile servers positions on the grid
* Responding to client requests on what servers are where
* Automatically removing unreachable servers

** Tile Servers
Servers possessing individual tiles. These can provide their own physics, as-
sets, rules, etc.
Responsible for:
* Contacting core server to possess a tile
* Accepting & authenticating client connections
* Providing assets upon client request

** Clients
The actual players. Clients can seamlessly walk between tiles, automatically
loading in assets of visible tiles as the player moves. Fundamentally, each client
is defined by their public RSA key. When they connect to a tile server, said
server will issue a challenge using the public key, which only the client can
solve. For technical purposes, all clients are represented by their public key.
They may specify a username to be visible to other clients.
Responsible for:
* Contacting the core to receive tile server information
* Contacting tile servers to request objects
* Establishing and maintaining an active connection with the current tile server to send/receive game-critical information
* Sanitizing all assets/objects received from tile servers to ensure that no malicious or malformed data is used
* Verifying the authenticity of a tile server
