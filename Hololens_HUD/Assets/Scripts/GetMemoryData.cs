using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using FindingUtils;
using DrawingUtils;

[System.Serializable]
public struct ObjectInMemoryInfo {

    public float[] pos;
    public int id;
    public string label;
    public long last_seen;
    public string status;
    public float[] xyxyn;
    public StateData state;
}

[System.Serializable]
public class StateData
{
    public Dictionary<string, float> state = new Dictionary<string, float>();
}

[System.Serializable]
public class ObjectsInMemoryResult
{
    public List<ObjectInMemoryInfo> result;
}
public class GetMemoryData : MonoBehaviour
{
    // Start is called before the first frame update
    const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    bool readyToSendEntity = true;
    public DrawLabels drawLabels;
    private DrawingGizmo gm;

    APIToken token;
    public static List<ObjectInMemoryInfo> objectInMemoryInfo;
    private Queue<List<ObjectInMemoryInfo>> objInMemoryList;
    public GetObjectFromMemory getObjectFromMemory;
    void Start()
    {
        objInMemoryList = new Queue<List<ObjectInMemoryInfo>>();
        //Debug.Log("Hi Objects Memory");
        drawLabels.InitDrawLabels();
        gm = this.GetComponent<DrawingGizmo>();

        StartCoroutine(this.GetBearerToken());
        //TestMemoryData();
        // TestMemoryData();
        //getObjectFromMemory.InitFindLabels();
    }
    IEnumerator GetBearerToken()
    {
        Debug.Log("Getting token");
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("username", "test"));
        formData.Add(new MultipartFormDataSection("password", "test"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/token", formData))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
            {
                this.token = APIToken.FromJSON(www.downloadHandler.text);
                Debug.Log(this.token.access_token);
            }
        }
        this.readyToSend = true;
    }

    public void TestMemoryData()
    {
        string test = "[{\"pos\": [-0.15625146866666473, -0.8371317528212436, -0.6927490014418084], \"id\": 1, \"label\": \"jar\", \"last_seen\": 133314156588973631, \"status\": \"outside\"}, { \"pos\": [-0.4858943354638425, -0.8379596287000366, -0.45763596621021796], \"id\": 4, \"label\": \"plate\", \"last_seen\": 133314156620960472, \"status\": \"tracked\", \"xyxyn\": [0.9515625, 0.5416666666666666, 0.9984375, 0.625], \"state\": { } }, { \"pos\": [-0.45783918563425385, -0.8485310446669867, -0.4034173386891438], \"id\": 5, \"label\": \"tortilla\", \"last_seen\": 133314156620960472, \"status\": \"tracked\", \"xyxyn\": [0.89609375, 0.6541666666666667, 0.94296875, 0.7375], \"state\": { \"plain\": 0.6100036977501946, \"cut\": 0.20024783173920785, \"nutella\": 0.07122405969726602, \"in-package\": 0.08264436717609581, \"peanut-butter\": 0.008253306691368097, \"nutella+banana\": 0.032382052176811366, \"pb+jelly\": 0.020797084049964557} }, { \"pos\": [-0.6377640109620134, -0.840088897205035, -0.11326367265112079], \"id\": 6, \"label\": \"tortilla2\", \"last_seen\": 133314156620960472, \"status\": \"tracked\", \"xyxyn\": [0.42578125, 0.4444444444444444, 0.47265625, 0.5277777777777778]}]";
        ObjectsInMemoryResult data = JsonUtility.FromJson<ObjectsInMemoryResult>("{\"result\":" + test + "}")!;
        objectInMemoryInfo = data.result;
        
        /* load the id to session mapping*/
        GlobalStats.SetObjIDtoSessionMap(getTestingObjectList());
        drawLabels.DrawPlainLabels(objectInMemoryInfo);

      //  gm.VisualizeTaskGroup();

        //return data.result;
    }

    public Dictionary<string,int> getTestingObjectList() {
        Dictionary<string, int> objectList = new Dictionary<string, int>();
        objectList.Add("jar", 1);
        objectList.Add("plate", 1);
        objectList.Add("tortilla", 2);
        objectList.Add("tortilla2", 1);
        return objectList;
    }
   
IEnumerator GetData_Coroutine()
    {
        //Debug.Log("Get Objects in Memory Data");

        using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/data/detic:memory"))

        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
                Debug.Log("data object in Memory:");
               // Debug.Log(www.downloadHandler.text);
                string jsonString = www.downloadHandler.text;
                // string jsonStringTestArtificial = "[{"xyz_center":[0.3158715267018071,0.04286724992930058,-0.2164452705772737],"label":"butter knife","track_id":"butter knife_1","seen_before":true},{"xyz_center":[0.524640984647311,0.19084135572791483,-0.3310319818568104],"label":"paper towel","track_id":"paper towel_1","seen_before":false},{"xyz_center":[-0.01420577449441146,0.1134371197402464,-0.43126849863431205],"label":"Jar of nut butter","track_id":"Jar of nut butter_1","seen_before":true},{"xyz_center":[0.0495014298554342,0.04520049326363962,-0.18016309017395338],"label":"cutting board","track_id":"cutting board_1","seen_before":true},{"xyz_center":[-0.1344299596075202,0.11509722533159278,-0.399588020394815],"label":"Jar of jelly / jam","track_id":"Jar of jelly / jam_1","seen_before":true},{"xyz_center":[0.6672618777671846,0.07245634829792369,-0.2996999933581419],"label":"Jar of jelly / jam","track_id":"Jar of jelly / jam_2","seen_before":true},{"xyz_center":[0.5519075717864717,0.16811266805973646,-0.09126717429861064],"label":"Jar of jelly / jam","track_id":"Jar of jelly / jam_3","seen_before":true},{"xyz_center":[0.7044525244872955,0.17190157120325972,-0.3484168498609881],"label":"cutting board","track_id":"cutting board_2","seen_before":true}]";
                try
                {

                // new format needed


                //ObjectsInMemoryResult data = JsonUtility.FromJson<ObjectsInMemoryResult>("{\"result\":" + jsonString+ "}")!;

                objectInMemoryInfo = JsonUtility.FromJson<ObjectsInMemoryResult>("{\"result\":" + jsonString + "}").result;
                //GlobalStats.SetObjIDtoSessionMap(getTestingObjectList());

                drawLabels.DrawPlainLabels(objectInMemoryInfo);
               // gm.VisualizeTaskGroup();
                //   Debug.Log("FindObject first:  " + data.result[0].track_id + " ------ " + data.result[0].label);
                //getObjectFromMemory.FindObject(data.result);
                //objectInMemoryInfo = data.result;
            }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }

        }
        this.readyToSend = true;
    }

    // Update is called once per frame
    void Update()
    {

        //objectInMemoryInfo = TestMemoryData();
        //GameObject.Find("HLUnityScriptHolder").GetComponent<GetObjects>().drawLabels.DrawPlainLabels(objectInMemoryInfo);
        //TestMemoryData();
        if (this.readyToSend)
        {            this.readyToSend = false;
            StartCoroutine(GetData_Coroutine());
        }
        
    }
}
