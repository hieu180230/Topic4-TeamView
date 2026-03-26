using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

class Program
{
    static ConcurrentDictionary<string, WebSocket> desktops = new();
    static ConcurrentDictionary<string, WebSocket> webs = new();

    static async Task Main(string[] args)
    {
        HttpListener listener = new HttpListener();

        listener.Prefixes.Add("http://localhost:5000/ws/");
        listener.Start();

        Console.WriteLine("WebSocket server started at ws://localhost:5000/ws");

        while (true)
        {
            var context = await listener.GetContextAsync();

            if (context.Request.IsWebSocketRequest)
            {
                _ = HandleClient(context); // run async
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    static async Task HandleClient(HttpListenerContext context)
    {
        WebSocket ws = (await context.AcceptWebSocketAsync(null)).WebSocket;

        string name = "";
        string type = "";

        var buffer = new byte[8192];

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var ms = new System.IO.MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var json = Encoding.UTF8.GetString(ms.ToArray());

                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string msgType = root.GetProperty("type").GetString();

                // ================= REGISTER =================
                if (msgType == "register")
                {
                    name = root.GetProperty("name").GetString();
                    type = root.GetProperty("clientType").GetString();

                    if (type == "desktop")
                        desktops[name] = ws;
                    else
                        webs[name] = ws;

                    Console.WriteLine($"{type} registered: {name}");

                    await SendDesktopListToAllWebs();
                }

                // ================= COMMAND =================
                else if (msgType == "command")
                {
                    string to = root.GetProperty("to").GetString();

                    if (desktops.TryGetValue(to, out var target))
                    {
                        await target.SendAsync(
                            Encoding.UTF8.GetBytes(json),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                }

                // ================= RESPONSE =================
                else if (msgType == "response")
                {
                    string to = root.GetProperty("to").GetString();

                    if (webs.TryGetValue(to, out var target))
                    {
                        await target.SendAsync(
                            Encoding.UTF8.GetBytes(json),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        // ================= CLEANUP =================
        if (type == "desktop")
            desktops.TryRemove(name, out _);
        else if (type == "web")
            webs.TryRemove(name, out _);

        Console.WriteLine($"{type} disconnected: {name}");

        await SendDesktopListToAllWebs();

        try { ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait(); } catch { }
    }

    // ================= SEND LIST =================
    static async Task SendDesktopListToAllWebs()
    {
        var list = desktops.Keys.ToList();

        var json = JsonSerializer.Serialize(new
        {
            type = "desktop_list",
            data = list
        });

        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var w in webs.Values)
        {
            if (w.State == WebSocketState.Open)
            {
                await w.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}