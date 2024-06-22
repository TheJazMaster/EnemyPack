using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace TheJazMaster.EnemyPack.Enemies;

internal sealed class JupiterEnemy : AI, IRegisterableEnemy
{
	[JsonProperty]
	private int aiCounter;
	private bool hasRevealed = false;

	public static void Register(IModHelper helper)
	{
		helper.Content.Enemies.RegisterEnemy(new() {
			EnemyType = typeof(JupiterEnemy),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "Jupiter", "name"]).Localize,
			ShouldAppearOnMap = (_, map) => map is MapThree ? BattleType.Normal : null
		});
	}

	public override void OnCombatStart(State s, Combat c)
	{
		c.bg = new BGVanilla
		{
			midgroundArt = MidgroundArt.JaggedRocks,
			showPlanets = false
		};
		c.stuff.Add(8, new JupiterDrone
		{
			x = 8,
			targetPlayer = true
		});
		if (s.GetHarderEnemies())
		{
			c.stuff.Add(10, new JupiterDrone
			{
				x = 10,
				targetPlayer = true
			});
		}
		if (ModEntry.Instance.IsCosmicEnabled(s)) {
			c.stuff.Add(12, new JupiterDrone
			{
				x = 12,
				targetPlayer = true
			});
		}
		c.stuff.Add(11, new JupiterDrone
		{
			x = 11,
			targetPlayer = true
		});
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "CorruptedIsaacHidden"
		};
		List<Part> parts = [
			new Part {
				key = "wing.left",
				type = PType.wing,
				skin = "EnemyPack_Jupiter_wing2",
				flip = true
			},
			new Part {
				key = "scaffold.left",
				type = PType.empty,
				skin = "EnemyPack_Jupiter_scaffolding2",
				flip = true
			},
			new Part {
				key = "comms",
				type = PType.comms,
				skin = "EnemyPack_Jupiter_comms",
				damageModifier = PDamMod.weak,
				flip = true
			},
			new Part {
				key = "missiles.left",
				type = PType.missiles,
				skin = "EnemyPack_Jupiter_missiles2",
				flip = true
			},
			new Part {
				key = "scaffold.right",
				type = PType.empty,
				skin = "EnemyPack_Jupiter_scaffolding1",
				flip = true
			},
			new Part {
				key = "missiles.right",
				type = PType.missiles,
				skin = "EnemyPack_Jupiter_missiles1",
				flip = true
			},
			new Part {
				key = "cockpit",
				type = PType.cockpit,
				skin = "EnemyPack_Jupiter_cockpit",
				flip = true
			},
			new Part {
				key = "wing.right",
				type = PType.wing,
				skin = "EnemyPack_Jupiter_wing1",
				flip = true
			}
		];
		return new Ship {
			x = 7,
			hull = 16,
			hullMax = 16,
			shieldMaxBase = 6,
			ai = this,
			chassisUnder = "EnemyPack_Jupiter_chassis",
			parts = parts
		};
	}

	public override void AfterWasHit(State s, Combat c, Ship selfShip, int? part)
	{
		if (!hasRevealed)
		{
			hasRevealed = true;
			character = new Character {
				type = "CorruptedIsaac"
			};
			c.Queue(new ADummyAction {
				dialogueSelector = ".corruptedIsaacReveal"
			});
		}
	}

	public static List<Status> GetAssignableStatuses(State s)
	{
		return s.characters.Select((Character c) => StatusMeta.deckToMissingStatus[c.deckType.Value]).ToList();
	}

	public static bool IsPositionAbovePlayerShip(State s, int x) {
		return x >= s.ship.x && x < s.ship.x + s.ship.parts.Count;
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		if (c.stuff.Count(pair => pair.Value is JupiterDrone && pair.Value.targetPlayer && IsPositionAbovePlayerShip(s, pair.Value.x)) <= 1) {
			List<Status> assignableStatuses = GetAssignableStatuses(s);
			Status nextStatus = (assignableStatuses.Count > 0) ? assignableStatuses.Random(s.rngAi) : Status.heat;
			return new EnemyDecision {
				actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, 3),
				intents = [
					new IntentSpawn
					{
						thing = new JupiterDrone {
							targetPlayer = true
						},
						key = "missiles.left",
						dialogueTag = ownShip.hull <= 6 ? "corruptedIsaacBouttaDie" : null
					},
					new IntentStatus
					{
						status = nextStatus,
						amount = 2,
						targetSelf = false,
						key = "comms"
					},
					new IntentSpawn
					{
						thing = new JupiterDrone {
							targetPlayer = true
						},
						key = "missiles.right"
					}
				]
			};
		}
		return MoveSet(aiCounter++, () => new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, "missiles.right"),
			intents = [
				new IntentSpawn
				{
					thing = new JupiterDrone {
						targetPlayer = true
					},
					key = "missiles.right",
						dialogueTag = ownShip.hull <= 6 ? "corruptedIsaacBouttaDie" : null
				},
				new IntentGiveCard
				{
					card = new TrashAutoShoot(),
					key = "comms",
					destination = CardDestination.Discard
				}
			]
		}, () => new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, "missiles.left"),
			intents = [
				new IntentSpawn
				{
					thing = new JupiterDrone {
						targetPlayer = true
					},
					key = "missiles.left",
					dialogueTag = ownShip.hull <= 6 ? "corruptedIsaacBouttaDie" : null
				},
				new IntentGiveCard
				{
					card = new TrashUnplayable(),
					key = "comms",
					destination = CardDestination.Deck
				}
			]
		});
	}
}
