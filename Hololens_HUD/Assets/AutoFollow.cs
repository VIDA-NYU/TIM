using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoFollow : MonoBehaviour
{
    [SerializeField]
    float autoFollowSpeed = 5;

    [SerializeField]
    float delayThreshold = 0;
    // Start is called before the first frame update

    [SerializeField]
    float z_factor = 1;

    [SerializeField]
    Vector3 obj_offset = new Vector3(0, 0, 0);


    [SerializeField]
    bool isDelayed = false;

    [SerializeField]
    float nearDistanceToUser = 0.4f;

    [SerializeField]
    float farDistanceToUser = 1.1f;


    float delayct = 0;
    Vector3 sphericalDistance = new Vector3(0, 0, 0);

    bool begin_follow = false;

    private Vector3 p_main_localpos = new Vector3(0, 0, 0);
    private Vector3 main_localpos = new Vector3(0, 0, 0);
    private bool follow_begin = false;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        float camera_to_obj_dist = Vector3.Distance(this.transform.position, Camera.main.transform.position);

        main_localpos = this.transform.localPosition;
        //if (gameObject.name == "3DUI")
        //  Debug.Log(camera_to_obj_dist);

        /* follow distance */
        if (!isDelayed)
        {
            Follow();
        }
        else
        {

            /* fix this part, because the distance */
            if (camera_to_obj_dist > farDistanceToUser || camera_to_obj_dist < nearDistanceToUser)
            {
                delayct += 1;
            }

            /* calculate angle */
            Vector3 v1 = this.transform.position - Camera.main.transform.position;
            Vector3 v2 = Camera.main.transform.forward;
            float a = Vector3.Angle(v1, v2);

            /* angle based reset */
            if (a > 45)
                delayct += 1;
            ;
            if (delayct > delayThreshold)
            {
                /* trigger follow */
                Follow();

                if (!follow_begin && Vector3.Distance(main_localpos, p_main_localpos) > 0.01f)
                    //Debug.Log(Vector3.Distance(main_localpos, p_main_localpos)<0.001f);
                    follow_begin = true;

                if (follow_begin && Vector3.Distance(main_localpos, p_main_localpos) < 0.001f)
                {
                    delayct = 0;
                    follow_begin = false;
                }
                /* follow stop, with resolution within 10cm, here 0.05 means 5cm */
                    //if (camera_to_obj_dist > (z_factor - 0.05f) && camera_to_obj_dist < (z_factor + 0.05f) && a < 2.0f)
                    //if (a < 2.0f)
                    //  delayct = 0;
            }
        }

        p_main_localpos = main_localpos;
    }


    void getSphericalDistance()
    {
        sphericalDistance = Camera.main.transform.position + Camera.main.transform.forward;
    }

    void Follow()
    {
        float t = Time.deltaTime * autoFollowSpeed;
        //Vector3 finalPos = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z) + Vector3.forward * z_factor;
        // if you want to preview the z in the editor, make sure the z_factor is equivalent to z values in the editor.
        //Vector3 finalPos = (Camera.main.transform.position + obj_offset) + (Camera.main.transform.forward * z_factor);
        Vector3 finalPos = (Camera.main.transform.position) + (Camera.main.transform.forward * z_factor)+ obj_offset;


        // finalPos += new Vector3(0, this.transform.position.y, 0);
        this.transform.position = Vector3.Lerp(this.transform.position, finalPos, t);
    }
}
