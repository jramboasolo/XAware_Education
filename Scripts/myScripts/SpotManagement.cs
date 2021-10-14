using System;
using UnityEngine;

//Class to manage the spot movement

public class SpotManagement : MonoBehaviour
{

    private GameObject sceneManagement,target;

    [HideInInspector]
    //Reference to the selected clinician
    public GameObject clinicianSelected {set;private get;}

    //Reference to the selected spot for the movement
    public GameObject spot {set;private get;}

    // Start is called before the first frame update
    private void Start()
    {
        clinicianSelected = null;
        sceneManagement = GameObject.Find("SceneManagementObject");
        target = GameObject.Find("upper_shieldScene");
    }

    //Function called when the user clicks on one clinician in the scene 
    public void Selectedclinician(GameObject clinician)
    {
        if(!sceneManagement.GetComponent<GameMangement>().beginModule2)
        {
            return;
        }
        
        if(clinicianSelected !=null)
        {
            try
            {
                clinicianSelected.GetComponent<ClinicienMovementManagement>().Unselected();
            }
            catch(Exception)
            {
                
            }
            try
            {
                clinicianSelected.GetComponent<ClinicienMovementManagement>().IsMoved = false;
            }
            catch
            {
                
            } 
        }
        clinicianSelected = clinician;
        try
        {
            clinicianSelected.GetComponent<ClinicienMovementManagement>().Selected();
        }
        catch(Exception)
        {
            
        }
    }

    public void UnseledtedClinician()
    {
        clinicianSelected = null;
    }

    //Function that move the selected clincian 
    public void MoveClinician()
    {
        Vector3 angle;
        Vector3 positionSpot;
        Vector3 positionClinician;
        if(clinicianSelected!=null)
        {
            clinicianSelected.GetComponent<ClinicienMovementManagement>().UpdatePreviousPosition(); //update the previous position
            angle = clinicianSelected.transform.localEulerAngles;
            positionSpot = spot.transform.position;
            positionClinician = clinicianSelected.transform.position;
            positionClinician.x = positionSpot.x;
            positionClinician.z = positionSpot.z;
            clinicianSelected.transform.position = positionClinician;
            if(target != null)
            {
                clinicianSelected.transform.LookAt(target.transform,Vector3.up); //Rotate the clinician as he looks at the target
                Vector3 temporalAngle = clinicianSelected.transform.localEulerAngles; //Retrieve the angle after the rotation
                angle.y = temporalAngle.y; //Set just the y and z angle to avoid the clinician to rotate around the x axis
                angle.z = temporalAngle.z; // because LookAt use the clinician pivot as reference to rotate the clincian
                clinicianSelected.transform.localEulerAngles = angle;
            }
        }      
    }

    //Function used to move the selected clinician to the current user's position and orientation
    //Function called through a voice command 'Move clinician'
    public void VoiceMoveClinicien(Camera mainCamera)
    {
        if(clinicianSelected != null)
        {
            Vector3 ClinicianPosition = clinicianSelected.transform.position;
            Vector3 targetPosition = mainCamera.transform.position;
            ClinicianPosition.x = targetPosition.x;
            ClinicianPosition.z = targetPosition.z;
            clinicianSelected.transform.position = ClinicianPosition;
            Vector3 targetAngle = mainCamera.transform.eulerAngles;
            Vector3 ClinicianAngle = clinicianSelected.transform.eulerAngles;
            ClinicianAngle.y = targetAngle.y;
            ClinicianAngle.z = targetAngle.z;
            clinicianSelected.transform.eulerAngles = ClinicianAngle;
        }
    }

    //Function called when the user wants to let enter the clinician in the scene (in the case where the user was taken outside the clinician and wants to undo this action)
    public void Inside()
    {
        clinicianSelected?.GetComponent<ClinicienMovementManagement>().Inside(); // A?.B: B is not evaluate if A is null
    }

    //Function called when the user wants to put ouside the selected clinician
    public void Outside()
    {
        clinicianSelected?.GetComponent<ClinicienMovementManagement>().Outside();
    }
}
