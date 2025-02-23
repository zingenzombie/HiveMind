using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public string username = null;

    public void updateDebugInfo(GameObject currentTile)
    {
        debugText.text = $"User: {username}\nOn Tile: {currentTile}";
    }
}
