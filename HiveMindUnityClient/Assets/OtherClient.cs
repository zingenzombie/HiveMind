using UnityEngine;

public class OtherClient : MonoBehaviour
{
    public string playerID;
    public string username = "Unnamed";

    Transform oldTramsform, newTransform, targetTransform;
    private Vector3 velocity = Vector3.zero;

    float timeDif = Time.time;

    void FixedUpdate()
    {
        oldTramsform = transform;
        targetTransform = newTransform;
        timeDif = Time.time;
    }

    void Update()
    {
        transform.position = Vector3.SmoothDamp(oldTramsform.position, targetTransform.position, ref velocity, (Time.time - timeDif) / Time.fixedDeltaTime);
    }

    public void ChangeTarget(Transform newTransform){
        this.newTransform = newTransform;
    }
}
