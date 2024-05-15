namespace BPXProjector;

internal static class ResourceManager {
    public const string CommonLibsTargetsKey = "CommonLibs.targets";
    public const string GameLibsTargetsKey = "ShoddyGameLibs.targets";
    
    private static Stream? CommonLibsTargetsContents { get; set; }
    private static Stream? GameLibsTargetsContents { get; set; }
    
    public static Stream? CommonLibsTargets {
        get
        {
            if (CommonLibsTargetsContents != null) return CommonLibsTargetsContents;
            
            var meType = typeof(ResourceManager);
            CommonLibsTargetsContents = meType.Assembly
                .GetManifestResourceStream($"{meType.Namespace}.{CommonLibsTargetsKey}");
            
            return CommonLibsTargetsContents;
        }
    }
    public static Stream? GameLibsTargets {
        get
        {
            if (GameLibsTargetsContents != null) return GameLibsTargetsContents;
            
            var meType = typeof(ResourceManager);
            GameLibsTargetsContents = meType.Assembly
                .GetManifestResourceStream($"{meType.Namespace}.{GameLibsTargetsKey}");
            
            return GameLibsTargetsContents;
        }
    }
}