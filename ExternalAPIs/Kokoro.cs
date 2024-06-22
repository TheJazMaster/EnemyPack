using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable
namespace TheJazMaster.EnemyPack;

public partial interface IKokoroApi
{
	void RegisterTypeForExtensionData(Type type);
	T GetExtensionData<T>(object o, string key);
	bool TryGetExtensionData<T>(object o, string key, [MaybeNullWhen(false)] out T data);
	T ObtainExtensionData<T>(object o, string key, Func<T> factory);
	T ObtainExtensionData<T>(object o, string key) where T : new();
	bool ContainsExtensionData(object o, string key);
	void SetExtensionData<T>(object o, string key, T data);
	void RemoveExtensionData(object o, string key);

	void RegisterStatusLogicHook(IStatusLogicHook hook, double priority);
	void UnregisterStatusLogicHook(IStatusLogicHook hook);

	void RegisterStatusRenderHook(IStatusRenderHook hook, double priority);
	void UnregisterStatusRenderHook(IStatusRenderHook hook);

	Color DefaultActiveStatusBarColor { get; }
	Color DefaultInactiveStatusBarColor { get; }

	IActionApi Actions { get; }

	public interface IActionApi
	{
		CardAction MakeExhaustEntireHandImmediate();
		CardAction MakePlaySpecificCardFromAnywhere(int cardId, bool showTheCardIfNotInHand = true);
		CardAction MakePlayRandomCardsFromAnywhere(IEnumerable<int> cardIds, int amount = 1, bool showTheCardIfNotInHand = true);

		CardAction MakeContinue(out Guid id);
		CardAction MakeContinued(Guid id, CardAction action);
		IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action);
		CardAction MakeStop(out Guid id);
		CardAction MakeStopped(Guid id, CardAction action);
		IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action);

		CardAction MakeHidden(CardAction action, bool showTooltips = false);
		AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer);
		AVariableHint MakeEnergyX(AVariableHint? action = null, bool energy = true, int? tooltipOverride = null);
		AStatus MakeEnergy(AStatus action, bool energy = true);

		List<CardAction> GetWrappedCardActions(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions);

		void RegisterWrappedActionHook(IWrappedActionHook hook, double priority);
		void UnregisterWrappedActionHook(IWrappedActionHook hook);
	}
}

public interface IWrappedActionHook
{
	List<CardAction>? GetWrappedCardActions(CardAction action);
}

public interface IStatusRenderHook
{
	IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(State state, Combat combat, Ship ship) => Enumerable.Empty<(Status Status, double Priority)>();
	bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount) => null;
	bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount) => null;
	(IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount) => new();
	List<Tooltip> OverrideStatusTooltips(Status status, int amount, bool isForShipStatus, List<Tooltip> tooltips) => tooltips;
	List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips) => tooltips;
}

public interface IStatusLogicHook
{
	int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount) => newAmount;
	bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status) => null;
	void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount) { }
	bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy) => false;
}

public enum StatusTurnTriggerTiming
{
	TurnStart, TurnEnd
}

public enum StatusTurnAutoStepSetStrategy
{
	Direct, QueueSet, QueueAdd, QueueImmediateSet, QueueImmediateAdd
}