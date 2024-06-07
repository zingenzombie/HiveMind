using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitUpAndDown : MonoBehaviour
{

    [SerializeField] MoveExit exitEmpty;
    [SerializeField] bool up;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            exitEmpty.UpdatePosition(up);
    }

}
