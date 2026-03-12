

using TheJazMaster.EnemyPack.Enemies;

namespace TheJazMaster.EnemyPack.Actions;

internal class ABoomerEnrage : CardAction {
	public required bool fromCheating;
	public override void Begin(G g, State s, Combat c)
	{
		if (c.otherShip.ai is BoomerEnemy boomer && boomer.playerMines < 3 && !boomer.enraged) {
			
			boomer.isChallengeActive = false;
			boomer.enraged = true;
						
			if (fromCheating && c.isPlayerTurn) c.QueueImmediate(AIHelpers.MoveToAimAt(s, c.otherShip, s.ship, "cannon", avoidMines: true, avoidAsteroids: true));
			if (fromCheating) c.QueueImmediate(new AStatus
			{
				status = Status.powerdrive,
				statusAmount = 1,
				targetPlayer = false
			});
			c.QueueImmediate([
				new AMidCombatDialogue {
					script = fromCheating ? ".Boomer_midcombat_cheating" : ".Boomer_midcombat_fail"
				},
				new AStatus {
					status = Status.payback,
					statusAmount = 1,
					targetPlayer = false
				}, 
				new AStatus {
					status = Status.shield,
					statusAmount = 3,
					targetPlayer = false
				}
			]);
		}
		else {
			timer = 0;
		}
	}
}