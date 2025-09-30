/*
============================================================
StatsFormatter.cs — Formateo de cadenas de stats (TMP)
------------------------------------------------------------
PROPÓSITO
- Unificar formato: base + bonus (oro) y difs (verde/rojo),
  con y sin sufijo “%”.

CONSTANTES
- BonusColor="#FFD700", UpColor="green", DownColor="red".

MÉTODOS
- BasePlusBonus(int baseValue, int bonus, bool percent=false)
  → "100  +20" (bonus en oro) o "10%  +5%".
- CurrentWithDiff(int current, int diff, bool percent=false)
  → "120  +10"/"-5" con colores según signo; añade “%” si toca.
- TotalWithDiff(int total, int diff, bool percent=false)
  → Igual que CurrentWithDiff pero partiendo del total nuevo.
============================================================
*/

public static class StatsFormatter
{
    // Colores (TMP): oro para bonus base+bonus, verde/rojo para difs
    public const string BonusColor = "#FFD700";
    public const string UpColor    = "green";
    public const string DownColor  = "red";

    /// Muestra "base  +bonus" (bonus en oro). Si percent=true, añade "%".
    public static string BasePlusBonus(int baseValue, int bonus, bool percent = false)
    {
        if (percent)
            return bonus > 0 ? $"{baseValue}%  <color={BonusColor}>+{bonus}%</color>" : $"{baseValue}%";
        else
            return bonus > 0 ? $"{baseValue}  <color={BonusColor}>+{bonus}</color>" : $"{baseValue}";
    }

    /// Muestra "actual  +/-diff" (verde/rojo). Si percent=true, añade "%".
    public static string CurrentWithDiff(int current, int diff, bool percent = false)
    {
        if (percent)
        {
            if (diff > 0)  return $"{current}% <color={UpColor}>+{diff}%</color>";
            if (diff < 0)  return $"{current}% <color={DownColor}>{diff}%</color>";
            return $"{current}%";
        }
        else
        {
            if (diff > 0)  return $"{current} <color={UpColor}>+{diff}</color>";
            if (diff < 0)  return $"{current} <color={DownColor}>{diff}</color>";
            return $"{current}";
        }
    }

    /// Muestra "total  +/-diff" (verde/rojo). Si percent=true, añade "%".
    public static string TotalWithDiff(int total, int diff, bool percent = false)
    {
        if (percent)
        {
            if (diff > 0)  return $"{total}% <color={UpColor}>+{diff}%</color>";
            if (diff < 0)  return $"{total}% <color={DownColor}>{diff}%</color>";
            return $"{total}%";
        }
        else
        {
            if (diff > 0)  return $"{total} <color={UpColor}>+{diff}</color>";
            if (diff < 0)  return $"{total} <color={DownColor}>{diff}</color>";
            return $"{total}";
        }
    }
}
