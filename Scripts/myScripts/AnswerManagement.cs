using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

//Class to manage the survey pop-up asking how many times do you use the x-rays
public class AnswerManagement : MonoBehaviour
{
    //Reference to the slider on the pop-up
    private PinchSlider slider;

    //Reference to the Text display on the pop-up
    private TextMeshPro text;

    //Function called when the user changes the slider's value, and update the text's info
    public void UpdateValueSlider()
    {
        slider = GameObject.Find("PinchSliderAnswer").GetComponent<PinchSlider>();
        text = GameObject.Find("QuestionUseXRays").GetComponent<TextMeshPro>();
        int value = (int)(slider.SliderValue*30) ; //SliderValue: return the current value of the slider
        if(value<=1)
        {
            text.text = string.Format("How many times do you think you have activated the X-rays: {0} time",value);
        }
        else
        {
            text.text = string.Format("How many times do you think you have activated the X-rays: {0} times",value);
        }
    }
}
