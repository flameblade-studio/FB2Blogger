using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FB2Blogger;

namespace FB2Blogger.Desktop;

public sealed partial class MainWindow : Window
{
    readonly UiPreferencesStore preferences = new(AppPaths.Current);
    readonly ArchiveInspectionService inspector = new(AppPaths.Current);
    readonly ComboBox languageSelector;
    readonly TextBlock headingText;
    readonly TextBlock noticeText;
    readonly TextBlock languageLabel;
    readonly Button chooseButton;
    readonly Button inspectButton;
    readonly TextBox statusBox;
    string selectedArchive = "";
    bool changingLanguage;

    public MainWindow()
    {
        InitializeComponent();
        languageSelector = this.FindControl<ComboBox>("LanguageSelector")!;
        headingText = this.FindControl<TextBlock>("HeadingText")!;
        noticeText = this.FindControl<TextBlock>("NoticeText")!;
        languageLabel = this.FindControl<TextBlock>("LanguageLabel")!;
        chooseButton = this.FindControl<Button>("ChooseButton")!;
        inspectButton = this.FindControl<Button>("InspectButton")!;
        statusBox = this.FindControl<TextBox>("StatusBox")!;

        var savedLanguage = preferences.LoadLanguage();
        L.Configure(savedLanguage);
        languageSelector.ItemsSource = L.Supported;
        languageSelector.SelectedItem = L.Supported.First(item => item.Code == L.Language);
        languageSelector.SelectionChanged += ChangeLanguage;
        chooseButton.Click += ChooseArchive;
        inspectButton.Click += InspectArchive;
        ApplyText();
        try
        {
            inspector.CleanupStaleWorkspaces();
        }
        catch (Exception error)
        {
            statusBox.Text = error.Message;
        }
    }

    void ApplyText()
    {
        Title = L.T("desktop_title");
        headingText.Text = L.T("desktop_heading");
        noticeText.Text = L.T("desktop_preview_notice");
        languageLabel.Text = L.T("desktop_language");
        chooseButton.Content = L.T("desktop_choose_zip");
        inspectButton.Content = L.T("desktop_inspect");
        statusBox.Text = selectedArchive.Length == 0
            ? L.T("desktop_no_zip")
            : L.T("desktop_zip_selected", Path.GetFileName(selectedArchive));
    }

    void ChangeLanguage(object? sender, SelectionChangedEventArgs e)
    {
        if (changingLanguage || languageSelector.SelectedItem is not LanguageOption option) return;
        changingLanguage = true;
        try
        {
            L.Configure(option.Code);
            preferences.SaveLanguage(option.Code);
            ApplyText();
        }
        finally { changingLanguage = false; }
    }

    async void ChooseArchive(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = L.T("choose_export_zip"),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(L.T("desktop_zip_file_type"))
                {
                    Patterns = ["*.zip"],
                    MimeTypes = ["application/zip", "application/x-zip-compressed"]
                }
            ]
        });
        if (files.Count == 0) return;
        var localPath = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            statusBox.Text = L.T("desktop_local_file_required");
            return;
        }
        selectedArchive = localPath;
        inspectButton.IsEnabled = true;
        statusBox.Text = L.T("desktop_zip_selected", Path.GetFileName(selectedArchive));
    }

    async void InspectArchive(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (selectedArchive.Length == 0) return;
        chooseButton.IsEnabled = false;
        inspectButton.IsEnabled = false;
        statusBox.Text = L.T("desktop_inspecting");
        try
        {
            var result = await inspector.InspectAsync(selectedArchive);
            statusBox.Text = L.T("desktop_inspection_result", result.PostCount, result.ImageCount, result.VideoCount);
        }
        catch (Exception error)
        {
            statusBox.Text = L.T("desktop_inspection_failed", error.Message);
        }
        finally
        {
            chooseButton.IsEnabled = true;
            inspectButton.IsEnabled = true;
        }
    }
}
