# UI Rendering API

Supported render objects:

- Bounding boxes
- Text

```json
{
    "Detections": [
        {
            "BoundingBox": {
                "X": 0,
                "Y": 0,
                "Width": 1,
                "Height": 1,
                "Label": "Orange",
                "LabelScore": 0.80,
                "Confidence": 0.95
            }
        }
    ],
    "Texts": [
        {
            "X": 0,
            "Y": 0,
            "FontSize": 14,
            "Content": "Step 1: Get an orange"
        }
    ]
}
```