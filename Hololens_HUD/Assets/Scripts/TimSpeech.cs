using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.WitAi;
using Facebook.WitAi.Lib;
using Facebook.WitAi.TTS.Utilities;
using UnityEngine.UI;
using TMPro;
using FindingUtils;

public class TimSpeech : MonoBehaviour
{
    [SerializeField] private Wit wit;
    [SerializeField] private TTSSpeaker speaker;
    [SerializeField] private TextMeshPro OutputText;
    [SerializeField] private GameObject UserMsgCenter;
    [SerializeField] private GetObjectFromMemory ObjectLocator;
    [SerializeField] private GameObject spatial_arrow;
    [SerializeField] private AutoFade fadeManager;
    [SerializeField] private ChatMessage chat;
    [SerializeField] private SliderController step_transition;
    [SerializeField] private GetReasoning reasoning_manager;


    public void OnResponse(WitResponseNode response)
    {
        if (!string.IsNullOrEmpty(response["text"]))
        {
           
            Debug.Log("I heard: " + response["text"]);
            if (GameObject.Find("User_msg_txt"))
                GameObject.Find("User_msg_txt").GetComponent<TextMeshPro>().text = "\"" + response["text"] + "\"";
            
            var intent = WitResultUtilities.GetIntentName(response);
 

            OutputText.text = "\"" + intent + "\"";
            StartCoroutine(GlobalStats.delayedDisappear(UserMsgCenter, 3));

            if (intent == "")
            {
                StartCoroutine(chat.SendMessage(response["text"]));
                // SayPhrase("Sorry, I don't understand");
            }
            //else if (intent == "show_object")
            //{
            //    var objectString = WitResultUtilities.GetAllEntityValues(response, "object:object");
            //    if (objectString.Length > 0)
            //    {
            //        SayPhrase("OK, show the location of " + objectString[0]);
            //        spatial_arrow.SetActive(true);
            //        OutputText.GetComponent<TextMeshPro>().text = "OK, show the location of " + objectString[0];
            //        ObjectLocator.updateTargetLabel(objectString[0]);
            //        ObjectLocator.TimResponded();
            //        fadeManager.DisAppearAfterTime(10);
            //    }
            //}
            else if (intent == "pause_system")
            {
                GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().pauseSystem();
            }
            else if (intent == "resume_system")
            {
                GameObject.Find("3DUI_main").GetComponent<GlbalUIController>().resumeSystem();
            }
            else if (intent == "switch_task")
            {
                Debug.Log("switching task!");
            }
            else if (intent == "prev_step")
            {
                step_transition.prevStep();
            }
            else if (intent == "next_step") {
                step_transition.UpdateProgress();
            }
            else
            {
                StartCoroutine(chat.SendMessage(response["text"]));
            }
        }
        else
        {
            UserMsgCenter.SetActive(false);
            Debug.Log("Empty voice message");
        }
    }


    public void OnError(string error, string message)
    {
        Debug.Log($"Error: {error}\n\n{message}");
        UserMsgCenter.SetActive(false);
    }

    public void ToggleActivation()
    {
        if (!wit.Active)
        {
            Debug.Log("Tim activated");
            wit.Activate();
            UserMsgCenter.SetActive(true);
        }
    }

    public void DisplayPhrase(string text){
        GameObject gm = GameObject.Find("User_msg_txt");
        if (gm != null) {
            gm.GetComponent<TextMeshPro>().text = "\"" + text + "\"";
        }
        
    }
    public void SayPhrase(string text)
    {
        if (!speaker.IsLoading && !speaker.IsSpeaking)
        {
            speaker.Speak(text);
        }
    }
}