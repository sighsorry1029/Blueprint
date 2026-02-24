using GUIFramework;
using Valheim.SettingsGui;

namespace kg_Blueprint;

public static class Configs
{
    public static ConfigEntry<int> BuildTime;
    public static ConfigEntry<int> BlueprintLoadFrameSkip, BlueprintBuildFrameSkip, LoadViewMaxPerFrame, GhostmentPlaceMaxPerFrame, MaxCreateNewSize;
    public static ConfigEntry<bool> RemoveBlueprintPlacementOnUnequip;
    private static ConfigEntry<string> SaveZDOForPrefabs;
    public static HashSet<int> SaveZDOHashset;
    private static void UpdateHashset() => SaveZDOHashset = [..SaveZDOForPrefabs.Value.Replace(" ", "").Split(',').Select(x => x.GetStableHashCode())];
    public static ConfigEntry<bool> IncludeTrees, IncludeDestructibles;
    public static ConfigEntry<bool> UseMultithreadIO;
    public static ConfigEntry<int> UseMultithreadIO_Cores;
    
    public static ConfigEntry<bool> UseOptimizedFileFormat;
    public static void Init()
    { 
        //synced
        BuildTime = kg_Blueprint.config("General", "BuildTime", 30, "Time in seconds it takes to build a blueprint (if UseDelayedBuildProgress is false)");
        SaveZDOForPrefabs = kg_Blueprint.config("General", "SaveZDOForPrefabs", "MarketPlaceNPC,sign", "Save ZDOs for prefabs with the given name (comma separated)");
        IncludeTrees = kg_Blueprint.config("General", "IncludeTrees", true, "Include trees in blueprints");
        IncludeDestructibles = kg_Blueprint.config("General", "IncludeDestructibles", true, "Include destructibles in blueprints");
        UpdateHashset();
        SaveZDOForPrefabs.SettingChanged += (_, _) => UpdateHashset();
        //local
        BlueprintLoadFrameSkip = kg_Blueprint._thistype.Config.Bind("General", "BlueprintLoadFrameSkip", 4, "Number of frames to skip when loading a blueprint");
        BlueprintBuildFrameSkip = kg_Blueprint._thistype.Config.Bind("General", "BlueprintBuildFrameSkip", 4, "Number of frames to skip when building a blueprint");
        LoadViewMaxPerFrame = kg_Blueprint._thistype.Config.Bind("General", "LoadViewMaxPerFrame", 20, "Maximum number of objects to load per frame when viewing a blueprint");
        GhostmentPlaceMaxPerFrame = kg_Blueprint._thistype.Config.Bind("General", "GhostmentPlaceMaxPerFrame", 10, "Maximum number of ghost objects loaded per frame (placement)");
        MaxCreateNewSize = kg_Blueprint._thistype.Config.Bind("General", "MaxCreateNewSize", 60, "Max radius of creating new blueprint");
        RemoveBlueprintPlacementOnUnequip = kg_Blueprint._thistype.Config.Bind("General", "RemoveBlueprintPlacementOnUnequip", false, "Remove the ghost object when the blueprint is unequipped");
        UseMultithreadIO = kg_Blueprint._thistype.Config.Bind("General", "UseMultithreadIO", true, "Use multithreaded yml read for loading blueprints");
        UseMultithreadIO_Cores = kg_Blueprint._thistype.Config.Bind("General", "UseMultithreadIO_Cores", 4, "Number of cores to use for multithreaded yml read");
        UseOptimizedFileFormat = kg_Blueprint._thistype.Config.Bind("General", "UseOptimizedFileFormat", true, "Use optimized file format for saving blueprints");
    }
}   