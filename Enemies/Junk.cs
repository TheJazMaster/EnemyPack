using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

#nullable enable
namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class JunkEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public bool leaves = false;

	public static void Register(IModHelper helper)
	{
		helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = typeof(JunkEnemy),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Junk", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => map is MapFirst ? BattleType.Normal : null
		});
	}

	private static Dictionary<PType, List<string>> partSkins = new()
	{
		{PType.cannon, new List<string> {
			"cannon_ancient",
			"cannon_apollo",
			"cannon_artemis_alt",
			"cannon_artemis",
			"cannon_boat",
			"cannon_cicada",
			"cannon_cicada3",
			"cannon_drone",
			"cannon_lawless",
			"cannon_goliath",
			"cannon_stag",
			"cannon_pod",
			"cannon_rust",
			"cannon_stinger",
		}},
		{PType.missiles, new List<string> {
			"missiles_ancient",
			"missiles_apollo",
			"missiles_artemis",
			"missiles_boat",
			"missiles_jupiter",
			"missiles_cicada3",
			"missiles_lawless",
			"missiles_goliath",
			"missiles_stag",
			"missiles_conveyor",
			"missiles_rust",
			"missiles_stinger",
		}},
		{PType.wing, new List<string> {
			"wing_ancient",
			"wing_apollo",
			"wing_boat",
			"wing_bruiser",
			"wing_jupiter_c",
			"wing_jupiter_d",
			"wing_cicada3",
			"wing_drone",
			"wing_player",
			"wing_goliath",
			"wing_pod",
			"wing_possum",
			"wing_stag",
			"wing_tiderunner",
			"wing_stinger",
		}}
	};

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "Doug"
		};
		List<Part> parts = [];
		List<PType> partTypes = new List<PType> {PType.wing, PType.cannon, PType.missiles}.Shuffle(s.rngAi).ToList();
		for (int i = 0; i < 3; i++) {
			PType type = partTypes[i];
			parts.Add(new Part {
				key = type.Key() + ".left",
				type = type,
				skin = partSkins[type].Shuffle(s.rngAi).ToList()[0],
				stunModifier = PStunMod.breakable
			});
		}
		parts.Add(new Part {
			key = "cockpit",
			type = PType.cockpit,
			skin = "cockpit_junker"
		});
		for (int i = 0; i < 3; i++) {
			PType type = partTypes[i];
			parts.Add(new Part {
				key = type.Key() + ".right",
				type = type,
				flip = true,
				skin = partSkins[type].Shuffle(s.rngAi).ToList()[0],
				stunModifier = PStunMod.breakable
			});
		}
		return new Ship {
			x = 7,
			hull = 10,
			hullMax = 10,
			shieldMaxBase = 0,
			ai = this,
			chassisUnder = "chassis_junker",
			parts = parts
		};
	}

	public override FightModifier? GetModifier(State s, Combat c)
	{
		if (s.GetWeirdWithIt())
		{
			return Mutil.Roll(s.rngAi.Next(), (0.5, null), new(double, FightModifier?)[2]
			{
				(0.25, new MSolarWind
				{
					dir = -1
				}),
				(0.25, new MSolarWind
				{
					dir = 1
				})
			});
		}
		return null;
	}

	public override void OnCombatStart(State s, Combat c)
	{
		c.Queue(new AMidCombatDialogue
		{
			script = ".Doug_midcombat"
		});
		if (s.GetHarderEnemies()) {
			c.Queue(new AStatus {
				status = Status.autododgeLeft,
				statusAmount = 1,
				targetPlayer = false
			});
		}
	}

	public override void AfterWasHit(State s, Combat c, Ship selfShip, int? part)
	{
		foreach (CardAction action in s.GetCurrentQueue()) {
			if (action is ABreakPart) {
				c.Queue(new ADummyAction {
					dialogueSelector = ".brokeDougsPart"
				});
				break;
			}
		}
	}
	
	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		if (leaves) {
			return new EnemyDecision {
				actions = [new AEscape {
					targetPlayer = false
				}]
			};
		}
		aiCounter++;
		if (ownShip.parts.Count(part => part.type != PType.empty) <= 1)
			return new EnemyDecision {
				intents = [
					new IntentEscape
					{
						key = "cockpit",
						dialogueTag = "brokeCoolShip"
					}
				]
			};
		
		List<Intent> intents = [
			new IntentAttack
			{
				damage = 2,
				key = "cannon.left"
			},
			new IntentSpawn
			{
				thing = new Missile {
					targetPlayer = true
				},
				key = "missiles.right"
			},
			new IntentStatus {
				status = Status.autododgeLeft,
				amount = 1,
				targetSelf = true,
				key = "wing.right"
			},
			new IntentStatus {
				status = Status.tempShield,
				amount = 2,
				targetSelf = true,
				key = "wing.left"
			},
			new IntentSpawn
			{
				thing = new AttackDrone {
					targetPlayer = true
				},
				key = "missiles.left"
			},
			new IntentAttack
			{
				damage = 1,
				status = Status.engineStall,
				statusAmount = 1,
				key = "cannon.right"
			}
		];
		intents.RemoveAll(intent => intent.key != null && ownShip.parts.Find(part => part.key == intent.key)?.type == PType.empty);
		
		return new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, (aiCounter % 2 == 0) ? "cannon.left" : "cannon.right"),
			intents = intents
		};
	}
}
