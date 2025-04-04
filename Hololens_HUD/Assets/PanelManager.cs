using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    public List<GameObject> panels;

    public void ActivatePanel(int index)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == index);
        }
    }

    public void DeactivatePanel(int index)
    {
        if (index >= 0 && index < panels.Count)
        {
            panels[index].SetActive(false);
        }
    }
}