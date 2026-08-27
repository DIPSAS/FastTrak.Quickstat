using System.Windows;
using Microsoft.Win32;

namespace QuickStat.Services;

/// <summary>The WPF <see cref="IFileDialogService"/>, on <see cref="SaveFileDialog"/>.</summary>
public sealed class WpfFileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? ShowSaveFileDialog(SaveFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SaveFileDialog dialog = new()
        {
            FileName = request.FileName,
            DefaultExt = request.DefaultExtension,
            Filter = request.Filter,
            AddExtension = true,
            OverwritePrompt = request.OverwritePrompt,
            CheckPathExists = true,
        };

        if (request.Title is not null)
        {
            dialog.Title = request.Title;
        }

        // Application.Current may be null - most of the suite composes the container without one,
        // as does any headless host - and ShowDialog(null) is not an overload, so the owner has to
        // be chosen rather than passed through.
        Window? owner = Application.Current?.MainWindow;

        bool? result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);

        return result == true ? dialog.FileName : null;
    }
}
