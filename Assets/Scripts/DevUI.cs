using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class DevUI : MonoBehaviour {
    [SerializeField] private Text textX;
    [SerializeField] private Text textY;
    [SerializeField] private Text textZ;
    [SerializeField] private ChunkManager chunkManager;
    // Start is called before the first frame update
    void Start () {

    }

    // Update is called once per frame
    void Update () {
        textX.text = $"meshQueue {chunkManager.meshQueue.Count}";
        textY.text = $"activeQueue {chunkManager.activeQueue.Count}";
        textZ.text = $"textZ {Input.gyro.userAcceleration.z}";
    }
}
