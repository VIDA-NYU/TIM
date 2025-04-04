using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedActive : MonoBehaviour
{
    // Start is called before the first frame update
    private bool delayedshowhand = false;
    private int ct = 0;
    [SerializeField]
    GameObject child;
    void Start()
    {
    }

    public void delayedShowHand() {
        delayedshowhand = true;
    }

    public void hideHand() {
        ct = 0;
        delayedshowhand = false;
        child.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        if (delayedshowhand)
        {
            ct++;
            if (ct > 300)
                child.SetActive(true);
        }
    }
}
