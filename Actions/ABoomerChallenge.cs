using System;
using System.Collections.Generic;
using TheJazMaster.EnemyPack.Enemies;

namespace TheJazMaster.EnemyPack.Actions;

public class ABoomerChallenge : CardAction
{
    public bool accepted;

    public override void Begin(G g, State s, Combat c)
	{
		if (c.otherShip.ai is BoomerEnemy boomer)
		{
			boomer.isChallengeActive = accepted;
		}
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		return new List<Tooltip>
		{
			new TTText(ModEntry.Instance.Localizations.Localize(["action", "BoomerChallenge", accepted.ToString()]))
		};
	}
        
}