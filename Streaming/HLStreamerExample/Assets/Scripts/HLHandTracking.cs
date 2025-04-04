using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

[System.Serializable]
public class HandTrackingData
{
    public string left;
    public string right;

    public HandTrackingData(string l, string r)
    {
        left = l;
        right = r;
    }
}

public class HLHandTracking : MonoBehaviour
{
    const string SERVER = "s://api.ptg.poly.edu";
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
                Debug.Log(this.token.access_token);
            }
        }
        this.readyToSend = true;
    }

    IEnumerator SendDataHandTracking(string jsonstr)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("entries", Encoding.ASCII.GetBytes(jsonstr), "txt", "application/octet-stream"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/hand", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);

        }
    }

    IEnumerator SendCurrentFrame(string jsonstr)
    {
        yield return this.SendDataHandTracking(jsonstr);
        this.readyToSend = true;
    }

    void Update()
    {
        if (this.readyToSend)
        {
            HandTrackingData data = new HandTrackingData(getOneHand(Handedness.Left), getOneHand(Handedness.Right));
            string jsonstr = JsonUtility.ToJson(data);
            this.readyToSend = false;
            StartCoroutine(this.SendCurrentFrame(jsonstr));
        }
    }

    string getOneHand(Handedness hand)
    {
        MixedRealityPose[] jointPoses = new MixedRealityPose[ArticulatedHandPose.JointCount];
        for (int i = 0; i < ArticulatedHandPose.JointCount; ++i)
        {
            HandJointUtils.TryGetJointPose((TrackedHandJoint)i, hand, out jointPoses[i]);
        }

        ArticulatedHandPose pose = new ArticulatedHandPose();
        pose.ParseFromJointPoses(jointPoses, hand, Quaternion.identity, Vector3.zero);

        return pose.ToJson();
    }
}