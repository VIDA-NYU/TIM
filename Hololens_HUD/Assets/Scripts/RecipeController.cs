using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class RecipeController : MonoBehaviour
{
   public static string currentRecipe;
   const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    APIToken token;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(this.GetBearerToken());
        Invoke("SetPinwheelsRecipe", 3);
    }
    public void SetPinwheelsRecipe()
    {
        currentRecipe = "pinwheels";
        StartCoroutine(StartRecipeServer(currentRecipe));
    }
    public void SetCoffeeRecipe()
    {
        currentRecipe = "coffee";
        StartCoroutine(StartRecipeServer(currentRecipe));
    }
    public void SetMugcakeRecipe()
    {
        currentRecipe = "mugcake";
        StartCoroutine(StartRecipeServer(currentRecipe));
    }
    public void SetTourniquetTask()
    {
        currentRecipe = "tourniquet";
        StartCoroutine(StartRecipeServer(currentRecipe));
    }
    IEnumerator GetBearerToken()
    {
        //Debug.Log("Getting token");
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
          //      Debug.Log(this.token.access_token);
            }
        }
        this.readyToSend = true;
    }

    // Initializes recipe. This includes reasoning and perception.
    public IEnumerator StartRecipeServer(string recipeId)
    {
        Debug.Log("Start Recipe: " + recipeId);
        byte[] myData = System.Text.Encoding.UTF8.GetBytes(recipeId.ToString());
        using (UnityWebRequest www = UnityWebRequest.Put("http" + SERVER + "/sessions/recipe/" + recipeId, myData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success){
                Debug.Log(www.error);
            }
            else {
                Debug.Log("Initialization done!");
            }
        }
    }
}