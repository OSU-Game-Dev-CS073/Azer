using UnityEngine;
using System;

public class CurrencyController : MonoBehaviour
{
    private static CurrencyController _instance;
    public static CurrencyController Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                // Try to find existing instance
                _instance = FindObjectOfType<CurrencyController>();
                
                // If none exists, create a new one
                if (_instance == null)
                {
                    GameObject go = new GameObject("CurrencyController");
                    _instance = go.AddComponent<CurrencyController>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[CurrencyController] Auto-created instance!");
                }
            }
            return _instance;
        }
    }
    
    [SerializeField] private int startingCurrency = 1000;
    private int playerCurrency;
    public event Action<int> OnCurrencyChanged;

    private void Awake()
    {
        // Singleton pattern - destroy duplicates
        if (_instance != null && _instance != this)
        {
            Debug.Log("[CurrencyController] Duplicate destroyed");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        playerCurrency = startingCurrency;
        DontDestroyOnLoad(gameObject);
        
        // Sync with GameManager on start
        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerMoney(playerCurrency);
        
        OnCurrencyChanged?.Invoke(playerCurrency);
        Debug.Log($"[CurrencyController] Initialized with {playerCurrency} currency");
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