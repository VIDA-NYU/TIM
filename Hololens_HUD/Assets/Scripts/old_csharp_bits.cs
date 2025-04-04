/***
using MessagePack;
using System.Threading.Tasks;
using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour
{

    private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private byte[] _recieveBuffer = new byte[8142];

    private void SetupServer()
    {
        try
        {
            _clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 6670));
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.Message);
        }

        _clientSocket.BeginReceive(_recieveBuffer, 0, _recieveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

    }

    public void ReceiveCallback(IAsyncResult AR)
    {
        //Check how much bytes are recieved and call EndRecieve to finalize handshake
        int recieved = _clientSocket.EndReceive(AR);

        if (recieved <= 0)
            return;

        //Copy the recieved data into new buffer , to avoid null bytes
        byte[] recData = new byte[recieved];
        Buffer.BlockCopy(_recieveBuffer, 0, recData, 0, recieved);

        //Process data here the way you want , all your bytes will be stored in recData

        //Start receiving again
        _clientSocket.BeginReceive(_recieveBuffer, 0, _recieveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
    }
}

[MessagePackObject]
public sealed class Detection
{
    [Key(0)]
    public BoundingBox Box { get; set; }
    //[Key(1)]
    //public Contour Contour { get; set; }
    // [IgnoreMember]
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

[MessagePackObject]
class SerializationHeader
{
    [Key(0)]
    public uint Length { get { return _Length; } set { _Length = value; } }
    private uint _Length = 0;
}

class Serializer<DataType>
{

    // this is what you need to read data
    Client reader = new Client();
    private int headerSize = 0;

    public async Task<DataType> ReadAsync()
    {

        // read header, then use header length to read data
        SerializationHeader header = await ReadDeserialized<SerializationHeader>(headerSize);
        return await ReadDeserialized<DataType>(Convert.ToInt32(header.Length));
    }

    private async Task<DataType> ReadDeserialized<T>(int bufferLength)
    {
        uint bytesLoaded = await reader.LoadAsync((uint)bufferLength);
        if (bytesLoaded != bufferLength) throw new Exception(); // TODO: helpful message
        byte[] data = new byte[bufferLength];
        reader.ReceiveCallback(data);
        return MessagePackSerializer.Deserialize<DataType>(data); ;
    }
}

// you probably won't need this but just in case you need to send anything
/***
DataWriter writer;

static public byte[] WriteAsync(DataType data) {
    // Serialize interface-typed object.
    byte[] dataBin = MessagePackSerializer.Serialize(data);
    byte[] header = MessagePackSerializer.Serialize(new SerializationHeader(){ Length = dataBin.Length });
    writer.WriteBytes(header);
    writer.WriteBytes(dataBin);
}
***/


