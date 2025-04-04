using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothLookAT : MonoBehaviour
{
    Vector3 lastUpdatedPos = new Vector3(0, 0, 0);
    bool newUpdates = false;
    public void ArrowFollow(Vector3 v)
    {
        newUpdates = true;
        lastUpdatedPos = v;
    }
    // Start is called before the first frame update
    void Start()
    {
    //    ArrowFollow(new Vector3(-2.0f, -1.1f, -1.0f));
    }

    // Update is called once per frame
    void Update()
    {
        if (newUpdates)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lastUpdatedPos), Time.deltaTime);
        }
    }
}
