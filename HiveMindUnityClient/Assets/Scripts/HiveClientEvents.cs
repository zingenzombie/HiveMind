using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HiveClientEvents
{
    public delegate void RecievedMessageFromTile(string message);
    public static event RecievedMessageFromTile OnRecievedMessageFromTile;

    public delegate void EnteredNewTile(HexTileController tile);
    public static event EnteredNewTile OnEnteredNewTile;


    public static void callTileChange(HexTileController tile)
    {
        OnEnteredNewTile?.Invoke(tile);
    }

    public static void callRecievedMessage(string message)
    {
        OnRecievedMessageFromTile?.Invoke(message);
    }
}
