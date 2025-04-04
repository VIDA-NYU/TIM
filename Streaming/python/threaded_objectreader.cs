class UsageInTheMainApp {
    ObjectReader<IList<Detection>> reader;
    void Start() {
        Application.runInBackground = true;
        startServer();
        reader = ObjectReader<IList<Detection>>();
        reader.Start();
    }

    void Update() {
        if(reader.data != null) {
            drawDetections(reader.data);
        }
    }

    void OnDisable() {
        reader.Stop();
    }
}


class ObjectReader<TData> {
    volatile bool running = false;
    public TData data = null;
    NetworkStream stream;
    System.Threading.Thread SocketThread;
    TcpClient client;

    public void Start() {
        running = true;
        SocketThread = new System.Threading.Thread(ThreadRun);
        SocketThread.IsBackground = true;
        SocketThread.Start();
    }

    public void Close() {
        running = false;
        data = null;
        //stop thread
        if (SocketThread != null) { SocketThread.Abort(); }
        if (stream != null && stream.Connected) {
            stream.Close();
            client.Close();
            Debug.Log("Disconnected!");
        }
    }

    void ThreadRun() {
        client = new TcpClient();

        while(running) {
            try {
                client.Connect(server, port);
                NetworkStream stream = client.GetStream();
                while (running) {
                    data = Read();
                }
            }
            catch (SocketException e) {
                Debug.Log(e.ToString());
                System.Threading.Thread.Sleep(disconnectSleep);
            }
        }
    }

    TData Read(int size) {
        SerializationHeader header = ReadDeserialized<SerializationHeader>(headerLength);
        return ReadDeserialized<TData>(header.Length);
    }

    private T ReadDeserialized<T>(int bufferLength) {
        return MessagePackSerializer.Deserialize<T>(ReadBytes(bufferLength));
    }

    private byte[] ReadBytes(int bufferLength) {
        byte[] data = new byte[bufferLength];
        int bytesCount = stream.Read(data, 0, bufferLength);
        if (bytesCount != bufferLength) {
            throw new Exception("Connection didn't return a full message."); // TODO: add proper exception type
        }
        return data;
    }
}