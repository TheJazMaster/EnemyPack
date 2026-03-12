

using TheJazMaster.EnemyPack.Enemies;

namespace TheJazMaster.EnemyPack.Actions;

internal class AMakeBoomerFlee : CardAction {

	public override void Begin(G g, State s, Combat c)
	{
		if (c.otherShip.ai is BoomerEnemy) {
			c.QueueImmediate(new AEscape {
				targetPlayer = false
			});
		}
	}
}