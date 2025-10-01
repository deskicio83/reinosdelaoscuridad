// Assets/Scripts/Scenes/Legion/LegionDetailPanelUI.cs
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.EventSystems;


public class LegionDetailPanelUI : MonoBehaviour
{
    [Header("Raiz")]
    [SerializeField] private GameObject rootPanel;                // LegionDetailPanel

    [Header("Header")]
    [SerializeField] private TMP_Text txtTitle;                   // Header/TxtTitle
    [SerializeField] private Button btnVolver;                    // Header/BtnVolver
    [SerializeField] private Image imgElement;                    // Header/ImgElement

    [Header("Left / Full")]
    [SerializeField] private RectTransform fullImageRoot;         // Content/Left/FullImageRoot
    [SerializeField] private Image fullImage;                     // Content/Left/FullImageRoot/FullImage
    [SerializeField] private TMP_Text fullName;                   // Content/Left/FullImageRoot/FullName
    [SerializeField] private TMP_Text FullSubClass;
    [SerializeField] private TMP_Text LadoTxt;

    [Header("Stats (grid)")]
    [SerializeField] private Transform statsGrid;                 // SectionStats/StatsGrid
    [SerializeField] private GameObject statRowTemplate;          // SectionStats/StatsGrid/StatRowTemplate
    // Hijos del template (se buscan por nombre)
    private TMP_Text _statLabelTemplate;
    private TMP_Text _statValueTemplate;

    [Header("Skills (lista)")]
    [SerializeField] private Transform skillsRoot;                // SectionSkills/SkillsRoot
    [SerializeField] private GameObject skillItemTemplate;        // SectionSkills/SkillsRoot/SkillItemTemplate
    // Hijos del template (se buscan por nombre)
    private Image _skillIconTpl;
    private TMP_Text _skillNameTpl;
    private TMP_Text _skillShortTpl;
    // --- Tooltip de Skill (panel compartido) ---
    [Header("Skills Tooltip")]
    [SerializeField] private RectTransform skillTooltip;       // Content/Right/SectionSkills/SkillTooltip (opcional, si no existe lo creo por cÃ³digo)
    [SerializeField] private TMP_Text skillTooltipName;   // hijo "Name"
    [SerializeField] private TMP_Text skillTooltipDesc;   // hijo "ShortDesc"
    [SerializeField] private Vector2 skillTooltipOffset = new Vector2(24f, -28f);
    

    [Header("Lore")]
    [SerializeField] private TMP_Text txtLore;                    // SectionLore/TxtLore
    // --- NUEVO: Left / Avatar (retrato pequeÃ±o opcional) ---
    [Header("Left / Avatar")]
    [SerializeField] private RectTransform avatarImageRoot;   // Content/Left/AvatarImageRoot
    [SerializeField] private Image avatarImage;               // Content/Left/AvatarImageRoot/FullImage

    // --- NUEVO: Left / Full - extras de clase ---
    [Header("Left / Full - extras")]
    [SerializeField] private TMP_Text fullClass;              // Content/Left/FullImageRoot/FullClass
    [SerializeField] private TMP_Text fullSubClass;           // Content/Left/FullImageRoot/FullSubClass

    // --- NUEVO: Right / Info ---
    [Header("Right / Info")]
    [SerializeField] private TMP_Text faccionName;            // Content/Right/SectionInfo/FaccionName
    [SerializeField] private TMP_Text ladoTxt;                // Content/Right/SectionInfo/LadoTxt
    [SerializeField] private Image faccionIcon;               // Content/Right/SectionInfo/FaccionIcon (si existe)
    [SerializeField] private Button awakenBtn;                // Content/Right/SectionInfo/AwakenBtn
    [SerializeField] private Image awakenBtnImage;            // Image del botÃ³n (si no estÃ¡ en el mismo GO, se autowirea)



    private CanvasGroup _cg;

    // ---------------- Estado ----------------
    private HeroCatalogEntry _def;
    // ======== MODO ESTÃTICO (sin instanciaciÃ³n) ========
    // Filas de stats ya existentes en la GUI
    [System.Serializable]
    public class StatRowRef
    {
        public GameObject go;
        public TMP_Text statLabel;
        public TMP_Text statValue;
    }
    [SerializeField] public StatRowRef[] staticStatRows = new StatRowRef[0];

    // Items de skills ya existentes en la GUI
    [System.Serializable]
    public class SkillItemRef
    {
        public GameObject go;
        public Image icon;
        public TMP_Text name;
        public TMP_Text shortDesc;
    }
    [SerializeField] public SkillItemRef[] staticSkillItems = new SkillItemRef[0];
    private void Awake()
    {
        // Botón volver
        if (btnVolver)
        {
            btnVolver.onClick.RemoveAllListeners();
            btnVolver.onClick.AddListener(Hide);
        }

        // -------- Autowire opcional (por si no rellenaste en el inspector) --------
        Transform rootT = (rootPanel != null ? rootPanel.transform : transform);

        // Stats (grid)
        if (!statsGrid)
            statsGrid = rootT.Find("Content/Right/SectionStats/StatsGrid");
        if (!statRowTemplate)
        {
            var t = rootT.Find("Content/Right/SectionStats/StatsGrid/StatRowTemplate");
            if (t) statRowTemplate = t.gameObject;
        }

        // Skills (lista)
        if (!skillsRoot)
            skillsRoot = rootT.Find("Content/Right/SectionSkills/SkillsRoot");
        if (!skillItemTemplate)
        {
            var t = rootT.Find("Content/Right/SectionSkills/SkillsRoot/SkillItemTemplate");
            if (t) skillItemTemplate = t.gameObject;
        }

        // Cache de sub-referencias de templates (si existen)
        if (statRowTemplate)
        {
            _statLabelTemplate = statRowTemplate.transform.Find("StatLabel")?.GetComponent<TMPro.TMP_Text>();
            _statValueTemplate = statRowTemplate.transform.Find("StatValue")?.GetComponent<TMPro.TMP_Text>();
            statRowTemplate.SetActive(false);
        }
        if (skillItemTemplate)
        {
            _skillIconTpl  = skillItemTemplate.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
            _skillNameTpl  = skillItemTemplate.transform.Find("Name")?.GetComponent<TMPro.TMP_Text>();
            _skillShortTpl = skillItemTemplate.transform.Find("ShortDesc")?.GetComponent<TMPro.TMP_Text>();
            skillItemTemplate.SetActive(false);
        }

        // Extras
        if (!avatarImageRoot) avatarImageRoot = rootT.Find("Content/Left/AvatarImageRoot") as RectTransform;
        if (!avatarImage)
        {
            var t = rootT.Find("Content/Left/FullImageRoot/AvatarImage");
            if (t) avatarImage = t.GetComponent<Image>();
        }
        if (!fullClass)    fullClass    = rootT.Find("Content/Left/FullImageRoot/FullClass")?.GetComponent<TMP_Text>();
        if (!fullSubClass) fullSubClass = rootT.Find("Content/Left/FullImageRoot/FullSubClass")?.GetComponent<TMP_Text>();
        if (!faccionName)  faccionName  = rootT.Find("Content/Right/SectionInfo/FaccionName")?.GetComponent<TMP_Text>();
        if (!ladoTxt)      ladoTxt      = rootT.Find("Content/Right/SectionInfo/LadoTxt")?.GetComponent<TMP_Text>();
        if (!faccionIcon)
        {
            faccionIcon = rootT.Find("Content/Right/SectionInfo/FaccionIcon")?.GetComponent<Image>()
                    ?? rootT.Find("Content/Right/SectionInfo/FaccionImage")?.GetComponent<Image>();
        }
        if (!awakenBtn)       awakenBtn       = rootT.Find("Content/Right/SectionInfo/AwakenBtn")?.GetComponent<Button>();
        if (!awakenBtnImage)  awakenBtnImage  = awakenBtn ? (awakenBtn.GetComponent<Image>() ?? awakenBtn.transform.Find("Image")?.GetComponent<Image>()) : null;

        // -------- Tooltip de skills --------
        // Busca el GO si ya existe y, IMPORTANTÍSIMO, garantiza que tengan TMP_Text.
        if (!skillTooltip)
        {
            var t = rootT.Find("Content/Right/SectionSkills/SkillTooltip");
            if (t) skillTooltip = t as RectTransform;
        }
        EnsureSkillTooltipBuilt();   // <- lo llamamos SIEMPRE para asegurar los TMP_Text
        if (skillTooltip) skillTooltip.gameObject.SetActive(false);

        // -------- Validaciones --------
        bool hasStaticStats  = staticStatRows  != null && staticStatRows.Length  > 0;
        bool hasStaticSkills = staticSkillItems!= null && staticSkillItems.Length > 0;

        if (!rootPanel) Debug.LogError("[Detail] Falta rootPanel");
        if (!txtTitle)  Debug.LogError("[Detail] Falta txtTitle");
        if (!imgElement)Debug.LogError("[Detail] Falta imgElement");
        if (!hasStaticStats && (!statsGrid || !statRowTemplate))
            Debug.LogError("[Detail] Falta StatsGrid/RowTemplate (o configura staticStatRows).");
        if (!hasStaticSkills && (!skillsRoot || !skillItemTemplate))
            Debug.LogError("[Detail] Falta SkillsRoot/SkillItemTemplate (o configura staticSkillItems).");
        if (!txtLore) Debug.LogWarning("[Detail] Falta TxtLore (opcional)");

        Hide();
    }

    // ---------- API publica ----------
    public void ShowHero(string heroId)
    {
        var def = FindHeroDef(heroId);
        if (def == null)
        {
            Debug.LogError($"[Detail] Hero '{heroId}' no encontrado");
            return;
        }
        Show(def);
    }


    public void Show(HeroCatalogEntry def)
    {
        _def = def;
        if (_def == null) { Debug.LogError("[Detail] Def nula"); return; }

        if (rootPanel) rootPanel.SetActive(true);   // asegura visibilidad antes de pintar

        BuildHeader();
        LoadFullImage();
        LoadAvatarImage();    // <-- NUEVO (portraitAddressable en AvatarImageRoot)
        BuildStatsGrid();
        BuildSkillsList();
        BuildLore();
        ApplySubClassAndSide(def);
        BuildExtraInfo();     // <-- NUEVO (classStandard, subClass, reino, lado, awaken)
    }

    // --------- NUEVO: helpers de lectura segura desde el JSON (por reflexiÃ³n) ---------
    private static string GetStr(object obj, params string[] names)
    {
        if (obj == null || names == null) return null;

        var t = obj.GetType();
        var flags = System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase;

        foreach (var n in names)
        {
            var f = t.GetField(n, flags);
            if (f != null) { var v = f.GetValue(obj); if (v != null) return v.ToString(); }

            var p = t.GetProperty(n, flags);
            if (p != null) { var v = p.GetValue(obj); if (v != null) return v.ToString(); }
        }
        return null;
    }

    private static void SetText(TMP_Text t, string value)
    {
        if (t) t.text = value ?? string.Empty;
    }

    // --------- NUEVO: carga del retrato en AvatarImageRoot usando portraitAddressable ---------
    private void LoadAvatarImage()
    {
        if (avatarImage == null)
        {
            if (avatarImageRoot) avatarImageRoot.gameObject.SetActive(false);
            return;
        }

        string portraitKey = GetStr(_def, "portraitAddressable", "portrait");
        if (string.IsNullOrEmpty(portraitKey))
        {
            if (avatarImageRoot) avatarImageRoot.gameObject.SetActive(false);
            return;
        }

        Addressables.LoadAssetAsync<Sprite>(portraitKey).Completed += op =>
        {
            if (!avatarImage) return;

            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                avatarImage.sprite = op.Result;
                if (avatarImageRoot) avatarImageRoot.gameObject.SetActive(true);
                avatarImage.enabled = true;
            }
            else
            {
#if UNITY_EDITOR
            Debug.LogWarning($"[Detail] No se pudo cargar portrait: {portraitKey}");
#endif
                if (avatarImageRoot) avatarImageRoot.gameObject.SetActive(false);
            }
        };
    }

    // --------- NUEVO: set de clase/subclase, reino/lado y sprite de facciÃ³n + awaken ---------
    private void BuildExtraInfo()
    {
        // classStandard / subClass
        SetText(fullClass, GetStr(_def, "classStandard", "class", "clase"));
        SetText(fullSubClass, GetStr(_def, "subClass", "subclase", "subClase"));

        // reino -> nombre y (si existe) icono en Assets/Addressables/Art/HeroScene/Faccion/<reino>.png
        string reino = GetStr(_def, "reino", "faccion", "kingdom");
        SetText(faccionName, reino);

        if (faccionIcon && !string.IsNullOrEmpty(reino))
        {
            string faccionKey = $"Assets/Addressables/Art/HeroScene/Faccion/{reino}.png";
            Addressables.LoadAssetAsync<Sprite>(faccionKey).Completed += op =>
            {
                if (!faccionIcon) return;

                if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                {
                    faccionIcon.sprite = op.Result;
                    faccionIcon.enabled = true;
                    faccionIcon.color = Color.white;
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[Detail] No se encontrÃ³ icono de facciÃ³n: {faccionKey}");
#endif
                    faccionIcon.enabled = false;
                    faccionIcon.sprite = null;
                    faccionIcon.color = new Color(1, 1, 1, 0);
                }
            };
        }
        else if (faccionIcon)
        {
            faccionIcon.enabled = false;
            faccionIcon.sprite = null;
            faccionIcon.color = new Color(1, 1, 1, 0);
        }

        // lado -> Right/SectionInfo/LadoTxt
        SetText(ladoTxt, GetStr(_def, "lado", "side"));

        // portraitAddressableAwaken -> imagen del botÃ³n AwakenBtn
        if (awakenBtnImage)
        {
            string awakenKey = GetStr(_def, "portraitAddressableAwaken", "portraitAwaken");
            if (!string.IsNullOrEmpty(awakenKey))
            {
                Addressables.LoadAssetAsync<Sprite>(awakenKey).Completed += op =>
                {
                    if (!awakenBtnImage) return;

                    if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                    {
                        awakenBtnImage.sprite = op.Result;
                        awakenBtnImage.enabled = true;
                        var c = awakenBtnImage.color; c.a = 1f; awakenBtnImage.color = c;
                    }
                    else
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"[Detail] No se pudo cargar awaken portrait: {awakenKey}");
#endif
                        awakenBtnImage.enabled = false;
                        awakenBtnImage.sprite = null;
                    }
                };
            }
            else
            {
                awakenBtnImage.enabled = false;
                awakenBtnImage.sprite = null;
            }
        }
    }

    // LegionDetailPanelUI.cs  (dentro de la clase LegionDetailPanelUI)
    private static string Safe(string s) => string.IsNullOrWhiteSpace(s) ? "" : s;

    /// <summary>
    /// Rellena FullSubClass y LadoTxt con los valores del catÃ¡logo (subClass, lado).
    /// No rompe si los campos no estÃ¡n arrastrados en el inspector.
    /// </summary>
    private void ApplySubClassAndSide(HeroCatalogEntry e)
    {
        try
        {
            if (FullSubClass != null)
                FullSubClass.text = Safe(e?.subClass);

            if (LadoTxt != null)
                LadoTxt.text = Safe(e?.lado);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Detail] ApplySubClassAndSide: " + ex.Message, this);
        }
    }


    public void Hide()
    {
        if (rootPanel) rootPanel.SetActive(false);
    }

    public void HideImmediate()
    {
        // Oculta sin animaciones
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    // ---------- ConstrucciÃƒÂ³n ----------
    private void BuildHeader()
    {
        if (txtTitle) txtTitle.text = _def.displayName;
        if (fullName) fullName.text = _def.displayName;

        // icono de elemento (intenta cargar; si falla, lo oculta)
        string elemKey = $"Assets/Addressables/Art/HeroScene/Elemento/{_def.element}.png";
        Addressables.LoadAssetAsync<Sprite>(elemKey).Completed += op =>
        {
            if (!imgElement) return;
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                imgElement.sprite = op.Result;
                imgElement.color = Color.white;
            }
            else
            {
                imgElement.sprite = null;
                imgElement.color = new Color(1, 1, 1, 0);
            }
        };
    }

    private void LoadFullImage()
    {
        string key = _def.fullAddressable;
        if (string.IsNullOrEmpty(key)) key = _def.portraitAddressable; // fallback

        if (string.IsNullOrEmpty(key))
        {
            if (fullImageRoot) fullImageRoot.gameObject.SetActive(false);
            return;
        }

        Addressables.LoadAssetAsync<Sprite>(key).Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                if (fullImage) fullImage.sprite = op.Result;
                if (fullImageRoot) fullImageRoot.gameObject.SetActive(true);
            }
            else
            {
                if (fullImageRoot) fullImageRoot.gameObject.SetActive(false);
                Debug.LogWarning($"[Detail] No se pudo cargar imagen: {key}");
            }
        };
    }

    private void BuildStatsGrid()
    {
        var stats = CollectMaxStats(_def);

        // Orden legible
        string[] order = { "HP", "ATK", "DEF", "SPD", "TCRI", "DCRI", "ACC", "RES", "LUK", "AGI" };

        // --- Modo estÃ¡tico: usamos las filas ya puestas en la GUI ---
        if (staticStatRows != null && staticStatRows.Length > 0)
        {
            for (int i = 0; i < staticStatRows.Length; i++)
            {
                var row = staticStatRows[i];

                bool within = i < order.Length;
                int v = 0; // <-- declarar y asignar antes, para evitar CS0165
                bool hasValue = within && stats.TryGetValue(order[i], out v);
                bool show = within && hasValue;

                if (row?.go) row.go.SetActive(show);
                if (!show) continue;

                if (row.statLabel) row.statLabel.text = order[i];
                if (row.statValue) row.statValue.text = v.ToString();
            }
            return;
        }

        // --- Fallback al modo "plantilla" por si no configuraste las filas estÃ¡ticas ---
        if (!statsGrid || !statRowTemplate) return;

        for (int i = statsGrid.childCount - 1; i >= 0; i++)
        {
            var c = statsGrid.GetChild(i).gameObject;
            if (c != statRowTemplate) Destroy(c);
        }

        foreach (var k in order)
        {
            if (!stats.TryGetValue(k, out var v)) continue;

            var row = Instantiate(statRowTemplate, statsGrid);
            row.SetActive(true);

            var lbl = row.transform.Find("StatLabel")?.GetComponent<TMP_Text>() ?? _statLabelTemplate;
            var val = row.transform.Find("StatValue")?.GetComponent<TMP_Text>() ?? _statValueTemplate;

            if (lbl) lbl.text = k;
            if (val) val.text = v.ToString();
        }
    }


    private Dictionary<string, int> CollectMaxStats(HeroCatalogEntry def)
    {
        var s = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (def?.stats == null) return s;

        int G(object o, string name)
        {
            try
            {
                var t = o.GetType();
                var f = t.GetField(name);
                if (f != null) return Convert.ToInt32(f.GetValue(o));
                var p = t.GetProperty(name);
                if (p != null) return Convert.ToInt32(p.GetValue(o));
            }
            catch { }
            return 0;
        }

        s["HP"] = G(def.stats, "maxHP");
        s["ATK"] = G(def.stats, "maxATK");
        s["DEF"] = G(def.stats, "maxDEF");
        s["SPD"] = G(def.stats, "maxSPD");
        s["TCRI"] = G(def.stats, "maxTCRI");
        s["DCRI"] = G(def.stats, "maxDCRI");
        s["ACC"] = G(def.stats, "maxACC");
        s["RES"] = G(def.stats, "maxRES");
        s["LUK"] = G(def.stats, "maxLUK");
        s["AGI"] = G(def.stats, "maxAGI");
        return s;
    }

    private void BuildSkillsList()
    {
        var skills = GetHeroSkills(_def);
        if (skills == null) skills = new List<SkillView>();

        // ---------- MODO ESTÁTICO ----------
        if (staticSkillItems != null && staticSkillItems.Length > 0)
        {
            for (int i = 0; i < staticSkillItems.Length; i++)
            {
                var item = staticSkillItems[i];
                bool show = i < skills.Count;

                if (item?.go) item.go.SetActive(show);
                if (!show) continue;

                var s = skills[i];

                if (item.name)      item.name.text = s.name;
                if (item.shortDesc) item.shortDesc.text = s.shortDesc;

                // Nombre de fichero del icono (mantenemos tu lógica)
                string fileName;
                if (!string.IsNullOrEmpty(s.iconFile))
                    fileName = s.iconFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? s.iconFile : s.iconFile + ".png";
                else if (!string.IsNullOrEmpty(s.id))
                    fileName = s.id + ".png";
                else
                {
                    var heroKey = !string.IsNullOrEmpty(_def?.heroId) ? _def.heroId : _def?.displayName ?? "hero";
                    fileName = $"{heroKey}_{s.slot}.png";
                }

                if (item.icon)
                {
                    item.icon.raycastTarget = true; // necesario para recibir eventos
                    LoadSkillIconAsync(fileName, item.icon);

                    var rt = item.icon.GetComponent<RectTransform>();
                    var h  = item.icon.GetComponent<SkillIconHoldHandler>() ?? item.icon.gameObject.AddComponent<SkillIconHoldHandler>();
                    h.Setup(this, s.name ?? "", s.shortDesc ?? "", rt);
                }
            }
            return;
        }

        // ---------- MODO PLANTILLA ----------
        if (!skillsRoot || !skillItemTemplate) return;

        for (int i = skillsRoot.childCount - 1; i >= 0; --i)
        {
            var child = skillsRoot.GetChild(i).gameObject;
            if (child == skillItemTemplate) continue;
            Destroy(child);
        }
        skillItemTemplate.SetActive(false);

        foreach (var s in skills)
        {
            var go = Instantiate(skillItemTemplate, skillsRoot, false);
            go.SetActive(true);

            var icon     = go.transform.Find("Icon")?.GetComponent<Image>();
            var txtName  = go.transform.Find("Name")?.GetComponent<TMP_Text>();
            var txtShort = go.transform.Find("ShortDesc")?.GetComponent<TMP_Text>();

            if (txtName)  txtName.text  = s.name;
            if (txtShort) txtShort.text = s.shortDesc;

            string fileName;
            if (!string.IsNullOrEmpty(s.iconFile))
                fileName = s.iconFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? s.iconFile : s.iconFile + ".png";
            else if (!string.IsNullOrEmpty(s.id))
                fileName = s.id + ".png";
            else
            {
                var heroKey = !string.IsNullOrEmpty(_def?.heroId) ? _def.heroId : _def?.displayName ?? "hero";
                fileName = $"{heroKey}_{s.slot}.png";
            }

            if (icon)
            {
                icon.raycastTarget = true;
                LoadSkillIconAsync(fileName, icon);

                var rt = icon.GetComponent<RectTransform>();
                var h  = icon.GetComponent<SkillIconHoldHandler>() ?? icon.gameObject.AddComponent<SkillIconHoldHandler>();
                h.Setup(this, s.name ?? "", s.shortDesc ?? "", rt);
            }
        }
    }

    // === Reemplaza TODO el método por esta versión ===
    private void EnsureSkillTooltipBuilt()
    {
        // 1) Contenedor (SkillTooltip) dentro de Content/Right
        if (!skillTooltip)
        {
            var right = transform.Find("Content/Right") as RectTransform
                        ?? (rootPanel ? rootPanel.transform.Find("Content/Right") as RectTransform : null)
                        ?? (rootPanel ? rootPanel.transform as RectTransform : transform as RectTransform);

            var go = new GameObject("SkillTooltip",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(ContentSizeFitter),
                typeof(LayoutElement));

            go.transform.SetParent(right, false);
            skillTooltip = go.GetComponent<RectTransform>();
        }

        // Fondo y CanvasGroup
        var bg = skillTooltip.GetComponent<Image>();
        bg.raycastTarget = false;
        bg.color = new Color(0f, 0f, 0f, 0.88f); // fondo más oscuro para contrastar

        var cg = skillTooltip.GetComponent<CanvasGroup>();
        cg.interactable   = false;
        cg.blocksRaycasts = false;

        // Auto-size del propio tooltip
        var fitter = skillTooltip.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Anclaje/pivote del tooltip (esquina sup-izq del contenedor Right)
        skillTooltip.anchorMin = new Vector2(0f, 1f);
        skillTooltip.anchorMax = new Vector2(0f, 1f);
        skillTooltip.pivot     = new Vector2(0f, 1f);

        // Tamaño preferido del tooltip
        var le = skillTooltip.GetComponent<LayoutElement>();
        le.minWidth        = 700f;
        le.preferredWidth  = 860f;
        le.minHeight       = 220f;
        le.flexibleWidth   = 0f;
        le.flexibleHeight  = 0f;

        // 2) Contenido vertical (Content)
        RectTransform contentRT;
        var content = skillTooltip.Find("Content");
        if (!content)
        {
            var go = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(skillTooltip, false);
            contentRT = go.GetComponent<RectTransform>();
        }
        else contentRT = content as RectTransform;

        // Anclas/pivote y tamaño del CONTENT para que no vuelva a 100x100
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(0f, 1f);
        contentRT.pivot     = new Vector2(0f, 1f);
        contentRT.sizeDelta = new Vector2(le.preferredWidth, le.minHeight);

        var cle = contentRT.GetComponent<LayoutElement>();
        cle.minWidth       = le.preferredWidth;
        cle.preferredWidth = le.preferredWidth;
        cle.minHeight      = le.minHeight;
        cle.preferredHeight= le.minHeight;

        var vlg = contentRT.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.spacing                = 6;
        vlg.padding                = new RectOffset(14, 14, 12, 12);

        // 3) Textos (crear si faltan)
        var nameRT = contentRT.Find("Name") as RectTransform;
        if (!nameRT)
        {
            var go = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(contentRT, false);
            nameRT = go.GetComponent<RectTransform>();
        }
        skillTooltipName = nameRT.GetComponent<TMP_Text>() ?? nameRT.gameObject.AddComponent<TextMeshProUGUI>();
        skillTooltipName.color            = Color.white;
        skillTooltipName.enableAutoSizing = true;
        skillTooltipName.fontSizeMin      = 18;
        skillTooltipName.fontSizeMax      = 34;
        skillTooltipName.fontStyle        = FontStyles.Bold;
        skillTooltipName.alignment        = TextAlignmentOptions.TopLeft;

        var descRT = contentRT.Find("ShortDesc") as RectTransform;
        if (!descRT)
        {
            var go = new GameObject("ShortDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(contentRT, false);
            descRT = go.GetComponent<RectTransform>();
        }
        skillTooltipDesc = descRT.GetComponent<TMP_Text>() ?? descRT.gameObject.AddComponent<TextMeshProUGUI>();
        skillTooltipDesc.color              = Color.white;
        skillTooltipDesc.enableAutoSizing   = true;
        skillTooltipDesc.fontSizeMin        = 14;
        skillTooltipDesc.fontSizeMax        = 26;
        skillTooltipDesc.alignment          = TextAlignmentOptions.TopLeft;
        skillTooltipDesc.enableWordWrapping = true;

        // Empieza oculto
        skillTooltip.gameObject.SetActive(false);
    }

    // === Reemplaza TODO el método por esta versión ===
    internal void ShowSkillTooltip(string name, string desc, RectTransform fromIcon)
    {
        if (!fromIcon) return;

        // Asegura construcción y textos
        if (!skillTooltip) EnsureSkillTooltipBuilt();
        if (!skillTooltip) return;

        if (skillTooltipName) skillTooltipName.text = name ?? string.Empty;
        if (skillTooltipDesc) skillTooltipDesc.text = desc ?? string.Empty;

        // Recalcular tamaño preferido antes de posicionar
        LayoutRebuilder.ForceRebuildLayoutImmediate(skillTooltip);
        if (skillTooltip.childCount > 0)
            LayoutRebuilder.ForceRebuildLayoutImmediate(skillTooltip.GetChild(0) as RectTransform);

        var parent = skillTooltip.parent as RectTransform;
        if (!parent) parent = transform as RectTransform;

        // ---------- POSICIONAMIENTO EN EL CUADRANTE SUPERIOR-DERECHO ----------
        // El tooltip tiene ancla/pivote (0,1) (esquina sup-izq del parent).
        // Tomamos la esquina superior-derecha del icono en espacio local del parent:
        var corners = new Vector3[4];
        fromIcon.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
        Vector2 localTR = parent.InverseTransformPoint(corners[2]);

        // Distancias desde la esquina sup-izq del parent
        float leftFromParent = localTR.x - parent.rect.xMin;      // a la derecha del icono
        float topFromParent = parent.rect.yMax - localTR.y;      // un poco por debajo

        float mx = (Mathf.Abs(skillTooltipOffset.x) < 1f) ? 20f : skillTooltipOffset.x; // margen X
        float my = (Mathf.Abs(skillTooltipOffset.y) < 1f) ? 16f : skillTooltipOffset.y; // margen Y

        // Con ancla/pivote (0,1): anchoredPosition = (left, -top)
        Vector2 pos = new Vector2(leftFromParent + mx, -(topFromParent + my));

        // ---------- CLAMPS PARA QUE NUNCA SE SALGA DE LA VISTA ----------
        float w = skillTooltip.rect.width;
        float h = skillTooltip.rect.height;

        // límites interiores (un pequeño margen)
        const float margin = 8f;

        // derecha / abajo
        float maxLeft = parent.rect.width - w - margin;
        float maxTop = parent.rect.height - margin;

        if (pos.x > maxLeft) pos.x = maxLeft;          // derecha
        if (-pos.y > maxTop) pos.y = -maxTop;          // abajo (y es negativo)

        // izquierda / arriba
        if (pos.x < margin) pos.x = margin;
        if (pos.y > -margin) pos.y = -margin;

        // Aplicar
        skillTooltip.anchoredPosition = pos;
        skillTooltip.SetAsLastSibling();
        skillTooltip.gameObject.SetActive(true);
    }





    internal void HideSkillTooltip()
    {
        if (skillTooltip) skillTooltip.gameObject.SetActive(false);
    }


    // Carga de iconos sin usar coroutines (evita error con GO inactivo)
    private void LoadSkillIconAsync(string fileName, UnityEngine.UI.Image target)
    {
        if (target == null) return;

        // Ruta real que me dijiste: Assets/Addressables/SkillIcon/*.png
        var key = $"Assets/Addressables/SkillIcon/{fileName}";

        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(key);
        handle.Completed += op =>
        {
            // Si el target fue destruido/ocultado durante la carga, liberamos y salimos
            if (target == null || target.gameObject == null)
            {
                if (handle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(handle);
                return;
            }

            if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && op.Result != null)
            {
                try
                {
                    target.sprite = op.Result;
                    target.enabled = true;
                }
                catch (MissingReferenceException)
                {
                    // Se destruyÃ³ justo en este frame: ignorar
                }
                // No liberamos en Ã©xito para mantener el sprite con vida por ref-count
            }
            else
            {
#if UNITY_EDITOR
            Debug.LogWarning($"[Detail] No se encontrÃ³ icono de skill: {key}");
#endif
                if (handle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(handle);
                target.enabled = false;
            }
        };
    }



    private System.Collections.IEnumerator TryLoadSpriteFirstAvailable(string fileName, Image target)
    {
        if (target == null) yield break;

        // Tus iconos estÃ¡n aquÃ­:
        var key = $"Assets/Addressables/SkillIcon/{fileName}";

        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(key);
        yield return handle;

        // Si durante la carga se destruyÃ³ el Image o su GO, aborta y libera
        if (target == null || target.gameObject == null)
        {
            if (handle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(handle);
            yield break;
        }

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            // Asigna con try/catch por si el Image se destruye justo en este frame
            try
            {
                target.sprite = handle.Result;
                target.enabled = true;
            }
            catch (MissingReferenceException)
            {
                // El objeto se destruyÃ³ entre el yield y la asignaciÃ³n; ignoramos
            }
            // Nota: no liberamos el handle en Ã©xito para mantener el sprite vivo (ref-count).
        }
        else
        {
#if UNITY_EDITOR
        Debug.LogWarning($"[Detail] No se encontrÃ³ icono de skill: {key}");
#endif
            if (handle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(handle);
            if (target) target.enabled = false;
        }
    }

    // Devuelve la lista de skills del hÃ©roe leyendo los campos reales del JSON por reflexiÃ³n.
    // Campos soportados por skill: type/slot, displayName/name, description/gameDesc, skillId/id, icon/iconFile.
    private class SkillView
    {
        public string id;
        public string name;
        public string shortDesc;
        public string slot;
        public string iconFile;
    }


    private List<SkillView> GetHeroSkills(HeroCatalogEntry def)
    {
        var list = new List<SkillView>();
        if (def == null) return list;

        // Intenta obtener la colecciÃ³n def.skills por campo o propiedad
        object skillsObj = null;
        var t = def.GetType();
        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;

        var f = t.GetField("skills", flags);
        if (f != null) skillsObj = f.GetValue(def);
        else
        {
            var p = t.GetProperty("skills", flags);
            if (p != null) skillsObj = p.GetValue(def);
        }

        if (skillsObj is System.Collections.IEnumerable enumerable)
        {
            foreach (var s in enumerable)
            {
                if (s == null) continue;
                var st = s.GetType();

                string GetStr(string name)
                {
                    var ff = st.GetField(name, flags);
                    if (ff != null) { var v = ff.GetValue(s); return v?.ToString(); }
                    var pp = st.GetProperty(name, flags);
                    if (pp != null) { var v = pp.GetValue(s); return v?.ToString(); }
                    return null;
                }

                string slot = GetStr("type") ?? GetStr("slot") ?? "basic";
                string dispName = GetStr("displayName") ?? GetStr("name") ?? slot;
                string desc = GetStr("description") ?? GetStr("gameDesc") ?? "";
                string skillId = GetStr("skillId") ?? GetStr("id");
                string iconFile = GetStr("icon") ?? GetStr("iconFile");

                // Fallbacks robustos
                if (string.IsNullOrEmpty(skillId))
                    skillId = (!string.IsNullOrEmpty(def.heroId) ? def.heroId : def.displayName) + "_" + slot;

                list.Add(new SkillView
                {
                    id = skillId,
                    name = dispName,
                    shortDesc = desc,
                    slot = slot,
                    iconFile = iconFile
                });
            }

            // Orden consistente
            list.Sort((a, b) => OrderOfType(a.slot).CompareTo(OrderOfType(b.slot)));
        }

        return list;
    }

    private int OrderOfType(string type)
    {
        switch ((type ?? "").ToLowerInvariant())
        {
            case "basic": return 0;
            case "strong": return 1;
            case "ultimate": return 2;
            case "passive": return 3;
        }
        return 9;
    }

    private void BuildLore()
    {
        if (!txtLore) return;

        // intenta varios nombres habituales por si tu JSON usa otro campo
        string lore = null;
        var t = _def.GetType();
        var flags = System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase;

        string[] candidates = { "lore", "story", "bio", "background", "flavor", "flavour", "historia", "descripcionLarga" };
        foreach (var name in candidates)
        {
            var f = t.GetField(name, flags);
            if (f != null) { lore = f.GetValue(_def)?.ToString(); break; }

            var p = t.GetProperty(name, flags);
            if (p != null) { lore = p.GetValue(_def)?.ToString(); break; }
        }

        // Fallback mÃƒÂ¡s comÃƒÂºn en tu catÃƒÂ¡logo
        if (string.IsNullOrWhiteSpace(lore))
            lore = _def.description;

        txtLore.text = string.IsNullOrWhiteSpace(lore) ? "" : lore;
    }


    // ---------- Helpers ----------
    private HeroCatalogEntry FindHeroDef(string heroId)
    {
        // 1) Gestor ligero de LegiÃƒÂ³n (si existe en escena)
        var lgm = FindObjectOfType<LegionHeroCatalogManager>();
        if (lgm?.heroes != null)
        {
            var d = lgm.heroes.FirstOrDefault(h => h.heroId == heroId);
            if (d != null) return d;
        }

        // 2) Gestor global
        var hcm = HeroCatalogManager.Instance;
        if (hcm != null)
        {
            try { var d = hcm.GetHeroById(heroId); if (d != null) return d; } catch { }
            if (hcm.heroes != null)
            {
                var d = hcm.heroes.FirstOrDefault(h => h.heroId == heroId);
                if (d != null) return d;
            }
        }
        return null;
    }
    private class SkillIconHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private LegionDetailPanelUI _ui;
        private string _name;
        private string _desc;
        private RectTransform _rt;
        private Image _img;
        private Vector3 _origScale;
        private Color _origColor;

        public void Setup(LegionDetailPanelUI ui, string name, string desc, RectTransform iconRT)
        {
            _ui   = ui;
            _name = name;
            _desc = desc;
            _rt   = iconRT;
            _img  = iconRT ? iconRT.GetComponent<Image>() : null;

            _origScale = iconRT ? iconRT.localScale : Vector3.one;
            _origColor = _img ? _img.color : Color.white;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_rt)  _rt.localScale = _origScale * 0.92f;                   // efecto “presionado”
            if (_img) _img.color     = _origColor * new Color(0.9f,0.9f,0.9f,1f);

            if (_ui != null && _rt != null)
                _ui.ShowSkillTooltip(_name, _desc, _rt);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_rt)  _rt.localScale = _origScale;
            if (_img) _img.color     = _origColor;
            if (_ui != null) _ui.HideSkillTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_rt)  _rt.localScale = _origScale;
            if (_img) _img.color     = _origColor;
            if (_ui != null) _ui.HideSkillTooltip();
        }
    }

}

