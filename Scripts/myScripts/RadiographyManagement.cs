using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Class to manage the game training's radiography in the scene
public class RadiographyManagement : MonoBehaviour
{
    //Path of the texure, name of game training and name of the texture
    private string pathTexture,nameGame,nameTexture; 
    private byte[] dataTexture;
    private int sizeTexture;

    //The radiography is represented like a texture
    private Texture2D texture; 

    //Rotation z initial of the game training, the current phi and theta angles
    private int YOrientationGame,phi,theta,position; 
    private Renderer _renderer;
    private bool YesNo;
    private GameObject YesNoObject,CubeObject;

    // List to store all the best radiographies
    private List<RadiographieData> responseData;

    //List of possible angle for the cube
    private List<int> cubeAngle;
    

    // Start is called before the first frame update
    //Add to the respondData list all the information for each response (angle theta and phi)
    //Retrieve the angle of rotation around the y axis
    private void Start()
    {
        nameGame = "cube";
        YesNo = false;
        sizeTexture = 256;
        YesNoObject = GameObject.Find("Yes_No");
        CubeObject = GameObject.Find("TrainingCube");
        YOrientationGame = Convert.ToInt32(CubeObject.transform.localEulerAngles.y);
        if(YOrientationGame > 180)
        {
            YOrientationGame = YOrientationGame - 360;
        }
        cubeAngle = new List<int>{-120,-115,-110,-105,-100,-95,-90,-85,-80,-75,-70,-65,-60,60,65,70,75,80,85,90,95,100,105,110,115,120};
        position = UnityEngine.Random.Range(0,cubeAngle.Count); 
        Vector3 currentAngle = CubeObject.transform.localEulerAngles;
        currentAngle.z = cubeAngle[position];
        currentAngle.y = 0;
        CubeObject.transform.localEulerAngles = currentAngle;
        texture = new Texture2D(sizeTexture,sizeTexture);
        responseData = new List<RadiographieData>();
        YOrientationGame = Convert.ToInt32(YesNoObject.transform.localEulerAngles.y);
        theta = 90;
        phi = 0;
        RetrieveAngle(ref theta,ref phi,YOrientationGame);
        responseData.Add(new RadiographieData{angleTheta = theta,anglePhi =phi,isYesNo = true});
        theta = -90;
        phi = 0;
        RetrieveAngle(ref theta,ref phi,YOrientationGame);
        responseData.Add(new RadiographieData{angleTheta = theta,anglePhi = phi,isYesNo = true});
        theta = 0;
        phi = 90;
        RetrieveAngle(ref theta,ref phi,YOrientationGame);
        responseData.Add(new RadiographieData{angleTheta = theta,anglePhi =phi,isYesNo = true});
        theta = 0;
        phi = -90;
        RetrieveAngle(ref theta,ref phi,YOrientationGame);
        responseData.Add(new RadiographieData{angleTheta = theta,anglePhi =phi,isYesNo = true});
        theta = 180;
        phi = 90;
        RetrieveAngle(ref theta,ref phi,YOrientationGame);
        responseData.Add(new RadiographieData{angleTheta = theta,anglePhi =phi,isYesNo = true});
        theta = 180;
        phi = -90;
        RetrieveAngle(ref theta,ref phi,YOrientationGame);
        responseData.Add(new RadiographieData{angleTheta = theta,anglePhi =phi,isYesNo = true});
        theta = 0;
        phi = 60;
        RetrieveAngle(ref theta,ref phi,cubeAngle[position]);
        responseData.Add(new RadiographieData{angleTheta = theta,anglePhi =phi,isYesNo = false});
        theta = 0;
        phi = 0;
        _renderer = this.GetComponent<Renderer>();
        YOrientationGame = cubeAngle[position];
        loadTexture(theta,phi);
    }

    //Function called to load the texture from the theta and phi angle
    //Take as input the current theta and phi of the C-arm
    //Update this angle thanks to the function UpdateAngle to load the corresponding radiography
    public void loadTexture(int angleTheta,int anglePhi)
    {
        theta = angleTheta;
        phi = anglePhi;
        UpdateAngle(ref theta,ref phi);
        nameTexture = string.Format("{0}_{1}_{2}.jpg",nameGame,theta,phi);
        string name = Path.Combine(Application.streamingAssetsPath,nameGame,nameTexture);
#if UNITY_WINRT
        dataTexture = UnityEngine.Windows.File.ReadAllBytes(name);
#else
        dataTexture = System.IO.File.ReadAllBytes(name);
#endif
        texture.LoadImage(dataTexture,true);
        _renderer.material.SetTexture("_MainTex",texture);
    }

    //Function called to change the current name Game and updates the variable of the y rotation
    public void YesNoUpload()
    {
        nameGame = "yesno";
        YesNo = true;
        YOrientationGame = Convert.ToInt32(YesNoObject.transform.localEulerAngles.y);
    }

    //Function called to change the current name Game and updates the variable of the y rotation
    public void CubeUpload()
    {
        nameGame = "cube";
        YesNo = false;
        YOrientationGame = cubeAngle[position];
    }

    //Fonction call when Vuforia find the pattern and enables the right renderer
    public void ShowGame()
    {
        if(YesNo)
        {
            YesNoObject.GetComponent<Renderer>().enabled = true;
            CubeObject.GetComponent<Renderer>().enabled = false;
        }
        else
        {
            YesNoObject.GetComponent<Renderer>().enabled = false;
            CubeObject.GetComponent<Renderer>().enabled = true;
        }
    }

    //Function to update the theta and phi angles according to the Yorientation of the game training
    //Logic behind this function:
    //The C-arm source is assumed to be at the P position, it is subject to two rotations on the z-axis of a theta angle and on the x-axis of a phi angle.
    //In addition, the rotation of the object is also applied to the source to allow to always visualize the object at the same angle, so the source also is subject to a rotation according to the y-axis of an alpha angle
    //So we get the new position of the source M such as M=RyRxRz where the R correspond to the rotation matrices according to y,x or z.
    //However one can also reach this position M with two rotation on z of a theta2 angle and on x of a phi2 angle.
    //So we also get the following formula M =Rx2,Rz2.
    //So finally we have RyRxRz = Rx2Rz2 and by applying the calculations we get the formulas in the function
    //Here the unknown are theta2 and phi2 angles in the rotation matrices Rz2 zt Rx2
    private void UpdateAngle(ref int angleTheta,ref int anglePhi)
    {
        float radianTheta = angleTheta*Mathf.PI/180;
        float radianPhi = anglePhi*Mathf.PI/180;
        float alpha = YOrientationGame*Mathf.PI/180;
        float sinTheta = Mathf.Cos(alpha) * Mathf.Sin(radianTheta) - Mathf.Sin(alpha)*Mathf.Sin(radianPhi)*Mathf.Cos(radianTheta);
        float cosTheta = Mathf.Sqrt(Mathf.Pow(Mathf.Cos(radianPhi)*Mathf.Cos(radianTheta),2) + Mathf.Pow(Mathf.Sin(alpha)*Mathf.Sin(radianTheta) + Mathf.Cos(alpha)*Mathf.Sin(radianPhi)*Mathf.Cos(radianTheta),2));
        angleTheta = Convert.ToInt32(Mathf.Atan2(sinTheta,cosTheta) * 180 / Mathf.PI);
        if(Math.Abs(angleTheta)%5 >2)
        {
            angleTheta = Convert.ToInt32(Math.Sign(angleTheta)*(Math.Abs(angleTheta)+(5-Math.Abs(angleTheta)%5)));
        }
        else
        {
            angleTheta = Convert.ToInt32(Math.Sign(angleTheta)*(Math.Abs(angleTheta) - Math.Abs(angleTheta)%5));
        }
        float cosPhi = (Mathf.Cos(radianPhi)*Mathf.Cos(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
        float sinPhi = (Mathf.Sin(alpha)*Mathf.Sin(radianTheta) + Mathf.Cos(alpha)*Mathf.Sin(radianPhi)*Mathf.Cos(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
        anglePhi = Convert.ToInt32(Mathf.Atan2(sinPhi,cosPhi)*180/Mathf.PI);
        if(Math.Abs(anglePhi)%5 >2)
        {
            anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi)+(5-Math.Abs(anglePhi)%5)));
        }
        else
        {
            anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi) - Math.Abs(anglePhi)%5));
        }
        if(Mathf.Abs(anglePhi)>90)
        {
            angleTheta = Convert.ToInt32(Mathf.Atan2(sinTheta,-cosTheta) * 180 / Mathf.PI);
            angleTheta -= (angleTheta%5);
            cosPhi = (Mathf.Cos(radianPhi)*Mathf.Cos(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
            sinPhi = (Mathf.Sin(alpha)*Mathf.Sin(radianTheta) + Mathf.Cos(alpha)*Mathf.Sin(radianPhi)*Mathf.Cos(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
            anglePhi = Convert.ToInt32(Mathf.Atan2(sinPhi,cosPhi)*180/Mathf.PI);
            if(Math.Abs(anglePhi)%5 >2)
            {
                anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi)+(5-Math.Abs(anglePhi)%5)));
            }
            else
            {
                anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi) - Math.Abs(anglePhi)%5));
            }
        }
    }

    // Give the angle to need to retrieve to find the best radiography
    //Here the logic is the same as the function UpdateAngle, juste here the unknown are theta and phi in the rotation matrices Rz and Rx
    //So we just need to multiply by the inverse of the Ry in the formule RyRxRz=Rx2Rz2 to retrieve the formule in the function
    private void RetrieveAngle(ref int angleTheta,ref int anglePhi,int alphaDegree)
    {
        float radianTheta = angleTheta * Mathf.PI/180;
        float radianPhi = anglePhi * Mathf.PI/180;
        float alpha = alphaDegree * Mathf.PI/180;
        float sinTheta = Mathf.Cos(alpha)*Mathf.Sin(radianTheta) + Mathf.Sin(alpha)*Mathf.Sin(radianPhi)*Mathf.Cos(radianTheta);
        float cosTheta = Mathf.Sqrt(Mathf.Pow(Mathf.Cos(radianPhi)*Mathf.Cos(radianTheta),2) + Mathf.Pow(Mathf.Cos(radianTheta)*Mathf.Cos(alpha)*Mathf.Sin(radianPhi) - Mathf.Sin(alpha)*Mathf.Sin(radianTheta),2));
        angleTheta = Convert.ToInt32(Mathf.Atan2(sinTheta,cosTheta)*180/Mathf.PI);
        if(Math.Abs(angleTheta)%5 >2)
        {
            angleTheta = Convert.ToInt32(Math.Sign(angleTheta)*(Math.Abs(angleTheta)+(5-Math.Abs(angleTheta)%5)));
        }
        else
        {
            angleTheta = Convert.ToInt32(Math.Sign(angleTheta)*(Math.Abs(angleTheta) - Math.Abs(angleTheta)%5));
        }
        float cosPhi = (Mathf.Cos(radianPhi)*Mathf.Cos(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
        float sinPhi = (Mathf.Cos(radianTheta)*Mathf.Cos(alpha)*Mathf.Sin(radianPhi) - Mathf.Sin(alpha)*Mathf.Sin(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
        anglePhi = Convert.ToInt32(Mathf.Atan2(sinPhi,cosPhi)*180/Mathf.PI);
        if(Math.Abs(anglePhi)%5 >2)
        {
            anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi)+(5-Math.Abs(anglePhi)%5)));
        }
        else
        {
            anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi) - Math.Abs(anglePhi)%5));
        }
        if(Mathf.Abs(anglePhi)>90)
        {
            angleTheta = Convert.ToInt32(Mathf.Atan2(sinTheta,-cosTheta)*180/Mathf.PI);
            angleTheta +=(angleTheta%5);
            angleTheta -= (angleTheta%5);
            cosPhi = (Mathf.Cos(radianPhi)*Mathf.Cos(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
            sinPhi = (Mathf.Cos(radianTheta)*Mathf.Cos(alpha)*Mathf.Sin(radianPhi) - Mathf.Sin(alpha)*Mathf.Sin(radianTheta))/Mathf.Cos(angleTheta*Mathf.PI/180);
            anglePhi = Convert.ToInt32(Mathf.Atan2(sinPhi,cosPhi)*180/Mathf.PI);
            if(Math.Abs(anglePhi)%5 >2)
            {
                anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi)+(5-Math.Abs(anglePhi)%5)));
            }
            else
            {
                anglePhi = Convert.ToInt32(Math.Sign(anglePhi)*(Math.Abs(anglePhi) - Math.Abs(anglePhi)%5));
            }
        }
    }

    //Give the closer best radiography from the current zeego angles
    //Consider the differents answers as points and compute a distance
    public void Answer(ref int zeegoTheta,ref int zeegoPhi)
    {
        int lastPhi = 0;
        int lastTheta = 0;
        int lastDistance = -1;
        int currentDistance = 0;
        foreach (RadiographieData data in responseData) //Look for each data into the list responseData
        {
            if(YesNo & data.isYesNo)//check if the angles of the answer are belove 130 and if the values correspond to the YesNo object, because just this object has severals solutions
            {
                if(lastDistance < 0)
                {
                    lastPhi = data.anglePhi;
                    lastTheta = data.angleTheta;
                    currentDistance = (zeegoTheta-data.angleTheta) * (zeegoTheta-data.angleTheta) + (zeegoPhi-data.anglePhi) * (zeegoPhi-data.anglePhi);//Compute a distance
                    lastDistance = currentDistance;
                }
                else
                {
                    currentDistance = (zeegoTheta-data.angleTheta) * (zeegoTheta-data.angleTheta) + (zeegoPhi-data.anglePhi) * (zeegoPhi-data.anglePhi);
                    if(currentDistance < lastDistance)//Store this distance if belove that the distance already store
                    {
                        lastPhi = data.anglePhi;
                        lastTheta = data.angleTheta;
                        lastDistance = currentDistance;
                    }
                }
            }
            if(!data.isYesNo & !YesNo)
            {
                lastPhi = data.anglePhi;
                lastTheta = data.angleTheta;
            }
        }
        zeegoTheta = lastTheta;
        zeegoPhi = lastPhi;
    }

    // Compute a scoring for the current radiography wwhen the user end the training
    // Scoring compute thanks to a Gaussien 2D
    //thetaMean and phiMean are the theta and phi of the answer
    public int TargetProjectionSystem(int thetaMean,int phiMean)
    {
        float sigma = 12.5f;
        UpdateAngle(ref thetaMean,ref phiMean);
        return Convert.ToInt32(Mathf.Exp(-(((theta-thetaMean) * (theta-thetaMean)  + (phi-phiMean) * (phi-phiMean)) / (2*sigma * sigma))) * 100);
    }

    // Internal class to define the angle theta and phi for a radiography
    internal class RadiographieData
    {
        public int angleTheta;
        public int anglePhi;
        public bool isYesNo;
    }

}
