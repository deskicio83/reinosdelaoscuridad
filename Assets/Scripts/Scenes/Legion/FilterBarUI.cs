// Assets/Scripts/Scenes/Legion/FilterBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using System;

public class FilterBarUI : MonoBehaviour
{
    [Header("Botones de elemento (asignar en inspector)")]
    public Button btnFuego;
    public Button btnAgua;
    public Button btnNaturaleza;
    public Button btnLuz;
    public Button btnOscuridad;
    public Button btnReset;

    public event Action<FilterData> OnFilterChanged;

    private void Awake()
    {
        if (btnFuego) btnFuego.onClick.AddListener(() => EmitFilter("Fuego"));
        if (btnAgua) btnAgua.onClick.AddListener(() => EmitFilter("Agua"));
        if (btnNaturaleza) btnNaturaleza.onClick.AddListener(() => EmitFilter("Naturaleza"));
        if (btnLuz) btnLuz.onClick.AddListener(() => EmitFilter("Luz"));
        if (btnOscuridad) btnOscuridad.onClick.AddListener(() => EmitFilter("Oscuridad"));
        if (btnReset) btnReset.onClick.AddListener(() => EmitFilter(null));
    }

    private void EmitFilter(string element)
    {
        var filter = new FilterData
        {
            element = element,
            classStandard = null,
            stars = 0
        };

        Debug.Log($"[FilterBar] Emitiendo filtro: {filter}");
        OnFilterChanged?.Invoke(filter);
    }
}
