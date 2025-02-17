using System.ComponentModel;
using UnityEngine;

public abstract class ItemObject : ScriptableObject
{
    public GameObject prefab;
    public ToolboxItemFilterType type;
    [TextArea(15, 20)] public string description;
}
