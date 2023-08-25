using UnityEngine;
public class TargetManager : MonoBehaviour {
    public GameObject nodePrefab;
    public Vector3 startPos;
    public float length;
    public int nextAvalible;
    public int numNodes;
    public GameObject[] targets;

    // Start is called before the first frame update
    void Awake()
    {
        //GenerateNode(startPos);
        Setup();
    }

    public void Setup() {
        nextAvalible = 0;
        numNodes = nodePrefab.transform.childCount;
        targets = new GameObject[numNodes];
        int i = 0;
        foreach(Transform child in nodePrefab.transform){
            targets[i] = child.gameObject;
            child.position = new Vector3(0.0f, -length+length*i/(float)numNodes, 0.0f);
            if(i != nextAvalible) child.gameObject.SetActive(false);
            i++;
        }
        //nextAvalible++;
    }

    public bool isActive (int index) {
        return targets[index].activeSelf;
    }

    public void CreateNode(Vector3 position) {
        targets[nextAvalible].transform.position = position;
        targets[nextAvalible].SetActive(true);
        nextAvalible++;
    }

}
