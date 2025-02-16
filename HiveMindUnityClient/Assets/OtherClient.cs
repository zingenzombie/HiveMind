using UnityEngine;

public class OtherClient : MonoBehaviour
{
    public string playerID;
    public string username = "Unnamed";

    Vector3 newPosition;
    Quaternion newRotation;

    private void Start()
    {
        newPosition = transform.position;
        newPosition = transform.position;
    }

    void Update()
    {

        // Interpolate position
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime*5);
        
        // Interpolate rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * 5);
    }

    public void ChangeTarget(Vector3 newPosition, Quaternion newRotation)
    {
        this.newPosition = newPosition;
        this.newRotation = newRotation;
    }
}
