using System.Collections;
using UnityEngine;
using ReinoOscuridad.Data;
using ReinoOscuridad.UI.Combat;

namespace ReinoOscuridad.Utils
{
    /// Utilidad permanente de diagnóstico para CombatScene.
    /// Añadir temporalmente al CombatController GameObject para verificar estado.
    public class CombatTestRunner : MonoBehaviour
    {
        private void Start()
        {
            TestCombatContext();
            StartCoroutine(TestATB());
            StartCoroutine(TestHPBar());
        }

        private void TestCombatContext()
        {
            var ctx = CombatSceneData.PendingContext;
            Assert(ctx != null,                                          "CombatContext no es null");
            Assert(ctx?.playerTeam != null && ctx.playerTeam.Length > 0, "PlayerTeam tiene héroes");
            Assert(ctx?.enemyTeam  != null && ctx.enemyTeam.Length  > 0, "EnemyTeam tiene enemigos");
            Assert(!string.IsNullOrEmpty(ctx?.encounterID),              "EncounterID no está vacío");
        }

        private IEnumerator TestATB()
        {
            yield return new WaitForSeconds(2f);
            var controller = FindAnyObjectByType<CombatSceneController>();
            Assert(controller != null, "CombatSceneController encontrado");
        }

        private IEnumerator TestHPBar()
        {
            yield return new WaitForSeconds(1f);
            var units = FindObjectsByType<ATBUnit>(FindObjectsInactive.Include, SortMode.None);
            Assert(units != null && units.Length > 0,
                   "ATBUnits encontradas: " + (units?.Length ?? 0));
        }

        private static void Assert(bool condition, string nombre)
        {
            if (condition)
                Debug.Log($"[CombatTest] [PASS] {nombre}");
            else
                Debug.LogError($"[CombatTest] [FAIL] {nombre}");
        }
    }
}
