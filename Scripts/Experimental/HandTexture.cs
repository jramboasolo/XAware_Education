using System;
using System.Collections;
using System.Threading;
using Microsoft.MixedReality.Toolkit.Experimental.RiggedHandVisualizer;
using UnityEngine;

//Class to make the hand texturage
//The script use the color from the script texture3DCreator and the hand's vertices
//The hand used are the hand "RiggedHand" from MRTK, for that you need to go MixedRealityToolkit->Input->Controller, then choose CammaControllerVisualisation or clone the current profile and add the prefab "RiggeedHand" to the Global hand visualiser
//The script compute the hand's vertices in the local cube position and the corresponding cube mapping between 0-39, then add the corressponding color
public class HandTexture : MonoBehaviour
{
    public GameObject cubePositionObject;
    private GameObject radiationCube;
    private MeshFilter meshFilter;

    //reference to store the color vector from the script texture3DCreator
    private Color[] color;

    //Vector to set the origine (0,0,0) of the cube to the left front bottom corner of the cube 
    private Vector3 radCubeOffset;

    //Matrice to go from the local position to the world position for the left hand
    private Matrix4x4 matriceLocalToWorldLeft;

    //Matrice to go from the local position to the world position for the right hand
    private Matrix4x4 matriceLocalToWorldRight;
    private Matrix4x4 matriceWorldToLocalCube;

    //Material used to see the vertices color in the game (used the material texture_0_0)
    public Material myMaterial;
    private Mesh handMeshLeft;
    private Mesh handMeshRight;
    public bool newXRaysActivation{set;private get;}
    private Thread thread;
    private Thread thread2;
    private static EventWaitHandle myEvent;
    private static EventWaitHandle myEvent2;
    private bool isWorking;

    //Color vector to store the right hand's color
    private Color32[] meshColorRight;

    //Color vector to store the left hand's color
    private Color32[] meshColorLeft;

    //Vector to store the left hand's vertices and to be able to use them in the thread
    private Vector3[] verticesLeft;

    //Vector to store the right hand's vertices and to be able to use them in the thread
    private Vector3[] verticesRight;
    private Vector3 cubePosition;
    private bool rightColorReady,leftColorReady;
    private int side = 64;

    private void Start()
    {
        radiationCube = GameObject.Find("radiationCube");
        radCubeOffset = new Vector3(1,1,1);
        radiationCube.GetComponent<texture3DCreator>().updateCloud(90,0);
        newXRaysActivation = false;
        isWorking = true;
        rightColorReady = false;
        leftColorReady = false;
        verticesLeft = null;
        verticesRight = null;
        thread = new Thread(GiveHandRightExposure);
        thread2 = new Thread(GiveHandLeftExposure);
        thread.IsBackground = true;
        thread2.IsBackground = true;
        thread.Start();
        thread2.Start();
        myEvent = new EventWaitHandle(false,EventResetMode.AutoReset);
        myEvent2 = new EventWaitHandle(false,EventResetMode.AutoReset);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            radiationCube.GetComponent<texture3DCreator>().updateCloud(-90,0);
        }
    }

    //Function called to give the color according to the cube position
    private Color getRadCubePosColor(Vector3 pos)
    {  
        int lookupPos = side*side* (int)pos[0] + side * (int) pos[1] + (int) pos[2];
        return (color[lookupPos]);
    }

    // CLIP POSITIONS WITHIN CUBE TO BE IN RANGE [MIN, MAX]
    private  bool clip(Vector3 pos, float min, float max)
    {
        for (int i = 0; i < 3; i++)
        {
            if (pos[i] < min) return false;
            if (pos[i] > max) return false;
        }

        return true;
    }

    //Function called to map the local position (after offset) into the cube position
    private Vector3 mapToCubeUnits(Vector3 pos)
    {
        pos[1] = -pos[1] + 2; // -pos[1] flips along y, 2 is the length of side of the cube
        for (int i = 0; i < 3; i++)
        {
            pos[i] = (float) System.Math.Round( (pos[i] / 2.0f) * 39.0f);
        } 
        return pos;
    }

    //Function called with the thread to compute the color for the right hand 
    private void GiveHandRightExposure()
    {
        while(isWorking)
        {
            myEvent2?.WaitOne();
            if(verticesRight!=null)
            {
                meshColorRight = new Color32[verticesRight.Length];
                for (int i = 0; i < verticesRight.Length; i++)
                {
                    Vector3 position = matriceWorldToLocalCube.MultiplyPoint3x4(matriceLocalToWorldRight.MultiplyPoint3x4(verticesRight[i])); //compute the world position of the vertice and give its local position in the radiatioàn cube
                    position += radCubeOffset; //apply the offset to the new vertice position
                    if(clip(position,0,2))
                    {
                        position = mapToCubeUnits(position);
                        try
                        {
                            meshColorRight[i] = getRadCubePosColor(position);
                        }
                        catch(Exception)
                        {
                            meshColorRight[i] = Color.blue;
                        }
                    }
                    else
                    {
                        meshColorRight[i] = Color.blue;
                    }
                    
                }
                rightColorReady = true;
            }
        }
    }

    //Function called with the thread to compute the color for the left hand
    private void GiveHandLeftExposure()
    {
        while(isWorking)
        {
            myEvent.WaitOne();
            if(verticesLeft!=null)   
            {
                meshColorLeft = new Color32[verticesLeft.Length];
                for (int i = 0; i < verticesLeft.Length; i++)
                {
                    Vector3 position = matriceWorldToLocalCube.MultiplyPoint3x4(matriceLocalToWorldLeft.MultiplyPoint3x4(verticesLeft[i])); //compute the world position of the vertice and give its local position in the radiatioàn cube
                    position += radCubeOffset; //apply the offset to the new vertice position
                    if(clip(position,0,2))
                    {
                        position = mapToCubeUnits(position);
                        try
                        {
                            meshColorLeft[i] = getRadCubePosColor(position);
                        }
                        catch(Exception)
                        {
                            meshColorLeft[i] = Color.blue;
                        }
                    }
                    else
                    {
                        meshColorLeft[i] = Color.blue;
                    }
                    
                }
                leftColorReady = true;
            }
        }
    }

    //Function called by another script to begin the hands texturage
    public void GiveHandExposure()
    {
        try //try to retrieve the gameObject of the left and right hand in the scene
        {
            GameObject rightHand = GameObject.Find("Right_RiggedHandRight(Clone)");
            GameObject leftHand = GameObject.Find("Left_RiggedHandLeft(Clone)");
            color = radiationCube.GetComponent<texture3DCreator>().GetColors();
            cubePosition = radiationCube.transform.position;
            matriceWorldToLocalCube = cubePositionObject.transform.worldToLocalMatrix;

            if(rightHand!=null)
            {
                rightHand.GetComponentInChildren<SkinnedMeshRenderer>().material = myMaterial;
                handMeshRight = rightHand.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                matriceLocalToWorldRight = rightHand.GetComponent<RiggedHandVisualizer>().Palm.localToWorldMatrix;
                verticesRight = handMeshRight.vertices;
                WaitUntil wait = new WaitUntil(RightColorReady);
                StartCoroutine(SetMeshColor(handMeshRight,wait,false));
                myEvent2.Set();
            }
            if(leftHand!=null)
            {
                leftHand.GetComponentInChildren<SkinnedMeshRenderer>().material = myMaterial;
                handMeshLeft = leftHand.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                matriceLocalToWorldLeft = leftHand.GetComponent<RiggedHandVisualizer>().Palm.localToWorldMatrix;
                verticesLeft = handMeshLeft.vertices;
                WaitUntil wait = new WaitUntil(LeftColorReady);
                StartCoroutine(SetMeshColor(handMeshLeft,wait,true));
                myEvent.Set();
            }
        }
        catch
        {

        }
    }

    private bool LeftColorReady()
    {
        return leftColorReady;
    }
    private bool RightColorReady()
    {
        return rightColorReady;
    }

    //Coroutine used to update the mesh color
    private IEnumerator SetMeshColor(Mesh _mesh,WaitUntil wait,bool isLeft)
    {
        yield return wait;
        if(isLeft)
        {
            _mesh.colors32 = meshColorLeft;
            leftColorReady = false;
        }
        else
        {
            _mesh.colors32 = meshColorRight;
            rightColorReady = false;
        }
    }

    private void OnDestroy()
    {
        isWorking = false;
        if(myEvent!=null)
            myEvent.Set();
        if(myEvent2!=null)
            myEvent2.Set();
    }
}
