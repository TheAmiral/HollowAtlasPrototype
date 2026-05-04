using UnityEngine;
using UnityEngine.InputSystem;

public class GoldWallet : MonoBehaviour
{
    [Header("Gold")]
    public int runGold;
    public int bankGold;

    [Header("UI")]
    public bool showLegacyOnGUI = false;
    public float uiX = 10f;
    public float uiY = 10f;
    public float uiWidth = 220f;
    public float uiHeight = 74f;

    public int RunGold => runGold;
    public int BankGold => bankGold;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

#if UNITY_EDITOR
        // Debug kısayolları — sadece Editor'da aktif, build'de yok
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
            BankRunGold();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            ResetRunGold();
#endif
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        runGold += amount;
        Debug.Log($"+{amount} gold | Run Gold: {runGold}");
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || runGold < amount)
            return false;
        runGold -= amount;
        return true;
    }

    public void BankRunGold()
    {
        if (runGold <= 0)
            return;

        bankGold += runGold;
        runGold = 0;
    }

    public void ResetRunGold()
    {
        runGold = 0;
    }

    void OnGUI()
    {
        if (!showLegacyOnGUI)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        GUI.Box(new Rect(uiX, uiY, uiWidth, uiHeight), "Gold");
        GUI.Label(new Rect(uiX + 10f, uiY + 20f, uiWidth - 20f, 18f), $"Run Gold: {runGold}");
        GUI.Label(new Rect(uiX + 10f, uiY + 38f, uiWidth - 20f, 18f), $"Bank Gold: {bankGold}");
        GUI.Label(new Rect(uiX + 10f, uiY + 56f, uiWidth - 20f, 18f), "B = Bank | R = Reset");
    }
}
