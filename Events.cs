using System;
using System.Collections.Generic;
using System.Linq;
using Nickel;
using TheJazMaster.EnemyPack;
using TheJazMaster.EnemyPack.Actions;
using TheJazMaster.EnemyPack.Artifacts;
using TheJazMaster.EnemyPack.Cards;

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

	public static List<Choice> GetGoldenEgg(State s) {
		string key = "GetGoldenEgg_Yes";
		string key2 = "GetGoldenEgg_No";

		return
		[
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", key]),
				key = Keyed(key),
				actions = { new AAddArtifact
				{
					artifact = new GoldenEggArtifact()
				} }
			},
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", key2]),
				key = Keyed(key2)
			}
		];
	}

	public static List<Choice> BoomerChallenge(State s) {
		string key = "BoomerChallenge_Accept";
		string key2 = "BoomerChallenge_Refuse";

		return
		[
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", key]),
				key = Keyed(key),
				actions = { new ABoomerChallenge {
					accepted = true
				} }
			},
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", key2]),
				key = Keyed(key2),
				actions = { new ABoomerChallenge {
					accepted = false
				} }
			}
		];
	}

	public static List<Choice> BoomerChallengeWin(State s) {
		return
		[
			new Choice
			{
				label = Localizations.Localize(["dialogueChoice", "BoomerChallengeWin"]),
				key = Keyed("Boomer_SucceedAfter"),
				actions = {
					new AAddCard {
						card = new BoomerMineCard(),
						callItTheDeckNotTheDrawPile = true
					},
					new AMakeBoomerFlee()
				}
			}
		];
	}
}