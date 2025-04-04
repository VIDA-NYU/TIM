using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GetRecipe : MonoBehaviour
{
    InputField outputArea;
    const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    APIToken token;
    //SliderController sliderController;
    //public GameObject progressBar;
    public string recipeId;
    void Start()
    {
        recipeId = RecipeController.currentRecipe;
        GameObject.Find("GetRecipeButton").GetComponent<Button>().onClick.AddListener(GetData);
        StartCoroutine(this.GetBearerToken());
        
        //sliderController = progressBar.GetComponent<SliderController>();
    }

    void GetData() => StartCoroutine(GetData_Coroutine());

    IEnumerator GetBearerToken()
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("username", "test"));
        formData.Add(new MultipartFormDataSection("password", "test"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/token", formData))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
            {
                this.token = APIToken.FromJSON(www.downloadHandler.text);
                if (GlobalStats.ALLOW_DEBUG_MSG)
                    Debug.Log(this.token.access_token);
                StartCoroutine(GetData_Coroutine());
            }
        }
        this.readyToSend = true;
    }

    IEnumerator GetData_Coroutine()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/recipes/" + recipeId))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
                Debug.Log("data:");
                string recipe_dic = www.downloadHandler.text;
               // sliderController.SetInfoRecipe(recipe_dic);


        }
    }

}
