

using TheJazMaster.EnemyPack.Enemies;

namespace TheJazMaster.EnemyPack.Actions;

internal class AInstantEnemyTurn : CardAction {

	public override void Begin(G g, State s, Combat c)
	{
		timer = 0.0;

		if (c.otherShip.ai != null)
		{
			EnemyDecision enemyDecision = c.otherShip.ai!.PickNextIntent(s, c, c.otherShip);
			if (enemyDecision.intents != null)
			{
				foreach (Intent item in enemyDecision.intents!)
				{
					string? key = item.key;
					if (key != null)
					{
						Part? part = c.otherShip.parts.Find((Part p) => p.key == key);
						if (part != null)
						{
							part.intent = item;
						}
					}
				}
			}
		}
	}
}