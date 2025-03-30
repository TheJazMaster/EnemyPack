using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Nickel;

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class OuroborosEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public static void Register(IModHelper helper)
	{
		Type thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;
		IRegisterableEnemy.MakeSetting(helper, helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = thisType,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Ouroboros", "name"]).Localize,
			ShouldAppearOnMap = (s, map) => IRegisterableEnemy.IfEnabled(thisType, s.GetWeirdWithIt() && map is MapThree ? BattleType.Normal : null)
		}));
	}

	public override void OnCombatStart(State s, Combat c)
	{
		c.bg = new BGNebulaLightning();
		c.Queue(new AStatus
		{
			targetPlayer = false,
			status = ModEntry.Instance.OmnipotenceStatus.Status,
			statusAmount = 99
		});
	}

	public override Ship BuildShipForSelf(State s)
	{
		int hull = s.GetHarderEnemies() ? 19 : 16;
		character = new Character {
			type = "Arachnidrone"
		};
		List<Part> parts = [
			new Part {
				key = "cockpit",
				type = PType.cockpit,
				skin = "EnemyPack_Ouroboros_cockpit",
				flip = true
			},
			new Part {
				key = "cannon.left",
				type = PType.cannon,
				skin = "EnemyPack_Ouroboros_cannon1"
			},
			new Part {
				key = "missiles",
				type = PType.missiles,
				skin = "EnemyPack_Ouroboros_missiles"
			},
			new Part {
				key = "cannon.middle",
				type = PType.cannon,
				skin = "EnemyPack_Ouroboros_cannon2",
			},
			new Part {
				key = "wing",
				type = PType.wing,
				skin = "EnemyPack_Ouroboros_wing",
			},
			new Part {
				key = "cannon.right",
				type = PType.cannon,
				skin = "EnemyPack_Ouroboros_cannon3",
			},
		];
		return new Ship {
			x = 6,
			hull = hull,
			hullMax = hull,
			shieldMaxBase = 13,
			ai = this,
			chassisUnder = "EnemyPack_Ouroboros_chassis",
			parts = parts
		};
	}

	internal static void SwapParts(Ship ship, int ind1, int ind2) {
		(ship.parts[ind2], ship.parts[ind1]) = (ship.parts[ind1], ship.parts[ind2]);
	}

	public override FightModifier? GetModifier(State s, Combat c)
	{
		return Mutil.Roll(s.rngAi.Next(), (1, null), new(double, FightModifier?)[2]
		{
			(0.4, new MSolarFlare()),
			(0.3, new MIonStorm())
		});
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip) => MoveSet(aiCounter++, () => new EnemyDecision
		{
			actions = c.isPlayerTurn ? null : AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cockpit"),
			intents = [
				new IntentHurtSelf {
					damage = ModEntry.Instance.IsCosmicEnabled(s) ? 1 : 2,
					key = "cockpit"
				}
			]
		}, () => new EnemyDecision
		{
			actions = c.isPlayerTurn ? null : AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon.left"),
			intents = [
				new IntentAttack {
					damage = 1,
					status = Status.lockdown,
					key = "cannon.left"
				}
			]
		}, () => new EnemyDecision
		{
			actions = c.isPlayerTurn ? null : AIHelpers.MoveToAimAt(s, ownShip, s.ship, "missiles"),
			intents = [
				new IntentSpawn {
					thing = new AttackDrone {
						upgraded = true,
						targetPlayer = true,
					},
					key = "missiles"
				}
			]
		}, () =>
		new EnemyDecision {
			actions = c.isPlayerTurn ? null : AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon.middle"),
			intents = [
				new IntentAttack {
					damage = 2,
					key = "cannon.middle"
				}
			]
		}, () => new EnemyDecision
		{
			actions = c.isPlayerTurn ? null : AIHelpers.MoveToAimAt(s, ownShip, s.ship, "wing"),
			intents = [
				new IntentStatus {
					status = Status.tempShield,
					amount = 3,
					targetSelf = true,
					key = "wing"
				}
			]
		}, () => new EnemyDecision
		{
			actions = c.isPlayerTurn ? null : AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon.right"),
			intents = [
				new IntentAttack {
					damage = 3,
					key = "cannon.right"
				}
			]
		}
	);
}
