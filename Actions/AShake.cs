using FSPRO;

namespace TheJazMaster.EnemyPack.Actions;

internal class AShake : CardAction {

	public bool targetPlayer;

	public override void Begin(G g, State s, Combat c)
	{
		Ship ship = targetPlayer ? s.ship : c.otherShip;
		Audio.Play(Event.Status_PowerDown);
		ship.shake += 1.0;
		return;
	}
}