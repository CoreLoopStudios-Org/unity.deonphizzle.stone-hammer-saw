using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

public class WeaponSelectManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject weaponSelectPanel;
    public WeaponButton[] weaponButtons;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    [Header("Game Settings")]
    public float reactionTimeLimit = 3f;

    [Header("Audio")]
    [Tooltip("Optional: Sound to play when timer is running out")]
    public AudioClip tickingSound;

    private float weaponSelectStartTime;
    private bool weaponSelectionActive = false;
    private float remainingTime = 0f;

    public event Action<int> OnWeaponSelected;
    public event Action OnTimeOut;

    [System.Serializable]
    public class WeaponButton
    {
        public GameObject buttonObject;
        public int weaponId;
        public string weaponName;
    }

    private void Start()
    {
        InitializeWeaponButtons();
    }

    private void Update()
    {
        if (weaponSelectionActive)
        {
            remainingTime = reactionTimeLimit - (Time.time - weaponSelectStartTime);

            // Update timer display
            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(remainingTime).ToString();
                // Pulse effect when time is low
                if (remainingTime <= 1f)
                {
                    timerText.transform.localScale = Vector3.one * (1f + (Mathf.PingPong(Time.time * 10f, 0.5f)));
                }
            }

            // Check timeout
            if (remainingTime <= 0f)
            {
                OnWeaponSelectionTimeout();
            }
        }
    }

    private void OnWeaponSelectionTimeout()
    {
        weaponSelectionActive = false;
        remainingTime = 0f;

        Debug.Log("Weapon selection timeout! Player eliminated.");

        if (statusText != null)
        {
            statusText.text = "TIME UP! Eliminated!";
        }

        DisableAllWeaponButtons();

        // Notify listeners
        OnTimeOut?.Invoke();

        // Notify game manager
        if (G3GameManager.Instance != null)
        {
            G3GameManager.Instance.OnPlayerEliminated();
        }
    }

    private void InitializeWeaponButtons()
    {
        foreach (var weaponBtn in weaponButtons)
        {
            if (weaponBtn.buttonObject != null)
            {
                weaponBtn.buttonObject.SetActive(false);
            }
        }
    }

    public void StartWeaponSelection()
    {
        weaponSelectionActive = true;
        weaponSelectStartTime = Time.time;

        if (weaponSelectPanel != null)
        {
            weaponSelectPanel.SetActive(true);
        }

        ShowWeaponsWithAnimation();
    }

    private void ShowWeaponsWithAnimation()
    {
        foreach (var weaponBtn in weaponButtons)
        {
            if (weaponBtn.buttonObject != null)
            {
                weaponBtn.buttonObject.SetActive(true);
                weaponBtn.buttonObject.transform.localScale = Vector3.zero;
                weaponBtn.buttonObject.transform.DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(UnityEngine.Random.Range(0f, 0.2f));
            }
        }
    }

    public void SelectWeapon(int weaponId)
    {
        if (!weaponSelectionActive) return;

        weaponSelectionActive = false;
        OnWeaponSelected?.Invoke(weaponId);

        float reactionTime = Time.time - weaponSelectStartTime;
        Debug.Log($"Weapon {weaponId} selected! Reaction time: {reactionTime:F2}s");

        if (statusText != null)
        {
            statusText.text = $"Weapon {weaponId} selected!";
        }

        DisableAllWeaponButtons();

        // Notify game manager of successful selection
        if (G3GameManager.Instance != null)
        {
            G3GameManager.Instance.OnPlayerSelectedWeapon(weaponId);
        }
    }

    // Helper methods for Unity UI button onClick events
    public void SelectWeapon0() => SelectWeapon(0);
    public void SelectWeapon1() => SelectWeapon(1);
    public void SelectWeapon2() => SelectWeapon(2);
    public void SelectWeapon3() => SelectWeapon(3);
    public void SelectWeapon4() => SelectWeapon(4);

    private void DisableAllWeaponButtons()
    {
        foreach (var weaponBtn in weaponButtons)
        {
            if (weaponBtn.buttonObject != null)
            {
                weaponBtn.buttonObject.SetActive(false);
            }
        }
    }

    public void HideWeaponSelection()
    {
        weaponSelectionActive = false;
        if (weaponSelectPanel != null)
        {
            weaponSelectPanel.SetActive(false);
        }
    }

    // Test method for editor testing
    [ContextMenu("Test: Start Weapon Selection")]
    public void TestStartWeaponSelection()
    {
        StartWeaponSelection();
    }

    [ContextMenu("Test: Select Random Weapon")]
    public void TestSelectRandomWeapon()
    {
        if (weaponButtons != null && weaponButtons.Length > 0)
        {
            int randomWeapon = weaponButtons[UnityEngine.Random.Range(0, weaponButtons.Length)].weaponId;
            SelectWeapon(randomWeapon);
        }
    }

    [ContextMenu("Test: Force Timeout")]
    public void TestForceTimeout()
    {
        weaponSelectionActive = true;
        weaponSelectStartTime = Time.time - reactionTimeLimit - 1f; // Set time in the past
    }
}
