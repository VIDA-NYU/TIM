using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using DrawingUtils;

[System.Serializable]
public struct Label {
    public float X;
    public float Y;
    public float Z;
    public string Content;
}
[System.Serializable]
public struct ObjectInfo {
    public List<float>  xyxyn;
    public float confidence;
    public int class_id;
    public string label;
    public List<float> xyz_top;
    public List<float> xyz_center;
    // public float depth_map_dist;
}
[System.Serializable]
public class QrCodeResult
{
    public List<ObjectInfo> result;
}

public class GetObjects : MonoBehaviour
{
    // Start is called before the first frame update
    const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    APIToken token;
    public DrawLabels drawLabels;
    int delay_timer = 0;
    // public ReasoningInfo currentReasoningInfo;
    void Start()
    {
        //StartCoroutine(this.GetBearerToken());

        drawLabels.InitDrawLabels();
        
    }

   // void GetData() => StartCoroutine(GetData_Coroutine());

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

    /* OBSOLETE */
    /*
    IEnumerator GetData_Coroutine()
    {
       // Debug.Log("Get Objects Data");
        // outputArea.text = "Loading ...";

        using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/data/detic:world"))
        // using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/data/reasoning"))

        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
                if (GlobalStats.ALLOW_DEBUG_MSG)
            {
                Debug.Log("data object:");
                Debug.Log(www.downloadHandler.text);
            }
                string jsonString = www.downloadHandler.text;

            // string jsonStringTest =  "[{\"xyxyn\":[0.90574247,0.26542813,0.9669545,0.4498217],\"confidence\":0.3359649,\"class_id\":7,\"label\":\"Jar of peanut butter\",\"xyz_top\":[-1.3027948647632925,0.01324658133295864,-1.0764475106715667],\"xyz_center\":[1.288487070661948,-0.003482997091537471,-0.5814199555514715],\"depth_map_dist\":105.39742234080745}]";
            // string jsonStringTest = "[{\"xyxyn\":[0.5707391,0.7773882,0.82357925,0.99531436],\"confidence\":0.73660636,\"class_id\":9,\"label\":\"person\",\"xyz_top\":[2.2966095676677742,-0.6999381291857674,1.5120982255445887],\"xyz_center\":[-0.08515332496022195,-0.5195536778818075,0.10639333213865136],\"depth_map_dist\":142.8493723416739}]";
            string jsonStringTestArtificial = "[{\"xyxyn\":[0.5707391,0.7773882,0.82357925,0.99531436],\"confidence\":0.73660636,\"class_id\":9,\"label\":\"person\",\"xyz_top\":[2.2966095676677742,-0.6999381291857674,1.5120982255445887],\"xyz_center\":[0.1,-0.1,0.5],\"depth_map_dist\":142.8493723416739}]";
                try
                {
                    QrCodeResult data = JsonUtility.FromJson<QrCodeResult>("{\"result\":" + jsonString+ "}")!;
                    // QrCodeResult dataToyExample = JsonUtility.FromJson<QrCodeResult>("{\"result\":" + jsonStringTestArtificial+ "}")!;

                    // drawLabels.Draw(dataToyExample.result);
                    drawLabels.Draw(data.result);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }

        }
        this.readyToSend = true;
    }
    */

    // Update is called once per frame
    void Update()
    {
        /*
         if (this.readyToSend)
         {
             if (delay_timer % 1 == 0)
             {
                 this.readyToSend = false;
                 StartCoroutine(GetData_Coroutine());
                 delay_timer = 0;
             }
         }
         delay_timer += 1;

 */
    }

}
