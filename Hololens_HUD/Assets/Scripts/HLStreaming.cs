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
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;
#endif

    private void Awake()
    {
#if ENABLE_WINMD_SUPPORT
        unityWorldOrigin = Microsoft.MixedReality.OpenXR.PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity) as Windows.Perception.Spatial.SpatialCoordinateSystem;
        Debug.Log("Fetched Unity coord system!!!");
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Windows Runtime Support enabled!!!");

        HLRSstreamer = new HLStreamer();

        // Activate desired sensors before initializing
        HLRSstreamer.ActivateMain();
        HLRSstreamer.ActivateDepth();

        // Initialize sensors
        HLRSstreamer.Initialize(unityWorldOrigin);
#endif
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if ENABLE_WINMD_SUPPORT
        if (!hasFocus) HLRSstreamer.ReleaseAllSensors();
#endif
    }

    public void StartStreaming()
    {
#if ENABLE_WINMD_SUPPORT
        HLRSstreamer.StartStreaming();
#endif
    }

    public void StopStreaming()
    {
#if ENABLE_WINMD_SUPPORT
        HLRSstreamer.StopStreaming();
#endif
    }
}
