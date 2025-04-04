using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARUIMain : MonoBehaviour
{

    
    // Start is called before the first frame update
    void Start()
    {
        GlobalStats.red_back = GameObject.Find("bk_red");
        if (GlobalStats.red_back)
            GlobalStats.red_back.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    
    }
}
