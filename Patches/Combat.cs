using HarmonyLib;

namespace TheJazMaster.EnemyPack.Patches;

public class CombatPatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;

    internal static Card? currentCard = null;

    public static void Apply()
    {
        Harmony.TryPatch(
		    logger: ModEntry.Instance.Logger,
		    original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_TryPlayCard_Prefix)),
            finalizer: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_TryPlayCard_Finalizer))
		);
    }

    private static void Combat_TryPlayCard_Prefix(Card card)
    {
        currentCard = card;
    }

    private static void Combat_TryPlayCard_Finalizer()
    {
        currentCard = null;
    }
}
