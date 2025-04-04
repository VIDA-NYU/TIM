
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // <= THIS
using System;
namespace DrawingUtils {

    [System.Serializable]
    public class StepEntities
    {
        public List<string> ingredients;
        public List<string> tools;
    }
    [System.Serializable]
    public struct RecipeEntity {
        public int  step_id;
        public StepEntities step_entities;
    }
    [System.Serializable]
    public class RecipeEntityResults
    {
        public List<RecipeEntity> result;
    }
    [System.Serializable]
    public class ObjectMemory
    {
        public bool visited;
        public GameObject obj_text;
    }

    public class DrawLabels : MonoBehaviour
    {
        public Vector2 textureSize;
        public GameObject textBoxPrefab;
        private Camera m_maincamera;
        private List<int> created_label_ids = new List<int>();
        private List<GameObject> m_holder = new List<GameObject>();
        // private GameObject _thisText;
        public List<RecipeEntity> reasoningInfo;
        public int stepId;
        Dictionary<string, List<ObjectMemory>> dicGameObjects = new Dictionary<string, List<ObjectMemory>>();
        public void InitDrawLabels() {

        }
        private double distanceTwoPoints(Vector3 obj,  GameObject objMemory)
        {
            var x1 = obj.x;
            var y1 = obj.y;
            var z1 = obj.z;
            
            var x2 = objMemory.transform.position.x; //xyz_center[0];
            var y2 = objMemory.transform.position.y;
            var z2 = objMemory.transform.position.z;
            
            double d = Math.Pow((Math.Pow(x2 - x1, 2) +
                                Math.Pow(y2 - y1, 2) +
                                Math.Pow(z2 - z1, 2) *
                                        1.0), 0.5);
            return d;
        }
        private string getClosestObjectID(Vector3 newObjectPosition,  string label )
        {
            // List<ObjectMemory> objectsLocalMemory  = dicGameObjects[label];
            string anyvisitedAtall = "None";
            double distanceMin = 100000000000.0;
            int objClosestID = 0;
            int count = 0;
            bool anyVisited = false;
            foreach (var objMemory in dicGameObjects[label]) {
                if(!objMemory.visited){
                    double distance = distanceTwoPoints(newObjectPosition,  objMemory.obj_text);
                    if (distance < distanceMin){
                        distanceMin = distance;
                        objClosestID = count;
                    }
                    anyVisited = true;
                }
                count = count +1;
            }
            if(anyVisited){
                float time = 0;
                float duration = 0.03f;
                while (time < duration)
                {
                    dicGameObjects[label][objClosestID].obj_text.transform.position = Vector3.Lerp(dicGameObjects[label][objClosestID].obj_text.transform.position, newObjectPosition, time/duration);
                    time += Time.deltaTime;
                }
                dicGameObjects[label][objClosestID].visited = true;
                return dicGameObjects[label][objClosestID].obj_text.name;
            } else {
                return anyvisitedAtall;
            }
        }
        public void updateDictionary(GameObject gameObject, string label){
            ObjectMemory newObject = new ObjectMemory();
            newObject.visited = false;
            newObject.obj_text = gameObject;
            if(dicGameObjects.ContainsKey(label)){
                List<ObjectMemory> tempGOExit =  dicGameObjects[label];
                tempGOExit.Add(newObject);
                dicGameObjects[label] = tempGOExit;
            } else {
                List<ObjectMemory> tempGO = new List<ObjectMemory>();
                tempGO.Add(newObject);
                dicGameObjects.Add(label,  tempGO);
            }
        }
        public GameObject createLabel(ObjectInMemoryInfo obj, Vector3 xyz_lefthandCoord, Color32 colorLabel, string typeLabel){
            GameObject _thisText = Instantiate(textBoxPrefab, xyz_lefthandCoord, Quaternion.Euler(0f,1800f,0f), this.gameObject.transform) as GameObject;
            m_maincamera = Camera.main;
            if (m_maincamera != null)
                _thisText.transform.LookAt(m_maincamera.transform);

            _thisText.transform.Rotate(0,180,0, Space.Self);

            _thisText.GetComponent<TextMeshPro>().text = obj.label;
            _thisText.GetComponent<TextMeshPro>().color = colorLabel;
            _thisText.name = GetInstanceID().ToString();
            // _thisText.tag = "displaylabel";
            return _thisText;
        }
        private Vector3 ConvertRightHandedToLeftHandedVector (Vector3 rightHandedVector)
        {
            return new Vector3(rightHandedVector.x, rightHandedVector.y, -rightHandedVector.z);
        }

        
       /* OBSOLETE */
       /*
        public string createNewTextObject (List<string> ingredients, List<string> tools, string label, Vector3 xyz_lefthandCoord, ObjectInfo detectedObj)
        {
            if (ingredients.Contains(label)) {
                GameObject gameObj =  createLabel( detectedObj, xyz_lefthandCoord, new Color32(255, 0, 0, 255), "Ingredients"); //red
                updateDictionary( gameObj, label);
                return gameObj.name;
            }
            if (tools.Contains(label)) {
                GameObject gameObj =  createLabel( detectedObj, xyz_lefthandCoord, new Color32(0, 0, 255, 255), "Tools"); //blue
                updateDictionary( gameObj, label);
                return gameObj.name;
            }
            return "None";
        }
       */

        public void DrawPlainLabels(List<ObjectInMemoryInfo> data) {
            // Destroy objects which are in memory but not in the new scene 

            /*List<string> labelsInMemory = new List<string>();
            foreach (var item in dicGameObjects)
            {
                var label = item.Key;
                if (ingredients.Contains(label) || tools.Contains(label))
                {
                    labelsInMemory.Add(label);
                }
                else
                {
                    foreach (var obj in item.Value)
                    {
                        Destroy(obj.obj_text, 0.02f);
                    }
                    dicGameObjects.Remove(item.Key);
                }
            }*/

            // List<string> objectNames = new List<string>(); // save Object ID of the rendered objects (in the current scene)
            // Draw bounding boxes at specified coordinates.
            // foreach det in detections;

            /* clear all game objects */
            /* this needs to be optimized*/
            foreach (GameObject m in m_holder)
                Destroy(m);

            m_holder.Clear();
            
            foreach (ObjectInMemoryInfo detectedObj in data)
            {
                    string label = detectedObj.label;
                    Vector3 xyz_righthandCoord = new Vector3(detectedObj.pos[0], detectedObj.pos[1], detectedObj.pos[2]);
                    Vector3 xyz_lefthandCoord = ConvertRightHandedToLeftHandedVector(xyz_righthandCoord);
                    GameObject gameObj = createLabel(detectedObj, xyz_lefthandCoord, new Color32(255, 0, 0, 255), "Ingredients"); //red
                  //  GlobalStats.updateObjMapEntry(detectedObj, xyz_lefthandCoord);
                    m_holder.Add(gameObj);
             }

            /* run a full update at the end */
           // GlobalStats.updateObjMap();

        }

        /* OBSOLETE*/
        /*
        public void Draw(List<ObjectInfo> detectedObjects) {

            stepId = GetReasoning.currentStepID;
            reasoningInfo = GetReasoning.recipeEntities;
            List<string> ingredients = reasoningInfo[stepId].step_entities.ingredients;
            List<string> tools = reasoningInfo[stepId].step_entities.tools;


            // Destroy objects which are in memory but not in the new scene 
            List<string> labelsInMemory = new List<string>();
            foreach (var item in dicGameObjects) {
                var label = item.Key;
                if (ingredients.Contains(label) || tools.Contains(label) ) {
                    labelsInMemory.Add(label);
                } else {
                    foreach (var obj in item.Value) {
                        Destroy(obj.obj_text, 0.02f);
                    }
                    dicGameObjects.Remove(item.Key);
                }
            }
            
            List<string> objectNames = new List<string>(); // save Object ID of the rendered objects (in the current scene)
            // Draw bounding boxes at specified coordinates.
            // foreach det in detections;

            foreach (var detectedObj in detectedObjects) {
                string label = detectedObj.label;
                if (detectedObj.confidence > 0.5 && (ingredients.Contains(label) || tools.Contains(label)) ) {
                    Vector3 xyz_righthandCoord = new Vector3(detectedObj.xyz_center[0], detectedObj.xyz_center[1], detectedObj.xyz_center[2]); // Vector3(xText, yText, zText)
                    Vector3 xyz_lefthandCoord = ConvertRightHandedToLeftHandedVector(xyz_righthandCoord);

                    if (labelsInMemory.Contains(label)){
                        string closestObjectID = getClosestObjectID(xyz_lefthandCoord, label);
                        if (!(closestObjectID.Equals("None"))){ objectNames.Add(closestObjectID); }
                        
                    } else {
                        objectNames.Add(createNewTextObject (ingredients, tools, label, xyz_lefthandCoord, detectedObj));
                    }

                }
            }


            // remove object which are not present in the scene (repited objects)
            foreach (var item in dicGameObjects) {
                List<ObjectMemory> tempGO = new List<ObjectMemory>();
                foreach (var obj in item.Value) {
                    obj.visited = false;
                    if(!(objectNames.Contains(obj.obj_text.name)) || objectNames.Count == 0)
                    {
                        Destroy(obj.obj_text, 0.02f);
                    } else {
                        tempGO.Add(obj);
                    }
                }
                if (dicGameObjects[item.Key].Count == 0){
                    dicGameObjects.Remove(item.Key);
                } else{
                    dicGameObjects[item.Key] = tempGO;
                } 
            }

           
        }
        */
    }
}
