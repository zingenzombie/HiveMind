using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{

    private Vector2 moveInput;

    [SerializeField] private float minViewDistance;
    [SerializeField] private float mouseSensitivity;

    [SerializeField] private Transform playerBody;

    private float xRotation = 0;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        xRotation -= moveInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, minViewDistance);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        playerBody.Rotate(Vector3.up * moveInput.x * mouseSensitivity);
    }

    public void OnLook(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
