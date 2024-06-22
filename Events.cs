using System;
using System.Collections.Generic;
using System.Linq;
using Nickel;
using TheJazMaster.EnemyPack;
using TheJazMaster.EnemyPack.Actions;

public static class EventsModded
{
	static ILocaleBoundLocalizationProvider<IReadOnlyList<string>> Localizations => ModEntry.Instance.Localizations;

	private static string Keyed(string str) {
		return "EnemyPack_" + str;
	}

	public static List<Choice> RiggsVsDougChoices(State s)
	{
		string key = "RiggsVsDoug_Normal";
		string key2 = "RiggsVsDoug_Funny";
		string key3 = "RiggsVsDoug_Quiet";
		return
		[
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", key]),
				key = Keyed(key)
			},
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", key2]),
				key = Keyed(key2)
			},
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", key3]),
				key = Keyed(key3),
				actions = { new AMakeFlee() }
			}
		];
	}
}