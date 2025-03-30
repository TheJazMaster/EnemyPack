using System;
using System.Collections.Generic;
using Nickel;
using TheJazMaster.EnemyPack;
using static TheJazMaster.EnemyPack.IModSettingsApi;

internal interface IRegisterableEnemy
{
    static ModSettings ModSettings => ModEntry.Instance.ModSettings;
    static List<IModSetting> SettingsEntries => ModEntry.Instance.SettingsEntries;
	abstract static void Register(IModHelper helper);

    public static void MakeSetting(IModHelper helper, IEnemyEntry entry) {
		if (helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings") is { } settingsApi) {
			SettingsEntries.Add(settingsApi.MakeCheckbox(
				() => entry.Configuration.Name?.Invoke(DB.currentLocale.locale) ?? "???",
				() => !ModSettings.enemiesDisabled.GetValueOrDefault(entry.Configuration.EnemyType.ToString()),
                (_, _, to) => ModSettings.enemiesDisabled[entry.Configuration.EnemyType.ToString()] = !to));
		}
    }

    public static BattleType? IfEnabled(Type type, BattleType? battleType) {
		return !ModSettings.enemiesDisabled.GetValueOrDefault(type.ToString()) ? battleType : null;
    }
}

internal interface IRegisterableCard
{
    static abstract void Register(IModHelper helper);
}

internal interface IRegisterableArtifact
{
    static abstract void Register(IModHelper helper);
}

internal interface IPostDBInitHook
{
    void PostDBInit();
}