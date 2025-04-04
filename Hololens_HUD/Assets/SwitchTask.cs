using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchTask : MonoBehaviour
{

    public int target_task_id;
    public string target_task_name;
    public bool is_disabled;
    // Start is called before the first frame update
    void Start()
    {
        is_disabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateTaskName(int task_id, string task_name) {
        target_task_name = task_name;
        target_task_id = task_id;
    }

    public void updateTask() {
        if (target_task_name != "" && !is_disabled)
            GlobalStats.changeTask(target_task_id, target_task_name);    
    }

}
