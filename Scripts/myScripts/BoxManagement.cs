using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

// Class used to manage the behavior of the box that hides the game object in the application (Word or Cube)
public class BoxManagement : MonoBehaviour
{
    private Vector3 initialPosition;

    // Start is called before the first frame update
    private void Start()
    {
        initialPosition = this.transform.localPosition; 
    }


    //Function used with the slider of the control menu
    //Control the transparency of the box to allow the user to see or not the game object inside
    public void UpdateAlphaValue()
    {
        GameObject slider = GameObject.Find("PinchSlider");
        float value = slider.GetComponent<PinchSlider>().SliderValue; //SliderValue: return the current value of the slider
        Color color = this.GetComponent<Renderer>().material.color; //Retrieve the current color of the box's material
        color.a = value; //Set the alpha channel of the color with the SliderValue (between 0-1) to update the transparency of the box according to the slider's value
        this.GetComponent<Renderer>().material.color = color; //Set the material's color with the new color with the updated alpha channel
    }
}
