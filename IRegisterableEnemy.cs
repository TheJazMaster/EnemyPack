using Nanoray.PluginManager;
using Nickel;

internal interface IRegisterableEnemy
{
	abstract static void Register(IModHelper helper);
}
