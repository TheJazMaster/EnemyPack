using HarmonyLib;

namespace TheJazMaster.EnemyPack.Patches;

public class ShipPatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;

    public static void Apply()
    {
        Harmony.TryPatch(
		    logger: ModEntry.Instance.Logger,
		    original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_OnBeginTurn_Postfix))
		);
    }

    private static void Ship_OnBeginTurn_Postfix(Ship __instance, State s, Combat c)
    {
        if (__instance.Get(Status.timeStop) > 0)
            return;
        
        __instance.Set(ModEntry.Instance.DuctTapeStatus.Status, 0);
    }
}
