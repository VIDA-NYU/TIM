using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HLStreamerManager : MonoBehaviour
{
    private HLStreaming streamer;

    void Start()
    {
        streamer = FindObjectOfType<HLStreaming>();
        streamer.StartStreaming();
    }

    void OnDestroy()
    {
        streamer.StopStreaming();
    }
}
