namespace TrailheadApi;

// Must stay in sync with the EQUIPMENT ids in trailhead-app/trailhead-fitness.html.
static class EquipmentCatalog
{
    public static readonly IReadOnlyList<string> KnownIds = new[]
    {
        "treadmill", "bike", "elliptical", "rowMachine", "legPress", "smith",
        "legExtension", "legCurl", "latPulldown", "cable", "chestPress",
        "shoulderPress", "dumbbells"
    };

    public const string Unknown = "unknown";

    public static bool IsKnown(string? id) => id is not null && KnownIds.Contains(id);
}
