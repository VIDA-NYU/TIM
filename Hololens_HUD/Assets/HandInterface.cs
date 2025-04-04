using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class HandInterface : MonoBehaviour
{
    // Start is called before the first frame update
    MixedRealityPose index;
    MixedRealityPose thumb;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out index)){
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out thumb)) {
               // GlobalStats.Debug(Vector3.Distance(index.Position, thumb.Position) +"");
                float dis = Vector3.Distance(index.Position, thumb.Position);
                if (dis < 0.02f)
                {
                    GlobalStats.Pause(); 
                    // system pause
                    
                }
            }
        }
    }
}
