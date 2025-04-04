using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DrawingGizmo : MonoBehaviour
{
    [SerializeField]
    GameObject active_gizmoVisualizer, other_gizmoVisualizer;

    private List<GameObject> gizmoCenterIndicators;
    // Start is called before the first frame update
    void Start()
    {
        gizmoCenterIndicators = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void VisualizeTaskGroup()
    {
        /* fetch list*/
        Dictionary<int, List<GlobalStats.ObjectTaskMapping>> gizmo = GlobalStats.getObjMappingList();

        /* destroy existing gizmo center if any */
        destroyGizmoCenters();

        /* visualize the gizmo */
        Dictionary<int, Vector3> sessionCenter = calculateSessionCenters(gizmo, "haha", true);

        
    }

    public void destroyGizmoCenters() {
        foreach (var giz in gizmoCenterIndicators) {
            Destroy(giz);
        }
        // clean the list
        gizmoCenterIndicators = new List<GameObject>();
    }

    public void drawGizmoCenters(string local_session, int session_id, Vector3 gizmo_center) {
        GameObject centerIndicator;
        if (session_id == GlobalStats.getActiveSession().task_id)
            centerIndicator = Instantiate(active_gizmoVisualizer, gizmo_center, Quaternion.identity);
        else
            centerIndicator = Instantiate(other_gizmoVisualizer, gizmo_center, Quaternion.identity);

        centerIndicator.name = "gizmo_" + session_id;
        centerIndicator.gameObject.transform.LookAt(Camera.main.transform);
        centerIndicator.transform.Rotate(0, 180, 0, Space.Self);
        centerIndicator.transform.GetChild(0).GetComponent<TextMeshPro>().text = local_session;
        gizmoCenterIndicators.Add(centerIndicator);
    }
    public Dictionary<int, Vector3> calculateSessionCenters(Dictionary<int, List<GlobalStats.ObjectTaskMapping>> gizmo, string task_name, bool drawcenter)
    {
        Dictionary<int, Vector3> sessionCenter = new Dictionary<int, Vector3>();
        foreach (KeyValuePair<int, List<GlobalStats.ObjectTaskMapping>> item in gizmo)
        {
            Vector3 c = new Vector3(0, 0, 0);
            foreach (var obj in item.Value)
            {
                c += obj.xyz_lefthandCoord;
            }
            c = c / item.Value.Count;

            if (drawcenter) {
                drawGizmoCenters(GlobalStats.getSessionName(item.Key), item.Key, c);
            }

            sessionCenter.Add(item.Key, c);
        }

        return sessionCenter;
    }

}
