using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTGAREyeTracking : MonoBehaviour
{
    [SerializeField]
    GameObject[] progressBarList;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AnimateEyeHover() {
        Debug.Log("Hover!!!");
    }

    public void ExpandMenu() {
        if(progressBarList.Length > 0)
        for (int i = 0; i < progressBarList.Length; i ++)
            progressBarList[i].SetActive(true);
    }
}
