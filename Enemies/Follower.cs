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

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class FollowerEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public static void Register(IModHelper helper)
	{
		helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = typeof(FollowerEnemy),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Follower", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => map is MapLawless ? BattleType.Normal : null
		});
	}

	public override void OnCombatStart(State s, Combat c)
	{
		c.bg = new BGNebulaLightning();
		c.Queue(new AStatus
		{
			targetPlayer = false,
			status = ModEntry.Instance.FollowStatus.Status,
			statusAmount = 99
		});
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "wasp"
		};
		int health = s.GetHarderEnemies() ? 8 : 11;
		List<Part> parts = [
			new Part {
				key = "wing.left",
				type = PType.wing,
				damageModifier = PDamMod.armor,
				skin = "wing_possum"
			},
			new Part {
				key = "cannon.left",
				type = PType.cannon,
				skin = "cannon_gemini"
			},
			new Part {
				key = "bay.left",
				type = PType.missiles,
				skin = "missiles_gemini"
			},
			new Part {
				key = "empty",
				type = PType.empty,
				skin = "scaffolding_tridim_red"
			},
			new Part {
				key = "bay.right",
				type = PType.missiles,
				skin = "missiles_gemini",
				flip = true
			},
			new Part {
				key = "cannon.right",
				type = PType.cannon,
				skin = "cannon_gemini",
				flip = true
			},
			new Part {
				key = "wing.right",
				type = PType.wing,
				damageModifier = PDamMod.armor,
				skin = "wing_possum",
				flip = true
			},
		];
		return new Ship {
			x = 6,
			hull = health,
			hullMax = health,
			shieldMaxBase = 14,
			ai = this,
			chassisUnder = "chassis_stinger",
			parts = parts
		};
	}

	public override MusicState? GetMusicState(State s)
	{
		if (!(s.route is Combat combat) || combat.otherShip.hull <= 0)
		{
			return null;
		}
		MusicState value = new()
		{
			scene = Song.Rezz,
			combat = 0.0
		};
		return value;
	}

	private static List<CardAction> WithHermes(List<CardAction> actions) {
		actions.Add(new AStatus {
			status = Status.hermes,
			statusAmount = 1,
			targetPlayer = false
		});
		return actions;
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		var actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon.left");
		double count = actions.Count / 2.0;
		foreach (CardAction action in actions) {
			if (action is AMove move) {
				if (move.dir < 0)
					count = Math.Ceiling(count);
				else
					count = Math.Floor(count);
			}
		}
		actions = WithHermes(actions.Take((int)count).ToList());
		foreach (CardAction action in actions) {
			if (action is AMove move)
				move.dir *= 2;
		}
		return MoveSet(aiCounter++, () => new EnemyDecision
		{
			actions = actions,
			intents = [
				new IntentAttack
				{
					damage = 1,
					status = Status.heat,
					key = "cannon.left"
				},
				new IntentSpawn
				{
					thing = new ShieldDrone {
						bubbleShield = true
					},
					key = "bay.left"
				},
				new IntentSpawn
				{
					thing = new ShieldDrone {
						bubbleShield = true
					},
					key = "bay.right"
				},
				new IntentAttack
				{
					damage = 1,
					status = Status.heat,
					key = "cannon.right"
				}
			]
		}, () => new EnemyDecision
		{
			actions = actions,
			intents = [
				new IntentAttack
				{
					damage = 2,
					status = Status.heat,
					key = "cannon.left"
				},
				new IntentAttack
				{
					damage = 2,
					status = Status.heat,
					key = "cannon.right"
				}
			]
		});
	}
}
