using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DrawingUtils {

    [System.Serializable]
    public struct BoundingBox {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public string Label;
        public float LabelScore;
        public float Confidence;
        [System.NonSerialized] public Color Color;
    }

    [System.Serializable]
    public struct Text {
        public float X;
        public float Y;
        public int FontSize;
        public string Content;
        [System.NonSerialized] public Color Color;
    }

    [System.Serializable]
    public struct Detection {
        public BoundingBox Box;
    }

    [System.Serializable]
    public struct FrameResults {
        public List<Detection> Detections;
        public List<Text> Texts;
    }

    public class DrawBoundingBoxes : MonoBehaviour {
        public Vector2 textureSize;
        public GameObject textBoxPrefab;

        private Material _material;
        private Texture2D _texture;
        private GameObject _thisBoundingBox;



        // Start is called before the first frame update
        public void InitDrawBoundingBoxes() {
            if (_texture != null)
                Destroy(_texture);

            // Get material component from attached game object via the mesh renderer.
            _material = this.gameObject.GetComponent<MeshRenderer>().material;

            // Create a new texture instance with same size as the canvas.
            _texture = new Texture2D((int)textureSize.x, (int)textureSize.y);

            // Set the texture to transparent (with helper method)
            _texture = Texture2DExtension.TransparentTexture(_texture);

            // Apply and set main material texture;
            _texture.Apply();
            _material.mainTexture = _texture;

            Debug.Log("Initialized");
        }

        public void DrawDetections(List<Detection> detections) {
            Debug.Log("Inside DrawDetections");
            List<DrawingUtils.BoundingBox> boundingBoxes = new List<DrawingUtils.BoundingBox>();
            Debug.Log($"Before foreach {detections} {detections?.Count}");
            foreach (Detection det in detections ?? new List<Detection>()) {
                Debug.Log("Box X: " + det.Box.X);
                boundingBoxes.Add(det.Box);
            }
            Debug.Log($"After foreach {boundingBoxes.Count}");
            DrawBoxes(boundingBoxes);
        }

        public void DrawBoxes(List<BoundingBox> boxes) {
            // Destroy cached variables to prevent memory leaks.
            if (_texture != null)
                Destroy(_texture);

            if (_thisBoundingBox != null)
                Destroy(_thisBoundingBox);
            //Destroy(_thisBoundingBox.GetComponent<TextMesh>());

            // Create a new texture instance with same size as the canvas.
            _texture = new Texture2D((int)textureSize.x, (int)textureSize.y);

            // Set the texture to transparent (with helper method)
            _texture = Texture2DExtension.TransparentTexture(_texture);

            // Draw bounding boxes at specified coordinates.
            // foreach det in detections;
            foreach (var box in boxes) {

                // Only draw boxes over a certain size
                //if (box.Height < 50 || box.Width < 50)
                //    continue;
                //Debug.Log("see");

                // Check boundary conditions
                // condition ? result_if_true : result_if_false
                // Add 2 extra pixels to boundary to prevent texture wrap
                //int x1 = box.X*500 > 0.0f ? (int)box.X*500 : 0 + 2;
                //int y1 = box.Y*500 > 0.0f ? (int)box.Y*500 : 0 + 2;
                //int x2 = (box.Width*500 + x1) > textureSize.x ? (int)(textureSize.x) - 2 : (int)(box.Width*500 + x1);
                //int y2 = (box.Height*500 + y1) > textureSize.y ? (int)(textureSize.y) - 2 : (int)(box.Height*500 + y1);


                int x1 = box.X * textureSize.x > textureSize.x ? (int)(textureSize.x) - 50 : (int)(box.X * textureSize.x);
                int y1 = box.Y * textureSize.y > textureSize.y ? (int)(textureSize.y) - 50 : (int)(box.Y * textureSize.y);

                int x2 = (box.Width * textureSize.x + x1) > textureSize.x ? (int)(textureSize.x) - 2 : (int)(box.Width * textureSize.x + x1);
                int y2 = (box.Height * textureSize.y + y1) > textureSize.y ? (int)(textureSize.y) - 2 : (int)(box.Height * textureSize.y + y1);



                //Debug.LogFormat("textureSize.x: {0}, textureSize.y: {1}", textureSize.x, textureSize.y);

                // fit to current canvas and webcam size
                // 416 x 416 is size of tensor input (Not using it now,but we can change it if necessary.)
                //x1 = (int)(textureSize.x * x1 / 416f);
                //y1 = (int)(textureSize.y * y1 / 416f);
                //x2 = (int)(textureSize.x * x2 / 416f);
                //y2 = (int)(textureSize.y * y2 / 416f);


                //Debug.LogFormat("x1: {0}, y1: {1}, x2: {2}, y2: {3}", x1, y1, x2, y2);

                // Define the vertex of box here
                // PS: the vertex may not be so called "topleft" or "bottomright".
                // It's influenced by the attached object's proprerty.
                var topLeft = new Vector2(x1, y1);
                var bottomRight = new Vector2(x2, y2);

                //Debug.LogFormat("topLeft: {0}, {1}; bottomRight: {2}, {3}", topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);

                _texture = Texture2DExtension.Box(
                    _texture,
                    topLeft,
                    bottomRight,
                    Color.cyan);


                //Create a new 3D text object at position and
                // set the label string.Canvas is scaled to x = -0.5, 5
                //and y = -0.5, 0.5
                var xText = ((topLeft.x / textureSize.x) - 0.5f) + 0.01f;
                var yText = 0.5f - (1.0f - (topLeft.y / textureSize.y));
                _thisBoundingBox = Instantiate(
                    textBoxPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    this.gameObject.transform) as GameObject;

                // Define the position of box here.
                _thisBoundingBox.transform.localPosition = new Vector3(xText, yText, 0f);


                // Set the label of the bounding box.

                // Confidence should be the probability of correctly prediction. So set it as float.
                var label = $"Label:{box.Label}, probability:{(float)box.Confidence}";

                // Defining the property of label here.
                _thisBoundingBox.GetComponent<TextMesh>().text = label;
                _thisBoundingBox.GetComponent<TextMesh>().color = Color.cyan;
                Destroy(_thisBoundingBox, 0.05f);
            }

            // Apply and set main material texture;
            _texture.Apply();
            _material.mainTexture = _texture;
        }
    }
} // namespace DrawingUtils