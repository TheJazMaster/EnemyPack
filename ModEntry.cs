using FMOD;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using TheJazMaster.EnemyPack.Artifacts;
using TheJazMaster.EnemyPack.Cards;
using TheJazMaster.EnemyPack.Enemies;
using TheJazMaster.EnemyPack.Patches;

namespace TheJazMaster.EnemyPack;

public sealed class ModEntry : SimpleMod {
    internal static ModEntry Instance { get; private set; } = null!;

    internal Harmony Harmony { get; }
	internal IMoreDifficultiesApi? MoreDifficultiesApi { get; }

    internal IStatusEntry FollowStatus { get; }
    internal IStatusEntry OmnipotenceStatus { get; }
    internal IStatusEntry DuctTapeStatus { get; }
	internal IDeckEntry BoomerDeck { get; }

	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal List<IModSettingsApi.IModSetting> SettingsEntries = [];
	internal ModSettings ModSettings = new();


    internal static IReadOnlyList<Type> EnemyTypes { get; } = [
		typeof(BoomerEnemy),
		typeof(FireflyEnemy),
		typeof(JunkEnemy),
		typeof(BigGunsEnemy),

		typeof(FollowerEnemy),
		typeof(GooseEnemy),
		typeof(TimestopEnemy),

		typeof(PupaMk2Enemy),
		typeof(OuroborosEnemy),
		typeof(JupiterEnemy),
	];

    internal static IReadOnlyList<Type> ArtifactTypes { get; } = [
		typeof(GoldenEggArtifact),
	];

    internal static IReadOnlyList<Type> CardTypes { get; } = [
		typeof(BoomerMineCard),
	];
    
    public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		MoreDifficultiesApi = helper.ModRegistry.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties");
		ModSettings = helper.Storage.LoadJson<ModSettings>(helper.Storage.GetMainStorageFile("json"));
	
		AMovePatches.Apply();
		APlayedCardPatches.Apply();
		AIHelpersPatches.Apply();
		DBPatches.Apply();
		ShipPatches.Apply();
		ABreakPartPatches.Apply();
		_ = new UnsteadyPartModManager();

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
				color = new("984dfc"),
				isGood = true
			},
			Name = AnyLocalizations.Bind(["status", "Follow", "name"]).Localize,
			Description = AnyLocalizations.Bind(["status", "Follow", "description"]).Localize,
			ShouldFlash = (s, c, sh, st) => true
		});
		OmnipotenceStatus = helper.Content.Statuses.RegisterStatus("Omnipotence", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("Sprites/Icons/Omnipotence.png")).Sprite,
				color = new("3fbfff"),
				isGood = true
			},
			Name = AnyLocalizations.Bind(["status", "Omnipotence", "name"]).Localize,
			Description = AnyLocalizations.Bind(["status", "Omnipotence", "description"]).Localize,
			ShouldFlash = (s, c, sh, st) => true
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

		BoomerDeck = helper.Content.Decks.RegisterDeck("Nibbs", new()
		{
			Definition = new() { color = new Color("996a3d"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("Sprites/Cards/border_boomer.png")).Sprite,
			Name = AnyLocalizations.Bind(["deck", "Boomer"]).Localize
		});

		foreach (Type type in EnemyTypes) {
			AccessTools.DeclaredMethod(type, nameof(IRegisterableEnemy.Register))?.Invoke(null, [helper]);
		}
		foreach (Type type in ArtifactTypes) {
			AccessTools.DeclaredMethod(type, nameof(IRegisterableArtifact.Register))?.Invoke(null, [helper]);
		}
		foreach (Type type in CardTypes) {
			AccessTools.DeclaredMethod(type, nameof(IRegisterableCard.Register))?.Invoke(null, [helper]);
		}

		SetUpModSettings(helper);
    }

	public bool IsCosmicEnabled(State s) {
		return MoreDifficultiesApi != null && s.GetDifficulty() >= MoreDifficultiesApi.Difficulty2;
	}

	private void SetUpModSettings(IModHelper helper) {
		if (helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings") is { } settingsApi) {
			settingsApi.RegisterModSettings(settingsApi.MakeList(SettingsEntries)
				.SubscribeToOnMenuClose(_ => {
                    helper.Storage.SaveJson(helper.Storage.GetMainStorageFile("json"), ModSettings);
                }));
		}
	}
}

class ModSettings {
	public readonly Dictionary<string, bool> enemiesDisabled = [];
}