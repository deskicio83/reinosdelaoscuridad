// Assets/Scripts/Scenes/Legion/FilterData.cs
using System;

/// <summary>
/// Datos de filtro aplicados al grid de Legión
/// </summary>
[Serializable]
public class FilterData
{
    public string element;        // "Fuego", "Agua", "Luz", etc.
    public string classStandard;  // "Atacante", "Sanador", etc.
    public int stars;             // Filtrar por nº de estrellas (0 = sin filtro)

    public override string ToString()
    {
        return $"[Filtro] Elemento={element ?? "ANY"}, Clase={classStandard ?? "ANY"}, Estrellas={(stars > 0 ? stars.ToString() : "ANY")}";
    }
}
