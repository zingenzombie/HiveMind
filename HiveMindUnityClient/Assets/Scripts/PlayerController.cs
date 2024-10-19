using System;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [SerializeField] float walkSpeed = 10f;
    [SerializeField] PlayerLook mainCamera;
    [SerializeField] float jumpPower = 10f;

    float walkSpeedActual;
    bool sprinting;
    bool jumping;
    Vector2 moveInput;
    Rigidbody myRigidbody;

    void Start()
    {

        walkSpeedActual = walkSpeed;
        myRigidbody = GetComponent<Rigidbody>();
    }

    Vector3 oldPosition;
    Quaternion oldRotation;
    public float timer = .01f;

    void Update()
    {
        Run();
        
        transform.GetPositionAndRotation(out Vector3 newPosition, out Quaternion newRotation);
        timer -= Time.deltaTime;
        //Vector 3 is 12 bytes and Quaternion is 16 bytes.
        if((oldPosition != newPosition || oldRotation != newRotation) && timer < 0)
        {
            timer = .01f;

            oldPosition = newPosition;
            oldRotation = newRotation;
            var eulerRot = newRotation.eulerAngles;

            var x = BitConverter.GetBytes(newPosition.x);
            var y = BitConverter.GetBytes(newPosition.y);
            var z = BitConverter.GetBytes(newPosition.z);

            var rotx = BitConverter.GetBytes(eulerRot.x);
            var roty = BitConverter.GetBytes(eulerRot.y);
            var rotz = BitConverter.GetBytes(eulerRot.z);

            var message = ConcatByteArrays(x, y, z, rotx, roty, rotz);

            NetworkMessage netMessage = new NetworkMessage("UpdateTransform", message);

            NetworkController.SendTCPMessage(netMessage);
        }
    }

    static byte[] ConcatByteArrays(params byte[][] arrays)
    {
        // Calculate the total length of all arrays
        int totalLength = 0;
        foreach (byte[] array in arrays)
        {
            totalLength += array.Length;
        }

        // Create a new byte array to hold all concatenated arrays
        byte[] result = new byte[totalLength];

        // Copy each array into the result array
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }

        return result;
    }

    void Run()
    {

        Vector3 playerVelocity;

        if (myRigidbody.velocity.y < -50)
            playerVelocity = new Vector3(moveInput.x * walkSpeedActual, -50, moveInput.y * walkSpeedActual);
        else
            playerVelocity = new Vector3(moveInput.x * walkSpeedActual, myRigidbody.velocity.y, moveInput.y * walkSpeedActual);
        
        myRigidbody.velocity = transform.TransformDirection(playerVelocity);


    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

    }
    public void OnLook(InputValue value)
    {
        mainCamera.OnLook(value);
    }

    public void OnSprint(InputValue value)
    {
        sprinting = value.isPressed;

        if (sprinting)
            walkSpeedActual = walkSpeed * 2;
        else
            walkSpeedActual = walkSpeed;
    }

    public void OnJump(InputValue value)
    {
        jumping = value.isPressed;

        if (jumping)
        {
            Vector3 playerVelocity = new Vector3(myRigidbody.velocity.x, myRigidbody.velocity.y + jumpPower, myRigidbody.velocity.z);
            myRigidbody.velocity = transform.TransformDirection(playerVelocity);
        }
    }

}