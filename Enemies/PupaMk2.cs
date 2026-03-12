using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Nickel;

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class PupaMk2Enemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public static void Register(IModHelper helper)
	{
		Type thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;
		IRegisterableEnemy.MakeSetting(helper, helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = thisType,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "PupaMk2", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => IRegisterableEnemy.IfEnabled(thisType, map is MapThree ? BattleType.Normal : null)
		}));
	}

	public override Ship BuildShipForSelf(State s)
	{
		int hull = ModEntry.Instance.IsCosmicEnabled(s) ? 30 : 24;
		character = new Character {
			type = "Arachnidrone"
		};
		List<Part> parts = [
			new Part {
				key = "wing.left",
				type = PType.wing,
				skin = "EnemyPack_PupaMk2_wing"
			},
			new Part {
				key = "missiles.left",
				type = PType.missiles,
				skin = "EnemyPack_PupaMk2_missiles"
			},
			new Part {
				key = "cannon.left",
				type = PType.cannon,
				skin = "EnemyPack_PupaMk2_cannon",
			},
			new Part {
				key = "cannon.middle",
				type = PType.cannon,
				skin = "EnemyPack_PupaMk2_cannon",
			},
			new Part {
				key = "cannon.right",
				type = PType.cannon,
				skin = "EnemyPack_PupaMk2_cannon",
			},
			new Part {
				key = "missiles.right",
				type = PType.missiles,
				skin = "EnemyPack_PupaMk2_missiles",
				flip = true
			},
			new Part {
				key = "wing.right",
				type = PType.wing,
				skin = "EnemyPack_PupaMk2_wing",
				flip = true
			},
		];
		return new Ship {
			x = 6,
			hull = hull,
			hullMax = hull,
			shieldMaxBase = 0,
			ai = this,
			chassisUnder = "EnemyPack_PupaMk2_chassis",
			parts = parts
		};
	}

	internal static void SwapParts(Ship ship, int ind1, int ind2) {
		(ship.parts[ind2], ship.parts[ind1]) = (ship.parts[ind1], ship.parts[ind2]);
	}

	public override bool IsSafeShipPartArrangement(State s, Combat c, Ship selfShip)
	{
		int leftInd = selfShip.parts.FindIndex(part => part.key == "cannon.left");
		int middleInd = selfShip.parts.FindIndex(part => part.key == "cannon.middle");
		int rightInd = selfShip.parts.FindIndex(part => part.key == "cannon.right");

		if (leftInd > rightInd) SwapParts(selfShip, leftInd, rightInd);
		if (leftInd > middleInd) SwapParts(selfShip, leftInd, middleInd);
		if (middleInd > rightInd) SwapParts(selfShip, rightInd, middleInd);

		return true;
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip) => MoveSet(aiCounter++, () =>
		new EnemyDecision {
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, ["cannon.middle"]),
			intents = [
				new IntentAttack
				{
					damage = 2,
					key = "cannon.left"
				},
				new IntentAttack
				{
					damage = s.GetHarderEnemies() ? 3 : 2,
					key = "cannon.middle"
				},
				new IntentAttack
				{
					damage = 2,
					key = "cannon.right"
				},
			]	
		}, () => new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, ["cannon.left", "cannon.middle", "cannon.right"]),
			intents = [
				new IntentGiveCard {
					key = "missiles.left",
					card = new ColorlessTrash(),
					amount = 2,
					destination = CardDestination.Deck
				},
				new IntentGiveCard {
					key = "missiles.right",
					card = new ColorlessTrash(),
					amount = 2,
					destination = CardDestination.Discard
				}
			]
		}
	);
}
