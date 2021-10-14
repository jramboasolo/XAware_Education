using System;
using UnityEngine;
using UnityEngine.UI;

//Class to manage the dose bar in the status pop-up
//Need to update the mean used to go from the dose used on one exercise to the theoretical dose received for one year
public class BarDoseManagement : MonoBehaviour
{
    [SerializeField]
    private Image barDoseImage;

    [SerializeField]
    private Gradient gradient;
    
    // Start is called before the first frame update
    private void Start()
    {
        
    }

    //Function called to update the bar using the theoretical dose
    public void UpdateBar(int currentDose)
    {
        double value = 0;
        if(currentDose<=100)
        {
            value = (0.34/100)*currentDose; 
        }
        if(currentDose>100 & currentDose<2000)
        {
            value = (0.34/1900)*currentDose + 0.33;
        }
        if(currentDose>=2000)
        {
            value = (0.32/8000) * currentDose + 0.6;
        }
        if(currentDose > 10000)
            value = 1;
        barDoseImage.fillAmount = Convert.ToSingle(Math.Round(value,2));
        barDoseImage.color = gradient.Evaluate(barDoseImage.fillAmount);
    }
}
