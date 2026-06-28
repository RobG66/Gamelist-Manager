using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views;

public partial class MediaItemView : UserControl
{
    public static readonly StyledProperty<bool> IsScaledProperty =
        AvaloniaProperty.Register<MediaItemView, bool>(nameof(IsScaled));

    public bool IsScaled
    {
        get => GetValue(IsScaledProperty);
        set => SetValue(IsScaledProperty, value);
    }

    public MediaItemView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    #region Drag and Drop

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (IsValidDrop(e, mediaItem))
        {
            e.DragEffects = DragDropEffects.Copy;
            mediaItem.IsDragOver = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = IsValidDrop(e, mediaItem)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (DataContext is MediaItemViewModel mediaItem)
            mediaItem.IsDragOver = false;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        try
        {
            if (DataContext is not MediaItemViewModel mediaItem)
                return;

            mediaItem.IsDragOver = false;

            if (!IsValidDrop(e, mediaItem))
                return;

            var parentViewModel = VisualTreeHelper.FindAncestorViewModel<MediaPreviewViewModel>(this);
            if (parentViewModel?.SelectedGame == null)
                return;

            var (filePath, isTemp) = await GetDroppedFile(e, mediaItem);
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                await parentViewModel.UpdateGameMedia(mediaItem.MediaType, filePath);
            }
            finally
            {
                if (isTemp && File.Exists(filePath))
                    try { File.Delete(filePath); } catch { }
            }
        }
        catch (Exception ex)
        {
            await ThreeButtonDialogView.ShowErrorAsync("Drop Error", "An error occurred while processing the dropped item.", detail: ex.Message, owner: this);
        }
    }

    private static bool IsValidDrop(DragEventArgs e, MediaItemViewModel mediaItem)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.TryGetFiles()?.ToList();
            if (files?.Count == 1)
                return mediaItem.IsValidDrop(files[0].Path.LocalPath);
            return false;
        }

        if (mediaItem.IsVideo || mediaItem.IsManual)
            return false;

        return e.DataTransfer.Formats.Contains(DataFormat.CreateStringPlatformFormat("text/uri-list")) ||
              e.DataTransfer.Formats.Contains(DataFormat.CreateStringPlatformFormat("text/html")) ||
              e.DataTransfer.Formats.Contains(DataFormat.Text);

    }

    private static async Task<(string? FilePath, bool IsTemp)> GetDroppedFile(DragEventArgs e, MediaItemViewModel mediaItem)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.TryGetFiles()?.ToList();
            if (files?.Count == 1)
                return (files[0].Path.LocalPath, false);
            return (null, false);
        }

        if (mediaItem.IsVideo || mediaItem.IsManual)
            return (null, false);

        var uriListFormat = DataFormat.CreateStringPlatformFormat("text/uri-list");
        if (e.DataTransfer.Formats.Contains(uriListFormat))
        {
            var url = e.DataTransfer.TryGetText()?.Trim();
            if (!string.IsNullOrWhiteSpace(url))
                return (await MediaDropHelper.DownloadImageFromUrlAsync(url), true);
        }


        if (e.DataTransfer.Contains(DataFormat.Text))
        {
            var text = e.DataTransfer.TryGetText()?.Trim();
            if (!string.IsNullOrEmpty(text) &&
                Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
                return (await MediaDropHelper.DownloadImageFromUrlAsync(text), true);
        }

        var htmlFormat = DataFormat.CreateStringPlatformFormat("text/html");
        if (e.DataTransfer.Formats.Contains(htmlFormat))
        {
            var html = e.DataTransfer.TryGetText() ?? string.Empty;
            var imageUrl = MediaDropHelper.ExtractImageUrlFromHtml(html);
            if (!string.IsNullOrEmpty(imageUrl))
                return (await MediaDropHelper.DownloadImageFromUrlAsync(imageUrl), true);
        }


        return (null, false);
    }

    #endregion
}
