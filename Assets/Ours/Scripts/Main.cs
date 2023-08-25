// TODO: get auto-running test case set up
// ???
// TODO: dragon vis
// TODO: remove head

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


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
unsafe public class Main : MonoBehaviour {
    void ASSERT(bool b) { if (!b) { print("ASSERT"); int[] foo = {}; foo[42] = 0; } }

    bool JIM_AUTOMATED_TEST = true;

    bool inputPressedA;
    bool inputPressedB;
    bool inputPressedX; 
    bool inputPressedY;
    bool inputPressedLeftGrip;
    bool inputPressedLeftStick;
    bool inputPressedLeftTrigger;
    bool inputPressedRightGrip;
    bool inputPressedRightStick;
    bool inputPressedRightTrigger;
    bool inputPressedMenu;
    bool inputHeldA;
    bool inputHeldB;
    bool inputHeldX;
    bool inputHeldY;
    bool inputHeldMenu;
    bool inputHeldLeftGrip;
    bool inputHeldLeftStick;
    bool inputHeldLeftTrigger;
    bool inputHeldRightGrip;
    bool inputHeldRightStick;
    bool inputHeldRightTrigger;
    bool inputReleasedLeftGrip;
    bool inputReleasedLeftStick;
    bool inputReleasedRightGrip;
    Vector3 inputLeftRayOrigin;
    Vector3 inputLeftRayDirection;
    Vector3 inputRightRayOrigin;
    Vector3 inputRightRayDirection;
    void InputUpdate() {
        {
            float value;
            bool  leftTriggerTemp =  inputHeldLeftTrigger; 
            bool rightTriggerTemp = inputHeldRightTrigger;
            bool     leftGripTemp =     inputHeldLeftGrip; 
            bool    rightGripTemp =    inputHeldRightGrip;

            inputHeldLeftTrigger = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out value) && value >= 0.1f;
            inputHeldRightTrigger = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out value) && value >= 0.1f;
            inputHeldLeftGrip = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip,    out value) && value >= 0.1f;
            inputHeldRightGrip = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip,    out value) && value >= 0.1f;

            inputPressedLeftTrigger = ( !leftTriggerTemp &&  inputHeldLeftTrigger); 
            inputPressedRightTrigger = (!rightTriggerTemp && inputHeldRightTrigger);
            inputPressedLeftGrip = (    !leftGripTemp &&     inputHeldLeftGrip); 
            inputPressedRightGrip = (   !rightGripTemp &&    inputHeldRightGrip);
            inputReleasedLeftGrip = (     leftGripTemp &&    !inputHeldLeftGrip); 
            inputReleasedRightGrip = (    rightGripTemp &&   !inputHeldRightGrip);
        }
        {
            bool aTemp  = inputHeldA;
            bool bTemp  = inputHeldB;
            bool xTemp  = inputHeldX;
            bool yTemp  = inputHeldY;
            bool lsTemp = inputHeldLeftStick;
            bool rsTemp = inputHeldRightStick;
            bool mTemp  = inputHeldMenu;

            bool value;
            inputHeldA    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton     , out value) && value;
            inputHeldB    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton   , out value) && value;
            inputHeldX    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton     , out value) && value;
            inputHeldY    = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton   , out value) && value;
            inputHeldLeftStick  = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out value) && value;
            inputHeldRightStick = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out value) && value;
            inputHeldMenu       = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton        , out value) && value;

            inputPressedA    = (! aTemp &&    inputHeldA);
            inputPressedB    = (! bTemp &&    inputHeldB);
            inputPressedX    = (! xTemp &&    inputHeldX);
            inputPressedY    = (! yTemp &&    inputHeldY);
            inputPressedLeftStick  = (!lsTemp &&  inputHeldLeftStick);
            inputPressedRightStick = (!rsTemp && inputHeldRightStick);
            inputReleasedLeftStick = ( lsTemp && !inputHeldLeftStick);
            inputPressedMenu       = (! mTemp &&       inputHeldMenu);
        }
        {
            inputLeftRayDirection  =  leftHand.transform.rotation * Vector3.forward;
            inputRightRayDirection = rightHand.transform.rotation * Vector3.forward;
            {
                inputLeftRayOrigin = new Vector3(); // FORNOW
                bool found = false;
                foreach (Transform child in leftHand.transform) {
                    if (child.name == "[Ray Interactor] Ray Origin") {
                        inputLeftRayOrigin = child.position;
                        found = true;
                        break;
                    }
                }
                ASSERT(found);
            }
            {
                inputRightRayOrigin = new Vector3(); // FORNOW
                bool found = false;
                foreach (Transform child in rightHand.transform) {
                    if (child.name == "[Ray Interactor] Ray Origin") {
                        inputRightRayOrigin = child.position;
                        found = true;
                        break;
                    }
                }
                ASSERT(found);
            }
        }
    }


    const int VIEW_SNAKE    = 0;
    const int VIEW_CABLES   = 1;
    const int VIEW_ALL      = 2; 
    const int VIEW_DRAGON   = 3;
    const int VIEW_MEMORIES = 4;
    const int _VIEW_COUNT   = 5;
    int _view;
    void ViewCycle() { ViewSet((_view + 1) % _VIEW_COUNT); }
    void ViewSet(int view) {
        _view = view;
        bool showUI = (_view < VIEW_DRAGON);
        bool showDragon = (_view != VIEW_SNAKE && _view != VIEW_CABLES);
        bool showCables = (_view == VIEW_CABLES);
        foreach (GameObject featurePoint in featurePoints) {
            if (featurePoint != null) featurePoint.GetComponent<MeshRenderer>().enabled = showUI;
        }
        foreach (GameObject target in targetManager.targetGameObjects) {
            if (target != null) {
                target.GetComponent<MeshRenderer>().enabled = showUI;
                foreach(Transform child in target.transform) {
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
        room.SetActive(_view == VIEW_MEMORIES);
    }



    void InstantiateFeaturePoint(Vector3 position) {
        GameObject featurePoint = Instantiate(targetPrefab, position, Quaternion.identity);
        featurePoints[targetManager.targetNumTargets] = featurePoint;
        targetManager.CreateNode(position);
        foreach(Transform child in targetManager.targetGameObjects[targetManager.targetNumTargets-1].transform) {
            if(child.name == "Lines") {
                foreach(Transform child2 in child){
                    child2.GetComponent<LinePos>().head = featurePoint;
                }
            }
        }
    }



    int STATE_START           = 0;
    int STATE_NODE_DRAGGING   = 1;
    int STATE_TARGET_DRAGGING = 2;
    int STATE_RELAX           = 3;
    int STATE_STATIC          = 4;
    int state;




    // TODO: buffers we maybe need?
    NativeArray<float> _nativeIntersectionPosition;



    public TargetManager targetManager;





    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static public extern IntPtr LoadLibrary(string lpFileName);
    [DllImport("kernel32")]
    static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static public extern bool FreeLibrary(IntPtr hModule);
    IntPtr library;
    delegate void cpp_init(bool SHOW_DRAGON = false); // TODO
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
            void *_targetPositions__FLOAT3_ARRAY, //target position
            void *vertex_positions__FLOAT3_ARRAY,
            void *vertex_normals__FLOAT3_ARRAY,
            void *triangle_indices__UINT_ARRAY,
            void *feature_point_positions__FLOAT3__ARRAY); // position on snake
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

    public GameObject leftHand;
    public GameObject rightHand;





    public GameObject dragon_head;
    public GameObject dragon_body;
    DragonMeshManager dragonMeshManager;
    const int HEAD = 0;
    const int BODY = 1;



    public GameObject node_1;
    public GameObject head;
    public GameObject interactionDotRight;
    public GameObject interactionDotLeft;
    public GameObject   targetPrefab;
    public GameObject[] featurePoints;
    public GameObject cylinderPrefab; 
    public GameObject   spherePrefab;
    public GameObject[][][] strings;
    public GameObject stringsContainer;
    //int num_cables = 1;
    public GameObject room;
    public Vector3 restPos;
    public UnityEngine.XR.InputFeatureUsage<float> fl;









    public const float radius = 0.025f;

    // Vector3 node_pos;
    Color targetColor;

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

        // init(true);
        init(false);

        targetManager.Setup();

        _nativeIntersectionPosition = new NativeArray<float>(3, Allocator.Persistent);
        posOnSnake = new NativeArray<float3>(targetManager.targetMaxNumberOfTargets, Allocator.Persistent);



        featurePoints = new GameObject[targetManager.targetMaxNumberOfTargets];
        //featurePoints[0] = head;
        targetColor = targetPrefab.transform.GetComponent<MeshRenderer>().sharedMaterial.GetColor("_Color");


        dragonMeshManager = new DragonMeshManager(dragon_head, dragon_body);
        dragonMeshManager.SetUpAll();
        strings = InitCables();
        stringsContainer.SetActive(false);
        //strings[0] = InitCylinderForString(Vector3.zero,Vector3.zero);
        SolveWrapper(); 


        if (JIM_AUTOMATED_TEST) {
            ViewSet(VIEW_CABLES);

            state = STATE_TARGET_DRAGGING;

            // TODO: CastRayWrapper
            if (castRay(0.0f, -0.6f, -1.0f, 0.0f, 0.0f, 1.0f, NativeArrayUnsafeUtility.GetUnsafePtr(_nativeIntersectionPosition), true, targetManager.targetNumTargets)) {
                InstantiateFeaturePoint(new Vector3(_nativeIntersectionPosition[0], _nativeIntersectionPosition[1], _nativeIntersectionPosition[2]));
            }
        }
    }

    float jimTime = 0.0f;
    void Update () {
        if (JIM_AUTOMATED_TEST) {
            jimTime += 0.0167f;
            targetManager.targetGameObjects[0].transform.position = new Vector3(
                    0.2f * Mathf.Sin(5 * jimTime),
                    targetManager.targetGameObjects[0].transform.position.y,
                    targetManager.targetGameObjects[0].transform.position.z
                    );
        }

        InputUpdate();

        //find any sphere interactions  
        bool rightEnteredTarget;
        bool  leftEnteredTarget;
        bool rightLeaveTarget; 
        bool  leftLeaveTarget;
        {
            int rightTargetTemp = rightSelectedTargetIndex; 
            int  leftTargetTemp =  leftSelectedTargetIndex;

            rightSelectedNodeIndex   = SphereCast(inputRightRayOrigin, inputRightRayDirection, true);
            leftSelectedNodeIndex   = SphereCast(inputLeftRayOrigin,   inputLeftRayDirection, true);
            rightSelectedTargetIndex = SphereCast(inputRightRayOrigin, inputRightRayDirection, false);
            leftSelectedTargetIndex = SphereCast(inputLeftRayOrigin,   inputLeftRayDirection, false);

            rightEnteredTarget = (rightTargetTemp == -1 && rightSelectedTargetIndex != -1);
            leftEnteredTarget = ( leftTargetTemp == -1 &&  leftSelectedTargetIndex != -1);
            rightLeaveTarget  = ResetColor(rightTargetTemp, rightSelectedTargetIndex);
            leftLeaveTarget  = ResetColor( leftTargetTemp,  leftSelectedTargetIndex);
        }

        //mesh gen
        if (state == STATE_TARGET_DRAGGING || state == STATE_NODE_DRAGGING || state == STATE_RELAX) {
            SolveWrapper();
            UpdateCables();
        }

        dragonMeshManager.UpdateAll();

        { //interaction dots
            //left cast
            if (castRay(
                        inputLeftRayOrigin.x,inputLeftRayOrigin.y,inputLeftRayOrigin.z,
                        inputLeftRayDirection.x,inputLeftRayDirection.y,inputLeftRayDirection.z,
                        NativeArrayUnsafeUtility.GetUnsafePtr(_nativeIntersectionPosition),
                        false, -1, NativeArrayUnsafeUtility.GetUnsafePtr(posOnSnake))) {
                interactionDotLeft.SetActive(true);
                Vector3 dotPos = new Vector3(_nativeIntersectionPosition[0], _nativeIntersectionPosition[1], _nativeIntersectionPosition[2]);
                if(leftSelectedTargetIndex != -1 && state == STATE_TARGET_DRAGGING) {
                    posOnSnake[leftSelectedTargetIndex] = dotPos;
                }
                interactionDotLeft.transform.position = dotPos;
            }
            else{
                interactionDotLeft.SetActive(false);
            }

            //right cast
            if (castRay(
                        inputRightRayOrigin.x,inputRightRayOrigin.y,inputRightRayOrigin.z,
                        inputRightRayDirection.x,inputRightRayDirection.y,inputRightRayDirection.z,
                        NativeArrayUnsafeUtility.GetUnsafePtr(_nativeIntersectionPosition),
                        false, -1)) {
                interactionDotRight.SetActive(true);
                interactionDotRight.transform.position = new Vector3(_nativeIntersectionPosition[0], _nativeIntersectionPosition[1], _nativeIntersectionPosition[2]);
            }
            else{
                interactionDotRight.SetActive(false);
            }
        }

        { //button control
            if(inputPressedMenu) state = STATE_STATIC;
            if(inputPressedLeftTrigger || inputPressedRightTrigger) {
                if (inputPressedLeftTrigger) {
                    if (castRay(inputLeftRayOrigin.x, inputLeftRayOrigin.y, inputLeftRayOrigin.z, inputLeftRayDirection.x, inputLeftRayDirection.y, inputLeftRayDirection.z, NativeArrayUnsafeUtility.GetUnsafePtr(_nativeIntersectionPosition),  true, targetManager.targetNumTargets)) {
                        InstantiateFeaturePoint(new Vector3(_nativeIntersectionPosition[0], _nativeIntersectionPosition[1], _nativeIntersectionPosition[2]));
                    }
                }
                if (inputPressedRightTrigger) {
                    if (castRay(inputRightRayOrigin.x, inputRightRayOrigin.y, inputRightRayOrigin.z, inputRightRayDirection.x, inputRightRayDirection.y, inputRightRayDirection.z, NativeArrayUnsafeUtility.GetUnsafePtr(_nativeIntersectionPosition), true, targetManager.targetNumTargets)) {
                        InstantiateFeaturePoint(new Vector3(_nativeIntersectionPosition[0], _nativeIntersectionPosition[1], _nativeIntersectionPosition[2]));
                    }
                }
            } else if(inputPressedY || inputPressedB) {
                targetManager.Setup();
                reset();
                for(int k = 0; k < targetManager.targetMaxNumberOfTargets; k++){
                    if(featurePoints[k] != null) {
                        Destroy(featurePoints[k]);
                    }
                }
                //state = STATE_RELAX;
                SolveWrapper(); 
                UpdateCables();
                state = STATE_START;         
                //targetManager.targetGameObjects[0].SetActive(true);
            } else if (leftSelectedNodeIndex != -1 || rightSelectedNodeIndex != -1 || leftSelectedTargetIndex != -1 || rightSelectedTargetIndex != -1) {
                bool nodeOrTarget = (leftSelectedNodeIndex != -1 || rightSelectedNodeIndex != -1);
                bool left = nodeOrTarget ? (leftSelectedNodeIndex != -1) : (leftSelectedTargetIndex != -1);
                //delete
                if (nodeOrTarget && (left ? inputPressedX : inputPressedA)) { 
                    foreach(Transform child in targetManager.targetGameObjects[left ? leftSelectedNodeIndex : rightSelectedNodeIndex].transform) {
                        if(child.name == "Lines") {
                            foreach(Transform child2 in child){
                                if(child2.GetComponent<LinePos>().head != null) child2.GetComponent<LinePos>().head.SetActive(false);
                            }
                        }
                    }
                    targetManager.targetGameObjects[left ? leftSelectedNodeIndex : rightSelectedNodeIndex].SetActive(false);
                    state = STATE_RELAX;
                } else if (left ? inputPressedLeftGrip : inputPressedRightGrip) {
                    state = nodeOrTarget ? STATE_NODE_DRAGGING : STATE_TARGET_DRAGGING;
                } else if (left ? inputReleasedLeftGrip : inputReleasedRightGrip) {
                    state = nodeOrTarget ? STATE_RELAX : STATE_STATIC;
                }
                //else state = nodeOrTarget ? STATE_RELAX : STATE_STATIC;
            } //delete and drag <- I think there is a mistake here that is keeping it in relaxing mode when should be static
            if(inputPressedLeftStick || Input.GetKeyDown("n")) { ViewCycle(); }
            if(leftEnteredTarget || rightEnteredTarget) {
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(leftEnteredTarget ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand).SendHapticImpulse(0, 0.3f, 0.15f);
            } //haptic feedback
            if(leftSelectedTargetIndex != -1 || rightSelectedTargetIndex != -1)
            { 
                //Color newColor = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * 0.7f);
                featurePoints[leftSelectedTargetIndex != -1 ? leftSelectedTargetIndex : rightSelectedTargetIndex].transform.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.blue);
            } //change color slightly <- come back to 

        }
        //clamp position
    }

    void SolveWrapper(){
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


        NativeArray<int> nativeBools = new NativeArray<int>(targetManager.targetMaxNumberOfTargets, Allocator.Temp);
        NativeArray<float3> nativeTargetPos = new NativeArray<float3>(targetManager.targetMaxNumberOfTargets, Allocator.Temp);
        {
            for(int k = 0; k < targetManager.targetMaxNumberOfTargets; k++){
                if (targetManager.isActive(k)) nativeBools[k] = 1;
                else nativeBools[k] = 0;
                nativeTargetPos[k] = new float3 (targetManager.targetGameObjects[k].transform.position.x, targetManager.targetGameObjects[k].transform.position.y, targetManager.targetGameObjects[k].transform.position.z);
            }
        }

        var simulationMeshPositions = (float3 *) NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(0));

        solve(
                targetManager.targetMaxNumberOfTargets,
                NativeArrayUnsafeUtility.GetUnsafePtr(nativeBools),
                NativeArrayUnsafeUtility.GetUnsafePtr(nativeTargetPos),
                simulationMeshPositions,
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(1)),
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetIndexData<int>()),
                NativeArrayUnsafeUtility.GetUnsafePtr(posOnSnake));

        nativeBools.Dispose();
        nativeTargetPos.Dispose();


        for(int k = 0; k < targetManager.targetNumTargets; k++){
            featurePoints[k].transform.position = posOnSnake[k];
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
        if(!found) GetComponent<MeshCollider>().enabled = false;
    }

    bool ResetColor(int tempIndex, int curIndex){
        if(tempIndex != -1 && curIndex == -1) {
            featurePoints[tempIndex].transform.GetComponent<MeshRenderer>().material.SetColor("_Color", targetColor);
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




    public void GenNode(bool leftHandFire, bool rightHandFire) {
    }


    int SphereCast(Vector3 origin, Vector3 direction, bool nodeOrTarget) {
        int indexToReturn = -1;
        for (int index = 0; index < (nodeOrTarget ? targetManager.targetGameObjects.Length : (targetManager.targetNumTargets)); ++index) {
            GameObject target = nodeOrTarget ? targetManager.targetGameObjects[index] : featurePoints[index];
            if (!target.activeSelf) { continue; }

            Vector3 center = target.transform.position;
            Vector3 oc = origin - center;
            float a = Vector3.Dot(direction, direction);
            float half_b = Vector3.Dot(oc, direction);
            float c = Vector3.Dot(oc, oc) - radius*radius;
            float discriminant = half_b * half_b  - a*c;
            if(discriminant > 0) {
                if (indexToReturn == -1) indexToReturn = index;
                else if((oc.magnitude < (origin - targetManager.targetGameObjects[indexToReturn].transform.position).magnitude)) indexToReturn = index;
            }
            if(nodeOrTarget) Clamp(target);
        }
        return indexToReturn;
    }

    void Clamp(GameObject target){
        target.transform.GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 new_pos = new Vector3(
                Mathf.Clamp(target.transform.position.x, -2.0f, 2.0f),
                Mathf.Clamp(target.transform.position.y, -2.0f, 2.0f), 
                Mathf.Clamp(target.transform.position.z, -2.0f, 2.0f)
                );

        target.transform.position = new_pos;
    }

    void OnApplicationQuit () {
        { // FORNOW: uber sketchy delay because solve may not have finished writing data allocated by C# and if we quit out and then it writes we crash unity
            int N = 31623;
            int k;
            for (int i = 0; i < N; ++i) for (int j = 0; j < N; ++j) k = i + j;
        }

        FreeLibrary(library);
        posOnSnake.Dispose(); 
        _nativeIntersectionPosition.Dispose();
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
                // TODO: assert bone_weights in descending order
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
