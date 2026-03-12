using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nickel;
using TheJazMaster.EnemyPack.Actions;

namespace TheJazMaster.EnemyPack.Artifacts;

internal sealed class GoldenEggArtifact : Artifact, IRegisterableArtifact
{
	public static void Register(IModHelper helper) {
		helper.Content.Artifacts.RegisterArtifact("GoldenEgg", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools =  [ ArtifactPool.EventOnly ]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("Sprites/Artifacts/GoldenEgg.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "GoldenEgg", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "GoldenEgg", "description"]).Localize
		});
	}

	public override void OnReceiveArtifact(State state)
	{
		state.GetCurrentQueue().Add(new ARemoveAnnoyances {
			timer = 0
		});
		state.GetCurrentQueue().Add(new AUpgradeCardRandom {
			upgradePath = state.rngCurrentEvent.Next() < 0.5 ? Upgrade.A : Upgrade.B,
			count = 1
		});
	}
}