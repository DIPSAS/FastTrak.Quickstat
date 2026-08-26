namespace QuickStat.Services;

/// <summary>Everything a save-file dialog needs, as one value.</summary>
/// <remarks>
/// Modelled on the <c>TFileSaveDialog</c> the Delphi configures at design time
/// (<c>MainQuickStat.dfm</c>, <c>FileSaveDialog1</c>), so the defaults here are that dialog's
/// settings and a caller only has to override what differs.
/// </remarks>
public sealed record SaveFileRequest
{
    /// <summary>Pre-filled file name, extension included. Delphi <c>FileName = 'QuickStat.csv'</c>.</summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Extension appended when the user types a bare name. Delphi <c>DefaultExtension = '*.csv'</c>,
    /// written here without the wildcard because that is what the WPF dialog expects.
    /// </summary>
    public required string DefaultExtension { get; init; }

    /// <summary>
    /// Win32 filter string. Delphi's single file type is
    /// <c>Comma separated values</c> / <c>*.csv</c>.
    /// </summary>
    public required string Filter { get; init; }

    /// <summary>Dialog caption, or <see langword="null"/> for the shell default.</summary>
    public string? Title { get; init; }

    /// <summary>Ask before overwriting. Delphi <c>fdoOverWritePrompt</c>.</summary>
    public bool OverwritePrompt { get; init; } = true;

    /// <summary>The Delphi's <c>Save dataset to CSV file</c> dialog, verbatim.</summary>
    /// <remarks>
    /// One thing does not carry over: <c>OkButtonLabel = 'Save'</c>. WPF's
    /// <c>Microsoft.Win32.SaveFileDialog</c> does not expose the underlying
    /// <c>IFileDialog::SetOkButtonLabel</c>, and the shell already labels a save dialog's button
    /// <c>Save</c>, so the visible result is identical. <c>fdoStrictFileTypes</c> has no equivalent
    /// either; <c>AddExtension</c> plus a single filter entry is the closest the managed dialog gets.
    /// </remarks>
    public static SaveFileRequest DatasetCsv => new()
    {
        FileName = "QuickStat.csv",
        DefaultExtension = "csv",
        Filter = "Comma separated values|*.csv",
    };
}
