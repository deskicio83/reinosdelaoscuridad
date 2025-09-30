using UnityEngine;
using UnityEditor;
using TMPro;

public static class LegionAutoWire
{
    [MenuItem("Tools/Reinos/Auto-Wire Legion Grid")]
    public static void Wire()
    {
        var gridCtrl = Object.FindObjectOfType<LegionGridController>();
        if (!gridCtrl) { Debug.LogError("No se encontró LegionGridController en escena."); return; }

        // Content Root
        var content = GameObject.Find("Canvas/GridRoot/HeroScroll/Viewport/Content")?.GetComponent<RectTransform>();
        if (!content) { Debug.LogError("No se encontró Canvas/GridRoot/HeroScroll/Viewport/Content"); return; }
        var so = new SerializedObject(gridCtrl);
        so.FindProperty("contentRoot").objectReferenceValue = content;

        // Buscar plantilla en Content
        var template = content.transform.Find("LegionCardTemplate");
        if (!template)
        {
            Debug.LogError("No existe 'LegionCardTemplate' bajo Content. Crea la tarjeta como en las instrucciones.");
            return;
        }
        var card = template.GetComponent<LegionCardUI>();
        if (!card) { Debug.LogError("LegionCardTemplate no tiene LegionCardUI."); return; }

        so.FindProperty("heroCardTemplate").objectReferenceValue = card;
        so.ApplyModifiedProperties();

        Debug.Log("✅ Auto-Wire completado: contentRoot + heroCardTemplate asignados.");
    }
}
