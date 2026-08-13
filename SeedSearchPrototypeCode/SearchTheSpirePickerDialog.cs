using Godot;

namespace SeedSearchPrototype;

/// <summary>
/// Godot counterpart of SearchTheSpire's sectioned picker modal.  It is kept
/// as a standalone control so opening a picker never changes the game's root
/// input policy; the modal is visible only while it owns the interaction.
/// </summary>
public partial class SearchTheSpirePickerDialog : Control
{
    private const int ArtGridColumns = 4;

    private static readonly Color Backdrop = new("0b0e14cc");
    private static readonly Color Panel = new("1c222c");
    private static readonly Color Background = new("12151b");
    private static readonly Color Raised = new("252d39");
    private static readonly Color Border = new("3d4858");
    private static readonly Color Text = new("e8edf5");
    private static readonly Color Muted = new("9ba8b8");
    private static readonly Color Accent = new("d9a441");
    private static readonly Color AccentDark = new("5e461f");
    private static readonly Color Danger = new("e09a8f");

    private readonly Dictionary<string, bool> _collapsedSections = new(StringComparer.Ordinal);
    private readonly List<SearchTheSpireOption> _options = new();
    private LineEdit _search = null!;
    private VBoxContainer _optionList = null!;
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private TextureRect _previewImage = null!;
    private Label _previewTitle = null!;
    private Label _previewSubtitle = null!;
    private Action<string>? _onPick;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildChrome();
        Visible = false;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!Visible || @event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (key.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Open(
        string title,
        IEnumerable<SearchTheSpireOption> options,
        Action<string> onPick)
    {
        _title.Text = Localization.T(title);
        _options.Clear();
        _options.AddRange(options);
        _onPick = onPick;
        _search.Text = string.Empty;
        _collapsedSections.Clear();
        ClearPreview();
        Visible = true;
        RefreshOptions();
        _search.GrabFocus();
    }

    public void Close()
    {
        Visible = false;
        ClearPreview();
        _onPick = null;
    }

    private void BuildChrome()
    {
        var backdrop = new ColorRect
        {
            Color = Backdrop,
            MouseFilter = MouseFilterEnum.Stop,
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        backdrop.GuiInput += OnBackdropInput;
        AddChild(backdrop);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        _panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(1120, 600),
            MouseFilter = MouseFilterEnum.Stop,
        };
        _panel.AddThemeStyleboxOverride("panel", MakeStyle(Panel, Border, 10));
        center.AddChild(_panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        _panel.AddChild(margin);

        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 14);
        margin.AddChild(body);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        content.AddThemeConstantOverride("separation", 10);
        body.AddChild(content);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        content.AddChild(header);
        _title = MakeLabel("pick", 18, Text);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_title);
        var close = MakeButton("×", "Close picker", 38);
        close.Pressed += Close;
        header.AddChild(close);

        _search = new LineEdit
        {
            PlaceholderText = Localization.T("search…"),
            CustomMinimumSize = new Vector2(0, 38),
        };
        _search.TextChanged += _ => RefreshOptions();
        _search.TextSubmitted += _ => PickFirstVisible();
        content.AddChild(_search);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 440),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        content.AddChild(scroll);
        _optionList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _optionList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_optionList);

        body.AddChild(BuildPreviewPanel());
    }

    private Control BuildPreviewPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(340, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Backdrop, Border, 8));
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(content);

        var image = new TextureRect
        {
            CustomMinimumSize = new Vector2(0, 420),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        content.AddChild(image);

        _previewTitle = MakeLabel("", 16, Text);
        _previewTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_previewTitle);

        _previewSubtitle = MakeLabel("", 12, Muted);
        _previewSubtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_previewSubtitle);

        _previewImage = image;
        return panel;
    }

    private void ShowPreview(SearchTheSpireOption option)
    {
        _previewTitle.Text = Localization.OptionTitle(option);
        _previewSubtitle.Text = Localization.OptionSection(option);
        var texture = GameArtPreview.OptionArt(option);
        _previewImage.Texture = texture;
        _previewImage.Visible = texture != null;
    }

    private void ClearPreview()
    {
        if (_previewImage == null)
        {
            return;
        }

        _previewImage.Texture = null;
        _previewImage.Visible = false;
        _previewTitle.Text = string.Empty;
        _previewSubtitle.Text = string.Empty;
    }

    private void RefreshOptions()
    {
        if (_optionList == null)
        {
            return;
        }

        ClearChildren(_optionList);
        var filtered = SearchTheSpirePicker.Filter(_options, _search.Text);
        if (filtered.Count == 0)
        {
            _optionList.AddChild(MakeLabel("no matching options", 13, Muted));
            return;
        }

        foreach (var section in filtered.GroupBy(option => option.Section, StringComparer.Ordinal))
        {
            var sectionName = section.Key;
            var collapsed = _collapsedSections.TryGetValue(sectionName, out var isCollapsed) && isCollapsed;
            var heading = MakeButton(
                $"{(collapsed ? "▸" : "▾")} {Localization.T(sectionName)} · {section.Count()}",
                "Collapse or expand this option group",
                0);
            heading.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            heading.Alignment = HorizontalAlignment.Left;
            heading.Pressed += () =>
            {
                _collapsedSections[sectionName] = !collapsed;
                RefreshOptions();
            };
            _optionList.AddChild(heading);

            if (collapsed)
            {
                continue;
            }

            if (OptionArtRouter.For(section.Key) != OptionArtKind.None)
            {
                var grid = new GridContainer { Columns = ArtGridColumns };
                grid.AddThemeConstantOverride("h_separation", 8);
                grid.AddThemeConstantOverride("v_separation", 8);
                grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                foreach (var option in section)
                {
                    grid.AddChild(BuildArtOption(option));
                }

                _optionList.AddChild(grid);
                continue;
            }

            foreach (var option in section)
            {
                var button = MakeButton(
                    Localization.OptionTitle(option),
                    Localization.OptionDescription(option),
                    0);
                button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                button.Alignment = HorizontalAlignment.Left;
                button.Disabled = option.Blocked;
                if (option.Blocked)
                {
                    button.TooltipText = Localization.OptionBlockReason(option);
                    button.AddThemeColorOverride("font_color", Muted);
                }
                else
                {
                    button.MouseEntered += () => ShowPreview(option);
                    button.FocusEntered += () => ShowPreview(option);
                    button.Pressed += () =>
                    {
                        var onPick = _onPick;
                        Close();
                        onPick?.Invoke(option.Id);
                    };
                }

                _optionList.AddChild(button);
            }
        }
    }

    private Button BuildArtOption(SearchTheSpireOption option)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(150, 210),
            FocusMode = FocusModeEnum.All,
            TooltipText = Localization.OptionDescription(option),
        };
        button.AddThemeStyleboxOverride("normal", MakeStyle(Raised, Border, 6));
        button.AddThemeStyleboxOverride("hover", MakeStyle(AccentDark, Accent, 6));
        button.AddThemeStyleboxOverride("pressed", MakeStyle(AccentDark, Accent, 6));
        button.AddThemeColorOverride("font_color", Text);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        margin.MouseFilter = MouseFilterEnum.Ignore;
        button.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 4);
        content.MouseFilter = MouseFilterEnum.Ignore;
        margin.AddChild(content);

        var image = new TextureRect
        {
            Texture = GameArtPreview.OptionArt(option),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(0, 128),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        content.AddChild(image);

        var title = MakeLabel(Localization.OptionTitle(option), 12, Text);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        title.CustomMinimumSize = new Vector2(0, 34);
        title.MouseFilter = MouseFilterEnum.Ignore;
        content.AddChild(title);

        button.Disabled = option.Blocked;
        if (option.Blocked)
        {
            button.TooltipText = Localization.OptionBlockReason(option);
            button.Modulate = new Color(0.55f, 0.55f, 0.55f, 0.85f);
        }
        else
        {
            button.MouseEntered += () => ShowPreview(option);
            button.FocusEntered += () => ShowPreview(option);
            button.Pressed += () =>
            {
                var onPick = _onPick;
                Close();
                onPick?.Invoke(option.Id);
            };
        }

        return button;
    }

    private void PickFirstVisible()
    {
        var first = SearchTheSpirePicker.Filter(_options, _search.Text)
            .FirstOrDefault(option => !option.Blocked);
        if (first == null)
        {
            return;
        }

        var onPick = _onPick;
        Close();
        onPick?.Invoke(first.Id);
    }

    private void OnBackdropInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed &&
            mouse.ButtonIndex == MouseButton.Left && !_panel.GetGlobalRect().HasPoint(mouse.Position))
        {
            Close();
        }
    }

    private static Button MakeButton(string text, string tooltip, int width)
    {
        var button = new Button
        {
            Text = Localization.T(text),
            TooltipText = Localization.T(tooltip),
            CustomMinimumSize = new Vector2(width, 36),
            FocusMode = FocusModeEnum.All,
        };
        button.AddThemeStyleboxOverride("normal", MakeStyle(Raised, Border, 6));
        button.AddThemeStyleboxOverride("hover", MakeStyle(AccentDark, Accent, 6));
        button.AddThemeStyleboxOverride("pressed", MakeStyle(AccentDark, Accent, 6));
        button.AddThemeColorOverride("font_color", Text);
        return button;
    }

    private static Label MakeLabel(string text, int size, Color color)
    {
        var label = new Label { Text = Localization.T(text) };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return label;
    }

    private static StyleBoxFlat MakeStyle(Color background, Color border, int radius)
    {
        var style = new StyleBoxFlat { BgColor = background, BorderColor = border };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(radius);
        return style;
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            child.QueueFree();
        }
    }
}
