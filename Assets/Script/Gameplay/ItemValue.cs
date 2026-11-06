using UnityEngine;

/// <summary>
/// Gives a SlottableItem a sell value.
/// </summary>
[RequireComponent(typeof(SlottableItem))]
public class ItemValue : MonoBehaviour
{
    [SerializeField, Min(0)] private int value = 1;

    public int Value
    {
        get { return value; }
    }
}
