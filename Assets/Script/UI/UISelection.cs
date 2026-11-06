using System;
using UnityEngine;

/// <summary>
/// Tracks the currently selected UIItem. Static so any script can read it
/// without needing a scene reference.
/// </summary>
public static class UISelection
{
    public static UIItem Current { get; private set; }

    // Fired with (previous, current). Either can be null.
    public static event Action<UIItem, UIItem> Changed;

    public static void Select(UIItem item)
    {
        if (item == Current)
        {
            return;
        }

        UIItem previous = Current;
        Current = item;
        Changed?.Invoke(previous, Current);
    }

    public static void Clear()
    {
        Select(null);
    }

    // Resets static state when Domain Reload is disabled in Enter Play Mode settings.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
        Changed = null;
    }
}
