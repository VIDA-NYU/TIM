using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimControl : MonoBehaviour
{
    // Start is called before the first frame update
    Renderer r;
    void Start()
    {
        r = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        r.material.color = new Color(1, 1, 1, 0.5f);
    }


}
