using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

public class WeaponSelectManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject weaponSelectPanel;
    public WeaponButton[] weaponButtons;

    [Header("Game Settings")]
    public float reactionTimeLimit = 3f;

    private float weaponSelectStartTime;
    private bool weaponSelectionActive = false;

    public event Action<int> OnWeaponSelected;

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

        DisableAllWeaponButtons();
    }

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
}
