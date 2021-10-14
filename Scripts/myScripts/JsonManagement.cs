using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

//Class to read the json file for each operation
//Store the initial and optimal position for each clinician for each step
//Move the clinician to the initial position at the beginning of each step
//Move the clinician to the optimal position when the user presses the button "See optimal position"
//Compute the scoring position for each clinician at the end of each step
//Used only for the module2

//Logic of the JSON File
//The key "step" is a vector that contains all the step in the operation
//Each step contains a list of clinicians
//Each clinician in the list contains the following information: name, initial position and optimal position
public class JsonManagement : MonoBehaviour
{
    //reference to the class used to deserialize the json, and store all the information about the intial and optimal position for each clincian for each step
    private Operation operation;

    //List to store all the information about all the clinician in the scene
    private List<InformationClinician> informationClinicians;

    //Reference to the current step
    private int currentStep;

    //Reference to the textMeshPro on the main menu to show the step's name
    [SerializeField]
    private GameObject nameStep;

    // Start is called before the first frame update
    //Move the clinician in the scene to his initial position for the first step of the operation
    //Read the JSON file and store all the information for each clinician for each step
    //Store into a list the names and the Transforms for each clinician in the scene
    //Use the name to retrieve in the JSON information the initial and optimal position for each step
    private void Start()
    {
        currentStep = 0;
        informationClinicians = new List<InformationClinician>();
        GameObject clinicianScene = GameObject.Find("clinicianScene"); //GameObject parent of all the clinicians in the scene
        foreach (Transform child in clinicianScene.transform) // Look for each child from the clinicianScene, and store all the information (name and Transform)
        {
            InformationClinician currentChild = new InformationClinician();
            currentChild.name = child.name;
            currentChild.transformObject = child;
            currentChild.nameRepresentation = NameClinicianToInt(child.name);
            informationClinicians.Add(currentChild);
        }
        string fileOperation = Path.Combine(Application.streamingAssetsPath,"Operation","CliniciansPositions.json"); // Set the path of the JSON
        using (StreamReader stream = new StreamReader(fileOperation)) //Read the JSON
        {
            string json = stream.ReadToEnd();
            operation = JsonUtility.FromJson<Operation>(json); //deserialize the JSON and store all the information
        }
        MoveClinician(currentStep);
    }

    //Function called to give the representation of the clinician's name in int
    //main operator: 0, assistant: 1, circulating nurse: 2 and radiology technician: 3
    private int NameClinicianToInt(string nameClinician)
    {
        switch(nameClinician)
        {
            case "main operator​":
                return 0;
            case "assistant​":
                return 1;
            case "circulating nurse​":
                return 2;
            case "radiology technician​":
                return 3;
            default:
                return 0;
        }
    }

    //Move all the clinicians in the scene to their initial position for the step, called at the beginnig of the step
    //Take as input the current step
    private void MoveClinician(int step)
    {
        foreach (Clinician clinician in operation.steps[step].clinicians)//Look for each clinician for the current step in the clinician list
        {
            
            InformationClinician currentClinician = informationClinicians.Find(x=>String.Compare(clinician.name,0,x.name,0,clinician.name.Length)==0);//Search the corresponding information using the name as reference 
            if(currentClinician!=null)
            {
                Vector3 position = new Vector3(clinician.x_init,clinician.y_init,clinician.z_init);
                currentClinician.transformObject.localPosition = position; //Move the clinician to the initial position
                try
                {
                    ClinicienMovementManagement clinicienManagement = currentClinician.transformObject.GetComponent<ClinicienMovementManagement>();
                    clinicienManagement.LookAtTarget();
                    clinicienManagement.LoadTexture(step+1,currentClinician.nameRepresentation,false);
                }
                catch
                {

                }
            }
        }
        try
        {
            nameStep.GetComponent<TextMeshPro>().text = string.Format("Step: {0}", operation.steps[step].nameStep);
        }
        catch
        {

        }
    }

    //Function called to retrieve the current step and move the clinicians
    public void RetryStep()
    {
        MoveClinician(currentStep);
    }
    
    //Function called to switch to the next step and move the clinicians
    public void NextStep()
    {
        currentStep++;
        if(currentStep>operation.steps.Length-1)//Check if the current step is above the length of the vector from the JSON file
        {
            GameObject.Find("SceneManagementObject").GetComponent<GameMangement>().EndModule2(); //If true, ends the module 2
            return;
        }
        MoveClinician(currentStep);
    }
    
    //Function called to move the clinicians to their optimal positions
    //Same logic that in the MoveClinician function
    public void MoveOptimalPosition()
    {
        foreach (Clinician clinician in operation.steps[currentStep].clinicians)
        {
            
            InformationClinician currentClinician = informationClinicians.Find(x=>String.Compare(clinician.name,0,x.name,0,clinician.name.Length)==0);
            if(currentClinician!=null)
            {
                Vector3 position = new Vector3(clinician.x_opti,clinician.y_opti,clinician.z_opti);
                currentClinician.transformObject.localPosition = position;
                try
                {
                    ClinicienMovementManagement clinicienManagement = currentClinician.transformObject.GetComponent<ClinicienMovementManagement>();
                    clinicienManagement.LookAtTarget();
                    clinicienManagement.LoadTexture(currentStep+1,currentClinician.nameRepresentation,true);
                }
                catch
                {

                }
            }
        }
    }
    
    //Give the scoring position for each clinician for the current step
    //Return a string with the scoring for each clinician
    public string GiveScoreStep()
    {
        string infoScore = "";
        foreach (Clinician clinician in operation.steps[currentStep].clinicians)//Look for each clinician for the current step in the clinicians list
        {
            InformationClinician currentClinician = informationClinicians.Find(x=>String.Compare(clinician.name,0,x.name,0,clinician.name.Length)==0);//Retrieve the corresponding clinician in the JSON
            if(currentClinician!=null)
            {
                GameObject currentObjectClincian = GameObject.Find(currentClinician.name);
                Vector3 currentPosition  = currentObjectClincian.transform.localPosition; //Retrieve the current position
                Vector3 optimalPosition = new Vector3(clinician.x_opti,clinician.y_opti,clinician.z_opti); //Set the optimal position
                int score = 0;
                if(currentObjectClincian.GetComponent<ClinicienMovementManagement>().IsInside)
                {
                    score = ComputeScore(optimalPosition.x,currentPosition.x,optimalPosition.z,currentPosition.z); //Compute a score using the x and z positions
                }
                else
                {
                    if(optimalPosition.y==0) //if the current clinician is outside check if the y value of the optimal position is set to 0
                        score = 100;         //If yes, set the score to 100, otherwise, put the score to 0
                }
                infoScore += string.Format("{0}: Position score {1}%\n",currentClinician.name,score);
            }
        }
        return infoScore;
    }
    
    //Use a Gaussian to compute the score
    //Take as input the optimal position in x and z (meanX and meanZ) and the current position in x and z
    //Compute a score with a 2D gaussian with the means defined by the optimal position in x and z
    private int ComputeScore(float meanX,float _x,float meanZ,float _z)
    {
        float sigma = 1f; //Set the sigma to 1 to have relevant scores according to the gap between the optimal position and the current position (emperical)
        return Convert.ToInt32(Mathf.Exp(-(((_x-meanX) * (_x-meanX)  + (_z-meanZ) * (_z-meanZ)) / (2*sigma * sigma))) * 100);
    }
    
    //Internal class to store all the information for the clinician in the scene
    internal class InformationClinician
    {
        public string name;
        public Transform transformObject;
        public int nameRepresentation;
    }

    //Class used for the deserialization
    [System.Serializable]
    public class Clinician
    {
        public string name;
        public float x_opti;
        public float y_opti;
        public float z_opti;
        public float x_init;
        public float y_init;
        public float z_init;
    }
    [System.Serializable]
    public class Steps
    {
        public string nameStep;
        public Clinician[] clinicians;
    }
    public class Operation
    {
        public Steps[] steps;
    }
}
