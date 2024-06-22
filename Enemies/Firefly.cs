using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

#nullable enable
namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class FireflyEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public static void Register(IModHelper helper)
	{
		helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = typeof(FireflyEnemy),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Firefly", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => map is MapFirst ? BattleType.Normal : null
		});
	}

	public override void OnCombatStart(State s, Combat c)
	{
		c.bg = new BGPlanetVolcanic();
		c.stuff.Add(17, new RepairKit
		{
			x = 17
		});
	}

	public override FightModifier? GetModifier(State s, Combat c)
	{
		return new MSolarFlare();
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "scrap"
		};
		List<Part> parts = [
			new Part {
				key = "shield",
				type = PType.wing,
				damageModifier = PDamMod.armor,
				stunModifier = PStunMod.stunnable,
				skin = "shield_knight"
			},
			new Part {
				key = "missiles",
				type = PType.missiles,
				stunModifier = PStunMod.stunnable,
				skin = "missiles_gemini_off",
				flip = true
			},
			new Part {
				key = "cannon",
				type = PType.cannon,
				stunModifier = PStunMod.stunnable,
				skin = "wing_knight",
				flip = true
			},
			new Part {
				key = "cockpit",
				type = PType.cockpit,
				damageModifier = PDamMod.weak,
				skin = "cockpit_wizard"//cockpit_bramblepelt
			},
		];
		return new Ship {
			x = 6,
			hull = 16,
			hullMax = 16,
			shieldMaxBase = 0,
			ai = this,
			chassisUnder = "chassis_lawless",
			parts = parts
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		aiCounter++;
		return new EnemyDecision {
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon"),
			intents = [
				new IntentStatus
				{
					key = "shield",
					status = Status.tempShield,
					amount = 1,
					targetSelf = true
				},
				new IntentAttack
				{
					key = "cannon",
					damage = 1,
					status = s.GetHarderEnemies() ? Status.heat : null,
					statusAmount = 1
				},
				new IntentGiveCard
				{
					key = "missiles",
					card = new TrashMiasma(),
					amount = 1,
					destination = CardDestination.Deck
				}
			]
		};
	}
}
