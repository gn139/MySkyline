using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Player : MonoBehaviour {
    private ChunkManager chunkManager;
    [SerializeField] private GameObject model;
    private bool justOne = false;
    private Bounds bounds;
    private float longest;
    private int lastCount;
    // Start is called before the first frame update
    void Start () {
        chunkManager = GetComponent<ChunkManager> ();
        bounds = model.GetComponent<MeshRenderer> ().bounds;
        longest = Mathf.Max (bounds.size.x, Mathf.Max (bounds.size.y, bounds.size.z));
        // Debug.Log (longest);
    }

    // Update is called once per frame
    void Update () {
        // Debug.Log (chunkManager.chunks.Count);
        // Debug.Log (chunkManager.meshQueue.Count);
        // Debug.Log (chunkManager.activeQueue.Count);
        if (chunkManager.meshQueue.Count < 1 && chunkManager.activeQueue.Count < 1 && !justOne) {
            Collider[] hitInfos = Physics.OverlapSphere (transform.position, longest);
            Vector3 direction = Vector3.zero;

            foreach (var hit in hitInfos) {
                direction += transform.position - hit.ClosestPointOnBounds (transform.position);
            }
            model.gameObject.SetActive (true);
            model.transform.SetParent (this.transform);
            transform.position += direction.normalized * longest;
            justOne = true;
        }
    }
}
