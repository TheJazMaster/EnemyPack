using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace TheJazMaster.EnemyPack.Patches;

public class AIHelpersPatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;

    public static void Apply()
    {
        
        Harmony.TryPatch(
		    logger: ModEntry.Instance.Logger,
		    original: typeof(AIHelpers).GetNestedTypes(AccessTools.all).Where(t => t.GetField("avoidMines") != null).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<MoveToAimAt>") && m.ReturnType == typeof(bool)),
			transpiler: new HarmonyMethod(typeof(AIHelpersPatches), nameof(AIHelpersPatches_MoveToAimAt_Delegate_Transpiler))
		);
    }

    private static IEnumerable<CodeInstruction> AIHelpersPatches_MoveToAimAt_Delegate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase originalMethod)
    {
        Label label = il.DefineLabel();
        var getDrone = new SequenceBlockMatcher<CodeInstruction>(instructions).Find(
                ILMatches.Ldarg(1),
                ILMatches.Call("get_drone").Anchor(out var anchor),
                ILMatches.Isinst(typeof(AttackDrone)),
                ILMatches.Brfalse,
                ILMatches.Ldarg(1),
                ILMatches.Call("get_drone"),
                ILMatches.Ldfld("targetPlayer"),
                ILMatches.Brfalse
            )
            .Anchors()
            .PointerMatcher(anchor).Element();
        
        return new SequenceBlockMatcher<CodeInstruction>(instructions).Find(
                ILMatches.Ldarg(1),
                ILMatches.Call("get_drone"),
                ILMatches.Isinst(typeof(AttackDrone)),
                ILMatches.Brfalse,
                ILMatches.Ldarg(1),
                ILMatches.Call("get_drone"),
                ILMatches.Ldfld("targetPlayer"),
                ILMatches.Brfalse
            )
            .EncompassUntil(SequenceMatcherPastBoundsDirection.After, ILMatches.Ldarg(1))
            .PointerMatcher(SequenceMatcherRelativeElement.Last).ExtractLabels(out var labels).AddLabel(label)
			.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, new List<CodeInstruction> {
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
                new(getDrone.opcode, getDrone.operand),
                new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AIHelpersPatches), nameof(IsJupiterDrone))),
                new(OpCodes.Brfalse, label),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ret)
            })
            .AllElements();
    }

    private static bool IsJupiterDrone(StuffBase thing) {
        return thing is JupiterDrone && thing.targetPlayer;
    }
}
