using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [SerializeField] float walkSpeed = 10f;
    [SerializeField] PlayerLook mainCamera;
    [SerializeField] float jumpPower = 10f;

    NetworkController networkController;

    float walkSpeedActual;
    bool sprinting;
    bool jumping;
    Vector2 moveInput;
    Rigidbody myRigidbody;

    void Start()
    {
        networkController = GameObject.FindWithTag("NetworkController").GetComponent<NetworkController>();

        walkSpeedActual = walkSpeed;
        myRigidbody = GetComponent<Rigidbody>();
    }

    Vector3 oldPosition;
    Quaternion oldRotation;

    bool sendThisFixedUpdate = true;

    void FixedUpdate(){
        if (UIOpenMenu.menuIsOpen)
        {
            moveInput = Vector2.zero;
            myRigidbody.linearVelocity = new Vector3(0, myRigidbody.linearVelocity.y, moveInput.y * walkSpeedActual);
            return;
        }

        VelocityCap();
        
        //Do position check every other fixed update.

        if(sendThisFixedUpdate)
            SendPositionMessage();

        sendThisFixedUpdate = !sendThisFixedUpdate;
    }
    
    void VelocityCap()
    {
        Vector3 playerVelocity;

        if (myRigidbody.linearVelocity.y < -50)
            playerVelocity = new Vector3(moveInput.x * walkSpeedActual, -50, moveInput.y * walkSpeedActual);
        else
            playerVelocity = new Vector3(moveInput.x * walkSpeedActual, myRigidbody.linearVelocity.y, moveInput.y * walkSpeedActual);
        
        myRigidbody.linearVelocity = transform.TransformDirection(playerVelocity);
    }

    void SendPositionMessage(){
        transform.GetPositionAndRotation(out Vector3 newPosition, out Quaternion newRotation);
        //Vector 3 is 12 bytes and Quaternion is 16 bytes.
        if(oldPosition != newPosition || oldRotation != newRotation)
        {

            oldPosition = newPosition;
            oldRotation = newRotation;

            byte[] posX = BitConverter.GetBytes(newPosition.x);
            byte[] posY = BitConverter.GetBytes(newPosition.y);
            byte[] posZ = BitConverter.GetBytes(newPosition.z);

            byte[] rotX = BitConverter.GetBytes(newRotation.x);
            byte[] rotY = BitConverter.GetBytes(newRotation.y);
            byte[] rotZ = BitConverter.GetBytes(newRotation.z);
            byte[] rotW = BitConverter.GetBytes(newRotation.w);

            byte[] message = new byte[28];

            for(int i = 0; i < 28; i++)
            {
                if (i < 4)
                    message[i] = posX[i];
                else if (i < 8)
                    message[i] = posY[i - 4];
                else if (i < 12)
                    message[i] = posZ[i - 8];
                else if (i < 16)
                    message[i] = rotX[i - 12];
                else if (i < 20)
                    message[i] = rotY[i - 16];
                else if (i < 24)
                    message[i] = rotZ[i - 20];
                else
                    message[i] = rotW[i - 24];
            }

            NetworkMessage netMessage = new NetworkMessage("PlayerPos", message);

            networkController.SendTCPMessage(netMessage);

        }
    }

    public void OnMove(InputValue value)
    {
        if (!UIOpenMenu.menuIsOpen)
            moveInput = value.Get<Vector2>();

    }
    public void OnLook(InputValue value)
    {
        if (!UIOpenMenu.menuIsOpen)
            mainCamera.OnLook(value);
    }

    public void OnSprint(InputValue value)
    {
        if (!UIOpenMenu.menuIsOpen)
            sprinting = value.isPressed;

        if (sprinting)
            walkSpeedActual = walkSpeed * 2;
        else
            walkSpeedActual = walkSpeed;
    }

    public void OnJump(InputValue value)
    {
        if (!UIOpenMenu.menuIsOpen)
            jumping = value.isPressed;

        if (jumping)
        {
            Vector3 playerVelocity = new Vector3(myRigidbody.linearVelocity.x, myRigidbody.linearVelocity.y + jumpPower, myRigidbody.linearVelocity.z);
            myRigidbody.linearVelocity = transform.TransformDirection(playerVelocity);
        }
    }

}