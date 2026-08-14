using Godot;

namespace SeedSearchPrototype;

/// <summary>
/// Draws a seed's Act 1 map the same way SearchTheSpire's inspector does:
/// a 7-column node grid with dashed route edges and the game's own room icons,
/// falling back to colored glyphs when the game assets are unavailable.
/// </summary>
public sealed partial class MapPreview : Control
{
    private const float CellWidth = 44f;
    private const float RowHeight = 42f;
    private const float Pad = 16f;
    private const float PreviewScale = 0.35f;
    private static readonly Color EdgeColor = new("888888");
    private static readonly Color GlyphColor = new("ffffff");

    private static readonly IReadOnlyDictionary<string, (Color Color, string Glyph)> KindStyle =
        new Dictionary<string, (Color Color, string Glyph)>(StringComparer.Ordinal)
        {
            ["monster"] = (new Color("c0392b"), "M"),
            ["elite"] = (new Color("8e44ad"), "E"),
            ["rest"] = (new Color("27ae60"), "R"),
            ["shop"] = (new Color("e1b12c"), "$"),
            ["treasure"] = (new Color("e67e22"), "T"),
            ["unknown"] = (new Color("7f8c8d"), "?"),
            ["boss"] = (new Color("7b241c"), "B"),
            ["ancient"] = (new Color("2980b9"), "A"),
        };

    private readonly MapLayout _layout;
    private readonly Vector2[] _positions;
    private readonly Texture2D?[] _icons;

    public MapPreview(MapLayout layout)
    {
        _layout = layout;
        MouseFilter = MouseFilterEnum.Ignore;

        var maxRow = 0;
        foreach (var node in layout.Nodes)
        {
            maxRow = Mathf.Max(maxRow, node.Row);
        }

        CustomMinimumSize = new Vector2(7 * CellWidth + Pad * 2, (maxRow + 1) * RowHeight + Pad * 2);
        _positions = new Vector2[layout.Nodes.Count];
        _icons = new Texture2D?[layout.Nodes.Count];
        for (var index = 0; index < layout.Nodes.Count; index++)
        {
            var node = layout.Nodes[index];
            _positions[index] = new Vector2(
                Pad + node.Col * CellWidth + CellWidth / 2f,
                Pad + (maxRow - node.Row) * RowHeight + RowHeight / 2f);
            AddTooltipNode(node, _positions[index]);
            var icon = GameArtPreview.MapNodeIcon(node.Kind, _layout.BossId, _layout.AncientId);
            _icons[index] = icon;
            if (icon == null)
            {
                continue;
            }

            // Mirror the game's per-kind icon boxes (normal 92x92,
            // boss 374x350, ancient 208x208) at preview scale, centered on
            // the node. FitWidthProportion keeps atlas regions from drawing
            // at their native size, which was misaligning and inflating them.
            var size = DisplaySize(node.Kind);
            AddChild(new TextureRect
            {
                Texture = icon,
                Position = _positions[index] - Vector2.One * size / 2f,
                Size = Vector2.One * size,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }
    }

    private void AddTooltipNode(MapNode node, Vector2 center)
    {
        var region = Mathf.Min(CellWidth, RowHeight);
        var hit = new Control
        {
            Position = center - Vector2.One * region * 0.5f,
            Size = Vector2.One * region,
            TooltipText = Localization.T(KindLabel(node.Kind)),
            MouseFilter = MouseFilterEnum.Pass,
        };
        AddChild(hit);
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        foreach (var edge in _layout.Edges)
        {
            if (edge.From < 0 || edge.From >= _positions.Length ||
                edge.To < 0 || edge.To >= _positions.Length)
            {
                continue;
            }

            DrawDashedLine(_positions[edge.From], _positions[edge.To], EdgeColor, 1.3f, 2.5f, 2.5f);
        }

        for (var index = 0; index < _layout.Nodes.Count; index++)
        {
            var node = _layout.Nodes[index];
            if (_icons[index] != null)
            {
                continue;
            }

            var style = KindStyle.TryGetValue(node.Kind, out var value)
                ? value
                : (Color: new Color("000000"), Glyph: "·");
            var center = _positions[index];
            var size = DisplaySize(node.Kind);
            DrawCircle(center, size * 0.24f, style.Color);

            var fontSize = node.Kind is "boss" or "ancient" ? 14 : 10;
            var textWidth = font.GetStringSize(style.Glyph, HorizontalAlignment.Left, -1, fontSize).X;
            DrawString(
                font,
                new Vector2(center.X - textWidth / 2f, center.Y + fontSize * 0.36f),
                style.Glyph,
                HorizontalAlignment.Left,
                -1,
                fontSize,
                GlyphColor);
        }
    }

    private static float DisplaySize(string kind) =>
        (kind switch
        {
            "boss" => 374f,
            "ancient" => 208f,
            _ => 92f,
        }) * PreviewScale;

    private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width, float dash, float gap)
    {
        var delta = to - from;
        var length = delta.Length();
        if (length <= 0.001f)
        {
            return;
        }

        var direction = delta / length;
        var traveled = 0f;
        while (traveled < length)
        {
            var end = Mathf.Min(traveled + dash, length);
            DrawLine(from + direction * traveled, from + direction * end, color, width, true);
            traveled = end + gap;
        }
    }

    private static string KindLabel(string kind) => kind switch
    {
        "monster" => "Monster",
        "elite" => "Elite",
        "rest" => "Rest site",
        "shop" => "Shop",
        "treasure" => "Treasure",
        "unknown" => "Unknown",
        "boss" => "Boss",
        "ancient" => "Ancient",
        _ => kind,
    };
}
