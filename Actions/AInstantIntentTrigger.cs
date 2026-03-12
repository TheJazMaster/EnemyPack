

using TheJazMaster.EnemyPack.Enemies;

namespace TheJazMaster.EnemyPack.Actions;

internal class AInstantIntentTrigger : CardAction {

	public override void Begin(G g, State s, Combat c)
	{
		timer = 0.0;
		int num = c.otherShip.parts.FindIndex((Part p) => p.intent != null);
		if (num != -1)
		{
			Part part = c.otherShip.parts[num];
			part.intent?.Apply(s, c, c.otherShip, num);
			part.intent = null;
			c.Queue(new AInstantIntentTrigger());
		} else {
			c.Queue(new AInstantEnemyTurn());
		}
	}
}