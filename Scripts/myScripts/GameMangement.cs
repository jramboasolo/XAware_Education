using System;
using TMPro;
using UnityEngine;

//Class to manage the game, show the pop up with the information on the exposure during the application at the end of the training
//Start the module 2 and show all the clinicians with their personal materials
public class GameMangement : MonoBehaviour
{
    //Bool to check if the module has already begun
    public bool beginModule2{private set;get;}

    //GameObject to reference the menu because, when the application launches, the menu is deactivated by Vuforia (target lost)
    //So when the function 'start' of the script tries to retrieve the gameObject 'NearMenuControl', this last can be deactived and menu==null
    //So menu is defined by Vuforia when the target is found 
    public GameObject menu {private get;set;}

    //list of clinicians
    [SerializeField]
    private GameObject[] clinicians;

    //Reference to differents pop-up and menu 
    private GameObject resultatInfo,infoBar,infoAnswer,sceneManagement,endMenu;
    private ZeegoManagement zeegoManagement;
    private HelperManagement helper;
    private float exposure,dose,timeEndExercise;

    //Boolean used to see or not the scatter map
    public bool SeeTheScattermap {get;set;}

    // Start is called before the first frame update
    private void Start()
    {
        beginModule2 = false;
        sceneManagement = GameObject.Find("SceneManagementObject");
        resultatInfo = GameObject.Find("ResultatInfo"); //pop up
        infoBar = GameObject.Find("InfoBar"); //Menu that contains the pop up and the button to come back to MenuScene
        infoBar?.SetActive(false);//Check if in the scene there is the infoBar object (not present in the scene2)
        infoAnswer = GameObject.Find("InfoBarAnswer");
        infoAnswer?.SetActive(false); // A?.B: B is not evaluate if A is null
        endMenu = GameObject.Find("EndMenu");
        endMenu?.SetActive(false);
        exposure = 0;
        dose = 0;
        SeeTheScattermap = false;
        zeegoManagement = GameObject.Find("ZeegoPretty").GetComponent<ZeegoManagement>();
        helper = GameObject.Find("SceneManagementObject").GetComponent<HelperManagement>();
    }

    private void Update()
    {
        if((menu?.activeSelf ?? false) & (infoAnswer?.activeSelf ?? false))
        {
            menu.SetActive(false);
        }
    }

    //Function called when the user finds (sees) the game object and wants to finish the training
    public void ShowAnswerForm()
    {
        if(menu == null | infoAnswer==null)
            return;
        timeEndExercise = Time.time;
        infoAnswer.SetActive(true);
        menu.SetActive(false);
    }

    //Function called when the user clicks on the button 'validate answer' of the Answer pop-up
    public void GiveInfoEndGame()
    {
        if(infoBar==null | menu == null )
            return;
        infoBar.SetActive(true);
        menu.SetActive(false);
        string info = "";
        //Look for each clinician the dose and the exposure but for the moment only valid if there is one clinician (modify the retrieval of information if we want to use that with several clinicians)
        foreach (GameObject clinician in clinicians) 
        {
            dose += clinician.GetComponent<ClinicianManagement>().dose;
            exposure += clinician.GetComponent<ClinicianManagement>().exposure;
        }
        exposure = (float)Math.Round(exposure,2);
        int numberOfActivation = zeegoManagement.numberOfActivation; //Find the # of Zeego's activation
        float averageTimePression = 0;
        if(numberOfActivation != 0)
        {
            averageTimePression = zeegoManagement.timePression / numberOfActivation; //Mean time pressure during the training
            averageTimePression = (float)Math.Round(averageTimePression,2);
        }
        if(helper.giveAnswer) //If the user finished the exercise after 10 minutes (because the C-arm gives automatically the solution's position) we don't show the radiography's score
        {
            info = string.Format("Dose: {0} mGy\nNumber of pedal pressures: {1}\nTime pressed: {2} seconds\nMean pressure time: {3} seconds\n"
            ,dose.ToString("0.00E0"),numberOfActivation,(float)Math.Round(zeegoManagement.timePression,2),averageTimePression); // Create the information for the user
        }
        else 
        {
            int radiographyScoring = zeegoManagement.GiveTargetProjectionScoring();
            info = string.Format("Dose: {0} mGy\nNumber of pedal pressures: {1}\nTime pressed: {2} seconds\nMean pressure time: {3} seconds\nRadiography score: {4}%\n"
            ,dose.ToString("0.00E0"),numberOfActivation,(float)Math.Round(zeegoManagement.timePression,2),averageTimePression,radiographyScoring); // Create the information for the user
        }
        info += "Some teaching points:\n- You need to use the shields on the same side as the clinician\n- Clinician should not be on the same side as the source\n- You need to minimize the number of X-ray activations";
        this.GetComponent<BarDoseManagement>().UpdateBar(Convert.ToInt32(dose*1000)); //false but just use to implement the bar in the pop-up
        resultatInfo.GetComponent<TextMeshPro>().text = info;
    }

    //Function for the module2, called when the user wants to finish one step
    public void GiveScoreStep()
    {
        beginModule2 = false;
        if(infoBar==null | menu == null )
            return;
        infoBar.SetActive(true);
        menu.SetActive(false);
        string info = "";
        resultatInfo.GetComponent<TextMeshPro>().text = info;
        foreach (GameObject clinician in clinicians) 
        {
            try
            {
                clinician.GetComponent<ClinicienMovementManagement>().Unselected();
            }
            catch
            {
                
            }
        }
        info = this.GetComponent<JsonManagement>().GiveScoreStep();
        resultatInfo.GetComponent<TextMeshPro>().text = info;
    }

    //Function called when the user wants to see the optimal position and texture for each clinician (module 2)
    public void SeeOptimalPosition()
    {
        foreach (GameObject clinician in clinicians) 
        {
            try
            {
                clinician.GetComponent<ClinicienMovementManagement>().OptimalPositionAndExposure();
            }
            catch
            {
                
            }
        }
    }

    //Function called to go to the next step    
    public void NextStep()
    {
        if(infoBar==null | menu == null )
            return;
        infoBar.SetActive(false);
        menu.SetActive(true);
        foreach (GameObject clinician in clinicians) 
        {
            try
            {
                clinician.GetComponent<ClinicienMovementManagement>().BeginStep();
            }
            catch
            {
                
            }
        }
    }

    //Function to put inside all the clinicians
    public void AppearClinicien()
    {
        foreach (GameObject clinician in clinicians)
        {
            try
            {
                clinician.GetComponent<ClinicienMovementManagement>().InsideAll();
            }
            catch
            {

            }
        }
        try
        {
            this.GetComponent<SpotManagement>().clinicianSelected = null;
        }
        catch
        {

        }
    }

    //Function called to begin the Module 2: switch from the clinician exposure to the personal material
    public void StartModule2()
    {
        beginModule2 = true;
        foreach (GameObject clinicien in clinicians)
        {
            try
            {
                clinicien.GetComponent<ClinicienMovementManagement>().BeginModule2();
            }
            catch
            {
                
            }
        }
    }

    //Function called to end the module 2
    public void EndModule2()
    {
        if(menu==null | infoBar == null | endMenu==null)
            return;
        menu.SetActive(false);
        infoBar.SetActive(false);
        endMenu.SetActive(true);
        GameObject imageTarget = GameObject.Find("ImageTarget");
        if(imageTarget!=null)
        {
            foreach (Transform child in imageTarget.transform)//Look at all the children of the ImageTarget object
            {
                child.gameObject.SetActive(false);
            }
        }
        GameObject.Find("InfoEndModule").GetComponent<TextMeshPro>().text = "You finished the exercise";
    } 
}
