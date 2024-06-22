using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;

namespace TheJazMaster.EnemyPack.Patches;

public class DBPatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;
    static IDirectoryInfo PackageRoot => Instance.Package.PackageRoot;


    public static void Apply()
    {
        Harmony.TryPatch(
		    logger: ModEntry.Instance.Logger,
		    original: AccessTools.DeclaredMethod(typeof(DB), nameof(DB.SetLocale)),
			postfix: new HarmonyMethod(typeof(DBPatches), nameof(DB_SetLocale_Postfix))
		);
    }

    private static bool hasInited = false;
    
    private static void DB_SetLocale_Postfix() {
        RegisterCharacterNames(DB.currentLocale.strings);
        RegisterDialogue(DB.currentLocale.strings);
        // DB.story.all["idc"] = new() {
        //     type = NodeType.@event,
		//     lookup = ["before_CorruptedIsaacHidden"],
		//     bg = "BGVanilla",
		//     once = true,
		//     priority = true,
		//     lines = [
        //         new CustomSay() {
        //             who = "comp",
        //             loopTag = "neutral",
        //             Text = "I'm picking up a distress signal."
        //         }
        //     ]
        // };

        if (hasInited) return;
        hasInited = true;

        RegisterCharacterAnimations();
        RegisterShipParts();
        RegisterChoiceFuncs();
    }

    private static void RegisterCharacterNames(Dictionary<string, string> locale) {
        Dictionary<string, string> characterNames = Mutil.LoadJsonFile<Dictionary<string, string>>(Path.Combine(PackageRoot.FullName, "I18n", "names.json"));
        
        foreach (var pair in characterNames) {
            locale["char." + pair.Key] = pair.Value;
        }
    }

    private static void RegisterChoiceFuncs() {
        foreach(MethodInfo info in from mi in typeof(EventsModded).GetMethods()
				let p = mi.GetParameters()
				where mi.IsStatic && p.Length == 1 && p[0].ParameterType == typeof(State) && mi.ReturnType == typeof(List<Choice>)
				select mi) {
                    DB.eventChoiceFns.Add(info.Name, info);
                }
    }
    
    private static string GetHash(string input, SHA256 hash) {
            byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new StringBuilder();
            foreach (Byte thisByte in bytes)
                builder.Append(thisByte.ToString("x2"));
            return builder.ToString()[..8];
        }

    private static void RegisterDialogue(Dictionary<string, string> locale) {
        foreach (IFileInfo file in PackageRoot.GetRelativeDirectory("I18n/Dialogue").GetFilesRecursively()) {
            Story dialogue = Mutil.LoadJsonFile<Story>(file.FullName);

            foreach (string key in dialogue.all.Keys) {
                if (!DB.story.all.ContainsKey(key)) {
                    DB.story.all[key] = dialogue.all[key];
                }
            }
        }
    }

    private static void RegisterCharacterAnimations() {
        foreach(IDirectoryInfo directory in PackageRoot.GetRelative("Sprites/Character").AsDirectory.Directories) {
            string key = directory.Name;
            if (!DB.charAnimations.TryGetValue(key, out Dictionary<string, List<Spr>> value)) {
				value = [];
				DB.charAnimations[key] = value;
            }
            foreach(IDirectoryInfo looptag in directory.Directories) {
                List<Spr> sprites = RegisterTalkSprites(directory.Name, looptag.Name);
				value.Add(looptag.Name, sprites);
            }
        }
    }

    private static void RegisterShipParts() {
        foreach(IDirectoryInfo directory in PackageRoot.GetRelativeDirectory("Sprites/Parts").Directories) {
		    foreach(IFileInfo file in directory.GetFilesRecursively()) {
                string key = string.Concat("EnemyPack_", directory.Name, "_", file.Name.AsSpan(0, file.Name.IndexOf('.')));
                if (!DB.parts.ContainsKey(key)) {
                    DB.parts[key] = Instance.Helper.Content.Sprites.RegisterSprite(file).Sprite;
                }
            }
        }
    }

    private static List<Spr> RegisterTalkSprites(string charName, string looptag)
    {
        var files = Instance.Package.PackageRoot.GetRelative($"Sprites/Character/{charName}/{looptag}").AsDirectory?.GetFilesRecursively();
		List<Spr> sprites = [];
		if (files != null) {
			foreach (IFileInfo file in files) {
				sprites.Add(Instance.Helper.Content.Sprites.RegisterSprite(file).Sprite);
			}
		}
		return sprites;
    }
}
