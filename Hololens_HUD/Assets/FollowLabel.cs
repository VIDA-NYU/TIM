using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowLabel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void followLabel(int labelID) {
        // assinging position
        Vector3 v = GlobalStats.getConsistentLabelPosition(labelID);
        this.gameObject.transform.position = v;
    }
}
