using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // <= THIS
using System;

namespace FindingUtils
{
    public class GetObjectFromMemory : MonoBehaviour
    {
        public List<ObjectInMemoryInfo> objectInMemoryInfo;
        // Start is called before the first frame update
        string targetLabel = "Jar of nut butter_1";
        public GameObject gameObjectText;
        private bool _TimResponded = false;
        [SerializeField] GameObject spatial_arrow;
        // public TextMesh objectText;
        void Start()
        {
            gameObjectText = GameObject.Find("ObjectInMemoryText");
            // objectText = gameObjectText.GetComponent<TextMesh>();
        }

        public void InitFindLabels()
        {
        }

        public void updateTargetLabel(string l) {
            targetLabel = l;
        }

        public void TimResponded() {
            _TimResponded = true;
        }


        /* test this function later, figure out what is the effect of changing track_id to id*/
        public void FindObject(List<ObjectInMemoryInfo> objectsInMemory)
        {
            bool flag = false;
            List<float> coordinates = new List<float>();
            int index = 0;
            // foreach det in detections;
            foreach (var objectInMemory in objectsInMemory)
            {
                string label = objectInMemory.label;
                Debug.Log("FindObject:  " + label + " --- " + targetLabel);

                string track_id_label = objectInMemory.id+"";
                int length_label = label.Length;
                string lastest_entrance = track_id_label.Substring(length_label + 1); // +1 because we need to ignore the _ symbol
                int index_lastest_entrance = Int32.Parse(lastest_entrance);

                if (label.Contains(targetLabel) && _TimResponded && index_lastest_entrance > index)
                {
                       // coordinates = objectInMemory.pos;
                        flag = true;
                }
            }
            if(flag)
            {
                string coordXYZ = "(" + coordinates[0].ToString() + "," + coordinates[1].ToString() + (-coordinates[2]).ToString() + ")";
                Debug.Log("FindObject Found:  " + coordXYZ);
                gameObjectText.GetComponent<TextMeshPro>().text = coordXYZ;
                spatial_arrow.GetComponent<SmoothLookAT>().ArrowFollow(new Vector3(coordinates[0], coordinates[1], -coordinates[2]));
                _TimResponded = false;
            }
        }
    }
}
