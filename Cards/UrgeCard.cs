using System.Reflection;
using Nickel;
using TheJazMaster.EnemyPack.Patches;

namespace TheJazMaster.EnemyPack.Cards;

public class UrgeCard : Card, IRegisterableCard
{
	public static void Register(IModHelper helper) {
		helper.Content.Cards.RegisterCard("UrgeCard", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.trash,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Cannon,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "UrgeCard", "name"]).Localize
		});
	}

	private int GetDamage(State s) => GetDmg(s, upgrade == Upgrade.A ? 3 : 2);

	public override CardData GetData(State state) => new() {
        cost = 0,
        unplayable = true,
		temporary = true,
		description = ModEntry.Instance.Localizations.Localize(["card", "UrgeCard", "description", upgrade.ToString()], new { Damage = GetDamage(state) })
	};
	
	public override void OnDiscard(State s, Combat c)
	{
		if (CombatPatches.currentCard != null && !c.discard.Contains(CombatPatches.currentCard)) return;

        c.QueueImmediate(new AAttack {
			damage = GetDamage(s)
		});
		if (upgrade == Upgrade.B) c.QueueImmediate(new AAttack {
			damage = GetDamage(s)
		});
	}
}
