using System;
using System.IO;
using UnityEngine;

//Class to manage the clinician's movement for the Module 2
//Attach this script to all the clinicians you want to move during the Module 2
//Use to manage the clinician's movement using a spot system
//Use three materials to allow a visual feedback for the user: 
//the first material (the classical material with a unique color to differentiate them)
//the second matrial (material highlight used to know when the clinician is selected)
//the thrid material (transparent material used to know which clinician is outside of the scene)
//the last material (material used to apply the texture for the optimal or initial position)
public class ClinicienMovementManagement : MonoBehaviour
{
    //Game Object used as target which allows the clinician to look towards the center of the scene (at the C-arm level) 
    private GameObject target;
    //Previous position of the clinician before the movement
    private Vector3 previousPosition;
    //Boolean used to know when there is a trigger collision with the clinician we need to remove (clinician with IsMoved=true)
    public bool IsMoved{set;get;}
    //Boolean used to know if the clinician is inside the scene or outside
    public bool IsInside{set;get;}
    //List of materials used to give a visual feedback when a clinician is currently selected
    public Material[] materials;
    private Renderer myRenderer;
    private Texture2D texture;
    private byte[] dataTexture;


    // Start is called before the first frame update
    private void Start()
    {
        Vector3 angle = this.transform.localEulerAngles; //store the angle before the clinician rotation
        target = GameObject.Find("upper_shieldScene");
        if(target!=null)
        {
            this.transform.LookAt(target.transform,Vector3.up); //Rotate the clinician such that he looks at the target
        }
        Vector3 temporalAngle = this.transform.localEulerAngles; //Retrieve the angle after the rotation
        angle.y = temporalAngle.y; //Set just the y angle to avoid the clinician to rotate around the x axis because LookAt uses the clinician pivot as reference to rotate the clincian
        this.transform.localEulerAngles = angle;
        previousPosition = this.transform.localPosition;
        IsMoved = false;
        IsInside=true;
        texture = new Texture2D(1024,1024);
        myRenderer = this.GetComponent<Renderer>();
    }

    //Function called to allow the clinician to look towards the center of the scene
    //Use the same logic that in the start function
    public void LookAtTarget()
    {
        Vector3 angle = this.transform.localEulerAngles;
        if(target!=null)
        {
            this.transform.LookAt(target.transform,Vector3.up);
        }
        Vector3 temporalAngle = this.transform.localEulerAngles;
        angle.y = temporalAngle.y;
        angle.z = temporalAngle.z;
        this.transform.localEulerAngles = angle;
    }

    //Function called when a clinician enters in a collider of another clinician
    private void OnTriggerEnter(Collider collider)
    {
        
        if(IsMoved & collider.CompareTag("ClinicianTrigger")) //check if the collider belongs to an object with the good Tag
        {                                                     //The Tag ClinicianTrigger allows to identify the objects that collide with a clinician
            this.transform.localPosition = previousPosition; // Set the current position to the previous position
            LookAtTarget();
        }
    }

    //Function used when the user wants to begin the Module (begin to move the clinician and so stop seeing the exposure on the clinician)
    public void BeginModule2()
    {
        if(myRenderer!=null)
            myRenderer.material = materials[0]; 
    }

    //Function called when the clinician is selected by the user
    public void Selected()
    {
        if(myRenderer!=null)
            myRenderer.material = materials[1];
    }

    //Function called when the Clinician is unselected by the user, when the user selects another clinician
    public void Unselected()
    {
        if(IsInside)
        {
            if(myRenderer!=null)
                myRenderer.material = materials[0];
        }
        else
        {
            if(myRenderer!=null)
                myRenderer.material = materials[2];
        }
    }

    //Function called when a clinician is successfully moved to another spot
    //Update the previous position to the current position
    public void UpdatePreviousPosition()
    {
        previousPosition = this.transform.localPosition;
    }

    //Function called when the user clicks on the button Inside (control menu module 2)
    public void Inside()
    {
        Color color = materials[1].color;
        color.a = 1;
        materials[1].color = color;
        IsInside = true;
    }

    //Function called when the user clicks on the button Outside (control menu module 2)
    public void Outside()
    {
        Color color = materials[1].color;
        color.a = 0.1f;
        materials[1].color = color;
        IsInside = false;
    }

    //Function called when the user clicks on the button All Inside (control menu module 2)
    public void InsideAll()
    {
        Color color = materials[1].color;
        color.a = 1f;
        materials[1].color = color;
        IsInside = true;
        if(myRenderer!=null)
            myRenderer.material = materials[0];
    }

    //Function called when the user clicks on the button Begin Step
    public void BeginStep()
    {
        if(myRenderer!=null)
            myRenderer.material = materials[3];
    }

    //Function called when the user clicks on the button See Optimal position
    public void OptimalPositionAndExposure()
    {
        if(myRenderer!=null)
            myRenderer.material = materials[3];
    }

    //Function called to load the texture using the current step, the name representation of the clincian and if it is for the optimal position or not
    public void LoadTexture(int phase,int clinician,bool isOptimalPosition)
    {
        string nameTexture = string.Format("texture_{0}_{1}_{2}.png",clinician,Convert.ToInt32(isOptimalPosition),phase);
        string name = Path.Combine(Application.streamingAssetsPath,"Textures","Module2",nameTexture);
#if UNITY_WINRT
        dataTexture = UnityEngine.Windows.File.ReadAllBytes(name);
#else
        dataTexture = System.IO.File.ReadAllBytes(name);
#endif
        texture.LoadImage(dataTexture,true);
        materials[3].SetTexture("_MainTex",texture);
        if(myRenderer!=null)
            myRenderer.material = materials[3];
    }
}
