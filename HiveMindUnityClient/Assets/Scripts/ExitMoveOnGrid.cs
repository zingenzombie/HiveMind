using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitMoveOnGrid : MonoBehaviour
{

    [SerializeField] MoveExit exitEmpty;
    [SerializeField] byte direction;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            exitEmpty.MoveExitOnGrid(direction);
    }

}
