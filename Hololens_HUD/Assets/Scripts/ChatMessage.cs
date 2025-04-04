using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public class ChatMessage : MonoBehaviour
{

    const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    APIToken token;
    int frameCount = 0;
    long latestMessageTime = 0;
   
    public string streamId = "chat";
    public bool chatStart = false;
    public long initialTimestamp = 0;

    [SerializeField] private TimSpeech Tim;

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
        DateTime now = DateTime.UtcNow.ToLocalTime();
        long currentTimestampInMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // TimeSpan timeSpan = now - unixEpoch;
        // long timestamp = (long)timeSpan.TotalSeconds;
        this.initialTimestamp = currentTimestampInMilliseconds;
        this.readyToSend = true;
    }

    public IEnumerator SendMessage(string message)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        DateTime now = DateTime.UtcNow.ToLocalTime();
        DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan timeSpan = now - unixEpoch;
        long timestamp = (long)timeSpan.TotalSeconds;
        var newMessage = message + " | " + timestamp.ToString();
        formData.Add(new MultipartFormFileSection("entries", Encoding.ASCII.GetBytes(newMessage), "txt", "application/octet-stream"));


        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/chat:user:message", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);

        }
    }

    public IEnumerator RecvMessage()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/data/chat:assistant:message"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            var text = www.downloadHandler.text;

            string[] parts = text.Split('|');

            string firstPart = parts[0].Trim();
            long secondPart = long.Parse(parts[1].Trim());
            if(secondPart > latestMessageTime && secondPart > initialTimestamp)
            {
                if(firstPart != "Default")
                {
                    Tim.DisplayPhrase(firstPart);
                    Tim.SayPhrase(firstPart);
                    latestMessageTime = secondPart;

                }
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
        StartCoroutine(this.RecvMessage());
    }
}
