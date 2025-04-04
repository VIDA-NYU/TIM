using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using TMPro;

[System.Serializable]
public class RecipeInfo
{
    public string id;
    public string name;
    public List<string> ingredients;
    public List<string> tools;
    public List<string> instructions;
    public List<string> steps;
}
public class SliderController : MonoBehaviour
{
    public Text valueText;
    int progress = 0;
    //public Slider slider;
    public int maxSliderValue  = 10;
    RecipeInfo recipe;
    TextMeshPro outputStepArea;
    Text recipeTitleText;
    private GetReasoning getReasoning;
    public GameObject reasoningGameObject;

    [SerializeField] private TimSpeech tim;
    [SerializeField] private GameObject progress_val;
    [SerializeField] GameObject loader;
    [SerializeField] DescriptionManager dmanager;
    void Start()
    {
        //slider.maxValue = maxSliderValue;
        outputStepArea = GameObject.Find("step_desc_3d_title").GetComponent<TextMeshPro>();
        recipeTitleText = GameObject.Find("RecipeTitleText").GetComponent<Text>();
        getReasoning = GameObject.Find("HLUnityScriptHolder").GetComponent<GetReasoning>();

    }
    public void OnSliderChanged(float value)
    {
        valueText.text = value.ToString();
    }
    public void UpdateMaxValue(int newMaxValue)
    {
        GlobalStats.totalSteps =  newMaxValue;
    }

    public void rewind() {
        GameObject.Find("HLUnityScriptHolder").GetComponent<GetReasoning>().UpdateReasoning(1);
    }
    public void UpdateProgress()
    {
        if (!GlobalStats.nextbt_lock)
        {
            GlobalStats.nextbt_lock = true;
            loader.SetActive(true);
            progress = GlobalStats.getActiveStepID();
            progress = progress + 1;
            int max_steps = GlobalStats.getActiveSession().total_steps;
            if (progress > max_steps)
                progress = max_steps;
            //slider.value = progress;
            //update slider value
            // progress_val.transform.localScale = new Vector3(0.009694027f * progress, 0.0200598f, 0.2f);
            GlobalStats.instruction_needs_refresh = true;
            GameObject.Find("HLUnityScriptHolder").GetComponent<GetReasoning>().UpdateReasoning(progress);
            // UpdateTextInstruction(progress);
        }

    }

    public void prevStep() {
        if (!GlobalStats.prevbt_lock)
        {
            GlobalStats.prevbt_lock = true;
            loader.SetActive(true);
            progress = GlobalStats.getActiveStepID();

            progress = progress - 1;
            if (progress < 0)
                progress = 0;
            //slider.value = progress;
            //update slider value
            // progress_val.transform.localScale = new Vector3(0.009694027f * progress, 0.0200598f, 0.2f);
            //UpdateTextInstruction(progress);
            GlobalStats.instruction_needs_refresh = true;
            GameObject.Find("HLUnityScriptHolder").GetComponent<GetReasoning>().UpdateReasoning(progress);
        }
    }

    //public void UpdateTextInstruction(int step)
    //{
      //  progress = step;
       // outputStepArea.text = "Step: " + step + " | " + GlobalStats.totalSteps + " " +  this.recipe.instructions[step];
    //}
    public void UpdateProgressTim()
    {
        UpdateProgress();
        tim.SayPhrase("OK, go to next step");

    }
    public void SetInfoRecipe(string jsonString)
    {
        //Debug.Log("Recipe");
       // Debug.Log(jsonString);
        RecipeInfo data = JsonUtility.FromJson<RecipeInfo>(jsonString)!;
        //Debug.Log(data);
        //Debug.Log(data.name);
        //Debug.Log(data.instructions.Count);
        int totalSteps = data.instructions.Count;

        GlobalStats.currentRecepe = data.name;

        dmanager.UpdateDescriptionFigure(GlobalStats.currentStep);
        //Debug.Log(totalSteps);
        //UpdateMaxValue(totalSteps-1);
        UpdateMaxValue(totalSteps);
        //recipeTitleText.text = data.name;
        this.recipe = data;
        //outputStepArea.text = "Step: " + progress + " | " + GlobalStats.totalSteps + " " +  this.recipe.instructions[progress]; // Display first step.
    }

}
