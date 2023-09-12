#if 0
//CARL
void jonesUpdateCableReferenceLengths() {
    State copy = currentState;

    printf("\nBEFORE: ");
    for(int i = 0; i < sim.num_cables; i++) {
        printf("%lf ", sim.cableReferenceLengths[i]);
    }

    copy.enabled__BIT_FIELD |= ~CABLES;
    copy = sim.getNext(&copy);
    sim.cableReferenceLengths = sim.getCableLengths(copy.x);

    printf("\nAFTER: ");
    for(int i = 0; i < sim.num_cables; i++) {
        printf("%lf ", sim.cableReferenceLengths[i]);
    }
}

const int JOSIE_NUM_FRAMES = 15649;
const int JOSIE_NUM_RIGID_BODIES = 7;
const int JOSIE_NUM_CABLES = 9;
const int JOSIE_NUM_CABLE_POSITIONS = 38;
const int JOSIE_NUM_MARKERS = 28;

void jones() {

    static vec3 rigidbodies[JOSIE_NUM_RIGID_BODIES][JOSIE_NUM_FRAMES];
    static vec3 markerpositions[JOSIE_NUM_MARKERS][JOSIE_NUM_FRAMES];
    static real tendonlengths[JOSIE_NUM_CABLES][JOSIE_NUM_CABLE_POSITIONS];
    static vec3 generated_positions[JOSIE_NUM_CABLE_POSITIONS+1];

    //Reading in the rigidbody position file
    FILE *file = fopen("rigidbodydata.csv", "r");
    ASSERT(file);
    char data_buffer[4096];
    int count = 0;
    while (fgets(data_buffer, _COUNT_OF(data_buffer), file) != NULL) {
        sscanf(data_buffer, "%lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf",
                &rigidbodies[0][count].x, &rigidbodies[0][count].y, &rigidbodies[0][count].z, &rigidbodies[1][count].x, &rigidbodies[1][count].y, &rigidbodies[1][count].z,
                &rigidbodies[2][count].x, &rigidbodies[2][count].y, &rigidbodies[2][count].z, &rigidbodies[3][count].x, &rigidbodies[3][count].y, &rigidbodies[3][count].z,
                &rigidbodies[4][count].x, &rigidbodies[4][count].y, &rigidbodies[4][count].z, &rigidbodies[5][count].x, &rigidbodies[5][count].y, &rigidbodies[5][count].z,
                &rigidbodies[6][count].x, &rigidbodies[6][count].y, &rigidbodies[6][count].z);
        for (int i = 0; i < JOSIE_NUM_RIGID_BODIES; i++) {
            rigidbodies[i][count] = cwiseProduct(rigidbodies[i][count], V3(1, 1, 1) * 0.001); //Was V3(-1, 1, -1)
        }
        count++;
    }
    fclose(file);

    //Reading in the marker position file
    file = fopen("markerdata.csv", "r");
    ASSERT(file);
    char* token;
    count = 0;
    while (fgets(data_buffer, _COUNT_OF(data_buffer), file) != NULL) {
        int index = 0;
        token = strtok(data_buffer, ",");
        while(token != NULL) {
            markerpositions[index/3][count][index%3] = std::atof(token) * 0.001;
            index++;
            token = strtok(NULL, ",");
        }
        count++;
    }
    fclose(file);


    vec3 x = rigidbodies[0][0];
    for(int i = 0; i < JOSIE_NUM_FRAMES; i++) {
        for(int j = 0; j < JOSIE_NUM_RIGID_BODIES; j++) {
            rigidbodies[j][i] -= x;
        }
        for(int k = 0; k < JOSIE_NUM_MARKERS; k++) {
            markerpositions[k][i] -= x;
        }
    }

    file = fopen("tendonlengthdata.csv", "r");
    ASSERT(file);
    count = 0;
    while (fgets(data_buffer, _COUNT_OF(data_buffer), file) != NULL) {
        sscanf(data_buffer, "%lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf",
                &tendonlengths[0][count], &tendonlengths[1][count], &tendonlengths[2][count], 
                &tendonlengths[3][count], &tendonlengths[4][count], &tendonlengths[5][count], 
                &tendonlengths[6][count], &tendonlengths[7][count], &tendonlengths[8][count]);
        for (int i = 0; i < JOSIE_NUM_CABLES; i++) {
            tendonlengths[i][count] = (0.001 * tendonlengths[i][count]);
        }
        count++;
    }
    fclose(file);

    int time_start              = 0;
    int time_end                = JOSIE_NUM_FRAMES - 1;
    bool playing                = false;
    bool generating             = false;
    bool generate_has_run       = false;
    int generate_curr_state     = -1;
    real generate_time          = 0.0;
    bool free_sliders           = false;
    int robot_state             = 0;
    int robot_angle             = 170;
    int kaa_draw_mode           = 1;
    int robot_draw_mode         = 3;
    real cable_input_multiplier = 1.0;

    cpp_init();

    Camera3D camera = { 1.7 * ROBOT_LENGTH, RAD(60), 0.0, 0.0, 0.0, -0.5 * ROBOT_LENGTH };

    while (cow_begin_frame()) {

        camera_move(&camera);
        mat4 P = camera_get_P(&camera);
        mat4 V = camera_get_V(&camera);
        mat4 PV = P * V;

        gui_checkbox("Generating", &generating, 'o');
        gui_checkbox("Playing", &playing, 'p');

        //ROBOT
        {

            gui_printf("");
            gui_printf("ROBOT");
            gui_slider("Draw Mode", &robot_draw_mode, 0, 9, 'n', 'm', true);

            if (playing) {
                time_end++;
                if (time_end >= JOSIE_NUM_FRAMES) time_end = 0;

                gui_slider("Time start", &time_start, 0, JOSIE_NUM_FRAMES, 't', 'y');
                gui_printf("Time end %d", time_end);
                if (time_start > time_end) time_start = time_end;
            }
            else {
                gui_slider("Time start", &time_start, 0, JOSIE_NUM_FRAMES, 't', 'y');
                if (time_start > time_end) time_end = time_start;
                gui_slider("Time end", &time_end, 0, JOSIE_NUM_FRAMES, 'g','h');
                if (time_start > time_end) time_start = time_end;
            }

            gui_printf("%d frames in range", 1 + time_end - time_start);

            gui_slider("Angle", &robot_angle, 0, 360);
            mat4 Mrobot = M4_RotationAboutYAxis(RAD(robot_angle));

            if(robot_draw_mode > 2) {
                for(int i = robot_draw_mode > 5 ? JOSIE_NUM_RIGID_BODIES - 1: 0; i < JOSIE_NUM_RIGID_BODIES; i++) {
                    soup_draw(PV * Mrobot, SOUP_LINE_STRIP, 1 + time_end - time_start, &(rigidbodies[i][time_start]),
                            NULL, monokai.red * ((playing && robot_draw_mode != 3) ? 0.5 : 1.0), 10);
                }
            }

            if(robot_draw_mode%3 > 0){
                glDisable(GL_DEPTH_TEST);
                eso_begin(PV * Mrobot, SOUP_LINE_STRIP, 2);
                eso_color(monokai.white);
                for(int i = 0; i < JOSIE_NUM_RIGID_BODIES; i++) {
                    eso_vertex(rigidbodies[i][time_end]);
                }
                eso_end();
                eso_begin(PV * Mrobot, SOUP_POINTS, 10);
                if (robot_draw_mode % 3 == 2) {
                    eso_color(monokai.yellow);
                    for(int i = 0; i < JOSIE_NUM_MARKERS; i++) {
                        eso_vertex(markerpositions[i][time_end]);
                    }
                }
                eso_color(monokai.white);
                for(int i = 0; i < JOSIE_NUM_RIGID_BODIES; i++) {
                    eso_vertex(rigidbodies[i][time_end]);
                }
                eso_end();
                glEnable(GL_DEPTH_TEST);
            }
        }

        //KAA
        {
            gui_printf("");
            gui_printf("KAA");

            gui_slider("Draw Mode", &kaa_draw_mode, 0, 3, 'u', 'i', true);

            if(!generating) {
                generate_time = 0.0;

                gui_checkbox("Free Sliders", &free_sliders);

                if (gui_button("reset", 'r')) {
                    if (!free_sliders) robot_state = 0;
                    cpp_reset();
                }

                { // parameter sliders
                    gui_slider("tetMassDensity", &tetMassDensity, 0.0, 1000.0);
                    gui_slider("tetYoungsModulus", &tetYoungsModulus, 2.0, 8.0, false, true);
                    gui_slider("tetPoissonsRatio", &tetPoissonsRatio, 0.01, 0.49);
                    gui_slider("cable input multiplier", &cable_input_multiplier, 0.5, 3.0);
                }
                /**
                  if (gui_button("update cable reference lengths")) {
                  jonesUpdateCableReferenceLengths();
                  }
                 **/
                if (gui_button("disable / enable cables", 'c')) currentState.enabled__BIT_FIELD ^= CABLES;

                if(free_sliders){
                    for_(j, sim.num_cables) {
                        char buffer[] = "u_?";
                        buffer[2] = char('0' + j);
                        real a = (j < 3) ? ROBOT_SEGMENT_LENGTH : 2 * ROBOT_SEGMENT_LENGTH;
                        gui_slider(buffer, &currentState.u[j], -a, a);
                    }
                }
                else {
                    gui_slider("Kaa State", &robot_state, 0, JOSIE_NUM_CABLE_POSITIONS, 'j', 'k');
                    for_(j, sim.num_cables) {
                        currentState.u[j] = tendonlengths[j][robot_state] * cable_input_multiplier;
                        //gui_printf("u%d: %lf\n", j, tendonlengths[j][robot_state] * cable_input_multiplier);
                    }
                }
            }
            else {
                if (generate_time == 0.0) generate_curr_state = -1;
                generate_has_run = true;
                if (generate_curr_state != (int)(generate_time/0.5)) {
                    generated_positions[(int)(generate_time/0.5)] = get(currentState.x, 9 + NUM_BONES * 10);
                }
                generate_curr_state = (int)(generate_time/0.5);
                gui_printf("Kaa State: %d", generate_curr_state);
                if (generate_curr_state > JOSIE_NUM_CABLE_POSITIONS) {
                    generate_time = 0.0;
                    generating = false;
                }
                else {
                    for_(j, sim.num_cables) {
                        currentState.u[j] = tendonlengths[j][generate_curr_state] * cable_input_multiplier;
                        gui_printf("u%d: %lf\n", j, tendonlengths[j][generate_curr_state] * cable_input_multiplier);
                    }
                    generate_time += 0.0167;
                }
            }

            currentState = sim.getNext(&currentState);

            if (kaa_draw_mode == 2) {
                sim.draw(P * V, &currentState);
                /**
                  int num_cables = cpp_getNumCables();
                  int total_vias = cpp_getTotalNumVias();
                  int* vias_per_cable = new int[num_cables];
                  float* via_positions = new float[total_vias];
                  float* tensions = new float[num_cables];

                  cpp_getNumViasPerCable((void*)vias_per_cable);
                  cpp_getCables(via_positions, tensions);

                  gui_printf("Num Cables: %d", num_cables);
                  gui_printf("Num Vias: %d", total_vias);
                  for (int i = 0; i < num_cables / 3; i++) {
                  gui_printf("%d\t%d\t%d", vias_per_cable[3*i], vias_per_cable[3*i+1], vias_per_cable[3*i+2]);
                  }
                 **/
            }
            else if (kaa_draw_mode == 1) {
                vec3 spine_positions[NUM_BONES+1];
                for_(j, NUM_BONES+1) {
                    spine_positions[j] = get(currentState.x, 9 + j * 10);
                }
                soup_draw(PV, SOUP_LINE_STRIP, NUM_BONES+1, spine_positions, NULL, monokai.blue, 3);
                soup_draw(PV, SOUP_POINTS, NUM_BONES+1, spine_positions, NULL, monokai.white, 10);
            }
            if (generate_has_run) soup_draw(PV, SOUP_LINE_STRIP, generate_curr_state,
                    generated_positions, NULL, monokai.purple, 10);
        }
    }
}

//END CARL

delegate void cpp_dragon_yzoHead (
        void *bones_y,
        void *bones_z,
        void *bones_o) {

    vec3 y = -normalized(get(currentState.x, 9 + (NUM_BONES) * 10) - get(currentState.x, 9 + (NUM_BONES - 1) * 10));
    vec3 up = { 0.0, 1.0, 0.0 }; 
    vec3 x = normalized(cross(y, up));
    vec3 z = cross(x, y);
    vec3 o = get(currentState.x, 9 + (NUM_BONES) * 10);

    float y_floats[3] = {(float)(y.x), (float)(y.y), (float)(y.z)};
    float z_floats[3] = {(float)(z.x), (float)(z.y), (float)(z.z)};
    float o_floats[3] = {(float)(o.x), (float)(o.y), (float)(o.z)};

    for_(i, 3) {
        ((float *) bones_y)[i] = y_floats[i];
        ((float *) bones_z)[i] = z_floats[i];
        ((float *) bones_o)[i] = o_floats[i];
    }
}


delegate UnityGeneralPurposeInt cpp_dragon_getNumVertices(UnityGeneralPurposeInt mesh_index) {
    IndexedTriangleMesh3D mesh = mesh_index ? dragonBody : _dragonHead;
    return mesh.num_vertices;
}

delegate UnityGeneralPurposeInt cpp_dragon_getNumTriangles(UnityGeneralPurposeInt mesh_index) {
    IndexedTriangleMesh3D mesh = mesh_index ? dragonBody : _dragonHead;
    return mesh.num_triangles;
}

delegate UnityGeneralPurposeInt cpp_dragon_getNumBones() { return NUM_BONES; }

delegate void cpp_dragon_getMesh (
        UnityGeneralPurposeInt mesh_index,
        void *vertex_positions,
        void *vertex_normals,
        void *vertex_colors,
        void *triangle_indices) {

    IndexedTriangleMesh3D mesh = mesh_index ? dragonBody : _dragonHead;

    for (int k = 0; k < cpp_dragon_getNumVertices(mesh_index); k++) {
        for_(d, 3) {
            ((UnityVertexAttributeFloat*) vertex_positions)[3 * k + d] = (UnityVertexAttributeFloat)(mesh.vertex_positions[k][d]);
            ((UnityVertexAttributeFloat*)   vertex_normals)[3 * k + d] = (UnityVertexAttributeFloat)(mesh.vertex_normals  [k][d]);
        }
        for_(d, 4) {
            ((UnityVertexAttributeFloat*) vertex_colors)[4 * k + d] = (d == 3) ? (UnityVertexAttributeFloat)(1.0)
                : (UnityVertexAttributeFloat)(mesh.vertex_colors[k][d]);
        }
    }
    for (int k = 0; k < cpp_dragon_getNumTriangles(mesh_index); k++) {
        for_(d, 3) {
            ((UnityTriangleIndexInt*) triangle_indices)[3 * k + d] = (UnityTriangleIndexInt)(mesh.triangle_indices[k][d]);
        }
    }
}
// TODO: GPU picking
// TODO: make line and spheres show up through the transparent mesh as well
// TODO: see if you can run cow while running an app in VR
// TODO: floor
// TODO: revisit the hessian (kim-style SparseMatrix parallel add?)
// TODO: talking to motors
// TODO: linear blend skinning in a vertex shader
//       get space fish back up and running
// TODO: #define in build.bat for gui stuff
// TODO: split IK line search between frames
// TODO: dFdu sparse matrix (why did this fail last time?)
// TODO: should x be a FixedSizeSelfDestructingArray<vec3>?
// // (waiting on josie) real-world trajectory Figure
// play more with sim params
// play more with ik weights
// // visualization
// pipe and sphere widget
// cable slack visualization
// // fun
// ordering juggling clubs
// // cow
// port MIN, MAX, etc. to be functions








// bones


// C++
// - IK

// C#
// - controllers
// - draw
// -- (GPU)
// --- "skinning" - vertex shader


IndexedTriangleMesh3D _dragonHead;
IndexedTriangleMesh3D dragonBody;
delegate void cpp_dragon_initializeBones (
        void *bones_y,
        void *bones_z,
        void *bones_o,
        void *bone_indices,
        void *bone_weights) {

    ASSERT(initialized);

    // TODO: this shouldn't be here
    for(int i = 0; i < NUM_BONES; i++) {
        for_(d, 3) {
            ((UnityVertexAttributeFloat*) bones_y)[3 * i + d] = (UnityVertexAttributeFloat)(currentBones[i](d, 1));
            ((UnityVertexAttributeFloat*) bones_z)[3 * i + d] = (UnityVertexAttributeFloat)(currentBones[i](d, 2));
            ((UnityVertexAttributeFloat*) bones_o)[3 * i + d] = (UnityVertexAttributeFloat)(currentBones[i](d, 3));
        }
    }

    for(int i = 0; i < dragonBody.num_vertices; i++) {
        for_(j, 4) {
            ((UnityGeneralPurposeInt*)    bone_indices)[4 * i + j] = (UnityGeneralPurposeInt)(   dragonBody.bone_indices[i][j]);
            ((UnityVertexAttributeFloat*) bone_weights)[4 * i + j] = (UnityVertexAttributeFloat)(dragonBody.bone_weights[i][j]);
        }
    }
}

if (0) { // set up skinned mesh
    { //CARL load meshes
        char headPath[256];
        char bodyPath[256];
        ASSERT(0);

        // printf("HeadPath: %s", headPath);
        // printf("\n");

        _dragonHead = _meshutil_indexed_triangle_mesh_load(headPath, false, true, false);
        dragonBody = _meshutil_indexed_triangle_mesh_load(bodyPath, false, true, false);
        mat4 RS = M4_RotationAboutXAxis(PI / 2) * M4_Scaling(0.05);
        _dragonHead._applyTransform(RS);
        dragonBody._applyTransform(M4_Translation(0.0, -0.67, 0.0) * RS);
    }

    { // create bones in mesh
        dragonBody.num_bones = DRAGON_NUM_BONES;
        dragonBody.bones = (mat4 *) malloc(DRAGON_NUM_BONES * sizeof(mat4));
        dragonBody.bone_indices = (int4 *) malloc(dragonBody.num_vertices * sizeof(int4));
        dragonBody.bone_weights = (vec4 *) malloc(dragonBody.num_vertices * sizeof(vec4));
    }

    { // set bones rest positions
        for_(j, _COUNT_OF(boneOriginsRest)) {
            boneOriginsRest[j] = get(sim.x_rest, 9 + j * 10);
        }
    }

    { // assign weights FORNOW hacky nonsense
        for_(vertex_i, dragonBody.num_vertices) {
            auto f = [&](int i) {
                real c = AVG(boneOriginsRest[i].y, boneOriginsRest[i + 1].y);
                real D = ABS(dragonBody.vertex_positions[vertex_i].y - c);
                return MAX(0.0, (1.0 / D) - 10.0);
            };

            real t = INVERSE_LERP(dragonBody.vertex_positions[vertex_i].y, 0.0, -ROBOT_LENGTH);
            real b = t * dragonBody.num_bones;

            int j = MIN(MAX(int(b + 0.5), 0), dragonBody.num_bones - 1);
            int i = MAX(0, j - 1);
            int k = MIN(dragonBody.num_bones - 1, j + 1);

            dragonBody.bone_indices[vertex_i] = { i, j, k };
            dragonBody.bone_weights[vertex_i] = { f(i), f(j), f(k) };
            dragonBody.bone_weights[vertex_i] /= sum(dragonBody.bone_weights[vertex_i]);

            jim_sort_against(
                    (int *) &dragonBody.bone_indices[vertex_i],
                    4,
                    sizeof(int),
                    (real *) &dragonBody.bone_weights[vertex_i],
                    true);
            dragonBody.bone_indices[vertex_i] = { 
                dragonBody.bone_indices[vertex_i][3],
                dragonBody.bone_indices[vertex_i][2],
                dragonBody.bone_indices[vertex_i][1],
                dragonBody.bone_indices[vertex_i][0],
            };
            dragonBody.bone_weights[vertex_i] = {
                dragonBody.bone_weights[vertex_i][3],
                dragonBody.bone_weights[vertex_i][2],
                dragonBody.bone_weights[vertex_i][1],
                dragonBody.bone_weights[vertex_i][0],
            };

            ASSERT(dragonBody.bone_weights[vertex_i][0] >= dragonBody.bone_weights[vertex_i][1]);
            ASSERT(dragonBody.bone_weights[vertex_i][1] >= dragonBody.bone_weights[vertex_i][2]);
            ASSERT(dragonBody.bone_weights[vertex_i][2] >= dragonBody.bone_weights[vertex_i][3]);
        }
    }
}

else { // skinning
    dragonBody.bones = currentBones.data;
    dragonBody.draw(P, V, globals.Identity);

    { // _dragonHead
        // FORNOW: hacky, with few dependencies
        vec3 y = -normalized(get(currentState.x, 9 + (NUM_BONES) * 10) - get(currentState.x, 9 + (NUM_BONES - 1) * 10));
        vec3 up = { 0.0, 1.0, 0.0 };
        vec3 x = cross(y, up);
        x = IS_ZERO(squaredNorm(x)) ? V3(1.0, 0.0, 0.0) : normalized(x);
        vec3 z = cross(x, y);
        vec3 o = get(currentState.x, 9 + (NUM_BONES) * 10);
        _dragonHead.draw(P, V, M4_xyzo(x, y, z, o));
    }
}

delegate void cpp_dragon_yzoBones(
        void *bones_y,
        void *bones_z,
        void *bones_o) {

    for(int i = 0; i < NUM_BONES; i++) {
        for_(d, 3) {
            ((UnityVertexAttributeFloat*) bones_y)[3 * i + d] = (UnityVertexAttributeFloat)(currentBones[i](d, 1));
            ((UnityVertexAttributeFloat*) bones_z)[3 * i + d] = (UnityVertexAttributeFloat)(currentBones[i](d, 2));
            ((UnityVertexAttributeFloat*) bones_o)[3 * i + d] = (UnityVertexAttributeFloat)(currentBones[i](d, 3));
        }
    }
}
#endif
