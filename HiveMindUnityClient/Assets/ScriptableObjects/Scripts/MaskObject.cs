using UnityEngine;

public class MaskObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Icon");
        GetComponent<MeshRenderer>().material.renderQueue = 3002;
    }
}
