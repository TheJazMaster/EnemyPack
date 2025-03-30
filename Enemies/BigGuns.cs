using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using TheJazMaster.EnemyPack.Actions;
using TheJazMaster.EnemyPack.Patches;

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class BigGunsEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;
	[JsonProperty]
	public int sidesDone = 0;
	private static Spr dummyMagnetSprite;
	private static List<Spr> magnetSprites = null!;

	class GetMagnetSpriteHook : IPostDBInitHook {
		public void PostDBInit() {
			dummyMagnetSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("Sprites/Parts/magnet.png")).Sprite;
			DB.parts["EnemyPack_Raider_magnet"] = dummyMagnetSprite;

			Regex re = new("^EnemyPack_Raider_magnet_(\\d+)");
			magnetSprites = 
				(from kv in DB.parts
				where re.IsMatch(kv.Key)
				select new
				{
					idx = int.Parse(re.Match(kv.Key.ToString()).Groups[1].Value),
					spr = kv.Value
				} into kv
				orderby kv.idx
				select kv.spr).ToList();
		}
	}

	public static void Register(IModHelper helper)
	{
		Type thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;
		IRegisterableEnemy.MakeSetting(helper, helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = thisType,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "BigGuns", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => IRegisterableEnemy.IfEnabled(typeof(BigGunsEnemy), map is MapFirst ? BattleType.Elite : null)
		}));

        DBPatches.RegisterPostDBInitHook(new GetMagnetSpriteHook());
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DrawTopLayer)),
			transpiler: new HarmonyMethod(typeof(BigGunsEnemy), nameof(Ship_DrawTopLayer_Transpiler))
		);
	}

	public override void OnCombatStart(State s, Combat c)
	{
		c.Queue(new AMidCombatDialogue
		{
			script = ".Ferrington_midcombat"
		});
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character {
			type = "Ferrington"
		};
		List<Part> parts = [
			new Part {
				key = "cannon.left",
				type = PType.cannon,
				skin = "EnemyPack_Raider_cannon_big4",
				damageModifier = s.GetHarderElites() ? PDamMod.weak : PDamMod.brittle,
				offset = new(0, 10)
			},
			new Part {key = "scaffold.left4", type = PType.empty, skin = "EnemyPack_Raider_magnet", offset = new(0, 10)},
			new Part {key = "scaffold.left3", type = PType.empty, skin = "EnemyPack_Raider_magnet", offset = new(0, 10)},
			new Part {key = "scaffold.left2", type = PType.empty, skin = "EnemyPack_Raider_magnet", offset = new(0, 10)},
			new Part {key = "scaffold.left1", type = PType.empty, skin = "EnemyPack_Raider_magnet", offset = new(0, 10)},

			new Part {
				key = "cockpit.left",
				type = PType.cockpit,
				skin = "EnemyPack_Raider_cockpit",
				stunModifier = PStunMod.stunnable,
				offset = new(0, 10)
			},
			new Part {
				key = "cannon.middle",
				type = PType.cannon,
				skin = "EnemyPack_Raider_cannon_center",
				offset = new(0, 6)
			},
			new Part {
				key = "cockpit.right",
				type = PType.cockpit,
				skin = "EnemyPack_Raider_cockpit",
				stunModifier = PStunMod.stunnable,
				flip = true,
				offset = new(0, 10)
			},
			new Part {key = "scaffold.right1", type = PType.empty, skin = "EnemyPack_Raider_magnet", flip = true, offset = new(0, 10) },
			new Part {key = "scaffold.right2", type = PType.empty, skin = "EnemyPack_Raider_magnet", flip = true, offset = new(0, 10)},
			new Part {key = "scaffold.right3", type = PType.empty, skin = "EnemyPack_Raider_magnet", flip = true, offset = new(0, 10)},
			new Part {key = "scaffold.right4", type = PType.empty, skin = "EnemyPack_Raider_magnet", flip = true, offset = new(0, 10)},
			new Part {
				key = "cannon.right",
				type = PType.cannon,
				skin = "EnemyPack_Raider_cannon_big4",
				damageModifier = s.GetHarderElites() ? PDamMod.weak : PDamMod.brittle,
				flip = true,
				offset = new(0, 10)
			},
		];
		return new Ship {
			x = 2,
			hull = 10,
			hullMax = 10,
			shieldMaxBase = 10,
			ai = this,
			chassisUnder = "EnemyPack_Raider_chassis",
			parts = parts
		};
	}

	public override Song? GetSong(State s)
	{
		return Song.Elite;
	}

	internal static int GetMagnetCount(Ship ship, bool left) {
		string key = "scaffold." + (left ? "left" : "right");
		for (int i = 4; i >= 1; i--) {
			Part? p = ship.parts.Find(part => part.key == (key + i));
			if (p != null)
				return i;
		}
		return 0;
	}

	public override bool IsSafeShipPartArrangement(State s, Combat c, Ship selfShip)
	{
		foreach (Part part in selfShip.parts) {
			if (part.skin == "EnemyPack_Raider_nothing" && part.key != null)
				selfShip.RemoveParts("cannon.middle", [part.key]);
		}
		return true;
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip) {
		List<Intent> intents = [
			new IntentAttack {
				damage = 2,
				key = "cannon.middle"
			}
		];
		List<string> propaAimingCannons = ["cannon.middle"];
		foreach (bool b in new List<bool> {true, false}) {
			string key = b ? "left" : "right";
			int count = GetMagnetCount(ownShip, b);
			if (count == 0) {
				string bigCannonKey = "cannon." + key;
				intents.Add(new IntentAttack {
					damage = 4,
					key = bigCannonKey,
					dialogueTag = "giantCannonReady"
				});
				propaAimingCannons.Add(bigCannonKey);
			}
			else intents.Add(new IntentRemovePart {
				key = "cockpit." + key,
				keyToRemove = "scaffold." + key + count,
				centerKey = "cannon.middle",
				addInvisiblePart = true
			});
		}

		return MoveSet(aiCounter++, () =>
			new EnemyDecision {
				actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, propaAimingCannons),
				intents = intents
			}
		);
	}

	private static IEnumerable<CodeInstruction> Ship_DrawTopLayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase originalMethod)
    {
		Label label = il.DefineLabel();
        var elements = new SequenceBlockMatcher<CodeInstruction>(instructions).Find(
                ILMatches.Ldloc<Spr?>(originalMethod).CreateLdlocaInstruction(out var ldLoca),
				ILMatches.Stloc<Spr?>(originalMethod),
                ILMatches.LdcI4((int)StableSpr.parts_cannon_drill)
            )
			.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
				ILMatches.Brfalse.GetBranchTarget(out var branch)
			])
			.EncompassUntil(SequenceMatcherPastBoundsDirection.After, ILMatches.Instruction(OpCodes.Ret))
			.Elements();

		int i = 0;
		while (!elements[++i].labels.Contains(branch.Value));

		return new SequenceBlockMatcher<CodeInstruction>(instructions).Find(
                ILMatches.Ldloc<Spr?>(originalMethod),
				ILMatches.Stloc<Spr?>(originalMethod),
                ILMatches.LdcI4((int)StableSpr.parts_cannon_drill)
            )
			.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
				ILMatches.Brfalse
			])
			.Find(ILMatches.Instruction(elements[i].opcode, elements[i].operand)).ExtractLabels(out var labels)
			.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
				ldLoca,
                new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BigGunsEnemy), nameof(AnimateMagnet))),
			])
            .AllElements();
    }

	private static void AnimateMagnet(G g, ref Spr? sprite) {
		if (sprite.HasValue && sprite.Value == dummyMagnetSprite) {
			sprite = magnetSprites.GetModulo((int)(g.state.time * 12.0));
		}
	}
}
