using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Facebook.WitAi;

public class OpenAndClose : MonoBehaviour
{
    public GameObject toggleTarget;
    public bool isEnabled = true;
    public bool disable = false;
    [SerializeField] private Wit wit;

    public void ButtonClicked(){
        if (disable)
            return;

        isEnabled = !isEnabled;
        toggleTarget.SetActive(isEnabled);
        SyncStateWithTarget();
    }

    public void hideTim() {
        wit.Deactivate();
    }

    public void SyncStateWithTarget() {
        isEnabled = toggleTarget.activeSelf;
    }
}
