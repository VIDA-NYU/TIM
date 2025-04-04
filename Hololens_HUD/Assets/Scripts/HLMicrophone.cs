using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class HLMicrophone : MonoBehaviour
{
    const string SERVER = GlobalStats.serverAddress;
    APIToken token;

    AudioClip clip;
    float[] samples;
    byte[] buffer;

    int lastPos = 0;
    long streamPos = 0;
    bool readyToSend = false;
    string device = null;

    // Start is called before the first frame update
    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            this.device = Microphone.devices[0];
            this.clip = Microphone.Start(this.device, true, 10, 44100);
            this.samples = new float[0];
            this.buffer = new byte[0];
            StartCoroutine(this.GetBearerToken());
        }
        this.readyToSend = true;
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

    IEnumerator SendDataPOST(byte[] bytes)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("entries", bytes, "mic0", "application/octet-stream"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/mic0", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
        }
    }

    IEnumerator SendCurrentClip(int newPos, int numSamples)
    {
        Array.Resize(ref this.samples, numSamples);
        Array.Resize(ref this.buffer, 16 + numSamples * sizeof(float));
        Buffer.BlockCopy(BitConverter.GetBytes(this.clip.frequency), 0, this.buffer, 0, sizeof(int));
        Buffer.BlockCopy(BitConverter.GetBytes(this.clip.channels), 0, this.buffer, 4, sizeof(int));
        Buffer.BlockCopy(BitConverter.GetBytes(this.streamPos), 0, this.buffer, 8, sizeof(long));

        this.clip.GetData(this.samples, this.lastPos);
        Buffer.BlockCopy(this.samples, 0, this.buffer, 16, numSamples * sizeof(float));

        this.lastPos = newPos;
        this.streamPos += numSamples;

        yield return this.SendDataPOST(this.buffer);
        this.readyToSend = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.readyToSend && this.device != null)
        {
            int newPos = Microphone.GetPosition(this.device);
            int numSamples = (((newPos + this.clip.samples) - this.lastPos) % this.clip.samples) * this.clip.channels;
            if (numSamples > 0)
            {
                this.readyToSend = false;
                StartCoroutine(this.SendCurrentClip(newPos, numSamples));
            }
        }
    }
}