using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DrawingUtils {

    public class DrawTexts : MonoBehaviour {
        public Vector2 textureSize;
        public GameObject textBoxPrefab;
        private GameObject _thisText;
        public void InitDrawTexts() {
        }
        public void Draw(List<Text> texts) {
            if (_thisText != null)
                Destroy(_thisText);

            // Draw bounding boxes at specified coordinates.
            // foreach det in detections;
            foreach (var text in texts) {

                int x1 = text.X * textureSize.x > textureSize.x ? (int)(textureSize.x) - 50 : (int)(text.X * textureSize.x);
                int y1 = text.Y * textureSize.y > textureSize.y ? (int)(textureSize.y) - 50 : (int)(text.Y * textureSize.y);

                var xText = ((x1 / textureSize.x) - 0.5f) + 0.01f;
                var yText = 0.5f - (1.0f - (y1 / textureSize.y));

                _thisText = Instantiate(textBoxPrefab, Vector3.zero, Quaternion.Euler(180f,0f,0f), this.gameObject.transform) as GameObject;

                // Define the position of box here.
                _thisText.transform.localPosition = new Vector3(xText, yText, 0f);

                _thisText.GetComponent<TextMesh>().text = text.Content;
                _thisText.GetComponent<TextMesh>().color = Color.green;
                Destroy(_thisText, 0.05f);
            }
        }
    }
} // namespace DrawingUtils