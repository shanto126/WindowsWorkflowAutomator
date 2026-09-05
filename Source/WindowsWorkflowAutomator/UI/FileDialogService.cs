namespace WindowsWorkflowAutomator.UI;

internal static class FileDialogService
{
    public static Task<string?> SelectFolderAsync(
        string description,
        string? selectedPath,
        SynchronizationContext? uiContext = null)
    {
        return RunOnPickerThreadAsync(() =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                SelectedPath = selectedPath ?? string.Empty
            };

            return dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath)
                ? dialog.SelectedPath
                : null;
        }, uiContext, "The folder picker could not be opened.");
    }

    public static Task<string[]?> SelectFilesAsync(
        string title,
        string filter,
        string? initialDirectory,
        bool multiselect,
        SynchronizationContext? uiContext = null)
    {
        return RunOnPickerThreadAsync(() =>
        {
            using var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                Multiselect = multiselect,
                InitialDirectory = initialDirectory ?? string.Empty
            };

            return dialog.ShowDialog() == DialogResult.OK && dialog.FileNames.Length > 0
                ? dialog.FileNames
                : null;
        }, uiContext, "The file picker could not be opened.");
    }

    private static Task<T?> RunOnPickerThreadAsync<T>(
        Func<T?> picker,
        SynchronizationContext? uiContext,
        string errorMessage)
    {
        var completion = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pickerThread = new Thread(() =>
        {
            try
            {
                completion.TrySetResult(picker());
            }
            catch (Exception ex)
            {
                completion.TrySetResult(default);
                uiContext?.Post(
                    _ => MessageBox.Show(
                        $"{errorMessage}\n\n{ex.Message}",
                        "File selection error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning),
                    null);
            }
        })
        {
            IsBackground = true,
            Name = "WindowsWorkflowAutomator.FilePicker"
        };

        pickerThread.SetApartmentState(ApartmentState.STA);
        pickerThread.Start();
        return completion.Task;
    }
}