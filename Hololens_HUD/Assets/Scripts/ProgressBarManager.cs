using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProgressBarManager : MonoBehaviour
{
    public GameObject progressBarsParent;
    public GameObject activeBarsParent;

    public void ResetAllProgressBars()
    {
        foreach (Transform child in progressBarsParent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in activeBarsParent.transform)
        {
            Destroy(child.gameObject);
        }

        // Clear any additional data structures tracking progress bars
        GlobalStats.clearSessionUIList();

        // Reset the 3D step description title
        GameObject.Find("step_desc_3d_title").GetComponent<TextMeshPro>().text = "";

        GameObject errorMini = GameObject.Find("error_mini");
        if (errorMini != null)
        {
            TextMeshPro errMsg = errorMini.GetComponentInChildren<TextMeshPro>();
            if (errMsg != null)
            {
                errMsg.text = ""; // Clear the text
            }
            errorMini.SetActive(false); // Deactivate 
        }

        GameObject.Find("bki_blue_UI").SetActive(false);

        // Reset any locks or flags related to error messaging
        //GlobalStats.ErrorMsgUpdateLock = false;
    }

}

