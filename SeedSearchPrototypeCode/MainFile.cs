using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Interop;
using StsLogger = MegaCrit.Sts2.Core.Logging.Logger;

namespace SeedSearchPrototype;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "SeedSearchPrototype";

    public static StsLogger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, Assembly.GetExecutingAssembly());

        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree?.Root == null)
        {
            Logger.Error("Could not find the main SceneTree. The seed search overlay was not mounted.");
            return;
        }

        tree.Root.CallDeferred("add_child", new SeedSearchOverlay());
        Logger.Info("Seed Search loaded. Press F2 to show the launcher.");
    }
}
