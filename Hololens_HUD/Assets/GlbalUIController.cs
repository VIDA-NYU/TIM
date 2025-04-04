using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SceneSystem;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlbalUIController : MonoBehaviour
{

    [SerializeField]
    private GameObject progress_bar_template;
    // Start is called before the first frame update

    [SerializeField]
    private GameObject err_mini;

    [SerializeField]
    private GameObject err_mini_back;

    [SerializeField]
    private GameObject notf_mini_back;

    [SerializeField]
    private GameObject notf_parent;

    [SerializeField]
    private GameObject Blue_UI_main;

    [SerializeField]
    private GameObject red_back_main;

    [SerializeField]
    private TextMeshPro sys_debug;


    [SerializeField]
    private GameObject main_UI_left;

    [SerializeField]
    private GameObject main_UI_Right;

    [SerializeField]
    private GameObject main_UI_pause;

    [SerializeField]
    private GameObject correction_menu;

    [SerializeField]
    private GameObject pause_menu;

    [SerializeField]
    private TextMeshPro tim_text;

    [SerializeField]
    private GameObject error_mini_txt;

    [SerializeField]
    private GameObject[] ctrl_buttons;

    [SerializeField]
    private GameObject UserMsgCenter;

    public GameObject progressBarsParent;

    [SerializeField]
    private GetReasoning reasoning_manager;

    [SerializeField]
    private SliderController step_manager;

    private float lastUIYPosition = 0f;
    private float yOffset = 0.001f;

    public static ReasoningInfo session_info;
    public static int active_session_id;

    private ReasoningInfo m_session;

    void Start()
    {
        GlobalStats.set_error_main(err_mini);
        GlobalStats.set_error_mini_backcolor(err_mini_back);
        GlobalStats.set_red_back_main(red_back_main);
        GlobalStats.set_sys_debug(sys_debug);
        GlobalStats.set_UI_left(main_UI_left);
        GlobalStats.set_UI_right(main_UI_Right);
        GlobalStats.set_UI_pause(main_UI_pause);
        GlobalStats.set_UI_correction_menu(correction_menu);
        GlobalStats.setGlobalUIController(this);
        reasoning_manager.resetReasoning();
        step_manager.rewind();
        // testing code
        /*triggerError1();
        
        //GenerateProgressCount(123);

        m_session.probability = 0.7f;
        m_session.task_id = "Pinwheels2";
        m_session.step_id = 3;
        m_session.session_id = 1;
        m_session.total_steps = 12;
        m_session.step_status = "IN_PROGRESS";
        m_session.step_description = "wipe off knife with a paper towel";
        m_session.error_status = true;
        m_session.error_description = "Errors detected in the step";

        GenerateProgressCount(m_session);
        UpdateProgress(m_session.session_id, m_session.step_id, (m_session.step_id * 1.0f / m_session.total_steps));
       // updateInstruction(m_session);
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void show_main_error() {
        red_back_main.SetActive(true);
    }

    public void hide_main_error() {
        red_back_main.SetActive(false);
    }
    public void resumeSystem() {
        pause_menu.SetActive(false);
        // update text
        tim_text.text = "Say \"Hi Tim\" for help";
        for (int i = 0; i < ctrl_buttons.Length; i++)
            ctrl_buttons[i].SetActive(true);

        reasoning_manager.pauseReasoning("resume");
        GlobalStats.ShowNotfMessage("System resumed", 2, 5);
    }

    public void pauseSystem() {
        pause_menu.SetActive(true);
        // update text
        tim_text.text = "Say \"Hi Tim\" to resume";
        for (int i = 0; i < ctrl_buttons.Length; i++)
            ctrl_buttons[i].SetActive(false);

        reasoning_manager.pauseReasoning("start");
        GlobalStats.ShowNotfMessage("System paused now.", 2,5);
    }

    public void resumeError1() {
        err_mini_back.GetComponent<AutoFade>().slowFadeOut();
        error_mini_txt.SetActive(false);
        correction_menu.GetComponent<AutoFade>().slowFadeOut();
    }

    public void triggerError(ReasoningInfo data) {
        GlobalStats.ErrorHandle(data.error_status, data.error_description, data.error_type);
    }


    public void triggerError1() {
        ReasoningInfo data = GlobalStats.GenerateError(0);
        GlobalStats.ErrorHandle(data.error_status, data.error_description, data.error_type);
    }

    /* generate progress bars, all bars will be by default non-active (inprogress) bars*/
    public void ManageProgressBar(ReasoningInfo m_session)
    {
        // rectify with server first

        if (!GlobalStats.SessionExists(m_session.task_id))
        {
            GameObject m = Instantiate(progress_bar_template, progress_bar_template.transform);
            // update transform and position 
            m.transform.SetParent(progressBarsParent.transform);
            m.SetActive(true);

            m.name = "Progress"+"_"+ m_session.task_id;
            
            /* disable the active menu*/
            SwitchTask m_task_manager = m.transform.Find("Progress_bar_bk").GetComponent<SwitchTask>();
            m_task_manager.updateTaskName(m_session.task_id, m_session.task_name);
            m_task_manager.is_disabled = false;

            /* disable active open and close, this will leave the switching function working*/
            m.transform.Find("Progress_bar_bk").GetComponent<OpenAndClose>().isEnabled = false;
            m.transform.Find("Progress_bar_bk").GetComponent<OpenAndClose>().disable = true;

            //float newYPosition = lastUIYPosition - 0.005f;
            m.transform.position = progress_bar_template.transform.position;
            //m.transform.position = new Vector3(progress_bar_template.transform.position.x, newYPosition, progress_bar_template.transform.position.z);
            m.transform.localScale = progress_bar_template.transform.localScale;
            m.transform.name = "UI_" + m_session.task_id;
            int total_sessions = GlobalStats.GetSessionUIList().Count;

            // generate the progress bars
            m.transform.Translate(new Vector3(0, -0.05f * total_sessions, 0));
            //lastUIYPosition = newYPosition;


            // update the progress bar content
            if (!m.transform.Find("Quad").Find("title_steps"))
                Debug.Log("Unable to find title_steps, required for this demo");
            else
                // TOOD: you can update the name of the task here, as an initial step.
                m.transform.Find("Quad").Find("title_steps").GetComponent<TextMeshPro>().text = m_session.task_name;

            GlobalStats.addSessionUIList(m);
        }
    }

    /* this function needs better implementation */
    public void updateInstruction(int step_id, string step_desc)
    {
        if (step_id != 0)
            GameObject.Find("step_desc_3d_title").GetComponent<TextMeshPro>().text = "Step " + step_id;
        else
            GameObject.Find("step_desc_3d_title").GetComponent<TextMeshPro>().text = "Require user intervention";
        
        GameObject.Find("desc_step").GetComponent<TextMeshPro>().text = step_desc;
    }

    public void UpdateProgress(ReasoningInfo data)
    {
        int session_id = data.task_id;
        int step_id = data.step_id;
        float completion = 0;
        if (data.total_steps != 0)
            completion = step_id * 1.0f / data.total_steps;

        foreach (GameObject uiobj in GlobalStats.GetSessionUIList())
        {
            if (uiobj.name == "UI_" + session_id)
            {
                Transform t = uiobj.transform.Find("Progree_value_pivot");
                /* do update here */
                if (!t)
                    Debug.Log("Unable to find progressbar, required for this demo");
                else
                    // TOOD: you can update the name of the task here, as an initial step.
                    t.localScale = new Vector3(completion * 2.35f, t.localScale.y, t.localScale.z);
            }
        }
    }

    public void showNotMessageHelper(string m, float d, float intial_wait = 0) {
        StartCoroutine(ShowNotfMessageCoroutine(m, d, intial_wait));
    }

    IEnumerator ShowNotfMessageCoroutine(string notfMessage, float duration, float initial_wait = 0)
    {
        yield return new WaitForSeconds(initial_wait);
        notf_parent.SetActive(true);
        GameObject.Find("notf_min_msg").GetComponent<TextMeshPro>().text = notfMessage;
        notf_mini_back.GetComponent<AutoFade>().slowFadeIn();
        yield return new WaitForSeconds(duration);
        notf_mini_back.GetComponent<AutoFade>().slowFadeOut();
        yield return new WaitForSeconds(0.3f);
        notf_parent.SetActive(false);
    }

    public void ResetSystem()
    {
        // Call the async method without awaiting it
        CallYourAsyncMethod();
    }

    private async void CallYourAsyncMethod()
    {
        // Now you can await your async Task method
        await restartAsync();

        // Do something after the task
        Debug.Log("Reset Complete");
    }


    public async Task restartAsync() {
        reasoning_manager.resetReasoning();
        IMixedRealitySceneSystem sceneSystem = MixedRealityToolkit.Instance.GetService<IMixedRealitySceneSystem>();

        GlobalStats.ResetSessionUIList();
        await sceneSystem.UnloadContent("PTG_V2");

        // Additively load a single content scene
        await sceneSystem.LoadContent("PTG_V2");

        step_manager.rewind();
        // Additively load a set of content scenes
        //await sceneSystem.UnloadContent(new string[] { "PTG_hub" });
        //await sceneSystem.LoadContent("PTG_hub");
    }

    public void activateTIM() {
        GameObject.Find("TimSpeech").GetComponent<TimSpeech>().ToggleActivation();
    }

    public void deactivateTIM() {
        StartCoroutine(GlobalStats.delayedDisappear(UserMsgCenter, 0.1f));
    }
}
