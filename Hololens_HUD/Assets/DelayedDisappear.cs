using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedDisappear : MonoBehaviour
{
    // Start is called before the first frame update
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void disappear(float delay) {
        this.transform.gameObject.SetActive(false);
    }
}
