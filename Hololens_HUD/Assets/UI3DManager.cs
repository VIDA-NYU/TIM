using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI3DManager : MonoBehaviour
{

    private float disappear_ct = 3.5f;
    private float current_ct = 0;
    private bool begin_fade = false;
    private float distance = 0;

    [SerializeField]
    private AutoFollow af;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       // if (begin_fade)
          //  fade();

     }

    private void fade() {
        current_ct += Time.deltaTime;
       // if (current_ct > disappear_ct)
       // {
            // disappear the mainbody
            this.transform.gameObject.SetActive(false);
            // stop fade
         //   begin_fade = false;
       // }
    }

    private void follow() { 
        
    }

    public void beginFade(float ftimer = 3.5f) {
        disappear_ct = ftimer;
        begin_fade = true;
    }

    public void ToggleFollow() {
        af.enabled = !af.enabled;
    }
}
