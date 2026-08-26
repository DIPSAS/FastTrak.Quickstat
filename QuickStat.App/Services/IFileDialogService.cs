namespace QuickStat.Services;

/// <summary>Shows the common file dialogs, so a view-model never touches one directly.</summary>
/// <remarks>
/// A modal shell dialog is the one thing that makes a command untestable: it blocks, and it needs a
/// window. Behind this interface, <c>SaveDatasetToCsvCommand</c> is an ordinary unit test with a
/// stub that returns a path - or <see langword="null"/> for cancel, which is the case that actually
/// has a bug in it.
/// </remarks>
public interface IFileDialogService
{
    /// <summary>Asks the user where to save a file.</summary>
    /// <param name="request">Initial file name, extension, filter.</param>
    /// <returns>The chosen full path, or <see langword="null"/> when the user cancelled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    string? ShowSaveFileDialog(SaveFileRequest request);
}
