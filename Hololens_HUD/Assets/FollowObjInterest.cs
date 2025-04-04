using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FollowObjInterest : MonoBehaviour
{

    [SerializeField]
    GameObject obj_of_interst;

    [SerializeField]
    private bool follow = true;

    [SerializeField]
    private AutoLookAT lookat;

    [SerializeField]
    private bool isLookAt = true;

    [SerializeField]
    private TextMeshPro msg_txt;
    // Start is called before the first frame update
    void Start()
    {
            
    }

    public void toggleFollow() {
        follow = !follow;
    }

    public void toggleLockAT() {
        isLookAt = !isLookAt;
        lookat.enabled = isLookAt;
    }

    // Update is called once per frame
    void Update()
    {
        if (follow)
        {
            this.transform.position = new Vector3(obj_of_interst.transform.position.x,
                obj_of_interst.transform.position.y+0.3f,
                obj_of_interst.transform.position.z); 
        }

        msg_txt.text = "Auto Follow: " + follow + ", Auto Rotation:" + isLookAt;
    }
}
