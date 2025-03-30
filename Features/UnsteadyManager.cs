using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace TheJazMaster.EnemyPack;

internal sealed class UnsteadyPartModManager
{
	internal const PStunMod UnsteadyLeftDamageModifier = (PStunMod)24877;
	internal const PStunMod UnsteadyRightDamageModifier = UnsteadyLeftDamageModifier + 1;

	internal static ISpriteEntry UnsteadyModifierIcon { get; private set; } = null!;


	public UnsteadyPartModManager()
	{
		UnsteadyModifierIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("Sprites/Icons/Unsteady.png"));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderPartUI)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_RenderPartUI_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Transpiler))
		);

		// ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerTakeNormalDamage), (State state, Combat combat, Part? part) =>
		// {
		// 	TriggerUnsteadyIfNeeded(state, combat, part, targetPlayer: true);
		// }, 0);

		// ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnEnemyGetHit), (State state, Combat combat, Part? part) =>
		// {
		// 	TriggerUnsteadyIfNeeded(state, combat, part, targetPlayer: false);
		// }, 0);
	}



	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase originalMethod)
    {
		return new SequenceBlockMatcher<CodeInstruction>(instructions).Find(
                ILMatches.Ldloc<Ship>(originalMethod).CreateLdlocInstruction(out var ldShip).ExtractLabels(out var labels),
				ILMatches.Ldloc<RaycastResult>(originalMethod).CreateLdlocInstruction(out var ldResult),
                ILMatches.Ldfld("worldX"),
				ILMatches.Call("GetPartAtWorldX"),
				ILMatches.Instruction(OpCodes.Dup),
				ILMatches.Brtrue
            )
			.PointerMatcher(SequenceMatcherRelativeElement.First)
			.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
				new CodeInstruction(OpCodes.Ldarg_3).WithLabels(labels),
				new(OpCodes.Ldarg_0),
                ldShip.Value,
				ldResult.Value,
                new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnsteadyPartModManager), nameof(ApplyUnsteady))),
			])
            .AllElements();
    }
	
	private static void ApplyUnsteady(Combat c, AAttack attack, Ship ship, RaycastResult result) {
		Part? p = ship.GetPartAtWorldX(result.worldX);
		if (p is not { } part || part.invincible)
			return;
		
		if (part.stunModifier == UnsteadyLeftDamageModifier) {
			c.QueueImmediate(new AMove
			{
				targetPlayer = attack.targetPlayer,
				dir = -1
			});
		}
		else if (part.stunModifier == UnsteadyRightDamageModifier)
			c.QueueImmediate(new AMove
			{
				targetPlayer = attack.targetPlayer,
				dir = 1
			});
	}


	internal static IEnumerable<Tooltip> MakeUnsteadyPartModTooltips(bool left)
	{
		return [new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::PartStunModifier::Unsteady")
			{
				Icon = UnsteadyModifierIcon.Sprite,
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["partModifier", "Unsteady", left ? "Left" : "Right", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["partModifier", "Unsteady", left ? "Left" : "Right", "description"])
			}
		];
	}

	private static void TriggerUnsteadyIfNeeded(State state, Combat combat, Part? part, bool targetPlayer)
	{
		if (part is not { } nonNullPart || nonNullPart.invincible)
			return;

		if (nonNullPart.stunModifier == UnsteadyLeftDamageModifier) {
			combat.QueueImmediate(new AMove
			{
				targetPlayer = targetPlayer,
				dir = -1
			});
		}
		else if (nonNullPart.stunModifier == UnsteadyRightDamageModifier)
			combat.QueueImmediate(new AMove
			{
				targetPlayer = targetPlayer,
				dir = 1
			});
	}

	private static void Ship_RenderPartUI_Postfix(Ship __instance, G g, Part part, int localX, string keyPrefix, bool isPreview)
	{
		if (part.invincible || (part.stunModifier != UnsteadyLeftDamageModifier && part.stunModifier != UnsteadyRightDamageModifier))
			return;
		if (g.boxes.FirstOrDefault(b => b.key == new UIKey(StableUK.part, localX, keyPrefix)) is not { } box)
			return;
		

		var offset = isPreview ? 25 : 34;
		var v = box.rect.xy + new Vec(8, __instance.isPlayerShip ? (offset - 16) : 8);

		bool left = part.stunModifier == UnsteadyLeftDamageModifier;
		var color = new Color(1, 1, 1, 0.8 + Math.Sin(g.state.time * 4.0) * 0.3);
		Draw.Sprite(UnsteadyModifierIcon.Sprite, v.x, v.y, left, color: color);

		if (!box.IsHover())
			return;
		g.tooltips.Add(g.tooltips.pos, MakeUnsteadyPartModTooltips(left));
	}
}