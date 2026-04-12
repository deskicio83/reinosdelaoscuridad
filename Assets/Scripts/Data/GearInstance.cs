using System;
using System.Collections.Generic;

namespace ReinoOscuridad.Data
{
    /// Instancia de una pieza de gear en memoria — modelo completo de trabajo.
    /// Se construye al crear/obtener gear y se mantiene en GearSystem.
    /// La persistencia usa PlayerGearInstance en PlayerData.
    [Serializable]
    public class GearInstance
    {
        /// GUID único por pieza — generado con System.Guid.NewGuid().ToString()
        public string instanceId;

        /// Referencia al catálogo (gear_catalog.json)
        public string gearId;

        /// Tipo de slot: weapon · helmet · armor · boots · ring · necklace
        public string slot;

        /// Rareza: mugroso · extranito · absurdamente_escaso ·
        ///         divinamente_ridiculo · memeticamente_unico
        public string rareza;

        /// Nivel de mejora (0 a 15)
        public int nivel;

        /// Nombre del stat principal (atk, hp, def, spd, atk%, hp%, def%…)
        public string mainStat;

        /// Valor calculado del stat principal según nivel y rareza
        public int mainStatValue;

        /// Substats — máximo 4. Se van revelando con los rolls en +3/+6/+9/+12.
        public List<SubstatEntry> substats;

        /// heroId del héroe que lleva este gear, o null si está en el inventario.
        public string equipadoEn;
    }

    /// Entrada individual de un substat dentro de una pieza de gear.
    [Serializable]
    public struct SubstatEntry
    {
        /// Nombre del stat (atk, def, hp, spd, tcri%, dcri%, acc, res, agi, luk%)
        public string statName;

        /// Valor acumulado (puede crecer con rolls adicionales en 4/4 revelados)
        public int valor;

        /// false hasta que el roll lo revela (al obtener o mejorar en +3/+6/+9/+12)
        public bool revelado;
    }
}
