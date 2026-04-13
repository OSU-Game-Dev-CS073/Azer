using UnityEngine;
using System;

public class CurrencyController : MonoBehaviour
{
    public static CurrencyController Instance;
    [SerializeField] private int startingCurrency = 1000;
    private int playerCurrency;
    public event Action<int> OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            playerCurrency = startingCurrency;
            DontDestroyOnLoad(gameObject);
            
            // Sync with GameManager on start
            if (GameManager.Instance != null)
                GameManager.Instance.SetPlayerMoney(playerCurrency);
            
            OnCurrencyChanged?.Invoke(playerCurrency);
            Debug.Log($"[CurrencyController] Initialized with {playerCurrency} currency");
        }
    }

    void Start()
    {
        OnCurrencyChanged?.Invoke(playerCurrency);
    }

    public int GetCurrency() => playerCurrency;
    
    public bool TrySpendCurrency(int amount)
    {
        if (playerCurrency >= amount)
        {
            playerCurrency -= amount;
            OnCurrencyChanged?.Invoke(playerCurrency);
            // Sync with GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.SetPlayerMoney(playerCurrency);
            return true;
        }
        Debug.LogWarning("Not enough currency!");
        return false;
    }
    
    public void AddCurrency(int amount)
    {
        playerCurrency += amount;
        OnCurrencyChanged?.Invoke(playerCurrency);
        // Sync with GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerMoney(playerCurrency);
        Debug.Log($"[CurrencyController] Added {amount}, new total: {playerCurrency}");
    }
    
    public void SetCurrency(int amount)
    {
        playerCurrency = amount;
        OnCurrencyChanged?.Invoke(playerCurrency);
        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerMoney(playerCurrency);
    }

    public int GetGold() => playerCurrency;
    public bool SpendGold(int amount) => TrySpendCurrency(amount);
    public void AddGold(int amount) => AddCurrency(amount);
}