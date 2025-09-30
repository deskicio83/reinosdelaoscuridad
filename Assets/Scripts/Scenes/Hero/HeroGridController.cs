/*
============================================================
HeroGridController.cs — Grid de cartas de héroe + selección
------------------------------------------------------------
PROPÓSITO
- Construir el grid, gestionar selección exclusiva y notificar detalle.

RESPONSABILIDADES
- Persistir selección por heroId, hacer scroll al seleccionado, animar construcción del grid.

DEPENDENCIAS
- HeroCardUI, HeroStatsPanelController, HeroDetailTabMenuController,
  GameDataManager, HeroCatalogManager, DOTween (animaciones).

REFERENCIAS (INSPECTOR)
- ScrollRect scrollRect, GridLayoutGroup gridLayoutGroup, contenedor de cartas.

MÉTODOS PRINCIPALES (LOS RELEVANTES Y PATCHES APLICADOS)
- SetHeroes(List<HeroProgress> heroes, int maxSpaces): establece data y reconstruye.
- RefreshGrid(int sortType): reconstruye/ordena.
- PopulateGrid(List<HeroProgress>): instancia HeroCardUI; **al final llama AnimateAllCardsOnBuild()**.
- OnHeroSelected(HeroProgress, HeroCatalogEntry): actualiza paneles → *guarda _selectedHeroId*.
- UpdateSelectedCardByIndex(int index, bool scroll=false): marca overlay y opcionalmente hace scroll.
- IEnumerator SelectFirstHeroNextFrame(List<HeroProgress>): restaura selección por heroId o el primero. Hace scroll.
- SelectByHeroId(string heroId, bool scroll=true): API pública para reseleccionar.
- RestoreSelectionAndFocus(): reselección y scroll al abrir/volver.
- ScrollToSelected(float duration=0.25f): calcula verticalNormalizedPosition y anima con DOTween.
- AnimateCardAppear(GameObject card, float delay): fade (si CanvasGroup) + scale-in suave.
- AnimateAllCardsOnBuild(): recorre todas las cartas y aplica escalonado.

NOTAS
- DOTween requerido (using DG.Tweening;).
- Si las cartas no tienen CanvasGroup, el fade se omite (no se añade en runtime).
============================================================
*/


using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using DG.Tweening;

public class HeroGridController : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject heroCardPrefab;
    public GameObject heroCardEmptyPrefab;
    public GameObject heroCardAddPrefab;
    public Transform gridParent;
    public ScrollRect scrollRect;

    public GameObject panelInfoHeroe;
    public HeroStatsPanelController statsPanelController;
    public HeroEquipmentPanelUI equipmentPanelUI;
    public GameObject panelEquipoHeroe;

    public HeroSceneController heroSceneController;
    public HeroDetailTabMenuController tabMenuController;

    private List<HeroProgress> _allHeroes = new List<HeroProgress>();
    private List<GameObject> heroCards = new List<GameObject>();
    private int _maxHeroSpaces = 0;

    private HeroCardUI firstHeroCardUI = null;

    private HeroProgress currentHero;
    public HeroProgress CurrentHero => currentHero;
    private HeroCatalogEntry currentHeroCatalog;

    [Header("Referencias a Panel Skills")]
    public HeroSkillsPanelController heroSkillsPanelController;

    [Header("Referencias a FBX")]
    public Hero3DPanelController hero3DPanelController;
    public GameObject defaultHeroPrefab;
    [Header("Animación de construcción")]
    public bool animateBuild = true;
    public float buildStagger = 0.015f; // retraso incremental por carta

    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private GridCellResizer gridCellResizer;

    // Evento externo por si alguien quiere engancharse
    public Action<HeroProgress> onCardSelected;

    // Índice de la card seleccionada actualmente (en la lista heroesList)
    private int _selectedCardIndex = -1;
    private Coroutine _statsSwapCo;
    private string _selectedHeroId = null;
    public string SelectedHeroId => _selectedHeroId;
    private Tween _scrollTween;
    void Start()
    {
        if (statsPanelController != null) statsPanelController.gameObject.SetActive(false);
        if (equipmentPanelUI != null) equipmentPanelUI.gameObject.SetActive(false);
        if (panelEquipoHeroe != null) panelEquipoHeroe.SetActive(false);
        if (panelInfoHeroe != null) panelInfoHeroe.SetActive(true);
    }

    private void SetPanelInfoHeroeActive(bool value)
    {
        if (panelInfoHeroe != null) panelInfoHeroe.SetActive(value);
    }

    public void SetHeroes(List<HeroProgress> heroList, int maxHeroSpaces = 0)
    {
        _allHeroes = heroList != null ? new List<HeroProgress>(heroList) : new List<HeroProgress>();
        if (maxHeroSpaces > 0) _maxHeroSpaces = maxHeroSpaces;
        RefreshGrid(0);
    }
    private void AnimateCardAppear(GameObject cardGO, float delay)
    {
        if (cardGO == null) return;

        var rt = cardGO.transform as RectTransform;
        var cg = cardGO.GetComponent<CanvasGroup>(); // NO añadimos si falta

        // Estado inicial
        if (rt != null)
        {
            rt.DOKill();
            rt.localScale = Vector3.one * 0.92f;
        }
        if (cg != null)
        {
            cg.DOKill();
            cg.alpha = 0f;
        }

        // Animación
        if (cg != null)
            cg.DOFade(1f, 0.18f).SetEase(Ease.OutSine).SetDelay(delay).SetUpdate(true);

        if (rt != null)
            rt.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetDelay(delay).SetUpdate(true);
    }

    private void AnimateAllCardsOnBuild()
    {
        if (!animateBuild) return;

        // Si tienes heroCards, úsalo. Si no, descubre por componentes:
        var list = new System.Collections.Generic.List<GameObject>();
        if (heroCards != null && heroCards.Count > 0)
        {
            list.AddRange(heroCards);
        }
        else
        {
            var uis = GetComponentsInChildren<HeroCardUI>(true);
            foreach (var ui in uis) list.Add(ui.gameObject);
        }

        for (int i = 0; i < list.Count; i++)
            AnimateCardAppear(list[i], i * buildStagger);
    }

    public void RefreshGrid(int sortType)
    {
        List<HeroProgress> sorted = _allHeroes;
        switch (sortType)
        {
            case 0: sorted = _allHeroes.OrderByDescending(h => h.stars).ToList(); break;
            case 1: sorted = _allHeroes.OrderByDescending(h => h.level).ToList(); break;
            case 2: sorted = _allHeroes.OrderByDescending(h => h.favorite ? 1 : 0).ToList(); break;
            case 3: sorted = _allHeroes.OrderByDescending(h => HeroCatalogManager.Instance?.GetHeroById(h.heroId)?.baseStars ?? 0).ToList(); break;
            case 4: sorted = _allHeroes.OrderBy(h => HeroCatalogManager.Instance?.GetHeroById(h.heroId)?.element ?? "").ToList(); break;
            case 5: sorted = _allHeroes.OrderBy(h => HeroCatalogManager.Instance?.GetHeroById(h.heroId)?.classStandard ?? "").ToList(); break;
            case 6: sorted = _allHeroes.OrderBy(h => HeroCatalogManager.Instance?.GetHeroById(h.heroId)?.reino ?? "").ToList(); break;
            default: sorted = _allHeroes.OrderByDescending(h => h.stars).ToList(); break;
        }
        PopulateGrid(sorted);
    }

    // Vuelve a pintar SOLO la card del héroe indicado (sin reconstruir el grid)
    public void RefreshCardStars(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            Debug.LogWarning("[Grid] RefreshCardStars: heroId vacío.");
            return;
        }

        // El HeroProgress ya debería estar actualizado (awaken = true)
        var hp = _allHeroes?.FirstOrDefault(h => h.heroId == heroId);
        if (hp == null)
        {
            Debug.LogWarning($"[Grid] RefreshCardStars: no se encuentra HeroProgress para heroId={heroId}.");
            return;
        }

        bool refreshed = false;
        foreach (var go in heroCards)
        {
            var ui = go != null ? go.GetComponent<HeroCardUI>() : null;
            if (ui != null && ui.HeroId == heroId)
            {
                Debug.Log($"[Grid] RefreshCardStars: repintando card de heroId={heroId} (awaken={hp.awaken}).");
                ui.UpdateProgress(hp);   // re-render de estrellas (usa prefab morado si awaken=true)
                refreshed = true;
                break;
            }
        }

        if (!refreshed)
            Debug.LogWarning($"[Grid] RefreshCardStars: no se encontró ninguna card con heroId={heroId} en la parrilla actual.");
    }

    public void SortHeroesBy(int sortType) => RefreshGrid(sortType);

    public void PopulateGrid(List<HeroProgress> heroesList)
    {
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        heroCards.Clear();

        int espaciosDisponibles = _maxHeroSpaces > 0 ? _maxHeroSpaces : heroesList.Count;
        int totalHeroes = heroesList.Count;
        int emptySlots = Mathf.Max(espaciosDisponibles - totalHeroes, 0);

        firstHeroCardUI = null;

        // Instancia héroes
        for (int i = 0; i < totalHeroes; i++)
        {
            var hero = heroesList[i];
            var heroCatalog = HeroCatalogManager.Instance?.GetHeroById(hero.heroId);

            var cardGO = Instantiate(heroCardPrefab, gridParent);
            heroCards.Add(cardGO);

            var cardUI = cardGO.GetComponent<HeroCardUI>();
            if (cardUI != null)
            {
                cardUI.SetRuntimeIndex(i); // <-- ¡clave!
                int capturedIndex = i;     // cerrar correctamente el índice en la lambda

                cardUI.Setup(hero, () =>
                {
                    OnHeroSelected(hero, heroCatalog);
                    UpdateSelectedCardByIndex(capturedIndex, scroll: true);  // <-- con scroll al seleccionar
                });
            }
        }

        // Slots vacíos
        for (int i = 0; i < emptySlots; i++)
        {
            if (heroCardEmptyPrefab != null)
            {
                var emptyCard = Instantiate(heroCardEmptyPrefab, gridParent);
                heroCards.Add(emptyCard);
            }
        }

        // Slot "+"
        if (heroCardAddPrefab != null)
        {
            var addCard = Instantiate(heroCardAddPrefab, gridParent);
            heroCards.Add(addCard);
            Button addBtn = addCard.GetComponent<Button>();
            if (addBtn != null && heroSceneController != null)
            {
                addBtn.onClick.RemoveAllListeners();
                addBtn.onClick.AddListener(() => heroSceneController.OnAddSpaceHeroBtnClicked());
            }
        }

        // Selección inicial diferida un frame para que todo el UI esté montado
        StartCoroutine(SelectFirstHeroNextFrame(heroesList));

        StartCoroutine(AdjustScrollPositionCoroutine());
        AnimateAllCardsOnBuild();
    }
    private void UpdateSelectedCardByIndex(int selectedIndex, bool scroll = false)
    {
        _selectedCardIndex = selectedIndex;
        string newSelectedHeroId = null;

        for (int c = 0; c < heroCards.Count; c++)
        {
            var ui = heroCards[c] ? heroCards[c].GetComponent<HeroCardUI>() : null;
            if (ui == null) continue;

            bool isSelected = (ui.RuntimeIndex == selectedIndex);
            ui.SetSelected(isSelected);
            if (isSelected) newSelectedHeroId = ui.HeroId;
        }

        if (!string.IsNullOrEmpty(newSelectedHeroId))
            _selectedHeroId = newSelectedHeroId;

        if (scroll) ScrollToSelected();
    }

    private void OnHeroSelected(HeroProgress hero, HeroCatalogEntry heroCatalog)
    {
        _selectedHeroId = hero?.heroId;
        Debug.Log($"[Grid] OnHeroSelected (local) heroId={hero?.heroId}");
        currentHero = hero;
        currentHeroCatalog = heroCatalog;

        // Tab de detalle
        if (tabMenuController != null)
            tabMenuController.SetHero(hero, heroCatalog);

        // --- Oculta SIEMPRE cualquier panel transitorio de bonus ---
        if (statsPanelController != null)
            statsPanelController.HideSetBonus();

        // --- Stats con transición local (no afecta a otras animaciones) ---
        if (statsPanelController != null && heroCatalog != null)
        {
            statsPanelController.gameObject.SetActive(true);
            statsPanelController.ShowChildPanels();
            StartCoroutine(SwapStatsSmooth(hero, heroCatalog));
        }

        // Skills
        if (heroSkillsPanelController != null && heroCatalog != null)
        {
            var skills = heroCatalog.skills;
            var playerSkills = hero.skills;
            heroSkillsPanelController.SetSkills(skills, playerSkills);
        }

        // Modelo 3D
        if (hero3DPanelController != null)
            LoadHero3DPrefab(heroCatalog);

        // Equipo (pasa el héroe para fijar heroId correctamente incluso con equipo vacío)
        if (panelEquipoHeroe != null && equipmentPanelUI != null && currentHero != null)
            equipmentPanelUI.SetEquipmentForHero(currentHero, null);

        // Notifica al Scene y a listeners externos
        if (heroSceneController != null)
            heroSceneController.OnHeroSelected(hero);

        onCardSelected?.Invoke(hero);
    }


    public System.Collections.IEnumerator BuildGridAsync(
        System.Collections.Generic.List<HeroProgress> heroes,
        int maxSpaces,
        System.Action<float, string> onProgress = null
    )
    {
        // Limpia grid y estados como hagas normalmente…
        // ClearGrid();  // <- si tienes una función así, úsala

        int total = heroes?.Count ?? 0;
        int created = 0;

        // Crea las cartas de héroe en lotes para permitir refresco de UI
        const int batch = 8; // cada 8 cartas hacemos un yield
        for (int i = 0; i < total; i++)
        {
            var hp = heroes[i];

            // ⬇️ TU LÓGICA REAL AQUÍ:
            // CreateOrReuseCard(hp);
            // Setup card, registrar callbacks, etc.

            created++;
            if (onProgress != null)
            {
                float p = (float)created / Mathf.Max(1, total);
                onProgress(p, $"Pintando cartas {created}/{total}…");
            }

            if ((created % batch) == 0)
                yield return null;
        }

        // Rellena huecos/espacios vacíos si procede…
        // FillEmptySpaces(maxSpaces - total);

        // Asegura 100%
        onProgress?.Invoke(1f, $"Pintando cartas {created}/{total}…");
    }


    // Carga el prefab 3D correcto (awaken si existe; si no, normal; si no, default; si no, limpia)
    private void LoadHero3DPrefab(HeroCatalogEntry heroCatalog)
    {
        if (hero3DPanelController == null) return;

        bool isAwaken = currentHero != null && currentHero.awaken;
        string awakenKey = (isAwaken && heroCatalog != null) ? heroCatalog.modelAddressableAwaken : null;
        string normalKey = heroCatalog != null ? heroCatalog.modelAddressable : null;

        // 1) Intentar awaken SOLO si el héroe está despierto y hay key
        if (!string.IsNullOrEmpty(awakenKey))
        {
            Addressables.LoadResourceLocationsAsync(awakenKey).Completed += locAw =>
            {
                if (locAw.Status == AsyncOperationStatus.Succeeded && locAw.Result != null && locAw.Result.Count > 0)
                {
                    Addressables.LoadAssetAsync<GameObject>(awakenKey).Completed += hAw =>
                    {
                        if (hAw.Status == AsyncOperationStatus.Succeeded)
                            hero3DPanelController.ShowHero(hAw.Result, heroCatalog, currentHero);
                        else
                            LoadNormal();
                    };
                }
                else
                {
                    // No existe awaken -> normal
                    LoadNormal();
                }
            };
        }
        else
        {
            // No procede awaken -> normal
            LoadNormal();
        }

        void LoadNormal()
        {
            if (!string.IsNullOrEmpty(normalKey))
            {
                Addressables.LoadResourceLocationsAsync(normalKey).Completed += locN =>
                {
                    if (locN.Status == AsyncOperationStatus.Succeeded && locN.Result != null && locN.Result.Count > 0)
                    {
                        Addressables.LoadAssetAsync<GameObject>(normalKey).Completed += hN =>
                        {
                            if (hN.Status == AsyncOperationStatus.Succeeded)
                                hero3DPanelController.ShowHero(hN.Result, heroCatalog, currentHero);
                            else
                                ShowDefaultOrClear();
                        };
                    }
                    else
                    {
                        ShowDefaultOrClear();
                    }
                };
            }
            else
            {
                ShowDefaultOrClear();
            }
        }

        void ShowDefaultOrClear()
        {
            if (defaultHeroPrefab != null)
                hero3DPanelController.ShowHero(defaultHeroPrefab, heroCatalog, currentHero);
            else
                hero3DPanelController.ClearHero();
        }
    }



    // NUEVO: permite refrescar el modelo 3D del héroe actualmente seleccionado
    public void Refresh3DForCurrent()
    {
        if (currentHeroCatalog != null)
            LoadHero3DPrefab(currentHeroCatalog);
    }



    public void RefreshEquipmentPanel()
    {
        if (currentHero != null && equipmentPanelUI != null)
        {
            equipmentPanelUI.gameObject.SetActive(true);
            panelEquipoHeroe?.SetActive(true);
            equipmentPanelUI.SetEquipment(currentHero.equipment, (gear, slotType) =>
            {
                Debug.Log($"[HeroGridController] Click en slot {slotType} con gear {gear?.gearId ?? "VACIO"}");
            });
        }
    }

    private IEnumerator SetHeroWithDelay(HeroProgress hero, HeroCatalogEntry heroCatalog)
    {
        yield return null;
        statsPanelController.SetHero(hero, heroCatalog);
    }

    private IEnumerator AdjustScrollPositionCoroutine()
    {
        yield return null;
        if (gridParent != null && scrollRect != null && scrollRect.viewport != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnValidate()
    {
        if (gridParent != null && gridLayoutGroup == null) gridLayoutGroup = gridParent.GetComponent<GridLayoutGroup>();
        if (gridParent != null && gridCellResizer == null) gridCellResizer = gridParent.GetComponent<GridCellResizer>();
    }

    public void RebindTo(RectTransform newGridParent, GridLayoutGroup newLayout = null, GridCellResizer newResizer = null)
    {
        gridParent = newGridParent;
        gridLayoutGroup = newLayout ?? (newGridParent != null ? newGridParent.GetComponent<GridLayoutGroup>() : null);
        gridCellResizer = newResizer ?? (newGridParent != null ? newGridParent.GetComponent<GridCellResizer>() : null);
    }

    public void SetColumns(int columns)
    {
        if (gridParent == null)
        {
            Debug.LogWarning("[HeroGridController] gridParent no asignado para SetColumns.");
            return;
        }

        if (gridLayoutGroup == null) gridLayoutGroup = gridParent.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup == null)
        {
            Debug.LogWarning("[HeroGridController] GridLayoutGroup no encontrado para SetColumns.");
            return;
        }

        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = Mathf.Max(1, columns);

        if (gridCellResizer != null)
        {
            gridCellResizer.SetColumns(columns, true);
            gridCellResizer.ResizeNow();
        }

        Debug.Log($"[HeroGridController] Columnas ajustadas a {columns}.");
    }
    private IEnumerator SwapStatsSmooth(HeroProgress hero, HeroCatalogEntry heroCatalog)
    {
        // Evita solapamientos
        if (_statsSwapCo != null) StopCoroutine(_statsSwapCo);
        _statsSwapCo = StartCoroutine(SwapCoroutine());
        yield break;

        IEnumerator SwapCoroutine()
        {
            if (statsPanelController == null) yield break;

            // CanvasGroup LOCAL (solo en el GO del panel de stats)
            var go = statsPanelController.gameObject;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            // Fade-out rapidito
            float t = 0f, outDur = 0.08f;
            float start = cg.alpha;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(start, 0f, t / outDur);
                yield return null;
            }
            cg.alpha = 0f;

            // Refresca contenido ya en negro
            statsPanelController.SetHero(hero, heroCatalog);

            // Fade-in un pelo más largo
            t = 0f;
            float inDur = 0.18f;
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / inDur);
                yield return null;
            }
            cg.alpha = 1f;

            _statsSwapCo = null;
        }
    }

    private IEnumerator SelectFirstHeroNextFrame(List<HeroProgress> heroesList)
    {
        // Espera a que el Canvas/Layout termine
        yield return null;

        // Intenta restaurar selección previa por heroId
        if (!string.IsNullOrEmpty(_selectedHeroId))
        {
            int idx = heroesList.FindIndex(h => h.heroId == _selectedHeroId);
            if (idx >= 0)
            {
                var hero = heroesList[idx];
                var heroCatalog = HeroCatalogManager.Instance?.GetHeroById(hero.heroId);
                Debug.Log($"[Grid] RESTORE SELECT heroId={hero.heroId} idx={idx}");
                OnHeroSelected(hero, heroCatalog);
                UpdateSelectedCardByIndex(idx, scroll: true);
                yield break;
            }
        }

        // Fallback: primero de la lista
        var firstHero = heroesList.FirstOrDefault();
        if (firstHero != null)
        {
            var heroCatalog = HeroCatalogManager.Instance?.GetHeroById(firstHero.heroId);
            Debug.Log($"[Grid] SELECT (deferred) heroId={firstHero.heroId}");
            OnHeroSelected(firstHero, heroCatalog);
            UpdateSelectedCardByIndex(0, scroll: true);
        }
        else
        {
            // Sin héroes → UI segura
            if (tabMenuController != null) tabMenuController.ShowInfoTab();
            if (hero3DPanelController != null) hero3DPanelController.ClearHero();
            if (heroSceneController != null) heroSceneController.OnHeroDeletedRefreshUI();
        }
    }
    public void SelectByHeroId(string heroId, bool scroll = true)
    {
        if (string.IsNullOrEmpty(heroId) || _allHeroes == null) return;
        int idx = _allHeroes.FindIndex(h => h.heroId == heroId);
        if (idx < 0) { if (_allHeroes.Count == 0) return; idx = 0; }

        var hero = _allHeroes[idx];
        var cat  = HeroCatalogManager.Instance?.GetHeroById(hero.heroId);

        OnHeroSelected(hero, cat);
        UpdateSelectedCardByIndex(idx, scroll);
    }

    public void RestoreSelectionAndFocus()
    {
        if (!string.IsNullOrEmpty(_selectedHeroId)) SelectByHeroId(_selectedHeroId, scroll: true);
        else if (_allHeroes != null && _allHeroes.Count > 0) SelectByHeroId(_allHeroes[0].heroId, scroll: true);
    }

    public void ScrollToSelected(float duration = 0.25f)
    {
        if (scrollRect == null || gridLayoutGroup == null) return;
        if (_selectedCardIndex < 0) return;

        int columns = Mathf.Max(1, gridLayoutGroup.constraintCount);
        int totalRows = Mathf.CeilToInt(heroCards.Count / (float)columns);
        int row = Mathf.FloorToInt(_selectedCardIndex / (float)columns);

        float targetV = (totalRows <= 1) ? 1f : 1f - Mathf.Clamp01(row / (float)(totalRows - 1));

        if (duration <= 0f)
        {
            scrollRect.verticalNormalizedPosition = targetV;
        }
        else
        {
            _scrollTween?.Kill();
            _scrollTween = DOTween.To(
                () => scrollRect.verticalNormalizedPosition,
                v => scrollRect.verticalNormalizedPosition = v,
                targetV,
                duration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
        }

        Debug.Log($"[HeroGrid] ScrollToSelected idx={_selectedCardIndex} row={row}/{totalRows} v={targetV:0.00}");
    }



}
