using System.Collections;
using System.Collections.Generic;
using DrawingUtils;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using TMPro;

[System.Serializable]
public struct SessionInfo {
    public int  total_sessions;
    public static List<ReasoningInfo> reasoningInfo;
}

public static class GlobalStats 
{
    public static int stepID;
    public static Vector3 obj_center;
    public static Vector3 obj_top;
    public static QrCodeResult obj;
    public static bool ALLOW_DEBUG_MSG = false;
    public static GameObject red_back;
    public static bool nextbt_lock = false;
    public static bool prevbt_lock = false;
    public static bool instruction_needs_refresh = false;
    public static int totalSteps = 0;
    public static int currentStep = 0;

    public static int totalSessions = 0;
    public static int currentSession = 0;
    public static SessionInfo sessionInfo;

    public static Dictionary<string, ReasoningInfo> dicReasoningInfo = new Dictionary<string, ReasoningInfo>();

    public static string currentRecepe = "";
    public static List<Vector3> lastUpdatedObjects = new List<Vector3>();
    public static bool HiTimObjectResponded = false;
    public const string serverAddress = "://<REPLACE_WITH_SERVER_ADDRESS>:7890";
    private static GameObject NE_red_back;
    private static GameObject Err_mini_back;
    private static GameObject main_UI_Left;
    private static GameObject main_UI_Right;
    private static GameObject main_UI_Pause;
    private static GameObject main_UI_correction_menu;
    private static TextMeshPro sys_debug;
    private static Queue<string> error_stack = new Queue<string>();
    private static GameObject sys_alert_txt;
    private static int last_update = 0;
    private static int last_update_td = 10;
    private static bool triggerCD = false;
    private static bool PausingErrorMsg = false;
    private static bool ErrorMsgUpdateLock = false;
    private static Vector3 consist_label_pos = new Vector3(0, 0, 0);
    private static int active_task_id;
    private static string active_task_name;
    //private static string active_task_id;
    private static int active_step_id;
    private static GlbalUIController uicontroller;
    private static ReasoningInfo active_session;
    private static List<GameObject> SessionUIList = new List<GameObject>();
    private static Dictionary<string, int> objIDtoSessionMap = new Dictionary<string, int>();
    private static Dictionary<int, List<ObjectTaskMapping>> objMappingTempList = new Dictionary<int, List<ObjectTaskMapping>>();
    private static Dictionary<int, List<ObjectTaskMapping>> objMappingList = new Dictionary<int, List<ObjectTaskMapping>>();


    private static Dictionary<int, string> SessionManager = new Dictionary<int, string>();

    public static List<int> activeSessions = new List<int>();

    public static void clearActiveSessions() {
        activeSessions = new List<int>();
    }
    //public static GetReasoning getReasoning;
    public struct ObjectTaskMapping
    {
        public int objectid;
        public int session_id;
        public string object_name;
        public Vector3 xyz_lefthandCoord;
        
    }

    // task_id is the recipe name
    // task_index represents the session_id
    public static void changeTask(int task_id, string task_name) {
        GameObject.Find("HLUnityScriptHolder").GetComponent<GetReasoning>().UpdateReasoningTask(task_id, task_name);
    }


    public static void SetObjIDtoSessionMap(Dictionary<string, int> d) {
        objIDtoSessionMap = new Dictionary<string, int>(d);

    }

    public static void setGlobalUIController(GlbalUIController gm) {
        uicontroller = gm;
    }

    public static int SessionIDLookup(string obj_name) {
        if (objIDtoSessionMap.ContainsKey(obj_name))
            return objIDtoSessionMap[obj_name];
        else
            return 0;
    }
    public static void updateObjMapEntry(ObjectInMemoryInfo obj, Vector3 coord) {
        int session_id = GlobalStats.SessionIDLookup(obj.label);
        ObjectTaskMapping instance = new ObjectTaskMapping
        {
            objectid = obj.id,
            session_id = session_id,
            object_name = obj.label,
            xyz_lefthandCoord = coord
        };

        if(!objMappingTempList.ContainsKey(session_id))
            objMappingTempList[session_id] = new List<ObjectTaskMapping>();

        objMappingTempList[session_id].Add(instance);
    }

    public static void updateObjMap() {
        objMappingList = new Dictionary<int, List<ObjectTaskMapping>>(objMappingTempList);
        objMappingTempList = new Dictionary<int, List<ObjectTaskMapping>>();
    }

    public static Dictionary<int, List<ObjectTaskMapping>> getObjMappingList() {
        return objMappingList;
    }

    public static void AddSession(int session_id) {
        if (totalSessions > 0){
            sessionInfo.total_sessions = sessionInfo.total_sessions + 1;
        } else {
            sessionInfo.total_sessions = 1;
            // sessionInfo.reasoningInfo.session_id;
        }

    }

    public static void setActiveSession(ReasoningInfo s) {
        active_session = s;
    }

    public static ReasoningInfo getActiveSession() {
        return active_session;
    }
    
    public static void setActiveTask(int task_id, string task_name) {
        active_task_id = task_id;
        active_task_name = task_name;
    }

    public static string getActiveTaskName() {
        return active_task_name;
    }

    public static int getActiveTaskID() {
        return active_task_id; 
    }
    public static void setActiveStepID(int step_id) {
        active_step_id = step_id;
    }

    public static int getActiveStepID() {
        return active_step_id;
    }

    public static void ResetSessionUIList() {
        SessionUIList = new List<GameObject>();
    }


    public static void SetSessionUIList(List<GameObject> gm) {
        SessionUIList = new List<GameObject>(gm);
    }
    public static List<GameObject> GetSessionUIList() {
        return SessionUIList;
    }

    public static void setSessionName(int task_id, string task_name) {
        SessionManager[task_id] = task_name;
    }

    public static string getSessionName(int task_id) {
        if (SessionManager.ContainsKey(task_id))
            return SessionManager[task_id];
        else
            return "Generic Objects";
    }
    public static string getTaskNameFromTaskID(int task_id) {
        return SessionManager[task_id];
    }
    public static void addSessionUIList(GameObject session_UI_gameobject) {
        SessionUIList.Add(session_UI_gameobject);
    }
    public static bool SessionExists(int session_id) {

        foreach (GameObject uiobj in SessionUIList)
        {
            if (uiobj.name == "UI_" + session_id) 
            //SessionUIList.Add();
            return true;
        }
        return false;
    }

    public static void ErrorMsgLock() {
        ErrorMsgUpdateLock = true;
    }

    public static void ErrorMsgLockReset() {
        ErrorMsgUpdateLock = false;
    }

    public static void pauseError() {
        PausingErrorMsg = true;
    }

    public static void resumeErrorMsg() {
        PausingErrorMsg = false;
    }
    public static void set_error_main(GameObject err_main) {
        NE_red_back = err_main;
    }

    public static Vector3 getConsistentLabelPosition(int labelid) {
       return consist_label_pos;
       
    }
    public static void set_error_mini_backcolor(GameObject err_mini_back)
    {
        Err_mini_back = err_mini_back;
    }

    public static void set_red_back_main(GameObject red_back_main)
    {
        red_back = red_back_main;
    }

    public static void set_UI_left(GameObject g)
    {
        main_UI_Left = g;
    }
    public static void set_UI_right(GameObject g)
    {
        main_UI_Right = g;
    }

    public static void set_UI_correction_menu(GameObject g)
    {
        main_UI_correction_menu = g;
    }

    public static void set_UI_pause(GameObject g) {
        main_UI_Pause = g;    
    }
    public static void Pause() {
        main_UI_Right.SetActive(false);
        main_UI_Left.SetActive(false);
        main_UI_Pause.SetActive(true);
    }

    public static GameObject get_correction_menu() {
        return main_UI_correction_menu;
    }

    public static void set_sys_debug(TextMeshPro sysdebug) {
        sys_debug = sysdebug;
    }

    public static void Debug(string msg) {
        sys_debug.text = msg;
    }

    public static Vector3 getCoordinateCenter(QrCodeResult obj) {
        if (obj == null)
            return new Vector3(0, 0, 0);    

            return new Vector3(obj.result[0].xyz_center[0],
                               obj.result[0].xyz_center[1],
                               obj.result[0].xyz_center[2]);
    }

    public static IEnumerator delayedDisappear(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        obj.gameObject.SetActive(false);
        // Code to execute after the delay
    }


    public static ReasoningInfo GenerateError(int error=-1) {


        string[] error_list = { "Slip", "Lapse", "No User", "NO_ERROR", "NO_ERROR", "NO_ERROR", "NO_ERROR" };
        string[] descript_list = { "Incorrect Step 1", "Folding Wrong", "No User", "NO_ERROR", "NO_ERROR", "NO_ERROR", "NO_ERROR" };

        int rn = 0;

        if (error == -1)
            rn = UnityEngine.Random.Range(0, error_list.Length);
        else
            rn = error;

        ReasoningInfo s = new ReasoningInfo();
        s.error_description =descript_list[rn];
        s.error_status = true;
        s.step_id = 1;
        s.error_type = error_list[rn];
        return s;
    }

    private static bool CD()
    {
        if(last_update > last_update_td)
        {
            // unlock cd
            last_update = 0;
            triggerCD = false;
            return false;
        }
        // still cooling down!
        last_update += 1;
        return true;
    }

    private static string ErrorHandleHelper(string e_type)
    {
        error_stack.Enqueue(e_type);

        // queue manage
        if (error_stack.Count > 10)
            error_stack.Dequeue();

        var l = error_stack.ToList();

        List<string> mostFrequent = error_stack.ToList<string>();

        var groupsWithCounts = from s in mostFrequent
                       group s by s into g
                       select new
                       {
                           Item = g.Key,
                           Count = g.Count()
                       };

        var groupsSorted = groupsWithCounts.OrderByDescending(g => g.Count);
        string mostFrequest = groupsSorted.First().Item;

        return mostFrequest;
    }

    /* categorize error based on the desc*/
    public static void ErrorHandle(bool err, string err_desc, string e_type) {
        //based on frequency, type

        // CD
        // make a cd algo
        // use a queue system
        //get frequency
        // use a buffer to avoid over call
        // NS = None Essential
        // CD is activated only if a recent call is triggered

        if (GlobalStats.PausingErrorMsg)
            return;

        if (triggerCD)
            if (CD())
                return;


        //confidence
        //enough error is detected
        string filtered_e_type = ErrorHandleHelper(e_type);
        if (filtered_e_type != "NO_ERROR"){
            ShowErrorMessage(1, filtered_e_type, err_desc);
            triggerCD = true;
        }

        
        /*
        float added_float = (err == true ? 1.0f : -1.0f);

        error_stack.Enqueue(added_float);

        if (error_stack.Count > 10)
        {
            // calculate error //
            float average = error_stack.Sum();

            error_stack.Dequeue();

            if (average > 2)
                return true;
            else
                return false;    
        }

        return false;
           */
    }

   
    public static void ShowErrorMessage(int msg_importance, string e_type, string message) {
        /* essential message*/
        if (msg_importance == 0)
        {
            if (red_back)
            {
                red_back.SetActive(true);
                red_back.transform.position = new Vector3(red_back.transform.position.x, 0, red_back.transform.position.z);
            }
            sys_alert_txt = GameObject.Find("sys_alert_txt");
            sys_alert_txt.GetComponent<TextMeshPro>().text = message;
        }
        else if (msg_importance == 1) {
            // None Essential Message
            // block exists
            if (GlobalStats.ErrorMsgUpdateLock)
                return;

            if (NE_red_back)
            {
                NE_red_back.SetActive(true);
                // NE_red_back.transform.position = new Vector3(NE_red_back.transform.position.x, 0.665f, NE_red_back.transform.position.z);
                GameObject.Find("err_min_msg").GetComponent<TextMeshPro>().text = message;
                Err_mini_back.GetComponent<AutoFade>().slowFadeInAndOut();
                //GameObject.Find("err_min_msg").SetActive(false);
            }
            
        }
    }

    public static void HideErrorMessage() {
        GameObject red_back = GameObject.Find("bk_red");

        if (red_back) {
            red_back.SetActive(false);
        }

        if (NE_red_back)
            NE_red_back.SetActive(false);

        GlobalStats.pauseError();

    }

    // Method to remove a progress bar from the list by task ID
    public static void removeSessionUIList(string taskId)
    {
        SessionUIList.RemoveAll(progressBar => progressBar.name == "UI_" + taskId);
    }

    // Method to clear the list of progress bars
    public static void clearSessionUIList()
    {
        SessionUIList.Clear();
    }

    public static void ShowNotfMessage(string notfMessage, int duration, float initial_wait)
    {
        uicontroller.showNotMessageHelper(notfMessage, duration, initial_wait);
    }

}
