using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [SerializeField] float walkSpeed = 10f;
    [SerializeField] float horizontalAcceleration = 1f;
    [SerializeField] float maxRunSpeed = 20f;
    [SerializeField] float jumpPower = 10f;
    [SerializeField] float gravity = Physics.gravity.y;
    [SerializeField] PlayerLook mainCamera;

    NetworkController networkController;

    float walkSpeedActual;
    float fallSpeedActual;
    bool sprinting;
    Vector2 moveInput;
    Rigidbody myRigidbody;

    bool flying = false;
    float delayBetweenPresses = 0.25f;
    bool pressedFirstTime = false;
    float lastPressedTime;
    bool justStoppedFlying = false;
    bool jumpHeld = false;
    private LayerMask groundLayer;


    private void Awake()
    {
        groundLayer = LayerMask.NameToLayer("Default");
    }

    void Start()
    {
        objectManager = GameObject.FindWithTag("ObjectController").GetComponent<ObjectManager>();

        networkController = GameObject.FindWithTag("NetworkController").GetComponent<NetworkController>();

        walkSpeedActual = walkSpeed;

        Physics.gravity = new Vector3(Physics.gravity.x, gravity, Physics.gravity.z);
        fallSpeedActual = jumpPower;

        if (maxRunSpeed < walkSpeed)
            maxRunSpeed = walkSpeed;

        myRigidbody = GetComponent<Rigidbody>();
    }

    Vector3 oldPosition;
    Quaternion oldRotation;

    bool sendThisFixedUpdate = true;

    void Update()
    {
        UpdateSprintSpeed();
        
        if (OpenMenu.menuIsOpen)
        {
            moveInput = Vector2.zero;
            myRigidbody.linearVelocity = new Vector3(0, myRigidbody.linearVelocity.y, moveInput.y * fallSpeedActual);
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

        if (onGround()) {
            playerVelocity = new Vector3(moveInput.x * walkSpeedActual, 0, moveInput.y * walkSpeedActual);
        }
        else if (!flying)
        {
            playerVelocity = new Vector3(moveInput.x * walkSpeedActual, myRigidbody.linearVelocity.y, moveInput.y * walkSpeedActual);
        }
        else
        {
            playerVelocity = new Vector3(moveInput.x * walkSpeedActual, 0, moveInput.y * walkSpeedActual);

            if (OpenMenu.menuIsOpen) 
            {
                playerVelocity.y = 0;
            }
            else if (jumpHeld)
            {
                playerVelocity.y = jumpPower / 2;
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                playerVelocity.y = -jumpPower / 2;
            }
        }
            
        myRigidbody.linearVelocity = transform.TransformDirection(playerVelocity);
    }

    void SendPositionMessage()
    {
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
        if (!OpenMenu.menuIsOpen)
            moveInput = value.Get<Vector2>();

    }
    public void OnLook(InputValue value)
    {
        if (!OpenMenu.menuIsOpen)
            mainCamera.OnLook(value);
    }

    void UpdateSprintSpeed()
    {
        if (sprinting)
        {
            walkSpeedActual += horizontalAcceleration; 
            walkSpeedActual = Mathf.Min(walkSpeedActual, maxRunSpeed); 
        }
        else
        {
            walkSpeedActual -= horizontalAcceleration;
            walkSpeedActual = Mathf.Max(walkSpeedActual, walkSpeed); 
        }
    }

    public void OnSprint(InputValue value)
    {
        if (!OpenMenu.menuIsOpen)
            sprinting = value.isPressed;
    }

    ObjectManager objectManager;
    [SerializeField] GameObject objectToSend;

    public void OnSpawnItem(InputValue value)
    {
        Debug.Log("1 pressed");

        networkController.SendTCPMessage(new NetworkMessage("SpawnObject", ASCIIEncoding.ASCII.GetBytes(objectManager.DecomposeObject(Instantiate(objectToSend, transform.position, transform.rotation, networkController.GetActiveServerTransform())))));
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            if (pressedFirstTime)
            {
                bool isDoublePress = Time.time - lastPressedTime <= delayBetweenPresses;
  
                if (isDoublePress)
                {
                    pressedFirstTime = false;
                    flying = !flying;
                    myRigidbody.useGravity = !flying;
                    justStoppedFlying = !flying;
                }
            }
            else
            {
                pressedFirstTime = true;
            }

            if (!OpenMenu.menuIsOpen)
            {
                if (!flying)
                {
                    if (!justStoppedFlying && myRigidbody.linearVelocity.y == 0)
                    {
                        Vector3 jumpVelocity = myRigidbody.linearVelocity;
                        jumpVelocity.y = jumpPower;
                        myRigidbody.linearVelocity = jumpVelocity;
                    }
                }
                else 
                {
                    jumpHeld = true;
                }
            }
        }
        else
        {
            jumpHeld = false;
        }

        lastPressedTime = Time.time;
        if (pressedFirstTime && Time.time - lastPressedTime > delayBetweenPresses) 
        {
            pressedFirstTime = false;
        }

        justStoppedFlying = false;
    }

    private bool onGround()
    {
        return Physics.Raycast(transform.position, Vector3.down, 0.1f, groundLayer);
    }
}