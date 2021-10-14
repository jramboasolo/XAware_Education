using System;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using System.Threading;
using System.Collections;

//Class to manage creation of the scatter map with shields from the scatter map without shield
//Used the ratcast system and the trigger system to know if a shield is present in the cube or not
//Need to add the tag "TableShield" or "UpperShield" to the shields in the scene (part 7 for the upper shield left or right and part 3 for the table shield)
//Need to add the layer "cube" to make the raycast on the cube
//The script compute the transformation matrice used to know if a point is inside the volume defined by the shield protection
//We need to make a correspondance between the point in the scene (hits points and point on the shield) and the normalized point ((0,0,0,1) or (0,0,1,1) for example)
//Added this script to the radiation cube

public class ShieldScatterMapCreation : MonoBehaviour
{
    public GameObject cubePositionObject;
    private RaycastHit hits;
    private int layerMask;

    //Vector to set the origine (0,0,0) of the cube to the left front bottom corner of the cube 
    private Vector3 radCubeOffset;
    private float minXUpper,minYUpper,minZUpper,maxXUpper,maxYUpper,maxZUpper,minXTable,minYTable,minZTable,maxXTable,maxYTable,maxZTable;

    //texture3D reference
    private Texture3D vol;
    private int side = 64;
    private GameObject radiationCube;

    //Matrice with the normalized point (ex: (0,0,0,1) or (0,0,1,1))
    private Matrix<double> matrice;

    //Transformation matrice
    private Matrix<double> transformationMatrice,tableTransformationMatrice;
    private Vector3 shieldPosition;
    private Thread thread;
    private static EventWaitHandle myEvent;
    private bool isWorking,colorReady,alreadyCall;
    private Vector3 radiationCenter;

    //reference to store the color vector from the script texture3DCreator
    private Color[] color;
    private WaitUntil wait;
    public GameObject cubeTableShield1;
    public GameObject cubeTableShield2;
    public GameObject cubeTableShield3;
    public GameObject cubeTableShield4;
    private Matrix4x4 matriceWorldToLocalCube;

    // Start is called before the first frame update
    private void Start()
    {
        layerMask = 1 << 9;
        radCubeOffset = new Vector3(1,1,1);
        minXUpper = 40;
        minYUpper = 40;
        minZUpper = 40;
        maxXUpper=0;
        maxYUpper=0;
        maxZUpper=0;
        minXTable = 40;
        minYTable = 40;
        minZTable = 40;
        maxXTable=0;
        maxYTable=0;
        maxZTable=0;
        radiationCenter = this.transform.position;
        isWorking = false;
        alreadyCall = false;
        color = null;
        transformationMatrice = null;
        tableTransformationMatrice = null;
        wait = new WaitUntil(ColorReady);
        thread = new Thread(CreateColorScatterMapShield);
        thread.IsBackground = true;
        thread.Start();
        myEvent = new EventWaitHandle(false,EventResetMode.AutoReset);
        vol = new Texture3D(side, side, side, TextureFormat.RGBA4444, false);
        radiationCube = GameObject.Find("radiationCube");
        matrice = Matrix<double>.Build.DenseOfArray(new double[,]{
            {0,1,0,1,0,1,0,1},
            {0,0,1,1,1,1,0,0},
            {0,0,0,0,1,1,1,1},
            {1,1,1,1,1,1,1,1}
        });
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.N))
        {
            Color[] cols = UpdateColor(this.GetComponent<texture3DCreator>().GetColors());
             // copy the 3D texture to GPU and apply to the shader
            vol.SetPixels(cols);
            vol.Apply();
            this.GetComponent<Renderer>().material.SetTexture("_Volume", vol);
            this.GetComponent<Renderer>().enabled = true;
        }
        if(Input.GetKeyDown(KeyCode.L))
        {
            radiationCube.GetComponent<texture3DCreator>().updateCloud(-90,0);
        }
        if(Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(ApplyColor());
        }
    }

    public void CreateScatterMap()
    {
        StartCoroutine(ApplyColor());
    }

    public void SetScatterMapWithoutShield()
    {
        radiationCube.GetComponent<texture3DCreator>().updateCloud(-90,0);
    }

    //Function called by the thread to compute the new color vector with the shield
    private void CreateColorScatterMapShield()
    {
        isWorking = true;
        while(isWorking)
        {
            myEvent.WaitOne();
            if(color==null)
                return;
            if(transformationMatrice!=null)
            {
                for (double i = 0; i < 1; i=i+0.02)
                {
                    for (double j = 0; j < 1; j=j+0.02)
                    {
                        for (double k = 0; k < 1; k=k+0.02)
                        {
                            Matrix<double> m = Matrix<double>.Build.DenseOfArray(new double[,]{
                                {Math.Round(i,3)},
                                {Math.Round(j,3)},
                                {Math.Round(k,3)},
                                {1}
                            });
                            Matrix<double> position = transformationMatrice*m;
                            Vector3 positionVector = new Vector3(Convert.ToSingle(position[0,0]),Convert.ToSingle(position[1,0]),Convert.ToSingle(position[2,0]));
                            positionVector = matriceWorldToLocalCube.MultiplyPoint3x4(positionVector); //give the local position in the radiation cube
                            positionVector+= radCubeOffset; //apply the offset to the new vertice position
                            if(clip(positionVector,0,2))
                            {
                                positionVector = mapToCubeUnits(positionVector);
                                int index = side*side*(int)positionVector.x + side*(int)positionVector.y + (int)positionVector.z;
                                color[index] = Color.blue; //set the color to blue, because behind the shield there are not radiations
                                color[index].a = 0;
                            }
                        }
                    }
                }
            }
            if(tableTransformationMatrice!=null)
            {    
                for (double i = 0; i < 1; i=i+0.02)
                {
                    for (double j = 0; j < 1; j=j+0.02)
                    {
                        for (double k = 0; k < 1; k=k+0.02)
                        {
                            Matrix<double> m = Matrix<double>.Build.DenseOfArray(new double[,]{
                                {Math.Round(i,3)},
                                {Math.Round(j,3)},
                                {Math.Round(k,3)},
                                {1}
                            });
                            Matrix<double> position = tableTransformationMatrice*m;
                            Vector3 positionVector = new Vector3(Convert.ToSingle(position[0,0]),Convert.ToSingle(position[1,0]),Convert.ToSingle(position[2,0]));
                            positionVector = matriceWorldToLocalCube.MultiplyPoint3x4(positionVector); //give the local position in the radiatioàn cube
                            positionVector+= radCubeOffset; //apply the offset to the new vertice position
                            if(clip(positionVector,0,2))
                            {
                                positionVector = mapToCubeUnits(positionVector);
                                int index = side*side*(int)positionVector.x + side*(int)positionVector.y + (int)positionVector.z;
                                color[index] = Color.blue; //set the color to blue, because behind the shield there are not radiations
                                color[index].a = 0;
                            }
                        }
                    }
                }
            }
            colorReady = true;
        }
    }

    private bool ColorReady()
    {
        return colorReady;
    }

    //Coroutine to apply the new color
    private IEnumerator ApplyColor()
    {
        radiationCenter = transform.position;
        color = this.GetComponent<texture3DCreator>().GetColors();
        matriceWorldToLocalCube = cubePositionObject.transform.worldToLocalMatrix;
        myEvent.Set();
        yield return wait;
        vol.SetPixels(color);
        vol.Apply();
        this.GetComponent<Renderer>().material.SetTexture("_Volume", vol);
        this.GetComponent<Renderer>().enabled = true;
        colorReady = false;
        this.GetComponent<texture3DCreator>().SetColors(color);
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
        
        //pos[0] = -pos[0] + 2; // -pos[1] flips along y, 2 is the length of side of the cube
        pos[1] = -pos[1] + 2;
        //pos[2] = -pos[2] + 2;
        for (int i = 0; i < 3; i++)
        {
            pos[i] = (float) System.Math.Round( (pos[i] / 2.0f) * 39.0f);
        } 
        return pos;
    }

    //Function to update the color using an approximative method, set to blue the voxel between the max and the min for the 3 axes
    private Color[] UpdateColor(Color[] col)
    {
        Color[] color = col;
        int miniX = Convert.ToInt32(minXUpper);
        int miniY = Convert.ToInt32(minYUpper);
        int miniZ = Convert.ToInt32(minZUpper);
        for (int i = miniX ; i < maxXUpper-2; i++)
        {
            for (int j = miniY; j < maxYUpper-2; j++)
            {
                for (int k = miniZ; k < maxZUpper-2; k++)
                {
                    int index = side*side*i + side*j +k;
                    color[index] = Color.blue;
                    color[index].a = 0;
                }
            }
        }
        miniX = Convert.ToInt32(minXTable);
        miniY = Convert.ToInt32(minYTable);
        miniZ = Convert.ToInt32(minZTable);
        for (int l = miniX ; l < maxXTable-2; l++)
        {
            for (int m = miniY; m < maxYTable-2; m++)
            {
                for (int n = miniZ; n < maxZTable-2; n++)
                {
                    int index = side*side*l + side*m +n;
                    color[index] = Color.blue;
                    color[index].a = 0;
                }
            }
        }
        return color;
    }

    //Function called when a object with the corresponding tag and a collider with the trigger enable is inside the box collider of the cube
    private void OnTriggerStay(Collider other)
    {
        Transform parent = null;
        if(!other.CompareTag("TableShield") & !other.CompareTag("UpperShield"))
            return;
        if(other.CompareTag("TableShield")) //allow to set the correct hierarchy for the cube used for the raycasting
        {
            //cubeTableShield3.transform.SetParent(other.transform.parent);
            //cubeTableShield4.transform.SetParent(other.transform.parent);
            parent = cubeTableShield1.transform.parent;
            cubeTableShield1.transform.SetParent(other.transform);
            cubeTableShield2.transform.SetParent(other.transform);
            cubeTableShield3.transform.SetAsLastSibling();
            cubeTableShield4.transform.SetAsLastSibling();
        }
        GameObject quad = other.gameObject;
        shieldPosition = quad.transform.position - transform.position;
        Matrix4x4 transformMatrice = quad.transform.localToWorldMatrix;
        Matrix4x4 local = transform.worldToLocalMatrix;
        radiationCenter = transform.position;
        Matrix<double> data = null;
        Matrix<double> tableData = null;
        foreach (Transform child in quad.transform)
        {
            Vector3 position = transform.position + (child.position - transform.position)*10; //create a point outside the cube in the direction of the child from the cube center, because we canno't make a raycast with the cube inside the box collider of the cube
            if(Physics.Raycast(position,(transform.position-position),out hits,100,layerMask)) //make the raycast
            {
                //Debug.DrawRay(this.transform.position,(hits.point-transform.position),Color.red);
                Vector3 positionHit = matriceWorldToLocalCube.MultiplyPoint3x4(hits.point);
                positionHit+= radCubeOffset;
                if(clip(positionHit,0,2))
                {
                    positionHit = mapToCubeUnits(positionHit);
                    if(other.CompareTag("UpperShield"))
                    {
                        position = hits.point + (hits.point - transform.position); //create a point a little further in the direction of the hit point to create at the end a volume encompassing the entire region protected by the protections
                        var m = Matrix<double>.Build.DenseOfArray(new double[,]{{position.x},{position.y},{position.z},{1}}); //store the position into the matrix
                        if(data==null)
                        {
                            data = m;
                        }
                        else
                        {
                            data = data.Append(m);
                        }
                        if(minXUpper>positionHit.x)
                            minXUpper=positionHit.x;
                        if(minYUpper>positionHit.y)
                            minYUpper=positionHit.y;
                        if(minZUpper>positionHit.z)
                            minZUpper=positionHit.z;
                        if(maxXUpper<positionHit.x)
                            maxXUpper=positionHit.x;
                        if(maxYUpper<positionHit.y)
                            maxYUpper=positionHit.y;
                        if(maxZUpper<positionHit.z)
                            maxZUpper=positionHit.z;
                    }
                    if(other.CompareTag("TableShield"))
                    {
                        position = hits.point + (hits.point - transform.position); //create a point a little further in the direction of the hit point to create at the end a volume encompassing the entire region protected by the protections
                        var m = Matrix<double>.Build.DenseOfArray(new double[,]{{position.x},{position.y},{position.z},{1}}); //store the position into the matrix
                        if(tableData==null)
                        {
                            tableData = m;
                        }
                        else
                        {
                            tableData = tableData.Append(m);
                        }
                        if(minXTable>positionHit.x)
                            minXTable=positionHit.x;
                        if(minYTable>positionHit.y)
                            minYTable=positionHit.y;
                        if(minZTable>positionHit.z)
                            minZTable=positionHit.z;
                        if(maxXTable<positionHit.x)
                            maxXTable=positionHit.x;
                        if(maxYTable<positionHit.y)
                            maxYTable=positionHit.y;
                        if(maxZTable<positionHit.z)
                            maxZTable=positionHit.z;
                    }
                }
            }
            position = matriceWorldToLocalCube.MultiplyPoint3x4(child.position);
            position+=radCubeOffset;
            if(clip(position,0,2))
            {
                position = mapToCubeUnits(position);
                if(other.CompareTag("UpperShield"))
                {
                    var m = Matrix<double>.Build.DenseOfArray(new double[,]{{child.position.x},{child.position.y},{child.position.z},{1}}); //store the child position into the matrix
                    data = data.Append(m);
                    if(minXUpper>position.x)
                        minXUpper=position.x;
                    if(minYUpper>position.y)
                        minYUpper=position.y;
                    if(minZUpper>position.z)
                        minZUpper=position.z;
                    if(maxXUpper<position.x)
                        maxXUpper=position.x;
                    if(maxYUpper<position.y)
                        maxYUpper=position.y;
                    if(maxZUpper<position.z)
                        maxZUpper=position.z;
                }
                if(other.CompareTag("TableShield"))
                {
                    var m = Matrix<double>.Build.DenseOfArray(new double[,]{{child.position.x},{child.position.y},{child.position.z},{1}}); //store the child position into the matrix
                    tableData = tableData.Append(m);
                    if(minXTable>position.x)
                        minXTable=position.x;
                    if(minYTable>position.y)
                        minYTable=position.y;
                    if(minZTable>position.z)
                        minZTable=position.z;
                    if(maxXTable<position.x)
                        maxXTable=position.x;
                    if(maxYTable<position.y)
                        maxYTable=position.y;
                    if(maxZTable<position.z)
                        maxZTable=position.z;
                }
            }
        }
        if(other.CompareTag("UpperShield"))
        {
            Matrix<double> transpose = matrice.Transpose();
            Matrix<double> temp = matrice; 
            temp = matrice*transpose;
            temp = temp.Inverse();
            transformationMatrice = data * transpose * temp; //compute the transformation matrix for the upper shield
            transformationMatrice[3,0]=0;
        }
        if(other.CompareTag("TableShield"))
        {
            Matrix<double> transpose = matrice.Transpose();
            Matrix<double> temp = matrice; 
            temp = matrice*transpose;
            temp = temp.Inverse();
            tableTransformationMatrice = tableData * transpose * temp; //compute the transformation matrix for the table shield
            tableTransformationMatrice[3,0]=0;
            cubeTableShield1.transform.SetParent(parent);
            cubeTableShield2.transform.SetParent(parent);
        }
        other.tag = "Untagged";
        if(GameObject.FindGameObjectsWithTag("TableShield").Length==0 &GameObject.FindGameObjectsWithTag("UpperShield").Length==0)
        {
            radiationCube.GetComponent<BoxCollider>().enabled = false;
        }
    }

    public void DisableCollider()
    {
        if(alreadyCall)
            return;
        this.GetComponent<BoxCollider>().enabled = false;
        alreadyCall = true;
    }

    private void OnDestroy()
    {
        isWorking = false;
        myEvent?.Set();
    }
}
