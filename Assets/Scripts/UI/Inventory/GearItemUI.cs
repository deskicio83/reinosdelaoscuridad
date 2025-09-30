/*
============================================================
GearItemUI.cs — Item de inventario (thumbnail de pieza)
------------------------------------------------------------
PROPÓSITO
- Mostrar icono, nivel (+N), y abrir panel de detalle/preview al pulsar.

REFERENCIAS (INSPECTOR)
- Image iconGear, TMP_Text txtLvl, Button BtnDetail, NewGearStatsPanelUI newGearStatsPanel.

MÉTODOS
- Setup(GearInstance gear, Action onClick):
  - Carga icono (Addressables) según gearId.
  - Muestra +upgradeLevel.
  - onClick del botón:
    · Resuelve slotType con GearUtils.GetSlotTypeFromGearId(...).
    · Llama NewGearStatsPanelUI.Show(gear, gearCat, slotType, heroId actual).
    · Actualiza StatsPreviewPanelController con ShowStatsWithGearPreview(heroId, gear).

NOTAS
- HeroId se obtiene de PanelEquiparController.currentHeroId.
============================================================
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GearItemUI : MonoBehaviour
{
    public Image frame;
    public Image iconGear;
    public TMP_Text txtLvl;
    public Transform starGroup;
    public Button BtnDetail;
    public NewGearStatsPanelUI newGearStatsPanel;

    private GearInstance gear;
    private Action onClick;

    public void Setup(GearInstance gear, Action onClick)
    {
        this.gear = gear;
        this.onClick = onClick;

        if (txtLvl != null)
            txtLvl.text = $"+{gear.upgradeLevel}";

        var gearCat = HeroCatalogManager.Instance.GetGearById(gear.gearId);
        if (iconGear != null && gearCat != null && !string.IsNullOrEmpty(gearCat.spriteAddressable))
        {
            UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(gearCat.spriteAddressable).Completed += (op) =>
            {
                if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    iconGear.sprite = op.Result;
            };
        }

        if (BtnDetail != null)
        {
            BtnDetail.onClick.RemoveAllListeners();
            BtnDetail.onClick.AddListener(() =>
            {
                Debug.Log("[GearItemUI] BtnDetail pulsado para: " + gear?.gearId + " | newGearStatsPanel asignado: " + (newGearStatsPanel != null));
                if (newGearStatsPanel != null)
                {
                    var gearCat2 = HeroCatalogManager.Instance.GetGearById(gear.gearId);

                    // slotType centralizado
                    string slotType = GearUtils.GetSlotTypeFromGearId(gear.gearId);

                    string heroId = PanelEquiparController.currentHeroId;
                    newGearStatsPanel.Show(gear, gearCat2, slotType, heroId);
                }
                else
                {
                    Debug.LogWarning("[GearItemUI] newGearStatsPanel no asignado por código, revisa instanciación.");
                }

                // Preview de stats con la pieza seleccionada
                var statsPanel = FindObjectOfType<StatsPreviewPanelController>();
                string heroId2 = PanelEquiparController.currentHeroId;
                if (statsPanel != null && !string.IsNullOrEmpty(heroId2))
                {
                    statsPanel.ShowStatsWithGearPreview(heroId2, gear);
                    Debug.Log("[GearItemUI] StatsPreviewPanel actualizado con gear: " + gear.gearId + " para héroe: " + heroId2);
                }
                else
                {
                    Debug.LogWarning("[GearItemUI] No se pudo actualizar StatsPreviewPanel. statsPanel=" + (statsPanel != null) + " heroId=" + heroId2);
                }
            });
        }
        else
        {
            Debug.LogWarning("[GearItemUI] BtnDetail NO encontrado o no asignado en prefab.");
        }
    }
}
