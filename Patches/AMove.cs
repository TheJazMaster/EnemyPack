using HarmonyLib;

namespace TheJazMaster.EnemyPack.Patches;

public class AMovePatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;

    internal static readonly string FromFollowKey = "FromFollow";

    public static void Apply()
    {
        Harmony.TryPatch(
		    logger: ModEntry.Instance.Logger,
		    original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			postfix: new HarmonyMethod(typeof(AMovePatches), nameof(AMove_Begin_Postfix))
		);
    }

    private static void AMove_Begin_Postfix(AMove __instance, G g, State s, Combat c, int __state)
    {
        Status follow = Instance.FollowStatus.Status;
        Ship ship = __instance.targetPlayer ? c.otherShip : s.ship;
        if (ship.Get(follow) > 0 && !(Instance.Helper.ModData.TryGetModData(__instance, FromFollowKey, out bool value) && value)) {
            c.QueueImmediate(new AMove {
                dir = __instance.dir,
                ignoreHermes = false,
                targetPlayer = !__instance.targetPlayer
            }.ApplyModData(FromFollowKey, true));
        }
    }
}
