using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GetMessage : MonoBehaviour
{
    const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    APIToken token;
    int frameCount = 0;

    private string p_message;
    public string message;

    IEnumerator GetBearerToken()
    {
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
            }
        }
        this.readyToSend = true;
    }

    private void triggerCommand(string message) {

        // pause this function
        return;
        /*continue here*/
        if (message == "p")
        {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().pauseSystem();
        }
        else if (message == "r")
        {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().resumeSystem();
        }
        else if (message == "a")
        {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().triggerError1();
        }
        else if (message == "b")
        {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().resumeError1();
        }
        else if (message == "c")
        {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().show_main_error();
        }
        else if (message == "v") {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().hide_main_error();
        }
        else if (message == "t")
        {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().activateTIM();
        }
        else if (message == "d")
        {
            GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().deactivateTIM();
        }
    }

    /* this function is for demo purpose only*/
    IEnumerator UpdateMessage()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/data/message"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
            {
                this.message = www.downloadHandler.text;
                //Debug.Log(this.message);
                if (message != p_message)
                    triggerCommand(message);

                p_message = message;
            }
        }
        this.readyToSend = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(this.GetBearerToken());
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.readyToSend) return;

        if (this.frameCount > 0)
        {
            frameCount--;
            return;
        }

        frameCount = 30;
        this.readyToSend = false;
     //   StartCoroutine(this.UpdateMessage());
    }
}