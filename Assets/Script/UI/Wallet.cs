using System;
using UnityEngine;

/// <summary>
/// Holds the player's money. Receives the total when a SlotGroup sells.
/// </summary>
public class Wallet : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingMoney = 0;

    public int Money { get; private set; }

    public event Action<int> MoneyChanged;

    private void Awake()
    {
        Money = startingMoney;
    }

    private void Start()
    {
        MoneyChanged?.Invoke(Money);
    }

    public void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Money += amount;
        MoneyChanged?.Invoke(Money);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > Money)
        {
            return false;
        }

        Money -= amount;
        MoneyChanged?.Invoke(Money);
        return true;
    }
}
