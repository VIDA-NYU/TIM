using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;


namespace Connection {
    public sealed class ObjectReader<TData> {
        volatile bool running = false;
        public TData data;
        NetworkStream stream;
        System.Threading.Thread SocketThread;
        TcpClient client;
        string ip;
        int port;
        // private int headerLength;
        int disconnectSleep = 3000;

        public ObjectReader() {
            // headerLength = MessagePackSerializer.Serialize(new SerializationHeader()).Length; // 29
        }

        public void Start(string ip_, int port_) {
            //data = new TData();
            ip = ip_;
            port = port_;
            running = true;
            SocketThread = new System.Threading.Thread(ThreadRun);
            SocketThread.IsBackground = true;
            SocketThread.Start();
        }

        public void Close() {
            running = false;
            //data = null;
            //stop thread
            if (SocketThread != null) { SocketThread.Abort(); }
            if (stream != null) // && stream.Connected
            {
                stream.Close();
                client.Close();
                Debug.Log("Disconnected!");
            }
        }

        void ThreadRun() {
            client = new TcpClient();

            while (running) {
                try {
                    Debug.Log("Trying to connect");
                    client.Connect(ip, port);
                    Debug.Log("Client connected. Trying to get client stream.");
                    stream = client.GetStream();
                    Debug.Log("Client.GetStream() worked");
                    while (running) {
                        Debug.Log("Trying to read data");
                        data = Read();
                        Debug.Log("Data read");
                    }
                } catch (SocketException e) {
                    Debug.Log("Socket exception happened!");
                    Debug.Log(e.ToString());
                    System.Threading.Thread.Sleep(disconnectSleep);
                }
            }
        }

        TData Read() {
            //Debug.Log("In Read() function");
            uint length = BitConverter.ToUInt32(ReadBytes(4), 0);
            Debug.Log("length: " + length);
            return ReadDeserialized<TData>(Convert.ToInt32(length));
        }

        private T ReadDeserialized<T>(int bufferLength) {

            var jsonString = System.Text.Encoding.UTF8.GetString(ReadBytes(bufferLength));
            Debug.Log($"In ReadDeserialized function. msg: {jsonString}");
            T data = JsonUtility.FromJson<T>(jsonString)!;
            return data;
            //var resolver = MessagePack.Resolvers.CompositeResolver.Create(MessagePack.Resolvers.GeneratedResolver.Instance);
            //var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            //return MessagePackSerializer.Deserialize<T>(ReadBytes(bufferLength), options);
        }

        private byte[] ReadBytes(int bufferLength) {
            //Debug.Log("In ReadBytes function");
            byte[] data = new byte[bufferLength];
            //Debug.Log("Before setting bytesCount");
            int bytesCount = stream.Read(data, 0, bufferLength);
            //Debug.Log("Set bytesCount successfully");
            if (bytesCount != bufferLength) {
                throw new Exception("Connection didn't return a full message."); // TODO: add proper exception type
            }
            Debug.Log("Returning data: " + System.Convert.ToBase64String(data));
            return data;
        }
    }


}