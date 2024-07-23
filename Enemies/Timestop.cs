using System.Collections.Generic;
using Newtonsoft.Json;
using Nickel;

#nullable enable
namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class TimestopEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public static void Register(IModHelper helper)
	{
		helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = typeof(TimestopEnemy),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Timestop", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => map is MapLawless ? BattleType.Elite : null
		});
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
			return Mutil.Roll(s.rngAi.Next(), (0.2, null), new(double, FightModifier?)[2]
			{
				(0.4, new MSolarWind
				{
					dir = -1
				}),
				(0.4, new MSolarWind
				{
					dir = -2
				})
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
				skin = "wing_ancient",
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "cannon.left",
				type = PType.cannon,
				skin = "cannon_ancient"
			},
			new Part {
				key = "wing.leftmiddle",
				type = PType.wing,
				skin = "wing_ancient",
				flip = true,
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "scaffold.left",
				type = PType.empty,
				skin = "scaffolding_asym"
			},
			new Part {
				key = "cockpit.left",
				type = PType.cockpit,
				skin = "cockpit_ancient",
				stunModifier = PStunMod.stunnable,
				damageModifier = s.GetHarderElites() ? PDamMod.armor : PDamMod.none
			},
			new Part {
				key = "cannon.middle",
				type = PType.cannon,
				skin = "cannon_ancient"
			},
			new Part {
				key = "cockpit.right",
				type = PType.cockpit,
				skin = "cockpit_ancient",
				stunModifier = PStunMod.stunnable,
				damageModifier = s.GetHarderElites() ? PDamMod.armor : PDamMod.none,
				flip = true
			},
			new Part {
				key = "scaffold.right",
				type = PType.empty,
				skin = "scaffolding_asym",
				flip = true
			},
			new Part {
				key = "wing.rightmiddle",
				type = PType.wing,
				skin = "wing_ancient",
				damageModifier = PDamMod.armor
			},
			new Part {
				key = "cannon.right",
				type = PType.cannon,
				skin = "cannon_ancient",
				flip = true
			},
			new Part {
				key = "wing.right",
				type = PType.wing,
				skin = "wing_ancient",
				flip = true,
				damageModifier = PDamMod.armor
			},
		];
		return new Ship {
			x = 6,
			hull = 16,
			hullMax = 16,
			shieldMaxBase = 7,
			ai = this,
			chassisUnder = "chassis_cobalt",
			parts = parts
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		if (aiCounter == 0 && s.GetHarderElites()) aiCounter++;
		return MoveSet(aiCounter++, () => new EnemyDecision
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
					multiHit = 2,
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
					status = Status.tempShield,
					key = "wing.right",
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
					amount = 2,
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
