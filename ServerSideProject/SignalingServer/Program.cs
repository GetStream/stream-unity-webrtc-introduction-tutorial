using WebSocketSharp.Server;

Console.WriteLine("Starting server application");

// Bootstrap the application
var wssv = new WebSocketServer(8080);
wssv.AddWebSocketService<SendToOthersService>("/");

// Start the server
wssv.Start();
Console.WriteLine("WebSocket Server Running");

// Stop the server when a key is pressed in the console
Console.WriteLine("Press any key to stop the server...");
Console.ReadKey(true);
wssv.Stop();