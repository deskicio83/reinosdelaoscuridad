using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;
using ReinoOscuridad.Utils;

namespace ReinoOscuridad.UI.Campaign
{
    /// Panel de preparación de batalla al estilo RAID Shadow Legends.
    /// Reemplaza el panel de confirmación + panel de selección de equipo.
    ///
    /// LAYOUT (full-screen overlay, 1280×720):
    ///   BarraSuperior  (top 10%)  — titulo, energia, [X] cancelar
    ///   ZonaIzquierda  (mid 55%)  — 4 slots en cuadrícula 2×2 + [BATALLAR]
    ///   ZonaDerecha    (mid 55%)  — tarjetas de enemigos
    ///   ZonaHeroes     (bot 35%)  — roster horizontal de héroes (scroll)
    ///
    /// Uso: BattlePrepPanel.Show(prefab, titulo, energyCost, enemies, onBatallar)
    public class BattlePrepPanel : MonoBehaviour
    {
        // ── Refs asignadas por SetupBattlePrepPrefab ───────────────────────────

        [SerializeField] private TMP_Text  _txtTitulo;
        [SerializeField] private TMP_Text  _txtEnergia;
        [SerializeField] private Button[]  _slotButtons;          // 4 pre-construidos (2×2)
        [SerializeField] private Transform _contenedorEnemigos;   // dinámico
        [SerializeField] private Transform _contenedorHeroes;     // dinámico (scroll content)
        [SerializeField] private Button    _btnBatallar;
        [SerializeField] private Button    _btnCancelar;

        // ── Estado ─────────────────────────────────────────────────────────────

        private Action<HeroInstance[]> _onBatallar;
        private readonly string[]      _selectedIds  = new string[4];
        private int                    _selectedCount = 0;

        // ── Static factory ────────────────────────────────────────────────────

        /// Muestra el panel como overlay via UIManager.
        /// Si prefab o UIManager son null, invoca el callback con equipo vacío.
        public static void Show(
            GameObject       prefab,
            string           titulo,
            int              energyCost,
            EnemyInstance[]  enemies,
            Action<HeroInstance[]> onBatallar)
        {
            if (prefab == null || UIManager.Instance == null)
            {
                Debug.LogWarning("[BattlePrepPanel] prefab null o UIManager no disponible — equipo vacío.");
                onBatallar?.Invoke(Array.Empty<HeroInstance>());
                return;
            }

            var go = UIManager.Instance.ShowOverlay(prefab);
            if (go == null)
            {
                onBatallar?.Invoke(Array.Empty<HeroInstance>());
                return;
            }

            go.GetComponent<BattlePrepPanel>()?.Initialize(titulo, energyCost, enemies, onBatallar);
        }

        // ── Inicialización ────────────────────────────────────────────────────

        public void Initialize(
            string           titulo,
            int              energyCost,
            EnemyInstance[]  enemies,
            Action<HeroInstance[]> onBatallar)
        {
            _onBatallar = onBatallar;

            LoadLastTeam();

            if (_txtTitulo  != null) _txtTitulo.text  = titulo;
            if (_txtEnergia != null) _txtEnergia.text = $"Energía: {energyCost}";

            BuildEnemyCards(enemies);
            BuildHeroCards();
            RefreshSlots();
            RefreshHeroCardIndicators();

            if (_btnBatallar != null) _btnBatallar.onClick.AddListener(OnBatallar);
            if (_btnCancelar != null) _btnCancelar.onClick.AddListener(OnCancelar);
        }

        // ── Carga del último equipo ────────────────────────────────────────────

        private void LoadLastTeam()
        {
            var pd = PlayerDataSystem.Instance?.GetPlayerData();
            if (pd?.lastTeam == null) return;

            _selectedCount = 0;
            for (int i = 0; i < pd.lastTeam.Length && _selectedCount < 4; i++)
            {
                string id = pd.lastTeam[i];
                if (string.IsNullOrEmpty(id)) continue;
                bool enRoster = pd.heroes?.Exists(h => h.heroId == id) ?? false;
                if (enRoster) _selectedIds[_selectedCount++] = id;
            }
        }

        // ── Tarjetas de enemigos ──────────────────────────────────────────────

        private void BuildEnemyCards(EnemyInstance[] enemies)
        {
            if (_contenedorEnemigos == null || enemies == null) return;

            foreach (var enemy in enemies)
            {
                string capturedId = enemy.enemyId;

                var card  = new GameObject($"ECard_{enemy.enemyId}");
                card.transform.SetParent(_contenedorEnemigos, false);

                var cardImg = card.AddComponent<Image>();
                cardImg.color = ElementColor(enemy.elemento) * 0.75f;

                var cardRT = card.GetComponent<RectTransform>();
                cardRT.sizeDelta = new Vector2(80f, 90f);

                // Icono
                var iconGO  = new GameObject("Icon");
                iconGO.transform.SetParent(card.transform, false);
                var iconImg = iconGO.AddComponent<Image>();
                var sprite  = PlaceholderAssets.GetEnemySprite("generico");
                iconImg.sprite = sprite;
                iconImg.color  = sprite != null ? Color.white : ElementColor(enemy.elemento);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.10f, 0.38f);
                iconRT.anchorMax = new Vector2(0.90f, 0.97f);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;

                // Nombre
                var nameGO  = new GameObject("Nombre");
                nameGO.transform.SetParent(card.transform, false);
                var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
                nameTxt.text      = enemy.nombre;
                nameTxt.fontSize  = 9f;
                nameTxt.color     = Color.white;
                nameTxt.alignment = TextAlignmentOptions.Center;
                var nameRT = nameGO.GetComponent<RectTransform>();
                nameRT.anchorMin = new Vector2(0f, 0.20f);
                nameRT.anchorMax = new Vector2(1f, 0.40f);
                nameRT.offsetMin = new Vector2(2, 0);
                nameRT.offsetMax = new Vector2(-2, 0);

                // Nivel + elemento
                var lvlGO  = new GameObject("Nivel");
                lvlGO.transform.SetParent(card.transform, false);
                var lvlTxt = lvlGO.AddComponent<TextMeshProUGUI>();
                lvlTxt.text      = $"Nv.{enemy.nivel}";
                lvlTxt.fontSize  = 9f;
                lvlTxt.color     = new Color(0.7f, 0.9f, 1f);
                lvlTxt.alignment = TextAlignmentOptions.Center;
                var lvlRT = lvlGO.GetComponent<RectTransform>();
                lvlRT.anchorMin = new Vector2(0f, 0.02f);
                lvlRT.anchorMax = new Vector2(1f, 0.22f);
                lvlRT.offsetMin = Vector2.zero;
                lvlRT.offsetMax = Vector2.zero;
            }
        }

        // ── Tarjetas de héroes (roster) ───────────────────────────────────────

        private void BuildHeroCards()
        {
            if (_contenedorHeroes == null) return;

            var pd = PlayerDataSystem.Instance?.GetPlayerData();

            if (pd?.heroes == null || pd.heroes.Count == 0)
            {
                var ph = new GameObject("NoHeroes");
                ph.transform.SetParent(_contenedorHeroes, false);
                var txt = ph.AddComponent<TextMeshProUGUI>();
                txt.text      = "Sin héroes en el roster.";
                txt.fontSize  = 14f;
                txt.color     = new Color(0.7f, 0.7f, 0.7f);
                txt.alignment = TextAlignmentOptions.MidlineLeft;
                var le = ph.AddComponent<LayoutElement>();
                le.preferredWidth = 220f;
                le.flexibleHeight = 1f;
                return;
            }

            var gs  = GearSystem.Instance;
            var hps = HeroProgressionSystem.Instance;

            foreach (var heroData in pd.heroes)
            {
                string capturedId = heroData.heroId;

                HeroInstance hi = null;
                if (gs != null && hps != null)
                    hi = gs.BuildCombatInstance(heroData.heroId);
                if (hi == null && hps != null)
                    hi = hps.BuildHeroInstance(heroData.heroId, heroData.level);

                string elemento = hi?.elemento ?? "Oscuridad";

                // Tarjeta vertical (portrait)
                var card    = new GameObject($"HCard_{heroData.heroId}");
                card.transform.SetParent(_contenedorHeroes, false);
                var cardImg = card.AddComponent<Image>();
                cardImg.color = ElementColor(elemento) * 0.70f;
                card.AddComponent<Button>().onClick.AddListener(() => OnHeroTapped(capturedId));
                var le = card.AddComponent<LayoutElement>();
                le.preferredWidth  = 80f;
                le.flexibleHeight  = 1f;

                // Icono
                var iconGO  = new GameObject("Icon");
                iconGO.transform.SetParent(card.transform, false);
                var iconImg = iconGO.AddComponent<Image>();
                var sprite  = PlaceholderAssets.GetHeroPortrait(elemento);
                iconImg.sprite = sprite;
                iconImg.color  = sprite != null ? Color.white : ElementColor(elemento);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.07f, 0.40f);
                iconRT.anchorMax = new Vector2(0.93f, 0.95f);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;

                // Nombre
                var nameGO  = new GameObject("Nombre");
                nameGO.transform.SetParent(card.transform, false);
                var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
                nameTxt.text      = heroData.heroId;
                nameTxt.fontSize  = 8f;
                nameTxt.color     = Color.white;
                nameTxt.alignment = TextAlignmentOptions.Center;
                var nameRT = nameGO.GetComponent<RectTransform>();
                nameRT.anchorMin = new Vector2(0f, 0.22f);
                nameRT.anchorMax = new Vector2(1f, 0.40f);
                nameRT.offsetMin = new Vector2(2, 0);
                nameRT.offsetMax = new Vector2(-2, 0);

                // Nivel
                var lvlGO  = new GameObject("Nivel");
                lvlGO.transform.SetParent(card.transform, false);
                var lvlTxt = lvlGO.AddComponent<TextMeshProUGUI>();
                lvlTxt.text      = $"Nv.{heroData.level}";
                lvlTxt.fontSize  = 8f;
                lvlTxt.color     = new Color(0.8f, 0.8f, 0.5f);
                lvlTxt.alignment = TextAlignmentOptions.Center;
                var lvlRT = lvlGO.GetComponent<RectTransform>();
                lvlRT.anchorMin = new Vector2(0f, 0.04f);
                lvlRT.anchorMax = new Vector2(1f, 0.24f);
                lvlRT.offsetMin = Vector2.zero;
                lvlRT.offsetMax = Vector2.zero;

                // Overlay de selección (tinte verde semitransparente)
                var selGO  = new GameObject("SelOverlay");
                selGO.transform.SetParent(card.transform, false);
                var selImg = selGO.AddComponent<Image>();
                selImg.color = new Color(0.20f, 0.90f, 0.30f, 0.38f);
                var selRT = selGO.GetComponent<RectTransform>();
                selRT.anchorMin = Vector2.zero;
                selRT.anchorMax = Vector2.one;
                selRT.offsetMin = Vector2.zero;
                selRT.offsetMax = Vector2.zero;
                selGO.SetActive(false);

                // Número de slot (se muestra sobre el icono cuando está seleccionado)
                var numGO  = new GameObject("SlotNum");
                numGO.transform.SetParent(card.transform, false);
                var numTxt = numGO.AddComponent<TextMeshProUGUI>();
                numTxt.text      = "";
                numTxt.fontSize  = 22f;
                numTxt.fontStyle = FontStyles.Bold;
                numTxt.color     = Color.white;
                numTxt.alignment = TextAlignmentOptions.Center;
                var numRT = numGO.GetComponent<RectTransform>();
                numRT.anchorMin = new Vector2(0f, 0.40f);
                numRT.anchorMax = new Vector2(1f, 0.97f);
                numRT.offsetMin = Vector2.zero;
                numRT.offsetMax = Vector2.zero;
            }
        }

        // ── Interacción ───────────────────────────────────────────────────────

        private void OnHeroTapped(string heroId)
        {
            if (IsSelected(heroId))
            {
                int idx = IndexOf(heroId);
                for (int i = idx; i < _selectedCount - 1; i++)
                    _selectedIds[i] = _selectedIds[i + 1];
                _selectedIds[--_selectedCount] = null;
            }
            else
            {
                if (_selectedCount >= 4) return;
                _selectedIds[_selectedCount++] = heroId;
            }

            RefreshSlots();
            RefreshHeroCardIndicators();
        }

        private void OnSlotTapped(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _selectedCount) return;
            for (int i = slotIndex; i < _selectedCount - 1; i++)
                _selectedIds[i] = _selectedIds[i + 1];
            _selectedIds[--_selectedCount] = null;
            RefreshSlots();
            RefreshHeroCardIndicators();
        }

        // ── Actualización de slots ────────────────────────────────────────────

        private void RefreshSlots()
        {
            if (_slotButtons == null) return;

            for (int i = 0; i < _slotButtons.Length; i++)
            {
                if (_slotButtons[i] == null) continue;

                bool   ocupado = i < _selectedCount && !string.IsNullOrEmpty(_selectedIds[i]);
                string id      = ocupado ? _selectedIds[i] : null;

                // Fondo del slot
                var img = _slotButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = ocupado
                        ? new Color(0.08f, 0.38f, 0.18f)
                        : new Color(0.06f, 0.10f, 0.18f);

                // Icono de héroe (cuadrado de color de elemento)
                var iconT   = _slotButtons[i].transform.Find("SlotIcon");
                var iconImg = iconT?.GetComponent<Image>();

                // Label
                var labelT = _slotButtons[i].transform.Find("Label");
                var txt    = labelT?.GetComponent<TMP_Text>();

                if (ocupado)
                {
                    var pd = PlayerDataSystem.Instance?.GetPlayerData();
                    var hd = pd?.heroes?.Find(h => h.heroId == id);

                    HeroInstance hi = null;
                    var hps = HeroProgressionSystem.Instance;
                    if (hps != null)
                        hi = hps.BuildHeroInstance(id, hd?.level ?? 1);

                    string elemento = hi?.elemento ?? "Oscuridad";

                    if (iconImg != null)
                    {
                        iconImg.color = ElementColor(elemento);
                        iconImg.gameObject.SetActive(true);
                    }

                    if (txt != null)
                        txt.text = $"<b><size=11>{id}</size></b>\n" +
                                   $"<size=9>Nv.{hd?.level ?? 0}</size>";
                }
                else
                {
                    if (iconImg != null) iconImg.gameObject.SetActive(false);

                    if (txt != null)
                        txt.text = $"<size=9><color=#555555>RANURA {i + 1}</color></size>\n" +
                                   "<size=10>— libre —</size>";
                }

                int capturedSlot = i;
                _slotButtons[i].onClick.RemoveAllListeners();
                if (ocupado)
                    _slotButtons[i].onClick.AddListener(() => OnSlotTapped(capturedSlot));
            }
        }

        private void RefreshHeroCardIndicators()
        {
            if (_contenedorHeroes == null) return;

            // Mapa heroId → número de slot (1-based)
            var slotOrder = new Dictionary<string, int>();
            for (int i = 0; i < _selectedCount; i++)
                if (!string.IsNullOrEmpty(_selectedIds[i]))
                    slotOrder[_selectedIds[i]] = i + 1;

            foreach (Transform card in _contenedorHeroes)
            {
                if (!card.name.StartsWith("HCard_")) continue;
                string heroId = card.name.Substring(6);
                bool   sel    = IsSelected(heroId);

                var selOverlay = card.Find("SelOverlay");
                var slotNumT   = card.Find("SlotNum");

                if (selOverlay != null) selOverlay.gameObject.SetActive(sel);
                if (slotNumT != null)
                {
                    var t = slotNumT.GetComponent<TMP_Text>();
                    if (t != null)
                        t.text = sel && slotOrder.TryGetValue(heroId, out int n) ? n.ToString() : "";
                }
            }
        }

        // ── Confirmar / Cancelar ──────────────────────────────────────────────

        private void OnBatallar()
        {
            if (_selectedCount == 0)
            {
                Debug.LogWarning("[BattlePrepPanel] Ningún héroe seleccionado.");
                return;
            }

            SaveLastTeam();
            var team = BuildSelectedTeam();
            UIManager.Instance?.HideOverlay(gameObject);
            _onBatallar?.Invoke(team);
        }

        private void OnCancelar()
        {
            UIManager.Instance?.HideOverlay(gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SaveLastTeam()
        {
            var pd = PlayerDataSystem.Instance?.GetPlayerData();
            if (pd == null) return;
            pd.lastTeam = new string[4];
            for (int i = 0; i < 4; i++)
                pd.lastTeam[i] = i < _selectedCount ? _selectedIds[i] : null;
            PlayerDataSystem.Instance?.MarkDirty();
        }

        private HeroInstance[] BuildSelectedTeam()
        {
            var gs  = GearSystem.Instance;
            var hps = HeroProgressionSystem.Instance;
            var pd  = PlayerDataSystem.Instance?.GetPlayerData();
            var result = new List<HeroInstance>();

            for (int i = 0; i < _selectedCount; i++)
            {
                string id = _selectedIds[i];
                if (string.IsNullOrEmpty(id)) continue;

                HeroInstance hi = null;
                if (gs != null && hps != null)
                    hi = gs.BuildCombatInstance(id);
                if (hi == null && hps != null)
                {
                    var hd = pd?.heroes?.Find(h => h.heroId == id);
                    hi = hps.BuildHeroInstance(id, hd?.level ?? 1);
                }
                if (hi != null) result.Add(hi);
            }

            return result.ToArray();
        }

        private bool IsSelected(string heroId)
        {
            for (int i = 0; i < _selectedCount; i++)
                if (_selectedIds[i] == heroId) return true;
            return false;
        }

        private int IndexOf(string heroId)
        {
            for (int i = 0; i < _selectedCount; i++)
                if (_selectedIds[i] == heroId) return i;
            return -1;
        }

        private static Color ElementColor(string elemento)
        {
            switch (elemento?.ToLower())
            {
                case "fuego":      return new Color(0.80f, 0.20f, 0.05f);
                case "agua":       return new Color(0.10f, 0.35f, 0.90f);
                case "tierra":     return new Color(0.45f, 0.28f, 0.10f);
                case "naturaleza": return new Color(0.10f, 0.55f, 0.10f);
                case "luz":        return new Color(0.90f, 0.90f, 0.20f);
                case "rayo":       return new Color(0.20f, 0.75f, 0.95f);
                case "hielo":      return new Color(0.55f, 0.80f, 0.97f);
                default:           return new Color(0.12f, 0.04f, 0.22f);
            }
        }
    }
}
