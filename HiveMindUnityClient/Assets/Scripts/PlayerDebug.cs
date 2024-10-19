using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public string username = null;

    private void Start()
    {
        HiveClientEvents.OnEnteredNewTile += updateDebugInfo;
    }

    public void updateDebugInfo(HexTileController hex)
    {
        debugText.text = $"User: {username}\nOn Tile: {hex.x},{hex.y}";
    }
}
