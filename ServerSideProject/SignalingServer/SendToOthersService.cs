using WebSocketSharp;
using WebSocketSharp.Server;

public class SendToOthersService : WebSocketBehavior
{
    protected override void OnOpen()
    {
        // Log that a new connection was opened
        var session = Sessions.Sessions.First(s => s.ID == ID);
        Console.WriteLine("Connection opened with: " + session.ID);
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Console.WriteLine("Received message: " + e.Data);

        // Send the received message to all clients except the sender
        foreach (var id in Sessions.ActiveIDs)
        {
            if (id != ID) // ID is the identifier of the current session
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }
}