using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;

namespace TheJazMaster.EnemyPack.Patches;

public class DBPatches
{
	static ModEntry Instance => ModEntry.Instance;
    static Harmony Harmony => Instance.Harmony;
    static IDirectoryInfo PackageRoot => Instance.Package.PackageRoot;

    private static readonly List<IPostDBInitHook> RegisteredHooks = [];

    public static T? LoadJsonFile<T>(IFileInfo file)
	{
		using StreamReader reader = new(file.OpenRead());
		using JsonTextReader reader2 = new JsonTextReader(reader);
		T? result = JSONSettings.serializer.Deserialize<T>(reader2);
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 2);
		return result;
	}

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

        if (hasInited) return;
        hasInited = true;

        RegisterCharacterAnimations();
        RegisterShipParts();
        RegisterChoiceFuncs();

        PostInit();
    }

    private static void RegisterCharacterNames(Dictionary<string, string> locale) {
        var characterNames = LoadJsonFile<Dictionary<string, string>>(Instance.Package.PackageRoot.GetRelativeDirectory("I18n").GetRelativeFile("names.json"));
        if (characterNames == null) {
            ModEntry.Instance.Logger.LogError("JSON loading failed. Make sure the mod's files are all there or report this to the developer");
			throw new Exception();
		}
        
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
            StringBuilder builder = new();
            foreach (byte thisByte in bytes)
                builder.Append(thisByte.ToString("x2"));
            return builder.ToString()[..8];
        }

    private static void RegisterDialogue(Dictionary<string, string> locale) {
        foreach (IFileInfo file in PackageRoot.GetRelativeDirectory("I18n/Dialogue").GetFilesRecursively().Where(f => f.Name.EndsWith(".json"))) {
            var dialogue = LoadJsonFile<Story>(file);
            if (dialogue == null) {
                ModEntry.Instance.Logger.LogError("JSON (dialogue) loading failed. Make sure the mod's files are all there or report this to the developer");
		    	throw new Exception();
		    }

            foreach (string key in dialogue.all.Keys) {
                if (!DB.story.all.ContainsKey(key)) {
                    DB.story.all[key] = dialogue.all[key];
                }
            }
        }
    }

    private static void RegisterCharacterAnimations() {
        foreach(IDirectoryInfo directory in PackageRoot.GetRelative("Sprites/Character").AsDirectory!.Directories) {
            string key = directory.Name;
            if (!DB.charAnimations.TryGetValue(key, out Dictionary<string, List<Spr>>? value)) {
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
		    foreach(IFileInfo file in directory.GetFilesRecursively().Where(f => f.Name.EndsWith(".png"))) {
                string key = string.Concat("EnemyPack_", directory.Name, "_", file.Name.AsSpan(0, file.Name.IndexOf('.')));
                if (!DB.parts.ContainsKey(key)) {
                    DB.parts[key] = Instance.Helper.Content.Sprites.RegisterSprite(file).Sprite;
                }
            }
        }
    }

    private static List<Spr> RegisterTalkSprites(string charName, string looptag)
    {
        // var files = Instance.Package.PackageRoot.GetRelative($"Sprites/Character/{charName}/{looptag}").AsDirectory?.GetFilesRecursively().Where(f => f.Name.EndsWith(".png"));
		// List<Spr> sprites = [];
        var dir = Instance.Package.PackageRoot.GetRelative($"Sprites/Character/{charName}/{looptag}").AsDirectory;
		if (dir != null) {
			// foreach (IFileInfo file in files) {
			// 	sprites.Add(Instance.Helper.Content.Sprites.RegisterSprite(file).Sprite);
			// }
            return Enumerable.Range(1, 10)
				.Select(i => dir.GetRelativeFile($"{charName}_{looptag}_{i}.png"))
				.TakeWhile(f => f.Exists)
				.Select(f => Instance.Helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList();
		}
		// return sprites;
        return [];
    }

    private static void PostInit() {
		foreach (IPostDBInitHook hook in RegisteredHooks) {
			hook.PostDBInit();
		}
    }

    internal static void RegisterPostDBInitHook(IPostDBInitHook hook) {
        RegisteredHooks.Add(hook);
    }
}
