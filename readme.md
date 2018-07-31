# BlockChain
This is a simple BlockChain experiment written in C#.   
The client is capable of conneting to peers, but peers have to be manually provided as there is currently no auto-discover.  

The client is written as a dotnet core project.  
In order to run it you need the DotNetCore 2.0+ runtime installed on your machine.  
To run it you can execute:
```
dotnet .\BlockChain.dll
```

Note that the client requires access to ports `8080` for hosting its web interface and port `8081` for peer to peer connections with other clients.

## Controlling client
The client can be controlled via HTTP command (use Postman or CURL).  

The following URIs are available:  
- `localhost:8080/blocks [GET]`
Returns json containing all blocks known by the client
- `localhost:8080/mineblock [POST]`
Body should contain data to be added to the Block. This will queue the Block for mining on the clients
- `localhost:8080/addpeer` Add a peer to the client with which the chain will be shared.
- `localhost:8080/getpeers` Returns a list of connected peers