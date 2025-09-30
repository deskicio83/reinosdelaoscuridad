/*
============================================================
AnalyticsManager.cs — Telemetría y eventos analíticos
------------------------------------------------------------
PROPÓSITO
- Encapsular envío de eventos a tu backend/SDK.

USO
- Singleton o componente global; consumir métodos TrackXxx.

MÉTODOS (COMPLETA AQUÍ)
- TrackScreen(name), TrackClick(name), TrackEconomy(delta, reason).
============================================================
*/

using UnityEngine;
using System.Collections.Generic;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("📊 AnalyticsManager inicializado.");
    }

    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        string msg = $"📈 Evento: {eventName}";

        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                msg += $" | {kvp.Key}: {kvp.Value}";
            }
        }

        Debug.Log(msg);

        // Aquí se conectarían SDKs como Firebase, AppsFlyer, Facebook...
        // Ejemplo:
        // FirebaseAnalytics.LogEvent(eventName, parameters);
    }

    // ✅ Ejemplos específicos que puedes usar desde otros managers

    public void LogHeroSummoned(string heroId, string rarity)
    {
        LogEvent("hero_summoned", new Dictionary<string, object>
        {
            { "hero_id", heroId },
            { "rarity", rarity }
        });
    }

    public void LogCombatStart(string battleId)
    {
        LogEvent("combat_started", new Dictionary<string, object>
        {
            { "battle_id", battleId },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        });
    }

    public void LogCurrencySpent(string type, int amount)
    {
        LogEvent("currency_spent", new Dictionary<string, object>
        {
            { "type", type },
            { "amount", amount }
        });
    }
}
