/*
============================================================
HeroSkillsPanelController.cs — Render de habilidades activas
------------------------------------------------------------
PROPÓSITO
- Construir la descripción final a partir de gameDesc + datos reales
  (placeholders, glosario, deltas en verde, totales en azul).

MÉTODOS (COMPLETA AQUÍ)
- SetHero(HeroProgress, HeroCatalogEntry).
- BuildDescription(skill, playerLevel): aplica reglas de formateo.
- OpenGlossary(key)/CloseGlossary(): coordina UIModalBlocker.
============================================================
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HeroSkillsPanelController : MonoBehaviour
{
    [Header("UI")]
    public Image imageBasic;
    public Image imageStrong;
    public Image imageUltimate;

    public TMP_Text textActive;   // Título
    public TMP_Text descActive;   // Descripción (clicable)
    public TMP_Text updateObtain; // Líneas Lv.N

    [Header("Glosario (opcional)")]
    [SerializeField] private GameObject glossaryPanel;
    [SerializeField] private TMP_Text glossaryTitleTxt;
    [SerializeField] private TMP_Text glossaryBodyTxt;


    [Header("Colores")]
    public Color colorSelected = Color.white;
    public Color colorUnselected = new Color(1f, 1f, 1f, 0.35f);
    [Header("Cierre del glosario")]
    [SerializeField] private GameObject uiBlockerPanel; // arrastra aquí tu UIBlockerPanel (tiene Button)


    private const string SkillIconPath = "Assets/Addressables/SkillIcon/{0}.png";

    // Datos activos
    private List<HeroSkill> currentSkills = new List<HeroSkill>();
    private List<SkillProgress> currentPlayerSkills = new List<SkillProgress>();
    private string currentType = "basic";
    private bool linksWired = false;
    private bool _blockerReady = false;
    private bool _blockerPrimed;
    private Coroutine _showGlossaryCo;
    private Coroutine _primeAndActivateCo;
    private void Awake()
    {
        PrepareUIBlockerOnce();
    }


    // ====================== API PRINCIPAL ======================
    public void SetSkills(List<HeroSkill> skills, List<SkillProgress> playerSkills)
    {
        currentSkills = skills ?? new List<HeroSkill>();
        currentPlayerSkills = playerSkills ?? new List<SkillProgress>();

        // Preparar botones e iconos
        PrepareButtonsAndIcons();

        // Selección por defecto
        SelectSkillByType("basic");

        // Enlazar clicks de <link="..."> una sola vez
        WireDescLinks();
    }

    private void PrepareButtonsAndIcons()
    {
        foreach (var img in new[] { imageBasic, imageStrong, imageUltimate })
        {
            if (!img) continue;
            img.raycastTarget = true;
            var b = img.GetComponent<Button>() ?? img.gameObject.AddComponent<Button>();
            b.onClick.RemoveAllListeners();
        }

        HeroSkill basic = null, strong = null, ultimate = null;

        // Traza del “inventario” de skills que nos pasan
        if (currentSkills != null)
        {
            foreach (var s in currentSkills)
            {
                if (s == null) continue;
                var sid = SafeGetString(s, "skillId");
                var typ = NormalizeType(SafeGetString(s, "type"));
                Debug.Log($"[SkillsPanel/Map] cat skill -> id='{sid}' type='{typ}' (sufijo: _basic/_strong/_ultimate={(sid ?? "").EndsWith("_basic", StringComparison.OrdinalIgnoreCase) || (sid ?? "").EndsWith("_strong", StringComparison.OrdinalIgnoreCase) || (sid ?? "").EndsWith("_ultimate", StringComparison.OrdinalIgnoreCase)})");
            }
        }

        // Localiza candidatos por type o por sufijo
        foreach (var s in currentSkills)
        {
            if (s == null) continue;
            var typeNorm = NormalizeType(SafeGetString(s, "type"));
            var sid = SafeGetString(s, "skillId");

            if (basic == null && (typeNorm == "basic" || sid.EndsWith("_basic", StringComparison.OrdinalIgnoreCase))) basic = s;
            if (strong == null && (typeNorm == "strong" || sid.EndsWith("_strong", StringComparison.OrdinalIgnoreCase))) strong = s;
            if (ultimate == null && (typeNorm == "ultimate" || sid.EndsWith("_ultimate", StringComparison.OrdinalIgnoreCase))) ultimate = s;
        }

        void Bind(Image img, HeroSkill sk, string t)
        {
            if (!img) return;
            var btn = img.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectSkillByType(t)); // siempre clickable

            var sid = SafeGetString(sk, "skillId");
            Debug.Log($"[SkillsPanel/Map] botón='{t}' -> skillId='{sid}'");

            // Carga icono si tenemos skill concreta
            if (!string.IsNullOrEmpty(sid))
            {
                string addr = string.Format(SkillIconPath, sid);
                Addressables.LoadAssetAsync<Sprite>(addr).Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded && img) img.sprite = h.Result;
                };
            }
        }

        Bind(imageBasic, basic, "basic");
        Bind(imageStrong, strong, "strong");
        Bind(imageUltimate, ultimate, "ultimate");
    }




    public void SelectSkillByType(string rawType)
    {
        Debug.Log($"[SkillsPanel/Select] pedido type='{rawType}' | skills={currentSkills?.Count ?? 0}");
        if (currentSkills == null || currentSkills.Count == 0)
        {
            Debug.LogWarning("[SkillsPanel/Select] No hay skills para seleccionar.");
            return;
        }

        string t = NormalizeType(rawType);
        if (t != "basic" && t != "strong" && t != "ultimate") t = "basic";

        // Listado diagnóstico
        foreach (var s in currentSkills)
        {
            if (s == null) continue;
            var sid = SafeGetString(s, "skillId");
            var typ = NormalizeType(SafeGetString(s, "type"));
            Debug.Log($"[SkillsPanel/Select] candidato -> id='{sid}' type='{typ}'");
        }

        string reason = "UNKNOWN";
        HeroSkill skill = null;

        // 1) Coincidencia exacta por type
        skill = currentSkills.FirstOrDefault(s =>
            string.Equals(NormalizeType(SafeGetString(s, "type")), t, StringComparison.OrdinalIgnoreCase));
        if (skill != null) reason = "FOUND_BY_TYPE";

        // 2) Por sufijo del skillId (_basic/_strong/_ultimate)
        if (skill == null)
        {
            string suf = "_" + t;
            skill = currentSkills.FirstOrDefault(s =>
                SafeGetString(s, "skillId").EndsWith(suf, StringComparison.OrdinalIgnoreCase));
            if (skill != null) reason = "FOUND_BY_SUFFIX";
        }

        // 3) Heurística: id contiene “_t”, description/gameDesc/effect existen…
        if (skill == null)
        {
            skill = currentSkills.FirstOrDefault(s =>
            {
                string sid = SafeGetString(s, "skillId");
                string typ = NormalizeType(SafeGetString(s, "type"));
                var eff = SafeGetEnumerable(s, "effect");
                string dsc = SafeGetString(s, "description");
                string gds = SafeGetString(s, "gameDesc");
                return sid.Contains("_" + t, StringComparison.OrdinalIgnoreCase) || typ == t || eff != null || !string.IsNullOrEmpty(dsc) || !string.IsNullOrEmpty(gds);
            });
            if (skill != null) reason = "FOUND_BY_HEURISTIC";
        }

        // 4) Último fallback: primera skill
        if (skill == null)
        {
            skill = currentSkills[0];
            reason = "FALLBACK_FIRST";
            Debug.LogWarning($"[SkillsPanel/Select] Fallback a primera skill.");
        }

        var selId = SafeGetString(skill, "skillId");
        var selType = NormalizeType(SafeGetString(skill, "type"));

        Debug.Log($"[SkillsPanel/Select] RESULT => pedido='{t}' | seleccionado id='{selId}' type='{selType}' | reason={reason}");

        currentType = t;
        UpdateSelectedImageVisual();

        int reachedLevel = LevelReached(selId);

        if (textActive) textActive.text = SafeGetString(skill, "name", t.ToUpperInvariant());

        if (descActive)
        {
            string desc = BuildDescWithLinksAndUpgrades(skill, reachedLevel);
            descActive.richText = true;                // <- minúscula
            descActive.text = desc;
            TryWireDescLinks();
        }

        if (updateObtain)
            updateObtain.text = BuildLevelUpLines(skill, reachedLevel);
    }




    private void TryWireDescLinks()
    {
        if (!descActive) return;

        // Necesario para que TMP detecte clicks en <link>
        descActive.raycastTarget = true;

        var fwd = descActive.GetComponent<TMPSimpleLinkForwarder>();
        if (!fwd) fwd = descActive.gameObject.AddComponent<TMPSimpleLinkForwarder>();

        fwd.Bind(descActive, OnDescLinkClicked);
    }







    // ====================== DESCRIPCIÓN + UPGRADES ======================

    private string BuildDescWithLinksAndUpgrades(object skillObj, int reachedLevel)
    {
        if (skillObj == null) return "";

        string gameDesc = SafeGetString(skillObj, "gameDesc", null);
        int hits = SafeGetInt(skillObj, "hits", 1);
        string scaleStat = SafeGetString(skillObj, "scaleStat");
        float multiplier = SafeGetFloat(skillObj, "multiplier", 0f);
        int cooldown = SafeGetInt(skillObj, "cooldown", 0);

        // Base
        string baseDesc = !string.IsNullOrWhiteSpace(gameDesc)
            ? ExpandGameDescTemplate(skillObj, gameDesc)
            : BuildRawDescription(skillObj, scaleStat, multiplier, hits, cooldown);

        // Agregados por nivel alcanzado
        var levelUpList = SafeGetEnumerable(skillObj, "levelUp");
        var (plusMultPP, plusChancePP, cdDelta) = ComputeAggregatesFromLevelUps(levelUpList, reachedLevel);

        // Decorado inline
        string withUpgrades = ApplyInlineBuffsToDesc(baseDesc, skillObj, plusMultPP, plusChancePP, cdDelta);

        // Enlazar tokens del glosario (UNA SOLA CADENA)
        string linked = LinkGlossaryTokens(withUpgrades);

        // Log
        float baseMultPct = Mathf.Round(SafeGetFloat(skillObj, "multiplier", 0f) * 100f);
        int baseChanceFirst = FirstEffectChancePercent(skillObj);
        int baseCd = SafeGetInt(skillObj, "cooldown", 0);
        Debug.Log($"[SkillsPanel] FormatDesc: baseMult={baseMultPct}%, +mult={plusMultPP}pp, baseChance={baseChanceFirst}%, +chance={plusChancePP}pp, baseCD={baseCd}, deltaCD={cdDelta}");

        return linked;
    }

    private string BuildRawDescription(object skillObj, string scaleStat, float multiplier, int hits, int cooldown)
    {
        var parts = new List<string>();

        // Prefijo "Activa el efecto y luego ataca." si aparece en description original (si te interesa forzarlo, añádelo aquí)
        string orig = SafeGetString(skillObj, "description");
        if (!string.IsNullOrEmpty(orig) && orig.TrimStart().StartsWith("Activa el efecto y luego ataca.", StringComparison.OrdinalIgnoreCase))
            parts.Add("Activa el efecto y luego ataca.");

        // Efectos
        var effList = SafeGetEnumerable(skillObj, "effect");
        if (effList != null)
        {
            foreach (var e in effList)
            {
                string type = SafeGetString(e, "type");
                string target = MapTarget(SafeGetString(e, "target"));
                float chance = SafeGetFloat(e, "chance", -1f);
                float value = SafeGetFloat(e, "value", -1f);

                if (string.IsNullOrEmpty(type)) continue;

                string line = $"Aplica {type} a {target}";
                bool isAllyBuff = (target == "a un aliado" || target == "al equipo");
                if (!isAllyBuff && chance >= 0f && chance < 0.999f)
                    line += $" con una probabilidad del {(int)Math.Round(chance * 100f)}%";

                if ((type == "heal" || type == "heal_team" || type == "shield") && value >= 0f)
                    line += $" por un valor del {(int)Math.Round(value * 100f)}%";

                parts.Add(line + ".");
            }
        }

        // Daño
        if (multiplier > 0f && !string.IsNullOrEmpty(scaleStat))
            parts.Add($"Inflige {ToPercent(multiplier)} del {scaleStat} con {Math.Max(1, hits)} golpe/s.");

        // Enfriamiento
        if (cooldown > 0)
            parts.Add($"Enfriamiento {cooldown} turno/s");

        return string.Join(" ", parts);
    }

    private string ExpandGameDescTemplate(object skillObj, string template)
    {
        string result = template ?? "";

        // Reemplazos simples
        result = result.Replace("multiplier", ToPercent(SafeGetFloat(skillObj, "multiplier", 0f)));
        result = result.Replace("scaleStat", SafeGetString(skillObj, "scaleStat"));
        result = result.Replace("hits", Math.Max(1, SafeGetInt(skillObj, "hits", 1)).ToString(CultureInfo.InvariantCulture));
        result = result.Replace("cooldown", Math.Max(0, SafeGetInt(skillObj, "cooldown", 0)).ToString(CultureInfo.InvariantCulture));

        // effect.*
        string effTarget = "al objetivo";
        string effChance = "";
        string effValue = "";

        var effList = SafeGetEnumerable(skillObj, "effect");
        if (effList != null)
        {
            foreach (var e in effList)
            {
                string t = SafeGetString(e, "type");
                string target = SafeGetString(e, "target");
                float chance = SafeGetFloat(e, "chance", -1f);
                float value = SafeGetFloat(e, "value", -1f);

                if (!string.IsNullOrEmpty(target)) effTarget = MapTarget(target);
                if (chance >= 0f) effChance = $"{(int)Math.Round(chance * 100f)}%";
                if ((t == "heal" || t == "heal_team" || t == "shield") && value >= 0f)
                    effValue = $"{(int)Math.Round(value * 100f)}%";
                break; // primera entrada
            }
        }

        result = result.Replace("effect.target", effTarget);
        if (!string.IsNullOrEmpty(effChance)) result = result.Replace("effect.chance", effChance);
        if (!string.IsNullOrEmpty(effValue)) result = result.Replace("effect.value", effValue);

        return result;
    }


    private (int plusMultPP, int plusChancePP, int cdDelta) ComputeAggregatesFromLevelUps(IEnumerable levelUpList, int reachedLevel)
    {
        int plusMultPP = 0;
        int plusChancePP = 0;
        int cdDelta = 0;
        if (levelUpList == null) return (plusMultPP, plusChancePP, cdDelta);

        var seenCooldownLvls = new HashSet<int>();
        foreach (var up in levelUpList)
        {
            int lvl = SafeGetInt(up, "lvl", SafeGetInt(up, "level", 0));
            if (lvl <= 0 || lvl > reachedLevel) continue;

            string change = SafeGetString(up, "change").ToLowerInvariant();
            string rawVal = SafeGetString(up, "value");

            if (change == "multiplier")
            {
                float pp = ParseValueAsPoints(rawVal, isChance: false); // devuelve pp
                plusMultPP += Mathf.RoundToInt(pp);
            }
            else if (change == "chance")
            {
                // En "chance" el valor ya suele venir como % (pp)
                float pp = ParseValueAsPoints(rawVal, isChance: true);
                plusChancePP += Mathf.RoundToInt(pp);
            }
            else if (change == "cooldown")
            {
                if (seenCooldownLvls.Add(lvl))
                    cdDelta -= 1; // cada upgrade reduce 1
            }
        }
        return (plusMultPP, plusChancePP, cdDelta);
    }

    private string ApplyInlineBuffsToDesc(string baseDesc, object skillObj, int plusMultPP, int plusChancePP, int cdDelta)
    {
        string result = baseDesc ?? "";

        string Green(string t) => $"<color=#20FF20>{t}</color>";
        string BlueB(string t) => $"<b><color=#00B4FF>{t}</color></b>";

        // DMG (primer porcentaje tras verbos típicos de daño)
        if (plusMultPP != 0)
        {
            result = RegexReplaceFirst(result, @"((Inflige|Lanza|Propina|Causa|Asesta)\s*)(\d+)\s*%", m =>
            {
                int basePct = SafeInt(m.Groups[3].Value);
                int fin = Mathf.Max(0, basePct + plusMultPP);
                return $"{m.Groups[1].Value}{basePct}% {Green($"+{plusMultPP}%")} = {BlueB($"{fin}%")}";
            });
        }

        // Probabilidad
        if (plusChancePP != 0)
        {
            result = RegexReplaceFirst(result, @"(probabilidad[^0-9]{0,10})(\d+)\s*%", m =>
            {
                int basePct = SafeInt(m.Groups[2].Value);
                int fin = Mathf.Clamp(basePct + plusChancePP, 0, 100);
                return $"{m.Groups[1].Value}{basePct}% {Green($"+{plusChancePP}%")} = {BlueB($"{fin}%")}";
            });
        }

        // Enfriamiento
        int baseCd = SafeGetInt(skillObj, "cooldown", 0);
        if (baseCd > 0 && cdDelta != 0)
        {
            int finalCd = Mathf.Max(0, baseCd + cdDelta);
            string minusStream = string.Concat(Enumerable.Repeat($" {Green("-1")}", Mathf.Abs(cdDelta)));
            result = RegexReplaceFirst(result, @"(Enfriamiento\s*)(\d+)", m =>
                $"{m.Groups[1].Value}{m.Groups[2].Value}{minusStream} {BlueB($"= {finalCd}")}");
        }

        return result;
    }


    private static string RegexReplaceFirst(string input, string pattern, Func<Match, string> replacer)
    {
        var rx = new Regex(pattern, RegexOptions.IgnoreCase);
        return rx.Replace(input, new MatchEvaluator(replacer), 1);
    }

    private int FirstEffectChancePercent(object skillObj)
    {
        var effList = SafeGetEnumerable(skillObj, "effect");
        if (effList == null) return 0;
        foreach (var e in effList)
        {
            float chance = SafeGetFloat(e, "chance", -1f);
            if (chance >= 0f) return Mathf.RoundToInt(chance * 100f);
        }
        return 0;
    }

    // ====================== LÍNEAS Lv.N ======================

    // Escribe una línea por nivel. Toda la línea en VERDE si lvl <= reachedLevel.
    private string BuildLevelUpLines(object skillObj, int reachedLevel)
    {
        var ups = SafeGetEnumerable(skillObj, "levelUp");
        if (ups == null) return "";

        var sb = new StringBuilder();
        foreach (var it in ups)
        {
            int lvl = SafeGetInt(it, "lvl", SafeGetInt(it, "level", 0));
            string ch = SafeGetString(it, "change", "");
            string raw = SafeGetString(it, "value", "");

            string line;
            if (string.Equals(ch, "multiplier", StringComparison.OrdinalIgnoreCase))
            {
                float pp = ParseValueAsPoints(raw, isChance: false);
                line = $"Lv.{lvl}: Dmg +{Mathf.RoundToInt(pp)}%";
            }
            else if (string.Equals(ch, "chance", StringComparison.OrdinalIgnoreCase))
            {
                line = $"Lv.{lvl}: Probabilidad {raw}";
            }
            else if (string.Equals(ch, "cooldown", StringComparison.OrdinalIgnoreCase))
            {
                line = $"Lv.{lvl}: Enfriamiento -1";
            }
            else line = $"Lv.{lvl}: {ch} {raw}";

            if (lvl <= reachedLevel) line = $"<color=#20FF20>{line}</color>";
            sb.AppendLine(line);
        }
        return sb.ToString().TrimEnd();
    }


    // ====================== GLOSARIO (LINKS) ======================

    private void WireDescLinks()
    {
        if (linksWired) return;
        if (!descActive) return;

        descActive.raycastTarget = true;  // MUY importante para poder clicar los <link>

        var fwd = descActive.GetComponent<TMPSimpleLinkForwarder>();
        if (!fwd) fwd = descActive.gameObject.AddComponent<TMPSimpleLinkForwarder>();
        fwd.Bind(descActive, OnDescLinkClicked);

        linksWired = true;
    }


    // Convierte tokens a <u><link="glossary:KEY"><i>[Nombre]</i></link></u>
    // Colocamos <u> POR FUERA del <link> para evitar el bug de TMP con subrayado.
    private string LinkGlossaryTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var dict = GetGlossaryDict();
        if (dict == null || dict.Count == 0) return text;

        // Claves más largas primero para evitar solapamientos (acc_down vs acc)
        var orderedKeys = dict.Keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .OrderByDescending(k => k.Length)
            .ToList();

        foreach (var key in orderedKeys)
        {
            if (!dict.TryGetValue(key, out var info)) continue;
            var display = string.IsNullOrWhiteSpace(info.nombre) ? key : info.nombre;

            // límites de palabra alfanumérica/_ para casar "atk_down" y no romper otras palabras
            var pattern = $@"(?<![A-Za-z0-9_]){Regex.Escape(key)}(?![A-Za-z0-9_])";

            text = Regex.Replace(
                text,
                pattern,
                m => $@"<u><link=""glossary:{key}""><i>[{display}]</i></link></u>",
                RegexOptions.IgnoreCase
            );
        }

        // Saneos de "a al" / "a a"
        text = Regex.Replace(text, @"\ba\s+al\b", " al", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\ba\s+a\b", " a", RegexOptions.IgnoreCase);

        return text;
    }



    private static string BeautifyKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        // "atk_down" -> "Atk Down" (simple)
        var s = key.Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
    }


    // Devuelve el diccionario del glosario desde el Manager.
    private Dictionary<string, (string nombre, string descripcion)> GetGlossaryDict()
    {
        return HeroCatalogManager.GetGlossaryDict();
    }



    // Al hacer click en el link
    private void OnDescLinkClicked(string linkId, string _)
    {
        if (string.IsNullOrEmpty(linkId) || !linkId.StartsWith("glossary:", StringComparison.OrdinalIgnoreCase))
            return;

        var key = linkId.Substring("glossary:".Length);
        var gl = GetGlossaryDict();

        if (gl.TryGetValue(key, out var info))
        {
            ShowGlossary(info.nombre, info.descripcion);
        }
        else
        {
            // Fallback: mostrar igualmente algo útil
            ShowGlossary(BeautifyKey(key), "Sin descripción disponible en el glosario.");
        }
    }




    // ====================== HELPERS ======================

    private void UpdateSelectedImageVisual()
    {
        if (imageBasic) imageBasic.color = (currentType == "basic") ? colorSelected : colorUnselected;
        if (imageStrong) imageStrong.color = (currentType == "strong") ? colorSelected : colorUnselected;
        if (imageUltimate) imageUltimate.color = (currentType == "ultimate") ? colorSelected : colorUnselected;
    }

    private string NormalizeType(string t)
    {
        t = (t ?? "").Trim().ToLowerInvariant();
        if (t == "basica" || t == "básica" || t == "basico") return "basic";
        if (t == "fuerte" || t == "pasiva") return "strong";
        return t;
    }

    private int LevelReached(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || currentPlayerSkills == null) return 0;
        var p = currentPlayerSkills.FirstOrDefault(ps =>
            ps != null &&
            !string.IsNullOrEmpty(ps.skillId) &&
            string.Equals(ps.skillId, skillId, StringComparison.OrdinalIgnoreCase));
        return p != null ? Mathf.Max(0, p.level) : 0;
    }

    private static string MapTarget(string raw)
    {
        switch ((raw ?? "").Trim().ToLowerInvariant())
        {
            case "ally": return "a un aliado";
            case "enemy": return "a un enemigo";
            case "team": return "al equipo";
            case "all_enemy": return "a todos los enemigos";
            default: return "al objetivo";
        }
    }

    private static string ToPercent(float f01) => $"{(int)Math.Round(f01 * 100f)}%";

    // "+0.05x", "0.05x", "+5%", "5", "0.05" → devuelve puntos porcentuales (pp)
    private static float ParseValueAsPoints(string raw, bool isChance)
    {
        if (string.IsNullOrEmpty(raw)) return 0f;
        raw = raw.Trim();

        // % => pp directos
        if (raw.Contains("%"))
        {
            if (float.TryParse(raw.Replace("%", "").Replace("+", "").Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var pp))
                return raw.StartsWith("-") ? -pp : pp;
            return 0f;
        }

        // ...x => factor → *100
        if (raw.EndsWith("x", StringComparison.OrdinalIgnoreCase))
        {
            var s = raw.Substring(0, raw.Length - 1).Replace("+", "").Replace(",", ".");
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            {
                var pp = factor * 100f;
                return raw.StartsWith("-") ? -pp : pp;
            }
            return 0f;
        }

        // número crudo: si no es chance y está 0..1 => *100, si >1 asumimos ya pp
        if (float.TryParse(raw.Replace("+", "").Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
        {
            float pp = isChance ? val : (val <= 1f ? val * 100f : val);
            return raw.StartsWith("-") ? -pp : pp;
        }
        return 0f;
    }

    private static int SafeInt(string s) => int.TryParse(s, out int v) ? v : 0;

    // --- reflexión segura mínima ---
    private static object GetMember(object o, string name)
    {
        if (o == null || string.IsNullOrEmpty(name)) return null;
        var t = o.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) return f.GetValue(o);
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.CanRead) return p.GetValue(o, null);
        return null;
    }
    private static string SafeGetString(object o, string name, string def = "")
    {
        var v = GetMember(o, name);
        return v?.ToString() ?? def;
    }
    private static int SafeGetInt(object o, string name, int def = 0)
    {
        var v = GetMember(o, name);
        if (v == null) return def;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int res)) return res;
        return def;
    }
    private static float SafeGetFloat(object o, string name, float def = 0f)
    {
        var v = GetMember(o, name);
        if (v == null) return def;
        if (v is float f) return f;
        if (v is double d) return (float)d;
        if (float.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float res)) return res;
        return def;
    }
    private static IEnumerable SafeGetEnumerable(object o, string name)
    {
        var v = GetMember(o, name);
        return v as IEnumerable;
    }
    // Envuelve las palabras clave del glosario con: <link="gloss:key"><u><i>[Nombre]</i></u></link>
    private string LinkifyGlossaryTokens(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var dict = GetGlossaryDict(); // key -> (nombre, descripcion)
        if (dict == null || dict.Count == 0) return input;

        foreach (var kv in dict)
        {
            string key = kv.Key;                  // p.ej. "burn"
            string display = kv.Value.nombre;         // p.ej. "Quemadura"
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(display)) continue;

            // Permitimos límites no alfabéticos para casar "burn." "burn," etc.
            var pattern = $@"(?<![A-Za-z0-9_]){Regex.Escape(key)}(?![A-Za-z0-9_])";

            input = Regex.Replace(
                input,
                pattern,
                m => $@"<link=""glossary:{key}""><u><i>[{display}]</i></u></link>",
                RegexOptions.IgnoreCase
            );
        }

        return input;
    }


    private static string MapTargetES(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "a un objetivo";
        switch (raw.ToLowerInvariant())
        {
            case "ally": return "a un aliado";
            case "team": return "al equipo";
            case "enemy": return "a un enemigo";
            case "all_enemy": return "a todos los enemigos";
            default: return "No se";
        }
    }

    // Llamado en Awake() y también desde ShowGlossary por seguridad
    private void PrepareUIBlockerOnce()
    {
        if (_blockerPrimed) return;
        _blockerPrimed = true;

        // Intenta autolocalizar si no está asignado
        if (!uiBlockerPanel)
        {
            var found = GameObject.Find("UIBlockerPanel");
            if (found) uiBlockerPanel = found;
        }

        // No re-parent, no cambiar orden. Solo aseguramos que empieza oculto.
        if (uiBlockerPanel && uiBlockerPanel.activeSelf)
            uiBlockerPanel.SetActive(false);
    }

    private void ShowGlossary(string title, string body)
    {
        if (!glossaryPanel) return;

        // Texto
        if (glossaryTitleTxt) glossaryTitleTxt.text = title ?? string.Empty;
        if (glossaryBodyTxt)  glossaryBodyTxt.text  = body  ?? string.Empty;

        // Mostrar panel
        glossaryPanel.SetActive(true);

        // Si UIModalBlocker ya está listo -> activar directamente
        if (UIModalBlocker.Instance != null)
        {
            UIModalBlocker.Instance.Activate(this, HideGlossary);
            return;
        }

        // PRIMER CLICK: el GO está inactivo y Awake del blocker aún no corrió.
        // Lo “disparamos” un frame para que su Awake cree Instance y apague el GO,
        // y acto seguido lo reactivamos correctamente con Activate().
        if (_primeAndActivateCo != null) StopCoroutine(_primeAndActivateCo);
        _primeAndActivateCo = StartCoroutine(CoPrimeAndActivateBlocker());
    }

    private System.Collections.IEnumerator CoPrimeAndActivateBlocker()
    {
        if (uiBlockerPanel)
        {
            // Esto fuerza que Awake de UIModalBlocker se ejecute por primera vez.
            uiBlockerPanel.SetActive(true);   // Awake lo apagará inmediatamente
            yield return null;                // dejamos que Awake se ejecute
        }

        // Ahora debería existir la instancia; se activa de forma oficial.
        UIModalBlocker.Instance?.Activate(this, HideGlossary);
    }


    private IEnumerator ShowGlossaryRoutine(string title, string body)
    {
        if (!glossaryPanel || !glossaryTitleTxt || !glossaryBodyTxt)
        {
            Debug.LogWarning("[SkillsPanel] GlossaryPanel/Texts no asignados.");
            yield break;
        }

        PrepareUIBlockerOnce(); // asegura botón, image y canvasgroup

        glossaryTitleTxt.text = title ?? "";
        glossaryBodyTxt.text = body ?? "";

        // Activa SIEMPRE el blocker primero y déjalo bajo el panel en la pila
        if (uiBlockerPanel)
        {
            var cg = uiBlockerPanel.GetComponent<CanvasGroup>() ?? uiBlockerPanel.AddComponent<CanvasGroup>();
            cg.alpha = Mathf.Max(cg.alpha, 0.01f);
            cg.blocksRaycasts = true;
            cg.interactable = true;

            if (!uiBlockerPanel.activeSelf) uiBlockerPanel.SetActive(true);
            uiBlockerPanel.transform.SetAsLastSibling();
        }

        // Activa el panel arriba del todo
        glossaryPanel.SetActive(true);
        glossaryPanel.transform.SetAsLastSibling();

        // IMPORTANTÍSIMO: esperar a fin de frame para que el sistema de raycasts
        // registre correctamente los nuevos estados en el primer uso.
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
    }
    private void HideGlossary()
    {
        if (glossaryPanel && glossaryPanel.activeSelf)
            glossaryPanel.SetActive(false);

        // Libera el bloqueador si nosotros somos el “owner”
        UIModalBlocker.Instance?.Release(this);
    }

}

/* Reenviador simple de clicks en <link> para TMP (sin clases externas) */
/* Reenviador simple de clicks en <link> para TMP (sin dependencias externas) */
public class TMPSimpleLinkForwarder : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text _text;
    private System.Action<string, string> _onLink;

    public void Bind(TMP_Text text, System.Action<string, string> onLink)
    {
        _text = text;
        _onLink = onLink;
    }

    public void OnPointerClick(PointerEventData ev)
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        if (_text == null) return;

        int idx = TMP_TextUtilities.FindIntersectingLink(_text, ev.position, ev.pressEventCamera);
        if (idx == -1) return;

        var linkInfo = _text.textInfo.linkInfo[idx];
        _onLink?.Invoke(linkInfo.GetLinkID(), linkInfo.GetLinkText());
    }


}

public class UIBlockerCloseRelay : MonoBehaviour
{
    private System.Action _onHide;

    public void SetCallback(System.Action cb)
    {
        _onHide = cb;
    }

    private void OnDisable()
    {
        // Ejecutar cada vez que el blocker se oculta, venga de donde venga.
        _onHide?.Invoke();
    }
}
