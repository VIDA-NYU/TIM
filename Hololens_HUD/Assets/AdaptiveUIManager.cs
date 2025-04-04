using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdaptiveUIManager : MonoBehaviour
{

    [SerializeField]
    GameObject[] anim;

    private bool stepChanged = false;

    private int btn_unlock_ct = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 objCoord = GlobalStats.getCoordinateCenter(GlobalStats.obj);
        if (FoundObj(objCoord)) {
            GameObject.Find("tracking_obj").transform.position = objCoord;
            if (stepChanged) {
                // generate animation
                Instantiate(anim[0], GameObject.Find("tracking_obj").transform);
                //reset
                stepChanged = false;
            }
        }

        btn_unlock_ct += 1;

        if (btn_unlock_ct > 200)
        {
            GlobalStats.nextbt_lock = false;
            GlobalStats.prevbt_lock = false;
            btn_unlock_ct = 0;
        }

    }

    public void StepChanged() {
        stepChanged = true;
    }

    private bool FoundObj(Vector3 objCoord) {
        if (objCoord.x != 0 && objCoord.y != 0)
            return true;

        return false;
    }
}
