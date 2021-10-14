using UnityEngine;

//Class used to manage the nameIcon for each clinician in the module 2
//Allow to rotate the icon such that it's always readable by the user
public class IconNameManagement : MonoBehaviour
{
    //Reference to the target of the icon: the camera
    [SerializeField]
    private Camera user;
    private Vector3 angle;

    // Start is called before the first frame update
    private void Start()
    {
        angle = this.transform.localEulerAngles; //retrieve the current angle of the icon
        if(user!=null)
        {
            this.transform.LookAt(user.transform,Vector3.up);//rotate the icon such that it looks at the camera
        }
        Vector3 temporalAngle = this.transform.localEulerAngles;//retrieve the angle after the rotation
        angle.y = temporalAngle.y-180; //set just the angle y from the angle before the rotation because we want just to rotate the icon around the y axis. We make temporalAngle-180 to assure that the name is readable (penser à être plus précis)
        this.transform.localEulerAngles = angle;
    }

    // Update is called once per frame
    // Same logic that in the start function
    private void Update()
    {
        angle = this.transform.localEulerAngles;
        if(user!=null)
        {
            this.transform.LookAt(user.transform,Vector3.up);
        }
        Vector3 temporalAngle = this.transform.localEulerAngles;
        angle.y = temporalAngle.y-180;
        this.transform.localEulerAngles = angle;
    }
}
