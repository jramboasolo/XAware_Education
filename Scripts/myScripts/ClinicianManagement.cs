using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

// Class to manage the clinician in the scene
// Attach this script at all clinicians that you want to update the texture and the exposure during the application
// This class is not used to manage the clinician in the module 2
public class ClinicianManagement : MonoBehaviour
{

    //Class that containt all the information for a certain combination of angles
    private class DataExposure
    {
        //angle phi info
        public int anglePhiData;

        //angle theta info 
        public int angleThetaData;

        //exposure info
        public float exposure;

        //dose info
        public float dose;
    }
    //Game Object used as target which allows the clinician to look towards the center of the scene (at the C-arm level) 
    private GameObject target;
    //bool to check if the clinician is curently manipulated
    public bool manipulated{  get; set; }

    //bool to check if the clinician is inside or ouside the OR (transparent or not)
    public bool isInside {get; set;}

    //current angle phi and theta for the clinician
    private int anglePhi,angleTheta;

    //name of the folder that contains all the information on the dose and exposure 
    private string folder;
    private string shieldsPresence;
    private string sidePath;
    private bool presenceShield, isRightShieldUse;
    public bool isSideA {get;private set;}
    private string nameTexture; 
    private byte[] dataTexture,fileDataWithShieldA,fileDataWithoutShieldA,fileDataWithShieldB,fileDataWithoutShieldB;
    private Texture2D texture;
    public float exposure {private set; get;}
    public float dose {private set; get;}

    //List of all the DataExposure class for all the combination of angles
    private List<DataExposure> dataExposuresWithoutShieldA;
    private List<DataExposure> dataExposuresWithShieldA;
    private List<DataExposure> dataExposuresWithoutShieldB;
    private List<DataExposure> dataExposuresWithShieldB; 
    private Thread thread,thread2,thread3,thread4;

    private Renderer myRenderer=null;

    //list of materials [personal material, selected material, outside material]
    public Material[] materials;

    // Start is called before the first frame update
    // Defined all the list used to store the information about the clinician dose and exposure
    // Defined the file path for all the possibility and make the reading
    // Then launch the thread to stock all the information provides by the text into the corresponding list
    private void Start()
    {
        Vector3 angle = this.transform.localEulerAngles; //store the angle before the clinician rotation
        target = GameObject.Find("upper_shieldScene");
        if(target!=null)
        {
            this.transform.LookAt(target.transform,Vector3.up); //Rotate the clinician such that he looks at the target
        }
        Vector3 temporalAngle = this.transform.localEulerAngles; //Retrieve the angle after the rotation
        angle.y = temporalAngle.y; //Set just the y angle to avoid the clinician to rotate around the x axis because LookAt uses the clinician pivot as reference to rotate the clinician
        this.transform.localEulerAngles = angle;
        manipulated = false;
        isInside = false;
        isSideA = false;
        isRightShieldUse = false;
        angleTheta = 0;
        anglePhi = 0;
        exposure = 0;
        dose = 0;
        folder = "Textures";
        shieldsPresence = "withoutShield";
        sidePath = "sideB";
        presenceShield = false;
        texture = new Texture2D(1024,1024);
        dataExposuresWithoutShieldA = new List<DataExposure>();
        dataExposuresWithShieldA = new List<DataExposure>();
        dataExposuresWithoutShieldB = new List<DataExposure>();
        dataExposuresWithShieldB = new List<DataExposure>();
        myRenderer = this.GetComponent<Renderer>();
        string nameFile = Path.Combine(Application.streamingAssetsPath,"ScatterMap","withShield","sideA","allExposures.txt");
#if UNITY_WINRT
        fileDataWithShieldA = UnityEngine.Windows.File.ReadAllBytes(nameFile);
#else
        fileDataWithShieldA = System.IO.File.ReadAllBytes(nameFile);
#endif
        nameFile = Path.Combine(Application.streamingAssetsPath,"ScatterMap","withoutShield","sideA","allExposures.txt");
#if UNITY_WINRT
        fileDataWithoutShieldA = UnityEngine.Windows.File.ReadAllBytes(nameFile);
#else
        fileDataWithoutShieldA = System.IO.File.ReadAllBytes(nameFile);
#endif
        nameFile = Path.Combine(Application.streamingAssetsPath,"ScatterMap","withShield","sideB","allExposures.txt");
#if UNITY_WINRT
        fileDataWithShieldB = UnityEngine.Windows.File.ReadAllBytes(nameFile);
#else
        fileDataWithShieldB = System.IO.File.ReadAllBytes(nameFile);
#endif
        nameFile = Path.Combine(Application.streamingAssetsPath,"ScatterMap","withoutShield","sideB","allExposures.txt");
#if UNITY_WINRT
        fileDataWithoutShieldB = UnityEngine.Windows.File.ReadAllBytes(nameFile);
#else
        fileDataWithoutShieldB = System.IO.File.ReadAllBytes(nameFile);
#endif
        thread = new Thread(() => LoadDataExposure(ref dataExposuresWithoutShieldA,fileDataWithoutShieldA));
        thread2 = new Thread(() => LoadDataExposure(ref dataExposuresWithShieldA,fileDataWithShieldA));
        thread3 = new Thread(() => LoadDataExposure(ref dataExposuresWithoutShieldB,fileDataWithoutShieldB));
        thread4 = new Thread(() => LoadDataExposure(ref dataExposuresWithShieldB,fileDataWithShieldB));
        thread.IsBackground = true;
        thread2.IsBackground = true;
        thread3.IsBackground = true;
        thread4.IsBackground = true;
        thread.Start();
        thread2.Start();
        thread3.Start();
        thread4.Start();
        try
        {
            this.GetComponent<ObjectManipulator>().enabled = false;
        }
        catch
        {
            
        }
    }

    //Function called when the clinician is inside another collider with the IsTrigger enable
    private void OnTriggerStay(Collider collider)
    {
        if(!manipulated & collider.tag == "ClinicienTrigger")
        {
            this.transform.Translate(0,0,-0.5f,Space.Self);
        } 
    }

    //Function called to freeze all the contrains of the clinician's rigidbody
    public void DisenableMovement()
    {
        this.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        this.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    //Function called to load the texture from the theta and phi
    //So we use the current theta and phi of the Zeego (input of the function) to retrieve the corresponding texture according to the presence of the shields
    //and the current position of the clinician: side A (screen side) or side B (shields side)
    public void LoadClinicianTexture(int _angleTheta,int _anglePhi,bool isRightShield)
    {
        angleTheta = _angleTheta;
        anglePhi = _anglePhi;
        isRightShieldUse = isRightShield;
        nameTexture = string.Format("Texture_{0}_{1}.png",angleTheta,anglePhi);
        string name= "";
        try
        {
            name = Path.Combine(Application.streamingAssetsPath,folder,shieldsPresence,sidePath,nameTexture);
        }
        catch
        {
            name = Path.Combine(Application.streamingAssetsPath,"Textures","withoutShield","sideA",nameTexture);
        }
        if(isRightShield) //Just one thing when the user uses the right shields, we inverse the sideA and the sideB because we have only the texture for a configuration with the left shields (see XAwareLive)
        {                 //But as the textures are symmetric we just need to inverse the side 
            if(isSideA)
            {
                name = Path.Combine(Application.streamingAssetsPath,folder,shieldsPresence,"sideB",nameTexture);
            }
            else
            {
                name = Path.Combine(Application.streamingAssetsPath,folder,shieldsPresence,"sideA",nameTexture);
            }
        }
           
#if UNITY_WINRT
        dataTexture = UnityEngine.Windows.File.ReadAllBytes(name);
#else
        dataTexture = System.IO.File.ReadAllBytes(name);
#endif
        texture.LoadImage(dataTexture,true);
        if(myRenderer!=null)
            myRenderer.material.SetTexture("_MainTex",texture); 
    }

    //Function to give the dose according to the theta and phi angles
    //Take the scopie time as input
    public void GiveDose(float time)
    {
        DataExposure data;
        if(presenceShield) //check if the shields are present or not
        {
            if(isRightShieldUse)//Just one thing when the user uses the right shield, we inverse the sideA and the sideB because we have only the texture for a configuration with the left shields (see XAwareLive)
            {                   //But we consider that the doses are symmetric we just need to inverse the side
                if(isSideA)
                {
                    data = dataExposuresWithShieldB.Find(x=>x.angleThetaData==angleTheta & x.anglePhiData==anglePhi); //allow to find in the list the DataExposure with the theta and phi angle
                }
                else
                {   
                    data  = dataExposuresWithShieldA.Find(x=>x.angleThetaData==angleTheta & x.anglePhiData==anglePhi); //allow to find in the list the DataExposure with the theta and phi angle
                }
            }
            else
            {
                if(isSideA)
                {
                    data = dataExposuresWithShieldA.Find(x=>x.angleThetaData==angleTheta & x.anglePhiData==anglePhi); //allow to find in the list the DataExposure with the theta and phi angle
                }
                else
                {   
                    data  = dataExposuresWithShieldB.Find(x=>x.angleThetaData==angleTheta & x.anglePhiData==anglePhi); //allow to find in the list the DataExposure with the theta and phi angle
                }
            }
        }
        else
        {
            if(isSideA)
            {
                data  = dataExposuresWithoutShieldA.Find(x=>x.angleThetaData==angleTheta & x.anglePhiData==anglePhi); //allow to find in the list the DataExposure with the theta and phi angle
            }
            else
            {
                data  = dataExposuresWithoutShieldB.Find(x=>x.angleThetaData==angleTheta & x.anglePhiData==anglePhi); //allow to find in the list the DataExposure with the theta and phi angle
            }
            
        }
        float realDose = data.dose * (float)Math.Pow(10,3) * time * (float)Math.Pow(10,2);
        dose += realDose;
        exposure += (data.exposure * time);
        string radiographyInformation = string.Format("Total Dose: {0} mGy\nDose: {1} mGy\nTime scopie: {2} s",dose.ToString("0.00E0"),realDose.ToString("0.00E0"),Math.Round(time,2));
        GameObject.Find("radiographyInformation").GetComponent<TextMeshPro>().text = radiographyInformation;
    }

    //Function called to move the clinician according to his current position
    //If he is on the screen side, move to the shield side, or vice versa
    public void MoveClinician(int side)
    {
        Vector3 position = this.transform.localPosition;
        Vector3 rotation = this.transform.localEulerAngles;

        if(Mathf.Sign(side)!=Mathf.Sign(position.x)) //Check the sign of the x position and update this value to move the clinician to the other side
        {
            position.x = -position.x;
        }

        this.transform.localEulerAngles = rotation;
        this.transform.localPosition = position;
        Vector3 angle = this.transform.localEulerAngles;
        if(target!=null)
        {
            this.transform.LookAt(target.transform,Vector3.up); //Same logic that in the Start function
        }
        Vector3 temporalAngle = this.transform.localEulerAngles;
        angle.y = temporalAngle.y;
        this.transform.localEulerAngles = angle;
    }

    //Function called when the user add or remove the shields to update the path for the clinician's texture (deprecated)
    public void ChangeShieldPath()
    {
        if(presenceShield)
        {
            shieldsPresence = "withoutShield";
            presenceShield = false;
        }
        else
        {
            shieldsPresence = "withShield";
            presenceShield = true;
        }
    }

    //Function called when the user removes the shield and updated the path for the clinician's texture
    //Use this fonction and AddShield because of the use of the right shields
    //So when the user removes the shield this fonction is always called and the path is always correct
    public void RemoveShield()
    {
        shieldsPresence = "withoutShield";
        presenceShield = false;
    }

    //Function called when the user adds the shield and updated the path for the clinician's texture
    //Use this fonction and RemoveShield because of the use of the right shields
    //So when the user adds the shield this fonction is always called and the path is always correct
    public void AddShield(bool isRightShield)
    {
        shieldsPresence = "withShield";
        presenceShield = true;
        isRightShieldUse = isRightShield;
    }

    //Function called when the user changes the clinician's side
    //Move the clinician to the Screen side and update the path information
    public void SideScreen()
    {
        sidePath = "sideA";
        MoveClinician(1);
        isSideA = true;
    }

    //Function called when the user changes the clinician's side
    //Move the clinician to the Shield side and update the path information
    public void SideShields()
    {
        sidePath = "sideB";
        MoveClinician(-1);
        isSideA = false;
    }
    
    //Function to load all the data exposure from the file and to update the list of DataExposure
    private void LoadDataExposure(ref List<DataExposure> dataExposures,byte[] fileDataName)
    {  
        string file = System.Text.Encoding.ASCII.GetString(fileDataName);
        string[] allLines = file.Split('\n');
        string line = "";
        NumberFormatInfo provider =  new NumberFormatInfo(); //provider to allow the conversion for the double
        provider.NumberDecimalSeparator = ".";
        for(int i = 0; i< allLines.Length-1;i++)
        {
            line = allLines[i];
            string[] entries = line.Split(null);
            DataExposure dataExposure = new DataExposure();
            dataExposure.angleThetaData = Convert.ToInt32(entries[4]);
            dataExposure.anglePhiData = Convert.ToInt32(entries[5]);
            dataExposure.exposure = (float)Convert.ToDouble(entries[6],provider);
            dataExposure.dose = (float)Convert.ToDouble(entries[7],provider);
            dataExposures.Add(dataExposure);
        }
    }

    //Function called for each x-rays activation to know if the clinician is on the same side as the source
    public string GetClinicianError(int side)
    {
        Vector3 position = this.transform.localPosition;
        if(Mathf.Sign(side)==Mathf.Sign(position.x) & side!=0)
        {
            return "The clinician shouldn't be on the same side as the source\n";
        }
        else
        {
            return "";
        }
    }
}
