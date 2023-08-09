using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Mathematics;
using static Unity.Mathematics.math;

using System.Threading;

public enum State {NodeDragging, TargetDragging, Relaxing, Starting, Static};

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
unsafe public class snake : MonoBehaviour {

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static public extern IntPtr LoadLibrary(string lpFileName);
    [DllImport("kernel32")]
    static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static public extern bool FreeLibrary(IntPtr hModule);
    IntPtr library;
    delegate void cpp_init(bool SHOW_DRAGON = false);
    delegate int cpp_getNumVertices();
    delegate int cpp_getNumTriangles();
    delegate void cpp_reset();
    delegate int cpp_getNumCables();
    delegate void cpp_getNumViasPerCable(
        void *num_vias__INT_ARRAY);
    delegate void cpp_getCables(
        void *cable_positions__FLOAT3_ARRAY,
        void *tensions__FLOAT_ARRAY);
    delegate void cpp_solve(
        int num_feature_points,
        void *_targetEnabled__BOOL_ARRAY,
        void *_targetPositions__FLOAT3_ARRAY, //node pos
        void *vertex_positions__FLOAT3_ARRAY,
        void *vertex_normals__FLOAT3_ARRAY,
        void *triangle_indices__UINT_ARRAY,
        void *feature_point_positions__FLOAT3__ARRAY); // pos on snake
    delegate bool cpp_castRay(
        float ray_origin_x,
        float ray_origin_y,
        float ray_origin_z,
        float ray_direction_x,
        float ray_direction_y,
        float ray_direction_z,
        void *intersection_position__FLOAT_ARRAY__LENGTH_3,
        bool pleaseSetFeaturePoint,
        int indexOfFeaturePointToSet,
        void *feature_point_positions__FLOAT3__ARRAY = null);
    
    cpp_init init;
    cpp_getNumVertices getNumVertices;
    cpp_getNumTriangles getNumTriangles;
    cpp_reset reset;
    cpp_getNumCables getNumCables;
    cpp_getNumViasPerCable getNumViasPerCable;
    cpp_getCables getCables;
    cpp_solve solve;
    cpp_castRay castRay;
    //cpp_test test;
    void LoadDLL() {
        library = LoadLibrary("Assets/snake");
        init               = (cpp_init)               Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_init"),               typeof(cpp_init));
        getNumVertices     = (cpp_getNumVertices)     Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumVertices"),     typeof(cpp_getNumVertices));
        getNumTriangles    = (cpp_getNumTriangles)    Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumTriangles"),    typeof(cpp_getNumTriangles));
        reset              = (cpp_reset)              Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_reset"),              typeof(cpp_reset));
        solve              = (cpp_solve)              Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_solve"),              typeof(cpp_solve));
        castRay            = (cpp_castRay)            Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_castRay"),            typeof(cpp_castRay));
        getNumCables       = (cpp_getNumCables)       Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumCables"),       typeof(cpp_getNumCables));    
        getNumViasPerCable = (cpp_getNumViasPerCable) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumViasPerCable"), typeof(cpp_getNumViasPerCable));
        getCables          = (cpp_getCables)          Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getCables"),          typeof(cpp_getCables));
         //test            = (cpp_test)            Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_test"),            typeof(cpp_test));
    }
    void ASSERT(bool b) {
        if (!b) {
            print("ASSERT");
            int[] foo = {};
            foo[42] = 0;
        }
    }

    public GameObject leftHand;
    public GameObject rightHand;
    bool  leftTriggerHeld = false; 
    bool rightTriggerHeld = false; 
    bool   leftGripHeld   = false; 
    bool  rightGripHeld   = false; 
    bool    yButtonHeld   = false;
    bool    bButtonHeld   = false;
    bool    xButtonHeld   = false;
    bool    aButtonHeld   = false;
    bool  leftStickHeld   = false;
    bool rightStickHeld   = false;
    bool      menuHeld    = false;
    bool  leftTriggerPressed;
    bool rightTriggerPressed;
    bool     leftGripPressed;
    bool    rightGripPressed;
    bool     leftGripReleased;
    bool    rightGripReleased;
    bool      yButtonPressed;
    bool      bButtonPressed;
    bool      xButtonPressed; 
    bool      aButtonPressed;
    bool    leftStickPressed;
    bool   rightStickPressed;
    bool    leftStickReleased;
    bool         menuPressed;
    Vector3  leftRayOrigin;
    Vector3 rightRayOrigin;
    Vector3  leftRayDirection;
    Vector3 rightRayDirection;
    void UpdateInput() {
        {
            float value;
            bool  leftTriggerTemp =  leftTriggerHeld; 
            bool rightTriggerTemp = rightTriggerHeld;
            bool     leftGripTemp =     leftGripHeld; 
            bool    rightGripTemp =    rightGripHeld;

             leftTriggerHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out value) && value >= 0.1f;
            rightTriggerHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out value) && value >= 0.1f;
                leftGripHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip,    out value) && value >= 0.1f;
               rightGripHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip,    out value) && value >= 0.1f;
           
             leftTriggerPressed = ( !leftTriggerTemp &&  leftTriggerHeld); 
            rightTriggerPressed = (!rightTriggerTemp && rightTriggerHeld);
                leftGripPressed = (    !leftGripTemp &&     leftGripHeld); 
               rightGripPressed = (   !rightGripTemp &&    rightGripHeld);
               leftGripReleased = (     leftGripTemp &&    !leftGripHeld); 
              rightGripReleased = (    rightGripTemp &&   !rightGripHeld);
        }
        {
            bool aTemp  =    aButtonHeld;
            bool bTemp  =    bButtonHeld;
            bool xTemp  =    xButtonHeld;
            bool yTemp  =    yButtonHeld;
            bool lsTemp =  leftStickHeld;
            bool rsTemp = rightStickHeld;
            bool mTemp  =       menuHeld;

            bool value;
            aButtonHeld    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton     , out value) && value;
            bButtonHeld    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton   , out value) && value;
            xButtonHeld    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton     , out value) && value;
            yButtonHeld    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton   , out value) && value;
            leftStickHeld  = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out value) && value;
            rightStickHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out value) && value;
            menuHeld       = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton        , out value) && value;

            aButtonPressed    = (! aTemp &&    aButtonHeld);
            bButtonPressed    = (! bTemp &&    bButtonHeld);
            xButtonPressed    = (! xTemp &&    xButtonHeld);
            yButtonPressed    = (! yTemp &&    yButtonHeld);
            leftStickPressed  = (!lsTemp &&  leftStickHeld);
            rightStickPressed = (!rsTemp && rightStickHeld);
            leftStickReleased = ( lsTemp && !leftStickHeld);
            menuPressed       = (! mTemp &&       menuHeld);
        }
        {
            leftRayDirection  =  leftHand.transform.rotation * Vector3.forward;
            rightRayDirection = rightHand.transform.rotation * Vector3.forward;
            {
                leftRayOrigin = new Vector3(); // FORNOW
                bool found = false;
                foreach (Transform child in leftHand.transform) {
                    if (child.name == "[Ray Interactor] Ray Origin") {
                        leftRayOrigin = child.position;
                        found = true;
                        break;
                    }
                }
                ASSERT(found);
            }
            {
                rightRayOrigin = new Vector3(); // FORNOW
                bool found = false;
                foreach (Transform child in rightHand.transform) {
                    if (child.name == "[Ray Interactor] Ray Origin") {
                        rightRayOrigin = child.position;
                        found = true;
                        break;
                    }
                }
                ASSERT(found);
            }
        }
    }



    public GameObject dragon_head;
    public GameObject dragon_body;
    DragonMeshManager DMM;
    const int HEAD = 0;
    const int BODY = 1;



    public GameObject node_1;
    public GameObject head;
    public GameObject interactionDotRight;
    public GameObject interactionDotLeft;
    public GameObject   targetPrefab;
    public GameObject[] targets;
    public GameObject cylinderPrefab; 
    public GameObject   spherePrefab;
    public GameObject[][][] strings;
    public GameObject stringsContainer;
    //int num_cables = 1;
    public GameObject room;
    public GameObject demosContainer;
    public GameObject[] demos;
    public NodeManager nodeManager;
    public Vector3 restPos;
    public UnityEngine.XR.InputFeatureUsage<float> fl;
    public State curState;

    const int SNAKE = 0;
    const int CABLES = 1;
    const int ALL = 2; 
    const int DRAGON = 3;
    const int MEMORIES = 4;
    int curView;
    int curDemo = 0;

    public const float radius = 0.025f;

    // Vector3 node_pos;
    Color targetColor;
 
    NativeArray<float> intersection_position;
    NativeArray<float3> posOnSnake; 

    NativeArray<int> num_vias; 
    NativeArray<float3> cable_positions;
    NativeArray<float> tensions;

    Vector3[] cable_positionsV3;

    //sphere intersection
    int rightSelectedNodeIndex   = -1;
    int  leftSelectedNodeIndex   = -1;
    int rightSelectedTargetIndex = -1;
    int  leftSelectedTargetIndex = -1;

    void Awake () {
        LoadDLL();
        // init();
        init(true); // TODO
        demos = InitDemos();
        intersection_position = new NativeArray<float>(3, Allocator.Persistent);
        posOnSnake = new NativeArray<float3>(nodeManager.numNodes, Allocator.Persistent);

        targets = new GameObject[nodeManager.numNodes];
        //targets[0] = head;
        targetColor = targetPrefab.transform.GetComponent<MeshRenderer>().sharedMaterial.GetColor("_Color");
        curView = SNAKE;
        DMM = new DragonMeshManager(dragon_head, dragon_body);
        DMM.SetUpAll();
        strings = InitCables();
        stringsContainer.SetActive(false);
        //strings[0] = InitCylinderForString(Vector3.zero,Vector3.zero);
        DrawMesh(); 
        curState = State.Starting;
    }

    void Update () {
        UpdateInput();

        //find any sphere interactions  
        bool rightEnteredTarget;
        bool  leftEnteredTarget;
        bool rightLeaveTarget; 
        bool  leftLeaveTarget;
        {
            int rightTargetTemp = rightSelectedTargetIndex; 
            int  leftTargetTemp =  leftSelectedTargetIndex;

            rightSelectedNodeIndex   = SphereCast(rightRayOrigin, rightRayDirection, true);
             leftSelectedNodeIndex   = SphereCast(leftRayOrigin,   leftRayDirection, true);
            rightSelectedTargetIndex = SphereCast(rightRayOrigin, rightRayDirection, false);
             leftSelectedTargetIndex = SphereCast(leftRayOrigin,   leftRayDirection, false);

            rightEnteredTarget = (rightTargetTemp == -1 && rightSelectedTargetIndex != -1);
             leftEnteredTarget = ( leftTargetTemp == -1 &&  leftSelectedTargetIndex != -1);
            rightLeaveTarget   = ResetColor(rightTargetTemp, rightSelectedTargetIndex);
             leftLeaveTarget   = ResetColor( leftTargetTemp,  leftSelectedTargetIndex);
        }

        //mesh gen
        if (curState == State.TargetDragging || curState == State.NodeDragging || curState == State.Relaxing) {DrawMesh(); UpdateCables();}

        DMM.UpdateAll();

        //cast interaction dots
        {
            //left cast
            if (castRay(
                leftRayOrigin.x,leftRayOrigin.y,leftRayOrigin.z,
                leftRayDirection.x,leftRayDirection.y,leftRayDirection.z,
                NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position),
                false, -1, NativeArrayUnsafeUtility.GetUnsafePtr(posOnSnake))) {
                interactionDotLeft.SetActive(true);
                Vector3 dotPos = new Vector3(intersection_position[0], intersection_position[1], intersection_position[2]);
                if(leftSelectedTargetIndex != -1 && curState == State.TargetDragging) {
                    posOnSnake[leftSelectedTargetIndex] = dotPos;
                }
                interactionDotLeft.transform.position = dotPos;
            }
            else{
                interactionDotLeft.SetActive(false);
            }

            //right cast
            if (castRay(
                rightRayOrigin.x,rightRayOrigin.y,rightRayOrigin.z,
                rightRayDirection.x,rightRayDirection.y,rightRayDirection.z,
                NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position),
                false, -1)) {
                interactionDotRight.SetActive(true);
                interactionDotRight.transform.position = new Vector3(intersection_position[0], intersection_position[1], intersection_position[2]);
            }
            else{
                interactionDotRight.SetActive(false);
            }
        }
        //button control
        {
            if(menuPressed) curState = State.Static;
            if(leftTriggerPressed || rightTriggerPressed)
            {
                GenNode(leftTriggerPressed, rightTriggerPressed);
            } //generate new target node
            else if(yButtonPressed || bButtonPressed)
            {
                nodeManager.Setup();
                reset();
                for(int k = 0; k < nodeManager.numNodes; k++){
                    if(targets[k] != null) {
                        Destroy(targets[k]);
                    }
                }
                //curState = State.Relaxing;
                DrawMesh(); 
                UpdateCables();
                curState = State.Starting;         
                //nodeManager.nodes[0].SetActive(true);
            } //reset
            else if(leftSelectedNodeIndex != -1 || rightSelectedNodeIndex != -1 || leftSelectedTargetIndex != -1 || rightSelectedTargetIndex != -1)
            {
                bool nodeOrTarget = (leftSelectedNodeIndex != -1 || rightSelectedNodeIndex != -1);
                bool left = nodeOrTarget ? (leftSelectedNodeIndex != -1) : (leftSelectedTargetIndex != -1);
                //delete
                if(nodeOrTarget && (left ? xButtonPressed : aButtonPressed)) { 
                    foreach(Transform child in nodeManager.nodes[left ? leftSelectedNodeIndex : rightSelectedNodeIndex].transform) {
                        if(child.name == "Lines") {
                            foreach(Transform child2 in child){
                                if(child2.GetComponent<LinePos>().head != null) child2.GetComponent<LinePos>().head.SetActive(false);
                            }
                        }
                    }
                    nodeManager.nodes[left ? leftSelectedNodeIndex : rightSelectedNodeIndex].SetActive(false);
                    curState = State.Relaxing;
                    //if(nodeManager.AnyActive()) ExcuseToUseIEnumeratorAndCoroutinesEvenThoughThereAreDeffinitlyBetterWaysToDoThisAndThisIsntEvenSomthingThatIsVeryNecessaryToDo();
                } //drag 
                else if(left ? leftGripPressed : rightGripPressed) {
                    curState = nodeOrTarget ? State.NodeDragging : State.TargetDragging;
                } //release
                else if(left ? leftGripReleased : rightGripReleased) {
                    curState = nodeOrTarget ? State.Relaxing : State.Static;
                    //if(nodeOrTarget) ExcuseToUseIEnumeratorAndCoroutinesEvenThoughThereAreDeffinitlyBetterWaysToDoThisAndThisIsntEvenSomthingThatIsVeryNecessaryToDo();
                }
                //else curState = nodeOrTarget ? State.Relaxing : State.Static;
            } //delete and drag <- I think there is a mistake here that is keeping it in relaxing mode when should be static
            if(leftStickPressed || Input.GetKeyDown("n"))
            {
                ChangeWhatIsShowing();
            } //change mode
            if(rightStickPressed)
            {
                CycleDemos();
            } //change demo being shown
            if(leftEnteredTarget || rightEnteredTarget)
            {
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(leftEnteredTarget ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand).SendHapticImpulse(0, 0.3f, 0.15f);
            } //haptic feedback
            if(leftSelectedTargetIndex != -1 || rightSelectedTargetIndex != -1)
            { 
                //Color newColor = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * 0.7f);
                targets[leftSelectedTargetIndex != -1 ? leftSelectedTargetIndex : rightSelectedTargetIndex].transform.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.blue);
            } //change color slightly <- come back to 

        }
        //clamp pos
    }

    void DrawMesh(){
        // node_pos = node_1.transform.position; // ???

        int triangleIndexCount = getNumTriangles() * 3; 

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        { // TODO: link to whatever tutorial/docs we got this stuff from
            int vertexCount = getNumVertices();
            int vertexAttributeCount = 2;

            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Temp);
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3); // position?
            vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            vertexAttributes.Dispose();

            meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
        }


        NativeArray<int> nativeBools = new NativeArray<int>(nodeManager.numNodes, Allocator.Temp);
        NativeArray<float3> nativeTargetPos = new NativeArray<float3>(nodeManager.numNodes, Allocator.Temp);
        {
            bool[] bools = nodeManager.getBools();
            for(int k = 0; k < nodeManager.numNodes; k++){
                if(bools[k]) nativeBools[k] = 1;
                else nativeBools[k] = 0;
                nativeTargetPos[k] = new float3 (nodeManager.nodes[k].transform.position.x, nodeManager.nodes[k].transform.position.y, nodeManager.nodes[k].transform.position.z);
            }
        }

        var simulationMeshPositions = (float3 *) NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(0));

        solve(
            nodeManager.numNodes,
            NativeArrayUnsafeUtility.GetUnsafePtr(nativeBools),
            NativeArrayUnsafeUtility.GetUnsafePtr(nativeTargetPos),
            simulationMeshPositions,
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(1)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetIndexData<int>()),
            NativeArrayUnsafeUtility.GetUnsafePtr(posOnSnake));
        
        nativeBools.Dispose();
        nativeTargetPos.Dispose();


        for(int k = 0; k < nodeManager.nextAvalible; k++){
            targets[k].transform.position = posOnSnake[k];
        }


        // FORNOW: TODO: try moving this before scope if we ever start passing triangle indices only once (e.g., in Awake)
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount));

        Mesh mesh = new Mesh {
            name = "Procedural Mesh"
        };
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = mesh;
        bool found = false;
        foreach(Transform demo in demosContainer.transform){
            if(demo.name == "Demo 1" && demo.gameObject.activeSelf) {
                GetComponent<MeshCollider>().sharedMesh = mesh;
                GetComponent<MeshCollider>().enabled = true;
                found = true;
                break;
            } 
        }
        if(!found) GetComponent<MeshCollider>().enabled = false;
    }

    bool ResetColor(int tempIndex, int curIndex){
        if(tempIndex != -1 && curIndex == -1) {
            targets[tempIndex].transform.GetComponent<MeshRenderer>().material.SetColor("_Color", targetColor);
            return true;
        }
        return false;
    }

    GameObject[][][] InitCables() {
        int num_cables = getNumCables();
        num_vias = new NativeArray<int>(num_cables, Allocator.Persistent);
        
        getNumViasPerCable(NativeArrayUnsafeUtility.GetUnsafePtr(num_vias));

        int total_vias = 0;
        GameObject[][][] tempStrings = new GameObject[num_cables][][];
        for(int i = 0; i < num_cables; i++){
            tempStrings[i] = new GameObject[num_vias[i]-1][];
            total_vias += num_vias[i];
        }
        cable_positions = new NativeArray<float3>(total_vias, Allocator.Persistent);
        tensions = new NativeArray<float>(num_cables, Allocator.Persistent);

        getCables(NativeArrayUnsafeUtility.GetUnsafePtr(cable_positions), NativeArrayUnsafeUtility.GetUnsafePtr(tensions));

        cable_positionsV3 = new Vector3[total_vias];
        for(int i = 0; i < cable_positions.Length; i++){
            cable_positionsV3[i] = new Vector3(cable_positions[i].x, cable_positions[i].y, cable_positions[i].z);
        }
        int viaIndex = 0;
        for(int cableIndex = 0; cableIndex < num_vias.Length; cableIndex++){
            for(int substringIndex = 0; substringIndex < num_vias[cableIndex]-1; substringIndex++){
                tempStrings[cableIndex][substringIndex] = InitCylinderForString(cable_positionsV3[viaIndex], cable_positionsV3[viaIndex+1]);
                viaIndex++;
            }
            viaIndex++;
        }
        return tempStrings;
    }

    GameObject[] InitCylinderForString(Vector3 startPoint, Vector3 endPoint){
        GameObject[] twoBallsAndACyliner = new GameObject[3];
        twoBallsAndACyliner[0] = Instantiate<GameObject>(spherePrefab, Vector3.zero, Quaternion.identity, stringsContainer.transform);
        twoBallsAndACyliner[1] = Instantiate<GameObject>(spherePrefab, Vector3.zero, Quaternion.identity, stringsContainer.transform);
        twoBallsAndACyliner[0].transform.position = startPoint;
        twoBallsAndACyliner[1].transform.position =   endPoint;

        twoBallsAndACyliner[2] = Instantiate<GameObject>(cylinderPrefab, Vector3.zero, Quaternion.identity, stringsContainer.transform);
        UpdateCylinderForString(startPoint, endPoint, twoBallsAndACyliner);
        return twoBallsAndACyliner;
    }

    void UpdateCables() {
        getCables(NativeArrayUnsafeUtility.GetUnsafePtr(cable_positions), NativeArrayUnsafeUtility.GetUnsafePtr(tensions));
        for(int i = 0; i < cable_positions.Length; i++){
            cable_positionsV3[i] = new Vector3(cable_positions[i].x, cable_positions[i].y, cable_positions[i].z);
        }
        int viaIndex = 0;
        for(int cableIndex = 0; cableIndex < num_vias.Length; cableIndex++){
            for(int substringIndex = 0; substringIndex < num_vias[cableIndex]-1; substringIndex++){
                UpdateCylinderForString(cable_positionsV3[viaIndex], cable_positionsV3[viaIndex+1], strings[cableIndex][substringIndex]);
                for(int objectIndex = 0; objectIndex < 3; objectIndex++){
                    Material mat = strings[cableIndex][substringIndex][objectIndex].GetComponent<MeshRenderer>().material;
                    //print(tensions[cableIndex]);
                    mat.color = new Color(tensions[cableIndex]*6.0f, tensions[cableIndex]*6.0f, -tensions[cableIndex]*16.0f+1);
                }
                viaIndex++;
            }
            viaIndex++;
        }
    }

    void UpdateCylinderForString(Vector3 startPoint, Vector3 endPoint, GameObject[] cylinderAndCaps){
        cylinderAndCaps[0].transform.position = startPoint;
        cylinderAndCaps[1].transform.position =   endPoint;
        Vector3 offset = endPoint - startPoint;
        Vector3 position = startPoint + (offset/2.0f);
        cylinderAndCaps[2].transform.position = position;
        cylinderAndCaps[2].transform.LookAt(endPoint);
        cylinderAndCaps[2].transform.eulerAngles -= new Vector3(90.0f, 0.0f, 0.0f);
        Vector3 localScale = cylinderAndCaps[2].transform.localScale;
        localScale.y = (endPoint - startPoint).magnitude / 2.0f;
        cylinderAndCaps[2].transform.localScale = localScale;
    }

    void ChangeWhatIsShowing() {
        curView = (curView == MEMORIES ? SNAKE : curView + 1); 
        bool showUI = (curView < DRAGON);
        bool showDragon = (curView != SNAKE && curView != CABLES);
        bool showCables = (curView == CABLES);
        foreach(GameObject target in targets){
            if(target != null) target.GetComponent<MeshRenderer>().enabled = showUI;
        }
        foreach(GameObject node in nodeManager.nodes){
            if(node != null){
                node.GetComponent<MeshRenderer>().enabled = showUI;
                foreach(Transform child in node.transform) {
                    if(child.name == "Lines") {
                        foreach(Transform child2 in child){
                            child2.GetComponent<LineRenderer>().enabled = showUI;
                        }
                    }
                }
            }
        }
        transform.GetComponent<MeshRenderer>().enabled = !showDragon;
        stringsContainer.SetActive(showCables);
        dragon_head.transform.GetComponent<SkinnedMeshRenderer>().enabled = showDragon;
        dragon_body.transform.GetComponent<SkinnedMeshRenderer>().enabled = showDragon;
        room.SetActive(curView == MEMORIES);
    }

    GameObject[] InitDemos() {
        int ind = 0;
        GameObject[] tempDemos = new GameObject[demosContainer.transform.childCount];
        foreach(Transform demo in demosContainer.transform){
            tempDemos[ind] = demo.gameObject;
            ind++;
        }
        return tempDemos;
    }

    void CycleDemos() {
        if(curDemo != 0) demos[curDemo - 1].SetActive(false);
        curDemo = (curDemo == demosContainer.transform.childCount) ? 0 : curDemo + 1;
        if(curDemo != 0) demos[curDemo - 1].SetActive(true);
    } 

    public void GenNode(bool leftHandFire, bool rightHandFire) {
        if(gameObject.activeSelf && (nodeManager.nextAvalible != nodeManager.numNodes)){

            Vector3 ray_origin_r = new Vector3(); Vector3 ray_origin_l = new Vector3();
            ASSERT(rightHand!=null && leftHand!=null); 
            foreach(Transform child in rightHand.transform){if(child.name == "[Ray Interactor] Ray Origin") ray_origin_r = child.position;} 
            foreach(Transform child in leftHand.transform) {if(child.name == "[Ray Interactor] Ray Origin") ray_origin_l = child.position;}
            
            Vector3 ray_direction_r = rightHand.transform.rotation * Vector3.forward;
            Vector3 ray_direction_l = leftHand.transform.rotation  * Vector3.forward;

            if (castRay(ray_origin_r.x, ray_origin_r.y, ray_origin_r.z, ray_direction_r.x, ray_direction_r.y, ray_direction_r.z, NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position), rightHandFire, nodeManager.nextAvalible) && rightHandFire) {
                InstantiateNode();
            }
            if (castRay(ray_origin_l.x, ray_origin_l.y, ray_origin_l.z, ray_direction_l.x, ray_direction_l.y, ray_direction_l.z, NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position),  leftHandFire, nodeManager.nextAvalible) &&  leftHandFire) {
                InstantiateNode();
            }
        }
    }

    void InstantiateNode() {
        Vector3 pos = new Vector3(intersection_position[0], intersection_position[1], intersection_position[2]);                
        GameObject tar = Instantiate(targetPrefab, pos, Quaternion.identity);
        targets[nodeManager.nextAvalible] = tar;
        nodeManager.SetProperties(pos);
        foreach(Transform child in nodeManager.nodes[nodeManager.nextAvalible-1].transform) {
            if(child.name == "Lines") {
                foreach(Transform child2 in child){
                    child2.GetComponent<LinePos>().head = tar;
                }
            }
        }
    }

    int SphereCast(Vector3 origin, Vector3 direction, bool nodeOrTarget) {
        int indexToReturn = -1;
        for (int index = 0; index < (nodeOrTarget ? nodeManager.nodes.Length : (nodeManager.nextAvalible)); ++index) {
            GameObject node = nodeOrTarget ? nodeManager.nodes[index] : targets[index];
            if (!node.activeSelf) { continue; }

            Vector3 center = node.transform.position;
            Vector3 oc = origin - center;
            float a = Vector3.Dot(direction, direction);
            float half_b = Vector3.Dot(oc, direction);
            float c = Vector3.Dot(oc, oc) - radius*radius;
            float discriminant = half_b * half_b  - a*c;
            if(discriminant > 0) {
                if (indexToReturn == -1) indexToReturn = index;
                else if((oc.magnitude < (origin - nodeManager.nodes[indexToReturn].transform.position).magnitude)) indexToReturn = index;
            }
            if(nodeOrTarget) Clamp(node);
        }
        return indexToReturn;
    }

    void Clamp(GameObject node){
        node.transform.GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 new_pos = new Vector3(
            Mathf.Clamp(node.transform.position.x, -2.0f, 2.0f),
            Mathf.Clamp(node.transform.position.y, -2.0f, 2.0f), 
            Mathf.Clamp(node.transform.position.z, -2.0f, 2.0f)
            );

        node.transform.position = new_pos;
    }

    void OnApplicationQuit () {
        { // FORNOW: uber sketchy delay because solve may not have finished writing data allocated by C# and if we quit out and then it writes we crash unity
            int N = 31623;
            int k;
            for (int i = 0; i < N; ++i) for (int j = 0; j < N; ++j) k = i + j;
        }

        FreeLibrary(library);
        posOnSnake.Dispose(); 
        intersection_position.Dispose();
        num_vias.Dispose();
        cable_positions.Dispose();
        tensions.Dispose();
    }
}

unsafe public class DragonMeshManager {

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static public extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32")]
    static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static public extern bool FreeLibrary(IntPtr hModule);

    IntPtr library;

    delegate int cpp_dragon_getNumVertices(int mesh_index);
    delegate int cpp_dragon_getNumTriangles(int mesh_index);
    delegate int cpp_dragon_getNumBones();

    delegate void cpp_dragon_getMesh(
        int mesh_index,
        void* vertex_positions,
        void* vertex_normals,
        void* vertex_colors,
        void* triangle_indices);

    delegate void cpp_dragon_yzoBones(
        void *bones_y,
        void *bones_z,
        void *bones_o);

    delegate void cpp_dragon_initializeBones (
        void *bones_y,
        void *bones_z,
        void *bones_o,
        void *bone_indices,
        void *bone_weights);

    delegate void cpp_dragon_yzoHead (
        void *bones_y,
        void *bones_z,
        void *bones_o);

    cpp_dragon_getNumVertices dragon_getNumVertices;
    cpp_dragon_getNumTriangles dragon_getNumTriangles;
    cpp_dragon_getNumBones dragon_getNumBones;
    cpp_dragon_getMesh dragon_getMesh;
    cpp_dragon_yzoBones dragon_yzoBones;
    cpp_dragon_initializeBones dragon_initializeBones;
    cpp_dragon_yzoHead dragon_yzoHead;

    void LoadDLL() {
        library = LoadLibrary("Assets/snake");
        dragon_getNumVertices = (cpp_dragon_getNumVertices) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getNumVertices"), typeof(cpp_dragon_getNumVertices));
        dragon_getNumTriangles = (cpp_dragon_getNumTriangles) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getNumTriangles"), typeof(cpp_dragon_getNumTriangles));
        dragon_getNumBones = (cpp_dragon_getNumBones) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getNumBones"), typeof(cpp_dragon_getNumBones));
        dragon_getMesh = (cpp_dragon_getMesh) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getMesh"), typeof(cpp_dragon_getMesh));
        dragon_yzoBones = (cpp_dragon_yzoBones) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_yzoBones"), typeof(cpp_dragon_yzoBones));
        dragon_initializeBones = (cpp_dragon_initializeBones) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_initializeBones"), typeof(cpp_dragon_initializeBones));
        dragon_yzoHead = (cpp_dragon_yzoHead) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_yzoHead"), typeof(cpp_dragon_yzoHead));
    }

    private GameObject head;
    private GameObject body;

    const int HEAD = 0;
    const int BODY = 1;

    SkinnedMeshRenderer bodyRend;
    NativeArray<Vector3> bodyBones_y;
    NativeArray<Vector3> bodyBones_z;
    NativeArray<Vector3> bodyBones_o;
    Transform[] bodyBones;

    // Precondition: head and body gameobjects should have the following components:
    // - skinnedmeshrenderer
    // - material that supports vertex colors
    public DragonMeshManager(GameObject h, GameObject b) {
        head = h;
        body = b;
        LoadDLL();
    }

    ~DragonMeshManager() {
        FreeLibrary(library);
    }

    public void SetUpAll() {
        SetUp(HEAD);
        SetUp(BODY);
    }

    public void SetUp(int index) {
        GameObject dragon_object = (index == HEAD) ? head : body;

        int triangleIndexCount = dragon_getNumTriangles(index) * 3; 

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        int vertexCount = dragon_getNumVertices(index);

        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp);
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
        vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Color, dimension:4, stream: 2);
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();

        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);

        dragon_getMesh(
            index,
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(0)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(1)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<Color>(2)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetIndexData<int>()));

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount));

        string mesh_name = (index == HEAD) ? "Dragon Head" : "Dragon Body";
        
        Mesh dragon_mesh = new Mesh {
            name = mesh_name
        };

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, dragon_mesh);
        dragon_mesh.RecalculateBounds();
        dragon_object.GetComponent<SkinnedMeshRenderer>().sharedMesh = dragon_mesh;

        if (index == BODY) {
            int num_bones = dragon_getNumBones();
            int num_vertices = dragon_getNumVertices(BODY);

            bodyBones_y = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_z = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_o = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            NativeArray<Vector4Int> cpp_bone_indices = new NativeArray<Vector4Int>(num_vertices, Allocator.Temp);
            NativeArray<Vector4> cpp_bone_weights = new NativeArray<Vector4>(num_vertices, Allocator.Temp);

            dragon_initializeBones (NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_y),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_z),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_o),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(cpp_bone_indices),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(cpp_bone_weights));

            Matrix4x4[] bindPoses = new Matrix4x4[num_bones];
            bodyRend = dragon_object.GetComponent<SkinnedMeshRenderer>();
            bodyBones = new Transform[num_bones];
            bindPoses = new Matrix4x4[num_bones];
            for (int boneIndex = 0; boneIndex < num_bones; boneIndex++) {


                bodyBones[boneIndex] = new GameObject("Bone " + boneIndex.ToString()).transform;
                bodyBones[boneIndex].parent = dragon_object.transform;
                bodyBones[boneIndex].localPosition = bodyBones_o[boneIndex];
                bodyBones[boneIndex].localRotation = Quaternion.identity; // Quaternion.LookRotation(bodyBones_z[boneIndex], bodyBones_y[boneIndex]);
                bindPoses[boneIndex] = Matrix4x4.identity;// bones[boneIndex].worldToLocalMatrix * transform.localToWorldMatrix;
            }
            bodyRend.sharedMesh.bindposes = bindPoses;
            bodyRend.bones = bodyBones;
            
            // ?? NOTE MUST BE IN DESCENDING ORDER OF WEIGHT VALUE
            BoneWeight[] bone_weights = new BoneWeight[num_vertices];
            for (int i = 0; i < num_vertices; i++) {
                bone_weights[i].boneIndex0 = cpp_bone_indices[i][0];
                bone_weights[i].boneIndex1 = cpp_bone_indices[i][1];
                bone_weights[i].boneIndex2 = cpp_bone_indices[i][2];
                bone_weights[i].boneIndex3 = cpp_bone_indices[i][3];
                bone_weights[i].weight0 = cpp_bone_weights[i][0];
                bone_weights[i].weight1 = cpp_bone_weights[i][1];
                bone_weights[i].weight2 = cpp_bone_weights[i][2];
                bone_weights[i].weight3 = cpp_bone_weights[i][3];
            }

            bodyRend.sharedMesh.boneWeights = bone_weights;

            bodyBones_y.Dispose();
            bodyBones_z.Dispose();
            bodyBones_o.Dispose();
            cpp_bone_indices.Dispose();
            cpp_bone_weights.Dispose();

        }
    }

    public void UpdateAll() {
        Update(HEAD);
        Update(BODY);
    }

    public void Update(int index) {
        if (index == BODY) {
            int num_bones = dragon_getNumBones();

            bodyBones_y = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_z = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_o = new NativeArray<Vector3>(num_bones, Allocator.Temp);

            dragon_yzoBones(NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_y),
                            NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_z),
                            NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_o));
            for (int boneIndex = 0; boneIndex < num_bones; boneIndex++) {
                bodyBones[boneIndex].localPosition = bodyBones_o[boneIndex];
                bodyBones[boneIndex].localRotation = Quaternion.LookRotation(bodyBones_z[boneIndex], bodyBones_y[boneIndex]);

            }

            bodyBones_y.Dispose();
            bodyBones_z.Dispose();
            bodyBones_o.Dispose();

            bodyRend.sharedMesh.RecalculateBounds();

        }

        else if (index == HEAD) {
            NativeArray<float> head_y = new NativeArray<float>(3, Allocator.Temp);
            NativeArray<float> head_z = new NativeArray<float>(3, Allocator.Temp);
            NativeArray<float> head_o = new NativeArray<float>(3, Allocator.Temp);

            dragon_yzoHead(NativeArrayUnsafeUtility.GetUnsafePtr(head_y),
                           NativeArrayUnsafeUtility.GetUnsafePtr(head_z),
                           NativeArrayUnsafeUtility.GetUnsafePtr(head_o));

            head.transform.localPosition = new Vector3(head_o[0], head_o[1], head_o[2]);
            head.transform.localRotation = Quaternion.LookRotation(new Vector3(head_z[0], head_z[1], head_z[2]),
                                                         new Vector3(head_y[0], head_y[1], head_y[2]));

            head.GetComponent<SkinnedMeshRenderer>().sharedMesh.RecalculateBounds();
        }
    }

}
