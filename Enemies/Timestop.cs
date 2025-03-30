using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Nickel;

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class TimestopEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public static void Register(IModHelper helper)
	{
		Type thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;
		IRegisterableEnemy.MakeSetting(helper, helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = thisType,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Timestop", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => IRegisterableEnemy.IfEnabled(thisType, map is MapLawless ? BattleType.Elite : null)
		}));
	}

	public override void OnCombatStart(State s, Combat c)
	{
		c.bg = new BGCloudCity();
		c.Queue(new AMidCombatDialogue
		{
			script = ".TimeTraveller_midcombat"
		});
	}

	public override Song? GetSong(State s)
	{
		return Song.Elite;
	}

	public override FightModifier? GetModifier(State s, Combat c)
	{
		if (s.GetWeirdWithIt())
		{
			return Mutil.Roll(s.rngAi.Next(), (0.2, null), new(double, FightModifier?)[1]
			{
				(0.4, new MBinaryStar())
			});
		}
		return null;
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "TimeTraveller"
		};
		List<Part> parts = [
			new Part {
				key = "wing.left",
				type = PType.wing,
				skin = "EnemyPack_Inevitable_wing1",
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "cannon.left",
				type = PType.cannon,
				skin = "EnemyPack_Inevitable_cannon1"
			},
			new Part {
				key = "wing.leftmiddle",
				type = PType.wing,
				skin = "EnemyPack_Inevitable_wing2",
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "scaffold.left",
				type = PType.empty,
				skin = "EnemyPack_Inevitable_scaffold"
			},
			new Part {
				key = "cockpit.left",
				type = PType.cockpit,
				skin = "EnemyPack_Inevitable_cockpit1",
				stunModifier = PStunMod.stunnable
			},
			new Part {
				key = "cannon.middle",
				type = PType.cannon,
				skin = "EnemyPack_Inevitable_cannon2"
			},
			new Part {
				key = "cockpit.right",
				type = PType.cockpit,
				skin = "EnemyPack_Inevitable_cockpit2",
				stunModifier = PStunMod.stunnable
			},
			new Part {
				key = "scaffold.right",
				type = PType.empty,
				skin = "EnemyPack_Inevitable_scaffold",
				flip = true
			},
			new Part {
				key = "wing.rightmiddle",
				type = PType.wing,
				skin = "EnemyPack_Inevitable_wing3",
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "cannon.right",
				type = PType.cannon,
				skin = "EnemyPack_Inevitable_cannon3"
			},
			new Part {
				key = "wing.right",
				type = PType.wing,
				skin = "EnemyPack_Inevitable_wing4",
				damageModifier = PDamMod.armor
			},
		];
		return new Ship {
			x = 6,
			hull = 17,
			hullMax = 17,
			shieldMaxBase = 7,
			ai = this,
			chassisUnder = "EnemyPack_Inevitable_chassis",
			parts = parts
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		if (aiCounter == 0) aiCounter++;
		return MoveSet(aiCounter++, () => new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon.middle"),
			intents = aiCounter % 6 == 1 ? [
				new IntentStatus
				{
					status = Status.powerdrive,
					key = "cannon.middle",
					amount = 1,
					targetSelf = true,
					dialogueTag = "timeTravellerPowerdrive"
				},
				new IntentStatus
				{
					status = Status.tempShield,
					key = "cockpit.left",
					amount = 1,
					targetSelf = true
				},
				new IntentStatus
				{
					status = Status.tempShield,
					key = "cockpit.right",
					amount = 1,
					targetSelf = true
				},
			] : [
				new IntentAttack
				{
					damage = 1,
					key = "cannon.left"
				},
				new IntentAttack
				{
					damage = 1,
					multiHit = s.GetHarderElites() ? 2 : 1,
					key = "cannon.middle"
				},
				new IntentAttack
				{
					damage = 1,
					key = "cannon.right"
				},
				new IntentStatus
				{
					status = Status.tempShield,
					key = "cockpit.left",
					amount = 1,
					targetSelf = true
				},
				new IntentStatus
				{
					status = Status.tempShield,
					key = "cockpit.right",
					amount = 1,
					targetSelf = true
				},
			]
		}, () => new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon.middle"),
			intents = [
				new IntentAttack
				{
					damage = 1,
					key = "cannon.left"
				},
				new IntentAttack
				{
					damage = 1,
					key = "cannon.middle"
				},
				new IntentAttack
				{
					damage = 1,
					key = "cannon.right"
				},
				new IntentStatus
				{
					status = Status.tempShield,
					key = "wing.left",
					amount = 1,
					targetSelf = true
				},
				new IntentStatus
				{
					status = Status.overdrive,
					key = "wing.right",
					amount = 1,
					targetSelf = true
				},
				new IntentStatus
				{
					status = Status.timeStop,
					key = "cockpit.left",
					amount = 1,
					targetSelf = true,
					dialogueTag = "timeTravellerTimeStop"
				},
				new IntentStatus
				{
					status = Status.timeStop,
					key = "cockpit.right",
					amount = 1,
					targetSelf = true,
					dialogueTag = "timeTravellerTimeStop"
				}
			]
		});
	}
}
