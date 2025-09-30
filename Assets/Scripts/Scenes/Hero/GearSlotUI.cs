/*
============================================================
GearSlotUI.cs — Slot visual del equipamiento del héroe
------------------------------------------------------------
PROPÓSITO
- Mostrar icono de pieza/placeholder y manejar clic para abrir inventario.

MÉTODOS (COMPLETA AQUÍ)
- Setup(GearInstance gear, string slotType, Action<GearInstance> onClick).
- OnAddGearClicked (Action): para abrir inventario filtrado por slot.
============================================================
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

public class GearSlotUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image iconImage;
    public Image rarityBorder;   // Hijo image para el marco
    public TMP_Text levelText;
    public Button button;
    public Button btnAddGear; // Asigna en el inspector el botón hijo con el icono "+"
    public Action OnAddGearClicked;

    private Action<GearInstance> onClick;
    private GearInstance currentGear;
    private Coroutine fadeCoroutine;

    public void Setup(GearInstance gear, string typeSlot, Action<GearInstance> onClickCallback)
    {
        currentGear = gear;
        onClick = onClickCallback;

        // Botón "+" visible solo si el slot está vacío
        if (btnAddGear != null)
            btnAddGear.gameObject.SetActive(gear == null);

        // Si hay corutina de fade en curso y venimos a pintar un estado vacío, la cortamos
        if (gear == null && fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (gear == null)
        {
            // --- LIMPIEZA DURA DEL SLOT ---
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0);
                iconImage.enabled = false;         // <- clave
            }
            if (rarityBorder != null)
            {
                rarityBorder.sprite = null;
                rarityBorder.color = new Color(1, 1, 1, 0);
                rarityBorder.enabled = false;      // <- clave
            }
            if (levelText != null)
            {
                levelText.text = string.Empty;
                var c = levelText.color;
                levelText.color = new Color(c.r, c.g, c.b, 0f);
                levelText.enabled = false;         // <- clave
            }
        }
        else
        {
            // Hay pieza -> aseguramos todo visible
            if (levelText != null)
            {
                levelText.text = $"{gear.upgradeLevel}";
                var c = levelText.color;
                levelText.color = new Color(c.r, c.g, c.b, 1f);
                levelText.enabled = true;
            }

            if (iconImage != null)
            {
                iconImage.enabled = true;

                string spritePath = null;
                var gearCatalog = HeroCatalogManager.Instance.GetGearById(gear.gearId);
                if (gearCatalog != null)
                    spritePath = gearCatalog.spriteAddressable;

                LoadGearIcon(spritePath);
            }

            // Marco de rareza por Addressables
            if (rarityBorder != null)
            {
                string originalRarity = gear.rarity;
                string safeRarity = gear.rarity.ToLower()
                    .Replace(" ", "_").Replace("í", "i").Replace("é", "e")
                    .Replace("á", "a").Replace("ó", "o").Replace("ú", "u");
                string borderPath = $"Assets/Addressables/Heroes/BorderGear/{safeRarity}.png";
                Debug.Log($"[GearSlotUI] Marco: '{originalRarity}' -> '{safeRarity}' ({borderPath})");

                Addressables.LoadAssetAsync<Sprite>(borderPath).Completed += (op) =>
                {
                    if (rarityBorder == null) return;

                    if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                    {
                        rarityBorder.sprite = op.Result;
                        rarityBorder.color = Color.white;
                        rarityBorder.enabled = true;
                    }
                    else
                    {
                        rarityBorder.sprite = null;
                        rarityBorder.color = new Color(1, 1, 1, 0);
                        rarityBorder.enabled = false;
                    }
                };
            }
        }

        // Clicks
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(gear));
        }
        if (btnAddGear != null)
        {
            btnAddGear.onClick.RemoveAllListeners();
            btnAddGear.onClick.AddListener(() => OnAddGearClicked?.Invoke());
        }
    }



    // No cambia
    private void LoadGearIcon(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath))
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0);
            }
            return;
        }

        Addressables.LoadAssetAsync<Sprite>(spritePath).Completed += (op) =>
        {
            if (iconImage == null)
                return;

            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                iconImage.sprite = op.Result;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0);
            }
        };
    }

    private Sprite GetPlaceholderSprite(string typeSlot)
    {
        return null;
    }

    public void AnimateFadeOutSlot(float duration = 0.35f)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[GearSlotUI] No se puede lanzar FadeOutRoutine porque el slot '{gameObject.name}' está inactivo.");
            return;
        }
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        // Referencias seguras
        var border = rarityBorder;
        var icon = iconImage;
        var lvl = levelText;

        float t = 0;
        Color borderStart = border != null ? border.color : Color.clear;
        Color iconStart = icon != null ? icon.color : Color.clear;
        Color lvlStart = lvl != null ? lvl.color : Color.clear;

        while (t < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / duration);
            if (border != null) border.color = new Color(borderStart.r, borderStart.g, borderStart.b, alpha);
            if (icon != null) icon.color = new Color(iconStart.r, iconStart.g, iconStart.b, alpha);
            if (lvl != null) lvl.color = new Color(lvlStart.r, lvlStart.g, lvlStart.b, alpha);
            t += Time.deltaTime;
            yield return null;
        }

        // Al terminar, asegúrate que queda todo invisible
        if (border != null)
        {
            border.color = new Color(borderStart.r, borderStart.g, borderStart.b, 0f);
            border.enabled = false;
        }
        if (icon != null)
        {
            icon.color = new Color(iconStart.r, iconStart.g, iconStart.b, 0f);
            icon.sprite = null;
        }
        if (lvl != null)
            lvl.color = new Color(lvlStart.r, lvlStart.g, lvlStart.b, 0f);

        // Limpieza final (esto es lo mismo que haría Setup(null,...))
        levelText.text = "";
    }
}