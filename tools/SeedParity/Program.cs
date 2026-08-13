using System.Text.Json;
using SeedSearchPrototype;

var indices = new List<long>();
if (args.FirstOrDefault() is "--indices")
{
    foreach (var token in args.Skip(1))
    {
        if (long.TryParse(token, out var index) && !indices.Contains(index))
        {
            indices.Add(index);
        }
    }
}
else
{
    var count = args.Length > 0 && int.TryParse(args[0], out var requested)
        ? Math.Clamp(requested, 1, 100)
        : 10;
    var random = new Random();
    while (indices.Count < count)
    {
        var index = random.NextInt64(0, long.MaxValue);
        if (!indices.Contains(index))
        {
            indices.Add(index);
        }
    }
}

var engine = new SeedSearchEngine();
var rows = new List<object>();
foreach (var index in indices)
{
    var seed = SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, index);
    var snapshot = engine.Inspect(seed, SeedBranch.PublicBeta);
    var neowParts = snapshot.NeowOffers.Split(" / ", StringSplitOptions.TrimEntries);
    rows.Add(new
    {
        seed,
        index,
        act1_map = snapshot.Act1MapId,
        elite_count = snapshot.EliteCount,
        shop_count = snapshot.ShopCount,
        rest_site_count = snapshot.RestSiteCount,
        neow_cursed = snapshot.NeowOfferId,
        neow_bonus_a = neowParts.ElementAtOrDefault(1),
        neow_bonus_b = neowParts.ElementAtOrDefault(2),
        bones_grant_a = snapshot.NeowGrantAId,
        bones_grant_b = snapshot.NeowGrantBId,
        boss1 = SearchTheSpireCatalog.DisplayName(snapshot.Boss1Id),
        boss2 = SearchTheSpireCatalog.DisplayName(snapshot.Boss2Id),
        boss3 = SearchTheSpireCatalog.DisplayName(snapshot.Boss3Id),
        boss3b = SearchTheSpireCatalog.DisplayName(snapshot.Boss3BId),
        ancient2 = snapshot.Ancient2Id,
        ancient3 = snapshot.Ancient3Id,
        ancient2_offer = snapshot.Ancient2OfferId,
        ancient3_offer = snapshot.Ancient3OfferId,
        reward_cards = snapshot.RewardCardIds,
        shop_relics = snapshot.ShopRelicIds,
        bag_relics = snapshot.BagRelicIds,
        events = snapshot.EventIds,
    });
}

Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = false }));
