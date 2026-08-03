using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using StsLogger = MegaCrit.Sts2.Core.Logging.Logger;

namespace SeedSearchPrototype;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "SeedSearchPrototype";

    public static StsLogger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree?.Root == null)
        {
            Logger.Error("Could not find the main SceneTree. The seed search overlay was not mounted.");
            return;
        }

        tree.Root.CallDeferred("add_child", new SeedSearchOverlay());
        Logger.Info("Seed Search loaded. Use the Seed Search button in the lower-right corner.");
    }
}
