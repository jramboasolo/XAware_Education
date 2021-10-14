using System;
using UnityEngine.SceneManagement;
using UnityEngine;


// Class to manage the upper and table shields in the scene
// For the rotation with the angles retrieved with the inverse kinematics, we use the function RotateAround to rotate the Upper Shield part around his pivot
// So with that and the hierarchy, we just need the angles to move the Upper shield to its new place
// Here the "shield" corresponds to the left shield in the scene (table and upper) and the "shieldRight" corresponds to the right shield
//Penser à factoriser

public class PoseShield : MonoBehaviour
{
    //Reference of all the upper shield parts in the scene
    private GameObject shield,shield2,shield3,shield4,shield5,shield6,shield7;

    //Reference of all table shield parts in the scene
    private GameObject tableShield1,tableShield2,tableShield3;

    //Reference of all the upper shield right parts in the scene
    private GameObject shieldRight,shield2Right,shield3Right,shield4Right,shield5Right,shield6Right,shield7Right;

    //Reference of all table shield right parts in the scene
    private GameObject tableShield1Right,tableShield2Right,tableShield3Right;

    //Reference of table shield's parent, used to replace the origin like XawareLite
    private GameObject parentTableShield;

    //Reference of table shield right's parent, used to replace the origin like XawareLite
    private GameObject parentTableShieldRight;

    //Reference to the upper shield's parent
    private GameObject parentUpperShield;

    //Reference to the right upper shield's parent
    private GameObject parentUpperShieldRight;
    private Vector3 centerOfMass;

    //Angle retrieved from the rotation matrix
    private Vector3 angle;

    //Reference of all the pivots for inverse kinematics 
    private GameObject pivotTheta1,pivotTheta2,pivotShield2,pivotShield3,pivotShield4,pivotShield5,pivotShield6;

    //Reference of all the pivots for inverse kinematics 
    private GameObject pivotTheta1Right,pivotTheta2Right,pivotShield2Right,pivotShield3Right,pivotShield4Right,pivotShield5Right,pivotShield6Right;

    private Vector3 tablePosition;
    private Vector3 tablePositionRight;

    //Previous angle for theta1 and theta2 (Table-shield), used to calculate the current rotation (because shield parts rotate from the previous angle)
    //So we compute the angle or we have the angle we want to have, so we need to compute the rotation to do to reach this angle from the current rotation
    private int theta1Previous,theta2Previous;

    //Previous angle for theta1 and theta2 (Table-shield right), used to calculate the current rotation (because shield part rotate from the previous angle)
    //So we compute the angle or we have the angle we want to have, so we need to compute the rotation to do to reach to this angle from the current rotation
    private int theta1PreviousRight,theta2PreviousRight;

    //Previous angle for theta1, theta2, beta2 and alpha (Upper-Shield), used to compute the current rotation (because shield parts rotate from the previous angle)
    //Same logical that the table shield
    private float theta1UpperPrevious,theta2UpperPrevious,beta2Previous,alphaPrevious;

    //Previous angle for theta1Right, theta2Right, beta2Right and alphaRight (Upper-Shield Right), used to compute the current rotation (because shield parts rotate from the previous angle)
    //Same logical that the table shield
    private float theta1UpperPreviousRight,theta2UpperPreviousRight,beta2PreviousRight,alphaPreviousRight;

    //Length of the Upper Shield part 2 (Name Arm1 in XawareLite)
    private float r1 = 1.45f;

    //Length of the Upper Shield part 4 (Name Arm2 in XawareLite)
    private float r2 = 1.025f;

    //Transformations matrices OR world to Upper Shield world (ORPositionUS), reference matrixof the upper shield
    private Matrix4x4 ORPositionUS,refMatUS,posUSRotCenter;

    //Distance Beta23, distance between the upper shield part2 and upper shield part 3 (see XawareLite)
    private float distanceBeta23 = Mathf.Sqrt(0.8f*0.8f + 0.015f*0.015f);

    [SerializeField]
    private float translationTable=0;
    [SerializeField]
    private int theta1=0;
    [SerializeField]
    private int theta2 = 0;
    [SerializeField]
    private float translationY = 0;
    [SerializeField]
    private float translationZ = 0;
    [SerializeField]
    private float translationX = 0;
    [SerializeField]
    private float alpha = 0;

    // Start is called before the first frame update
    private void Start()
    {
        DefineAllObject();
        PoseShieldPosition(shield,shield2,shield3,shield4,shield5,shield6,shield7,
                            ref theta1UpperPrevious,ref theta2UpperPrevious,ref beta2Previous,ref alphaPrevious,ref theta1Previous,ref theta2Previous,false);
        if(SceneManager.GetActiveScene().name=="Scenario2Scene")
        {
            MoveShields(false);
        }
        else
        {
            PoseShieldPosition(shieldRight,shield2Right,shield3Right,shield4Right,shield5Right,shield6Right,shield7Right,
                                ref theta1UpperPreviousRight,ref theta2UpperPreviousRight,ref beta2PreviousRight,ref alphaPreviousRight,ref theta1PreviousRight,ref theta2PreviousRight,true);
        }
    }

    //Function called to define all the objects (shield)
    private void DefineAllObject()
    {
        shield = GameObject.Find("upper_shield_part1");
        shield2 = GameObject.Find("upper_shield_part2");
        shield3 = GameObject.Find("upper_shield_part3");
        shield4 = GameObject.Find("upper_shield_part4");
        shield5 = GameObject.Find("upper_shield_part5");
        shield6 = GameObject.Find("upper_shield_part6");
        shield7 = GameObject.Find("upper_shield_part7");

        parentUpperShield = GameObject.Find("upper_shieldScene");

        pivotShield2 = GameObject.Find("PivotShield2");
        pivotShield3 = GameObject.Find("PivotShield3");
        pivotShield4 = GameObject.Find("PivotShield4");
        pivotShield5 = GameObject.Find("PivotShield5");
        pivotShield6 = GameObject.Find("PivotShield6");

        tableShield1 = GameObject.Find("table_shield_part1");
        tableShield2 = GameObject.Find("table_shield_part2");
        tableShield3 = GameObject.Find("table_shield_part3");
        parentTableShield = GameObject.Find("table_shield");

        tablePosition = parentTableShield.transform.localPosition;

        pivotTheta1 = GameObject.Find("PivotTheta1");
        pivotTheta2 = GameObject.Find("PivotTheta2");


        shieldRight = GameObject.Find("upper_shield_part1_right");
        shield2Right = GameObject.Find("upper_shield_part2_right");
        shield3Right = GameObject.Find("upper_shield_part3_right");
        shield4Right = GameObject.Find("upper_shield_part4_right");
        shield5Right = GameObject.Find("upper_shield_part5_right");
        shield6Right = GameObject.Find("upper_shield_part6_right");
        shield7Right = GameObject.Find("upper_shield_part7_right");

        parentUpperShieldRight = GameObject.Find("upper_shieldScene_right");

        pivotShield2Right = GameObject.Find("PivotShield2_right");
        pivotShield3Right = GameObject.Find("PivotShield3_right");
        pivotShield4Right = GameObject.Find("PivotShield4_right");
        pivotShield5Right = GameObject.Find("PivotShield5_right");
        pivotShield6Right = GameObject.Find("PivotShield6_right");

        tableShield1Right = GameObject.Find("table_shield_part1_right");
        tableShield2Right = GameObject.Find("table_shield_part2_right");
        tableShield3Right = GameObject.Find("table_shield_part3_right");

        parentTableShieldRight = GameObject.Find("table_shield_right");
        if(parentTableShieldRight!=null)
            tablePositionRight = parentTableShieldRight.transform.localPosition;

        pivotTheta1Right = GameObject.Find("PivotTheta1_right");
        pivotTheta2Right = GameObject.Find("PivotTheta2_right");

    }
    
    //Compute the intial position for the table and upper shield using the same formula that XAwareLive and put the hierarchy for the upper shield
    //Take as input the GameObject shield,2,3,4,5,6,7 , beta2Previous, alphaPrevious, theta1-2Previous ans theta1-2UpperPrevious (classical if left shield or with Right at the end if right shield ex: shield4Right)
    private void PoseShieldPosition(GameObject _shield,GameObject _shield2,GameObject _shield3,GameObject _shield4,GameObject _shield5,GameObject _shield6,GameObject _shield7,
                                ref float _theta1UpperPrevious,ref float _theta2UpperPrevious,ref float _beta2Previous,ref float _alphaPrevious,ref int _theta1Previous,ref int _theta2Previous,
                                bool isRight)
    {
        centerOfMass = new Vector3(-2.73465f,0.215676f,3.6754f); //Center of mass provided by XAwareLite
        if(isRight)
        {
            ORPositionUS = ComputeTranslation(-2.355f,-1.688f,-0.1f) * ComputeRotation('x',90); //Transformation matrix provided by XAwareLite, go to the center of the scene to the upper shield position
        }
        else
        {
            ORPositionUS = ComputeTranslation(2.355f,-1.688f,-0.1f) * ComputeRotation('x',90); //Transformation matrix provided by XAwareLite, go to the center of the scene to the upper shield position
        }

        Matrix4x4 eigen1 = ComputeTranslation(-centerOfMass[0],centerOfMass[1],centerOfMass[2]);

        Matrix4x4 a1 = ORPositionUS*eigen1.inverse;  // 1 transformation matrix

        eigen1 = eigen1.inverse;

        Vector3 translation = -(a1.GetColumn(3)); //Use -position because of the difference of axes

        angle = MatrixRotationToEulerAngle(a1);

        angle.x = -angle.x; //Use -angle x because of the difference of axes
        
        _shield.transform.localEulerAngles = angle;

        _shield.transform.localPosition = translation;

        Matrix4x4 eigen2;
        if(isRight)
        {
            eigen2 = eigen1 * ComputeTranslation(2.725f,0.216f,3.400f) * ComputeRotation('z',-60) * ComputeTranslation(0,0.014f,0);
        }
        else
        {
            eigen2 = eigen1 * ComputeTranslation(2.725f,0.216f,3.400f) * ComputeRotation('z',60) * ComputeTranslation(0,0.014f,0);
        }

        Matrix4x4 a2 = ORPositionUS * eigen2; // 2 transformation matrix

        angle = MatrixRotationToEulerAngle(a2);

        angle.x = - angle.x; //Use -angle x because of the difference of axes

        translation = -(a2.GetColumn(3)); //Use -position because of the difference of axes

        _shield2.transform.localEulerAngles = angle;

        _shield2.transform.localPosition = translation;

        Matrix4x4 eigen3 = eigen2 * ComputeTranslation(0,1.425f + 0.013f,0) * ComputeRotation('z',180) * ComputeTranslation(0,-0.013f,0);

        Matrix4x4 a3 = ORPositionUS * eigen3; // 3 transformation matrix

        angle = MatrixRotationToEulerAngle(a3);

        angle.y = - angle.y; //Use -angle y because of the difference of axes
        
        translation = -(a3.GetColumn(3)); //Use -position because of the difference of axes

        _shield3.transform.localEulerAngles = angle;

        _shield3.transform.localPosition = translation;

        Matrix4x4 eigen4 = eigen3;  
 
        Matrix4x4 a4 = ORPositionUS * eigen4; // 4 transformation matrix

        _shield4.transform.localEulerAngles = angle;

        translation = -(a4.GetColumn(3)); //Use -position because of the difference of axes

        _shield4.transform.localPosition = translation;

        Matrix4x4 eigen5 = eigen4;

        Matrix4x4 a5 = ORPositionUS * eigen5; // 5 transformation matrix

        translation = -(a5.GetColumn(3)); //Use -position because of the difference of axes

        _shield5.transform.localEulerAngles = angle;

        _shield5.transform.localPosition = translation;

        Matrix4x4 eigen6 = eigen5 * ComputeTranslation(0.377f,1.025f,-1.075f) * ComputeRotation('x',90) * ComputeTranslation(-0.377f,0,0.098f) * ComputeRotation('y',180) * ComputeTranslation(0.377f,0,-0.098f);

        Matrix4x4 a6 = ORPositionUS * eigen6; // 6 transformation matrix

        angle = MatrixRotationToEulerAngle(a6);

        angle.x += 180; //Add 180 to angle x to retrieve the good rotation with unity (maybe pb with the function to retrieve the angle from the rotation matrix or the fact that unity uses the local angle)

        translation = -(a6.GetColumn(3)); //Use -position because of the difference of axes

        _shield6.transform.localEulerAngles = angle;

        _shield6.transform.localPosition = translation;

        Matrix4x4 eigen7 = eigen6;

        Matrix4x4 a7 = ORPositionUS * eigen7; // 7 transformation matrix

        translation = -(a7.GetColumn(3)); //Use -position because of the difference of axes

        _shield7.transform.localEulerAngles = angle;

        _shield7.transform.localPosition = translation;
        

        _theta1Previous = 0;
        _theta2Previous = 0;

        Destroy(_shield.GetComponentInChildren<Rigidbody>());
        Destroy(_shield.GetComponentInChildren<Collider>());

        _theta1UpperPrevious = 0;
        _theta2UpperPrevious = 0;
        _beta2Previous = 0;
        _alphaPrevious = 0;

        _shield2.transform.SetParent(_shield.transform); //Add Upper Shield part 2 as son of the Upper Shield part 1 
        _shield3.transform.SetParent(_shield2.transform); //Add Upper Shield part 3 as son of the Upper Shield part 2
        _shield4.transform.SetParent(_shield3.transform); //Add Upper Shield part 4 as son of the Upper Shield part 3
        _shield5.transform.SetParent(_shield4.transform); //Add Upper Shield part 5 as son of the Upper Shield part 4
        _shield6.transform.SetParent(_shield5.transform); //Add Upper Shield part 6 as son of the Upper Shield part 5
        _shield7.transform.SetParent(_shield6.transform); //Add Upper Shield part 7 as son of the Upper Shield part 6
        //We change the hierarchy to make easier the inverse kinematics

        InitialPosition(isRight);
    }

    //Function called to update the upper shields's position according to the X,Y and Z translation and the alpha rotation of the Upper Shield part 7
    //The order is important: find beta2 with the y translation and after find the others angles
    //Take as inputs the corresponding beta2Previous, alphaPrevious, parentUpperShield,shield4,5,6,7 (classical if left shield or with Right at the end if right shield ex: shield4Right)
    //One thing more, the shield7 needs to be in the same rotation that in the initial position if angle alpha=0, so because of the fact that the shield7 is the son of
    //the shield6, unity ensures that the shield is always in the same position and orientation with respect to its parent (not wish for the shield7's rotation, so need to be corrected)
    private void UpdateUpperShieldPosition(float _translationX,float _translationY,float _translationZ,float _alpha,ref float _alphaPrevious,
                                            ref float _beta2Previous,ref float _theta1UpperPrevious,ref float _theta2UpperPrevious,
                                            GameObject _shield4,GameObject _shield5,GameObject _shield6,GameObject _shield7,GameObject _parentUpperShield,
                                            GameObject _pivotShield4,GameObject _pivotShield5,GameObject _pivotShield6,bool isRight)
    {
        float currentYTranslation = _translationY;
        if(Mathf.Abs(currentYTranslation/distanceBeta23) > 1)
        {
            ComputeRefMatUS(60,180,0,180);
            return;
        }
        float angle = -(Mathf.Asin(currentYTranslation/distanceBeta23) - Mathf.Atan2(15,800));
        r2 = 0.11f + distanceBeta23 * Mathf.Cos(angle + Mathf.Atan2(15,800)); // length of the part 4 recomputed, length of the part 4 after projection on the axis x and z
        angle = (angle*180) / Mathf.PI;
        float currentAngle = angle - _beta2Previous;
        _shield4.transform.RotateAround(_pivotShield4.transform.position,_shield4.transform.right,Convert.ToInt32(currentAngle));
        _shield5.transform.RotateAround(_pivotShield5.transform.position,_shield5.transform.right,-Convert.ToInt32(currentAngle));
        _beta2Previous = angle;
        ComputeRefMatUS(60,180,angle,0);
        _shield7.transform.SetParent(_parentUpperShield.transform); //Reset part to retrieve the local euler angle before the kinematics
        float angleShield7 = _shield7.transform.localEulerAngles.y; //Use local euler angle because the world angle is not good with the hololens
        _shield7.transform.SetParent(_shield6.transform); 
        ComputeTheta(_translationX,_translationZ,ref _theta1UpperPrevious,ref _theta2UpperPrevious,isRight);
        _shield7.transform.SetParent(_parentUpperShield.transform); //Reset part to retrieve the local euler angle after the kinematics
        float rotationCorrection = angleShield7-_shield7.transform.localEulerAngles.y; //Give the rotation has to be corrected to have the rotation of the shield 7 in the good rotation
        _shield7.transform.SetParent(_shield6.transform); 
        _shield6.transform.RotateAround(_pivotShield6.transform.position,_shield6.transform.up, rotationCorrection);
        float currentAlpha = _alpha - _alphaPrevious;
        if(isRight)
        {
            _shield6.transform.RotateAround(_pivotShield6.transform.position,_shield6.transform.up,-currentAlpha);
        }
        else
        {
            _shield6.transform.RotateAround(_pivotShield6.transform.position,_shield6.transform.up,currentAlpha);
        }
        _alphaPrevious = _alpha;
    }

    
    
    //Function called to update the table shields's position  according to the y translation and the theta1 and theta2 rotation
    //Take as input the theta1Previous, theta2Previous,the tableShield2,3 , the pivotTheta1,2 , the parentTableShield (classical if left shield or with Right at the end if right shield ex: tableShield4Right)
    private void UpdateTableShieldPosition(float zTranslation,int _theta1,int _theta2,ref int _theta1Previous, ref int _theta2Previous,Vector3 _tablePosition,
                                        GameObject _tableShield2,GameObject _tableShield3,GameObject _pivotTheta1,GameObject _pivotTheta2,GameObject _parentTableShield,bool isRight)
    {
        Vector3 position = _tablePosition;
        position.z -= zTranslation; // "-" because of the difference of axis
        _parentTableShield.transform.localPosition = position;
        int currentTheta1 = Mathf.Abs(_theta1Previous) - _theta1;
        int currentTheta2 = Mathf.Abs(_theta2Previous) - _theta2;
        if(isRight)
        {
            _tableShield3.transform.RotateAround(_pivotTheta1.transform.position,_tableShield3.transform.up,currentTheta1);
            _tableShield2.transform.RotateAround(_pivotTheta2.transform.position,_tableShield2.transform.up,currentTheta2);
        }
        else
        {
            _tableShield3.transform.RotateAround(_pivotTheta1.transform.position,_tableShield3.transform.up,-currentTheta1);
            _tableShield2.transform.RotateAround(_pivotTheta2.transform.position,_tableShield2.transform.up,-currentTheta2);
        }
        _theta1Previous = _theta1;
        _theta2Previous = _theta2;
        
    }

    //Function to compute the translation matrix
    private Matrix4x4 ComputeTranslation(float x,float y,float z)
    {

        Vector4 row1 = new Vector4(1,0,0,0);
        Vector4 row2 = new Vector4(0,1,0,0);
        Vector4 row3 = new Vector4(0,0,1,0);
        Vector4 row4 = new Vector4(x,y,z,1);

        Matrix4x4 matrix= new Matrix4x4(row1,row2,row3,row4);

        return matrix;
    }

    //Function to compute the rotation matrixfor a certain axis and angle
    private Matrix4x4 ComputeRotation(char axe, float angle)
    {
        float radian = angle * (Mathf.PI/180);

        Matrix4x4 matrix;

        Vector4 col1,col2,col3,col4;

        switch(axe)
        {
            case 'x':
                col1 = new Vector4(1,0,0,0);
                col2 = new Vector4(0,Mathf.Cos(radian),Mathf.Sin(radian),0);
                col3 = new Vector4(0,-Mathf.Sin(radian),Mathf.Cos(radian),0);
                col4 = new Vector4(0,0,0,1);
            break;

            case 'y':
                col1 = new Vector4(Mathf.Cos(radian),0,-Mathf.Sin(radian),0);
                col2 = new Vector4(0,1,0,0);
                col3 = new Vector4(Mathf.Sin(radian),0,Mathf.Cos(radian),0);
                col4 = new Vector4(0,0,0,1);
            break;

            case 'z':
                col1 = new Vector4(Mathf.Cos(radian),Mathf.Sin(radian),0,0);
                col2 = new Vector4(-Mathf.Sin(radian),Mathf.Cos(radian),0,0);
                col3 = new Vector4(0,0,1,0);
                col4 = new Vector4(0,0,0,1);
            break;

            default:
                col1 = new Vector4(0,0,0,0);
                col2 = new Vector4(0,0,0,0);
                col3 = new Vector4(0,0,0,0);
                col4 = new Vector4(0,0,0,0);
            break;
        }

        matrix = new Matrix4x4(col1,col2,col3,col4);

        return matrix;
    }

    //Function to retrieve the euler angle from the rotation matrix
    private Vector3 MatrixRotationToEulerAngle(Matrix4x4 matrix)
    {
        Vector3 angle = new Vector3();

        float thetax = Mathf.Atan2(matrix[2,1],matrix[2,2]);
        float thetay = Mathf.Atan2(-matrix[2,0],Mathf.Sqrt(matrix[2,1]*matrix[2,1] + matrix[2,2] * matrix[2,2]));
        float thetaz = Mathf.Atan2(matrix[1,0],matrix[0,0]);

        angle.x = thetax * 180 / Mathf.PI;
        angle.y = thetay * 180 / Mathf.PI;
        angle.z = thetaz * 180 / Mathf.PI;

        return angle;
    }

    //Compute the refMatUs according to the theta1, theta2, beta2 and alpha3 (see XawareLite)
    private void ComputeRefMatUS(float _theta1,float _theta2,float _beta2, float _alpha3)
    {
        Matrix4x4 T1;
        T1 = ComputeTranslation(-0.010f,0,-0.29f);
        T1 = T1 * ComputeRotation('z',_theta1) * ComputeTranslation(0,1.45f,0);
        T1 = T1 * ComputeRotation('z',_theta2) * ComputeTranslation(0,0.060f,-0.1f);
        T1 = T1 * ComputeRotation('x',_beta2) * ComputeTranslation(0,0.8f,0.015f);
        T1 = T1 * ComputeRotation('x',-_beta2) * ComputeTranslation(0,0.05f,-0.2f);
        refMatUS = T1;
    }

    //Compute the theta angle for the upper shield (inverse kinematics) and makes the rotation
    //All the details for the formula are given by the XawareLite team
    private void ComputeTheta(float x,float y,ref float _theta1UpperPrevious,ref float _theta2UpperPrevious,bool isRight)
    {
        Matrix4x4 ref_mat = ComputeTranslation(x,y,0) * refMatUS; //Transformation matrix upper shield 7 to Upper Shield world
        float X0 = -0.01f;
        float c2 =  ((Mathf.Pow(X0 - ref_mat[0,3],2) + Mathf.Pow(ref_mat[1,3],2) - (Mathf.Pow(r1,2) + Mathf.Pow(r2,2) ))/ (2 * r1 * r2)); //Cosine of the theta2
        float theta2;
        float theta1;


        if(Mathf.Abs(c2) > 1) //Stop the computation if c2>1 or c2<-1
        {
            return;
        }

        float s2 = Mathf.Sqrt(1-c2*c2); // Sine of theta 2
        theta2 = -(180 - (Mathf.Atan2(s2,c2) * 180 )/ Mathf.PI); // Difference with XawareLite: sign "-" 
        theta1 = (Mathf.Atan2(ref_mat[1,3],X0-ref_mat[0,3]) - Mathf.Atan2(r2*s2,r1+r2*c2)) * 180 / Mathf.PI - 30;//Difference with XAwareLite: Atan2(X0-ref_mat[0,3],ref_mat[1,3]) -> Atan2(ref_mat[1,3],X0-ref_mat[0,3]) and -60 -> -30

        float currentTheta1 = (theta1 - _theta1UpperPrevious);
        float currentTheta2 = (theta2 - _theta2UpperPrevious);

        if(isRight)
        {
            shield2Right.transform.RotateAround(pivotShield2Right.transform.position,shield2Right.transform.forward,-currentTheta1);
            shield3Right.transform.RotateAround(pivotShield3Right.transform.position,shield3Right.transform.forward,-currentTheta2);
        }
        else
        {
            shield2.transform.RotateAround(pivotShield2.transform.position,shield2.transform.forward,currentTheta1);
            shield3.transform.RotateAround(pivotShield3.transform.position,shield3.transform.forward,currentTheta2);
        }
        _theta1UpperPrevious = theta1;
        _theta2UpperPrevious = theta2;
    }

    //Function used to put the shields (left and right) in the fixed position
    public void MoveShields(bool isRight)
    {
        if(isRight)
        {
            UpdateUpperShieldPosition(translationX,translationY,translationZ,alpha,
                                ref alphaPreviousRight,ref beta2PreviousRight,ref theta1UpperPreviousRight,ref theta2UpperPreviousRight,
                                shield4Right,shield5Right,shield6Right,shield7Right,parentUpperShieldRight,pivotShield4Right,pivotShield5Right,pivotShield6Right,isRight);
            UpdateTableShieldPosition(translationTable,theta1,theta2,ref theta1PreviousRight,ref theta2PreviousRight,tablePositionRight,
                                tableShield2Right,tableShield3Right,pivotTheta1Right,pivotTheta2Right,parentTableShieldRight,isRight);
        }
        else
        {
            UpdateUpperShieldPosition(translationX,translationY,translationZ,alpha,
                                ref alphaPrevious,ref beta2Previous,ref theta1UpperPrevious,ref theta2UpperPrevious,
                                shield4,shield5,shield6,shield7,parentUpperShield,pivotShield4,pivotShield5,pivotShield6,isRight);
            UpdateTableShieldPosition(translationTable,theta1,theta2,ref theta1Previous,ref theta2Previous,tablePosition,tableShield2,tableShield3,pivotTheta1,pivotTheta2,parentTableShield,isRight);
        }
    }

    //Function used to put the shields (left and right) in the initial position
    public void InitialPosition(bool isRight)
    {
        if(isRight)
        {
            UpdateUpperShieldPosition(0,0,0,0,
                                ref alphaPreviousRight,ref beta2PreviousRight,ref theta1UpperPreviousRight,ref theta2UpperPreviousRight,
                                shield4Right,shield5Right,shield6Right,shield7Right,parentUpperShieldRight,pivotShield4Right,pivotShield5Right,pivotShield6Right,isRight);
            UpdateTableShieldPosition(1,0,0,ref theta1PreviousRight,ref theta2PreviousRight,tablePositionRight,tableShield2Right,tableShield3Right,
                                pivotTheta1Right,pivotTheta2Right,parentTableShieldRight,isRight);
        }
        else
        {
            UpdateUpperShieldPosition(0,0,0,0,
                                ref alphaPrevious,ref beta2Previous,ref theta1UpperPrevious,ref theta2UpperPrevious,
                                shield4,shield5,shield6,shield7,parentUpperShield,pivotShield4,pivotShield5,pivotShield6,isRight);
            UpdateTableShieldPosition(1,0,0,ref theta1Previous,ref theta2Previous,tablePosition,tableShield2,tableShield3,pivotTheta1,pivotTheta2,parentTableShield,isRight);
        }
    }

}
