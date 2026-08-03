using System.Globalization;
using System.Text.Json;
using Godot;

namespace SeedSearchPrototype;

public partial class SeedSearchOverlay : CanvasLayer
{
    private static readonly Color Background = new("12151b");
    private static readonly Color Surface = new("1c222c");
    private static readonly Color SurfaceRaised = new("252d39");
    private static readonly Color Border = new("3d4858");
    private static readonly Color Text = new("e8edf5");
    private static readonly Color MutedText = new("9ba8b8");
    private static readonly Color Accent = new("d9a441");
    private static readonly Color AccentDark = new("5e461f");
    private static readonly Color Danger = new("c56d65");

    private readonly SeedSearchEngine _engine = new();
    private readonly List<SavedSearch> _savedSearches = new();

    private Control _shell = null!;
    private Control _backdrop = null!;
    private Control _boardPage = null!;
    private Control _popularPage = null!;
    private Control _savedPage = null!;
    private VBoxContainer _resultsList = null!;
    private VBoxContainer _savedList = null!;
    private Label _statusLabel = null!;
    private Label _progressLabel = null!;
    private Label _resultCountLabel = null!;
    private Label _inspectStatusLabel = null!;
    private LineEdit _inspectInput = null!;
    private LineEdit _advancedInput = null!;
    private OptionButton _branchInput = null!;
    private OptionButton _stopAfterInput = null!;
    private OptionButton _maxCandidatesInput = null!;
    private Button _neowInputButton = null!;
    private VBoxContainer _neowDetails = null!;
    private VBoxContainer _advancedDetails = null!;
    private OptionButton _eliteInput = null!;
    private OptionButton _shopInput = null!;
    private OptionButton _restInput = null!;
    private OptionButton _characterInput = null!;
    private OptionButton _ascensionInput = null!;
    private OptionButton _runModeInput = null!;
    private OptionButton _ancientInput = null!;
    private OptionButton _bossInput = null!;
    private Button _searchButton = null!;
    private Button _cancelButton = null!;
    private Button _spoilerButton = null!;
    private SearchTheSpirePickerDialog _pickerDialog = null!;
    private SearchTheSpireBoardState _boardState = SearchTheSpireBoardState.Empty;

    private CancellationTokenSource? _searchCancellation;
    private Task<IReadOnlyList<SeedMatch>>? _searchTask;
    private SeedQuery? _lastQuery;
    private IReadOnlyList<SeedMatch> _lastResults = Array.Empty<SeedMatch>();
    private long _lastProgress;
    private bool _showSpoilers;

    public override void _Ready()
    {
        Layer = 1000;
        ProcessMode = ProcessModeEnum.Always;
        SetProcess(true);

        _shell = new Control
        {
            Name = "SeedSearchShell",
            // The shell fills the viewport; only its child controls should receive mouse input.
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _shell.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_shell);

        BuildLauncher();
        BuildPages();
        LoadSavedSearches();
        ShowBoard();
    }

    public override void _Process(double delta)
    {
        if (_searchTask is { IsCompleted: true } completedTask)
        {
            _searchTask = null;
            _searchCancellation?.Dispose();
            _searchCancellation = null;

            if (completedTask.IsCanceled)
            {
                RestoreSearchUi();
                SetStatus("search cancelled", MutedText);
            }
            else if (completedTask.IsFaulted)
            {
                MainFile.Logger.Error($"Seed search failed: {completedTask.Exception}");
                RestoreSearchUi();
                SetStatus("search failed; check godot.log", Danger);
            }
            else
            {
                try
                {
                    ApplyResults(completedTask.GetAwaiter().GetResult());
                }
                catch (Exception exception)
                {
                    MainFile.Logger.Error($"Seed search result handling failed: {exception}");
                    RestoreSearchUi();
                    SetStatus("search failed; check godot.log", Danger);
                }
            }
        }

        if (_searchTask != null)
        {
            _progressLabel.Text = $"searching · {_lastProgress.ToString("N0", CultureInfo.InvariantCulture)} candidates checked";
        }
    }

    private void BuildLauncher()
    {
        var launcher = new Button
        {
            Text = "Seed Search",
            TooltipText = "Open the seed search board",
            FocusMode = Control.FocusModeEnum.All,
            Size = new Vector2(170, 48)
        };
        launcher.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        launcher.Position = new Vector2(-190, -70);
        launcher.AddThemeStyleboxOverride("normal", MakeStyle(SurfaceRaised, Border));
        launcher.AddThemeStyleboxOverride("hover", MakeStyle(AccentDark, Accent));
        launcher.AddThemeColorOverride("font_color", Text);
        launcher.Pressed += () => _backdrop.Visible = true;
        _shell.AddChild(launcher);
    }

    private void BuildPages()
    {
        _backdrop = new ColorRect
        {
            Name = "SeedSearchBackdrop",
            Color = new Color(0.02f, 0.025f, 0.035f, 0.92f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false
        };
        _backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _shell.AddChild(_backdrop);

        var center = new CenterContainer { Name = "SeedSearchCenter" };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _backdrop.AddChild(center);

        var panel = new PanelContainer
        {
            Name = "SeedSearchPanel",
            CustomMinimumSize = new Vector2(1240, 790)
        };
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Background, Border, 12));
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        panel.AddChild(margin);

        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 12);
        margin.AddChild(page);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        page.AddChild(header);
        var title = MakeLabel("Search TheSpire", 26, Text);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(title);
        var beta = MakeLabel("v0.110.1", 13, MutedText);
        beta.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(beta);
        var closeButton = MakeButton("×", "Close", 40);
        closeButton.Pressed += () => _backdrop.Visible = false;
        header.AddChild(closeButton);

        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 8);
        page.AddChild(tabs);
        var boardButton = MakeButton("Board", "Show search board", 100);
        var popularButton = MakeButton("Popular", "Show popular searches", 100);
        var savedButton = MakeButton("Saved", "Show saved searches", 100);
        boardButton.Pressed += ShowBoard;
        popularButton.Pressed += ShowPopular;
        savedButton.Pressed += ShowSaved;
        tabs.AddChild(boardButton);
        tabs.AddChild(popularButton);
        tabs.AddChild(savedButton);

        _boardPage = BuildBoardPage();
        _popularPage = BuildInfoPage("Popular this week", "nothing popular yet this week. Searches will appear here after more local searches are saved.");
        _savedPage = BuildSavedPage();
        page.AddChild(_boardPage);
        page.AddChild(_popularPage);
        page.AddChild(_savedPage);

        // The picker is a sibling of the page/backdrop, so it can temporarily
        // own input without changing the shell's pass-through policy.
        _pickerDialog = new SearchTheSpirePickerDialog { Name = "SearchTheSpirePicker" };
        _shell.AddChild(_pickerDialog);
    }

    private Control BuildBoardPage()
    {
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 12);
        page.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var searchBar = new HBoxContainer();
        searchBar.AddThemeConstantOverride("separation", 8);
        page.AddChild(searchBar);
        searchBar.AddChild(MakeLabel("searching", 14, MutedText));
        _branchInput = MakeOption(new[] { "public beta · v0.110.1" }, 0, 240);
        searchBar.AddChild(_branchInput);
        var shareButton = MakeButton("share", "Copy a shareable search spec", 92);
        shareButton.Pressed += ShareSearch;
        searchBar.AddChild(shareButton);
        var clearButton = MakeButton("clear", "Clear the board", 82);
        clearButton.Pressed += ClearBoard;
        searchBar.AddChild(clearButton);

        var split = new HSplitContainer { Name = "SearchSplit" };
        split.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        split.CustomMinimumSize = new Vector2(0, 540);
        page.AddChild(split);

        split.AddChild(BuildFilterPanel());
        split.AddChild(BuildResultsPanel());

        return page;
    }

    private Control BuildFilterPanel()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(390, 0) };
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Surface, Border, 8));
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        panel.AddChild(scroll);
        var margin = MakeMargin(16);
        scroll.AddChild(margin);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(content);

        content.AddChild(MakeLabel("compose the run start you want", 16, Text));
        content.AddChild(MakeLabel("empty slots match anything", 12, MutedText));
        content.AddChild(MakeLabel("bulk search · reference RNG projection · Inspect uses game runtime when available", 11, MutedText));

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 8);
        content.AddChild(grid);

        _neowInputButton = MakeButton("any Neow offers", "Open Neow offer picker", 210);
        _neowInputButton.Pressed += () => OpenSlotPicker("neowOffer");
        _characterInput = MakeOption(new[] { "any character", "Ironclad", "Silent", "Regent", "Defect", "Necrobinder" }, 0, 210);
        _characterInput.ItemSelected += index =>
        {
            _boardState = _boardState.WithCharacter((RunCharacter)index);
            RefreshNeowDetails();
            RefreshAdvancedDetails();
        };
        _ascensionInput = MakeOption(new[] { "Ascension 0", "Ascension 5", "Ascension 10", "Ascension 15", "Ascension 20" }, 0, 210);
        _ascensionInput.ItemSelected += index =>
        {
            _boardState = _boardState.WithAscension((int)index * 5);
            RefreshAdvancedDetails();
        };
        _runModeInput = MakeOption(new[] { "plain run" }, 0, 210);
        _eliteInput = MakeOption(new[] { "0+ elites", "1+ elites", "2+ elites", "3+ elites" }, 0, 210);
        _shopInput = MakeOption(new[] { "0+ shops", "1+ shops", "2+ shops" }, 0, 210);
        _restInput = MakeOption(new[] { "0+ rest sites", "1+ rest sites", "2+ rest sites", "3+ rest sites" }, 0, 210);
        _ancientInput = MakeOption(new[] { "Any", "Ancient A", "Ancient B", "Ancient C", "Ancient D" }, 0, 210);
        _bossInput = MakeOption(new[] { "Any", "Boss 1", "Boss 2", "Boss 3" }, 0, 210);
        AddField(grid, "Neow", _neowInputButton);
        AddField(grid, "Character", _characterInput);
        AddField(grid, "Ascension", _ascensionInput);
        AddField(grid, "Run mode", _runModeInput);
        AddField(grid, "Act 1 elites", _eliteInput);
        AddField(grid, "Act 1 shops", _shopInput);
        AddField(grid, "Act 1 rests", _restInput);
        AddField(grid, "Ancient", _ancientInput);
        AddField(grid, "Boss", _bossInput);

        _neowDetails = new VBoxContainer();
        _neowDetails.AddThemeConstantOverride("separation", 6);
        content.AddChild(_neowDetails);
        RefreshNeowDetails();

        content.AddChild(MakeSeparator());
        content.AddChild(MakeLabel("run layout & drop pins", 15, Text));
        content.AddChild(MakeLabel("bosses, ancients, rewards, shops, relic bags and events use the same nested picker model", 11, MutedText));
        _advancedDetails = new VBoxContainer();
        _advancedDetails.AddThemeConstantOverride("separation", 6);
        content.AddChild(_advancedDetails);
        RefreshAdvancedDetails();

        content.AddChild(MakeSeparator());
        content.AddChild(MakeLabel("search controls", 15, Text));
        var controls = new GridContainer { Columns = 2 };
        controls.AddThemeConstantOverride("h_separation", 10);
        controls.AddThemeConstantOverride("v_separation", 8);
        content.AddChild(controls);
        _stopAfterInput = MakeOption(new[] { "5 matches", "10 matches", "20 matches", "50 matches" }, 2, 210);
        _maxCandidatesInput = MakeOption(new[] { "10k candidates", "50k candidates", "250k candidates", "1m candidates" }, 2, 210);
        _advancedInput = new LineEdit
        {
            Text = "0",
            PlaceholderText = "0",
            CustomMinimumSize = new Vector2(210, 36)
        };
        AddField(controls, "stop after", _stopAfterInput);
        AddField(controls, "runs to search", _maxCandidatesInput);
        AddField(controls, "advanced offset", _advancedInput);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        content.AddChild(actions);
        _searchButton = MakeButton("Search", "Search seeds", 110);
        _searchButton.Pressed += StartSearch;
        actions.AddChild(_searchButton);
        _cancelButton = MakeButton("Cancel", "Cancel the running search", 100);
        _cancelButton.Disabled = true;
        _cancelButton.Pressed += CancelSearch;
        actions.AddChild(_cancelButton);

        _progressLabel = MakeLabel("ready", 12, MutedText);
        _progressLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_progressLabel);

        content.AddChild(MakeSeparator());
        content.AddChild(MakeLabel("inspect a seed", 15, Text));
        content.AddChild(MakeLabel("preview any seed's run start, no search needed", 12, MutedText));
        var inspectRow = new HBoxContainer();
        inspectRow.AddThemeConstantOverride("separation", 8);
        content.AddChild(inspectRow);
        _inspectInput = new LineEdit
        {
            PlaceholderText = "paste a seed…",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 36)
        };
        inspectRow.AddChild(_inspectInput);
        var inspectButton = MakeButton("Inspect", "Inspect this seed", 86);
        inspectButton.Pressed += InspectSeed;
        inspectRow.AddChild(inspectButton);
        _inspectStatusLabel = MakeLabel("", 12, MutedText);
        _inspectStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_inspectStatusLabel);

        return panel;
    }

    private Control BuildResultsPanel()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Surface, Border, 8));
        var margin = MakeMargin(16);
        panel.AddChild(margin);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(content);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        content.AddChild(header);
        var heading = MakeLabel("results", 16, Text);
        heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(heading);
        _resultCountLabel = MakeLabel("0 matches", 12, MutedText);
        header.AddChild(_resultCountLabel);
        _spoilerButton = MakeButton("show all spoilers", "Toggle result detail", 150);
        _spoilerButton.Pressed += ToggleSpoilers;
        header.AddChild(_spoilerButton);

        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        content.AddChild(headerRow);
        headerRow.AddChild(MakeCell("seed", 132, MutedText, true));
        headerRow.AddChild(MakeCell("act 1 map", 270, MutedText, true));
        headerRow.AddChild(MakeCell("neow offers", 140, MutedText, true));
        headerRow.AddChild(MakeCell("ancients", 120, MutedText, true));
        headerRow.AddChild(MakeCell("bosses", 150, MutedText, true));

        var scroll = new ScrollContainer { Name = "ResultsScroll" };
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddChild(scroll);
        _resultsList = new VBoxContainer();
        _resultsList.AddThemeConstantOverride("separation", 6);
        _resultsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_resultsList);

        _statusLabel = MakeLabel("results show up here. select some filters, then hit Search.", 13, MutedText);
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_statusLabel);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        content.AddChild(actions);
        var saveButton = MakeButton("save this search", "Save this search in the mod", 138);
        saveButton.Pressed += SaveSearch;
        actions.AddChild(saveButton);
        var randomButton = MakeButton("copy random seed", "Copy a seed from the current branch", 144);
        randomButton.Pressed += CopyRandomSeed;
        actions.AddChild(randomButton);

        return panel;
    }

    private Control BuildInfoPage(string title, string message)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Surface, Border, 8));
        var margin = MakeMargin(24);
        panel.AddChild(margin);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 12);
        margin.AddChild(content);
        content.AddChild(MakeLabel(title, 20, Text));
        var label = MakeLabel(message, 14, MutedText);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(label);
        return panel;
    }

    private Control BuildSavedPage()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Surface, Border, 8));
        var margin = MakeMargin(24);
        panel.AddChild(margin);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 12);
        margin.AddChild(content);
        content.AddChild(MakeLabel("Saved in this mod", 20, Text));
        content.AddChild(MakeLabel("saved searches remain available while this game session is open.", 14, MutedText));
        _savedList = new VBoxContainer();
        _savedList.AddThemeConstantOverride("separation", 8);
        content.AddChild(_savedList);
        return panel;
    }

    private void StartSearch()
    {
        if (_searchTask != null)
        {
            return;
        }

        var query = ReadQuery();
        _lastQuery = query;
        _lastProgress = 0;
        _searchCancellation = new CancellationTokenSource();
        var token = _searchCancellation.Token;
        _searchTask = Task.Run(
            () => _engine.Search(query, token, progress => Interlocked.Exchange(ref _lastProgress, progress.Checked)),
            token);

        _searchButton.Disabled = true;
        _cancelButton.Disabled = false;
        _progressLabel.Text = "searching";
        SetStatus("searching…", MutedText);
    }

    private void CancelSearch()
    {
        if (_searchTask == null)
        {
            return;
        }

        _searchCancellation?.Cancel();
        _progressLabel.Text = "cancelling…";
    }

    private void InspectSeed()
    {
        var seed = _inspectInput.Text.Trim();
        if (seed.Length == 0)
        {
            _inspectStatusLabel.Text = "paste a seed first";
            return;
        }

        var query = ReadQuery();
        var snapshot = GameSeedRuntimeBackend.TryInspect(
            seed,
            query.Branch,
            query.Character,
            query.Ascension,
            BuildContext(query),
            out var runtimeSnapshot,
            out var runtimeReason)
            ? runtimeSnapshot
            : _engine.Inspect(seed, query.Branch, BuildContext(query));
        if (runtimeSnapshot == null && !string.IsNullOrWhiteSpace(runtimeReason))
        {
            MainFile.Logger.Warn($"Game runtime preview unavailable for {seed}: {runtimeReason}");
        }

        var match = new SeedMatch(seed, snapshot);
        _lastQuery = query;
        ApplyResults(new[] { match });
        _inspectStatusLabel.Text = snapshot.Backend == "game-runtime"
            ? $"inspected {seed} · game runtime"
            : $"inspected {seed} · reference engine";
        SetStatus($"inspected {seed} · {BackendLabel(snapshot.Backend)}", Accent);
    }

    private void ApplyResults(IReadOnlyList<SeedMatch> results)
    {
        _lastResults = results;
        _searchButton.Disabled = false;
        _cancelButton.Disabled = true;
        _resultCountLabel.Text = $"{results.Count} matches";
        _progressLabel.Text = "search complete";
        ClearChildren(_resultsList);

        if (results.Count == 0)
        {
            SetStatus("no seeds matched. remove or loosen a filter and search again.", MutedText);
            return;
        }

        foreach (var result in results)
        {
            _resultsList.AddChild(BuildResultRow(result));
        }

        var hitMatchCap = _lastQuery != null && results.Count >= _lastQuery.StopAfter;
        SetStatus(hitMatchCap ? "stopped at the match cap" : $"searched {_lastProgress:N0} candidates", Accent);
    }

    private Control BuildResultRow(SeedMatch match)
    {
        var wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 4);

        var row = new PanelContainer();
        row.AddThemeStyleboxOverride("panel", MakeStyle(SurfaceRaised, Border, 6));
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        var margin = MakeMargin(8);
        margin.AddChild(content);
        row.AddChild(margin);
        wrapper.AddChild(row);

        var seedButton = MakeButton(match.Seed, "Copy seed", 132);
        seedButton.Pressed += () => CopyToClipboard(match.Seed);
        var detail = BuildResultDetail(match);
        detail.Visible = false;
        var detailButton = MakeButton("details", "Expand this seed's preview", 132);
        detailButton.Pressed += () =>
        {
            detail.Visible = !detail.Visible;
            detailButton.Text = detail.Visible ? "hide details" : "details";
        };
        var seedColumn = new VBoxContainer();
        seedColumn.AddThemeConstantOverride("separation", 4);
        seedColumn.CustomMinimumSize = new Vector2(132, 0);
        seedColumn.AddChild(seedButton);
        seedColumn.AddChild(detailButton);
        content.AddChild(seedColumn);
        content.AddChild(MakeCell(match.Snapshot.Act1Map, 270, Text));
        content.AddChild(MakeCell(match.Snapshot.NeowOffers, 140, Text));
        content.AddChild(MakeCell(match.Snapshot.Ancients, 120, Text));
        content.AddChild(MakeCell(match.Snapshot.Bosses, 150, Text));
        if (_showSpoilers)
        {
            var spoiler = MakeLabel($"E{match.Snapshot.EliteCount} · ${match.Snapshot.ShopCount} · R{match.Snapshot.RestSiteCount}", 12, Accent);
            spoiler.CustomMinimumSize = new Vector2(120, 36);
            spoiler.VerticalAlignment = VerticalAlignment.Center;
            content.AddChild(spoiler);
        }

        wrapper.AddChild(detail);

        return wrapper;
    }

    private Control BuildResultDetail(SeedMatch match)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Background, Border, 6));
        var margin = MakeMargin(12);
        panel.AddChild(margin);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 6);
        margin.AddChild(content);

        content.AddChild(MakeLabel("seed preview", 13, Accent));
        content.AddChild(MakeLabel($"backend · {BackendLabel(match.Snapshot.Backend)}", 12, MutedText));
        content.AddChild(MakeLabel($"Act 1 path · {match.Snapshot.Act1Map}", 12, Text));
        content.AddChild(MakeLabel($"Neow detail · {match.Snapshot.NeowOffers}", 12, Text));
        content.AddChild(MakeLabel($"Ancients · {match.Snapshot.Ancients}    Bosses · {match.Snapshot.Bosses}", 12, Text));
        content.AddChild(MakeLabel(
            $"early route counts · {match.Snapshot.EliteCount} elites · {match.Snapshot.ShopCount} shops · {match.Snapshot.RestSiteCount} rest sites",
            12,
            MutedText));

        if (_lastQuery is { HiddenSpec.Length: > 0 } query)
        {
            content.AddChild(MakeLabel($"extended pins · {query.HiddenSpec}", 12, MutedText));
        }

        var copy = MakeButton("copy seed", "Copy this seed", 94);
        copy.Pressed += () => CopyToClipboard(match.Seed);
        content.AddChild(copy);
        return panel;
    }

    private static string BackendLabel(string backend) => backend switch
    {
        "game-runtime" => "game runtime",
        "reference-rng" => "reference RNG",
        _ => backend,
    };

    private void OpenSlotPicker(string slotId)
    {
        var view = _boardState.DescribeSlot(slotId);
        var options = new List<SearchTheSpireOption>
        {
            new("", "any", "clear this slot", "Leave this constraint open."),
        };
        options.AddRange(view.Options);
        _pickerDialog.Open(view.Slot.Label, options, value => SetBoardSlot(slotId, value));
    }

    private void SetBoardSlot(string slotId, string? value)
    {
        _boardState = _boardState.Select(slotId, string.IsNullOrEmpty(value) ? null : value);
        if (slotId == "neowOffer")
        {
            var selected = _boardState.Selected(slotId);
            _neowInputButton.Text = selected == null
                ? "any Neow offers"
                : SearchTheSpireCatalog.OptionsFor(SearchTheSpireCatalog.GetSlot(slotId), _boardState)
                    .FirstOrDefault(option => option.Id == selected)?.Title ?? selected;
        }

        RefreshNeowDetails();
        RefreshAdvancedDetails();
    }

    private void RefreshNeowDetails()
    {
        if (_neowDetails == null)
        {
            return;
        }

        ClearChildren(_neowDetails);
        var offer = _boardState.Selected("neowOffer");
        _neowDetails.Visible = !string.IsNullOrWhiteSpace(offer);
        if (string.IsNullOrWhiteSpace(offer))
        {
            return;
        }

        _neowDetails.AddChild(MakeLabel("extended Neow options", 13, Accent));
        _neowDetails.AddChild(MakeLabel(
            "click a slot to narrow what this relic grants; grouped slots share one reward roll",
            11,
            MutedText));

        foreach (var cluster in _boardState.VisibleChildren("neowOffer")
                     .GroupBy(slot => slot.Cluster ?? "details", StringComparer.Ordinal))
        {
            var heading = MakeLabel(cluster.Key, 12, Text);
            heading.AddThemeColorOverride("font_color", Accent);
            _neowDetails.AddChild(heading);

            foreach (var slot in cluster)
            {
                var view = _boardState.DescribeSlot(slot.Id);
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 8);
                var label = MakeLabel(slot.Label, 11, MutedText);
                label.CustomMinimumSize = new Vector2(150, 34);
                label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                row.AddChild(label);

                var selected = _boardState.Selected(slot.Id);
                var selectedTitle = selected == null
                    ? "any"
                    : view.Options.FirstOrDefault(option => option.Id == selected)?.Title ?? selected;
                var button = MakeButton(selectedTitle, $"Open {slot.Label} picker", 0);
                button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                if (view.RequiresCharacter)
                {
                    button.Text = "choose a character first";
                    button.Disabled = true;
                    button.TooltipText = "this child slot needs a character-specific card pool";
                }
                else
                {
                    button.Pressed += () => OpenSlotPicker(slot.Id);
                }

                row.AddChild(button);
                _neowDetails.AddChild(row);
            }
        }
    }

    private void RefreshAdvancedDetails()
    {
        if (_advancedDetails == null)
        {
            return;
        }

        ClearChildren(_advancedDetails);
        foreach (var cluster in SearchTheSpireCatalog.Slots
                     .Where(slot => slot.ParentId == null && slot.Cluster != null && slot.Cluster != "neow")
                     .GroupBy(slot => slot.Cluster!, StringComparer.Ordinal))
        {
            _advancedDetails.AddChild(MakeLabel(cluster.Key, 12, Accent));
            foreach (var slot in cluster)
            {
                AddAdvancedSlotRow(_advancedDetails, slot, 0);
            }
        }
    }

    private void AddAdvancedSlotRow(VBoxContainer container, SearchTheSpireSlot slot, int depth)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var label = MakeLabel(new string(' ', depth * 2) + slot.Label, 11, MutedText);
        label.CustomMinimumSize = new Vector2(170, 34);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(label);

        var enabled = SearchTheSpireCatalog.IsEnabled(slot, _boardState);
        var view = _boardState.DescribeSlot(slot.Id);
        var selected = _boardState.Selected(slot.Id);
        var selectedTitle = selected == null
            ? "any"
            : view.Options.FirstOrDefault(option => option.Id == selected)?.Title ?? selected;
        var button = MakeButton(selectedTitle, $"Open {slot.Label} picker", 0);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (!enabled)
        {
            button.Text = slot.RequiresAscension > _boardState.Ascension
                ? $"A{slot.RequiresAscension} only"
                : "select its parent first";
            button.Disabled = true;
        }
        else if (view.RequiresCharacter)
        {
            button.Text = "choose a character first";
            button.Disabled = true;
            button.TooltipText = "this slot needs a character-specific card pool";
        }
        else
        {
            button.Pressed += () => OpenSlotPicker(slot.Id);
        }

        row.AddChild(button);
        container.AddChild(row);
        foreach (var child in _boardState.VisibleChildren(slot.Id))
        {
            AddAdvancedSlotRow(container, child, depth + 1);
        }
    }

    private void ToggleSpoilers()
    {
        _showSpoilers = !_showSpoilers;
        _spoilerButton.Text = _showSpoilers ? "hide spoilers" : "show all spoilers";
        ApplyResults(_lastResults);
    }

    private void SaveSearch()
    {
        if (_lastQuery == null)
        {
            SetStatus("run a search before saving it", MutedText);
            return;
        }

        _savedSearches.Add(new SavedSearch(_lastQuery, _lastResults.ToList()));
        PersistSavedSearches();
        RebuildSavedList();
        SetStatus("saved this search", Accent);
    }

    private void ShareSearch()
    {
        var query = ReadQuery();
        var spec = $"sts2seed://search?version={query.GameApiVersion}&branch={query.Branch}&character={query.Character}&ascension={query.Ascension}&mode={query.RunMode}&neow={query.NeowFilter}&ancient={query.AncientFilter}&boss={query.BossFilter}&elites={query.MinimumElites}&shops={query.MinimumShops}&rests={query.MinimumRestSites}&offset={query.StartOffset}&extended={Uri.EscapeDataString(query.HiddenSpec)}";
        CopyToClipboard(spec);
        SetStatus("search spec copied", Accent);
    }

    private void CopyRandomSeed()
    {
        var seed = SeedSearchEngine.CreateSeed(ReadBranch(), DateTime.UtcNow.Ticks);
        CopyToClipboard(seed);
        SetStatus($"copied {seed}", Accent);
    }

    private void ClearBoard()
    {
        CancelSearch();
        _lastQuery = null;
        _lastResults = Array.Empty<SeedMatch>();
        _boardState = SearchTheSpireBoardState.Empty;
        _ascensionInput.Selected = 0;
        _neowInputButton.Text = "any Neow offers";
        RefreshNeowDetails();
        RefreshAdvancedDetails();
        _inspectInput.Text = string.Empty;
        _inspectStatusLabel.Text = string.Empty;
        _resultCountLabel.Text = "0 matches";
        _progressLabel.Text = "ready";
        ClearChildren(_resultsList);
        SetStatus("results show up here. select some filters, then hit Search.", MutedText);
    }

    private void RebuildSavedList()
    {
        ClearChildren(_savedList);
        if (_savedSearches.Count == 0)
        {
            _savedList.AddChild(MakeLabel("nothing saved yet. run a search, then hit save this search.", 13, MutedText));
            return;
        }

        foreach (var saved in _savedSearches)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            row.AddChild(MakeLabel($"{saved.Results.Count} matches · {saved.Query.NeowFilter} · offset {saved.Query.StartOffset}", 14, Text));
            var open = MakeButton("open", "Open saved results", 72);
            open.Pressed += () =>
            {
                _lastQuery = saved.Query;
                ApplyResults(saved.Results);
                ShowBoard();
            };
            row.AddChild(open);
            _savedList.AddChild(row);
        }
    }

    private void ShowBoard()
    {
        ShowPage(_boardPage);
    }

    private void ShowPopular()
    {
        ShowPage(_popularPage);
    }

    private void ShowSaved()
    {
        RebuildSavedList();
        ShowPage(_savedPage);
    }

    private void ShowPage(Control selectedPage)
    {
        _boardPage.Visible = selectedPage == _boardPage;
        _popularPage.Visible = selectedPage == _popularPage;
        _savedPage.Visible = selectedPage == _savedPage;
    }

    private SeedQuery ReadQuery()
    {
        var offset = long.TryParse(_advancedInput.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedOffset)
            ? Math.Max(0, parsedOffset)
            : 0;

        var stopAfter = new[] { 5, 10, 20, 50 }[_stopAfterInput.Selected];
        var maxCandidates = new[] { 10_000L, 50_000L, 250_000L, 1_000_000L }[_maxCandidatesInput.Selected];
        return new SeedQuery(
            Branch: ReadBranch(),
            GameApiVersion: "0.110.1",
            Character: (RunCharacter)_characterInput.Selected,
            Ascension: _ascensionInput.Selected * 5,
            RunMode: (RunMode)_runModeInput.Selected,
            StopAfter: stopAfter,
            StartOffset: offset,
            MaxCandidates: maxCandidates,
            MinimumElites: _eliteInput.Selected,
            MinimumShops: _shopInput.Selected,
            MinimumRestSites: _restInput.Selected,
            NeowFilter: _boardState.Selected("neowOffer") == null
                ? NeowFilter.Any
                : NeowFilter.Any,
            AncientFilter: _ancientInput.GetItemText(_ancientInput.Selected),
            BossFilter: _bossInput.GetItemText(_bossInput.Selected),
            HiddenSpec: _boardState.ToSpec());
    }

    private SeedBranch ReadBranch() => SeedBranch.PublicBeta;

    private static string BuildContext(SeedQuery query) =>
        $"{query.GameApiVersion}|{query.Character}|A{query.Ascension}|{query.RunMode}|{query.HiddenSpec}";

    private void RestoreSearchUi()
    {
        _searchButton.Disabled = false;
        _cancelButton.Disabled = true;
        _progressLabel.Text = "ready";
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.AddThemeColorOverride("font_color", color);
    }

    private static void AddField(GridContainer grid, string label, Control control)
    {
        var labelNode = MakeLabel(label, 13, MutedText);
        labelNode.VerticalAlignment = VerticalAlignment.Center;
        grid.AddChild(labelNode);
        grid.AddChild(control);
    }

    private static OptionButton MakeOption(IReadOnlyList<string> options, int selected, int width)
    {
        var option = new OptionButton { CustomMinimumSize = new Vector2(width, 36) };
        foreach (var item in options)
        {
            option.AddItem(item);
        }

        option.Selected = selected;
        return option;
    }

    private static Button MakeButton(string text, string tooltip, int width)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(width, 36),
            FocusMode = Control.FocusModeEnum.All
        };
        button.AddThemeStyleboxOverride("normal", MakeStyle(SurfaceRaised, Border, 6));
        button.AddThemeStyleboxOverride("hover", MakeStyle(AccentDark, Accent, 6));
        button.AddThemeStyleboxOverride("pressed", MakeStyle(AccentDark, Accent, 6));
        button.AddThemeColorOverride("font_color", Text);
        return button;
    }

    private static Label MakeLabel(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.VerticalAlignment = VerticalAlignment.Center;
        return label;
    }

    private static Label MakeCell(string text, int width, Color color, bool header = false)
    {
        var label = MakeLabel(text, header ? 11 : 12, color);
        label.CustomMinimumSize = new Vector2(width, header ? 28 : 36);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return label;
    }

    private static MarginContainer MakeMargin(int value)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", value);
        margin.AddThemeConstantOverride("margin_top", value);
        margin.AddThemeConstantOverride("margin_right", value);
        margin.AddThemeConstantOverride("margin_bottom", value);
        return margin;
    }

    private static HSeparator MakeSeparator()
    {
        var separator = new HSeparator();
        separator.AddThemeConstantOverride("separation", 10);
        return separator;
    }

    private static StyleBoxFlat MakeStyle(Color background, Color border, int radius = 6)
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

    private static void CopyToClipboard(string text)
    {
        DisplayServer.ClipboardSet(text);
    }

    private static string SavedSearchPath => ProjectSettings.GlobalizePath("user://seed_search_saved.json");

    private void LoadSavedSearches()
    {
        try
        {
            if (!File.Exists(SavedSearchPath))
            {
                return;
            }

            var json = File.ReadAllText(SavedSearchPath);
            var saved = JsonSerializer.Deserialize<List<SavedSearch>>(json);
            if (saved != null)
            {
                _savedSearches.AddRange(saved);
            }
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error($"Could not load saved seed searches: {exception.Message}");
        }
    }

    private void PersistSavedSearches()
    {
        try
        {
            var json = JsonSerializer.Serialize(_savedSearches, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavedSearchPath, json);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error($"Could not save seed searches: {exception.Message}");
        }
    }

    public sealed record SavedSearch(SeedQuery Query, List<SeedMatch> Results);
}
