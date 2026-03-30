using OpenCvSharp;

class Webcam
{
    private VideoCapture cap;
    private Mat frame;

    public void Start()
    {
        cap = new VideoCapture(0); // 0 = default camera

        if (!cap.IsOpened())
        {
            Console.WriteLine("Cannot open webcam");
            return;
        }

        frame = new Mat();
        Console.WriteLine("Webcam started");
    }

    public byte[] GetFrame()
    {
        if (cap == null || !cap.IsOpened())
            return null;

        cap.Read(frame);

        if (frame.Empty())
            return null;

        // Encode to JPEG (IMPORTANT)
        //Cv2.ImEncode(".jpg", frame, out byte[] imageData);

        Cv2.ImEncode(".jpg", frame, out byte[] imageData,
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 40));

        frame = frame.Resize(new OpenCvSharp.Size(640, 480));

        return imageData;
    }

    public void Stop()
    {
        cap?.Release();
        cap?.Dispose();
        frame?.Dispose();

        Console.WriteLine("Webcam stopped");
    }
}