using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoLookAT : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    bool flip = true;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.LookAt(Camera.main.transform);
        if (flip)
            this.transform.Rotate(new Vector3(0, 180, 0));
    }
}
