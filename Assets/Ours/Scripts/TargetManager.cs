using UnityEngine;

public class TargetManager : MonoBehaviour {
    public GameObject targetTargetsParentObject;
    public int targetNumTargets;
    public int targetMaxNumberOfTargets;
    public GameObject[] targetGameObjects;


    public void Setup() {
        targetNumTargets = 0;
        targetMaxNumberOfTargets = targetTargetsParentObject.transform.childCount;
        targetGameObjects = new GameObject[targetMaxNumberOfTargets];
        for (int i = 0; i < targetTargetsParentObject.transform.childCount; ++i) {
            targetGameObjects[i] = targetTargetsParentObject.transform.GetChild(i).gameObject;
        }
    }

    public bool isActive (int index) {
        return targetGameObjects[index].activeSelf;
    }

    public void CreateNode(Vector3 position) {
        targetGameObjects[targetNumTargets].transform.position = position;
        targetGameObjects[targetNumTargets].SetActive(true);
        targetNumTargets++;
    }
}
