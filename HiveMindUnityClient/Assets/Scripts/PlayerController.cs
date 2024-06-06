using System;
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

    void Update()
    {
        Run();
    }


    float newY;
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