using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using FSPRO;
using HarmonyLib;
using Newtonsoft.Json;
using Nickel;

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class BoomerEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	public bool isChallengeActive;
	[JsonProperty]
	public int selfMines = 0;
	[JsonProperty]
	public int playerMines = 0;
	[JsonProperty]
	public bool enraged = false;
	

	public static void Register(IModHelper helper)
	{
		Type thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;
		IRegisterableEnemy.MakeSetting(helper, helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = thisType,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Boomer", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => IRegisterableEnemy.IfEnabled(typeof(BoomerEnemy), map is MapFirst ? BattleType.Normal : null)
		}));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: AccessTools.DeclaredMethod(typeof(SpaceMine), nameof(SpaceMine.GetActionsOnDestroyed)),
			postfix: new HarmonyMethod(typeof(BoomerEnemy), nameof(SpaceMine_GetActionsOnDestroyed_Postfix))
		);
	}

	public override FightModifier? GetModifier(State s, Combat c) => new MMinefield {
		mineFillChance = 0.35,
	};

	private const double BETTER_MINE_FILL_CHANCE = 0.1;
	private const double BETTER_MINE_FILL_CHANCE_HARDER = 0.2;
	private const double ASTEROID_FILL_CHANCE = 0.25;
	public override void OnCombatStart(State s, Combat c)
	{
		for (int i = -30; i < 60; i++)
		{
			if (s.rngAi.Next() < (s.GetHarderElites() ? BETTER_MINE_FILL_CHANCE_HARDER : BETTER_MINE_FILL_CHANCE) && !c.stuff.ContainsKey(i))
			{
				c.stuff.Add(i, new SpaceMine
				{
					bigMine = true,
					x = i
				});
			}
			if (s.rngAi.Next() < ASTEROID_FILL_CHANCE && !c.stuff.ContainsKey(i))
			{
				c.stuff.Add(i, new Asteroid
				{
					x = i
				});
			}
		}
		c.Queue(new AMidCombatDialogue
		{
			script = ".Boomer_midcombat"
		});
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "Boomer"
		};
		List<Part> parts = [
			new Part {
				key = "wing.left",
				type = PType.wing,
				skin = "EnemyPack_Boomer_wing",
				stunModifier = UnsteadyPartModManager.UnsteadyRightDamageModifier
			},
			new Part {
				key = "cannon",
				type = PType.cannon,
				skin = "EnemyPack_Boomer_cannon",
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "cockpit.left",
				type = PType.cockpit,
				skin = "EnemyPack_Boomer_cockpit",
			},
			new Part {
				key = "cockpit.right",
				type = PType.cockpit,
				skin = "EnemyPack_Boomer_cockpit",
				flip = true
			},
			new Part {
				key = "missiles",
				type = PType.missiles,
				skin = "EnemyPack_Boomer_missiles",
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "wing.right",
				type = PType.wing,
				skin = "EnemyPack_Boomer_wing",
				flip = true,
				stunModifier = UnsteadyPartModManager.UnsteadyLeftDamageModifier
			},
		];
		return new Ship {
			x = 6,
			hull = 6,
			hullMax = 6,
			shieldMaxBase = 9,
			ai = this,
			chassisUnder = "EnemyPack_Boomer_chassis",
			parts = parts
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip) {
		return new() {
			actions = isChallengeActive ? MoveToAimAtMine(s, c, ownShip, "cannon", 2) : AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon", avoidMines: true, avoidAsteroids: true), 
			intents = [
				new IntentAttack
				{
					key = "cannon",
					damage = isChallengeActive ? 2 : 3
				},
				new IntentSpawn
				{
					key = "missiles",
					thing = new Asteroid()
				}
			]
		};
	}

	public override bool IsSafeShipPartArrangement(State s, Combat c, Ship selfShip)
	{
		return selfShip.parts.FindIndex(part => part.key == "cockpit.left") == selfShip.parts.FindIndex(part => part.key == "cockpit.right") - 1;
	}

	public override void OnHitByAttack(State s, Combat c, int worldX, AAttack attack)
	{
		if (c.otherShip.hull + c.otherShip.Get(Status.shield) <= (c.otherShip.hullMax + c.otherShip.shieldMaxBase) / 2 - 2) {
			Enrage(s, c, true);
		}
	}

	internal static List<CardAction> MoveToAimAtMine(State s, Combat c, Ship ship, string key, int maxMove = 999) {
		Route route = s.route;
		int index = ship.parts.FindIndex((Part p) => p.key == key);
		if (index == -1) return [];

		if (c == null)
		{
			return [];
		}
		if (ship.Get(Status.engineStall) > 0)
		{
			Audio.Play(Event.Status_PowerDown);
			ship.Add(Status.engineStall, -1);
			ship.shake += 1.0;
			return [];
		}
		int min = c.stuff.Select(pair => Math.Abs(pair.Key - index - ship.x)).Min();
		var list = c.stuff.Where(pair =>
		{
			if (pair.Value is SpaceMine)
			{
				return true;
			}
			return false;
		}).Shuffle(s.rngActions).OrderBy(pair => Math.Abs(pair.Key - index - ship.x)).ToList();

		if (list.Count == 0)
		{
			return [];
		}
		int num = list[0].Key - (ship.x + index);
		if (Math.Abs(num) > maxMove)
		{
			num = maxMove * Math.Sign(num);
		}
		return AIHelpers.Move(num);
	}

	private static void SpaceMine_GetActionsOnDestroyed_Postfix(State s, Combat c, bool wasPlayer, int worldX) {
		if (c.otherShip.ai is not BoomerEnemy boomer || !boomer.isChallengeActive) return;

		if (wasPlayer) {
			boomer.playerMines++;
			c.Queue(new ADummyAction
			{
				dialogueSelector = $".Boomer_{boomer.playerMines}MinePlayer"
			});
		}
		else {
			boomer.selfMines++;
			c.Queue(new ADummyAction
			{
				dialogueSelector = $".Boomer_{boomer.selfMines}MineSelf"
			});
		}

		if (boomer.playerMines >= 3) boomer.Concede(s, c);
		else if (boomer.selfMines >= 3) boomer.Enrage(s, c);
	}

	public void Enrage(State s, Combat c, bool fromCheating = false) {
		if (!isChallengeActive) return;

		isChallengeActive = false;
		enraged = true;
		c.Queue(new AMidCombatDialogue
		{
			script = fromCheating ? ".Boomer_midcombat_cheating" : ".Boomer_midcombat_fail"
		});
		c.Queue(new AStatus
		{
			status = Status.payback,
			statusAmount = 1,
			targetPlayer = false
		});
		c.Queue(new AStatus
		{
			status = Status.shield,
			statusAmount = 2,
			targetPlayer = false
		});
		if (fromCheating) c.Queue(new AStatus
		{
			status = Status.powerdrive,
			statusAmount = 1,
			targetPlayer = false
		});
		if (fromCheating && c.isPlayerTurn) c.Queue(AIHelpers.MoveToAimAt(s, c.otherShip, s.ship, "cannon", avoidMines: true, avoidAsteroids: true));
	}

	public void Concede(State s, Combat c) {
		if (!isChallengeActive) return;

		isChallengeActive = false;
		c.Queue(new AMidCombatDialogue
		{
			script = ".Boomer_midcombat_succeed"
		});
	}
}
