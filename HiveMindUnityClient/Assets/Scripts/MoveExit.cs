using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveExit : MonoBehaviour
{

    public GridController gridController;

    public int x, y;

    public void UpdatePosition(bool up)
    {
        if (up)
            transform.Translate(new Vector3(0, 50f, 0));

        else
            transform.Translate(new Vector3(0, -50f, 0));
    }

    public void MoveExitOnGrid(byte direction)
    {
        gridController.ChangeActiveTile(direction, x, y);
    }
}
