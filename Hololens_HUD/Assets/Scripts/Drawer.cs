using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrawingUtils;
using Connection;
using System.Threading.Tasks;
using System.Net.Sockets;

public class Drawer : MonoBehaviour {

    // Texture handler for bounding boxes
    public DrawBoundingBoxes drawBoundingBoxes;
    public DrawTexts drawTexts;
    // Parameters for host connect
    // https://stackoverflow.com/questions/32876966/how-to-get-local-host-name-in-c-sharp-on-a-windows-10-universal-app
    // Connecting to desktop host IP, not the hololens... Get the IP of PC and retry with specified port 
    public string server = "10.18.201.167";
    public int port = 23939;

    //private bool running = true; // if this is set to false, we will stop trying to reconnect to the app.
    //int disconnectSleep = 3000; // when we disconnect, sleep for 3 (?) seconds

    // From Tiny YOLO string labels.
    private string[] _labels = {
            "aeroplane", "bicycle", "bird", "boat", "bottle",
            "bus", "car", "cat", "chair", "cow",
            "diningtable", "dog", "horse", "motorbike", "person",
            "pottedplant", "sheep", "sofa", "train", "tvmonitor"
        };

    List<DrawingUtils.BoundingBox> boundingBoxes;
    List<DrawingUtils.Text> texts;
    // Start is called before the first frame update
    Connection.ObjectReader<FrameResults> reader;

    void Start() {
        // Connect to socket here
        Debug.Log("Before initializing reader");
        reader = new ObjectReader<FrameResults>() { data = new FrameResults { Detections = new List<Detection>() } }; // idk if this is necessary
        reader.Start(server, port);
        Debug.Log("initialized reader");
        // Initialize the bounding box canvas 
        drawBoundingBoxes.InitDrawBoundingBoxes();
        drawTexts.InitDrawTexts();
        boundingBoxes = new List<DrawingUtils.BoundingBox>();
        texts = new List<DrawingUtils.Text>();
        DrawingUtils.BoundingBox box = new DrawingUtils.BoundingBox
        {
            Label = "1", // TopLabel is int
            X = 0.4f,
            Y = 0.3f,

            Height = 0.2f,
            Width = 0.5f,

            Confidence = 0.99F
        };

        DrawingUtils.BoundingBox box2 = new DrawingUtils.BoundingBox
        {
            Label = "2", // TopLabel is int
            X = 0.9f,
            Y = 0.2f,

            Height = 600,
            Width = 400,

            Confidence = 0.89F
        };

        // Add the filled box to list
        boundingBoxes.Add(box);
        //boundingBoxes.Add(box2);

        //drawBoundingBoxes.DrawBoxes(boundingBoxes);
        //drawTexts.Draw(texts);
    }

    private void Update() {
        Debug.Log("Calling DrawDetections");
        Debug.Log($"reader data {reader.data}");
        try {
            Debug.Log($"reader data len {reader.data.Detections?.Count}");
        } catch {
            Debug.Log("probably null detections");
        }
        drawBoundingBoxes.DrawDetections(reader.data.Detections);
        drawTexts.Draw(reader.data.Texts ?? new List<Text>());
    }

    void OnDisable() {
        reader.Close();
    }
}