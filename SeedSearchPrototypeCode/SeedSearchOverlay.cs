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
    private VBoxContainer _pageContent = null!;
    private Control _boardPage = null!;
    private Control _popularPage = null!;
    private Control _savedPage = null!;
    private VBoxContainer _resultsList = null!;
    private VBoxContainer _popularList = null!;
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
    private OptionButton _ascensionInput = null!;
    private readonly List<Button> _characterButtons = new();
    private Button _searchButton = null!;
    private Button _cancelButton = null!;
    private Button _spoilerButton = null!;
    private SearchTheSpirePickerDialog _pickerDialog = null!;
    private Button _launcherButton = null!;
    private Label _titleLabel = null!;
    private Label _betaLabel = null!;
    private Button _boardTabButton = null!;
    private Button _popularTabButton = null!;
    private Button _savedTabButton = null!;
    private OptionButton _languageInput = null!;
    private ScrollContainer _filterScroll = null!;
    private SearchTheSpireBoardState _boardState = SearchTheSpireBoardState.Empty;

    private CancellationTokenSource? _searchCancellation;
    private Task<IReadOnlyList<SeedMatch>>? _searchTask;
    private SeedQuery? _lastQuery;
    private IReadOnlyList<SeedMatch> _lastResults = Array.Empty<SeedMatch>();
    private long _lastProgress;
    private bool _showSpoilers;
    private SeedSearchLanguage _language = SeedSearchLanguage.Chinese;
    private PageKind _pageKind = PageKind.Board;

    private enum PageKind
    {
        Board,
        Popular,
        Saved,
    }

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
                SetStatus(T("status.cancelled"), MutedText);
            }
            else if (completedTask.IsFaulted)
            {
                MainFile.Logger.Error($"Seed search failed: {completedTask.Exception}");
                RestoreSearchUi();
                SetStatus(T("status.failed"), Danger);
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
                    SetStatus(T("status.failed"), Danger);
                }
            }
        }

        if (_searchTask != null)
        {
            _progressLabel.Text = _language == SeedSearchLanguage.Chinese
                ? $"正在搜索 · 已检查 {_lastProgress.ToString("N0", CultureInfo.InvariantCulture)} 个候选"
                : $"searching · {_lastProgress.ToString("N0", CultureInfo.InvariantCulture)} candidates checked";
        }
    }

    private void BuildLauncher()
    {
        _launcherButton = new Button
        {
            Text = T("launcher.title"),
            TooltipText = T("launcher.tooltip"),
            FocusMode = Control.FocusModeEnum.All,
            Size = new Vector2(170, 48)
        };
        _launcherButton.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        _launcherButton.Position = new Vector2(-190, -70);
        _launcherButton.AddThemeStyleboxOverride("normal", MakeStyle(SurfaceRaised, Border));
        _launcherButton.AddThemeStyleboxOverride("hover", MakeStyle(AccentDark, Accent));
        _launcherButton.AddThemeColorOverride("font_color", Text);
        _launcherButton.Pressed += OpenBoardOverlay;
        _shell.AddChild(_launcherButton);
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

        _pageContent = new VBoxContainer();
        _pageContent.AddThemeConstantOverride("separation", 12);
        margin.AddChild(_pageContent);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        _pageContent.AddChild(header);
        _titleLabel = MakeLabel(T("app.title"), 26, Text);
        _titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(_titleLabel);
        _betaLabel = MakeLabel(T("beta"), 13, MutedText);
        _betaLabel.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_betaLabel);
        _languageInput = MakeOption(
            new[] { T("language.chinese"), T("language.english") },
            (int)_language,
            96);
        _languageInput.TooltipText = T("language");
        _languageInput.ItemSelected += index => SwitchLanguage((SeedSearchLanguage)index);
        header.AddChild(_languageInput);
        var closeButton = MakeButton("×", T("close"), 40);
        closeButton.Pressed += () => _backdrop.Visible = false;
        header.AddChild(closeButton);

        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 8);
        _pageContent.AddChild(tabs);
        _boardTabButton = MakeButton(T("board"), T("board.tooltip"), 100);
        _popularTabButton = MakeButton(T("popular"), T("popular.tooltip"), 100);
        _savedTabButton = MakeButton(T("saved"), T("saved.tooltip"), 100);
        _boardTabButton.Pressed += ShowBoard;
        _popularTabButton.Pressed += ShowPopular;
        _savedTabButton.Pressed += ShowSaved;
        tabs.AddChild(_boardTabButton);
        tabs.AddChild(_popularTabButton);
        tabs.AddChild(_savedTabButton);

        _boardPage = BuildBoardPage();
        _popularPage = BuildPopularPage();
        _savedPage = BuildSavedPage();
        _pageContent.AddChild(_boardPage);
        _pageContent.AddChild(_popularPage);
        _pageContent.AddChild(_savedPage);

        // The picker is a sibling of the page/backdrop, so it can temporarily
        // own input without changing the shell's pass-through policy.
        _pickerDialog = new SearchTheSpirePickerDialog { Name = "SearchTheSpirePicker" };
        _shell.AddChild(_pickerDialog);
        _pickerDialog.SetLanguage(_language);
    }

    private Control BuildBoardPage()
    {
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 12);
        page.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var searchBar = new HBoxContainer();
        searchBar.AddThemeConstantOverride("separation", 8);
        page.AddChild(searchBar);
        searchBar.AddChild(MakeLabel(T("searching"), 14, MutedText));
        _branchInput = MakeOption(new[] { T("branch.publicBeta") }, 0, 240);
        searchBar.AddChild(_branchInput);
        var shareButton = MakeButton(T("share"), T("share.tooltip"), 92);
        shareButton.Pressed += ShareSearch;
        searchBar.AddChild(shareButton);
        var clearButton = MakeButton(T("clear"), T("clear.tooltip"), 82);
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
        _filterScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        panel.AddChild(_filterScroll);
        var margin = MakeMargin(16);
        _filterScroll.AddChild(margin);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(content);

        content.AddChild(MakeLabel(T("compose"), 16, Text));
        content.AddChild(MakeLabel(T("emptySlots"), 12, MutedText));
        content.AddChild(MakeLabel(T("dataNote"), 11, MutedText));

        var setup = new VBoxContainer();
        setup.AddThemeConstantOverride("separation", 6);
        setup.AddChild(MakeLabel(T("setup"), 13, MutedText));
        var characterGrid = new GridContainer { Columns = 3 };
        characterGrid.AddThemeConstantOverride("h_separation", 6);
        characterGrid.AddThemeConstantOverride("v_separation", 6);
        _characterButtons.Clear();
        foreach (var character in SearchTheSpireUiLayout.CharacterOrder)
        {
            var characterButton = MakeButton(
                SeedSearchCopy.Character(character, _language),
                T("character.tooltip"),
                0);
            characterButton.CustomMinimumSize = new Vector2(124, 42);
            characterButton.ToggleMode = true;
            characterButton.ButtonPressed = _boardState.Character == character;
            characterButton.Pressed += () => SelectCharacter(character);
            _characterButtons.Add(characterButton);
            characterGrid.AddChild(characterButton);
        }

        setup.AddChild(characterGrid);
        var ascensionRow = new HBoxContainer();
        ascensionRow.AddThemeConstantOverride("separation", 8);
        ascensionRow.AddChild(MakeLabel(T("ascension"), 13, MutedText));
        _ascensionInput = MakeOption(
            new[] { T("ascension.none") }
                .Concat(Enumerable.Range(1, SearchTheSpireUiLayout.MaxAscension)
                    .Select(value => SeedSearchCopy.Format("ascension.level", _language, value)))
                .ToArray(),
            Math.Clamp(_boardState.Ascension, 0, SearchTheSpireUiLayout.MaxAscension),
            210);
        ascensionRow.AddChild(_ascensionInput);
        setup.AddChild(ascensionRow);
        content.AddChild(setup);

        _neowInputButton = MakeButton(T("neow.any"), T("neow.tooltip"), 210);
        _neowInputButton.Pressed += () => OpenSlotPicker("neowOffer");
        var neowSection = new VBoxContainer();
        neowSection.AddThemeConstantOverride("separation", 6);
        neowSection.AddChild(MakeLabel(T("neow"), 13, MutedText));
        neowSection.AddChild(_neowInputButton);
        _neowDetails = new VBoxContainer();
        _neowDetails.AddThemeConstantOverride("separation", 6);
        neowSection.AddChild(_neowDetails);
        content.AddChild(neowSection);

        _ascensionInput.ItemSelected += index =>
        {
            _boardState = _boardState.WithAscension(SearchTheSpireUiLayout.AscensionAt((int)index));
            RefreshAdvancedDetails();
        };

        RefreshNeowDetails();

        content.AddChild(MakeSeparator());
        _advancedDetails = new VBoxContainer();
        _advancedDetails.AddThemeConstantOverride("separation", 12);
        content.AddChild(_advancedDetails);
        RefreshAdvancedDetails();

        content.AddChild(MakeSeparator());
        content.AddChild(MakeLabel(T("searchControls"), 15, Text));
        var controls = new GridContainer { Columns = 2 };
        controls.AddThemeConstantOverride("h_separation", 10);
        controls.AddThemeConstantOverride("v_separation", 8);
        content.AddChild(controls);
        _stopAfterInput = MakeOption(new[] { T("matches5"), T("matches10"), T("matches20"), T("matches50") }, 2, 210);
        _maxCandidatesInput = MakeOption(new[] { T("candidates10k"), T("candidates50k"), T("candidates250k"), T("candidates1m") }, 2, 210);
        _advancedInput = new LineEdit
        {
            Text = "0",
            PlaceholderText = "0",
            CustomMinimumSize = new Vector2(210, 36)
        };
        AddField(controls, T("stopAfter"), _stopAfterInput);
        AddField(controls, T("runsToSearch"), _maxCandidatesInput);
        AddField(controls, T("advancedOffset"), _advancedInput);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        content.AddChild(actions);
        _searchButton = MakeButton(T("search"), T("search.tooltip"), 110);
        _searchButton.Pressed += StartSearch;
        actions.AddChild(_searchButton);
        _cancelButton = MakeButton(T("cancel"), T("cancel.tooltip"), 100);
        _cancelButton.Disabled = true;
        _cancelButton.Pressed += CancelSearch;
        actions.AddChild(_cancelButton);

        _progressLabel = MakeLabel(T("ready"), 12, MutedText);
        _progressLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_progressLabel);

        content.AddChild(MakeSeparator());
        content.AddChild(MakeLabel(T("inspect.title"), 15, Text));
        content.AddChild(MakeLabel(T("inspect.note"), 12, MutedText));
        var inspectRow = new HBoxContainer();
        inspectRow.AddThemeConstantOverride("separation", 8);
        content.AddChild(inspectRow);
        _inspectInput = new LineEdit
        {
            PlaceholderText = T("inspect.placeholder"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 36)
        };
        inspectRow.AddChild(_inspectInput);
        var inspectButton = MakeButton(T("inspect"), T("inspect.tooltip"), 86);
        inspectButton.Pressed += InspectSeed;
        inspectRow.AddChild(inspectButton);
        _inspectStatusLabel = MakeLabel("", 12, MutedText);
        _inspectStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_inspectStatusLabel);

        return panel;
    }

    private void OpenBoardOverlay()
    {
        _backdrop.Visible = true;
        _filterScroll.ScrollVertical = 0;
    }

    private void SelectCharacter(RunCharacter character)
    {
        var next = _boardState.Character == character ? RunCharacter.Any : character;
        _boardState = _boardState.WithCharacter(next);
        UpdateCharacterButtons(next);
        RefreshNeowDetails();
        RefreshAdvancedDetails();
    }

    private void UpdateCharacterButtons(RunCharacter character)
    {
        for (var index = 0; index < _characterButtons.Count; index++)
        {
            _characterButtons[index].ButtonPressed =
                index < SearchTheSpireUiLayout.CharacterOrder.Count &&
                SearchTheSpireUiLayout.CharacterOrder[index] == character;
        }
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
        var heading = MakeLabel(T("results"), 16, Text);
        heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(heading);
        _resultCountLabel = MakeLabel(T("matches.zero"), 12, MutedText);
        header.AddChild(_resultCountLabel);
        _spoilerButton = MakeButton(T("spoilers.show"), T("spoilers.tooltip"), 150);
        _spoilerButton.Pressed += ToggleSpoilers;
        header.AddChild(_spoilerButton);

        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        content.AddChild(headerRow);
        headerRow.AddChild(MakeCell(T("header.seed"), 132, MutedText, true));
        headerRow.AddChild(MakeCell(T("header.map"), 270, MutedText, true));
        headerRow.AddChild(MakeCell(T("header.neow"), 140, MutedText, true));
        headerRow.AddChild(MakeCell(T("header.ancients"), 120, MutedText, true));
        headerRow.AddChild(MakeCell(T("header.bosses"), 150, MutedText, true));

        var scroll = new ScrollContainer { Name = "ResultsScroll" };
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddChild(scroll);
        _resultsList = new VBoxContainer();
        _resultsList.AddThemeConstantOverride("separation", 6);
        _resultsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_resultsList);

        _statusLabel = MakeLabel(T("status.initial"), 13, MutedText);
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_statusLabel);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        content.AddChild(actions);
        var saveButton = MakeButton(T("save"), T("save.tooltip"), 138);
        saveButton.Pressed += SaveSearch;
        actions.AddChild(saveButton);
        var randomButton = MakeButton(T("random"), T("random.tooltip"), 144);
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

    private Control BuildPopularPage()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", MakeStyle(Surface, Border, 8));
        var margin = MakeMargin(24);
        panel.AddChild(margin);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 12);
        margin.AddChild(content);
        content.AddChild(MakeLabel(T("popular.title"), 20, Text));
        content.AddChild(MakeLabel(
            T("popular.note"),
            14,
            MutedText));
        _popularList = new VBoxContainer();
        _popularList.AddThemeConstantOverride("separation", 8);
        content.AddChild(_popularList);
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
        content.AddChild(MakeLabel(T("saved.title"), 20, Text));
        content.AddChild(MakeLabel(T("saved.note"), 14, MutedText));
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
        _progressLabel.Text = T("searching");
        SetStatus(T("status.searching"), MutedText);
    }

    private void CancelSearch()
    {
        if (_searchTask == null)
        {
            return;
        }

        _searchCancellation?.Cancel();
        _progressLabel.Text = T("search.cancelling");
    }

    private void InspectSeed()
    {
        var seed = _inspectInput.Text.Trim();
        if (seed.Length == 0)
        {
            _inspectStatusLabel.Text = T("status.pasteSeed");
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
        _inspectStatusLabel.Text = SeedSearchCopy.Format("status.seedInspected", _language, seed, SeedSearchCopy.Backend(snapshot.Backend, _language));
        SetStatus(SeedSearchCopy.Format("status.seedInspected", _language, seed, SeedSearchCopy.Backend(snapshot.Backend, _language)), Accent);
    }

    private void ApplyResults(IReadOnlyList<SeedMatch> results)
    {
        _lastResults = results;
        _searchButton.Disabled = false;
        _cancelButton.Disabled = true;
        _resultCountLabel.Text = _language == SeedSearchLanguage.Chinese
            ? $"{results.Count} 个结果"
            : $"{results.Count} matches";
        _progressLabel.Text = T("search.complete");
        ClearChildren(_resultsList);

        if (results.Count == 0)
        {
            SetStatus(T("status.noMatches"), MutedText);
            return;
        }

        foreach (var result in results)
        {
            _resultsList.AddChild(BuildResultRow(result));
        }

        var hitMatchCap = _lastQuery != null && results.Count >= _lastQuery.StopAfter;
        SetStatus(hitMatchCap
            ? T("status.matchCap")
            : _language == SeedSearchLanguage.Chinese
                ? $"已搜索 {_lastProgress:N0} 个候选"
                : $"searched {_lastProgress:N0} candidates", Accent);
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

        var seedButton = MakeButton(match.Seed, T("result.copy.tooltip"), 132);
        seedButton.Pressed += () => CopyToClipboard(match.Seed);
        var detail = BuildResultDetail(match);
        detail.Visible = false;
        var detailButton = MakeButton(T("result.details"), T("result.details.tooltip"), 132);
        detailButton.Pressed += () =>
        {
            detail.Visible = !detail.Visible;
            detailButton.Text = detail.Visible ? T("result.details.hide") : T("result.details");
        };
        var seedColumn = new VBoxContainer();
        seedColumn.AddThemeConstantOverride("separation", 4);
        seedColumn.CustomMinimumSize = new Vector2(132, 0);
        seedColumn.AddChild(seedButton);
        seedColumn.AddChild(detailButton);
        content.AddChild(seedColumn);
        content.AddChild(MakeCell(SeedSearchCopy.LocalizeText(match.Snapshot.Act1Map, _language), 270, Text));
        content.AddChild(MakeCell(SeedSearchCopy.LocalizeText(match.Snapshot.NeowOffers, _language), 140, Text));
        content.AddChild(MakeCell(SeedSearchCopy.LocalizeText(match.Snapshot.Ancients, _language), 120, Text));
        content.AddChild(MakeCell(SeedSearchCopy.LocalizeText(match.Snapshot.Bosses, _language), 150, Text));
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

        content.AddChild(MakeLabel(T("result.preview"), 13, Accent));
        content.AddChild(MakeLabel(SeedSearchCopy.Format("result.backend", _language, SeedSearchCopy.Backend(match.Snapshot.Backend, _language)), 12, MutedText));
        content.AddChild(MakeLabel(SeedSearchCopy.Format("result.map", _language, SeedSearchCopy.LocalizeText(match.Snapshot.Act1Map, _language)), 12, Text));
        content.AddChild(MakeLabel(SeedSearchCopy.Format("result.neow", _language, SeedSearchCopy.LocalizeText(match.Snapshot.NeowOffers, _language)), 12, Text));
        content.AddChild(MakeLabel(
            SeedSearchCopy.Format(
                "result.ancientsBosses",
                _language,
                SeedSearchCopy.LocalizeText(match.Snapshot.Ancients, _language),
                SeedSearchCopy.LocalizeText(match.Snapshot.Bosses, _language)),
            12,
            Text));
        content.AddChild(MakeLabel(
            SeedSearchCopy.Format("result.route", _language, match.Snapshot.EliteCount, match.Snapshot.ShopCount, match.Snapshot.RestSiteCount),
            12,
            MutedText));

        if (_lastQuery is { HiddenSpec.Length: > 0 } query)
        {
            content.AddChild(MakeLabel(SeedSearchCopy.Format("result.pins", _language, query.HiddenSpec), 12, MutedText));
        }

        var copy = MakeButton(T("result.copy"), T("result.copy.tooltip"), 94);
        copy.Pressed += () => CopyToClipboard(match.Seed);
        content.AddChild(copy);
        return panel;
    }

    private void OpenSlotPicker(string slotId)
    {
        var view = _boardState.DescribeSlot(slotId);
        var options = new List<SearchTheSpireOption>
        {
            new("", T("any.option"), T("picker.clear"), T("emptySlots")),
        };
        options.AddRange(view.Options);
        _pickerDialog.Open(
            SeedSearchCopy.SlotLabel(view.Slot, _language),
            SeedSearchCopy.LocalizeOptions(options, _language),
            value => SetBoardSlot(slotId, value));
    }

    private void SetBoardSlot(string slotId, string? value)
    {
        var selectedOption = !string.IsNullOrEmpty(value)
            ? _boardState.OptionsFor(slotId).FirstOrDefault(option => option.Id == value)
            : null;
        if (selectedOption?.OwnerCharacter is { } owner &&
            _boardState.Character == RunCharacter.Any &&
            Enum.TryParse<RunCharacter>(owner, true, out var ownerCharacter))
        {
            _boardState = _boardState.WithCharacter(ownerCharacter);
            UpdateCharacterButtons(ownerCharacter);
        }

        _boardState = _boardState.Select(slotId, string.IsNullOrEmpty(value) ? null : value);
        if (slotId == "neowOffer")
        {
            RefreshNeowInputText();
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

        _neowDetails.AddChild(MakeLabel(T("neow.details"), 13, Accent));
        _neowDetails.AddChild(MakeLabel(
            T("neow.details.note"),
            11,
            MutedText));

        foreach (var cluster in SearchTheSpireUiLayout.VisibleNeowChildren(_boardState)
                     .GroupBy(slot => slot.Cluster ?? "details", StringComparer.Ordinal))
        {
            var heading = MakeLabel(SeedSearchCopy.ClusterLabel(cluster.Key, _language), 12, Text);
            heading.AddThemeColorOverride("font_color", Accent);
            _neowDetails.AddChild(heading);

            foreach (var slot in cluster)
            {
                var view = _boardState.DescribeSlot(slot.Id);
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 8);
                var label = MakeLabel(SeedSearchCopy.SlotLabel(slot, _language), 11, MutedText);
                label.CustomMinimumSize = new Vector2(150, 34);
                label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                row.AddChild(label);

                var selected = _boardState.Selected(slot.Id);
                var selectedTitle = selected == null
                    ? T("any.option")
                    : view.Options.FirstOrDefault(option => option.Id == selected) is { } option
                        ? SeedSearchCopy.OptionTitle(option, _language)
                        : SeedSearchCopy.DisplayName(selected, _language);
                var button = MakeButton(selectedTitle, $"{SeedSearchCopy.SlotLabel(slot, _language)} · {T("picker.pick")}", 0);
                button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                if (view.RequiresCharacter)
                {
                    button.Text = T("blocked.character");
                    button.Disabled = true;
                    button.TooltipText = T("blocked.character.tooltip");
                }
                else
                {
                    button.Pressed += () => OpenSlotPicker(slot.Id);
                }

                row.AddChild(button);
                _neowDetails.AddChild(row);
            }
        }

        if (SearchTheSpireCatalog.RaresEnabled(_boardState))
        {
            _neowDetails.AddChild(MakeLabel(T("neow.freshRares"), 12, Accent));
            AddAdvancedSlotRow(_neowDetails, SearchTheSpireCatalog.GetSlot("rares"), 0);
        }
    }

    private void RefreshAdvancedDetails()
    {
        if (_advancedDetails == null)
        {
            return;
        }

        ClearChildren(_advancedDetails);
        foreach (var group in SearchTheSpireUiLayout.AdvancedGroups(_boardState))
        {
            var section = new VBoxContainer();
            section.AddThemeConstantOverride("separation", 6);
            var heading = MakeLabel(SeedSearchCopy.ClusterLabel(group.Id, _language), 12, Accent);
            section.AddChild(heading);
            var body = new VBoxContainer();
            body.AddThemeConstantOverride("separation", 6);
            switch (group.Id)
            {
                case "rewards":
                    AddRewardsSection(body, group.Slots);
                    break;
                case "relics":
                    AddRelicsSection(body);
                    break;
                case "events":
                    AddEventsSection(body);
                    break;
                case "ancients":
                    AddAncientsSection(body);
                    break;
                case "route":
                    AddRouteSection(body);
                    break;
                default:
                    foreach (var slot in group.Slots)
                    {
                        AddAdvancedSlotRow(body, slot, 0);
                    }

                    break;
            }

            section.AddChild(body);
            _advancedDetails.AddChild(section);
        }
    }

    private void AddRewardsSection(VBoxContainer body, IReadOnlyList<SearchTheSpireSlot> slots)
    {
        body.AddChild(MakeLabel(T("rewards.caption"), 11, MutedText));
        AddProgressivePickerRow(body, SearchTheSpireCatalog.RewardPickIds);
        if (SearchTheSpireCatalog.RewardPickIds.Any(id => _boardState.Selected(id) != null))
        {
            AddSelectSlotRow(body, "rewardWithin");
            AddOrderSlotRow(body, "rewardOrdered");
        }

        body.AddChild(BuildExactRewardDisclosure(slots));
    }

    private void AddRelicsSection(VBoxContainer body)
    {
        body.AddChild(MakeLabel(T("relics.shop"), 11, Text));
        body.AddChild(MakeLabel(T("relics.shop.caption"), 11, MutedText));
        AddProgressivePickerRow(body, SearchTheSpireCatalog.ShopPickIds);
        if (SearchTheSpireCatalog.ShopPickIds.Any(id => _boardState.Selected(id) != null))
        {
            AddSelectSlotRow(body, "shopWithin");
        }

        body.AddChild(MakeLabel(T("relics.reward"), 11, Text));
        body.AddChild(MakeLabel(T("relics.reward.caption"), 11, MutedText));
        AddProgressivePickerRow(body, SearchTheSpireCatalog.BagPickIds);
        if (SearchTheSpireCatalog.BagPickIds.Any(id => _boardState.Selected(id) != null))
        {
            AddSelectSlotRow(body, "bagWithin");
        }
    }

    private void AddEventsSection(VBoxContainer body)
    {
        body.AddChild(MakeLabel(T("events.caption"), 11, MutedText));
        AddProgressivePickerRow(body, SearchTheSpireCatalog.EventPickIds);
        if (SearchTheSpireCatalog.EventPickIds.Any(id => _boardState.Selected(id) != null))
        {
            AddSelectSlotRow(body, "eventWithin");
        }
    }

    private void AddAncientsSection(VBoxContainer body)
    {
        AddAdvancedSlotRow(body, SearchTheSpireCatalog.GetSlot("ancient2"), 0);
        AddAdvancedSlotRow(body, SearchTheSpireCatalog.GetSlot("ancient3"), 0);
    }

    private void AddRouteSection(VBoxContainer body)
    {
        AddActToggleRow(body);
        AddCompactArtSlotRow(body, "boss1");
        AddCompactArtSlotRow(body, "boss2");
        AddCompactArtSlotRow(body, "boss3");
        if (_boardState.Ascension >= SearchTheSpireCatalog.GetSlot("boss3b").RequiresAscension)
        {
            AddCompactArtSlotRow(body, "boss3b");
        }
    }

    private void AddProgressivePickerRow(
        VBoxContainer body,
        IReadOnlyList<string> slotIds)
    {
        var first = SearchTheSpireCatalog.GetSlot(slotIds[0]);
        body.AddChild(MakeLabel(SeedSearchCopy.SlotLabel(first, _language), 11, MutedText));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var highest = slotIds
            .Select((id, index) => _boardState.Selected(id) != null ? index + 1 : 0)
            .Max();
        var count = Math.Min(highest + 1, slotIds.Count);
        for (var index = 0; index < count; index++)
        {
            var control = BuildSlotControl(SearchTheSpireCatalog.GetSlot(slotIds[index]));
            control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(control);
        }

        body.AddChild(row);
    }

    private void AddSelectSlotRow(VBoxContainer body, string slotId)
    {
        var slot = SearchTheSpireCatalog.GetSlot(slotId);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var label = MakeLabel(SeedSearchCopy.SlotLabel(slot, _language), 11, MutedText);
        label.CustomMinimumSize = new Vector2(170, 34);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(label);

        var options = SeedSearchCopy.LocalizeOptions(_boardState.OptionsFor(slotId), _language);
        var select = MakeOption(options.Select(option => option.Title).ToArray(), 0, 0);
        select.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var current = _boardState.Selected(slotId);
        var selectedIndex = options
            .Select((option, index) => (option, index))
            .FirstOrDefault(pair => pair.option.Id == current)
            .index;
        if (current != null)
        {
            select.Selected = selectedIndex;
        }

        var floor = slotId switch
        {
            "rewardWithin" => SearchTheSpireCatalog.RewardPackageFloor(_boardState),
            "shopWithin" => SearchTheSpireCatalog.ShopFloor(_boardState),
            "bagWithin" => SearchTheSpireCatalog.BagFloor(_boardState),
            "eventWithin" => SearchTheSpireCatalog.EventFloor(_boardState),
            _ => 0,
        };
        for (var index = 0; index < options.Count; index++)
        {
            if (int.TryParse(options[index].Id, out var value) && value < floor)
            {
                select.SetItemDisabled(index, true);
            }
        }

        select.ItemSelected += index => SetBoardSlot(slotId, options[(int)index].Id);
        row.AddChild(select);
        body.AddChild(row);
    }

    private void AddOrderSlotRow(VBoxContainer body, string slotId)
    {
        var check = new CheckButton
        {
            Text = SeedSearchCopy.OptionTitle(
                new SearchTheSpireOption("true", "in this order", "reward order"),
                _language),
            ButtonPressed = string.Equals(_boardState.Selected(slotId), "true", StringComparison.OrdinalIgnoreCase),
            TooltipText = T("common.inOrder"),
        };
        check.Toggled += pressed => SetBoardSlot(slotId, pressed ? "true" : null);
        body.AddChild(check);
    }

    private void AddActToggleRow(VBoxContainer body)
    {
        var slot = SearchTheSpireCatalog.GetSlot("act");
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var label = MakeLabel(SeedSearchCopy.SlotLabel(slot, _language), 11, MutedText);
        label.CustomMinimumSize = new Vector2(110, 34);
        row.AddChild(label);
        var toggles = new HBoxContainer();
        toggles.AddThemeConstantOverride("separation", 5);
        var options = _boardState.OptionsFor("act");
        AddToggle("", T("any.option"), null);
        foreach (var option in options)
        {
            AddToggle(option.Id, SeedSearchCopy.OptionTitle(option, _language), option);
        }

        row.AddChild(toggles);
        body.AddChild(row);

        void AddToggle(string value, string title, SearchTheSpireOption? option)
        {
            var button = MakeButton(title, title, 0);
            button.ToggleMode = true;
            button.ButtonPressed = (value.Length == 0 && _boardState.Selected("act") == null) ||
                                   _boardState.Selected("act") == value;
            if (option?.Blocked == true && _boardState.Selected("act") != option.Id)
            {
                button.Disabled = true;
                button.TooltipText = SeedSearchCopy.BlockReason(option.BlockReason, _language);
            }

            button.Pressed += () => SetBoardSlot("act", value.Length == 0 ? null : value);
            toggles.AddChild(button);
        }
    }

    private void AddCompactArtSlotRow(VBoxContainer body, string slotId)
    {
        var slot = SearchTheSpireCatalog.GetSlot(slotId);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var label = MakeLabel(SeedSearchCopy.SlotLabel(slot, _language), 11, MutedText);
        label.CustomMinimumSize = new Vector2(110, 34);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(label);
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        var current = _boardState.Selected(slotId);
        AddCell(null, T("any.option"), null);
        foreach (var option in _boardState.OptionsFor(slotId))
        {
            AddCell(option.Id, SeedSearchCopy.OptionTitle(option, _language), option);
        }

        row.AddChild(grid);
        body.AddChild(row);

        void AddCell(string? value, string title, SearchTheSpireOption? option)
        {
            var button = MakeButton(title, title, 0);
            button.CustomMinimumSize = new Vector2(78, 32);
            button.ToggleMode = true;
            button.ButtonPressed = current == value;
            if (option?.Blocked == true && current != option.Id)
            {
                button.Disabled = true;
                button.TooltipText = SeedSearchCopy.BlockReason(option.BlockReason, _language);
            }

            button.Pressed += () => SetBoardSlot(slotId, current == value ? null : value);
            grid.AddChild(button);
        }
    }

    private Control BuildSlotControl(SearchTheSpireSlot slot)
    {
        var wrapper = new HBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 4);
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var button = BuildSlotButton(slot);
        wrapper.AddChild(button);
        if (_boardState.Selected(slot.Id) != null)
        {
            var clear = MakeButton("×", T("picker.clear"), 30);
            clear.Pressed += () => SetBoardSlot(slot.Id, null);
            wrapper.AddChild(clear);
        }

        return wrapper;
    }

    private Button BuildSlotButton(SearchTheSpireSlot slot)
    {
        var enabled = SearchTheSpireCatalog.IsEnabled(slot, _boardState);
        var view = _boardState.DescribeSlot(slot.Id);
        var selected = _boardState.Selected(slot.Id);
        var selectedTitle = selected == null
            ? T("any.option")
            : view.Options.FirstOrDefault(option => option.Id == selected) is { } option
                ? SeedSearchCopy.OptionTitle(option, _language)
                : SeedSearchCopy.DisplayName(selected, _language);
        var button = MakeButton(
            selectedTitle,
            $"{SeedSearchCopy.SlotLabel(slot, _language)} · {T("picker.pick")}",
            0);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (!enabled)
        {
            button.Text = slot.RequiresAscension > _boardState.Ascension
                ? SeedSearchCopy.Format("blocked.ascension", _language, slot.RequiresAscension)
                : slot.Id == "rares"
                    ? T("blocked.neow")
                    : slot.Id == "rewardOrdered"
                        ? T("blocked.reward")
                        : T("blocked.parent");
            button.Disabled = true;
        }
        else if (view.RequiresCharacter)
        {
            button.Text = T("blocked.character");
            button.Disabled = true;
            button.TooltipText = T("blocked.character.tooltip");
        }
        else
        {
            button.Pressed += () => OpenSlotPicker(slot.Id);
        }

        return button;
    }

    private bool IsAdvancedSlotVisible(SearchTheSpireSlot slot)
    {
        if (slot.Id is "rewardWithin" or "rewardOrdered")
        {
            return SearchTheSpireCatalog.RewardPickIds.Any(id => _boardState.Selected(id) != null);
        }

        if (slot.Id == "shopWithin")
        {
            return SearchTheSpireCatalog.ShopPickIds.Any(id => _boardState.Selected(id) != null);
        }

        if (slot.Id == "bagWithin")
        {
            return SearchTheSpireCatalog.BagPickIds.Any(id => _boardState.Selected(id) != null);
        }

        if (slot.Id == "eventWithin")
        {
            return SearchTheSpireCatalog.EventPickIds.Any(id => _boardState.Selected(id) != null);
        }

        if (slot.Id.StartsWith("rewardPick", StringComparison.Ordinal) ||
            slot.Id.StartsWith("shopPick", StringComparison.Ordinal) ||
            slot.Id.StartsWith("bagPick", StringComparison.Ordinal) ||
            slot.Id.StartsWith("eventPick", StringComparison.Ordinal))
        {
            var prefix = slot.Id.StartsWith("rewardPick", StringComparison.Ordinal)
                ? "rewardPick"
                : slot.Id.StartsWith("shopPick", StringComparison.Ordinal)
                    ? "shopPick"
                    : slot.Id.StartsWith("bagPick", StringComparison.Ordinal)
                        ? "bagPick"
                        : "eventPick";
            var index = int.Parse(slot.Id[prefix.Length..], CultureInfo.InvariantCulture);
            return index == 1 || _boardState.Selected($"{prefix}{index - 1}") != null;
        }

        return !slot.Id.StartsWith("reward", StringComparison.Ordinal) ||
               !int.TryParse(slot.Id["reward".Length..], out _);
    }

    private Control BuildExactRewardDisclosure(IReadOnlyList<SearchTheSpireSlot> slots)
    {
        var wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 6);
        var selected = slots
            .Where(slot => slot.Id is "reward1" or "reward2" or "reward3")
            .Any(slot => _boardState.Selected(slot.Id) != null);
        var heading = MakeButton(
            DisclosureText(T("rewards.exact"), selected),
            selected ? T("advanced.hide") : T("advanced.show"),
            0);
        heading.Alignment = HorizontalAlignment.Left;
        heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 6);
        body.Visible = selected;
        foreach (var slot in slots.Where(slot => slot.Id is "reward1" or "reward2" or "reward3"))
        {
            AddAdvancedSlotRow(body, slot, 0);
        }

        heading.Pressed += () =>
        {
            body.Visible = !body.Visible;
            heading.Text = DisclosureText(T("rewards.exact"), body.Visible);
            heading.TooltipText = body.Visible ? T("advanced.hide") : T("advanced.show");
        };
        wrapper.AddChild(heading);
        wrapper.AddChild(body);
        return wrapper;
    }

    private void AddAdvancedSlotRow(VBoxContainer container, SearchTheSpireSlot slot, int depth)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var label = MakeLabel(new string(' ', depth * 2) + SeedSearchCopy.SlotLabel(slot, _language), 11, MutedText);
        label.CustomMinimumSize = new Vector2(170, 34);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(label);

        row.AddChild(BuildSlotControl(slot));
        container.AddChild(row);
        if (!SearchTheSpireUiLayout.ShouldRenderChildren(slot, _boardState))
        {
            return;
        }

        foreach (var child in _boardState.VisibleChildren(slot.Id))
        {
            AddAdvancedSlotRow(container, child, depth + 1);
        }
    }

    private static string DisclosureText(string label, bool expanded) =>
        $"{(expanded ? "▾" : "▸")} {label}";

    private void ToggleSpoilers()
    {
        _showSpoilers = !_showSpoilers;
        _spoilerButton.Text = _showSpoilers ? T("spoilers.hide") : T("spoilers.show");
        ApplyResults(_lastResults);
    }

    private void SaveSearch()
    {
        if (_lastQuery == null)
        {
            SetStatus(T("status.saveFirst"), MutedText);
            return;
        }

        _savedSearches.Add(new SavedSearch(_lastQuery, _lastResults.ToList()));
        PersistSavedSearches();
        RebuildSavedList();
        RebuildPopularList();
        SetStatus(T("status.saved"), Accent);
    }

    private void ShareSearch()
    {
        var query = ReadQuery();
        var spec = $"sts2seed://search?version={query.GameApiVersion}&branch={query.Branch}&character={query.Character}&ascension={query.Ascension}&mode={query.RunMode}&neow={query.NeowFilter}&ancient={query.AncientFilter}&boss={query.BossFilter}&elites={query.MinimumElites}&shops={query.MinimumShops}&rests={query.MinimumRestSites}&offset={query.StartOffset}&extended={Uri.EscapeDataString(query.HiddenSpec)}";
        CopyToClipboard(spec);
        SetStatus(T("status.specCopied"), Accent);
    }

    private void CopyRandomSeed()
    {
        var seed = SeedSearchEngine.CreateSeed(ReadBranch(), DateTime.UtcNow.Ticks);
        CopyToClipboard(seed);
        SetStatus(SeedSearchCopy.Format("status.seedCopied", _language, seed), Accent);
    }

    private void ClearBoard()
    {
        CancelSearch();
        _lastQuery = null;
        _lastResults = Array.Empty<SeedMatch>();
        _boardState = SearchTheSpireBoardState.Empty;
        UpdateCharacterButtons(RunCharacter.Any);
        _ascensionInput.Selected = 0;
        _neowInputButton.Text = T("neow.any");
        RefreshNeowDetails();
        RefreshAdvancedDetails();
        _inspectInput.Text = string.Empty;
        _inspectStatusLabel.Text = string.Empty;
        _resultCountLabel.Text = T("matches.zero");
        _progressLabel.Text = T("ready");
        ClearChildren(_resultsList);
        _stopAfterInput.Selected = 2;
        _maxCandidatesInput.Selected = 2;
        _advancedInput.Text = "0";
        SetStatus(T("status.initial"), MutedText);
    }

    private void RefreshNeowInputText()
    {
        var selected = _boardState.Selected("neowOffer");
        _neowInputButton.Text = selected == null
            ? T("neow.any")
            : SearchTheSpireCatalog.OptionsFor(SearchTheSpireCatalog.GetSlot("neowOffer"), _boardState)
                .FirstOrDefault(option => option.Id == selected) is { } option
                    ? SeedSearchCopy.OptionTitle(option, _language)
                    : SeedSearchCopy.DisplayName(selected, _language);
    }

    private void RebuildSavedList()
    {
        ClearChildren(_savedList);
        if (_savedSearches.Count == 0)
        {
            _savedList.AddChild(MakeLabel(T("saved.empty"), 13, MutedText));
            return;
        }

        foreach (var saved in _savedSearches)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            row.AddChild(MakeLabel(SeedSearchCopy.Format("saved.row", _language, saved.Results.Count, saved.Query.NeowFilter, saved.Query.StartOffset), 14, Text));
            var open = MakeButton(T("open"), T("open.saved.tooltip"), 72);
            open.Pressed += () => OpenSavedSearch(saved);
            row.AddChild(open);
            _savedList.AddChild(row);
        }
    }

    private void ShowBoard()
    {
        _pageKind = PageKind.Board;
        ShowPage(_boardPage);
    }

    private void ShowPopular()
    {
        RebuildPopularList();
        _pageKind = PageKind.Popular;
        ShowPage(_popularPage);
    }

    private void RebuildPopularList()
    {
        if (_popularList == null)
        {
            return;
        }

        ClearChildren(_popularList);
        var groups = _savedSearches
            .GroupBy(saved => SearchKey(saved.Query), StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(10)
            .ToArray();
        if (groups.Length == 0)
        {
            _popularList.AddChild(MakeLabel(T("popular.empty"), 13, MutedText));
            return;
        }

        foreach (var group in groups)
        {
            var first = group.First();
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            var label = MakeLabel(
                $"{group.Count()} {(group.Count() == 1 ? T("popular.row.single") : T("popular.row.plural"))} · {DescribeQuery(first.Query)}",
                14,
                Text);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            row.AddChild(label);
            var open = MakeButton(T("open"), T("open.popular.tooltip"), 72);
            open.Pressed += () => OpenSavedSearch(first);
            row.AddChild(open);
            _popularList.AddChild(row);
        }
    }

    private static string SearchKey(SeedQuery query) =>
        JsonSerializer.Serialize(query);

    private string DescribeQuery(SeedQuery query)
    {
        var parts = new List<string>();
        if (query.Character != RunCharacter.Any)
        {
            parts.Add(SeedSearchCopy.Character(query.Character, _language));
        }

        if (!string.IsNullOrWhiteSpace(query.HiddenSpec))
        {
            parts.Add(query.HiddenSpec);
        }
        else
        {
            parts.Add(T("common.anyRunStart"));
        }

        return string.Join(" · ", parts);
    }

    private void OpenSavedSearch(SavedSearch saved)
    {
        _lastQuery = saved.Query;
        RestoreQueryControls(saved.Query);
        ApplyResults(saved.Results);
        ShowBoard();
    }

    private void RestoreQueryControls(SeedQuery query)
    {
        _boardState = SearchTheSpireBoardState.FromSpec(query.Character, query.Ascension, query.HiddenSpec);
        _branchInput.Selected = 0;
        UpdateCharacterButtons(_boardState.Character);
        _ascensionInput.Selected = Math.Clamp(query.Ascension, 0, _ascensionInput.ItemCount - 1);
        _stopAfterInput.Selected = query.StopAfter switch
        {
            <= 5 => 0,
            <= 10 => 1,
            <= 20 => 2,
            _ => 3,
        };
        _maxCandidatesInput.Selected = query.MaxCandidates switch
        {
            <= 10_000 => 0,
            <= 50_000 => 1,
            <= 250_000 => 2,
            _ => 3,
        };
        _advancedInput.Text = query.StartOffset.ToString(CultureInfo.InvariantCulture);
        RefreshNeowInputText();
        RefreshNeowDetails();
        RefreshAdvancedDetails();
    }

    private void ShowSaved()
    {
        RebuildSavedList();
        _pageKind = PageKind.Saved;
        ShowPage(_savedPage);
    }

    private void ShowPage(Control selectedPage)
    {
        _boardPage.Visible = selectedPage == _boardPage;
        _popularPage.Visible = selectedPage == _popularPage;
        _savedPage.Visible = selectedPage == _savedPage;
    }

    private void SwitchLanguage(SeedSearchLanguage language)
    {
        if (_language == language)
        {
            return;
        }

        _language = language;
        UpdateHeaderCopy();
        _pickerDialog.Close();

        var selectedPage = _pageKind;
        foreach (var oldPage in new[] { _boardPage, _popularPage, _savedPage })
        {
            _pageContent.RemoveChild(oldPage);
            oldPage.QueueFree();
        }

        _boardPage = BuildBoardPage();
        _popularPage = BuildPopularPage();
        _savedPage = BuildSavedPage();
        _pageContent.AddChild(_boardPage);
        _pageContent.AddChild(_popularPage);
        _pageContent.AddChild(_savedPage);
        _pickerDialog.SetLanguage(_language);

        RefreshNeowInputText();
        RebuildSavedList();
        RebuildPopularList();
        if (_lastQuery != null)
        {
            ApplyResults(_lastResults);
        }

        switch (selectedPage)
        {
            case PageKind.Popular:
                ShowPopular();
                break;
            case PageKind.Saved:
                ShowSaved();
                break;
            default:
                ShowBoard();
                break;
        }
    }

    private void UpdateHeaderCopy()
    {
        if (_launcherButton == null)
        {
            return;
        }

        _launcherButton.Text = T("launcher.title");
        _launcherButton.TooltipText = T("launcher.tooltip");
        _titleLabel.Text = T("app.title");
        _betaLabel.Text = T("beta");
        _boardTabButton.Text = T("board");
        _boardTabButton.TooltipText = T("board.tooltip");
        _popularTabButton.Text = T("popular");
        _popularTabButton.TooltipText = T("popular.tooltip");
        _savedTabButton.Text = T("saved");
        _savedTabButton.TooltipText = T("saved.tooltip");
        _languageInput.SetItemText(0, T("language.chinese"));
        _languageInput.SetItemText(1, T("language.english"));
        _languageInput.TooltipText = T("language");
    }

    private string T(string key) => SeedSearchCopy.Get(key, _language);

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
            Character: _boardState.Character,
            Ascension: SearchTheSpireUiLayout.AscensionAt(_ascensionInput.Selected),
            RunMode: RunMode.Plain,
            StopAfter: stopAfter,
            StartOffset: offset,
            MaxCandidates: maxCandidates,
            MinimumElites: 0,
            MinimumShops: 0,
            MinimumRestSites: 0,
            NeowFilter: _boardState.Selected("neowOffer") is { } neowOffer
                ? SearchTheSpireCatalog.CursedOffers.Contains(neowOffer, StringComparer.Ordinal)
                    ? NeowFilter.HasCurse
                    : NeowFilter.HasBlessing
                : NeowFilter.Any,
            AncientFilter: SearchTheSpireUiLayout.AncientFilterIds[0],
            BossFilter: SearchTheSpireUiLayout.BossFilterIds[0],
            HiddenSpec: _boardState.ToSpec());
    }

    private SeedBranch ReadBranch() => SeedBranch.PublicBeta;

    private static string BuildContext(SeedQuery query) =>
        $"{query.GameApiVersion}|{query.Character}|A{query.Ascension}|{query.RunMode}|{query.HiddenSpec}";

    private void RestoreSearchUi()
    {
        _searchButton.Disabled = false;
        _cancelButton.Disabled = true;
        _progressLabel.Text = T("ready");
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
