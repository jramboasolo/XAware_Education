using System;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using UnityEngine;

//Class to manage the keyboard control
//Allow to open a keyboard when the tcp manager doesn't find the server (for example if the ip adress change)
//And allow to redefine the ip adress for the tcp manager

public class KeyboardManagement : MonoBehaviour
{

    private GameObject keyboard,tcpManager,menu;

    // Start is called before the first frame update
    private void Start()
    {
        keyboard = GameObject.Find("KeyboardSystem"); //reference to the keyboard in the scene
        tcpManager = GameObject.Find("TcpManager"); //reference to the tcp manager in the scene
    }

    //Function called to open a new keyboard
    public void openKeyboard()
    {
        keyboard.GetComponent<MixedRealityKeyboard>().ShowKeyboard();
    }

    //Function called when the user submit a new message and send it to the tcp manager
    public void SendIPAdress()
    {
        try
        {
            tcpManager.GetComponent<TcpClientHoloLens>().ipServer = keyboard.GetComponent<MixedRealityKeyboard>().Text;
        }
        catch(Exception)
        {

        }
        
    }

}
