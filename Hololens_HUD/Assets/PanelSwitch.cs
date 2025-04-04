using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelSwitch : MonoBehaviour
{
    public PanelManager panelManager;
    public int panelIndex;
    private bool isPanelActive = false;

    public void TogglePanel()
    {
        isPanelActive = !isPanelActive;
        if (isPanelActive)
        {
            panelManager.ActivatePanel(panelIndex);
        }
        else
        {
            panelManager.DeactivatePanel(panelIndex);
        }
    }
}