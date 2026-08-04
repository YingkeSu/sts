namespace SeedSearchPrototype;

public sealed record SearchTheSpireUiGroup(
    string Id,
    IReadOnlyList<SearchTheSpireSlot> Slots,
    bool HasSelection);

/// <summary>
/// Rendering policy copied from SearchTheSpire's board source: child slots are
/// local to the selected parent, and related run-layout mechanisms share one
/// section. Keeping this policy pure makes the confusing/unrelated-options
/// regression testable without Godot.
/// </summary>
public static class SearchTheSpireUiLayout
{
    public const int MaxAscension = 10;

    public static IReadOnlyList<RunCharacter> CharacterOrder { get; } =
        new[]
        {
            RunCharacter.Any,
            RunCharacter.Ironclad,
            RunCharacter.Silent,
            RunCharacter.Regent,
            RunCharacter.Necrobinder,
            RunCharacter.Defect,
        };

    public static IReadOnlyList<string> AncientFilterIds { get; } =
        new[] { "Any", "Ancient A", "Ancient B", "Ancient C", "Ancient D" };

    public static IReadOnlyList<string> BossFilterIds { get; } =
        new[] { "Any", "Boss 1", "Boss 2", "Boss 3" };

    public static int AscensionAt(int index) => Math.Clamp(index, 0, MaxAscension);

    public static int CharacterIndex(RunCharacter character)
    {
        var index = CharacterOrder
            .Select((candidate, position) => (candidate, position))
            .FirstOrDefault(pair => pair.candidate == character)
            .position;
        return index < 0 ? 0 : index;
    }

    public static int FilterIndex(IReadOnlyList<string> filters, string? value)
    {
        var index = filters
            .Select((candidate, position) => (candidate, position))
            .FirstOrDefault(pair => string.Equals(pair.candidate, value, StringComparison.OrdinalIgnoreCase))
            .position;
        return index;
    }

    public static IReadOnlyList<SearchTheSpireSlot> VisibleNeowChildren(SearchTheSpireBoardState state) =>
        state.VisibleChildren("neowOffer");

    public static IReadOnlyList<SearchTheSpireUiGroup> AdvancedGroups(SearchTheSpireBoardState state) =>
        SearchTheSpireCatalog.Slots
            .Where(slot => slot.ParentId == null && slot.Cluster != null && slot.Cluster != "neow" && slot.Id != "rares")
            .GroupBy(slot => slot.Cluster is "act" or "bosses"
                ? "route"
                : slot.Cluster is "shops" or "bag"
                    ? "relics"
                    : slot.Cluster!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal)
            // Keep the same vertical order as board_ui.js: card rewards,
            // relic channels, events, ancients, then route. The catalog is
            // organized for model/spec compilation, not for screen order.
            .OrderBy(pair => pair.Key switch
            {
                "rewards" => 0,
                "relics" => 1,
                "events" => 2,
                "ancients" => 3,
                "route" => 4,
                _ => 99,
            })
            .Select(pair => new SearchTheSpireUiGroup(
                pair.Key,
                pair.Value,
                pair.Value.Any(slot => HasSelectionInTree(slot, state))))
            .ToArray();

    public static bool ShouldRenderChildren(SearchTheSpireSlot slot, SearchTheSpireBoardState state) =>
        state.Selected(slot.Id) != null || state.VisibleChildren(slot.Id).Any(child => HasSelectionInTree(child, state));

    private static bool HasSelectionInTree(SearchTheSpireSlot slot, SearchTheSpireBoardState state) =>
        state.Selected(slot.Id) != null ||
        state.VisibleChildren(slot.Id).Any(child => HasSelectionInTree(child, state));
}
