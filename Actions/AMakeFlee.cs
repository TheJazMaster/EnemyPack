

using TheJazMaster.EnemyPack.Enemies;

namespace TheJazMaster.EnemyPack.Actions;

internal class AMakeFlee : CardAction {

	public override void Begin(G g, State s, Combat c)
	{
		if (c.otherShip.ai is JunkEnemy junkEnemy) {
			junkEnemy.leaves = true;
		}
	}
}