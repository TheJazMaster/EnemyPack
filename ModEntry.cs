using FMOD;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using TheJazMaster.EnemyPack.Enemies;
using TheJazMaster.EnemyPack.Patches;

#nullable enable
namespace TheJazMaster.EnemyPack;

public sealed class ModEntry : SimpleMod {
    internal static ModEntry Instance { get; private set; } = null!;

    internal Harmony Harmony { get; }
	internal IMoreDifficultiesApi? MoreDifficultiesApi { get; }

    internal IStatusEntry FollowStatus { get; }
    internal IStatusEntry DuctTapeStatus { get; }

	internal Spr TimeTravelerSprite { get; }

	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }


    internal static IReadOnlyList<Type> EnemyTypes { get; } = [
		typeof(FollowerEnemy),
		typeof(FireflyEnemy),
		typeof(JupiterEnemy),
		typeof(JunkEnemy),
		typeof(TimestopEnemy),
	];
    
    public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		MoreDifficultiesApi = helper.ModRegistry.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties");
	
		AMovePatches.Apply();
		AIHelpersPatches.Apply();
		DBPatches.Apply();
		ShipPatches.Apply();
		ABreakPartPatches.Apply();

		AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"I18n/en.json").OpenRead()
		);
		Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
		);

		FollowStatus = helper.Content.Statuses.RegisterStatus("Follow", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("Sprites/Icons/Follow.png")).Sprite,
				color = new("ff0000"),
				isGood = true
			},
			Name = AnyLocalizations.Bind(["status", "Follow", "name"]).Localize,
			Description = AnyLocalizations.Bind(["status", "Follow", "description"]).Localize
		});
		DuctTapeStatus = helper.Content.Statuses.RegisterStatus("DuctTape", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("Sprites/Icons/DuctTape.png")).Sprite,
				color = new("b0b0b0"),
				isGood = true
			},
			Name = AnyLocalizations.Bind(["status", "DuctTape", "name"]).Localize,
			Description = AnyLocalizations.Bind(["status", "DuctTape", "description"]).Localize
		});

		foreach (Type type in EnemyTypes)
		{
			AccessTools.DeclaredMethod(type, nameof(IRegisterableEnemy.Register))?.Invoke(null, [helper]);
		}
    }

	public bool IsCosmicEnabled(State s) {
		return MoreDifficultiesApi != null && s.GetDifficulty() >= MoreDifficultiesApi.Difficulty2;
	}
}