namespace BusinessRules.Domain.Common
{
    public static class FeatureFlags
    {


#if RELEASEOFFLINEMODE
        public static bool OfflineMode { get; set; } = true;
#elif DEBUGOFFLINEMODE
        public static bool OfflineMode { get; set; } = true;
#else
        public static bool OfflineMode { get; set; } = false;
#endif


    }
}
