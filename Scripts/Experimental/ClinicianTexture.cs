using System;
using System.Collections;
using System.Threading;
using UnityEngine;

//Class to make the clinician texture
//To make the texture, the script use the color vector from the script texture3DCreator and the vertices of the clinicians
//So, the script compute the position for each vertice in the radiation cube space, check if the vertice is inside the cube, find the cube position for this vertice and apply the corresponding color
//Added this script to all the clinician in the scene

public class ClinicianTexture : MonoBehaviour
{
    public GameObject cubePositionObject;
    private GameObject radiationCube;

    //reference to store the color vector from the script texture3DCreator
    private Color[] color;

    //Vector to set the origine (0,0,0) of the cube to the left front bottom corner of the cube 
    private Vector3 radCubeOffset;

    //Matrice to go from the local position to the world position
    private Matrix4x4 matriceLocalToWorld,matriceWorldToLocalCube;
    private Mesh clinicianMesh;
    private Thread thread;
    private static EventWaitHandle myEvent;
    private bool isWorking;

    //Color vector to store the clinician's color
    private Color32[] meshColor;

    //Vector to store the clinician's vertices and to be able to use them in the thread
    private Vector3[] verticesClinicien;
    private Vector3 cubePosition;
    private bool colorReady;

    //Material used to see the vertices color in the game (used the material texture_0_0)
    public Material myMaterial;
    private int side=64;

    private void Start()
    {
        radiationCube = GameObject.Find("radiationCube");
        radCubeOffset = new Vector3(1,1,1);
        radiationCube.GetComponent<texture3DCreator>().updateCloud(-90,0);
        isWorking = true;
        colorReady = false;
        verticesClinicien = null;
        thread = new Thread(ComputeClinicianExposure);
        thread.IsBackground = true;
        thread.Start();
        myEvent = new EventWaitHandle(false,EventResetMode.AutoReset);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            radiationCube.GetComponent<texture3DCreator>().updateCloud(-90,0);
        }
        if(Input.GetKeyDown(KeyCode.B))
        {
            //GiveClinicianExposure();
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
            if (pos[i] < (min-0.01f)) return false;
            if (pos[i] > (max+0.01f)) return false;
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

    //Function called with the thread to compute the color
    private void ComputeClinicianExposure()
    {
        while(isWorking)
        {
            myEvent.WaitOne();
            if(verticesClinicien!=null)
            {
                meshColor = new Color32[verticesClinicien.Length];
                for (int i = 0; i < verticesClinicien.Length; i++)
                {
                    Vector3 position = matriceWorldToLocalCube.MultiplyPoint3x4(matriceLocalToWorld.MultiplyPoint3x4(verticesClinicien[i])); //compute the world position of the vertice and give its local position in the radiatioàn cube
                    position += radCubeOffset; //apply the offset to the new vertice position
                    if(clip(position,0,2))
                    {
                        position = mapToCubeUnits(position);
                        try
                        {
                            meshColor[i] = getRadCubePosColor(position);
                        }
                        catch(Exception)
                        {
                            meshColor[i] = Color.blue;
                        }
                    }
                    else
                    {
                        meshColor[i] = Color.blue;
                    }
                    
                }
                colorReady = true;
            }
        }   
    }

    //Function called by another script to begin the clinician texturage
    public void GiveClinicianExposure()
    {
        color = radiationCube.GetComponent<texture3DCreator>().GetColors();
        cubePosition = radiationCube.transform.position;
        matriceLocalToWorld = this.transform.localToWorldMatrix;
        matriceWorldToLocalCube = cubePositionObject.transform.worldToLocalMatrix;
        try
        {
            clinicianMesh = this.GetComponent<MeshFilter>().mesh;
            this.GetComponent<Renderer>().material = myMaterial;
            verticesClinicien = clinicianMesh.vertices;
            WaitUntil wait = new WaitUntil(ColorReadyToSet);
            StartCoroutine(SetMeshColor(clinicianMesh,wait));
            myEvent.Set();
        }
        catch
        {

        }
    }

    private bool ColorReadyToSet()
    {
        return colorReady;
    }

    //Coroutine used to update the mesh color
    private IEnumerator SetMeshColor(Mesh _mesh,WaitUntil wait)
    {
        yield return wait;
        _mesh.colors32 = meshColor;
        colorReady = false;
    }

    private void OnDestroy()
    {
        isWorking = false;
        if(myEvent!=null)
            myEvent.Set();
    }
}
