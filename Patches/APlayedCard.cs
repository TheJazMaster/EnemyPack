using HarmonyLib;
using TheJazMaster.EnemyPack.Actions;

namespace TheJazMaster.EnemyPack.Patches;

public class APlayedCardPatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;

    public static void Apply()
    {
        Harmony.TryPatch(
		    logger: ModEntry.Instance.Logger,
		    original: AccessTools.DeclaredMethod(typeof(APlayedCard), nameof(APlayedCard.Begin)),
			postfix: new HarmonyMethod(typeof(APlayedCardPatches), nameof(APlayedCard_Begin_Postfix))
		);
    }

    private static void APlayedCard_Begin_Postfix(G g, State s, Combat c)
    {
        if (c.otherShip.Get(ModEntry.Instance.OmnipotenceStatus.Status) > 0) {
		    c.Queue(new AInstantIntentTrigger());
        }
    }
}
