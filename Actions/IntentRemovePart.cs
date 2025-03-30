using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FSPRO;
using TheJazMaster.EnemyPack.Enemies;

namespace TheJazMaster.EnemyPack.Actions;

public class IntentRemovePart : Intent
{
    public required string keyToRemove;
	public required string centerKey;
	public bool addInvisiblePart = false;

    public override void Apply(State s, Combat c, Ship fromShip, int actualX)
	{
		c.Queue(new ARemoveParts {
			keys = [keyToRemove],
			centerKey = centerKey,
			addInvisiblePart = addInvisiblePart
		});
	}

	public override string GetSingleTooltip(State s, Combat c, Ship fromShip)
	{
		return "intent.completeMystery";
	}
        
}

class ARemoveParts : CardAction
{
	public required HashSet<string> keys;
	public required string centerKey;
	public bool addInvisiblePart = false;
	private static int counter = 0;
	private static readonly Regex regex = new Regex("^nothing\\d");

	public override void Begin(G g, State s, Combat c)
	{
		Ship ship = c.otherShip;
		Audio.Play(Event.TogglePart);
		int rBound = ship.x + ship.parts.Count;
		int lBound = ship.x;
		ship.RemoveParts(centerKey, keys);
		if (addInvisiblePart) {
			int newRBound = ship.x + ship.parts.Count;
			int newLBound = ship.x;
			int diff = rBound - newRBound + lBound - newLBound;
			int toAdd = -Math.Sign(diff);
			while (diff != 0) {
				if (regex.IsMatch(ship.parts[toAdd == -1 ? 0 : (ship.parts.Count - 1)].key ?? ""))
					ship.RemoveParts(centerKey, [ship.parts[toAdd == -1 ? 0 : (ship.parts.Count - 1)].key!]);
				else
					ship.InsertPart(s, toAdd == 1 ? 0 : (ship.parts.Count - 1), ship.parts.FindIndex(part => part.key == centerKey), toAdd == -1, new Part {
						type = PType.empty,
						skin = "EnemyPack_Raider_nothing",
						key = $"nothing{counter++}"
					});
				diff += toAdd;
			}
		} else {
			
		}
		if (ship.ai is BigGunsEnemy bigGuns) {
			foreach (bool b in new List<bool> { true, false }) {
				string key = b ? "left" : "right";
				int count = BigGunsEnemy.GetMagnetCount(ship, b);
				Part? cannon = ship.parts.Find(part => part.key == "cannon." + key);
				if (cannon != null) cannon.skin = "EnemyPack_Raider_cannon_big" + count;
			}
		}
	}
}