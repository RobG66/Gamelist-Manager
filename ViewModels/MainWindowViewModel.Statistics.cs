using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Observable Properties
    [ObservableProperty] private int _totalGamesCount;
    [ObservableProperty] private int _filteredGamesCount;
    [ObservableProperty] private int _visibleGamesCount;
    [ObservableProperty] private int _hiddenGamesCount;
    [ObservableProperty] private int _favoriteGamesCount;
    [ObservableProperty] private int _uniqueGenresCount;
    [ObservableProperty] private int _totalPlayCount;
    [ObservableProperty] private string _totalPlayTime = "0h";
    [ObservableProperty] private string _mostPlayedGame = "-";
    [ObservableProperty] private string _mostPlayedGameTime = "";
    #endregion

    #region Public Properties
    public ObservableCollection<MediaAuditItem> MediaAuditItems { get; } = new();
    #endregion

    #region Private Fields
    private DispatcherTimer? _statsDebounceTimer;

    private static readonly SolidColorBrush[] s_mediaBrushes =
    [
        new(Color.FromRgb(0,   120, 212)),
        new(Color.FromRgb(136,  23, 152)),
        new(Color.FromRgb(16,  137,  62)),
        new(Color.FromRgb(255, 140,   0)),
        new(Color.FromRgb(0,   188, 242)),
        new(Color.FromRgb(227,   0, 140)),
        new(Color.FromRgb(0,   183, 195)),
        new(Color.FromRgb(16,  124,  16)),
        new(Color.FromRgb(92,   45, 145)),
        new(Color.FromRgb(255, 185,   0)),
        new(Color.FromRgb(255,  68,  68)),
        new(Color.FromRgb(0,   153, 188)),
        new(Color.FromRgb(180,   0, 158)),
        new(Color.FromRgb(73,  130,   5)),
        new(Color.FromRgb(135,  86,  11)),
        new(Color.FromRgb(104, 118, 138)),
    ];

    // Builds the media type + brush pairs fresh each call so mode switches (standard ↔ ES-DE)
    // are always reflected in the audit panel.
    private static IReadOnlyList<(MetaDataDecl MediaType, SolidColorBrush Brush)> GetMediaTypeInfo()
    {
        var mediaDecls = GamelistMetaData.GetColumnDeclarations()
            .Where(d => d.IsMedia)
            .ToList();

        var result = new List<(MetaDataDecl, SolidColorBrush)>(mediaDecls.Count);
        for (int i = 0; i < mediaDecls.Count; i++)
            result.Add((mediaDecls[i], s_mediaBrushes[i % s_mediaBrushes.Length]));

        return result;
    }
    #endregion

    #region Private Methods
    private void InitializeStatisticsPipeline()
    {
        _statsDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _statsDebounceTimer.Tick += (_, _) =>
        {
            _statsDebounceTimer.Stop();
            var filteredGames = Games.ToList();
            Task.Run(() =>
            {
                var stats = ComputeStatistics(filteredGames);
                Dispatcher.UIThread.Post(() => ApplyStatistics(stats));
            });
        };
    }

    private void CalculateStatistics()
    {
        _statsDebounceTimer?.Stop();
        _statsDebounceTimer?.Start();
    }

    private StatisticsSnapshot ComputeStatistics(IReadOnlyList<GameMetadataRow> filteredGames)
    {
        var total = filteredGames.Count;

        var gameStats = filteredGames.Select(g => new
        {
            IsHidden = g.GetValue(MetaDataKeys.hidden) is true,
            IsFavorite = g.GetValue(MetaDataKeys.favorite) is true,
            Genre = g.GetValue(MetaDataKeys.genre)?.ToString(),
            PlayCount = int.TryParse(g.GetValue(MetaDataKeys.playcount)?.ToString(), out var pc) ? pc : 0,
            GameTime = int.TryParse(g.GetValue(MetaDataKeys.gametime)?.ToString(), out var gt) ? gt : 0,
            Name = g.GetValue(MetaDataKeys.name)?.ToString() ?? string.Empty,
        }).ToList();

        var totalSeconds = gameStats.Sum(g => g.GameTime);

        var mostPlayed = gameStats
            .OrderByDescending(g => g.PlayCount)
            .FirstOrDefault(g => g.PlayCount > 0);

        var auditItems = GetMediaTypeInfo()
            .Where(kvp => _sharedData.AvailableMedia.Any(m => m.Type == kvp.MediaType.Type))
            .Select(kvp =>
            {
                var count = filteredGames.Count(g => !string.IsNullOrWhiteSpace(g.GetValue(kvp.MediaType.Key)?.ToString()));
                return new MediaAuditItem { Name = kvp.MediaType.Name, Count = count, Total = total, BarBrush = kvp.Brush, DataType = kvp.MediaType.DataType };
            })
            .ToList();

        return new StatisticsSnapshot
        {
            TotalGamesCount = _sourceCache.Count,
            FilteredGamesCount = total,
            VisibleGamesCount = gameStats.Count(g => !g.IsHidden),
            HiddenGamesCount = gameStats.Count(g => g.IsHidden),
            FavoriteGamesCount = gameStats.Count(g => g.IsFavorite),
            UniqueGenresCount = gameStats
                .Select(g => g.Genre)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            TotalPlayCount = gameStats.Sum(g => g.PlayCount),
            TotalPlayTime = FormatPlayTime(totalSeconds),
            MostPlayedGame = mostPlayed?.Name ?? "-",
            MostPlayedGameTime = mostPlayed?.PlayCount.ToString() ?? string.Empty,
            AuditItems = auditItems,
        };
    }

    private void ApplyStatistics(StatisticsSnapshot stats)
    {
        TotalGamesCount = stats.TotalGamesCount;
        FilteredGamesCount = stats.FilteredGamesCount;
        VisibleGamesCount = stats.VisibleGamesCount;
        HiddenGamesCount = stats.HiddenGamesCount;
        FavoriteGamesCount = stats.FavoriteGamesCount;
        UniqueGenresCount = stats.UniqueGenresCount;
        TotalPlayCount = stats.TotalPlayCount;
        TotalPlayTime = stats.TotalPlayTime;
        MostPlayedGame = stats.MostPlayedGame;
        MostPlayedGameTime = stats.MostPlayedGameTime;

        MediaAuditItems.Clear();
        foreach (var item in stats.AuditItems)
            MediaAuditItems.Add(item);
    }

    private static string FormatPlayTime(int totalSeconds) => totalSeconds switch
    {
        0 => "0h",
        < 3600 => $"{totalSeconds / 60}m",
        _ => (totalSeconds % 3600) / 60 > 0
            ? $"{totalSeconds / 3600}h {(totalSeconds % 3600) / 60}m"
            : $"{totalSeconds / 3600}h"
    };
    #endregion

    #region Nested Types
    private record StatisticsSnapshot
    {
        public int TotalGamesCount { get; init; }
        public int FilteredGamesCount { get; init; }
        public int VisibleGamesCount { get; init; }
        public int HiddenGamesCount { get; init; }
        public int FavoriteGamesCount { get; init; }
        public int UniqueGenresCount { get; init; }
        public int TotalPlayCount { get; init; }
        public string TotalPlayTime { get; init; } = "0h";
        public string MostPlayedGame { get; init; } = "-";
        public string MostPlayedGameTime { get; init; } = string.Empty;
        public List<MediaAuditItem> AuditItems { get; init; } = [];
    }
    #endregion
}