using TMPro;
using UnityEngine;

// Class to manage the use of a mouse for the X-Ray activation
public class ClickManagement : MonoBehaviour
{
    //Reference to the Zeego and the radiation Cube
    private GameObject zeego, radiationCube;

    //Reference to the Zeego object's script: ZeegoManagement
    private ZeegoManagement zeegoManagement;

    // Boolean use to disable the click when the user has clicked on the button 'Finish the exercise'
    public bool running {private get;set;}


    // Start is called before the first frame update
    private void Start()
    {
        running = true;
        zeego = GameObject.Find("ZeegoPretty");
        radiationCube = GameObject.Find("radiationCube");
        zeegoManagement = zeego.GetComponent<ZeegoManagement>();
    }

    // Update is called once per frame
     private void Update()
    {
        if(running)
        {
            
            if(Input.GetMouseButtonDown(0)) //Check if during the frame the user pushed on the primary (left) button of the mouse
            {
                radiationCube.GetComponent<texture3DCreator>().enabled = true;
                zeegoManagement.ActiveZeego();
                
            }
            if(Input.GetMouseButtonUp(0)) //Check if during the frame the user released the primary button of the mouse
            {
                zeegoManagement.Release();
                
            }
        }
    }
}
