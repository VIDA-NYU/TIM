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

namespace HLHandTracking
{
    [Serializable]
    public class HandTrackingData
    {
        public string left;
        public string right;
        public long t;

        public HandTrackingData(string l, string r)
        {
            t = System.DateTime.Now.ToFileTime();
            left = l;
            right = r;
        }
    }


    public class HLHandTracking : MonoBehaviour
    {
        private static readonly TrackedHandJoint[] Joints = Enum.GetValues(typeof(TrackedHandJoint)) as TrackedHandJoint[];
        public static int JointCount { get; } = Joints.Length;

        [Serializable]
        internal struct ArticulatedHandPoseItem
        {
            private static readonly string[] jointNames = Enum.GetNames(typeof(TrackedHandJoint));

            public string joint;
            public MixedRealityPose pose;

            public TrackedHandJoint JointIndex
            {
                get
                {
                    int nameIndex = Array.FindIndex(jointNames, IsJointName);
                    if (nameIndex < 0)
                    {
                        Debug.LogError($"Joint name {joint} not in TrackedHandJoint enum");
                        return TrackedHandJoint.None;
                    }
                    return (TrackedHandJoint)nameIndex;
                }
                set { joint = jointNames[(int)value]; }
            }

            private bool IsJointName(string s)
            {
                return s == joint;
            }

            public ArticulatedHandPoseItem(TrackedHandJoint joint, MixedRealityPose pose)
            {
                this.joint = jointNames[(int)joint];
                this.pose = pose;
            }
        }

        [Serializable]
        internal class ArticulatedHandPoseDictionary
        {
            public ArticulatedHandPoseItem[] items = null;

            public void FromJointPoses(MixedRealityPose[] jointPoses)
            {
                items = new ArticulatedHandPoseItem[JointCount];
                for (int i = 0; i < JointCount; ++i)
                {
                    items[i].JointIndex = (TrackedHandJoint)i;
                    items[i].pose = jointPoses[i];
                }
            }
        }

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
                   // Debug.Log(this.token.access_token);
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

            var dict = new ArticulatedHandPoseDictionary();
            dict.FromJointPoses(jointPoses);
            return JsonUtility.ToJson(dict, true);
        }
    }
}