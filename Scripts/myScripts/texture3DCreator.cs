using UnityEngine;
using System.IO;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.SceneManagement;

// Class to manage the scattermap
public class texture3DCreator : MonoBehaviour
{
    //Texture3D torepresent the scattermap
    private Texture3D vol;

    [SerializeField]
    private int side = 64;
    private GameObject radiationCube;

    // provider for the conversion
    private  NumberFormatInfo provider;

    //Values of alpha used to retrieve the alpha value from the data in the files
    private  float meanAlpha = 0.4f,minAlpha = 0, maxAlpha = 0.8f;

    //Thread to read the file where the information are and to make the computations for the texture3D
    private Thread thread; 
    private Color[] cols;
    private Color[] colors;

    //Objet used to keep waiting the thread
    private static EventWaitHandle myEvent; 

    //Boolean used to manage the thread
    private bool finished,presenceShield,endReadScatterMap; /*threadWorking,*/
    public bool isRightShield {set;private get;}
    private byte[] allLinesBytes;
    private int angleTheta,anglePhi;
    private string shieldPath;
    private Renderer radiationCubeRenderer,radiographyRenderer;
    private RadiographyManagement radiographyManagement;
    private GameObject radiography;
    private WaitUntil wait;

    //List of clinicians in the scene
    [SerializeField]
    private GameObject[] clinicians;

    // for initialization
    private void Start () 
    {
        shieldPath = "withoutShield";
        angleTheta = 0;
        anglePhi = 0;
        radiationCube = GameObject.Find("radiationCube");
        radiationCubeRenderer = radiationCube.GetComponent<Renderer>();
        radiography = GameObject.Find("radiography");
        if(radiography!=null)
        {
            radiographyRenderer = GameObject.Find("radiography").GetComponent<Renderer>();
            radiographyManagement = GameObject.Find("radiography").GetComponent<RadiographyManagement>();
        }
        finished = false;
        thread = new Thread(create3DTex);
        presenceShield = false;
        //threadWorking = false;
        isRightShield = false;
        endReadScatterMap = false;
        wait = new WaitUntil(EndReadScatterMap);
        vol = new Texture3D(side, side, side, TextureFormat.RGBA4444, false); // 3d texture to be visualized on the cube
        vol.wrapMode = TextureWrapMode.Clamp;  // clamp so that texture map is not repeated periodically outside the domain of the cube also need to be careful about this in the shader
        myEvent = new EventWaitHandle(false,EventResetMode.AutoReset);
        thread.Start();
    }

    // Update is called once per frame
    //Check if the thread is working and finished this work to update the texture3D of the scatter map
    /*private void Update() 
    {   
        if(thread.ThreadState == ThreadState.WaitSleepJoin & threadWorking)
        {
            // copy the 3D texture to GPU and apply to the shader
            vol.SetPixels(cols);
            vol.Apply();

            //if(GameObject.Find("SceneManagementObject").GetComponent<GameMangement>().SeeTheScattermap)//Check if the user wants to see the Scatter Map
            //{
                // update the shader's texture with the newly copied texture and enable the renderer
                radiationCubeRenderer.material.SetTexture("_Volume", vol);
                //radiationCubeRenderer.enabled = true;
            //}

            //if(!GameObject.Find("ButtonFive").GetComponent<Interactable>().HasPress & !Input.GetMouseButton(0)) //Check if the button on the Menu or the primary button of the mouse are pressed
            //{
              //  StartCoroutine(TimeManagememt());
            //}
            
            foreach(GameObject clinician in clinicians)//Look for each clinician in clinicians to load the texture and search the dose and exposure
            {
                if(isRightShield & presenceShield)//If use of the shield and these shields are the right shield, send -angleTheta to the clincian function to make the symmetric because we have the data for the clinician and the shield only for the left shield
                {                                 //So we need to take the opposite for the angleTheta to retrieve the information where the clinician is on the shield side
                    clinician.GetComponent<ClinicianManagement>().LoadClinicianTexture(-angleTheta,anglePhi,true);
                    Vector3 scale = clinician.transform.localScale;
                    scale.x = -11; //use -11 for the scale to flip the texture
                    clinician.transform.localScale = scale;
                }
                else
                {
                    clinician.GetComponent<ClinicianManagement>().LoadClinicianTexture(angleTheta,anglePhi,false);
                    Vector3 scale = clinician.transform.localScale;
                    scale.x = 11;
                    clinician.transform.localScale = scale;
                }
            }
            threadWorking = false;
        }
    }*/

    // adds local path of the 'StreamingAssets' folderin front of the supplied filename
    private string getFilePath(string filename)
    {
        string path="";
        try
        {
            path = Path.Combine(Application.streamingAssetsPath, "ScatterMap",shieldPath,filename);
        }
        catch
        {
            path = Path.Combine(Application.streamingAssetsPath, "ScatterMap","withoutShield",filename);
        }
        return path;
    }

    //Return colors
    public Color[] GetColors()
    {
        return colors;
    }

    // 3d texture creator
    private void create3DTex()
    {
        do
        {
            myEvent.WaitOne();
            
            if(finished)
            {
                return;
            }

            //threadWorking = true;

            string filedata = "";
            filedata = System.Text.Encoding.ASCII.GetString(allLinesBytes); // filedata contains all lines in the txt file (\n splits the lines)
            string[] allLines = filedata.Split('\n');
            string line = ""; // volume text file to be read one line at a time
            string[] entries;
            cols = new Color[side * side * side]; // colors
            colors = new Color[side*side*side];
            int nlines = allLines.Length-1; // give the number of lines in one file, normaly 64k lines in total in each text file

            Color c = Color.black;

            provider =  new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            
            List<float> grayValue = new List<float>(); //list of all the gray value in the file, use to compute the mean gray value
            float mean = 0;
            float var = 0;

            for (int nline = 0; nline < nlines; nline++)
            {              
                line = allLines[nline]; // read a line from the volume text file               
                entries = line.Split(null); // split the string using whitespace as splitter
                grayValue.Add((float)Convert.ToDouble(entries[3],provider));
                mean += (float)Convert.ToDouble(entries[3],provider);
                var += ((float)Convert.ToDouble(entries[3],provider) * (float)Convert.ToDouble(entries[3],provider));        
            }

            grayValue.Sort();
            mean = mean/(grayValue.Count); //Mean of the gray values
            float grayValueMax = grayValue[grayValue.Count-1];
            float grayValueMin = grayValue[0];
            int numberOfVoxels = Convert.ToInt32(Mathf.Pow(nlines,1.0f/3.0f)) - 1;
            for (int i = 0; i < nlines; i++)
            {
                line = allLines[i]; // read a line from the volume text file
                entries = line.Split(null); // split the string using whitespace as splitter
                // compute index of string's color in colors array using xyz
                int x = Convert.ToInt16(entries[0]);
                int y = Convert.ToInt16(entries[1]);
                int z = Convert.ToInt16(entries[2]);
                int index =  side * side * x + side * y + z ;
                if(isRightShield)//If we use the right shield we use (39-x) to store the data to flip the scatter map around the x axis
                    index = side*side*(numberOfVoxels-x) + side*y +z;
                int index2 = side*side*x + side *y + z;
                c = GetColor((float)Convert.ToDouble(entries[3],provider),grayValueMin,grayValueMax,mean);
                //if(c.b > 0.2f)
                  //  c.a = 0;
                cols[index] = c;
                colors[index2] = c;
            }
            endReadScatterMap = true;

        }while(true);
    } //3D texture creator ends
    
    // Load cloud from the theta and phi angle
    public void updateCloud(int _angleTheta, int _anglePhi)
    {
        angleTheta = _angleTheta;
        anglePhi = _anglePhi;
        string filename = string.Format("scatter-1M_{0}_{1}_Energy.txt",angleTheta,anglePhi);
        if(isRightShield)
            filename = string.Format("scatter-1M_{0}_{1}_Energy.txt",-angleTheta,anglePhi);  
        string volumeName = getFilePath(filename);
#if UNITY_WINRT
        allLinesBytes = UnityEngine.Windows.File.ReadAllBytes(volumeName);
#else
        allLinesBytes = System.IO.File.ReadAllBytes(volumeName);
#endif
        StartCoroutine(ApplyScatterMap());
        myEvent?.Set(); // A?.B: B is not evaluate if A is null
    }

    // Coroutine to manage the timer for the scattermap, after 0.5s in real time, the radiation cube is no longer visible
    // Coroutine is only called when the pressure by the user is shorter than 0.6s in real time
    private IEnumerator TimeManagememt()
    {
        yield return new WaitForSecondsRealtime(0.5f); //Wait 0.5s to allow the user to see the scatter map
        radiationCubeRenderer.material.SetTexture("_Volume", null);
        radiationCubeRenderer.enabled = false;

        if(radiography!=null)
        {
            radiographyRenderer.enabled = true;
            radiographyManagement.loadTexture(angleTheta,anglePhi);
        }
    }

    //Coroutine used to apply the color for the texture 3D, to show the texture if need and load for each clinician the corresponding texture
    private IEnumerator ApplyScatterMap()
    {
        yield return wait;
        endReadScatterMap = false;

        // copy the 3D texture to GPU and apply to the shader
        vol.SetPixels(cols);
        vol.Apply();

        if(SceneManager.GetActiveScene().name=="Scenario2Scene") //if scenario 2 just apply the texture to the material
        {
            radiationCubeRenderer.material.SetTexture("_Volume", vol);
            yield break;
        }
        
        if(GameObject.Find("SceneManagementObject").GetComponent<GameMangement>().SeeTheScattermap)//Check if the user wants to see the Scatter Map
        {
            // update the shader's texture with the newly copied texture and enable the renderer
            radiationCubeRenderer.material.SetTexture("_Volume", vol);
            radiationCubeRenderer.enabled = true;
        }

        if(!GameObject.Find("ButtonFive").GetComponent<Interactable>().HasPress & !Input.GetMouseButton(0)) //Check if the button on the Menu or the primary button of the mouse are pressed
        {
            StartCoroutine(TimeManagememt());
        }
        
        foreach(GameObject clinician in clinicians)//Look for each clinician in clinicians to load the texture and search the dose and exposure
        {
            if(isRightShield & presenceShield)//If use of the shield and these shields are the right shield, send -angleTheta to the clincian function to make the symmetric because we have the data for the clinician and the shield only for the left shield
            {                                 //So we need to take the opposite for the angleTheta to retrieve the information where the clinician is on the shield side
                clinician.GetComponent<ClinicianManagement>().LoadClinicianTexture(-angleTheta,anglePhi,true);
                Vector3 scale = clinician.transform.localScale;
                scale.x = -11; //use -11 for the scale to flip the texture
                clinician.transform.localScale = scale;
            }
            else
            {
                clinician.GetComponent<ClinicianManagement>().LoadClinicianTexture(angleTheta,anglePhi,false);
                Vector3 scale = clinician.transform.localScale;
                scale.x = 11;
                clinician.transform.localScale = scale;
            }
        }
    }

    // Stop the thread and release the resource
    // Unity Function called when the object is destroyed
    private void OnDestroy()
    {
        finished = true;
        myEvent?.Set();
    }

    // Get the color depending of the gray value, the min gray value and the max gray value
    private Color GetColor(float grayValue,float grayValueMin, float grayValueMax,float mean)
    {
        Color color = Color.white;
        float distance = grayValueMax - grayValueMin;

        // Compute the linear regressions between 0 and 0.4 for the value alpha and between 0.4 and 0.8 also for alpha value
        float coefficentRegression1 =( (maxAlpha+meanAlpha) * (grayValueMax+mean) - 2 *(maxAlpha*grayValueMax + mean * meanAlpha) )/ ((grayValueMax+mean)*(grayValueMax+mean) - 2 * (grayValueMax*grayValueMax+mean*mean));
        float coefficentRegression2 = ( (minAlpha+meanAlpha) * (grayValueMin+mean) - 2 *(minAlpha*grayValueMin + mean * meanAlpha) )/ ((grayValueMin+mean)*(grayValueMin+mean) - 2 * (grayValueMin*grayValueMin+mean*mean));
        float origine1 = (maxAlpha-meanAlpha)/2 - coefficentRegression1 * mean;
        float origine2 = (meanAlpha-minAlpha)/2 - coefficentRegression2 * mean;

        if(grayValue >= grayValueMax)
        {
            grayValue = grayValueMax;
            color = Color.red;
            color.a = maxAlpha;
        }
        else if(grayValue <= grayValueMin)
        {
            grayValue = grayValueMin;
            color = Color.blue;
            color.a = minAlpha;
        }
        else
        {
            if(grayValue < mean)
            {
                color.a = coefficentRegression2 * grayValue + origine2;

            }
            else
            {
                color.a = coefficentRegression1 * grayValue + origine1;

            }
        }

        if (grayValue < (grayValueMin + 0.25 * distance)) {
            color.r = 0;
            color.g =  4 * (grayValue - grayValueMin) / distance;
        } else if (grayValue < (grayValueMin + 0.5 * distance)) {
            color.r = 0;
            color.b = 1 + 4 * (grayValueMin + 0.25f * distance - grayValue) / distance;
        } else if (grayValue < (grayValueMin + 0.75 * distance)) {
            color.r = 4 * (grayValue - grayValueMin - 0.5f * distance) / distance;
            color.b = 0;
        } else {
            color.g = 1 + 4 * (grayValueMin + 0.75f * distance - grayValue) / distance;
            color.b = 0;
        }

        return color;
    }


    //Function called when the user removes the shield and for each clinician in the list clincians call the function RemoveShield
    //So each time that the user removes the shields this function is called and avoid some problems that can appear with ChangeShieldPath
    public void RemoveShield()
    {
        shieldPath = "withoutShield";
        presenceShield = false;
        foreach (GameObject clinician in clinicians)
        {
            try
            {
                clinician.GetComponent<ClinicianManagement>().RemoveShield();
            }
            catch
            {

            }
        }
    }

    //Function called when the user adds the shield and for each clinician in the list clincians call the function AddShield
    //So each time that the user adds the shields this function is called and avoid some problems that can appear with ChangeShieldPath
    public void AddShield()
    {
        shieldPath = "withShield";
        presenceShield = true;
        foreach (GameObject clinician in clinicians)
        {
            try
            {
                clinician.GetComponent<ClinicianManagement>().AddShield(isRightShield);
            }
            catch
            {

            }
        }
    }

    //Return boolean presenceShield
    public bool GetPresenceShield()
    {
        return presenceShield;
    }

    private bool EndReadScatterMap()
    {
        return endReadScatterMap;
    }
    
    //Function called each x-rays activation to check if the user uses the shields
    public string GetInfoErrorShield()
    {
        bool isSideA = clinicians[0].GetComponent<ClinicianManagement>().isSideA;
        if(presenceShield & ((isRightShield&isSideA)|(!isRightShield&!isSideA)))
        {
            return "";   
        }
        else if((isRightShield&!isSideA)|(!isRightShield&isSideA))
        {
            return "You should used the shields on the same side as the clinician\n";
        }
        else
        {
            return "You forgot the shields\n";
        }
        
    }
}
