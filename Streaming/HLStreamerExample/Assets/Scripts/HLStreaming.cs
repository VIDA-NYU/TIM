using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using HLStreamerUnityPlugin;
#endif

public class HLStreaming : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    HLStreamer HLRSstreamer;
#endif

    // Start is called before the first frame update
    void Start()
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Windows Runtime Support enabled!!!");

        HLRSstreamer = new HLStreamer();

        // Activate desired sensors before initializing
        HLRSstreamer.ActivateAll();

        // Initialize sensors and start streaming
        HLRSstreamer.Initialize();
        HLRSstreamer.StartStreaming();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if ENABLE_WINMD_SUPPORT
        if (!hasFocus) HLRSstreamer.ReleaseAllSensors();
#endif
    }
}
