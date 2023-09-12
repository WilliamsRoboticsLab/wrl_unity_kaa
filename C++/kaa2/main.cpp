// TODO: unity_config.txt (circle test)
// TODO: dump u as well


// TODO: fit young's modulus and poissons
// TODO: try 12 vs 9


const int  MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_UPPER_SEGMENT = 3;
const int  MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_LOWER_SEGMENT = 2;
const bool  INCLUDE_DUMMY_SEGMENT                             = false; // FORNOW: bottom segment always 1 stack
const bool  INCLUDE_PYRAMID_CAP                               = true;  // Just one additional node

int IK_MAX_LINE_SEARCH_STEPS = 8;

#include "include.cpp"

bool KAA_CPP_INIT_FLAG = true;
bool USE_FRANCESCO_STL_INSTEAD = true;
bool _DRAGON_SHOW;
bool _DRAGON_DRIVING__SET_IN_CPP_INIT;

struct IntersectionResult {
    bool hit;
    vec3 p;
    int3 tri;
    vec3 w;
    real t; // _NOT_ set by GPU version of picking
};
IntersectionResult ray_triangle_intersection(vec3 ray_origin, vec3 ray_direction, Tri tri, const SDVector &x) {
    vec3 a = get(x, tri[0]);
    vec3 b = get(x, tri[1]);
    vec3 c = get(x, tri[2]);
    //                                          p = p          
    // alpha * a + beta * b + gamma * c           = ray_origin + t * ray_direction
    // alpha * a + beta * b + gamma * c - t * ray_direction = ray_origin          
    //                            [ a b c -ray_direction] w = ray_origin          
    //                                        F w = ray_origin          
    vec4 w__t = inverse(M4(a.x, b.x, c.x, -ray_direction.x, a.y, b.y, c.y, -ray_direction.y, a.z, b.z, c.z, -ray_direction.z, 1.0, 1.0, 1.0, 0.0)) * V4(ray_origin.x, ray_origin.y, ray_origin.z, 1);
    bool hit; {
        hit = true;
        for (int k = 0; k < 4; ++k) hit &= w__t[k] > -TINY_VAL;
    }
    IntersectionResult result = {};
    result.tri = tri; // ! (have to copy this over so it makes sense with the ray_mesh_intersection API)
    result.hit = hit;
    if (result.hit) {
        for_(d, 3) result.w[d] = w__t[d];
        result.t = w__t[3];
        result.p = ray_origin + result.t * ray_direction;
    }
    return result;
}
IntersectionResult ray_mesh_intersection(vec3 ray_origin, vec3 ray_direction, const SDVector &x, int num_triangles, int3 *triangle_indices) {
    IntersectionResult result = {}; {
        real min_t = INFINITY;
        for_(triangle_i, num_triangles) {
            Tri tri = triangle_indices[triangle_i];
            IntersectionResult singleRayResult = ray_triangle_intersection(ray_origin, ray_direction, tri, x);
            if (singleRayResult.hit && (singleRayResult.t < min_t)) {
                min_t = singleRayResult.t;
                result = singleRayResult;
            }
        }
    }
    return result;
}
#include "fbo.cpp"


void dll_agnostic_path(char *dst, int COUNT_OF_dst, char *filename) {
    GetCurrentDirectory(COUNT_OF_dst, dst);
    strcat(dst, "\\");
    strcat(dst, filename);
}

FILE *dll_agnostic_fopen(char *filename, char *mode) {
    char path[256];
    dll_agnostic_path(path, _COUNT_OF(path), filename);
    FILE *result = fopen(path, mode);
    ASSERT(result);
    return result;
}


IndexedTriangleMesh3D dragon; // FORNOW
real dragonAnimationTime;

boolean MESH_9_12_TOGGLE = false;
int _9_12() { return (!MESH_9_12_TOGGLE) ? 9 : 12; }
int _10_13() { return _9_12() + 1; }

const real ROBOT_SEGMENT_LENGTH = 0.1450;
const real ROBOT_SEGMENT_RADIUS = 0.06 / 2;
const int  ROBOT_NUMBER_OF_UPPER_SEGMENTS = 1;
const int  ROBOT_NUMBER_OF_LOWER_SEGMENTS = 4;
const int  ROBOT_NUMBER_OF_SEGMENTS = ROBOT_NUMBER_OF_UPPER_SEGMENTS + ROBOT_NUMBER_OF_LOWER_SEGMENTS + (INCLUDE_DUMMY_SEGMENT ? 1 : 0);
const real ROBOT_LENGTH = ROBOT_NUMBER_OF_SEGMENTS * ROBOT_SEGMENT_LENGTH;
const real MESH_UPPER_STACK_LENGTH = ROBOT_SEGMENT_LENGTH / MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_UPPER_SEGMENT;
const real MESH_LOWER_STACK_LENGTH = ROBOT_SEGMENT_LENGTH / MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_LOWER_SEGMENT;
const int  MESH_NUMBER_OF_ANGULAR_SECTIONS = (!MESH_9_12_TOGGLE) ? 9 : 12;
const int  MESH_NUMBER_OF_NODES_PER_NODE_LAYER = 1 + MESH_NUMBER_OF_ANGULAR_SECTIONS;
const int  _MESH_NUMBER_OF_UPPER_NODE_LAYERS_EXCLUSIVE = ROBOT_NUMBER_OF_UPPER_SEGMENTS * MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_UPPER_SEGMENT;
const int  _MESH_NUMBER_OF_LOWER_NODE_LAYERS_EXCLUSIVE = ROBOT_NUMBER_OF_LOWER_SEGMENTS * MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_LOWER_SEGMENT;
const int  MESH_NUMBER_OF_NODE_LAYERS = 1 + _MESH_NUMBER_OF_UPPER_NODE_LAYERS_EXCLUSIVE + _MESH_NUMBER_OF_LOWER_NODE_LAYERS_EXCLUSIVE + (INCLUDE_DUMMY_SEGMENT ? 1 : 0);
const int  NUM_BONES = MESH_NUMBER_OF_NODE_LAYERS - 1;
real FRANCESCO_CLOCK = RAD(30);

////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////

Sim sim;
SDVector u_MAX;
State currentState;
FixedSizeSelfDestructingArray<mat4> currentBones;
#define MAX_NUM_FEATURE_POINTS 16
Via featurePoints[MAX_NUM_FEATURE_POINTS];

////////////////////////////////////////////////////////////////////////////////

#define DLL_EXPORT extern "C" __declspec(dllexport)
#define delegate DLL_EXPORT
typedef float    UnityVertexAttributeFloat;
typedef int      UnityTriangleIndexInt;
typedef int      UnityGeneralPurposeInt;

////////////////////////////////////////////////////////////////////////////////

#define LEN_U sim.num_cables
#define LEN_X (SOFT_ROBOT_DIM * sim.num_nodes)
#define LEN_S (3 * dragon.num_vertices)

delegate UnityGeneralPurposeInt cpp_getNumVertices() { return sim.num_nodes; }
delegate UnityGeneralPurposeInt cpp_getNumTriangles() { return sim.num_triangles; }
bool initialized;

typedef FixedSizeSelfDestructingArray<mat4> Bones;
const int DRAGON_BODY_NUM_BONES = MESH_NUMBER_OF_NODE_LAYERS - 1;
const int DRAGON_HEAD_NUM_BONES = 1;
const int DRAGON_NUM_BONES = DRAGON_BODY_NUM_BONES + DRAGON_HEAD_NUM_BONES;

vec3 bodyBoneOriginsRest[DRAGON_BODY_NUM_BONES + 1]; // ? okay FORNOW
FixedSizeSelfDestructingArray<vec3> getBodyBoneOrigins(SDVector &x) {
    FixedSizeSelfDestructingArray<vec3> result(DRAGON_BODY_NUM_BONES + 1);
    for_(j, result.N) {
        if (j != result.N - 1) {
            result[j] = get(x, _9_12() + j * (_10_13()));
        } else {
            result[j] = get(x, sim.num_nodes - 1);
        }
    }
    return result;
}
Bones getBones(SDVector &x) {
    Bones result(DRAGON_NUM_BONES);
    FixedSizeSelfDestructingArray<vec3> bodyBoneOrigins = getBodyBoneOrigins(x);
    { // body
        vec3 bodyBoneNegativeYAxis[DRAGON_BODY_NUM_BONES];
        vec3 bodyBonePositiveXAxis[DRAGON_BODY_NUM_BONES];
        {
            vec3 bodyBoneXAxisFeaturePoint[DRAGON_BODY_NUM_BONES + 1]; {
                for_(j, bodyBoneOrigins.N) {
                    bodyBoneOrigins              [j] = get(x, 9 + j * 10);
                    bodyBoneXAxisFeaturePoint    [j] = get(x, 0 + j * 10);
                }
            }
            {
                for_(j, _COUNT_OF(bodyBoneNegativeYAxis)) {
                    bodyBoneNegativeYAxis[j] = normalized(bodyBoneOrigins[j + 1] - bodyBoneOrigins[j]);
                    bodyBonePositiveXAxis[j] = normalized(bodyBoneXAxisFeaturePoint[j] - bodyBoneOrigins[j]);
                }
            }
        }
        {
            for_(bone_i, DRAGON_BODY_NUM_BONES) {
                vec3 y_hat = -bodyBoneNegativeYAxis[bone_i];
                vec3 x_hat = bodyBonePositiveXAxis[bone_i];
                vec3 z_hat = cross(x_hat, y_hat);
                mat4 invBind = M4_Translation(-bodyBoneOriginsRest[bone_i]);
                mat4 Bone = M4_xyzo(x_hat, y_hat, z_hat, bodyBoneOrigins[bone_i]);
                result[bone_i] = Bone * invBind;
            }
        }
    }
    { // head
        // FORNOW: hacky, with few dependencies

        vec3 y_hat = -normalized(bodyBoneOrigins[DRAGON_BODY_NUM_BONES] - bodyBoneOrigins[DRAGON_BODY_NUM_BONES - 1]);
        vec3 up = { 0.0, 1.0, 0.0 };
        vec3 x_hat = cross(y_hat, up);
        x_hat = IS_ZERO(squaredNorm(x_hat)) ? V3(1.0, 0.0, 0.0) : normalized(x_hat);
        vec3 z_hat = cross(x_hat, y_hat);
        mat4 M = M4_xyzo(x_hat, y_hat, z_hat, bodyBoneOrigins[DRAGON_BODY_NUM_BONES]);
        result[DRAGON_BODY_NUM_BONES] = M
            * M4_RotationAboutYAxis(0.2 * sin(4.0 * dragonAnimationTime))
            * M4_RotationAboutXAxis(0.2 * sin(2.0 * dragonAnimationTime));

    }
    return result;
}

vec3 skinnedGet(IndexedTriangleMesh3D *mesh, const Bones &bones, Via via) {
    vec3 result;
    mat4 *tmp = mesh->bones; {
        mesh->bones = bones.data;
        int3 tri = { via.data[0].index, via.data[1].index, via.data[2].index, };
        vec3 w = { via.data[0].weight, via.data[1].weight, via.data[2].weight, };
        result = mesh->_skin(tri, w); 
    } mesh->bones = tmp;
    return result;
}

vec3 skinnedGet(IndexedTriangleMesh3D *mesh, const Bones &bones, int i) {
    vec3 result;
    mat4 *tmp = mesh->bones; {
        mesh->bones = bones.data;
        result = mesh->_skin(i); 
    } mesh->bones = tmp;
    return result;
}


#ifdef JIM_DLL
int _ZZZ(int d) {
    // unity is left-handed
    return (d == 2) ? -1 : 1;
}
#else
int _ZZZ(int) { return 1; }
#endif




bool _csv_initialized;
#define _CSV_MAX_REALS 10000000
long _csv_start_time;
real _csv_buffer[_CSV_MAX_REALS];
int _csv_index;
void csv_solve(State state) {
    SDVector &x = state.x;
    if (!_csv_initialized) {
        _csv_initialized = true;
        _csv_start_time = util_timestamp_in_milliseconds();
    }
    FixedSizeSelfDestructingArray<vec3> boneOrigins = getBodyBoneOrigins(x); // FORNOW
    _csv_buffer[_csv_index++] = (util_timestamp_in_milliseconds() - _csv_start_time) / 1000.0;
    for_(i, boneOrigins.N) {
        bool isMarkerRigidBodyLayer = false; {
            int cumm = 0;
            isMarkerRigidBodyLayer |= (i == cumm);
            for____(ROBOT_NUMBER_OF_UPPER_SEGMENTS) {
                cumm += MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_UPPER_SEGMENT;
                isMarkerRigidBodyLayer |= (i == cumm);
            }
            for____(ROBOT_NUMBER_OF_LOWER_SEGMENTS) {
                cumm += MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_LOWER_SEGMENT;
                isMarkerRigidBodyLayer |= (i == cumm);
            }
            isMarkerRigidBodyLayer |= (i == boneOrigins.N - 1);
        }
        if (!isMarkerRigidBodyLayer) continue;
        for_(d, 3) if (_csv_index < _CSV_MAX_REALS) _csv_buffer[_csv_index++] = boneOrigins[i][d];
    }

    SDVector &u = state.u;
    for_(j, LEN_U) {
        _csv_buffer[_csv_index++] = u[j];
    }
}
void csv_exit() {
    FILE *file = dll_agnostic_fopen("spine.csv", "w");
    int CSV_NUMBER_OF_RIGID_BODIES = (ROBOT_NUMBER_OF_SEGMENTS + 1) + (INCLUDE_PYRAMID_CAP ? 1 : 0);
    int CSV_NUM_COLUMNS = 1 + 3 * CSV_NUMBER_OF_RIGID_BODIES + LEN_U;
    {
        fprintf(file, "time (seconds),");
        for_(i, CSV_NUMBER_OF_RIGID_BODIES) {
            fprintf(file, "x_%d,", i);
            fprintf(file, "y_%d,", i);
            fprintf(file, "z_%d,", i);
        }
        for_(j, LEN_U) {
            fprintf(file, "u_%d,", j);
        }
        fprintf(file, "\n");
    }
    int CSV_NUM_ROWS = _csv_index / CSV_NUM_COLUMNS;
    {
        int k = 0;
        for_(r, CSV_NUM_ROWS) {
            for_(c, CSV_NUM_COLUMNS) {
                fprintf(file, "%lf,", _csv_buffer[k++]);
            }
            fprintf(file, "\n");
        }
    }
    fclose(file);
}


#define BALLOON_MAXIMUM_NUMBER_OF_BALLOONS 16
int balloon_number_of_balloons;
vec3 balloon_positions[BALLOON_MAXIMUM_NUMBER_OF_BALLOONS];
real balloon_radius = 0.1;
void balloon_init() {
    FILE *file = dll_agnostic_fopen("balloon_config.txt", "r");
    char buffer[4096];
    while (fgets(buffer, _COUNT_OF(buffer), file) != NULL) {
        ASSERT(balloon_number_of_balloons < BALLOON_MAXIMUM_NUMBER_OF_BALLOONS);
        sscanf(buffer, "%lf %lf %lf", &balloon_positions[balloon_number_of_balloons].x, &balloon_positions[balloon_number_of_balloons].y, &balloon_positions[balloon_number_of_balloons].z);
        ++balloon_number_of_balloons;
    }
    fclose(file);
}
delegate int cpp_getNumberOfBalloons() {
    return balloon_number_of_balloons;
}
delegate void cpp_getBalloonPositions(void *balloons__FLOAT3_ARRAY) {
    for_(i, balloon_number_of_balloons) {
        for_(d, 3) {
            ((UnityVertexAttributeFloat*) balloons__FLOAT3_ARRAY)[3 * i + d] = (UnityVertexAttributeFloat)(_ZZZ(d) * balloon_positions[i][d]);
        }
    }
}


struct {
    int AUTO_TEST;
    float CircleY;
    float CircleRadius;
} unity_config;

void cpp_unity_config_load() {
    FILE *file = dll_agnostic_fopen("unity_config.txt", "r");

    int lineNumber = 0;
    char line[4096];
    while (fgets(line, _COUNT_OF(line), file) != NULL) {
        real tmp;
        if (lineNumber == 0) {
            sscanf(line, "%d", &unity_config.AUTO_TEST);
        } else if (lineNumber == 1) {
            sscanf(line, "%lf", &tmp);
            unity_config.CircleY = float(tmp);
        } else if (lineNumber == 2) {
            sscanf(line, "%lf", &tmp);
            unity_config.CircleRadius = float(tmp);
        }
        ++lineNumber;
    }
    fclose(file);
}
delegate int cpp_getAUTO_TEST() {
    return unity_config.AUTO_TEST;
}
delegate float cpp_getCircleY() {
    return unity_config.CircleY;

}
delegate float cpp_getCircleRadius() {
    return unity_config.CircleRadius;
}

bool CPP_HACK_RUNNING_ON_UNITY__NOTE_FOR_GPU_STUFF;

// resets the CPP stuff (featurePoints, simultaion state)
// the caller is responsible for resetting targetPositions and targetEnabled
delegate void cpp_reset() {
    memset(featurePoints, 0, sizeof(featurePoints));
    // featurePoints[0] = { { sim.num_nodes - 1, 1.0 } };
    currentState.x = sim.x_rest;
    currentState.u.setZero();
    currentBones = getBones(currentState.x);
}
delegate void cpp_init(bool _DRAGON_DRIVING = false) {
    cpp_unity_config_load();
    {
        char path[256];
        GetCurrentDirectory(_COUNT_OF(path), path);
        strcat(path, "\\motor_config.txt");
        kaa_dxl_init_FORNOW_ASSUMES_9_MOTORS(path);
    }

    balloon_init();


    // FORNOW
    _DRAGON_DRIVING__SET_IN_CPP_INIT = _DRAGON_DRIVING;
    if (_DRAGON_DRIVING) {
        _DRAGON_SHOW = true;
        if (!COW0._cow_initialized) {
            CPP_HACK_RUNNING_ON_UNITY__NOTE_FOR_GPU_STUFF = true;
            _cow_init();
            _cow_reset();
        }
    }

    ASSERT(!initialized);
    initialized = true;

    sim = {}; {
        // slice    
        //     2    
        // 3       1
        //     n    
        // 5       0
        //    ...   


        StretchyBuffer<vec3> X = {}; {
            real length = 0.0;
            for_(i, MESH_NUMBER_OF_NODE_LAYERS) {
                real angle = FRANCESCO_CLOCK;
                for_(a, MESH_NUMBER_OF_ANGULAR_SECTIONS) {
                    // real angularWidth = RAD(((a % 3) == 2) ? 60 : 30);
                    sbuff_push_back(&X, V3(ROBOT_SEGMENT_RADIUS * cos(angle), -length, -ROBOT_SEGMENT_RADIUS * sin(angle)));
                    angle += (!MESH_9_12_TOGGLE) ? (RAD(((a % 3) == 2) ? 60 : 30)) : RAD(30);
                }
                sbuff_push_back(&X, V3(0.0, -length, 0.0));
                if (i == MESH_NUMBER_OF_NODE_LAYERS - 1) ASSERT(ARE_EQUAL(length, ROBOT_LENGTH));

                if (i < _MESH_NUMBER_OF_UPPER_NODE_LAYERS_EXCLUSIVE) {
                    length += MESH_UPPER_STACK_LENGTH;
                } else if (i < _MESH_NUMBER_OF_UPPER_NODE_LAYERS_EXCLUSIVE + _MESH_NUMBER_OF_LOWER_NODE_LAYERS_EXCLUSIVE) {
                    length += MESH_LOWER_STACK_LENGTH;
                } else {
                    length += ROBOT_SEGMENT_LENGTH;
                }
            }

            if (INCLUDE_PYRAMID_CAP) {
                sbuff_push_back(&X, V3(0.0, -length, 0.0));
            }
        }

        StretchyBuffer<Tet> tets = {}; {
            // https://www.alecjacobson.com/weblog/media/triangular-prism-split-into-three-tetrahedra.pdf
            for_(_c, MESH_NUMBER_OF_NODE_LAYERS - 1) {
                int c = (1 + _c) * MESH_NUMBER_OF_NODES_PER_NODE_LAYER - 1; // center of slice
                int f = c + MESH_NUMBER_OF_NODES_PER_NODE_LAYER;
                int o = _c * MESH_NUMBER_OF_NODES_PER_NODE_LAYER;
                for_line_loop_(_a, _b, MESH_NUMBER_OF_ANGULAR_SECTIONS) {
                    int a = o + _a;
                    int b = o + _b;
                    int d = a + MESH_NUMBER_OF_NODES_PER_NODE_LAYER;
                    int e = b + MESH_NUMBER_OF_NODES_PER_NODE_LAYER;
                    sbuff_push_back(&tets, { a, b, c, d       });
                    sbuff_push_back(&tets, {    b, c, d, e    });
                    sbuff_push_back(&tets, {       c, d, e, f });
                }
            }

            if (INCLUDE_PYRAMID_CAP) {
                int _c = MESH_NUMBER_OF_NODE_LAYERS - 1;
                int c = (1 + _c) * MESH_NUMBER_OF_NODES_PER_NODE_LAYER - 1; // center of slice
                int o = _c * MESH_NUMBER_OF_NODES_PER_NODE_LAYER;
                int t = X.length - 1; // tip
                for_line_loop_(_a, _b, MESH_NUMBER_OF_ANGULAR_SECTIONS) {
                    int a = o + _a;
                    int b = o + _b;
                    sbuff_push_back(&tets, { a, b, c, t });
                }
            }
        }

        StretchyBuffer<int> pins = {}; {
            for_(i, MESH_NUMBER_OF_NODES_PER_NODE_LAYER) { sbuff_push_back(&pins, i); }
        }

        StretchyBuffer<int> num_vias = {};
        StretchyBuffer<Via> vias = {};
        {
            for_(cable_group, 3) {
                for_(d, 3) {
                    int cable_num_vias = (cable_group == 0) ?
                        (1 + MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_UPPER_SEGMENT) :
                        (1 + 2 * MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_LOWER_SEGMENT);

                    sbuff_push_back(&num_vias, cable_num_vias);

                    int i_0; {

                        i_0 = cable_group + ((!MESH_9_12_TOGGLE) ? 3 : 4) * d;
                        if (cable_group >= 1) i_0 +=     (MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_UPPER_SEGMENT * MESH_NUMBER_OF_NODES_PER_NODE_LAYER);
                        if (cable_group >= 2) i_0 += 2 * (MESH_NUMBER_OF_VOLUMETRIC_STACKS_PER_LOWER_SEGMENT * MESH_NUMBER_OF_NODES_PER_NODE_LAYER);
                    }

                    for_(k, cable_num_vias) {
                        sbuff_push_back(&vias, { { i_0 + (k * MESH_NUMBER_OF_NODES_PER_NODE_LAYER), 1.0 } });
                    }
                }
            }
        }

        SimInput simInput = {}; {
            simInput.num_nodes = X.length;
            simInput.x_rest = (real *) X.data;

            simInput.num_tets = tets.length;
            simInput.tets = tets.data;

            simInput.num_pins = pins.length;
            simInput.pins = pins.data;

            simInput.num_vias = num_vias.data;
            simInput.num_cables = num_vias.length;
            simInput.vias = vias.data;
        }

        sim.allocAndPrecompute(&simInput);
    }

    u_MAX = SDVector(sim.num_cables); {
        for_(j, sim.num_cables) {
            u_MAX[j] = 0.8 * ROBOT_SEGMENT_LENGTH;
            if (j > 3) u_MAX[j] *= 2;

        }
    }
    currentState = State(&sim);
    currentState.enabled__BIT_FIELD = TETS | GRAVITY | PINS | CABLES;

    sim.getNext(&currentState); // FORNOW: global_U_xx


    { // set up skinned mesh
        IndexedTriangleMesh3D _dragonHead;
        IndexedTriangleMesh3D _dragonBody;
        { //CARL load meshes
            char pwd[256];
            #ifdef COW_OS_WINDOWS
            GetCurrentDirectory(_COUNT_OF(pwd), pwd);
            #else
            getcwd(pwd, _COUNT_OF(pwd));
            #endif
            // printf("\n\n");
            // printf(pwd);
            // printf("\n");

            // imagine we knew what directory we were in

            char headPath[256];
            char bodyPath[256];
            dll_agnostic_path(headPath, _COUNT_OF(headPath), "dragon_head.obj");
            dll_agnostic_path(bodyPath, _COUNT_OF(bodyPath), "dragon_body.obj");

            _dragonHead = _meshutil_indexed_triangle_mesh_load(headPath, false, true, false);
            _dragonBody = _meshutil_indexed_triangle_mesh_load(bodyPath, false, true, false);;

            mat4 RS = M4_RotationAboutXAxis(PI / 2) * M4_Scaling(0.05);
            _dragonHead._applyTransform(RS);
            _dragonBody._applyTransform(M4_Translation(0.0, -0.67, 0.0) * RS);
        }

        { // create bones in mesh
            { // body
                _dragonBody.num_bones = DRAGON_BODY_NUM_BONES;
                _dragonBody.bones        = (mat4 *) malloc(_dragonBody.num_bones    * sizeof(mat4));
                _dragonBody.bone_indices = (int4 *) malloc(_dragonBody.num_vertices * sizeof(int4));
                _dragonBody.bone_weights = (vec4 *) malloc(_dragonBody.num_vertices * sizeof(vec4));
            }
            { // head
                _dragonHead.num_bones = DRAGON_HEAD_NUM_BONES;
                _dragonHead.bones        = (mat4 *) malloc(_dragonHead.num_bones    * sizeof(mat4));
                _dragonHead.bone_indices = (int4 *) malloc(_dragonHead.num_vertices * sizeof(int4));
                _dragonHead.bone_weights = (vec4 *) malloc(_dragonHead.num_vertices * sizeof(vec4));
            }
        }


        { // assign indices and weights
            { // body
                { // set bones rest positions (body only)
                    for_(j, _COUNT_OF(bodyBoneOriginsRest)) {
                        bodyBoneOriginsRest[j] = get(sim.x_rest, 9 + j * 10);
                    }
                }
                for_(vertex_i, _dragonBody.num_vertices) {
                    auto f = [&](int i) {
                        real c = AVG(bodyBoneOriginsRest[i].y, bodyBoneOriginsRest[i + 1].y);
                        real D = ABS(_dragonBody.vertex_positions[vertex_i].y - c);
                        return MAX(0.0, (1.0 / D) - 10.0);
                    };

                    real t = INVERSE_LERP(_dragonBody.vertex_positions[vertex_i].y, 0.0, -ROBOT_LENGTH);
                    real b = t * _dragonBody.num_bones;

                    int j = MIN(MAX(int(b + 0.5), 0), _dragonBody.num_bones - 1);
                    int i = MAX(0, j - 1);
                    int k = MIN(_dragonBody.num_bones - 1, j + 1);

                    _dragonBody.bone_indices[vertex_i] = { i, j, k };
                    _dragonBody.bone_weights[vertex_i] = { f(i), f(j), f(k) };
                    _dragonBody.bone_weights[vertex_i] /= sum(_dragonBody.bone_weights[vertex_i]);

                    jim_sort_against(
                            (int *) &_dragonBody.bone_indices[vertex_i],
                            4,
                            sizeof(int),
                            (real *) &_dragonBody.bone_weights[vertex_i],
                            true);
                    _dragonBody.bone_indices[vertex_i] = { 
                        _dragonBody.bone_indices[vertex_i][3],
                        _dragonBody.bone_indices[vertex_i][2],
                        _dragonBody.bone_indices[vertex_i][1],
                        _dragonBody.bone_indices[vertex_i][0],
                    };
                    _dragonBody.bone_weights[vertex_i] = {
                        _dragonBody.bone_weights[vertex_i][3],
                        _dragonBody.bone_weights[vertex_i][2],
                        _dragonBody.bone_weights[vertex_i][1],
                        _dragonBody.bone_weights[vertex_i][0],
                    };

                    ASSERT(_dragonBody.bone_weights[vertex_i][0] >= _dragonBody.bone_weights[vertex_i][1]);
                    ASSERT(_dragonBody.bone_weights[vertex_i][1] >= _dragonBody.bone_weights[vertex_i][2]);
                    ASSERT(_dragonBody.bone_weights[vertex_i][2] >= _dragonBody.bone_weights[vertex_i][3]);
                }
            }
            { // head
                for_(i, _dragonHead.num_vertices) {
                    _dragonHead.bone_indices[i] = { 0 };  
                    _dragonHead.bone_weights[i] = { 1.0 };  
                }
            }
        }
        dragon = _dragonBody + _dragonHead;
    }


    cpp_reset();
}

delegate void cpp_exit() {
    dxl_exit();
    csv_exit();
}

// returns whether the ray hit the mesh
// if the ray hit the mesh, functions writes to intersection_position__FLOAT_ARRAY__LENGTH_3
// if the ray hit the mesh and pleaseSetFeaturePoint, updates indexOfFeaturePointToSet-th feature point in C++
//                                                    and writes to feature_point_positions__FLOAT3__ARRAY (unless NULL)
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
        void *feature_point_positions__FLOAT3__ARRAY = NULL) {

    vec3 ray_origin = { ray_origin_x, ray_origin_y, _ZZZ(2) * ray_origin_z };
    vec3 ray_direction = { ray_direction_x, ray_direction_y, _ZZZ(2) * ray_direction_z };
    IntersectionResult result; {
        if (_DRAGON_DRIVING__SET_IN_CPP_INIT) {
            if (CPP_HACK_RUNNING_ON_UNITY__NOTE_FOR_GPU_STUFF) cow_begin_frame();
            dragon.bones = currentBones.data;
            result = GPU_pick(ray_origin, ray_direction, &dragon);
        } else {
            result = ray_mesh_intersection(ray_origin, ray_direction, currentState.x, sim.num_triangles, sim.triangle_indices);
        }
    }
    if (result.hit) {
        for_(d, 3) (((float *) intersection_position__FLOAT_ARRAY__LENGTH_3)[d]) = float(_ZZZ(d) * result.p[d]);
        if (pleaseSetFeaturePoint) {
            ASSERT(indexOfFeaturePointToSet >= 0);
            ASSERT(indexOfFeaturePointToSet < MAX_NUM_FEATURE_POINTS);
            featurePoints[indexOfFeaturePointToSet] = { { { result.tri[0], result.w[0] }, { result.tri[1], result.w[1] }, { result.tri[2], result.w[2] } } };
            if (feature_point_positions__FLOAT3__ARRAY) {
                for_(d, 3) ((float *) feature_point_positions__FLOAT3__ARRAY)[3 * indexOfFeaturePointToSet + d] = float(_ZZZ(d) * result.p[d]);
            }
        }
    }

    return result.hit;
}

// solves one step of IK (and physics; and bones)
// writes resulting mesh to vertex_positions__FLOAT3_ARRAY, vertex_normals__FLOAT3_ARRAY, triangle_indices__UINT_ARRAY
// also writes feature_point_positions__FLOAT3__ARRAY for use by unity
delegate void cpp_solve(
        int num_feature_points,
        void *_targetEnabled__INT_ARRAY,
        void *_targetPositions__FLOAT3_ARRAY,
        void *vertex_positions__FLOAT3_ARRAY,
        void *vertex_normals__FLOAT3_ARRAY,
        void *triangle_indices__UINT_ARRAY,
        void *feature_point_positions__FLOAT3__ARRAY) {
    ASSERT(num_feature_points <= MAX_NUM_FEATURE_POINTS);
    int *targetEnabled = (int *) _targetEnabled__INT_ARRAY;
    vec3 targetPositions[MAX_NUM_FEATURE_POINTS]; {
        for_(i, num_feature_points) {
            for_(d, 3) targetPositions[i][d] = _ZZZ(d) * ((float *) _targetPositions__FLOAT3_ARRAY)[3 * i + d];
        }
    }
    bool relax; {
        relax = true;
        for_(i, MAX_NUM_FEATURE_POINTS) relax &= (!targetEnabled[i]);
    }

    IndexedTriangleMesh3D *mesh = &dragon;

    { // step ik
        static real Q_c      = 1.0;
        static real R_c_log  = 0.0036;
        static real R_c_quad = 0.0036;
        static real S_c      = 7.5;
        static real S_eps    = 0.002;
        static real alpha_0  = 0.0045;
        static real _R_c_RELAX  = 32.0;
        static bool project = true;

        // {
        //     gui_slider("Q_c", &Q_c, 0.0, 10.0);
        //     gui_slider("R_c", &R_c, 0.0, 0.1);
        //     gui_slider("S_c", &S_c, 0.0, 10.0);
        //     gui_slider("S_eps", &S_eps, 0.0, 0.01);
        //     gui_slider("alpha_0", &alpha_0, 0.0, 0.01);
        // }
        // gui_checkbox("project", &project, 'b'); // FORNOW (will break dll?)

        auto get_O = [&](State staticallyStableState, const Bones &correspondingBones) -> real {
            const SDVector &u = staticallyStableState.u;
            const SDVector &x = staticallyStableState.x;
            const Bones &bones = correspondingBones;

            if (relax) {
                return _R_c_RELAX * (0.5 * squaredNorm(u));
            } else {
                real Q = 0.0; {
                    for_(i, MAX_NUM_FEATURE_POINTS) if (targetEnabled[i]) {
                        vec3 p = (_DRAGON_DRIVING__SET_IN_CPP_INIT) ? skinnedGet(mesh, bones, featurePoints[i]) : get(x, featurePoints[i]);
                        Q += Q_c * .5 * squaredNorm(p - targetPositions[i]);
                    }
                }

                real R = 0.0; {
                    R += R_c_quad * (0.5 * squaredNorm(u));
                    for_(j, sim.num_cables) {
                        R += R_c_log  * (-log(u_MAX[j] - u[j]));
                        if (u[j] > u_MAX[j]) { R = HUGE_VAL; }
                    }

                }

                real S = 0.0; {
                    SDVector Deltas = sim.computeCableDeltas(x, u);
                    for_(j, LEN_U) {
                        S += ZCQ(S_c, S_eps - Deltas[j]);
                    }
                }

                real result = Q + R + S;
                if (_isnan(result)) { result = INFINITY; }
                return result;
            }
        };

        { // single gradient descent step with backtracking line search
            SDVector dOdu(LEN_U);
            SDVector &u = currentState.u;
            SDVector x = currentState.x; // FORNOW NOT CONST
            Bones &bones = currentBones;
            {
                if (project) { // project
                    SDVector Slacks = sim.computeCableDeltas(x, u);
                    for_(j, LEN_U) Slacks[j] = -MIN(0.0, Slacks[j]);
                    for_(j, LEN_U) {
                        if (Slacks[j] > -.000001) {
                            u[j] += Slacks[j] + .00001;
                        }
                    }
                }
                if (relax) {
                    // TODO scalar multiplication
                    for_(j, sim.num_cables) {
                        dOdu[j] += _R_c_RELAX * u[j];
                    }
                } else {
                    SDVector dQdu; {
                        SDVector dQdx(LEN_X); {
                            if (_DRAGON_DRIVING__SET_IN_CPP_INIT) {
                                SDVector dQds(LEN_S); {
                                    for_(i, MAX_NUM_FEATURE_POINTS) if (targetEnabled[i]) {
                                        vec3 p = skinnedGet(mesh, bones, featurePoints[i]);
                                        add(dQds, featurePoints[i], Q_c * (p - targetPositions[i]));
                                    }
                                }

                                Eigen::SparseMatrix<real> SPARSE_dsdx(LEN_S, LEN_X); {
                                    real delta = 1e-5;
                                    StretchyBuffer<OptEntry> triplets = {}; {
                                        // sparse finite difference
                                        StretchyBuffer<int> S_node_indices = {};
                                        StretchyBuffer<int> X_node_indices = {};
                                        {
                                            { // S
                                                for_(featurePoint_i, MAX_NUM_FEATURE_POINTS) if (targetEnabled[featurePoint_i]) {
                                                    for_(d, 3) sbuff_push_back(&S_node_indices, featurePoints[featurePoint_i][d].index);
                                                }
                                            }
                                            { // X FORNOW SO HACKY
                                                for_(bone_i, DRAGON_BODY_NUM_BONES + 1) {
                                                    sbuff_push_back(&X_node_indices, _9_12() + bone_i * _10_13());
                                                    sbuff_push_back(&X_node_indices, 0 + bone_i * _10_13());
                                                }
                                            }
                                        }
                                        Bones bonesRight;
                                        Bones bonesLeft;
                                        for_(_jj, X_node_indices.length) { int j = X_node_indices[_jj];
                                            for_(d_j, 3) {
                                                real tmp = x[3 * j + d_j]; {
                                                    x[3 * j + d_j] = tmp + delta;
                                                    bonesRight = getBones(x); 
                                                    x[3 * j + d_j] = tmp - delta;
                                                    bonesLeft = getBones(x); 

                                                    for_(_ii, S_node_indices.length) { int i = S_node_indices[_ii];
                                                        vec3 s_i_Right = skinnedGet(mesh, bonesRight, i); 
                                                        vec3 s_i_Left = skinnedGet(mesh, bonesLeft, i); 
                                                        vec3 s_i_prime = (s_i_Right - s_i_Left) / (2 * delta);
                                                        for_(d_i, 3) {
                                                            sbuff_push_back(&triplets, { 3 * i + d_i, 3 * j + d_j, s_i_prime[d_i] });
                                                        }
                                                    }
                                                } x[3 * j + d_j] = tmp;
                                            }
                                        }
                                    }
                                    SPARSE_dsdx.setFromTriplets(triplets.data, triplets.data + triplets.length);
                                }

                                Eigen::Map<EigenVectorXr> MAP_dQds(dQds.data, LEN_S);
                                EigenVectorXr EIGEN_dQdx = MAP_dQds.transpose() * SPARSE_dsdx;
                                memcpy(dQdx.data, EIGEN_dQdx.data(), LEN_X * sizeof(real));

                            } else {
                                for_(i, MAX_NUM_FEATURE_POINTS) if (targetEnabled[i]) {
                                    add(dQdx, featurePoints[i], Q_c * (get(x, featurePoints[i]) - targetPositions[i]));
                                }
                            }
                        }
                        SDVector L = global_U_xx.solve(dQdx);
                        dQdu = L * global_dFdu;
                    }

                    SDVector dRdu(LEN_U); {
                        for_(j, sim.num_cables) {
                            dRdu[j] += R_c_quad * u[j];
                            dRdu[j] += R_c_log  / (u_MAX[j] - u[j]);
                            if (u[j] > u_MAX[j]) { dRdu[j] = INFINITY; }
                        }
                    }

                    SDVector dSdu(LEN_U); {
                        SDVector Deltas = sim.computeCableDeltas(x, u);
                        for_(j, sim.num_cables) {
                            dSdu[j] += -ZCQp(S_c, S_eps - Deltas[j]);
                        }
                    }

                    for_(j, LEN_U) dOdu[j] = dQdu[j] + dRdu[j] + dSdu[j];
                    for_(j, LEN_U) if (_isnan(dOdu[j])) dOdu[j] = 0; // FORNOW
                }
            }
            {
                State nextState = currentState;
                {
                    real O_curr = get_O(currentState, currentBones);
                    int attempt = 0;
                    do {
                        for_(j, LEN_U) nextState.u[j] = currentState.u[j] - alpha_0 * pow(.5, attempt) * dOdu[j];
                        nextState = sim.getNext(&nextState);
                        real O_next = get_O(nextState, getBones(nextState.x));
                        if (O_next < O_curr) { break; }
                    } while (attempt++ < IK_MAX_LINE_SEARCH_STEPS);
                }
                currentState = nextState;
                currentBones = getBones(currentState.x);
            }
        }
    }

    csv_solve(currentState); // FORNOW

    { // marshall mesh
        SDVector vertex_normals = sim.get_vertex_normals(currentState.x);
        for_(k, LEN_X) {
            int d = k % 3;
            ((UnityVertexAttributeFloat *) vertex_positions__FLOAT3_ARRAY)[k] = UnityVertexAttributeFloat(_ZZZ(d) * currentState.x[k]);
            ((UnityVertexAttributeFloat *) vertex_normals__FLOAT3_ARRAY)[k] = UnityVertexAttributeFloat(_ZZZ(d) * vertex_normals[k]);
        }
        for_(k, 3 * sim.num_triangles) {
            ((UnityTriangleIndexInt *) triangle_indices__UINT_ARRAY)[k] = (UnityTriangleIndexInt) ((int *) sim.triangle_indices)[k];
        }
        #ifdef JIM_DLL
        // need to flip orientation for unity (ew)
        for_(i, sim.num_triangles) {
            UnityTriangleIndexInt tmp = ((UnityTriangleIndexInt *) triangle_indices__UINT_ARRAY)[3 * i + 0];
            ((UnityTriangleIndexInt *) triangle_indices__UINT_ARRAY)[3 * i + 0] = ((UnityTriangleIndexInt *) triangle_indices__UINT_ARRAY)[3 * i + 1];
            ((UnityTriangleIndexInt *) triangle_indices__UINT_ARRAY)[3 * i + 1] = tmp;
        }
        #endif
        for_(indexOfFeaturePointToSet, num_feature_points) {
            vec3 tmp = (_DRAGON_DRIVING__SET_IN_CPP_INIT)
                ? skinnedGet(mesh, currentBones, featurePoints[indexOfFeaturePointToSet])
                : get(currentState.x, featurePoints[indexOfFeaturePointToSet]);
            for_(d, 3) ((float *) feature_point_positions__FLOAT3__ARRAY)[3 * indexOfFeaturePointToSet + d] = UnityVertexAttributeFloat(_ZZZ(d) * tmp[d]);
        }
    }

    ASSERT(currentBones.N > 0);
}

delegate void cpp_send2motors() {
    kaa_dxl_write(currentState.u.data);
}




vec3 KAA_targetPositions[MAX_NUM_FEATURE_POINTS];
int  KAA_targetEnabled[MAX_NUM_FEATURE_POINTS];
int  KAA_numPopped;
void KAA_reset() {
    memset(KAA_targetPositions, 0, sizeof(KAA_targetPositions));
    memset(KAA_targetEnabled, 0, sizeof(KAA_targetEnabled));
    // KAA_targetPositions[0] = get(sim.x_rest, featurePoints[0]) + .3 * V3(0, 1, 1);
    // KAA_targetEnabled[0] = TRUE;
}
bool KAA_AUTOMATED_SPEED_TEST__QUITS_AFTER_A_COUPLE_SECONDS = false; // TODO: this crashes if true?
void kaa() {
    cpp_init(KAA_CPP_INIT_FLAG);
    KAA_reset();

    UnityVertexAttributeFloat *SPOOF_vertex_positions = (UnityVertexAttributeFloat *) calloc(LEN_X, sizeof(UnityVertexAttributeFloat));
    UnityVertexAttributeFloat *SPOOF_vertex_normals = (UnityVertexAttributeFloat *) calloc(LEN_X, sizeof(UnityVertexAttributeFloat));
    UnityTriangleIndexInt     *SPOOF_triangle_indices = (UnityTriangleIndexInt *) calloc(3 * sim.num_triangles, sizeof(UnityTriangleIndexInt));
    UnityVertexAttributeFloat *SPOOF_feature_point_positions = (UnityVertexAttributeFloat *) calloc(3 * MAX_NUM_FEATURE_POINTS, sizeof(UnityVertexAttributeFloat));

    COW1._gui_hide_and_disable = KAA_AUTOMATED_SPEED_TEST__QUITS_AFTER_A_COUPLE_SECONDS;
    Camera3D camera = { 1.7 * ROBOT_LENGTH, RAD(60), 0.0, 0.0, 0.0, -0.5 * ROBOT_LENGTH };
    while (cow_begin_frame()) {
        camera_move(&camera);
        mat4 P = camera_get_P(&camera);
        mat4 V = camera_get_V(&camera);
        mat4 PV = P * V;

        dragonAnimationTime += 0.0167; // FORNOW


        auto draw_sphere = [&](vec3 position, real radius = 0.0, vec3 color = monokai.white) {
            if (IS_ZERO(radius)) radius = 0.01;
            library.meshes.sphere.draw(P, V, M4_Translation(position) * M4_Scaling(radius), color);
        };

        struct CastRayResult {
            bool intersects;
            vec3 intersection_position;
        };
        auto castRay = [&](bool pleaseSetFeaturePoint, int featurePointIndex) -> CastRayResult {
            CastRayResult result = {};
            vec3 ray_origin = camera_get_position(&camera);
            vec3 ray_direction = camera_get_mouse_ray(&camera);
            float intersection_position__FLOAT_ARRAY__LENGTH_3[3]; {
                result.intersects = cpp_castRay(
                        (float) ray_origin.x,
                        (float) ray_origin.y,
                        (float) ray_origin.z,
                        (float) ray_direction.x,
                        (float) ray_direction.y,
                        (float) ray_direction.z,
                        intersection_position__FLOAT_ARRAY__LENGTH_3,
                        pleaseSetFeaturePoint,
                        featurePointIndex,
                        SPOOF_feature_point_positions);
            }
            for_(d, 3) result.intersection_position[d] = intersection_position__FLOAT_ARRAY__LENGTH_3[d];
            return result;
        };

        if (gui_button("reset", 'r')) {
            cpp_reset();
            KAA_reset();
        }

        // NOTE: very important to have solved physics at least once so the global hessian is ret to go
        static bool SPOOF_solveIK = true;
        gui_checkbox("SPOOF_solveIK", &SPOOF_solveIK, COW_KEY_SPACE);
        if (SPOOF_solveIK) { // ik
            if ((sim.num_cables >= 0)) {
                float _SPOOF_target_positions__FLOAT_ARRAY[3 * MAX_NUM_FEATURE_POINTS]; {
                    for_(k, _COUNT_OF(_SPOOF_target_positions__FLOAT_ARRAY)) _SPOOF_target_positions__FLOAT_ARRAY[k] = float(((real *) KAA_targetPositions)[k]);
                }

                cpp_solve(
                        MAX_NUM_FEATURE_POINTS,
                        KAA_targetEnabled,
                        _SPOOF_target_positions__FLOAT_ARRAY,
                        SPOOF_vertex_positions,
                        SPOOF_vertex_normals,
                        SPOOF_triangle_indices,
                        SPOOF_feature_point_positions);
            }
        }
        static bool SPOOF_send2motors = true;
        gui_checkbox("SPOOF_send2motors", &SPOOF_send2motors);
        if (SPOOF_send2motors) {
            cpp_send2motors();
        }

        { // draw scene
            static int tabs = 0;
            if (globals.key_pressed['1']) ++tabs;
            gui_checkbox("_DRAGON_SHOW", &_DRAGON_SHOW, COW_KEY_TAB);
            if (!_DRAGON_SHOW) {
                {
                    if (tabs % 3 == 0) {
                        sim.draw(P, V, M4_Identity(), &currentState);
                    } else if (tabs % 3 == 1) { // draw
                        sim.draw(P * V, &currentState);
                    } else { // check stuff being sent to C#
                        eso_begin(PV, SOUP_OUTLINED_TRIANGLES);
                        eso_color(monokai.black);
                        for_(triangle_i, cpp_getNumTriangles()) {
                            for_(d, 3) {
                                eso_vertex(
                                        SPOOF_vertex_positions[3 * SPOOF_triangle_indices[3 * triangle_i + d] + 0],
                                        SPOOF_vertex_positions[3 * SPOOF_triangle_indices[3 * triangle_i + d] + 1],
                                        SPOOF_vertex_positions[3 * SPOOF_triangle_indices[3 * triangle_i + d] + 2]
                                        );
                            }
                        }
                        eso_end();
                    }
                }
            } else {
                dragon.bones = currentBones.data;
                dragon.draw(P, V, globals.Identity);
            }


            { // widget
                bool mouseClickConsumed = false;
                bool mouseHotConsumed = false;
                { // KAA_targetPositions
                    for_(featurePointIndex, MAX_NUM_FEATURE_POINTS) {
                        if (!KAA_targetEnabled[featurePointIndex]) continue;
                        vec3 color = color_kelly(featurePointIndex);
                        vec3 SPOOF_feature_point_position = { SPOOF_feature_point_positions[3 * featurePointIndex + 0], SPOOF_feature_point_positions[3 * featurePointIndex + 1], SPOOF_feature_point_positions[3 * featurePointIndex + 2] };

                        WidgetResult widgetResult = widget(P, V, featurePointIndex, &KAA_targetPositions[featurePointIndex], SPOOF_feature_point_position, color);

                        mouseClickConsumed |= widgetResult.mouseClickConsumed; // FORNOW
                        mouseHotConsumed |= widgetResult.mouseHotConsumed; // FORNOW
                        if (widgetResult.pleaseDisableHandle) {
                            KAA_targetEnabled[featurePointIndex] = FALSE;
                        }
                        if (widgetResult.recastFeaturePoint) {
                            castRay(true, featurePointIndex);
                        }
                    }
                }


                if (!mouseClickConsumed && !mouseHotConsumed) { // SPOOF_intersection_position
                    bool pleaseSetFeaturePoint = globals.mouse_left_pressed;
                    int featurePointIndex; {
                        for (featurePointIndex = 0; KAA_targetEnabled[featurePointIndex]; ++featurePointIndex) {}
                        ASSERT(featurePointIndex < MAX_NUM_FEATURE_POINTS);
                    }
                    CastRayResult castRayResult = castRay(pleaseSetFeaturePoint, featurePointIndex);
                    if (!globals.mouse_left_held && castRayResult.intersects) draw_ball(P, V, castRayResult.intersection_position, color_kelly(featurePointIndex));
                    if (castRayResult.intersects && pleaseSetFeaturePoint) {
                        KAA_targetEnabled[featurePointIndex] = TRUE;
                        KAA_targetPositions[featurePointIndex] = castRayResult.intersection_position;
                    }
                }
            }

            { // ceiling
                real r = 0.3;
                eso_begin(PV, (tabs % 3 == 0) ? SOUP_QUADS : SOUP_OUTLINED_QUADS);
                if (tabs % 3 == 0) {
                    eso_color(1.0, 1.0, 1.0, 0.5);
                } else {
                    eso_color(0.0, 0.0, 0.0, 0.5);
                }
                eso_vertex( r, 0.0,  r);
                eso_vertex( r, 0.0, -r);
                eso_vertex(-r, 0.0, -r);
                eso_vertex(-r, 0.0,  r);
                eso_end();
            }
        }

        { // fornow
            if (globals.key_held['a']) KAA_targetPositions[0] = transformPoint(M4_RotationAboutYAxis(RAD(1)), KAA_targetPositions[0]);
        }


        if (1) { // manual sliders
            for_(j, sim.num_cables) {
                char buffer[] = "u_X";
                buffer[2] = char('0' + j);
                gui_slider(buffer, &currentState.u[j], -(ROBOT_LENGTH / 3), (ROBOT_LENGTH / 3));
            }
        }



        { // balloons
            if (KAA_numPopped < balloon_number_of_balloons) {
                vec3 tipPosition = get(currentState.x, sim.num_nodes - 1);
                vec3 currentBalloonPosition = balloon_positions[KAA_numPopped];
                if (norm(tipPosition - currentBalloonPosition) < balloon_radius) {
                    ++KAA_numPopped;
                }
            }
            for_(i, balloon_number_of_balloons) {
                if ( i < KAA_numPopped) continue;
                vec3 color = (i == KAA_numPopped) ? monokai.green : monokai.gray;
                draw_sphere(balloon_positions[i], balloon_radius, color);
            }

        }


        { // FORNOW automated testing
            if (KAA_AUTOMATED_SPEED_TEST__QUITS_AFTER_A_COUPLE_SECONDS) {
                static real testTime = 0.0;
                KAA_targetPositions[0] += V3(0.003);
                if (testTime > 2.0) {
                    exit(1);
                }
                testTime += .0167;
            }
        }
    }
    cpp_exit();
}

#undef LEN_U
#undef LEN_X


////////////////////////////////////////////////////////////////////////////////
delegate UnityGeneralPurposeInt cpp_getTotalNumVias() { return sim.num_cable_vias_total; }
delegate UnityGeneralPurposeInt cpp_getNumCables() { return sim.num_cables; }
delegate void cpp_getNumViasPerCable(void *cpp_vias) {
    for (int i = 0; i < sim.num_cables; i++) {
        ((UnityGeneralPurposeInt *)cpp_vias)[i] = sim.num_vias[i];
    }
}
delegate void cpp_getCables(
        void *cpp_cable_positions,
        void *cpp_tensions) {

    for (int i = 0; i < sim.num_cable_vias_total; i++) {
        vec3 v = get(currentState.x, sim.vias[i]);
        for_(d, 3) {
            ((float *)cpp_cable_positions)[i * 3 + d] = (float)(_ZZZ(d) * v[d]);
        }
    }

    SDVector tensions = sim.computeCableTensions(currentState.x, currentState.u);
    for (int i = 0; i < sim.num_cables; i++) {
        ((float *)cpp_tensions)[i] = (float)(tensions[i]);
    }
}
////////////////////////////////////////////////////////////////////////////////

void jones() {
    cpp_init();

    Camera3D camera = {};
    camera.persp_distance_to_origin =  1.0;
    camera.angle_of_view = RAD(0);
    camera.theta = RAD(0);
    camera.phi = RAD(-90);

    StretchyBuffer<real> u_buffer = {}; { // FORNOW
        FILE *file = fopen("CircleU.csv", "r");
        ASSERT(file);
        char line[4096];
        real u[9];
        while (fgets(line, _COUNT_OF(line), file) != NULL) {
            sscanf(line, "%lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf", &u[0], &u[1], &u[2], &u[3], &u[4], &u[5], &u[6], &u[7], &u[8]);
            for_(j, 9) sbuff_push_back(&u_buffer, u[j]);
        }
        fclose(file);
    }

    StretchyBuffer<vec3> mocap_buffer = {}; {
        FILE *file = fopen("CircleTip.csv", "r");
        ASSERT(file);
        char line[4096];
        vec3 tip;
        while (fgets(line, _COUNT_OF(line), file) != NULL) {
            sscanf(line, "%lf, %lf, %lf", &tip[0], &tip[1], &tip[2]);
            tip -= V3(-699.702209, 1050.0,  -493.657928);
            tip = transformPoint(M4_RotationAboutYAxis(RAD(180)), tip);
            tip *= .001;
            sbuff_push_back(&mocap_buffer, tip);
        }
        fclose(file);
    }

    #define TRACE_QUEUE_LENGTH 512
    vec3 trace[TRACE_QUEUE_LENGTH] = {};

    int frame = 0;
    int frame_number_of_frames = u_buffer.length / 9;
    int frame_jump = 5;

    tetMassDensity = 1300.0;
    tetYoungsModulus = 800000.0;

    while (cow_begin_frame()) {

        // camera_attach_to_gui(&camera);
        camera_move(&camera);
        mat4 P = camera_get_P(&camera);
        mat4 V = camera_get_V(&camera);
        mat4 PV = P * V;

        gui_printf("MESH_9_12_TOGGLE %d", MESH_9_12_TOGGLE);
        gui_slider("tetMassDensity", &tetMassDensity, 0.0, 5000.0);
        gui_slider("tetYoungsModulus", &tetYoungsModulus, 2.0, 8.0, false, true);
        gui_slider("tetPoissonsRatio", &tetPoissonsRatio, 0.45, 0.49);
        gui_slider("frame_jump", &frame_jump, 1, 10, 'j', 'k');

        memcpy(currentState.u.data, u_buffer.data + 9 * frame, 9 * sizeof(real));
        frame = (frame + frame_jump) % frame_number_of_frames;
        currentState = sim.getNext(&currentState);

        { // update trace
            for (int i = TRACE_QUEUE_LENGTH - 1; i > 0; --i) trace[i] = trace[i - 1];
            trace[0] = get(currentState.x, sim.num_nodes - 1);
        }

        { // draw
            eso_begin(PV, SOUP_LINE_STRIP);
            for_(i, MIN(frame, TRACE_QUEUE_LENGTH)) {
                eso_color(color_plasma(NUM_DENm1(i, TRACE_QUEUE_LENGTH)));
                eso_vertex(trace[i]);
            }
            eso_end();
            sim.draw(P, V, M4_Identity(), &currentState);

            soup_draw(PV, SOUP_LINE_STRIP, mocap_buffer.length, mocap_buffer.data, NULL, monokai.blue);
        }
    }
}


void main() {
    omp_set_num_threads(6);
    APPS {
        // APP(jones);
        APP(kaa);
        // APP(eg_fbo);
    }
}
