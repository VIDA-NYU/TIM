/*using System.Net.Sockets;
using System.Threading.Tasks;
using MessagePack;


//namespace YoloRuntime
//{

// this is just for show obviously, integrate it however you want
*//***
class ExampleApp
{
    // for now, you'll need to get the local IP address of the desktop computer
    string server;  // TODO: TALK TO Shaoyu, maybe we can get the IP from his app?
    int port = 12345; // we can change this - just need to make sure it's the same on both sides

    private bool running = true; // if this is set to false, we will stop trying to reconnect to the app.
    int disconnectSleep = 3000; // when we disconnect, sleep for 3 (?) seconds

    public async void main()
    {
        ObjectReader reader = ObjectReader<IList<Detection>>();

        // if we disconnect from the server, sleep then try to reconnect
        while (running)
        {
            try
            {
                // make a connection
                await reader.ConnectAsync(server, port);
                while (true)
                {
                    // continuously read then draw
                    data = await reader.ReadAsync();
                    DrawDetections(data);
                }
            }
            catch (SocketException e)
            {
                reader.Close();
                Console.WriteLine($"[TCP Connection ended] {e}");
                Console.WriteLine($"Sleeping for {disconnectSleep} seconds...");
                await Task.Delay(disconnectSleep);
            }
        }
    }
    /***
    private void DrawDetections(IList<Detection> detections)
    {
            foreach(Detection det in detections)
        {
            if (det.box) { DrawBox(det.box); }
        }
        // another alternative is to define Draw as methods of the Detection and BoundingBox classes.
        // see detections.py Detection.draw_cv and Box.draw_cv for examples
    }

    private void DrawBox(BoundingBox box)
    {
        // ... your special bounding box drawing code
    }
}

class ObjectReader<TData>
{
    TcpClient client;
    NetworkStream stream;
    private int headerLength;

    private int closeTimeout = 5000;
    ObjectReader()
    {
        // Calculate the size of the serializer header message.
        headerLength = MessagePackSerializer.Serialize(new SerializationHeader()).Length; // 29
    }

    public async Task ConnectAsync(string server, int port)
    {
        client = new TcpClient();
        await client.ConnectAsync(server, port);
        stream = client.GetStream();
    }

    public void Close()
    {
//Console.WriteLine("Closing tcp client.");
        stream.Close(closeTimeout);
        client.Close();
    }

    /* This will read the next list of detections from the stream */
/***
public async Task ReadAsync(int size)
    {
        SerializationHeader header = ReadDeserialized<SerializationHeader>(headerLength);
        return ReadDeserialized<TData>(header.Length);
    }

    /* this will read a deserialized object from the NetworkStream object. */
/***
    private async Task<T> ReadDeserialized<T>(int bufferLength)
    {
        return MessagePackSerializer.Deserialize<T>(ReadBytesAsync());
    }

    /* read a specified number of bytes from the stream */
/***
    private async Task<byte[]> ReadBytesAsync(int bufferLength)
    {
        byte[] data = new Byte[bufferLength];
        Int32 bytesCount = await stream.ReadAsync(data, 0, bufferLength);
        if (bytesCount != bufferLength)
        {
            throw SocketException("Connection didn't return a full message.");
        }
        return data;
    }
}

/*z

These are the Detection primatives.

*/

/* This is a fixed size message. This lets us know how many bytes to read for the Detection object. */
/***
[MessagePackObject]
class SerializationHeader
{
    [Key(0)]
    public int Length { get { return _Length; } set { _Length = value; } }
    private int _Length = 0;
}

/* This is a top level object that lets you transmit multiple forms of outputs 
(bounding boxes, contours, attention maps, etc.)

By using a generic top-level Class like this, if the detection format changes, it's
just a matter of adding a new field to this object.
*/
/***
[MessagePackObject]
public sealed class Detection
{
    [Key(0)]
    public BoundingBox Box { get; set; }
    // [Key(1)]
    // public Contour Contour { get; set; }
    // [Key(2)]
    // public Mask mask { get; set; }
}

/// <summary>
/// Pass bounding box from managed (C#) windows runtime (C++/CX)
/// </summary>
[MessagePackObject]
public sealed class BoundingBox
{
    // Also add a get/set to allow for int to be passed
    [Key(0)]
    public float X { get; set; }
    [Key(1)]
    public float Y { get; set; }
    [Key(2)]
    public float Width { get; set; }
    [Key(3)]
    public float Height { get; set; }
    [Key(4)]
    public string Label { get; set; }
    [Key(5)]
    public float Confidence { get; set; }
}

    // // If you remove this, just comment out the Detection.contour attribute from detections.py
    // [MessagePackObject]
    // public sealed class Contour {}

***/