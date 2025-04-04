using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DescriptionManager : MonoBehaviour
{

    [SerializeField]
    private GameObject UI3D;
    [SerializeField]
    private GameObject step_desc_3d_title;
    [SerializeField]
    private GameObject step_desc_3d;
    [SerializeField]
    Texture[] coffee;

    [SerializeField]
    Texture[] mug_cake;

    [SerializeField]
    Texture[] pinwheels;

    [SerializeField]
    Texture[] tourniquet;

    [SerializeField]
    UI3DManager UIManager;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public void UpdateDescriptionFigure(int stepid) {
        if (GlobalStats.currentRecepe == "Pinwheels")
            if (stepid <= pinwheels.Length)
                step_desc_3d.GetComponent<Renderer>().material.mainTexture = pinwheels[stepid];
        if (GlobalStats.currentRecepe == "Mug Cake")
            if (stepid <= mug_cake.Length)
                step_desc_3d.GetComponent<Renderer>().material.mainTexture = mug_cake[stepid];
        if (GlobalStats.currentRecepe == "Pour-over Coffee")
            if (stepid <= coffee.Length)
                step_desc_3d.GetComponent<Renderer>().material.mainTexture = coffee[stepid];
        if (GlobalStats.currentRecepe == "Tourniquet")
            if (stepid <= tourniquet.Length)
                step_desc_3d.GetComponent<Renderer>().material.mainTexture = tourniquet[stepid];
    }

    public void ShowDescription(int stepid, string title_desc) {
        UI3D.SetActive(true);
        //UI3D.transform.SetParent(Camera.main.transform);
        // string[] recepe = new string["Pour-over Coffee", "Pinwheels","Mug Cake"];
        UpdateDescriptionFigure(stepid);
       

        if (GlobalStats.totalSteps != 0)
            step_desc_3d_title.GetComponent<TextMeshPro>().text = "Step: " + (stepid +1) + " | " + GlobalStats.totalSteps + " " + title_desc; 
        else
            step_desc_3d_title.GetComponent<TextMeshPro>().text = "Step: " + (stepid + 1) + " | " + title_desc;
        UIManager.beginFade();
    }
}
