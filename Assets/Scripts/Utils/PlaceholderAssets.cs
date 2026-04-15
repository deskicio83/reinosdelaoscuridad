using System.Collections.Generic;
using UnityEngine;

namespace ReinoOscuridad.Utils
{
    /// Acceso rápido a texturas placeholder durante el desarrollo.
    /// EXCEPCIÓN a la regla Addressables — solo para placeholders de desarrollo.
    /// En producción todos los assets visuales deben ir vía Addressables.
    ///
    /// Prerequisito: ejecutar Tools → Reino Oscuridad → 0. Generate Placeholder Assets
    public static class PlaceholderAssets
    {
        // ── Mapas elemento / tipo → nombre de archivo ─────────────────────────

        private static readonly Dictionary<string, string> ELEMENTO_MAP = new()
        {
            { "fuego",      "hero_fuego"      },
            { "agua",       "hero_agua"       },
            { "tierra",     "hero_tierra"     },
            { "naturaleza", "hero_naturaleza" },
            { "luz",        "hero_luz"        },
            { "oscuridad",  "hero_oscuridad"  },
            { "rayo",       "hero_rayo"       },
            { "hielo",      "hero_hielo"      },
        };

        private static readonly Dictionary<string, string> TIPO_ENEMIGO_MAP = new()
        {
            { "humanoide", "enemy_humanoide" },
            { "bestia",    "enemy_bestia"    },
            { "no_muerto", "enemy_no_muerto" },
            { "demonio",   "enemy_demonio"   },
            { "dragon",    "enemy_dragon"    },
            { "elemental", "enemy_elemental" },
        };

        // ── API pública ───────────────────────────────────────────────────────

        /// Retrato de héroe por elemento. Devuelve null si el archivo no existe.
        public static Sprite GetHeroPortrait(string elemento)
        {
            string key  = elemento?.ToLower() ?? "";
            string file = ELEMENTO_MAP.TryGetValue(key, out var f) ? f : "placeholder";
            return SpriteFromTexture($"Placeholders/{file}");
        }

        /// Sprite de enemigo por tipo. Devuelve null si el archivo no existe.
        public static Sprite GetEnemySprite(string tipo)
        {
            string key  = tipo?.ToLower() ?? "";
            string file = TIPO_ENEMIGO_MAP.TryGetValue(key, out var f) ? f : "enemy_generico";
            return SpriteFromTexture($"Placeholders/{file}");
        }

        /// Sprite para un nodo del mapa según estado.
        /// estado: "bloqueado" | "disponible" | "jefe" | "completado"
        public static Sprite GetNodeSprite(string estado)
        {
            string file = $"node_{estado?.ToLower() ?? "bloqueado"}";
            return SpriteFromTexture($"Placeholders/{file}");
        }

        /// Fondo de pantalla por sceneId.
        /// sceneId: "campaign" | "combat" | "mainmenu" | "boot"
        public static Sprite GetBackground(string sceneId)
        {
            string file = $"bg_{sceneId?.ToLower() ?? "campaign"}";
            return SpriteFromTexture($"Placeholders/{file}");
        }

        // ── Helper privado ────────────────────────────────────────────────────

        private static Sprite SpriteFromTexture(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[PlaceholderAssets] Textura no encontrada: {resourcePath}. " +
                                 "Ejecuta Tools → Reino Oscuridad → 0. Generate Placeholder Assets");
                return null;
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
