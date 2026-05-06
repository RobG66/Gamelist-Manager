using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Gamelist_Manager.ViewModels;

public partial class DatToolViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    private readonly List<GameReportItem> _gamelistSummary = [];
    private readonly List<GameReportItem> _datSummary = [];
    private bool _allowStreamingFromMame;
    private DatHeader? _datHeader;
    private System.Diagnostics.Process? _mameProcess;
    private bool _isDisposed;

    #endregion

    #region Observable Properties — DAT Summary

    [ObservableProperty] private string _datTotal = "—";
    [ObservableProperty] private string _datParents = "—";
    [ObservableProperty] private string _datClones = "—";
    [ObservableProperty] private string _datCHD = "—";
    [ObservableProperty] private string _datPlayable = "—";
    [ObservableProperty] private string _datNonPlayable = "—";

    #endregion

    #region Observable Properties — Gamelist Summary

    [ObservableProperty] private string _gamelistTotal = "—";
    [ObservableProperty] private string _gamelistParents = "—";
    [ObservableProperty] private string _gamelistClones = "—";
    [ObservableProperty] private string _gamelistCHD = "—";
    [ObservableProperty] private string _gamelistNonPlayable = "—";
    [ObservableProperty] private string _gamelistMissingParents = "—";
    [ObservableProperty] private string _gamelistMissingClones = "—";
    [ObservableProperty] private string _gamelistNotInDat = "—";

    #endregion

    #region Observable Properties — DAT Header

    [ObservableProperty] private string _datFileName = string.Empty;
    [ObservableProperty] private string _datInfoName = "—";
    [ObservableProperty] private string _datInfoVersion = "—";
    [ObservableProperty] private string _datInfoAuthor = "—";
    [ObservableProperty] private string _datInfoDate = "—";
    [ObservableProperty] private string _datInfoDescription = "—";

    #endregion

    #region Observable Properties — UI State

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isStreamFromMameEnabled;
    [ObservableProperty] private bool _isReportComboEnabled;
    [ObservableProperty] private bool _isFindMissingEnabled;
    [ObservableProperty] private bool _isIncludeHiddenEnabled;
    [ObservableProperty] private bool _includeHidden;
    [ObservableProperty] private bool _isCsvOutputEnabled;
    [ObservableProperty] private bool _csvOutput;
    [ObservableProperty] private int _reportViewIndex;
    [ObservableProperty] private bool _missingFilterParents = true;
    [ObservableProperty] private bool _missingFilterClones;
    [ObservableProperty] private bool _missingFilterAll;
    [ObservableProperty] private bool _isClearReportEnabled;

    #endregion

    #region Events

    public event EventHandler? CloseRequested;
    public event EventHandler<string>? ReportColumnAdded;
    public event EventHandler? PanelDisposing;

    #endregion

    #region Report Column Lookup

    public Dictionary<string, Dictionary<string, string>> ReportLookup { get; } =
        new(StringComparer.Ordinal);

    #endregion

    #region Constructor

    public DatToolViewModel()
    {
        _sharedData.PropertyChanged += OnSharedDataPropertyChanged;
        Reset();
    }

    #endregion

    #region Property Change Callbacks

    private void OnSharedDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SharedDataService.CurrentSystem) or nameof(SharedDataService.MamePath))
            UpdateMameStreamingEligibility();
    }

    partial void OnIncludeHiddenChanged(bool value) =>
        UpdateGamelistSummaryCounts(value);

    partial void OnMissingFilterParentsChanged(bool value)
    {
        if (!value) return;
        _missingFilterClones = false;
        _missingFilterAll = false;
        OnPropertyChanged(nameof(MissingFilterClones));
        OnPropertyChanged(nameof(MissingFilterAll));
    }

    partial void OnMissingFilterClonesChanged(bool value)
    {
        if (!value) return;
        _missingFilterParents = false;
        _missingFilterAll = false;
        OnPropertyChanged(nameof(MissingFilterParents));
        OnPropertyChanged(nameof(MissingFilterAll));
    }

    partial void OnMissingFilterAllChanged(bool value)
    {
        if (!value) return;
        _missingFilterParents = false;
        _missingFilterClones = false;
        OnPropertyChanged(nameof(MissingFilterParents));
        OnPropertyChanged(nameof(MissingFilterClones));
    }

    partial void OnReportViewIndexChanged(int value)
    {
        if (value <= 0) return;

        string columnName = value switch
        {
            1 => "Parents",
            2 => "Clones",
            3 => "Requires CHD",
            4 => "Non-Playable",
            5 => "Not In DAT",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(columnName)) return;

        var summaryLookup = new Dictionary<string, string>(FilePathHelper.PathComparer);

        foreach (var item in _gamelistSummary)
        {
            string display = value switch
            {
                1 => string.IsNullOrEmpty(item.CloneOf) && string.IsNullOrEmpty(item.NotInDat) ? "Parent" : string.Empty,
                2 => !string.IsNullOrEmpty(item.CloneOf) ? item.CloneOf : string.Empty,
                3 => item.CHDRequired,
                4 => item.NonPlayable,
                5 => item.NotInDat,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(display))
                summaryLookup[item.Name] = display;
        }

        var lookup = new Dictionary<string, string>(FilePathHelper.PathComparer);
        var gamelistData = _sharedData.GamelistData;

        if (gamelistData != null)
        {
            foreach (var row in gamelistData)
            {
                var normalized = FilePathHelper.NormalizeRomName(row.Path);
                if (summaryLookup.TryGetValue(normalized, out var display))
                    lookup[row.Path] = display;
            }
        }

        ReportLookup[columnName] = lookup;
        IsClearReportEnabled = true;
        ReportColumnAdded?.Invoke(this, columnName);
    }

    #endregion

    #region Reset

    public void Reset()
    {
        DatFileName = string.Empty;
        DatTotal = DatParents = DatClones = DatCHD = DatPlayable = DatNonPlayable = "—";
        GamelistTotal = GamelistParents = GamelistClones = GamelistCHD = "—";
        GamelistNonPlayable = GamelistMissingParents = GamelistMissingClones = GamelistNotInDat = "—";
        DatInfoName = DatInfoVersion = DatInfoAuthor = DatInfoDate = DatInfoDescription = "—";

        IsIncludeHiddenEnabled = false;
        IncludeHidden = false;
        IsCsvOutputEnabled = false;
        CsvOutput = false;
        IsReportComboEnabled = false;
        ReportViewIndex = 0;
        IsFindMissingEnabled = false;
        IsClearReportEnabled = false;

        _datSummary.Clear();
        _gamelistSummary.Clear();
        _datHeader = null;

        UpdateMameStreamingEligibility();
    }

    private void UpdateMameStreamingEligibility()
    {
        _allowStreamingFromMame =
            _sharedData?.CurrentSystem == "mame" &&
            !string.IsNullOrWhiteSpace(_sharedData.MamePath) &&
            File.Exists(_sharedData.MamePath);

        IsStreamFromMameEnabled = _allowStreamingFromMame;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_sharedData != null)
            _sharedData.PropertyChanged -= OnSharedDataPropertyChanged;

        PanelDisposing?.Invoke(this, EventArgs.Empty);
        ReportLookup.Clear();

        if (_mameProcess != null && !_mameProcess.HasExited)
        {
            try { _mameProcess.Kill(); } catch { }
        }

        _mameProcess?.Dispose();
        _mameProcess = null;
    }

    #endregion
}
