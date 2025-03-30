using System.Collections.Generic;
using System.Reflection;
using Nickel;

namespace TheJazMaster.EnemyPack.Cards;

public class BoomerMineCard : Card, IRegisterableCard
{
	public static void Register(IModHelper helper) {
		helper.Content.Cards.RegisterCard("BoomerMineCard", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BoomerDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_GoatDrone,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BoomerMineCard", "name"]).Localize
		});
	}

	public override CardData GetData(State state) => new() {
        cost = upgrade == Upgrade.B ? 1 : 0,
        singleUse = upgrade == Upgrade.None,
		retain = upgrade != Upgrade.B,
        exhaust = upgrade == Upgrade.A
	};

	public override List<CardAction> GetActions(State s, Combat c) => upgrade switch {
		Upgrade.B => [
        	new ASpawn {
                thing = new SpaceMine
                {
                    yAnimation = 0.0,
                    bigMine = true
                }
            },
        	new AStatus {
                status = Status.droneShift,
                statusAmount = 1,
                targetPlayer = true
            }
        ],
        _ => [
			new ASpawn {
                thing = new SpaceMine
                {
                    yAnimation = 0.0,
                    bigMine = true
                }
            }
        ]
	};
}
