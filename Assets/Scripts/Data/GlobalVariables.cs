using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ReinoOscuridad.Data
{
    [Serializable]
    public class GlobalVariables
    {
        public ResetMaestrias reset_maestrias;
        public AwakenPercent awaken_percent;
        public LoginDiario login_diario;
        public GeneradorCaosifera generador_caosifera;
        public EconomiaCaosifera economia_caosifera;
        public PresenciaMaldita presencia_maldita;
    }

    // ── Reset de maestrías ─────────────────────────────────────────────────────

    [Serializable]
    public class ResetMaestrias
    {
        public string moneda;
        public int coste;
    }

    // ── Multiplicadores de awaken por stat ────────────────────────────────────

    [Serializable]
    public class AwakenPercent
    {
        /// Clave: nombre del stat (HP, ATK, DEF…) · Valor: multiplicador (e.g. 1.15)
        public Dictionary<string, float> per_stat;
    }

    // ── Recompensa de login diario ────────────────────────────────────────────

    [Serializable]
    public class LoginDiario
    {
        public int caosifera_por_dia;
        public string descripcion;
    }

    // ── Generador pasivo de Caosifera ────────────────────────────────────────

    [Serializable]
    public class GeneradorCaosifera
    {
        public int desbloqueo_nivel_jugador;
        /// Clave: nivel del generador ("1"…"8")
        public Dictionary<string, GeneradorNivel> niveles;
        public string descripcion;
    }

    [Serializable]
    public class GeneradorNivel
    {
        public int caosifera_por_dia;
        public int coste_subida_oro;
    }

    // ── Economía de Caosifera ─────────────────────────────────────────────────

    [Serializable]
    public class EconomiaCaosifera
    {
        /// Clave: identificador de fuente (login_diario, mision_diaria…)
        public Dictionary<string, FuenteCaosifera> fuentes_recurrentes;
        /// Clave: perfil de jugador (f2p_poco_activo, p2w_pase_oscuro…)
        public Dictionary<string, AnalisisMensual> analisis_mensual;
    }

    [Serializable]
    public class FuenteCaosifera
    {
        public int cantidad;
        public string frecuencia;
        public string notas;
    }

    [Serializable]
    public class AnalisisMensual
    {
        public int total_mes;
        public int invocaciones;
        public float coste_eur;
    }

    // ── Sistema de Presencia Maldita ──────────────────────────────────────────

    [Serializable]
    public class PresenciaMaldita
    {
        public string descripcion;
        public string campo_firestore;
        public string visibilidad;
        public string formula_total;
        public PesoEsbirros peso_esbirros;
        public PesoConjuros peso_conjuros;
        public PesoMaestrias peso_maestrias;
        public PesoGear peso_gear;
        public RecalculoPM recalculo;
        /// Rangos de referencia por perfil de jugador (f2p_1_mes, p2w_intensivo…)
        public Dictionary<string, RangoReferencia> rangos_referencia;
        public List<RangoPM> rangos_pm;
    }

    [Serializable]
    public class PesoEsbirros
    {
        public string formula;
        /// Clave: rareza ("3★", "4★", "5★")
        public Dictionary<string, int> peso_base_por_rareza;
        public string bonus_nivel;
        public string bonus_estrellas;
        public string bonus_awaken;
    }

    [Serializable]
    public class PesoConjuros
    {
        /// Clave: rareza de conjuro (basico, retorcido…)
        public Dictionary<string, int> por_rareza;
    }

    [Serializable]
    public class PesoMaestrias
    {
        public int por_nodo;
        public int max_nodos;
        public int max_pm_maestrias;
    }

    [Serializable]
    public class PesoGear
    {
        public string formula;
        public string valor_max_teorico;
        /// Clave: tipo de substat (atk%, spd, tcri%…) · Valor: peso PM
        public Dictionary<string, float> pesos_por_tipo;
        public int pm_max_por_pieza;
        public int pm_max_conjunto_6_piezas;
        public string fuente;
    }

    [Serializable]
    public class RecalculoPM
    {
        public string en_cliente;
        public string en_servidor;
        public List<CheckpointPM> checkpoints;
        public string nota;
    }

    [Serializable]
    public class CheckpointPM
    {
        public string evento;
        public string coste_extra;
    }

    [Serializable]
    public class RangoReferencia
    {
        public int pm_total;
        public string desglose;
    }

    [Serializable]
    public class RangoPM
    {
        public string rango;
        public int min;
        /// null cuando no hay techo (último rango)
        public int? max;
    }
}
