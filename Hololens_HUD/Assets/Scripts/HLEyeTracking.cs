using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

[System.Serializable]
public class EyeTrackingData
{
    public bool IsEyeCalibrationValid;
    public bool IsEyeTrackingDataValid;
    public bool IsEyeTrackingEnabled;
    public bool IsEyeTrackingEnabledAndValid;
    public Vector3 GazeOrigin;
    public Vector3 GazeDirection;
    public Vector3 HeadMovementDirection;
    public Vector3 HeadVelocity;
    public Int64 Timestamp;
    public long t;

    public EyeTrackingData(IMixedRealityEyeGazeProvider provider)
    {
        t = System.DateTime.Now.ToFileTime();
        IsEyeCalibrationValid = provider.IsEyeCalibrationValid.HasValue ? (bool)provider.IsEyeCalibrationValid.HasValue : false;
        IsEyeTrackingDataValid = provider.IsEyeTrackingDataValid;
        IsEyeTrackingEnabled = provider.IsEyeTrackingEnabled;
        IsEyeTrackingEnabledAndValid = provider.IsEyeTrackingEnabledAndValid;
        GazeOrigin = provider.GazeOrigin;
        GazeDirection = provider.GazeDirection;
        HeadMovementDirection = provider.HeadMovementDirection;
        HeadVelocity = provider.HeadVelocity;
        Timestamp = provider.Timestamp.ToFileTime();
    }
}

public class HLEyeTracking : MonoBehaviour
{
    const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    APIToken token;

    void Start()
    {
        StartCoroutine(this.GetBearerToken());
    }

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
                if (GlobalStats.ALLOW_DEBUG_MSG)
                    Debug.Log(this.token.access_token);
            }
        }
        this.readyToSend = true;
    }

    IEnumerator SendDataEyeTracking(string jsonstr)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("entries", Encoding.ASCII.GetBytes(jsonstr), "txt", "application/octet-stream"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/eye", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);

        }
    }

    IEnumerator SendCurrentFrame(string jsonstr)
    {
        yield return this.SendDataEyeTracking(jsonstr);
        this.readyToSend = true;
    }

    void Update()
    {
        if (this.readyToSend)
        {
            var gazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
            if (gazeProvider != null)
            {
                EyeTrackingData data = new EyeTrackingData(gazeProvider);
                string jsonstr = JsonUtility.ToJson(data);
                this.readyToSend = false;
                StartCoroutine(this.SendCurrentFrame(jsonstr));
            }
        }
    }
}