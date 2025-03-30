using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Nickel;
using TheJazMaster.EnemyPack.Actions;

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class GooseEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;

	public static void Register(IModHelper helper)
	{
		Type thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;
		IRegisterableEnemy.MakeSetting(helper, helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = thisType,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Goose", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => IRegisterableEnemy.IfEnabled(thisType, map is MapLawless ? BattleType.Normal : null)
		}));
	}

	public override void OnCombatStart(State s, Combat c)
	{
		s.storyVars.afterCombat = ".after_goose_egg";
	}

	public override bool IsSafeShipPartArrangement(State s, Combat c, Ship selfShip) =>
		selfShip.GetLocalXOfPart("wing.right") > selfShip.GetLocalXOfPart("cannon") &&
		selfShip.GetLocalXOfPart("wing.right") > selfShip.GetLocalXOfPart("missiles.left") &&
		selfShip.GetLocalXOfPart("wing.right") > selfShip.GetLocalXOfPart("missiles.right");

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character {
			type = "Goose"
		};
		List<Part> parts = [
			new Part {
				key = "wing.left",
				type = PType.wing,
				skin = "EnemyPack_Ganderer_wing",
				offset = new(0, 4)
			},
			new Part {
				key = "missiles.left",
				type = PType.missiles,
				skin = "EnemyPack_Ganderer_missiles",
				offset = new(0, 4)
			},
			new Part {
				key = "cannon",
				type = PType.missiles,
				skin = "EnemyPack_Ganderer_missilesCenter",
				offset = new(0, 4)
			},
			new Part {
				key = "missiles.right",
				type = PType.missiles,
				skin = "EnemyPack_Ganderer_missiles",
				flip = true,
				offset = new(0, 4)
			},
			new Part {
				key = "wing.right",
				type = PType.wing,
				skin = "EnemyPack_Ganderer_wing",
				flip = true,
				offset = new(0, 4)
			},
		];
		return new Ship {
			x = 6,
			hull = 9,
			hullMax = 9,
			shieldMaxBase = ModEntry.Instance.IsCosmicEnabled(s) ? 15 : 11,
			ai = this,
			chassisUnder = null,
			parts = parts
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip) => new()
	{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, "cannon"),
			intents = aiCounter++ switch {
				0 => [
					new IntentAttack
					{
						key = "cannon",
						damage = 1,
						cardOnHit = new TrashAnnoyance {
							upgrade = s.GetHarderEnemies() ? Upgrade.A : Upgrade.None
						},
						destination = CardDestination.Discard
					},
				],
				1 => [
					new IntentAttack
					{
						key = "cannon",
						damage = 1,
						cardOnHit = new TrashAnnoyance {
							upgrade = s.GetHarderEnemies() ? Upgrade.A : Upgrade.None
						},
						destination = CardDestination.Discard
					},
					new IntentGiveCard
					{
						key = "missiles.left",
						card = new TrashAnnoyance {
							upgrade = s.GetHarderEnemies() ? Upgrade.A : Upgrade.None
						},
						amount = 1,
						destination = CardDestination.Discard
					}
				], 
				_ => [
					new IntentAttack
					{
						key = "cannon",
						damage = 1,
						cardOnHit = new TrashAnnoyance {
							upgrade = s.GetHarderEnemies() ? Upgrade.A : Upgrade.None
						},
						destination = CardDestination.Discard
					},
					new IntentGiveCard
					{
						key = "missiles.left",
						card = new TrashAnnoyance {
							upgrade = s.GetHarderEnemies() ? Upgrade.A : Upgrade.None
						},
						amount = 1,
						destination = CardDestination.Discard
					},
					new IntentGiveCard
					{
						key = "missiles.right",
						card = new TrashAnnoyance {
							upgrade = s.GetHarderEnemies() ? Upgrade.A : Upgrade.None
						},
						amount = 1,
						destination = CardDestination.Discard
					},
					new IntentStoppableEscape
					{
						key = "wing.right",
						dialogueTag = "gooseEscape",
						postEscapeScript = "after_goose"
					}
				], 

		}
	};
}
