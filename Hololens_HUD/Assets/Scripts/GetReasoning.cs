using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using DrawingUtils;
using TMPro;

[System.Serializable]
public class SimplificationInfo
{
    public string recipe;
    public int task_id;

    public string task_name;

    public int step_id;
    public string simplified;
}

[System.Serializable]
public class SimplificationData
{
    public SimplificationInfo[] result;
}


[System.Serializable]
public struct ObjectOfInterestReasoning
{
    public int id;
    public string name;
    public float[] pos;
}

[System.Serializable]
public struct ReasoningInfo
{
    public int task_id;
    public string task_name;
    public int total_steps;
    public int step_id;
    public string step_status;
    public string step_description;
    public bool error_status;
    public string error_description;
    public string error_type;
    public List<ObjectOfInterestReasoning> objects;
}

[System.Serializable]
public struct ReasoningOutputs
{
    public List<ReasoningInfo> active_tasks;
    public int[] inprogress_task_ids;
}

[System.Serializable]
public class ReasoningResult
{
    public ReasoningOutputs result;
}


// public struct SessionsInfo {
//     public int  session_id;
//     public static Dictionary<string, ReasoningInfo> dicReasoningInfo = new Dictionary<string, ReasoningInfo>();
// }

public class GetReasoning : MonoBehaviour
{
    // Start is called before the first frame update
    const string SERVER = GlobalStats.serverAddress;
    bool readyToSend = false;
    int delay_timer;
    bool readyToSendEntity = true;
    bool entities = true;
    bool recepe_first_time = true;

    private SliderController m_slider;
    APIToken token;
    public static ReasoningInfo reasoningInfo;
    private GameObject sys_step;
    private GameObject progress_val;
    private GameObject loader;
    private Dictionary<int, bool> description_displayed;

    public static Dictionary<string, ReasoningInfo> dicReasoningInfo = new Dictionary<string, ReasoningInfo>();

    private DescriptionManager dm;
    private int error_td = 200;
    private int error_td_ct = 0;
    private int totalErrorct = 0;
    public static int currentStepID;
    public static int currentSessionID;
    public static List<RecipeEntity> recipeEntities;

    [SerializeField]
    Material active_task_mat, normal_mat;
    [SerializeField]
    GameObject object_of_interest;
    [SerializeField]
    GameObject progress_template;
    //[SerializeField]
    //TextMeshPro debug;
    void Start()
    {
        StartCoroutine(this.GetBearerToken());
        description_displayed = new Dictionary<int, bool>();
        loader = GameObject.Find("reasoning_loading");
        if (loader)
            loader.SetActive(false);
        // initialize all step descriptions
        for (int i = 0; i < 10; i++)
            description_displayed.Add(i, false);


        delay_timer = 0;
        //GenerateTestData(2);
        //GlobalStats.ShowNotfMessage("hello", 2);
        // GenerateTestData(2, "Tortilla", 3);

        // GenerateTestObjectList();
        //VisualizeTaskSwitching();
        //  GetData();
    }

    private Vector3 ConvertRightHandedToLeftHandedVector(Vector3 rightHandedVector)
    {
        return new Vector3(rightHandedVector.x, rightHandedVector.y, -rightHandedVector.z);
    }

    void Process(List<ReasoningInfo> activeTasks, int[] inprogressIDs, SimplificationData simplificationData)
    {

        GlbalUIController gl = GameObject.Find("3DUI_main").GetComponent<GlbalUIController>();
        // clean active sessions
        GlobalStats.clearActiveSessions();
        if (activeTasks.Count == 1)
        {
            reasoningInfo = activeTasks[0];
                        // update the display

            // all progress bar are created with functions to switch tasks (as in progress), and we will determine which ones are active later
            gl.ManageProgressBar(reasoningInfo);
            
            // text siplification
            string simplifiedText = GetSimplifiedTextByKey(simplificationData, reasoningInfo.task_id, reasoningInfo.step_id);

            if (!string.IsNullOrEmpty(simplifiedText)){
                gl.updateInstruction(reasoningInfo.step_id, simplifiedText);
            }else{
                gl.updateInstruction(reasoningInfo.step_id, reasoningInfo.step_description);
            }

            // gl.updateInstruction(reasoningInfo.step_id, reasoningInfo.step_description);
            gl.UpdateProgress(reasoningInfo);
            string object_label = reasoningInfo.objects[0].name;
            Vector3 pos = new Vector3(reasoningInfo.objects[0].pos[0], reasoningInfo.objects[0].pos[1], reasoningInfo.objects[0].pos[2]);
            pos = ConvertRightHandedToLeftHandedVector(pos);

            object_of_interest.transform.position = pos;
            object_of_interest.transform.Translate(new Vector3(0, 0.03f, 0));
            object_of_interest.transform.GetChild(0).GetComponent<TextMeshPro>().text = reasoningInfo.task_name;
           // debug.text = reasoningInfo.task_id+","+ reasoningInfo.task_name+","+ pos.x + "," + pos.y + "," + pos.z;

            GlobalStats.setActiveTask(reasoningInfo.task_id, reasoningInfo.task_name);
            GlobalStats.setActiveSession(reasoningInfo);
            GlobalStats.setActiveStepID(reasoningInfo.step_id);
            GlobalStats.setSessionName(reasoningInfo.task_id, reasoningInfo.task_name);
            GlobalStats.activeSessions.Add(reasoningInfo.task_id);

            // error handling
            if (reasoningInfo.error_description != "")
            {
                gl.triggerError(reasoningInfo);
            }
        }
        else
        {
            //multiple active sessions
            for (int i = 0; i < activeTasks.Count; i++)
            {
                gl.ManageProgressBar(activeTasks[i]);
                GlobalStats.activeSessions.Add(activeTasks[i].task_id);
            }

            gl.updateInstruction(0, "Please determine what is the next step");

        }

        /* update the ranking and display */
        RankActiveSessions(activeTasks.Count);

        /* delete redundant task sessions */
        UpdateSessionListWithServer(inprogressIDs);

        /* reorder*/
        OrderTasks();
    }


    private void OrderTasks() {
        // update display
        List<GameObject> gm = GlobalStats.GetSessionUIList();
        for (int i = 0; i < gm.Count; i++)
        {
            gm[i].transform.position = progress_template.transform.position;
            gm[i].transform.Translate(new Vector3(0, -0.05f * i, 0));

        }

    }
    private void UpdateSessionListWithServer(int[] ids){
        List<GameObject> gm = GlobalStats.GetSessionUIList();
        List<GameObject> objectsToRemove = new List<GameObject>();
        foreach (var g in gm)
        {
            string[] parts = g.transform.name.Split('_');
            int m_id = 0;
            if (parts.Length > 1 && int.TryParse(parts[1], out int result))
            {
                m_id = result;
            }
            else
            {
                Debug.Log("error! invalid UI name for progress bar");
                m_id = -1;
            }

            List<int> idList = new List<int>(ids);
            if (!idList.Contains(m_id)) {
                objectsToRemove.Add(g);
            }

        }

        foreach (GameObject dgm in objectsToRemove) {
            gm.Remove(dgm);
            Destroy(dgm);
        }

        GlobalStats.SetSessionUIList(gm);
    }

    private List<GameObject> resetAllMenu(List<GameObject> gm) {
        // make all game objects task switching and reset parent
        Transform p = GameObject.Find("ProgressBarsParent").transform;
        for (int i = 0; i < gm.Count; i++) {
            Transform t = gm[i].transform.Find("Progress_bar_bk");
            SwitchTask m_task_manager = t.GetComponent<SwitchTask>();
            t.GetComponent<SwitchTask>().enabled = true;
            m_task_manager.is_disabled = false;
            t.GetComponent<OpenAndClose>().isEnabled = false;
            t.GetComponent<OpenAndClose>().disable = true;
            t.GetComponent<Renderer>().material = normal_mat;
            gm[i].transform.SetParent(p);
        }

        return gm;
    }
    public string GetSimplifiedTextByKey(SimplificationData simplificationData, int task_id, int step_id)
    {
        if (simplificationData.result == null)
            return null;

        foreach (var item in simplificationData.result)
        {
            if (item.task_id == task_id && item.step_id == step_id)
            {
                return item.simplified;
            }
        }
        return null; // or some default value if not found
    }


    void update_progress_rank_helper(int active_task_id, bool multi_active_case)
    {
        bool rank_success = false;

        List<GameObject> gm = GlobalStats.GetSessionUIList();
        Transform D3UI = GameObject.Find("ActiveBarsParent").transform;
        /* reset all menu as non-active */

        // find index
        int index = -1;
        for (int i = 0; i < gm.Count; i++)
            if (gm[i].transform.name == "UI_" + active_task_id)
            {
                index = i;
                rank_success = true;
                break;
            }

        if (rank_success)
        {
            GameObject newgm = gm[index];
            // update newgm
            newgm.transform.GetChild(3).GetComponent<Renderer>().material = active_task_mat;


            // we only change to open and close when there is only one active menu
            if (!multi_active_case)
            {
                ActivateOpenandCloseAction(newgm);
                newgm.transform.SetParent(D3UI);
            }

            gm.RemoveAt(index);
            gm.Insert(0, newgm);
        }

        GlobalStats.SetSessionUIList(gm);
    }

    void ActivateOpenandCloseAction(GameObject gm) {
        Transform t = gm.transform.Find("Progress_bar_bk");
        SwitchTask m_task_manager = t.GetComponent<SwitchTask>();
        m_task_manager.is_disabled = true;
        t.GetComponent<SwitchTask>().enabled = false;

        /* disable active open and close, this will leave the switching function working*/
        t.GetComponent<OpenAndClose>().isEnabled = true;
        t.GetComponent<OpenAndClose>().disable = false;
    }

    void RankActiveSessions(int active_task_count)
    {
        GlobalStats.SetSessionUIList(resetAllMenu(GlobalStats.GetSessionUIList()));
        if (active_task_count == 1)
        {
            // look for the active session
            update_progress_rank_helper(GlobalStats.getActiveTaskID(), false);

        }
        else
        {
            for (int i = 0; i < GlobalStats.activeSessions.Count; i++)
                update_progress_rank_helper(GlobalStats.activeSessions[i], true);
        }

      
    }

    public void GenerateTestData(int step)
    {

        string step1 = "{\"active_tasks\":[{\"task_id\":0,\"task_name\":\"pinwheels\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Place tortilla on cutting board.\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":12,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]},{\"task_id\":1,\"task_name\":\"quesadilla\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Place tortilla on cutting board.\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":7,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]}],\"inprogress_task_ids\":[0,1]}";
        string step2 = "{\"active_tasks\":[{\"task_id\":1,\"task_name\":\"quesadilla\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Place tortilla on cutting board.\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":7,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]}],\"inprogress_task_ids\":[1]}";
        string step3 = "{\"active_tasks\":[{\"task_id\":4,\"task_name\":\"lovely cake\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Hello lets make a cake!\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":7,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]}],\"inprogress_task_ids\":[1,4]}";
        string step4 = "{\"active_tasks\":[{\"task_id\":5,\"task_name\":\"Wow2\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Wow2 description here\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":12,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]},{\"task_id\":6,\"task_name\":\"Haha3\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"this is description for haha\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":7,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]}],\"inprogress_task_ids\":[1,4,5,6]}";
        string step5 = "{\"active_tasks\":[{\"task_id\":5,\"task_name\":\"Wow2\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Wow2 description here\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":12,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]}],\"inprogress_task_ids\":[1,4,5]}";
        string str = "";
        //string str = "[{\"session_id\":"+ session_id + ",\"task_id\":\""+ task_id + "\",\"step_id\":"+step_id+",\"step_status\":\"IN_PROGRESS\",\"step_description\":\"Use a butter knife to spread a layer of Nutella onto tortilla.\",\"error_status\":false,\"error_description\":\"\"}]";
        //string str = "{\"active_tasks\":[{\"task_id\":0,\"task_name\":\"pinwheels\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Place tortilla on cutting board.\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":12,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]},{\"task_id\":1,\"task_name\":\"quesadilla\",\"step_id\":1,\"step_status\":\"NOT_STARTED\",\"step_description\":\"Place tortilla on cutting board.\",\"error_status\":false,\"error_description\":\"\",\"total_steps\":7,\"objects\":[{\"id\":0,\"name\":\"tortilla\",\"pos\":[-0.2149151724097291,-0.4343880843796524,-0.6208099189217009]}]}],\"inprogress_task_ids\":[0,1]}";
        //string step2 = ""
        //ReasoningOutputs jsonString = parseReasoningOutput(str);
        /*
        List<ReasoningInfo> activeTasks = data.active_tasks;
        int[] inprogressTaskIDs = data.inprogress_task_ids;
        Debug.Log("New output form Reasoning");
        Debug.Log(data);
        Debug.Log(activeTasks);
        */
        if (step == 1)
            str = step1;
        else if (step == 2)
            str = step2;
        else if (step == 3)
            str = step3;
        else if (step == 4)
            str = step4;
        else if (step == 5)
            str = step5;
        else
            str = "";
        ReasoningOutputs data = parseReasoningOutput(str);
        List<ReasoningInfo> activeTasks = data.active_tasks;
        int[] inprogressTaskIDs = data.inprogress_task_ids;
        // ReasoningInfo data = parseReasoningOutput(jsonStringReasoningTest); // data toy example
        string tsimerStr = "{}";

        SimplificationData simplificationData = JsonUtility.FromJson<SimplificationData>(tsimerStr);

        Process(activeTasks, inprogressTaskIDs, simplificationData);





        // reasoningInfo = data[0];
        // GlbalUIController gl = GameObject.Find("3DUI_main").GetComponent<GlbalUIController>();
        //if (data[0].total_steps == 0) 
        //Debug.Log("TOTAL_STEPS GOT 0, expected a non-zero positive integer");

        //gl.ManageProgressBar(data[0]);
        //gl.updateInstruction(data[0]);
        //gl.UpdateProgress(data[0]);


        //GlbalUIController.session_info = data[0];
        //// Active Session
        //GlbalUIController.active_session_id = currentSessionID;
        //GlobalStats.setActiveSessionUIID(currentSessionID);
        //GlobalStats.setActiveTaskID(data[0].task_id);
        //GlobalStats.setActiveSession(data[0]);
        //GlobalStats.setActiveStepID(data[0].step_id);
    }
    void GetData() => StartCoroutine(GetData_Coroutine());

    public ReasoningOutputs parseReasoningOutput(string jsonString)
    {
        //Debug.Log(jsonString);
        ReasoningResult data = JsonUtility.FromJson<ReasoningResult>("{\"result\":" + jsonString + "}")!;
        return data.result;
    }

    // Create a dictionary of sessions.
    // public void updateReasoningDictionary(ReasoningInfo newReasoningInfo, string sessionID){
    //     if(dicReasoningInfo.ContainsKey(sessionID)){
    //         newReasoningInfo[recipe_first_time] = false;
    //         dicReasoningInfo[sessionID] = newReasoningInfo;
    //     } else {
    //         newReasoningInfo[recipe_first_time] = true;
    //         dicReasoningInfo.Add(sessionID,  newReasoningInfo);
    //     }
    // }

    public void UpdateReasoning(int step_id) => StartCoroutine(UpdateReasoningServer(step_id));
    public void UpdateReasoningTask(int task_id, string task_name) => StartCoroutine(UpdateReasoningServerTask(task_id, task_name));
    public void resetReasoning() => StartCoroutine(resetReasoningServer());
    public void pauseReasoning(string status) => StartCoroutine(pauseReasoningServer(status)); // status is an string which can take two values: 'start' or 'resume'
    IEnumerator GetBearerToken()
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("username", "test"));
        formData.Add(new MultipartFormDataSection("password", "test"));
        //   formData.Add(new MultipartFormDataSection("task_id", GlobalStats.getActiveTaskID()));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/token", formData))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
            {
                this.token = APIToken.FromJSON(www.downloadHandler.text);
                Debug.Log(this.token.access_token);

            }
        }
        this.readyToSend = true;
    }

    IEnumerator GetData_Coroutine()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/data/reasoning:check_status"),
           wwwTSimer = UnityWebRequest.Get("http" + SERVER + "/data/tsimer:simplification") 
        )
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();

            wwwTSimer.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return wwwTSimer.SendWebRequest();


            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
                if (GlobalStats.ALLOW_DEBUG_MSG)
            {
                Debug.Log("Reasoning object:");
                Debug.Log(www.downloadHandler.text);
            }
            string jsonString = www.downloadHandler.text;
            string jsonStringTSimer = wwwTSimer.downloadHandler.text;

            // string jsonStringReasoningTest = "[{\"session_id\": 1, {\"probability\": 0.7, \"task_id\": \"Pinwheels\", \"total_steps\": 12, \"step_id\": 3, \"step_status\": \"IN_PROGRESS\", \"step_description\": \"wipe off knife with a paper towel\", \"error_status\": \"True\", \"error_description\": \"Errors detected in the step\"}]";
            try
            {
                SimplificationData simplificationData = JsonUtility.FromJson<SimplificationData>(jsonStringTSimer);

                ReasoningOutputs data = parseReasoningOutput(jsonString);
                List<ReasoningInfo> activeTasks = data.active_tasks;
                int[] inprogressTaskIDs = data.inprogress_task_ids;
                // ReasoningInfo data = parseReasoningOutput(jsonStringReasoningTest); // data toy example

                Process(activeTasks, inprogressTaskIDs, simplificationData);
                // currentStepID = data.step_id;
                // GlobalStats.currentStep = currentStepID;

                // // Active Session
                // currentSession = dicReasoningInfo[currentSessionID];

                // if(!dicReasoningInfo.ContainsKey(currentSessionID)){ // first time recipe
                //     progress_val = GameObject.Find(string.Concat("Progree_value_pivot", currentSessionID));
                //     sys_step = GameObject.Find(string.Concat("title_steps", currentSessionID));
                // } else {
                //     sys_step.GetComponent<TextMeshPro>().text = "Step " + ((int)(data.step_id) + 1) +" | " + currentSession.total_steps;
                // }

                // //if (sys_title_desc)
                // //   sys_title_desc.GetComponent<TextMeshPro>().text = "" + data.step_description;
                // // Go from one step to another or setting it for the first time
                // if (dicReasoningInfo.ContainsKey(currentSessionID) && data.step_id != dicReasoningInfo[currentSessionID].step_id || !dicReasoningInfo.ContainsKey(currentSessionID))
                // {
                //     if (GameObject.Find(string.Concat("AR_Interface_FP", currentSessionID)))
                //     {
                //         loader.SetActive(false);
                //         dm = GameObject.Find(string.Concat("AR_Interface_FP", currentSessionID)).GetComponent<DescriptionManager>();
                //         dm.ShowDescription((int)(data.step_id), data.step_description);

                //         /* update progress bar*/
                //         float step_size = 2.46135f / GlobalStats.totalSteps;
                //         progress_val.transform.localScale = new Vector3(step_size * (int)data.step_id,
                //                                                         0.197421f,
                //                                                         progress_val.transform.localScale.z);
                //     //progress_val.transform.position += new Vector3( (float)(0.0096 / 2 * (int)data.step_id), 0, 0);
                //     recepe_first_time = false;
                //     }
                // }

                // /* the below is for animation */
                // /* call activation event */
                // if ((int)data.step_id != GlobalStats.stepID) {
                //     /* step change deteceted */
                //     if (GameObject.Find(string.Concat("AR_Interface_FP", currentSessionID)))
                //         /* provoke change step event */
                //         GameObject.Find(string.Concat("AR_Interface_FP", currentSessionID)).GetComponent<AdaptiveUIManager>().StepChanged();
                // }

                // /* assign to global stats*/    
                // GlobalStats.stepID = (int)(data.step_id);
                // GlobalStats.activeSession = currentSessionID;
            }
            /* the below is for animation */
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
        this.readyToSend = true;
    }



    // Update the step index from Hololens
    public IEnumerator UpdateReasoningServer(int step_id)
    {
        //Debug.Log(" Update Reasoning: " + step_id);
        //string jsonData = string.Format("{{\"step_id\":\"{0}\",\"task_id\":{1}}}", 3, 5);

        //byte[] myData = System.Text.Encoding.UTF8.GetBytes(step_id.ToString()+"5");
        //string session_id = currentSessionID.ToString();
        string session_id = GlobalStats.getActiveTaskID()+"";
        string task_step = step_id.ToString() + "&" + session_id;
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("entries", Encoding.ASCII.GetBytes(task_step), "txt", "application/octet-stream"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/arui:change_step", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (GlobalStats.ALLOW_DEBUG_MSG)
            {
                if (www.result != UnityWebRequest.Result.Success)
                    Debug.Log(www.error);
                else
                    Debug.Log("Step updated!");
                GlobalStats.nextbt_lock = false;
                GlobalStats.prevbt_lock = false;
            }

        }
    }

    // Update the task_index and task_id (task name) from Hololens
    public IEnumerator UpdateReasoningServerTask(int task_id, string task_name)
    {
        //Debug.Log(" Update Reasoning: " + step_id);
        //string jsonData = string.Format("{{\"step_id\":\"{0}\",\"task_id\":{1}}}", 3, 5);

        string task_info = task_name + "&" + task_id.ToString();

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("entries", Encoding.ASCII.GetBytes(task_info), "txt", "application/octet-stream"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/arui:change_task", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (GlobalStats.ALLOW_DEBUG_MSG)
            {
                if (www.result != UnityWebRequest.Result.Success)
                    Debug.Log(www.error);
                else
                    Debug.Log("Task updated!");
            }

        }
    }

    // Reset a session from the Hololens
    public IEnumerator resetReasoningServer()
    {
        string message = "reset";

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("entries", Encoding.ASCII.GetBytes(message), "txt", "application/octet-stream"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/arui:reset", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (GlobalStats.ALLOW_DEBUG_MSG)
            {
                if (www.result != UnityWebRequest.Result.Success)
                    Debug.Log(www.error);
                else
                    Debug.Log("Reset session completed!");
            }

        }
    }

    // Pause a session from the Hololens
    // Status can take two values: start or resume. If the 'start' string is sent, this means the system needs to be pause. If the 'resume' string is sent then the system should go frrom pause status to working status.
    public IEnumerator pauseReasoningServer(string status)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("entries", Encoding.ASCII.GetBytes(status), "txt", "application/octet-stream"));
        using (UnityWebRequest www = UnityWebRequest.Post("http" + SERVER + "/data/arui:pause", formData))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (GlobalStats.ALLOW_DEBUG_MSG)
            {
                if (www.result != UnityWebRequest.Result.Success)
                    Debug.Log(www.error);
                else
                    Debug.Log("Pause session!");
            }

        }
    }


    IEnumerator GetRecipeEntities_Coroutine()
    {
        Debug.Log("Call RecipeEntities");
        using (UnityWebRequest www = UnityWebRequest.Get("http" + SERVER + "/data/reasoning:entities"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + this.token.access_token);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                //Debug.Log("RecipeEntities www.error");
                Debug.Log(www.error);
            }
            else
            {
                string jsonString = www.downloadHandler.text;
                //Debug.Log("RecipeEntities " + jsonString);
                if (!(string.IsNullOrEmpty(jsonString)))
                {
                    try
                    {
                        RecipeEntityResults data = JsonUtility.FromJson<RecipeEntityResults>("{\"result\":" + jsonString + "}")!;
                        recipeEntities = data.result;
                        this.entities = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("RecipeEntities Trycatch exception");
                        Debug.Log(ex.Message);
                        this.entities = true;
                    }
                }
            }
        }
        this.readyToSendEntity = true;
        this.readyToSend = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.readyToSend)
        {
            this.readyToSend = false;

            StartCoroutine(GetData_Coroutine());
            if (this.entities && this.readyToSendEntity)
            {
                this.readyToSendEntity = false;

                StartCoroutine(GetRecipeEntities_Coroutine());
            }
        }

        delay_timer += 1;
    }
}
