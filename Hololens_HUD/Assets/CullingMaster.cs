using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CullingMaster : MonoBehaviour
{

    CullingGroup group;
   
    [SerializeField]
    Camera cullingCamera;

    BoundingSphere[] spheres;

    // Start is called before the first frame update
    void Start()
    {
        group = new CullingGroup();
        group.targetCamera = Camera.main;
        spheres = new BoundingSphere[1];
        spheres[0] = new BoundingSphere(Vector3.zero, 1f);
        group.SetBoundingSpheres(spheres);
        group.SetBoundingSphereCount(1);

    }

    // Update is called once per frame
    void Update()
    {
        spheres[0].position = this.transform.position;
        bool sphereIsVisible = group.IsVisible(0);
       // print("is visible::" + sphereIsVisible);
    }
}
