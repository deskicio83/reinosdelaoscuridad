// Assets/Editor/CreateDetailStaticChildren.cs
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateDetailStaticChildren
{
    [MenuItem("Tools/Legion/Create static children for Detail Panel")]
    private static void CreateForSelected()
    {
        var panel = Object.FindFirstObjectByType<LegionDetailPanelUI>();
        if (!panel)
        {
            EditorUtility.DisplayDialog("Legion", "No se encontró un LegionDetailPanelUI en la escena.", "Ok");
            return;
        }

        var root = panel.gameObject.transform.Find("Content/Right/Scroll/Viewport/Stack");
        if (!root)
        {
            EditorUtility.DisplayDialog("Legion", "No se encontró Content/Right/Scroll/Viewport/Stack.", "Ok");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Create Detail Static Children");

        // --- Secciones (asegurar que existen)
        var secStats  = Ensure(root, "SectionStats");
        var secSkills = Ensure(root, "SectionSkills");
        var secLore   = Ensure(root, "SectionLore");

        // ========== STATS ==========
        var statsGrid = Ensure(secStats, "StatsGrid");
        // borra todo lo que no sea UI Layout/Mask accidental
        for (int i = statsGrid.childCount - 1; i >= 0; --i)
            Object.DestroyImmediate(statsGrid.GetChild(i).gameObject);

        string[] order = { "HP","ATK","DEF","SPD","TCRI","DCRI","ACC","RES","LUK","AGI" };
        var statRows = new LegionDetailPanelUI.StatRowRef[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            var row = new GameObject($"StatRow_{i}_{order[i]}", typeof(RectTransform));
            row.transform.SetParent(statsGrid, false);
            var rt = (RectTransform)row.transform;
            rt.sizeDelta = new Vector2(400, 36);

            var lblGO = new GameObject("StatLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(row.transform, false);
            var lblRT = (RectTransform)lblGO.transform; lblRT.anchorMin = new Vector2(0, .5f); lblRT.anchorMax = new Vector2(0, .5f); lblRT.pivot = new Vector2(0, .5f);
            lblRT.anchoredPosition = new Vector2(0, 0); lblRT.sizeDelta = new Vector2(160, 36);
            var lbl = lblGO.GetComponent<TextMeshProUGUI>();
            lbl.text = order[i]; lbl.enableWordWrapping = false;

            var valGO = new GameObject("StatValue", typeof(RectTransform), typeof(TextMeshProUGUI));
            valGO.transform.SetParent(row.transform, false);
            var valRT = (RectTransform)valGO.transform; valRT.anchorMin = new Vector2(1, .5f); valRT.anchorMax = new Vector2(1, .5f); valRT.pivot = new Vector2(1, .5f);
            valRT.anchoredPosition = new Vector2(0, 0); valRT.sizeDelta = new Vector2(120, 36);
            var val = valGO.GetComponent<TextMeshProUGUI>();
            val.alignment = TextAlignmentOptions.Right; val.enableWordWrapping = false; val.text = "-";

            statRows[i] = new LegionDetailPanelUI.StatRowRef { go = row, statLabel = lbl, statValue = val };
        }

        // ========== SKILLS ==========
        var skillsRoot = Ensure(secSkills, "SkillsRoot");
        for (int i = skillsRoot.childCount - 1; i >= 0; --i)
            Object.DestroyImmediate(skillsRoot.GetChild(i).gameObject);

        var skillItems = new LegionDetailPanelUI.SkillItemRef[3];
        for (int i = 0; i < 3; i++)
        {
            var item = new GameObject($"SkillItem_{i}", typeof(RectTransform));
            item.transform.SetParent(skillsRoot, false);
            var rt = (RectTransform)item.transform; rt.sizeDelta = new Vector2(500, 100);

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(item.transform, false);
            var iconRT = (RectTransform)iconGO.transform; iconRT.anchorMin = new Vector2(0, .5f); iconRT.anchorMax = new Vector2(0, .5f); iconRT.pivot = new Vector2(0, .5f);
            iconRT.anchoredPosition = new Vector2(0, 0); iconRT.sizeDelta = new Vector2(64, 64);

            var nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(item.transform, false);
            var nameRT = (RectTransform)nameGO.transform; nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(0, 1); nameRT.pivot = new Vector2(0, 1);
            nameRT.anchoredPosition = new Vector2(80, -6); nameRT.sizeDelta = new Vector2(420, 30);
            var name = nameGO.GetComponent<TextMeshProUGUI>(); name.text = "Nombre skill";

            var descGO = new GameObject("ShortDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGO.transform.SetParent(item.transform, false);
            var descRT = (RectTransform)descGO.transform; descRT.anchorMin = new Vector2(0, 0); descRT.anchorMax = new Vector2(0, 0); descRT.pivot = new Vector2(0, 0);
            descRT.anchoredPosition = new Vector2(80, 4); descRT.sizeDelta = new Vector2(420, 58);
            var desc = descGO.GetComponent<TextMeshProUGUI>(); desc.text = "Descripción corta…"; desc.enableWordWrapping = true; desc.overflowMode = TextOverflowModes.Truncate;

            skillItems[i] = new LegionDetailPanelUI.SkillItemRef { go = item, icon = iconGO.GetComponent<Image>(), name = name, shortDesc = desc };
        }

        // ========== LORE ==========
        var loreTxt = secLore.Find("TxtLore");
        if (!loreTxt)
        {
            var go = new GameObject("TxtLore", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(secLore, false);
            var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(520, 300);
            var t = go.GetComponent<TextMeshProUGUI>(); t.text = "Lore…"; t.enableWordWrapping = true;
        }

        // ========= Auto-bind en el componente =========
        Undo.RecordObject(panel, "Bind Detail Panel Arrays");
        panel.staticStatRows  = statRows;
        panel.staticSkillItems = skillItems;
        EditorUtility.SetDirty(panel);

        EditorUtility.DisplayDialog("Legion", "Estructura creada y referencias asignadas.\nAhora puedes reposicionar libremente cada fila/skill desde la GUI.", "Ok");
    }

    private static Transform Ensure(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (!t)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        return t;
    }
}
