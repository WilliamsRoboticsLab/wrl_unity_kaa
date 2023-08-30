// NOTE: I don't use any scriptful objects (besides whatever object has this God script)
//       => Deactivating a (scriptless) game object is the same as telling it to not draw itself--it's data is still accessible :)
//          Do this with the GAME_OBJECT_DRAW(...) function

// TODO: Roll your own grab interactable functionality

// // TODO: Widget
// TODO: move feature point inside of target (they should never be created and destroyed)
// NOTE: featurePoint and target
// ???
// TODO: dragon vis
// TODO: remove head

// TODO: just sphere cast against targets

using System;
using System.Collections;
// using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
unsafe public class Main : MonoBehaviour {


    public bool AUTO_TEST_DO_TEST = true;
    bool AUTO_TEST_SINUSOIDAL_TEST = true;
    int _JIM_AUTOMATED_TEST_PHASE__NOTE_NOT_USED_BY_SINUOSOIDAL_TEST;
    float jimAutomatedTestTime = 0.0f;



    void ASSERT(bool b) {
        if (!b) {
            Debug.Log("[ASSERT]");
            Debug.Log(Environment.StackTrace);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }



    void GAME_OBJECT_SET_CHILDS_PARENT(GameObject childGameObject, GameObject parentGameObject) {
        childGameObject.transform.parent = parentGameObject.transform;
    }
    GameObject GAME_OBJECT_CREATE(String gameObjectName, GameObject parentGameObject = null) {
        GameObject result = new GameObject(gameObjectName);
        if (parentGameObject != null) GAME_OBJECT_SET_CHILDS_PARENT(result, parentGameObject);
        return result;
    }
    GameObject PREFAB_INSTANTIATE(GameObject prefab, String gameObjectName = null, GameObject parentGameObject = null) {
        GameObject result = GameObject.Instantiate(prefab);
        ASSERT(result != null);
        result.name = (gameObjectName != null) ? gameObjectName : prefab.name;
        if (parentGameObject != null) GAME_OBJECT_SET_CHILDS_PARENT(result, parentGameObject);
        return result;
    }
    GameObject PREFAB_LOAD(String resourceName) {
        GameObject result = (GameObject) Resources.Load(resourceName);
        ASSERT(result != null);
        return result;
    }
    void GAME_OBJECT_DRAW(GameObject gameObject, bool draw) {
        gameObject.SetActive(draw);
    }
    bool GAME_OBJECT_IS_DRAWING(GameObject gameObject) {
        return gameObject.activeSelf;
    }
    void GAME_OBJECT_SET_POSITION(GameObject gameObject, Vector3 position) {
        gameObject.transform.position = position;
    }

    int MODULO(int x, int N) { return (((x) % (N) + (N)) % (N)); } // works on negative numbers
    Color[] _color_kelly_colors = {
        new Color(255.0f/255,179.0f/255,  0.0f/255,1.0f),
        new Color(128.0f/255, 62.0f/255,117.0f/255,1.0f),
        new Color(255.0f/255,104.0f/255,  0.0f/255,1.0f),
        new Color(166.0f/255,189.0f/255,215.0f/255,1.0f),
        new Color(193.0f/255,  0.0f/255, 32.0f/255,1.0f),
        new Color(206.0f/255,162.0f/255, 98.0f/255,1.0f),
        new Color(129.0f/255,112.0f/255,102.0f/255,1.0f),
        new Color(  0.0f/255,125.0f/255, 52.0f/255,1.0f),
        new Color(246.0f/255,118.0f/255,142.0f/255,1.0f),
        new Color(  0.0f/255, 83.0f/255,138.0f/255,1.0f),
        new Color(255.0f/255,122.0f/255, 92.0f/255,1.0f),
        new Color(83.0f/255,  55.0f/255,122.0f/255,1.0f),
        new Color(255.0f/255,142.0f/255,  0.0f/255,1.0f),
        new Color(179.0f/255, 40.0f/255, 81.0f/255,1.0f),
        new Color(244.0f/255,200.0f/255,  0.0f/255,1.0f),
        new Color(127.0f/255, 24.0f/255, 13.0f/255,1.0f),
        new Color(147.0f/255,170.0f/255,  0.0f/255,1.0f),
        new Color( 89.0f/255, 51.0f/255, 21.0f/255,1.0f),
        new Color(241.0f/255, 58.0f/255, 19.0f/255,1.0f),
        new Color( 35.0f/255, 44.0f/255, 22.0f/255,1.0f) };
    Color ColorKelly(int i) {
        return _color_kelly_colors[MODULO(i, _color_kelly_colors.Length)];
    }



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


    GameObject prefabCableSphere;
    GameObject prefabCableCylinder; 
    GameObject interactionDotLeft;
    GameObject interactionDotRight;

    bool solving = true;
    bool sending2motors = true;
    // bool drawDragon;

    // transform.GetComponent<MeshRenderer>().enabled = !showDragon;
    // dragon_head.transform.GetComponent<SkinnedMeshRenderer>().enabled = showDragon;
    // dragon_body.transform.GetComponent<SkinnedMeshRenderer>().enabled = showDragon;
    // bool drawDragon;

    // transform.GetComponent<MeshRenderer>().enabled = !showDragon;
    // dragon_head.transform.GetComponent<SkinnedMeshRenderer>().enabled = showDragon;
    // dragon_body.transform.GetComponent<SkinnedMeshRenderer>().enabled = showDragon;






    class CastRayResult {
        public bool hit;
        public Vector3 intersectionPosition;
    }
    CastRayResult CastRayWrapper(Vector3 rayOrigin, Vector3 rayDirection, bool pleaseSetFeaturePoint) {
        CastRayResult result = new CastRayResult();
        result.hit = castRay(
                rayOrigin.x,
                rayOrigin.y,
                rayOrigin.z,
                rayDirection.x,
                rayDirection.y,
                rayDirection.z,
                NativeArrayUnsafeUtility.GetUnsafePtr(_castRayIntersectionPosition),
                pleaseSetFeaturePoint,
                widgetNumberOfActiveWidgets);
        if (result.hit) {
            result.intersectionPosition = new Vector3(_castRayIntersectionPosition[0], _castRayIntersectionPosition[1], _castRayIntersectionPosition[2]);
            if (pleaseSetFeaturePoint) {
                WidgetActivate(result.intersectionPosition);
            }
        }
        return result;
    }
    NativeArray<float> _castRayIntersectionPosition;


    GameObject widgetWidgetsParentObject;
    int widgetNumberOfActiveWidgets;
    int widgetMaximumNumberOfActiveWidgets;
    GameObject[] widgetWidgetGameObjects;
    GameObject[] widgetTargetGameObjects;
    Rigidbody[] widgetTargetRigidbodies;
    GameObject[] widgetFeaturePointGameObjects;
    NativeArray<int> _widgetTargetEnabled;
    NativeArray<float3> _widgetTargetPositions;
    NativeArray<float3> _widgetFeaturePointPositions; 
    void WidgetAwake() {
        widgetWidgetsParentObject = GAME_OBJECT_CREATE("widgetWidgetsParentObject");
        widgetNumberOfActiveWidgets = 0;
        widgetMaximumNumberOfActiveWidgets = 16;
        widgetWidgetGameObjects = new GameObject[widgetMaximumNumberOfActiveWidgets];
        for (int i = 0; i < widgetMaximumNumberOfActiveWidgets; ++i) {
            widgetWidgetGameObjects[i] = GAME_OBJECT_CREATE("widget " + i, widgetWidgetsParentObject);
            GAME_OBJECT_DRAW(widgetWidgetGameObjects[i], false);
        }
        widgetTargetGameObjects = new GameObject[widgetMaximumNumberOfActiveWidgets];
        widgetTargetRigidbodies = new Rigidbody[widgetMaximumNumberOfActiveWidgets];
        {
            GameObject prefabTarget = PREFAB_LOAD("prefabTarget");
            for (int i = 0; i < widgetMaximumNumberOfActiveWidgets; ++i) {
                widgetTargetGameObjects[i] = PREFAB_INSTANTIATE(prefabTarget, "target " + i, widgetWidgetGameObjects[i]);
                widgetTargetRigidbodies[i] = widgetTargetGameObjects[i].transform.GetComponent<Rigidbody>();
            }
        }
        widgetFeaturePointGameObjects = new GameObject[widgetMaximumNumberOfActiveWidgets];
        {
            GameObject prefabFeaturePoint  = PREFAB_LOAD("prefabFeaturePoint");
            for (int i = 0; i < widgetMaximumNumberOfActiveWidgets; ++i) {
                widgetFeaturePointGameObjects[i] = PREFAB_INSTANTIATE(prefabFeaturePoint, "featurePoint " + i, widgetWidgetGameObjects[i]);
            }
        }
        _widgetTargetEnabled = new NativeArray<int>(widgetMaximumNumberOfActiveWidgets, Allocator.Persistent);
        _widgetTargetPositions = new NativeArray<float3>(widgetMaximumNumberOfActiveWidgets, Allocator.Persistent);
        _widgetFeaturePointPositions = new NativeArray<float3>(widgetMaximumNumberOfActiveWidgets, Allocator.Persistent);
    }
    void WidgetActivate(Vector3 position) {
        GAME_OBJECT_DRAW(widgetWidgetGameObjects[widgetNumberOfActiveWidgets], true);
        GAME_OBJECT_SET_POSITION(widgetTargetGameObjects[widgetNumberOfActiveWidgets], position);
        GAME_OBJECT_SET_POSITION(widgetFeaturePointGameObjects[widgetNumberOfActiveWidgets], position);
        ++widgetNumberOfActiveWidgets;
    }
    void WidgetDeactivate(int i) {
        // arrdelswap
        ASSERT((0 <= i) && (i < widgetMaximumNumberOfActiveWidgets));
        ASSERT(GAME_OBJECT_IS_DRAWING(widgetWidgetGameObjects[i]));
        GAME_OBJECT_DRAW(widgetWidgetGameObjects[i], false);
        --widgetNumberOfActiveWidgets;
        GameObject tmp = widgetWidgetGameObjects[i];
        widgetWidgetGameObjects[i] = widgetWidgetGameObjects[widgetNumberOfActiveWidgets];
        widgetWidgetGameObjects[widgetNumberOfActiveWidgets] = tmp;
    }


    int specialInputLeftRayHotTargetIndex = -1;
    int specialInputRightRayHotTargetIndex = -1;
    bool specialInputLeftRayEnteredTarget;
    bool specialInputRightRayEnteredTarget;
    bool specialInputLeftRayExitedTarget;
    bool specialInputRightRayExitedTarget; 
    void SpecialInputUpdate() {
        int leftTargetTemp = specialInputLeftRayHotTargetIndex;
        int rightTargetTemp = specialInputRightRayHotTargetIndex; 
        specialInputLeftRayHotTargetIndex = SpecialInputCastRayAtTargets(inputLeftRayOrigin, inputLeftRayDirection);
        specialInputRightRayHotTargetIndex = SpecialInputCastRayAtTargets(inputRightRayOrigin, inputRightRayDirection);
        specialInputLeftRayEnteredTarget  = ( leftTargetTemp == -1 &&  specialInputLeftRayHotTargetIndex != -1);
        specialInputRightRayEnteredTarget = (rightTargetTemp == -1 && specialInputRightRayHotTargetIndex != -1);
        specialInputLeftRayExitedTarget   = ResetColor( leftTargetTemp,  specialInputLeftRayHotTargetIndex);
        specialInputRightRayExitedTarget  = ResetColor(rightTargetTemp, specialInputRightRayHotTargetIndex);
    }
    int SpecialInputCastRayAtTargets(Vector3 origin, Vector3 direction) {
        float radius = 0.025f;
        int result = -1;
        for (int i = 0; i < widgetMaximumNumberOfActiveWidgets; ++i) {
            GameObject target = widgetTargetGameObjects[i];
            if (!target.activeSelf) { continue; }

            Vector3 center = target.transform.position;
            Vector3 oc = origin - center;
            float a = Vector3.Dot(direction, direction);
            float half_b = Vector3.Dot(oc, direction);
            float c = Vector3.Dot(oc, oc) - radius*radius;
            float discriminant = half_b * half_b  - a*c;
            if (discriminant > 0) {
                if (result == -1) result = i;
                else if ((oc.magnitude < (origin - widgetTargetGameObjects[result].transform.position).magnitude)) result = i;
            }
        }
        return result;
    }


    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static public extern IntPtr LoadLibrary(string lpFileName);
    [DllImport("kernel32")]
    static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static public extern bool FreeLibrary(IntPtr hModule);
    IntPtr library;
    delegate void cpp_send2motors();
    delegate void cpp_exit();
    delegate void cpp_init(bool SHOW_DRAGON = false); // TODO
    delegate int cpp_getNumVertices();
    delegate int cpp_getNumTriangles();
    delegate void cpp_reset();
    delegate int cpp_getNumCables();
    delegate void cpp_getNumViasPerCable(void *num_vias__INT_ARRAY);
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

    cpp_send2motors        send2motors;
    cpp_exit               exit;
    cpp_init               init;
    cpp_reset              reset;
    cpp_solve              solve;
    cpp_castRay            castRay;
    cpp_getNumVertices     getNumVertices;
    cpp_getNumTriangles    getNumTriangles;
    cpp_getNumCables       getNumCables;
    cpp_getNumViasPerCable getNumViasPerCable;
    cpp_getCables          getCables;

    void LoadDLL() {
        library = LoadLibrary("Assets/snake");
        send2motors        = (cpp_send2motors)        Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_send2motors"),        typeof(cpp_send2motors));
        exit               = (cpp_exit)               Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_exit"),               typeof(cpp_exit));
        init               = (cpp_init)               Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_init"),               typeof(cpp_init));
        reset              = (cpp_reset)              Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_reset"),              typeof(cpp_reset));
        solve              = (cpp_solve)              Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_solve"),              typeof(cpp_solve));
        castRay            = (cpp_castRay)            Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_castRay"),            typeof(cpp_castRay));
        getNumVertices     = (cpp_getNumVertices)     Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumVertices"),     typeof(cpp_getNumVertices));
        getNumTriangles    = (cpp_getNumTriangles)    Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumTriangles"),    typeof(cpp_getNumTriangles));
        getNumCables       = (cpp_getNumCables)       Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumCables"),       typeof(cpp_getNumCables));    
        getNumViasPerCable = (cpp_getNumViasPerCable) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumViasPerCable"), typeof(cpp_getNumViasPerCable));
        getCables          = (cpp_getCables)          Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getCables"),          typeof(cpp_getCables));
    }




    public GameObject leftHand;
    public GameObject rightHand;


    public GameObject dragon_head;
    public GameObject dragon_body;
    DragonMeshManager dragonMeshManager;
    const int HEAD = 0;
    const int BODY = 1;



    GameObject head;


    GameObject[][][] cables;
    public GameObject cablesParentObject;
    //int num_cables = 1;










    Color targetColor;
    NativeArray<int> num_vias; 
    NativeArray<float3> cable_positions;
    NativeArray<float> tensions;
    Vector3[] cable_positionsV3;



    void Awake () {
        LoadDLL();

        // init(true);
        init(false);

        WidgetAwake();

        { // CastRayAwake
            _castRayIntersectionPosition = new NativeArray<float>(3, Allocator.Persistent);
        }

        { // InteractionDotAwake
            GameObject prefabInteractionDot = PREFAB_LOAD("prefabInteractionDot");
            GameObject interactionDotsParentObject = GAME_OBJECT_CREATE("interactionDotsParentObject");
            interactionDotLeft  = PREFAB_INSTANTIATE(prefabInteractionDot, "interactionDotLeft", interactionDotsParentObject);
            interactionDotRight = PREFAB_INSTANTIATE(prefabInteractionDot, "interactionDotRight", interactionDotsParentObject);
            interactionDotLeft.SetActive(false);
            interactionDotRight.SetActive(false);
        }

        { // CableAwake
            prefabCableSphere   = PREFAB_LOAD("prefabCableSphere");
            prefabCableCylinder = PREFAB_LOAD("prefabCableCylinder");
            cables = CablesInit();
        }

        dragonMeshManager = new DragonMeshManager(dragon_head, dragon_body);
        dragonMeshManager.SetUpAll();

        SolveWrapper(); 

        if (AUTO_TEST_DO_TEST) {
            CastRayWrapper(new Vector3(0.0f, -0.6f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f), true);
            if (!AUTO_TEST_SINUSOIDAL_TEST) {
                widgetTargetGameObjects[0].transform.position = new Vector3(
                        0.2f,
                        widgetTargetGameObjects[0].transform.position.y,
                        widgetTargetGameObjects[0].transform.position.z
                        );
            }
        }
    }

    void Update () {
        if (AUTO_TEST_DO_TEST) {
            jimAutomatedTestTime += 0.01f;
            if (AUTO_TEST_SINUSOIDAL_TEST) {
                widgetTargetGameObjects[0].transform.position = new Vector3(
                        Mathf.Min(1.0f, jimAutomatedTestTime) * 0.3f * Mathf.Cos(jimAutomatedTestTime),
                        -0.4f,
                        Mathf.Min(1.0f, jimAutomatedTestTime) * 0.3f * Mathf.Sin(jimAutomatedTestTime)
                        );
            } else {
                if (jimAutomatedTestTime > 0.66f && _JIM_AUTOMATED_TEST_PHASE__NOTE_NOT_USED_BY_SINUOSOIDAL_TEST == 0) {
                    ++_JIM_AUTOMATED_TEST_PHASE__NOTE_NOT_USED_BY_SINUOSOIDAL_TEST;
                    WidgetDeactivate(0);
                } else if (jimAutomatedTestTime > 1.33f && _JIM_AUTOMATED_TEST_PHASE__NOTE_NOT_USED_BY_SINUOSOIDAL_TEST == 1) {
                    ++_JIM_AUTOMATED_TEST_PHASE__NOTE_NOT_USED_BY_SINUOSOIDAL_TEST;
                    CastRayWrapper(new Vector3(0.0f, -0.6f, -1.0f), new Vector3(0.0f, 0.0f, 1.0f), true);
                    widgetTargetGameObjects[0].transform.position = new Vector3(
                            -0.2f,
                            widgetTargetGameObjects[0].transform.position.y,
                            widgetTargetGameObjects[0].transform.position.z
                            );
                } else if (jimAutomatedTestTime > 2.00f && _JIM_AUTOMATED_TEST_PHASE__NOTE_NOT_USED_BY_SINUOSOIDAL_TEST == 2) {
                    ++_JIM_AUTOMATED_TEST_PHASE__NOTE_NOT_USED_BY_SINUOSOIDAL_TEST;
                    WidgetDeactivate(0);
                }
            }
        }

        InputUpdate();
        SpecialInputUpdate();

        { // interactionDotLeft, interactionDotRight
            {
                CastRayResult castRayResult = CastRayWrapper(inputLeftRayOrigin, inputLeftRayDirection, false);
                interactionDotLeft.SetActive(castRayResult.hit);
                if (castRayResult.hit) { interactionDotLeft.transform.position = castRayResult.intersectionPosition; }
            }

            {
                CastRayResult castRayResult = CastRayWrapper(inputRightRayOrigin, inputRightRayDirection, false);
                interactionDotRight.SetActive(castRayResult.hit);
                if (castRayResult.hit) { interactionDotRight.transform.position = castRayResult.intersectionPosition; }
            }
        }

        { // core UI
            if (inputPressedMenu) {
                solving = !solving;
            } else if (inputPressedLeftTrigger) {
                CastRayWrapper(inputLeftRayOrigin, inputLeftRayDirection, true);
            } else if (inputPressedRightTrigger) {
                CastRayWrapper(inputRightRayOrigin, inputRightRayDirection, true);
            } else {
                if (specialInputLeftRayEnteredTarget ) { UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode. LeftHand).SendHapticImpulse(0, 0.3f, 0.15f); }
                if (specialInputRightRayEnteredTarget) { UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).SendHapticImpulse(0, 0.3f, 0.15f); }
            }
        }

        { // FORNOW: Aggressive clamping
            for (int i = 0; i < widgetMaximumNumberOfActiveWidgets; ++i) {
                widgetTargetRigidbodies[i].velocity = new Vector3(0.0f, 0.0f, 0.0f);
                widgetTargetGameObjects[i].transform.position = new Vector3(
                        Mathf.Clamp(widgetTargetGameObjects[i].transform.position.x, -2.0f, 2.0f),
                        Mathf.Clamp(widgetTargetGameObjects[i].transform.position.y, -2.0f, 2.0f), 
                        Mathf.Clamp(widgetTargetGameObjects[i].transform.position.z, -2.0f, 2.0f)
                        );
            }
        }

        if (solving) {
            SolveWrapper();
            UpdateCables();
        }

        if (sending2motors) {
            send2motors();
        }

        dragonMeshManager.UpdateAll();
    }

    void SolveWrapper() {

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

        for (int k = 0; k < widgetMaximumNumberOfActiveWidgets; k++){
            _widgetTargetEnabled[k] = GAME_OBJECT_IS_DRAWING(widgetWidgetGameObjects[k]) ? 1 : 0;
            _widgetTargetPositions[k] = new float3(
                    widgetTargetGameObjects[k].transform.position.x,
                    widgetTargetGameObjects[k].transform.position.y,
                    widgetTargetGameObjects[k].transform.position.z
                    );
        }

        solve(
                widgetMaximumNumberOfActiveWidgets,
                NativeArrayUnsafeUtility.GetUnsafePtr(_widgetTargetEnabled),
                NativeArrayUnsafeUtility.GetUnsafePtr(_widgetTargetPositions),
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(0)),
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(1)),
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetIndexData<int>()),
                NativeArrayUnsafeUtility.GetUnsafePtr(_widgetFeaturePointPositions)
             );



        for(int k = 0; k < widgetMaximumNumberOfActiveWidgets; k++){
            widgetFeaturePointGameObjects[k].transform.position = _widgetFeaturePointPositions[k];
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
            widgetFeaturePointGameObjects[tempIndex].transform.GetComponent<MeshRenderer>().material.SetColor("_Color", targetColor);
            return true;
        }
        return false;
    }

    GameObject[][][] CablesInit() {
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
        twoBallsAndACyliner[0] = Instantiate<GameObject>(prefabCableSphere, Vector3.zero, Quaternion.identity, cablesParentObject.transform);
        twoBallsAndACyliner[1] = Instantiate<GameObject>(prefabCableSphere, Vector3.zero, Quaternion.identity, cablesParentObject.transform);
        twoBallsAndACyliner[0].transform.position = startPoint;
        twoBallsAndACyliner[1].transform.position =   endPoint;

        twoBallsAndACyliner[2] = Instantiate<GameObject>(prefabCableCylinder, Vector3.zero, Quaternion.identity, cablesParentObject.transform);
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
                UpdateCylinderForString(cable_positionsV3[viaIndex], cable_positionsV3[viaIndex+1], cables[cableIndex][substringIndex]);
                for(int objectIndex = 0; objectIndex < 3; objectIndex++){
                    Material mat = cables[cableIndex][substringIndex][objectIndex].GetComponent<MeshRenderer>().material;
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



    void OnApplicationQuit () {
        exit();

        { // FORNOW: uber sketchy delay because solve may not have finished writing data allocated by C# and if we quit out and then it writes we crash unity
            int N = 40000;
            int k;
            for (int i = 0; i < N; ++i) for (int j = 0; j < N; ++j) k = i + j;
        }

        FreeLibrary(library);

        _castRayIntersectionPosition.Dispose();

        _widgetTargetEnabled.Dispose();
        _widgetTargetPositions.Dispose();
        _widgetFeaturePointPositions.Dispose(); 

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

// TODO


// if (specialInputLeftRayHotTargetIndex != -1 || specialInputRightRayHotTargetIndex != -1) { 
//     widgetFeaturePointGameObjects[specialInputLeftRayHotTargetIndex != -1 ? specialInputLeftRayHotTargetIndex : specialInputRightRayHotTargetIndex].transform.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.blue);
// }

// } else if(inputPressedY || inputPressedB) {
//     WidgetAwake();
//     reset();
//     for(int k = 0; k < widgetMaximumNumberOfActiveWidgets; k++){
//         if (widgetFeaturePointGameObjects[k] != null) {
//             Destroy(widgetFeaturePointGameObjects[k]);
//         }
//     }
//     SolveWrapper(); 
//     UpdateCables();
//     state = STATE_START;         

// foreach (GameObject target in widgetTargetGameObjects) {
//     if (target != null) {
//         target.GetComponent<MeshRenderer>().enabled = showUI;
//         foreach(Transform child in target.transform) {
//             if(child.name == "Lines") {
//                 foreach(Transform child2 in child){
//                     child2.GetComponent<LineRenderer>().enabled = showUI;
//                 }
//             }
//         }
//     }
// }

// foreach (Transform child in widgetTargetGameObjects[widgetNumberOfActiveWidgets].transform) {
//     if (child.name == "Lines") {
//         child.gameObject.SetActive(true);
//         foreach (Transform child2 in child) {
//             child2.GetComponent<LineRendererWrapper>().head = widgetFeaturePointGameObjects[widgetNumberOfActiveWidgets];
//         }
//     }
// }
