using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

// Class to manage the Zeego or his phantom
// Attach this script to the Zeego and the ZeegoPhantom
// Use a phantom to allow the user to see which direction the Zeego will rotate
// For the phi rotation, we use RotateAround to make the rotation like the rotation in the XawareLite application
public class ZeegoManagement : MonoBehaviour
{
    //Reference to the Zeego, radiation Cube, radiography and activation message for the Zeego
    private GameObject radiationCube,radiography,activationZeegoInfo,zeegoPivot,gameScene,thetaInfo,phiInfo,zeego;

    [SerializeField]
    private GameObject infoShields;

    private TextMeshPro thetaText,phiText;

    //List of the clinician in the scene
    public GameObject[] clinicians;

    //Direction used to know how to rotate the Zeego
    private int angleTheta,anglePhi,directionRotation = -1;
    private int step;
    public int numberOfActivation {private set; get;}

    //Boolean used to know if we bound the rotation of the Zeego according to the training
    private bool limitRotation = true;

    //Boolean used to know if we manipulate the real Zeego or not 
    public bool isRealZeego { set; get;}

    //Boolean used to know if the Zeego is rotating, the x-rays are activated and if the Zeego is already colliding
    private bool currentlyRotate,currentlyActivate,takeAnswer;

    //Boolean used to know if the Zeego is colliding something
    public bool manageCollision {get;private set;}

    //Center of rotation for the Zeego
    private Vector3 center;

    //Global pressure time for the training
    public float timePression {private set; get;}

    //Time when the pedale is pressed (Menu button or primary button from the mouse)
    private float time;

    //Time when the pedale is released
    private float currentTime;
    private Interactable buttonFiveInteractable;
    private texture3DCreator radiationCubeTexture3DCreator;
    private Renderer radiationCubeRenderer,radiographyRenderer;
    private RadiographyManagement radiographyManagement;

    private WaitForSecondsRealtime wait;

    [SerializeField]
    private AudioClip zeegoRotation;

    //Audio clip for the pedale when it is pressed (sound)
    [SerializeField]
    private AudioClip zeegoActivation;

    //Audio clip for the pedale when it is released (sound)
    [SerializeField]
    private AudioClip zeegoRelease;

    // Start is called before the first frame update
    private void Start()
    {
        anglePhi = 0;
        angleTheta = 0;
        step = 5;
        isRealZeego = false;
        currentlyRotate = false;
        currentlyActivate = false;
        manageCollision = false;
        takeAnswer = false;
        radiationCube = GameObject.Find("radiationCube");
        radiography = GameObject.Find("radiography");
        center = GameObject.Find("GameScene").transform.position;
        activationZeegoInfo = GameObject.Find("informationActivationZeego");
        zeegoPivot = GameObject.Find("ZeegoPivot");
        buttonFiveInteractable = GameObject.Find("ButtonFive").GetComponent<Interactable>();
        radiationCubeTexture3DCreator = radiationCube.GetComponent<texture3DCreator>();
        radiationCubeRenderer = radiationCube.GetComponent<Renderer>();
        if(radiography!=null)
        {
            radiographyRenderer = GameObject.Find("radiography").GetComponent<Renderer>();
            radiographyManagement = GameObject.Find("radiography").GetComponent<RadiographyManagement>();
        }
        gameScene = GameObject.Find("GameScene");
        thetaInfo = GameObject.Find("ThetaText");
        phiInfo = GameObject.Find("PhiText");
        if(thetaInfo!=null & phiInfo!=null)
        {
            thetaText = GameObject.Find("ThetaText").GetComponent<TextMeshPro>();
            phiText = GameObject.Find("PhiText").GetComponent<TextMeshPro>();
        }
        zeego = GameObject.Find("ZeegoPretty");
        if(infoShields!=null)
            infoShields.SetActive(false);
        timePression = 0;
        numberOfActivation = 0;
        wait = new WaitForSecondsRealtime(3.5f/10);
    }

    private void Update()
    {
        //"If" use in the case where the user press and release the button just after that the scattermap appear
        if((buttonFiveInteractable.HasPress | Input.GetMouseButton(0)) & !radiationCubeTexture3DCreator.enabled)
        {
            radiationCubeTexture3DCreator.enabled = true;
        }
    }

    //Function called when the Zeego is inside another collider with the tag: "ZeegoTrigger"
    private void OnTriggerStay(Collider collision)
    {
        center = gameScene.transform.position;

        if(!collision.CompareTag("ZeegoTrigger") | currentlyRotate | manageCollision) //check if the collider is good, if the Zeego isn't rotating and that the Zeego isn't already colliding
        {
            return;
        }
        manageCollision = true;
        GameObject zeegoPhantom = GameObject.Find("ZeegoPrettyPhantom");
        ZeegoManagement zeegoManagementPhantom = zeegoPhantom.GetComponent<ZeegoManagement>();
        switch(directionRotation)
        {
            case 1:
                StartCoroutine(RotateZeego(2));
                StartCoroutine(zeegoManagementPhantom.PhantomRotation(2,false));
            break;

            case 2:
                StartCoroutine(RotateZeego(1));
                StartCoroutine(zeegoManagementPhantom.PhantomRotation(1,false));
            break;

            case 3:
                StartCoroutine(RotateZeego(4));
                StartCoroutine(zeegoManagementPhantom.PhantomRotation(4,false));
            break;

            case 4:
                StartCoroutine(RotateZeego(3));
                StartCoroutine(zeegoManagementPhantom.PhantomRotation(3,false));
            break;
        }
    }

    //Function called to rotate the real zeego, takes as input the direction for the rotation
    //1: direction right, 2: left, 3: forward and 4: behind
    public void RotateZeegoDirection(int direction)
    {
        center = gameScene.transform.position;
        if(currentlyActivate | currentlyRotate)
            return;
        StartCoroutine(RotateZeego(direction));
    }

    //Function called to rotate the zeego's phantom, takes as input the direction for the rotation
    //1: direction right, 2: left, 3: forward and 4: behind
    public void RotateZeegoPhantomDirection(int direction)
    {
        center = gameScene.transform.position;
        ZeegoManagement zeegoManagement = zeego.GetComponent<ZeegoManagement>();
        if(zeegoManagement.IsActvate() | zeegoManagement.IsRotate())
            return;
        StartCoroutine(PhantomRotation(direction,false));
    }

    //Function called to set the Zeego angles and position to the current Zeego angles and position after the users releases the control buttons
    //Function called when the user focus one of the Zeego control button
    public void RotateZeegoPhantomDirectionRelease()
    {
        Vector3 angle = zeego.transform.localEulerAngles;
        Vector3 position = zeego.transform.localPosition;
        this.transform.localEulerAngles = angle;
        this.transform.localPosition = position;
        ZeegoManagement management = zeego.GetComponent<ZeegoManagement>();
        angleTheta = management.angleTheta;
        anglePhi = management.anglePhi;
    }

    //Function called when the Hololens is connected to the server
    //Make the x and z rotation
    //Also used by the resetPosition function
    //Input the theta and phi angle desired
    public void RotateZeego(int angleZ, int angleX,bool isPhantom)
    {
        if(takeAnswer)
            return;
        float currentAngleTheta = angleZ - angleTheta;
        float currentAnglePhi = angleX - anglePhi;
        center = gameScene.transform.position;
        this.transform.RotateAround(center,gameScene.transform.forward,currentAngleTheta);
        this.transform.RotateAround(center,gameScene.transform.right,-currentAnglePhi);
        Vector3 worldPosition = new Vector3();
        Vector3 currentAngle;
        worldPosition = this.transform.localPosition;
        if(angleX==0)
            worldPosition.x=0;
        if(angleZ==0)
        {
            currentAngle = new Vector3(angleZ,270,angleX);
            this.transform.localEulerAngles = currentAngle;
            this.transform.localPosition = worldPosition;
        }
        if(angleZ==0 & angleX==0)
        {
            this.transform.localPosition = zeegoPivot.transform.localPosition;
        }
        angleTheta = angleZ;
        anglePhi = angleX;
        if(isPhantom)
            return;
        if(thetaInfo!=null)
        {
            if(isRealZeego)
            {
                thetaText.text = string.Format("{0}°",-angleTheta);
            }
            else
            {
                thetaText.text = string.Format("{0}°",angleTheta);
            }
        }
            
        if(phiInfo!=null)
        {
            if(isRealZeego)
            {
                phiText.text = string.Format("{0}°",-anglePhi);
            }
            else
            {
                phiText.text = string.Format("{0}°",anglePhi);
            }
        }
    }

    // Function called to reset the Zeego position theta=0 and phi=0
    public void ResetPosition()
    {
        if(currentlyRotate)
            return;
        RotateZeego(0,0,false);
    }

    // Function called to reset the phantom position theta=0 and phi=0
    public void ResetPositionDirect()
    {
        if(zeego.GetComponent<ZeegoManagement>().IsRotate())
            return;
        RotateZeego(0,0,true);
    }

    //Function called when the user pushes the buttom to activate (simulate) the radiation
    public void ActiveZeego()
    {
        if(currentlyRotate)
        {
            return;
        }
        this.GetComponent<AudioSource>().PlayOneShot(zeegoActivation);
        currentlyActivate = true;
        time = Time.time;
        numberOfActivation++;
        if(activationZeegoInfo!=null) //Check if there is the activationZeegoInfo object in the scene (not present in the scene2)
        {
            activationZeegoInfo.GetComponent<TextMeshPro>().text = "Zeego running";
            activationZeegoInfo.GetComponent<TextMeshPro>().color = Color.red;
        }  
        radiationCubeTexture3DCreator.updateCloud(angleTheta,anglePhi);
    }

    //Function called when the user releases the pedale (button menu or primary button mouse)
    public void Release()
    {
        if(currentlyRotate | !currentlyActivate)
        {
            return;
        }
        this.GetComponent<AudioSource>().PlayOneShot(zeegoRelease);
        currentlyActivate = false;
        currentTime = Time.time - time;
        timePression += currentTime;

        if(activationZeegoInfo!=null) //Check if there is the activationZeegoInfo object in the scene (not present in the scene2)
        {
            activationZeegoInfo.GetComponent<TextMeshPro>().text = "Zeego inactive";
            activationZeegoInfo.GetComponent<TextMeshPro>().color = Color.green;
        }
            

        if(currentTime > 0.6f)
        {

            radiationCubeRenderer.material.SetTexture("_Volume", null);
            radiationCubeRenderer.enabled = false;

            if(radiography!=null)
            {
                radiographyRenderer.enabled = true;
                radiographyManagement.loadTexture(angleTheta,anglePhi);
            }
        }
        foreach(GameObject clinician in clinicians)
        {
                clinician.GetComponent<ClinicianManagement>().GiveDose(currentTime);
        }
        string info = "";
        if(currentTime > 0.75f)
        {
            info += "You shouldn't use the X-rays for longer than 0.75 seconds\n";
        }   
        info += clinicians[0].GetComponent<ClinicianManagement>().GetClinicianError(angleTheta);
        info += radiationCubeTexture3DCreator.GetInfoErrorShield();
        if(!string.IsNullOrEmpty(info))
        {
            infoShields.SetActive(true);
            GameObject.Find("InfoError").GetComponent<TextMeshPro>().text = info;
            StartCoroutine(ShowInfoError());
        }
        
    }
    
    // Function used to allow the use of the function WaitWhile in the coroutine
    public bool IsRotate()
    {
        return currentlyRotate;
    }

    //Function used to know if the real Zeego has activate the fluroscopy
    public bool IsActvate()
    {
        return currentlyActivate;
    }
    
    //Function called to check if the Zeego is currently rotate to show his phantom
    public void ShowPhantom()
    {
        if(zeego.GetComponent<ZeegoManagement>().IsRotate())
        {
            return;
        }
        else
        {
            this.GetComponent<Renderer>().enabled = true;
        }
    }

    //Function to see the phantom with the voice commands
    public void RotationPhantomVoice(int direction)
    {
        RotateZeegoPhantomDirectionRelease();
        StartCoroutine(PhantomRotation(direction,true));
    }

    //Function called to get the C-arm the solution's position
    public void TakeAnswerRotation(bool isPhantom)
    {
        int answerTheta = angleTheta;
        int answerPhi = anglePhi;
        radiographyManagement.Answer(ref answerTheta, ref answerPhi);
        if(Mathf.Abs(answerPhi)>40)
        {
            answerPhi = Convert.ToInt32(Mathf.Sign(answerPhi)*40);
        }
        if(isPhantom)
        {
            ResetPositionDirect();
        }
        else
        {
            ResetPosition();
        }
        RotateZeego(answerTheta,answerPhi,isPhantom);
        isRealZeego = true;
        takeAnswer = true;
    }

    //Function called with the object ZeegoPrettyAnswer to use a phantom to see the solution
    public void ShowAnswer()
    {
        ZeegoManagement management = zeego.GetComponent<ZeegoManagement>();
        int answerTheta = management.angleTheta;
        int answerPhi = management.anglePhi;
        radiographyManagement.Answer(ref answerTheta,ref answerPhi);
        if(Mathf.Abs(answerPhi)>40)
        {
            answerPhi = Convert.ToInt32(Mathf.Sign(answerPhi)*40);
        }
        ResetPositionDirect();
        RotateZeego(answerTheta,answerPhi,true);
        this.GetComponent<MeshRenderer>().enabled = true;
    }
    
    //Function called to compute a score with the current C-arm angles
    public int GiveTargetProjectionScoring()
    {
        int answerTheta = angleTheta;
        int answerPhi = anglePhi;
        radiographyManagement.Answer(ref answerTheta, ref answerPhi);
        if(Mathf.Abs(answerPhi)>40)
        {
            answerPhi = Convert.ToInt32(Mathf.Sign(answerPhi)*40);
        }
        return radiographyManagement.TargetProjectionSystem(answerTheta,answerPhi);
    }

    //Coroutine to make the rotation smoother
    private IEnumerator RotateZeego(int direction)
    {
        Vector3 worldPosition = new Vector3();
        Vector3 currentAngle = new Vector3();
        yield return new WaitWhile(IsRotate); // Wait while the Zeego is rotate from a previous call to this coroutine
        currentlyRotate = true;
        center = gameScene.transform.position;
        switch(direction)
        {
            case 1:
                if((angleTheta <= -130 | isRealZeego) & limitRotation)
                {
                    break;
                }
                if(!manageCollision)
                    directionRotation = 1;
                this.GetComponent<AudioSource>().PlayOneShot(zeegoRotation);
                for (int i = 1; i < step+1; i++)
                {
                    this.transform.RotateAround(center,gameScene.transform.forward,-1);
                    worldPosition = this.transform.localPosition;
                    if(anglePhi==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition.x=0; 
                        currentAngle.x = (angleTheta - i);
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                    if(thetaInfo!=null)
                        thetaText.text = string.Format("{0}°",(angleTheta - i).ToString());
                    yield return wait;
                }
                angleTheta += -step;
            break;

            case 2:
                if((angleTheta >= 130 | isRealZeego) & limitRotation)
                {
                    break;
                }
                if(!manageCollision)
                    directionRotation = 2;
                this.GetComponent<AudioSource>().PlayOneShot(zeegoRotation);
                for (int i = 1; i < step+1; i++)
                {
                    this.transform.RotateAround(center,gameScene.transform.forward,1);
                    worldPosition = this.transform.localPosition; 
                    if(anglePhi==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition.x=0;
                        currentAngle.x = (angleTheta + i);
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                    if(thetaInfo!=null)
                        thetaText.text = string.Format("{0}°",(angleTheta + i).ToString());
                    yield return wait;
                }
                angleTheta += step;
            break;

            case 3:
                if((anglePhi >= 40 | isRealZeego) & limitRotation)
                {
                    break;
                }
                if(!manageCollision)
                    directionRotation = 3;
                this.GetComponent<AudioSource>().PlayOneShot(zeegoRotation);
                for (int i = 1; i < step+1; i++)
                {
                    this.transform.RotateAround(center,gameScene.transform.right,-1);
                    if(anglePhi+i==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition = this.transform.localPosition;
                        worldPosition.x=0;
                        currentAngle.x = angleTheta;
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi + i;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                    if(phiInfo!=null)
                        phiText.text = string.Format("{0}°",(anglePhi + i).ToString());
                    yield return wait;
                }
                anglePhi += step;
            break;

            case 4:
                if((anglePhi <= -40 | isRealZeego) & limitRotation)
                {
                    break;
                }
                if(!manageCollision)
                    directionRotation = 4;
                this.GetComponent<AudioSource>().PlayOneShot(zeegoRotation);
                for (int i = 1; i < step+1; i++)
                {
                    this.transform.RotateAround(center,gameScene.transform.right,1);
                    worldPosition = this.transform.localPosition;
                    if(anglePhi-i==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition.x=0;
                        currentAngle.x = angleTheta;
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi - i;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                    if(phiInfo!=null)
                        phiText.text = string.Format("{0}°",(anglePhi - i).ToString());
                    yield return wait;
                }
                anglePhi += -step;
            break;
        }
        currentlyRotate = false;
        if(anglePhi==0 & angleTheta==0) // Condition used to reset the good position when theta=0 and phi=0
        {
            this.transform.localPosition = zeegoPivot.transform.localPosition;
        }
        manageCollision = false;
    }

    //Coroutine to rotate the phantom
    private IEnumerator PhantomRotation(int direction,bool byVoiceCommand)
    {
        
        Vector3 worldPosition = new Vector3();
        Vector3 currentAngle = new Vector3();
        yield return new WaitWhile(IsRotate); // Wait while the Zeego is rotating from a previous call to this coroutine
        currentlyRotate = true;
        center = gameScene.transform.position;
        if(byVoiceCommand)
            this.GetComponent<Renderer>().enabled = true;
        switch(direction)
        {
            case 1:
                if((angleTheta <= -130 | isRealZeego) & limitRotation)
                {
                    break;
                }
                this.transform.RotateAround(center,gameScene.transform.forward,-step);
                if(currentlyActivate)
                    break;
                directionRotation = 1;
                angleTheta += -step;
                worldPosition = this.transform.localPosition;
                if(anglePhi==0) //check if the phi angle=0 and correct the x position
                {
                    worldPosition.x=0; 
                    currentAngle.x = (angleTheta);
                    currentAngle.y = 270;
                    currentAngle.z = anglePhi;
                    this.transform.localEulerAngles = currentAngle;
                    this.transform.localPosition = worldPosition;
                }
                if(byVoiceCommand)
                {
                    yield return new WaitForSecondsRealtime(2);
                    this.transform.RotateAround(center,gameScene.transform.forward,step);
                    worldPosition = this.transform.localPosition;
                    angleTheta += step;
                    if(anglePhi==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition.x=0; 
                        currentAngle.x = (angleTheta);
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                }
            break;

            case 2:
                if((angleTheta >= 130 | isRealZeego) & limitRotation)
                {
                    break;
                }
                this.transform.RotateAround(center,gameScene.transform.forward,step);
                if(currentlyActivate)
                    break;
                directionRotation = 2;
                angleTheta += step;
                worldPosition = this.transform.localPosition;
                if(anglePhi==0) //check if the phi angle=0 and correct the x position
                {
                    worldPosition.x=0; 
                    currentAngle.x = (angleTheta);
                    currentAngle.y = 270;
                    currentAngle.z = anglePhi;
                    this.transform.localEulerAngles = currentAngle;
                    this.transform.localPosition = worldPosition;
                }
                if(byVoiceCommand)
                {
                    yield return new WaitForSecondsRealtime(2);
                    this.transform.RotateAround(center,gameScene.transform.forward,-step);
                    worldPosition = this.transform.localPosition;
                    angleTheta += -step;
                    if(anglePhi==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition.x=0; 
                        currentAngle.x = (angleTheta);
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                }
            break;

            case 3:
                if((anglePhi >= 40 | isRealZeego) & limitRotation)
                {
                    break;
                }
                if(currentlyActivate)
                    break;
                directionRotation = 3;
                this.transform.RotateAround(center,gameScene.transform.right,-step);
                anglePhi += step;
                worldPosition = this.transform.localPosition;
                if(anglePhi==0) //check if the phi angle=0 and correct the x position
                {
                    worldPosition.x=0;
                    currentAngle.x = angleTheta;
                    currentAngle.y = 270;
                    currentAngle.z = anglePhi;
                    this.transform.localEulerAngles = currentAngle;
                    this.transform.localPosition = worldPosition;
                }
                if(byVoiceCommand)
                {
                    yield return new WaitForSecondsRealtime(2);
                    this.transform.RotateAround(center,gameScene.transform.right,step);
                    anglePhi += -step;
                    worldPosition = this.transform.localPosition;
                    if(anglePhi==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition.x=0;
                        currentAngle.x = angleTheta;
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                }
            break;

            case 4:
                if((anglePhi <= -40 | isRealZeego) & limitRotation)
                {
                    break;
                }
                if(currentlyActivate)
                    break;
                directionRotation = 4;
                this.transform.RotateAround(center,gameScene.transform.right,step);
                anglePhi += -step;
                worldPosition = this.transform.localPosition;
                if(anglePhi==0) //check if the phi angle=0 and correct the x position
                {
                    worldPosition.x=0;
                    currentAngle.x = angleTheta;
                    currentAngle.y = 270;
                    currentAngle.z = anglePhi;
                    this.transform.localEulerAngles = currentAngle;
                    this.transform.localPosition = worldPosition;
                }
                if(byVoiceCommand)
                {
                    yield return new WaitForSecondsRealtime(2);
                    this.transform.RotateAround(center,gameScene.transform.right,-step);
                    anglePhi += step;
                    worldPosition = this.transform.localPosition;
                    if(anglePhi==0) //check if the phi angle=0 and correct the x position
                    {
                        worldPosition.x=0;
                        currentAngle.x = angleTheta;
                        currentAngle.y = 270;
                        currentAngle.z = anglePhi;
                        this.transform.localEulerAngles = currentAngle;
                        this.transform.localPosition = worldPosition;
                    }
                }
            break;
        }
        currentlyRotate = false;
        if(anglePhi==0 & angleTheta==0) // Condition use to reset the good position when theta=0 and phi=0
        {
            this.transform.localPosition = zeegoPivot.transform.localPosition;
        }
        if(byVoiceCommand)
            this.GetComponent<Renderer>().enabled = false;
    }

    //Coroutine called to show, wait and disable the pop-up
    private IEnumerator ShowInfoError()
    {
        yield return new WaitForSecondsRealtime(6);
        infoShields.SetActive(false);
    }
}

