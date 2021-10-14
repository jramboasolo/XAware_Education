using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;


//Class used to manage the help during all the exercise of the module 1
//Use several coroutines for that
public class HelperManagement : MonoBehaviour
{
    //Reference to the Help slider
    private PinchSlider slider;

    //Reference to all the Zeego (real Zeego, Zeego's phantom,answer Zeego)
    private ZeegoManagement zeegoPretty,zeegoPhantom,zeegoAnswer;
    private MeshRenderer zeegoAnswerRenderer;

    //Reference to the pop-up used to display the information
    [SerializeField]
    private GameObject info;
    
    [SerializeField]
    private GameObject buttonTenBis;
    
    [SerializeField]
    private GameObject buttonTen;
    
    [SerializeField]
    private GameObject buttonNine;
    private GameObject sceneManagement;

    //Boolean used to start the Timer and the coroutines just once
    private bool isRunning;

    public bool giveAnswer {private set;get;}

    //Float to know when the user looks at the pattern (used for the GameManagement script)
    public float startTimer {private set;get;}
    private WaitForSecondsRealtime wait;

    // Start is called before the first frame update
    private void Start()
    {
        isRunning = false;
        giveAnswer = false;
        sceneManagement = GameObject.Find("SceneManagementObject");
        wait = new WaitForSecondsRealtime(5);
    }

    //Function to start all the coroutines
    public void StartTimer()
    {
        if(!isRunning)
        {
            slider = GameObject.Find("PinchSlider").GetComponent<PinchSlider>();
            zeegoPretty = GameObject.Find("ZeegoPretty").GetComponent<ZeegoManagement>();
            zeegoPhantom = GameObject.Find("ZeegoPrettyPhantom").GetComponent<ZeegoManagement>();
            zeegoAnswer = GameObject.Find("ZeegoPrettyAnswer").GetComponent<ZeegoManagement>();
            zeegoAnswerRenderer = GameObject.Find("ZeegoPrettyAnswer").GetComponent<MeshRenderer>();
            slider.enabled = false;
            StartCoroutine(EnableSlider());
            StartCoroutine(ShowAnswer());
            StartCoroutine(TakeAnswer());
            StartCoroutine(ShowScatterMap());
            StartCoroutine(AutomaticEnd());
            startTimer = Time.time;
            isRunning = true;
        }
    }

    //Coroutine to enable the use of the slider
    private IEnumerator EnableSlider()
    {
        yield return new WaitForSecondsRealtime(60*5);
        slider.enabled = true;
        info.SetActive(true);
        string infoText = "You can now use the Help slider";
        GameObject.Find("InfoError").GetComponent<TextMeshPro>().text = infoText;
        yield return wait;
        info.SetActive(false);
        
    }

    //Coroutine to show the solution thanks to a phantom
    private IEnumerator ShowAnswer()
    {
        yield return new WaitForSecondsRealtime(60*6);
        zeegoAnswer.ShowAnswer();
        info.SetActive(true);
        string infoText = "The Zeego's phantom shows the solution";
        GameObject.Find("InfoError").GetComponent<TextMeshPro>().text = infoText;
        yield return wait;
        info.SetActive(false);
    }
    
    //Courotine to allow the user to see the scatter map
    private IEnumerator ShowScatterMap()
    {
        yield return new WaitForSecondsRealtime(60*6.5f);
        sceneManagement.GetComponent<GameMangement>().SeeTheScattermap = true;
        info.SetActive(true);
        string infoText = "You can now see the scatter map each time that you activate the fluoroscopy";
        GameObject.Find("InfoError").GetComponent<TextMeshPro>().text = infoText;
        //buttonTen.SetActive(false);
        //buttonTenBis.SetActive(true);
        yield return wait;
        info.SetActive(false);
    }
    
    //Coroutine to move automatically the C-arm to the solution
    private IEnumerator TakeAnswer()
    {
        yield return new WaitForSecondsRealtime(60*8);
        zeegoAnswerRenderer.enabled = false;
        giveAnswer = true;
        zeegoPretty.TakeAnswerRotation(false);
        zeegoPhantom.TakeAnswerRotation(true);
        info.SetActive(true);
        string infoText = "This is the solution, now you need to take an image and finish the exercise";
        GameObject.Find("InfoError").GetComponent<TextMeshPro>().text = infoText;
        yield return wait;
        info.SetActive(false);
    }

    private IEnumerator AutomaticEnd()
    {
        yield return new WaitForSecondsRealtime(60*8.2f);
        buttonNine.GetComponent<Interactable>().TriggerOnClick();
    }
    
    //Function called when the user finishes the exercise and stops all the started coroutines
    public void FinishExercise()
    {
        StopAllCoroutines();
        info.SetActive(false);
    }
}
