namespace TheJazMaster.EnemyPack.Actions;

internal class IntentStoppableEscape : IntentEscape {

	public override void Apply(State s, Combat c, Ship fromShip, int actualX)
	{
		if (fromShip.Get(Status.engineStall) > 0 || fromShip.Get(Status.lockdown) > 0) {
			c.Queue(new AShake {
				targetPlayer = false
			});
		}
		else {
			c.Queue(new AEscape {
				targetPlayer = false,
				script = postEscapeScript
			});
		}
	}
}