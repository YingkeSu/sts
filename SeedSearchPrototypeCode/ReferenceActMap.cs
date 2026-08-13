namespace SeedSearchPrototype;

/// <summary>
/// Faithful port of MegaCrit.Sts2.Core.Map.StandardActMap (v0.110.1) onto the
/// pure reference RNG, so search results describe the same Act 1 map the game
/// generates for the seed. Decompiled reference snapshots live in
/// docs/sts2-decompile/v0.110.1/.
/// </summary>
internal static class ReferenceActMap
{
    public static MapLayout Generate(
        string act1MapId,
        ulong mapStreamSeed,
        int ascension,
        string bossId,
        string ancientId = "neow")
    {
        var isUnderdocks = act1MapId == "1";
        var rng = Sts2ReferenceRng.Create(mapStreamSeed);
        var map = new Generator(isUnderdocks, ascension, ref rng);
        return map.BuildLayout(bossId, ancientId);
    }

    internal enum MapPointType
    {
        Unassigned,
        Unknown,
        Shop,
        Treasure,
        RestSite,
        Monster,
        Elite,
        Boss,
        Ancient,
    }

    internal sealed class MapPoint : IComparable<MapPoint>
    {
        public readonly HashSet<MapPoint> Parents = new();
        public (int Col, int Row) Coord;
        public bool CanBeModified = true;
        public MapPointType PointType;
        public HashSet<MapPoint> Children { get; } = new();

        public MapPoint(int col, int row)
        {
            Coord = (col, row);
        }

        public void AddChildPoint(MapPoint child)
        {
            Children.Add(child);
            child.Parents.Add(this);
        }

        public void RemoveChildPoint(MapPoint child)
        {
            Children.Remove(child);
            child.Parents.Remove(this);
        }

        public int CompareTo(MapPoint? other) =>
            other == null ? 1 : Coord.CompareTo(other.Coord);
    }

    internal sealed record MapPointTypeCounts(int NumOfUnknowns, int NumOfRests, int NumOfElites)
    {
        public int NumOfShops => 3;
    }

    private sealed class Generator
    {
        private const int MapWidth = 7;
        private const int Iterations = 7;
        private const int BaseNumberOfRooms = 15;

        private static readonly HashSet<MapPointType> LowerMapPointRestrictions =
            new() { MapPointType.RestSite, MapPointType.Elite };
        private static readonly HashSet<MapPointType> UpperMapPointRestrictions =
            new() { MapPointType.RestSite };
        private static readonly HashSet<MapPointType> ParentMapPointRestrictions =
            new() { MapPointType.Elite, MapPointType.RestSite, MapPointType.Treasure, MapPointType.Shop };
        private static readonly HashSet<MapPointType> ChildMapPointRestrictions =
            new() { MapPointType.Elite, MapPointType.RestSite, MapPointType.Treasure, MapPointType.Shop };
        private static readonly HashSet<MapPointType> SiblingPointTypeRestrictions =
            new() { MapPointType.RestSite, MapPointType.Monster, MapPointType.Unknown, MapPointType.Elite, MapPointType.Shop };

        private readonly int _mapLength;
        private readonly MapPointTypeCounts _pointTypeCounts;
        private readonly HashSet<MapPoint> _startMapPoints = new();
        private Sts2ReferenceRng.RngState _rng;
        private MapPoint?[,] _grid;

        public MapPoint BossMapPoint { get; }

        public MapPoint StartingMapPoint { get; }

        public Generator(bool isUnderdocks, int ascension, ref Sts2ReferenceRng.RngState rng)
        {
            _rng = rng;
            _mapLength = BaseNumberOfRooms + 1;
            _grid = new MapPoint?[MapWidth, _mapLength];
            _pointTypeCounts = RollCounts(isUnderdocks, ascension, ref _rng);
            BossMapPoint = new MapPoint(MapWidth / 2, _mapLength);
            StartingMapPoint = new MapPoint(MapWidth / 2, 0);
            GenerateMap();
            AssignPointTypes();
            ReferenceMapPathPruning.PruneAndRepair(
                _grid,
                _startMapPoints,
                StartingMapPoint,
                _pointTypeCounts,
                ref _rng,
                IsValidPointType);
            _grid = ReferenceMapPostProcessing.CenterGrid(_grid);
            _grid = ReferenceMapPostProcessing.SpreadAdjacentMapPoints(_grid);
            _grid = ReferenceMapPostProcessing.StraightenPaths(_grid);
        }

        private static MapPointTypeCounts RollCounts(
            bool isUnderdocks,
            int ascension,
            ref Sts2ReferenceRng.RngState rng)
        {
            var restCount = NextGaussianInt(ref rng, 7, 1, 6, 7);
            var unknownCount = NextGaussianInt(ref rng, 12, 1, 10, 14);
            var eliteCount = (int)Math.Round(5f * (ascension >= 1 ? 1.6f : 1f));
            return new MapPointTypeCounts(unknownCount, restCount, eliteCount);
        }

        private static int NextGaussianInt(
            ref Sts2ReferenceRng.RngState rng,
            int mean,
            int stdDev,
            int min,
            int max)
        {
            int value;
            do
            {
                var d = 1.0 - Sts2ReferenceRng.NextDouble(ref rng);
                var num = 1.0 - Sts2ReferenceRng.NextDouble(ref rng);
                var gaussian = Math.Sqrt(-2.0 * Math.Log(d)) * Math.Sin(Math.PI * 2.0 * num);
                value = (int)Math.Round(mean + (double)stdDev * gaussian);
            }
            while (value < min || value > max);
            return value;
        }

        private MapPoint GetOrCreatePoint(int col, int row)
        {
            var point = GetPoint(col, row);
            if (point != null)
            {
                return point;
            }

            point = new MapPoint(col, row);
            _grid[col, row] = point;
            return point;
        }

        private MapPoint? GetPoint(int col, int row)
        {
            if (col == BossMapPoint.Coord.Col && row == BossMapPoint.Coord.Row)
            {
                return BossMapPoint;
            }

            if (col == StartingMapPoint.Coord.Col && row == StartingMapPoint.Coord.Row)
            {
                return StartingMapPoint;
            }

            if (col >= 0 && col < MapWidth && row >= 0 && row < _grid.GetLength(1))
            {
                return _grid[col, row];
            }

            return null;
        }

        private void GenerateMap()
        {
            for (var i = 0; i < Iterations; i++)
            {
                var point = GetOrCreatePoint(Sts2ReferenceRng.NextInt(ref _rng, MapWidth), 1);
                if (i == 1)
                {
                    while (_startMapPoints.Contains(point))
                    {
                        point = GetOrCreatePoint(Sts2ReferenceRng.NextInt(ref _rng, MapWidth), 1);
                    }
                }

                _startMapPoints.Add(point);
                PathGenerate(point);
            }

            ForEachInRow(_grid, GetRowCount() - 1, point => point.AddChildPoint(BossMapPoint));
            ForEachInRow(_grid, 1, point => StartingMapPoint.AddChildPoint(point));
        }

        private void PathGenerate(MapPoint startingPoint)
        {
            var point = startingPoint;
            while (point.Coord.Row < _mapLength - 1)
            {
                var coord = GenerateNextCoord(point);
                var next = GetOrCreatePoint(coord.Col, coord.Row);
                point.AddChildPoint(next);
                point = next;
            }
        }

        private (int Col, int Row) GenerateNextCoord(MapPoint current)
        {
            var col = current.Coord.Col;
            var minCol = Math.Max(0, col - 1);
            var maxCol = Math.Min(col + 1, MapWidth - 1);
            var offsets = new List<int> { -1, 0, 1 };
            StableShuffle(offsets, ref _rng);
            foreach (var offset in offsets)
            {
                var row = current.Coord.Row + 1;
                var targetCol = offset switch
                {
                    -1 => minCol,
                    0 => col,
                    1 => maxCol,
                    _ => throw new InvalidOperationException("This isn't possible"),
                };
                if (!HasInvalidCrossover(current, targetCol))
                {
                    return (targetCol, row);
                }
            }

            throw new InvalidOperationException("Cannot find next node");
        }

        private bool HasInvalidCrossover(MapPoint current, int targetX)
        {
            var diff = targetX - current.Coord.Col;
            if (diff == 0 || diff == MapWidth)
            {
                return false;
            }

            var point = _grid[targetX, current.Coord.Row];
            if (point == null)
            {
                return false;
            }

            foreach (var child in point.Children)
            {
                if (child.Coord.Col - point.Coord.Col == -diff)
                {
                    return true;
                }
            }

            return false;
        }

        private void AssignPointTypes()
        {
            ForEachInRow(_grid, GetRowCount() - 1, point =>
            {
                point.PointType = MapPointType.RestSite;
                point.CanBeModified = false;
            });
            ForEachInRow(_grid, GetRowCount() - 7, point =>
            {
                point.PointType = MapPointType.Treasure;
                point.CanBeModified = false;
            });
            ForEachInRow(_grid, 1, point =>
            {
                point.PointType = MapPointType.Monster;
                point.CanBeModified = false;
            });

            var queue = new Queue<MapPointType>();
            for (var i = 0; i < _pointTypeCounts.NumOfRests; i++)
            {
                queue.Enqueue(MapPointType.RestSite);
            }

            for (var i = 0; i < _pointTypeCounts.NumOfShops; i++)
            {
                queue.Enqueue(MapPointType.Shop);
            }

            for (var i = 0; i < _pointTypeCounts.NumOfElites; i++)
            {
                queue.Enqueue(MapPointType.Elite);
            }

            for (var i = 0; i < _pointTypeCounts.NumOfUnknowns; i++)
            {
                queue.Enqueue(MapPointType.Unknown);
            }

            AssignRemainingTypesToRandomPoints(queue);
            foreach (var point in GetAllMapPoints())
            {
                if (point.PointType == MapPointType.Unassigned)
                {
                    point.PointType = MapPointType.Monster;
                }
            }

            BossMapPoint.PointType = MapPointType.Boss;
            StartingMapPoint.PointType = MapPointType.Ancient;
        }

        private void AssignRemainingTypesToRandomPoints(Queue<MapPointType> pointTypesToBeAssigned)
        {
            for (var i = 0; i < 3; i++)
            {
                if (pointTypesToBeAssigned.Count <= 0)
                {
                    break;
                }

                var list = GetAllMapPoints()
                    .Where(point => point.PointType == MapPointType.Unassigned)
                    .ToList();
                StableShuffle(list, ref _rng);
                foreach (var item in list)
                {
                    if (pointTypesToBeAssigned.Count == 0)
                    {
                        break;
                    }

                    item.PointType = GetNextValidPointType(pointTypesToBeAssigned, item);
                }
            }
        }

        private MapPointType GetNextValidPointType(Queue<MapPointType> queue, MapPoint point)
        {
            for (var i = 0; i < queue.Count; i++)
            {
                var type = queue.Dequeue();
                if (IsValidPointType(type, point))
                {
                    return type;
                }

                queue.Enqueue(type);
            }

            return MapPointType.Unassigned;
        }

        public bool IsValidPointType(MapPointType pointType, MapPoint point) =>
            IsValidForUpper(pointType, point) &&
            IsValidForLower(pointType, point) &&
            IsValidWithParents(pointType, point) &&
            IsValidWithChildren(pointType, point) &&
            IsValidWithSiblings(pointType, point);

        private static bool IsValidForLower(MapPointType pointType, MapPoint point) =>
            point.Coord.Row < 6
                ? !LowerMapPointRestrictions.Contains(pointType)
                : true;

        private bool IsValidForUpper(MapPointType pointType, MapPoint point) =>
            point.Coord.Row >= _mapLength - 3
                ? !UpperMapPointRestrictions.Contains(pointType)
                : true;

        private static bool IsValidWithParents(MapPointType pointType, MapPoint point) =>
            !ParentMapPointRestrictions.Contains(pointType) ||
            !point.Parents.Concat(point.Children).Any(parent => parent.PointType == pointType);

        private static bool IsValidWithChildren(MapPointType pointType, MapPoint point) =>
            !ChildMapPointRestrictions.Contains(pointType) ||
            !point.Children.Any(child => child.PointType == pointType);

        private static bool IsValidWithSiblings(MapPointType pointType, MapPoint point) =>
            !SiblingPointTypeRestrictions.Contains(pointType) ||
            !GetSiblings(point).Any(sibling => sibling.PointType == pointType);

        private static IEnumerable<MapPoint> GetSiblings(MapPoint point) =>
            point.Parents.SelectMany(parent => parent.Children).Where(sibling => !ReferenceEquals(sibling, point));

        private IEnumerable<MapPoint> GetAllMapPoints()
        {
            for (var col = 0; col < MapWidth; col++)
            {
                for (var row = 0; row < _grid.GetLength(1); row++)
                {
                    if (_grid[col, row] != null)
                    {
                        yield return _grid[col, row]!;
                    }
                }
            }
        }

        private static void ForEachInRow(
            MapPoint?[,] grid,
            int rowIndex,
            Action<MapPoint> processor)
        {
            for (var i = 0; i < grid.GetLength(0); i++)
            {
                var point = grid[i, rowIndex];
                if (point != null)
                {
                    processor(point);
                }
            }
        }

        private int GetRowCount() => _grid.GetLength(1);

        public MapLayout BuildLayout(string bossId, string ancientId)
        {
            var nodes = new List<MapNode>();
            var allPoints = new List<MapPoint>();
            var indices = new Dictionary<MapPoint, int>();

            void AddPoint(MapPoint point)
            {
                if (indices.ContainsKey(point))
                {
                    return;
                }

                indices[point] = allPoints.Count;
                allPoints.Add(point);
                nodes.Add(new MapNode(point.Coord.Col, point.Coord.Row, MapKind(point.PointType)));
            }

            AddPoint(StartingMapPoint);
            foreach (var point in GetAllMapPoints())
            {
                AddPoint(point);
            }

            AddPoint(BossMapPoint);

            var edges = new List<MapEdge>();
            foreach (var point in allPoints)
            {
                foreach (var child in point.Children)
                {
                    if (indices.TryGetValue(child, out var childIndex))
                    {
                        edges.Add(new MapEdge(indices[point], childIndex));
                    }
                }
            }

            edges.Sort((first, second) => first.From != second.From
                ? first.From.CompareTo(second.From)
                : first.To.CompareTo(second.To));
            return new MapLayout(nodes, edges, bossId, ancientId);
        }

        private static string MapKind(MapPointType type) => type switch
        {
            MapPointType.Monster => "monster",
            MapPointType.Elite => "elite",
            MapPointType.RestSite => "rest",
            MapPointType.Shop => "shop",
            MapPointType.Treasure => "treasure",
            MapPointType.Boss => "boss",
            MapPointType.Ancient => "ancient",
            _ => "unknown",
        };
    }

    private static void StableShuffle<T>(List<T> list, ref Sts2ReferenceRng.RngState rng)
        where T : IComparable<T>
    {
        var sorted = list.ToList();
        sorted.Sort();
        for (var i = 0; i < list.Count; i++)
        {
            list[i] = sorted[i];
        }

        UnstableShuffle(list, ref rng);
    }

    private static void UnstableShuffle<T>(List<T> list, ref Sts2ReferenceRng.RngState rng)
    {
        var count = list.Count;
        while (count > 1)
        {
            count--;
            var index = Sts2ReferenceRng.NextInt(ref rng, count + 1);
            (list[count], list[index]) = (list[index], list[count]);
        }
    }
}

/// <summary>
/// Port of MegaCrit.Sts2.Core.Map.MapPathPruning (v0.110.1). Keeps the
/// duplicate-segment removal and point-type repair exactly in game order.
/// </summary>
internal static class ReferenceMapPathPruning
{
    public static void PruneAndRepair(
        ReferenceActMap.MapPoint?[,] grid,
        HashSet<ReferenceActMap.MapPoint> startMapPoints,
        ReferenceActMap.MapPoint startingMapPoint,
        ReferenceActMap.MapPointTypeCounts pointTypeCounts,
        ref Sts2ReferenceRng.RngState rng,
        Func<ReferenceActMap.MapPointType, ReferenceActMap.MapPoint, bool> isValidPointType)
    {
        for (var i = 0; i < 3; i++)
        {
            PruneDuplicateSegments(grid, startMapPoints, startingMapPoint, ref rng);
            if (!RepairPrunedPointTypes(grid, pointTypeCounts, ref rng, isValidPointType))
            {
                break;
            }
        }
    }

    private static void PruneDuplicateSegments(
        ReferenceActMap.MapPoint?[,] grid,
        HashSet<ReferenceActMap.MapPoint> startMapPoints,
        ReferenceActMap.MapPoint startingMapPoint,
        ref Sts2ReferenceRng.RngState rng)
    {
        var iterations = 0;
        var matchingSegments = FindMatchingSegments(startingMapPoint);
        while (PrunePaths(grid, startMapPoints, matchingSegments, ref rng))
        {
            iterations++;
            if (iterations > 50)
            {
                throw new InvalidOperationException($"Unable to prune matching segments in {iterations} iterations");
            }

            matchingSegments = FindMatchingSegments(startingMapPoint);
        }
    }

    private static List<List<ReferenceActMap.MapPoint[]>> FindMatchingSegments(
        ReferenceActMap.MapPoint startingMapPoint)
    {
        var segments = new SortedDictionary<string, List<ReferenceActMap.MapPoint[]>>(StringComparer.Ordinal);
        CollectPaths(startingMapPoint, new List<ReferenceActMap.MapPoint>(), segments);

        return segments.Values.Where(segmentList => segmentList.Count > 1).ToList();
    }

    // DFS with a shared path buffer: identical traversal order and segment
    // addition order to the decompiled FindAllPaths/AddSegmentsToDictionary
    // pair, without materializing a List per sub-path.
    private static void CollectPaths(
        ReferenceActMap.MapPoint current,
        List<ReferenceActMap.MapPoint> path,
        IDictionary<string, List<ReferenceActMap.MapPoint[]>> segments)
    {
        path.Add(current);
        if (current.PointType == ReferenceActMap.MapPointType.Boss)
        {
            AddSegmentsToDictionary(path, segments);
        }
        else
        {
            foreach (var child in current.Children)
            {
                CollectPaths(child, path, segments);
            }
        }

        path.RemoveAt(path.Count - 1);
    }

    private static void AddSegmentsToDictionary(
        IReadOnlyList<ReferenceActMap.MapPoint> path,
        IDictionary<string, List<ReferenceActMap.MapPoint[]>> segments)
    {
        for (var i = 0; i < path.Count - 1; i++)
        {
            if (!IsValidSegmentStartMapPoint(path[i]))
            {
                continue;
            }

            for (var j = 2; j < path.Count - i; j++)
            {
                var end = path[i + j];
                if (!IsValidSegmentEndMapPoint(end))
                {
                    continue;
                }

                var segment = path.Skip(i).Take(j + 1).ToArray();
                var key = GenerateSegmentKey(segment);
                if (!segments.ContainsKey(key))
                {
                    segments[key] = new List<ReferenceActMap.MapPoint[]> { segment };
                }
                else if (!AnyOverlappingSegments(segments[key], segment))
                {
                    segments[key].Add(segment);
                }
            }
        }
    }

    private static bool IsValidSegmentStartMapPoint(ReferenceActMap.MapPoint start) =>
        start.Children.Count <= 1 ? start.Coord.Row == 0 : true;

    private static bool IsValidSegmentEndMapPoint(ReferenceActMap.MapPoint end) =>
        end.Parents.Count >= 2;

    private static string GenerateSegmentKey(IReadOnlyList<ReferenceActMap.MapPoint> segment)
    {
        var start = segment[0];
        var end = segment[^1];
        var prefix = start.Coord.Row == 0
            ? $"{start.Coord.Row}-{end.Coord.Col},{end.Coord.Row}-"
            : $"{start.Coord.Col},{start.Coord.Row}-{end.Coord.Col},{end.Coord.Row}-";
        return prefix + string.Join(",", segment.Select(point => (int)point.PointType));
    }

    private static bool AnyOverlappingSegments(
        IEnumerable<ReferenceActMap.MapPoint[]> existingSegments,
        IReadOnlyList<ReferenceActMap.MapPoint> segment) =>
        existingSegments.Any(existing => OverlappingSegment(existing, segment));

    private static bool OverlappingSegment(
        IReadOnlyList<ReferenceActMap.MapPoint> a,
        IReadOnlyList<ReferenceActMap.MapPoint> b)
    {
        if (a.Count < 3 || b.Count < 3)
        {
            return false;
        }

        for (var i = 1; i <= a.Count - 2; i++)
        {
            if (ReferenceEquals(a[i], b[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PrunePaths(
        ReferenceActMap.MapPoint?[,] grid,
        HashSet<ReferenceActMap.MapPoint> startMapPoints,
        IEnumerable<List<ReferenceActMap.MapPoint[]>> matchingSegments,
        ref Sts2ReferenceRng.RngState rng)
    {
        foreach (var matchingSegment in matchingSegments)
        {
            UnstableShuffle(matchingSegment, ref rng);
            if (PruneAllButLast(grid, startMapPoints, matchingSegment) != 0)
            {
                return true;
            }

            if (BreakAParentChildRelationshipInAnySegment(matchingSegment))
            {
                return true;
            }
        }

        return false;
    }

    private static int PruneAllButLast(
        ReferenceActMap.MapPoint?[,] grid,
        HashSet<ReferenceActMap.MapPoint> startMapPoints,
        IReadOnlyList<ReferenceActMap.MapPoint[]> matches)
    {
        var pruned = 0;
        foreach (var match in matches)
        {
            if (pruned == matches.Count - 1)
            {
                return pruned;
            }

            if (PruneSegment(grid, startMapPoints, match))
            {
                pruned++;
            }
        }

        return pruned;
    }

    private static bool PruneSegment(
        ReferenceActMap.MapPoint?[,] grid,
        HashSet<ReferenceActMap.MapPoint> startMapPoints,
        ReferenceActMap.MapPoint[] segment)
    {
        var result = false;
        for (var i = 0; i < segment.Length - 1; i++)
        {
            var point = segment[i];
            if (!IsInMap(grid, point))
            {
                return true;
            }

            if (point.Children.Count > 1 ||
                point.Parents.Count > 1 ||
                point.Parents.Any(parent => parent.Children.Count == 1 && !IsRemoved(grid, parent)))
            {
                continue;
            }

            var source = segment.Skip(i).ToArray();
            if (!source.Any(item => item.Children.Count > 1 && item.Parents.Count == 1))
            {
                if (segment[^1].Parents.Count == 1)
                {
                    return false;
                }

                if (!point.Children
                    .Where(child => !segment.Contains(child))
                    .Any(child => child.Parents.Count == 1))
                {
                    RemovePoint(grid, startMapPoints, point);
                    result = true;
                }
            }
        }

        return result;
    }

    private static void RemovePoint(
        ReferenceActMap.MapPoint?[,] grid,
        HashSet<ReferenceActMap.MapPoint> startMapPoints,
        ReferenceActMap.MapPoint point)
    {
        grid[point.Coord.Col, point.Coord.Row] = null;
        startMapPoints.Remove(point);
        foreach (var child in point.Children.ToList())
        {
            point.RemoveChildPoint(child);
        }

        foreach (var parent in point.Parents.ToList())
        {
            parent.RemoveChildPoint(point);
        }
    }

    private static bool IsInMap(ReferenceActMap.MapPoint?[,] grid, ReferenceActMap.MapPoint point)
    {
        if (grid[point.Coord.Col, point.Coord.Row] == null && point.PointType != ReferenceActMap.MapPointType.Ancient)
        {
            return point.PointType == ReferenceActMap.MapPointType.Boss;
        }

        return true;
    }

    private static bool IsRemoved(ReferenceActMap.MapPoint?[,] grid, ReferenceActMap.MapPoint point) =>
        grid[point.Coord.Col, point.Coord.Row] == null;

    private static bool BreakAParentChildRelationshipInAnySegment(
        IEnumerable<ReferenceActMap.MapPoint[]> matches)
    {
        foreach (var match in matches)
        {
            if (BreakAParentChildRelationshipInSegment(match))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BreakAParentChildRelationshipInSegment(ReferenceActMap.MapPoint[] segment)
    {
        var result = false;
        for (var i = 0; i < segment.Length - 1; i++)
        {
            var point = segment[i];
            if (point.Children.Count < 2)
            {
                continue;
            }

            var child = segment[i + 1];
            if (child.Parents.Count != 1)
            {
                point.RemoveChildPoint(child);
                result = true;
            }
        }

        return result;
    }

    private static bool RepairPrunedPointTypes(
        ReferenceActMap.MapPoint?[,] grid,
        ReferenceActMap.MapPointTypeCounts pointTypeCounts,
        ref Sts2ReferenceRng.RngState rng,
        Func<ReferenceActMap.MapPointType, ReferenceActMap.MapPoint, bool> isValidPointType)
    {
        var changed = false;
        changed |= RepairPointType(grid, ReferenceActMap.MapPointType.Shop, pointTypeCounts.NumOfShops, ref rng, isValidPointType);
        changed |= RepairPointType(grid, ReferenceActMap.MapPointType.Elite, pointTypeCounts.NumOfElites, ref rng, isValidPointType);
        changed |= RepairPointType(grid, ReferenceActMap.MapPointType.RestSite, pointTypeCounts.NumOfRests, ref rng, isValidPointType);
        return changed | RepairPointType(grid, ReferenceActMap.MapPointType.Unknown, pointTypeCounts.NumOfUnknowns, ref rng, isValidPointType);
    }

    private static bool RepairPointType(
        ReferenceActMap.MapPoint?[,] grid,
        ReferenceActMap.MapPointType type,
        int targetCount,
        ref Sts2ReferenceRng.RngState rng,
        Func<ReferenceActMap.MapPointType, ReferenceActMap.MapPoint, bool> isValidPointType)
    {
        var current = GetAllMapPoints(grid).Count(point => point.PointType == type);
        var missing = targetCount - current;
        if (missing <= 0)
        {
            return false;
        }

        var changed = false;
        var candidates = GetAllMapPoints(grid)
            .Where(point => point.PointType == ReferenceActMap.MapPointType.Monster && point.CanBeModified)
            .ToList();
        StableShuffle(candidates, ref rng);
        foreach (var candidate in candidates)
        {
            if (missing == 0)
            {
                break;
            }

            if (isValidPointType(type, candidate))
            {
                candidate.PointType = type;
                missing--;
                changed = true;
            }
        }

        return changed;
    }

    private static IEnumerable<ReferenceActMap.MapPoint> GetAllMapPoints(ReferenceActMap.MapPoint?[,] grid)
    {
        for (var col = 0; col < grid.GetLength(0); col++)
        {
            for (var row = 0; row < grid.GetLength(1); row++)
            {
                if (grid[col, row] != null)
                {
                    yield return grid[col, row]!;
                }
            }
        }
    }

    private static void StableShuffle<T>(List<T> list, ref Sts2ReferenceRng.RngState rng)
        where T : IComparable<T>
    {
        var sorted = list.ToList();
        sorted.Sort();
        for (var i = 0; i < list.Count; i++)
        {
            list[i] = sorted[i];
        }

        UnstableShuffle(list, ref rng);
    }

    private static void UnstableShuffle<T>(List<T> list, ref Sts2ReferenceRng.RngState rng)
    {
        var count = list.Count;
        while (count > 1)
        {
            count--;
            var index = Sts2ReferenceRng.NextInt(ref rng, count + 1);
            (list[count], list[index]) = (list[index], list[count]);
        }
    }
}

/// <summary>
/// Port of MegaCrit.Sts2.Core.Map.MapPostProcessing (v0.110.1): centering,
/// spreading and path straightening run after pruning, exactly as the game
/// does before the map screen reads coordinates.
/// </summary>
internal static class ReferenceMapPostProcessing
{
    public static ReferenceActMap.MapPoint?[,] CenterGrid(ReferenceActMap.MapPoint?[,] grid)
    {
        var columnCount = grid.GetLength(0);
        var rowCount = grid.GetLength(1);
        var leftEmpty = IsColumnEmpty(grid, 0) && IsColumnEmpty(grid, 1);
        var rightEmpty = IsColumnEmpty(grid, columnCount - 1) && IsColumnEmpty(grid, columnCount - 2);
        var shift = 0;
        if (leftEmpty && !rightEmpty)
        {
            shift = -1;
        }
        else if (!leftEmpty && rightEmpty)
        {
            shift = 1;
        }

        if (shift == 0)
        {
            return grid;
        }

        if (shift > 0)
        {
            for (var row = 0; row < rowCount; row++)
            {
                for (var col = columnCount - 1; col >= 0; col--)
                {
                    var point = grid[col, row];
                    grid[col, row] = null;
                    var nextCol = col + shift;
                    if (nextCol < columnCount)
                    {
                        grid[nextCol, row] = point;
                        if (point != null)
                        {
                            point.Coord.Col = nextCol;
                        }
                    }
                }
            }
        }
        else
        {
            for (var row = 0; row < rowCount; row++)
            {
                for (var col = 0; col < columnCount; col++)
                {
                    var point = grid[col, row];
                    grid[col, row] = null;
                    var nextCol = col + shift;
                    if (nextCol >= 0)
                    {
                        grid[nextCol, row] = point;
                        if (point != null)
                        {
                            point.Coord.Col = nextCol;
                        }
                    }
                }
            }
        }

        return grid;
    }

    public static ReferenceActMap.MapPoint?[,] SpreadAdjacentMapPoints(ReferenceActMap.MapPoint?[,] grid)
    {
        var columnCount = grid.GetLength(0);
        var rowCount = grid.GetLength(1);
        for (var row = 0; row < rowCount; row++)
        {
            var rowNodes = new List<ReferenceActMap.MapPoint>();
            for (var col = 0; col < columnCount; col++)
            {
                var point = grid[col, row];
                if (point != null)
                {
                    rowNodes.Add(point);
                }
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var item in rowNodes)
                {
                    var currentCol = item.Coord.Col;
                    var allowed = GetAllowedPositions(item, columnCount);
                    var currentGap = ComputeGap(currentCol, rowNodes, item);
                    var bestCol = currentCol;
                    var bestGap = currentGap;
                    foreach (var candidate in allowed)
                    {
                        if (candidate == currentCol ||
                            (grid[candidate, row] != null && grid[candidate, row] != item))
                        {
                            continue;
                        }

                        var candidateGap = ComputeGap(candidate, rowNodes, item);
                        if (candidateGap > bestGap)
                        {
                            bestCol = candidate;
                            bestGap = candidateGap;
                        }
                    }

                    if (bestCol != currentCol)
                    {
                        grid[currentCol, row] = null;
                        grid[bestCol, row] = item;
                        item.Coord.Col = bestCol;
                        changed = true;
                    }
                }
            }
        }

        return grid;
    }

    public static ReferenceActMap.MapPoint?[,] StraightenPaths(ReferenceActMap.MapPoint?[,] grid)
    {
        var columnCount = grid.GetLength(0);
        var rowCount = grid.GetLength(1);
        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < columnCount; col++)
            {
                var point = grid[col, row];
                if (point == null || point.Parents.Count != 1 || point.Children.Count != 1)
                {
                    continue;
                }

                var parent = point.Parents.First();
                var child = point.Children.First();
                var bendsLeft = point.Coord.Col < child.Coord.Col && point.Coord.Col < parent.Coord.Col;
                var bendsRight = point.Coord.Col > child.Coord.Col && point.Coord.Col > parent.Coord.Col;
                if (bendsLeft && col < columnCount - 1)
                {
                    var nextCol = col + 1;
                    if (grid[nextCol, row] == null)
                    {
                        point.Coord.Col = nextCol;
                        grid[col, row] = null;
                        grid[nextCol, row] = point;
                    }
                }

                if (bendsRight && col > 0)
                {
                    var previousCol = col - 1;
                    if (grid[previousCol, row] == null)
                    {
                        point.Coord.Col = previousCol;
                        grid[col, row] = null;
                        grid[previousCol, row] = point;
                    }
                }
            }
        }

        return grid;
    }

    private static bool IsColumnEmpty(ReferenceActMap.MapPoint?[,] grid, int col)
    {
        for (var row = 0; row < grid.GetLength(1); row++)
        {
            if (grid[col, row] != null)
            {
                return false;
            }
        }

        return true;
    }

    private static HashSet<int> GetNeighborAllowedPositions(int column, int totalColumns)
    {
        var result = new HashSet<int>();
        for (var offset = -1; offset <= 1; offset++)
        {
            var col = column + offset;
            if (col >= 0 && col < totalColumns)
            {
                result.Add(col);
            }
        }

        return result;
    }

    private static HashSet<int> GetAllowedPositions(ReferenceActMap.MapPoint node, int totalColumns)
    {
        var result = Enumerable.Range(0, totalColumns).ToHashSet();
        foreach (var parent in node.Parents)
        {
            result.IntersectWith(GetNeighborAllowedPositions(parent.Coord.Col, totalColumns));
        }

        foreach (var child in node.Children)
        {
            result.IntersectWith(GetNeighborAllowedPositions(child.Coord.Col, totalColumns));
        }

        return result;
    }

    private static int ComputeGap(
        int candidateCol,
        List<ReferenceActMap.MapPoint> rowNodes,
        ReferenceActMap.MapPoint currentNode)
    {
        var gap = int.MaxValue;
        foreach (var rowNode in rowNodes)
        {
            if (ReferenceEquals(rowNode, currentNode))
            {
                continue;
            }

            gap = Math.Min(gap, Math.Abs(candidateCol - rowNode.Coord.Col));
        }

        return gap;
    }
}
