using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AutoFade : MonoBehaviour
{
    private bool fadeIn = false;
    private bool fadeOut = false;
    private bool slowFadeInAndOutFlag = false;
    private float lerpv = 0;
    private bool slowFadeOutFlag = false;
    private int fadeout_ct = 0;
    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        if (fadeIn) {
            this.transform.gameObject.SetActive(true);
            if(_fadeIn())
                // fade in complete
                fadeIn = false;
        }

        if (fadeOut) {
            if (_fadeOut())
            {
                // fade in complete
                fadeOut = false;
                this.transform.gameObject.SetActive(false);
            }
        }
   

        if (slowFadeInAndOutFlag)
        {
            GlobalStats.ErrorMsgLock();
            if (_fadeIn())
            {
                slowFadeInAndOutFlag = false;
                slowFadeOutFlag = true;
                fadeout_ct = 0;
                lerpv = 0;
               // slowFadeInAndOutFlag = false;
              //  fadeinCorrectionMenu();
            }
        }

        if (slowFadeOutFlag)
        {

            Color cr = this.transform.gameObject.GetComponent<Renderer>().material.color;
            lerpv +=0.01f;
            float v = 1-Mathf.Lerp(0, 1, lerpv);
            this.transform.gameObject.GetComponent<Renderer>().material.color = new Color(cr.r, cr.g, cr.b, v);

            if (v < 0.1f)
            {
                slowFadeOutFlag = false;
                GlobalStats.ErrorMsgLockReset();
            }
        }
    }


    private bool _fadeOut() {
        Color cr = this.transform.gameObject.GetComponent<Renderer>().material.color;
        lerpv += 0.01f;
        float v = 1 - Mathf.Lerp(0, 1, lerpv);
        this.transform.gameObject.GetComponent<Renderer>().material.color = new Color(cr.r, cr.g, cr.b, v);

        if (v < 0.1f)
            return true;
        return false;
    }

    private bool _fadeIn() {
        Color cr = this.transform.gameObject.GetComponent<Renderer>().material.color;
        lerpv += Time.deltaTime;
        float v = Mathf.Lerp(0, 1, lerpv);
        this.transform.gameObject.GetComponent<Renderer>().material.color = new Color(cr.r, cr.g, cr.b, v);

        if (v > 0.9f)
            fadeout_ct++;

        if (fadeout_ct > 50)
        {
            fadeout_ct = 0;
            return true;
        }

        return false;
    }

    public void fadeinCorrectionMenu() {
        GameObject m = GlobalStats.get_correction_menu();
        if (m)
        {
            m.SetActive(true);
            m.GetComponent<AutoFade>().slowFadeIn();
            GlobalStats.pauseError();
            slowFadeOutFlag = false;
        }
    }

    public void fadeOutCorrectionMenu() {
        GameObject m = GlobalStats.get_correction_menu();
        if (m)
        {
            m.SetActive(true);
            m.GetComponent<AutoFade>().slowFadeOut();
            GlobalStats.pauseError();
            fadeIn = false;
        }
    }

    public void slowFadeIn() {
        fadeOut = false;
        fadeIn = true;
        lerpv = 0;
    }
    public void slowFadeInAndOut()
    {
        slowFadeInAndOutFlag = true;
    }



    public void DisAppear() {
        fadeOut = true;
        fadeIn = false;
        lerpv = 0;
    }
    public void Appear() {
        fadeIn = true;
        fadeOut = false;
        lerpv = 0;
    }

    public void slowFadeOut() {
        fadeOut = true;
        fadeIn = false;
        lerpv = 0;
    }

    public void DisAppearAfterTime(float time) {
        StartCoroutine(_DisAppearAfterTime(time));
    }

    public IEnumerator _DisAppearAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

        DisAppear();
    }


}
