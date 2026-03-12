using HarmonyLib;

namespace TheJazMaster.EnemyPack.Patches;

public class ABreakPartPatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;

    public static void Apply()
    {
        Harmony.TryPatch(
		    logger: ModEntry.Instance.Logger,
		    original: AccessTools.DeclaredMethod(typeof(ABreakPart), nameof(ABreakPart.Begin)),
			prefix: new HarmonyMethod(typeof(ABreakPartPatches), nameof(ABreakPart_Begin_Prefix))
		);
    }

    private static bool ABreakPart_Begin_Prefix(ABreakPart __instance, G g, State s, Combat c)
    {
        Ship ship = __instance.targetPlayer ? s.ship : c.otherShip;

        Status status = ModEntry.Instance.DuctTapeStatus.Status;
        if (ship.Get(status) > 0) {
            c.QueueImmediate(new AStatus {
                status = status,
                statusAmount = -1,
                targetPlayer = __instance.targetPlayer,
                statusPulse = status
            });
            return false;
        }
        return true;
    }
}
