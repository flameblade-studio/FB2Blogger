namespace FB2Blogger;

internal sealed class SetupDialog : Form
{
    readonly TextBox clientId = new() { Width = 470, PlaceholderText = "Google OAuth Desktop Client ID" };
    readonly TextBox secret = new() { Width = 470, PlaceholderText = L.T("secret_placeholder"), UseSystemPasswordChar = true };
    readonly ComboBox language = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox privacy = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly CheckBox draft = new() { Text = L.T("draft_default"), AutoSize = true };
    public AppSettings Settings { get; }

    public SetupDialog(AppSettings settings)
    {
        Settings = settings; Text = L.T("setup_title"); Width = 590; Height = 450; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; Font = new(L.FontName, 10);
        clientId.Text = settings.ClientId; secret.Text = settings.ClientSecret;
        language.Items.AddRange(L.Supported);
        language.SelectedItem = L.Supported.First(item => item.Code == L.Language);
        privacy.Items.AddRange([L.T("privacy_private"), L.T("privacy_unlisted"), L.T("privacy_public")]);
        privacy.SelectedIndex = settings.VideoPrivacy switch { "public" => 2, "unlisted" => 1, _ => 0 }; draft.Checked = settings.CreateAsDraft;
        var note = new Label { AutoSize = false, Width = 520, Height = 95, Text = L.T("setup_note") };
        var save = new Button { Text = L.T("save_connect"), AutoSize = true, Height = 36 };
        var cancel = new Button { Text = L.T("cancel"), AutoSize = true, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(clientId.Text)) { MessageBox.Show(L.T("missing_client_id"), "FB2Blogger"); return; } settings.InterfaceLanguage = ((LanguageOption)language.SelectedItem!).Code; settings.ClientId = clientId.Text.Trim(); settings.ClientSecret = secret.Text.Trim(); settings.VideoPrivacy = privacy.SelectedIndex switch { 2 => "public", 1 => "unlisted", _ => "private" }; settings.CreateAsDraft = draft.Checked; settings.RefreshToken = ""; SettingsStore.Save(settings); DialogResult = DialogResult.OK; };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(22), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        panel.Controls.Add(note);
        var languageRow = new FlowLayoutPanel { Width = 520, Height = 40 }; languageRow.Controls.Add(new Label { Text = L.T("language"), AutoSize = true, Padding = new(0, 8, 0, 0) }); languageRow.Controls.Add(language); languageRow.Controls.Add(new Label { Text = L.T("language_restart"), AutoSize = true, Padding = new(0, 8, 0, 0) }); panel.Controls.Add(languageRow);
        panel.Controls.Add(clientId); panel.Controls.Add(secret);
        var row = new FlowLayoutPanel { Width = 520, Height = 40 }; row.Controls.Add(new Label { Text = "YouTube：", AutoSize = true, Padding = new(0, 8, 0, 0) }); row.Controls.Add(privacy); row.Controls.Add(draft); panel.Controls.Add(row);
        var buttons = new FlowLayoutPanel { Width = 520, Height = 50 }; buttons.Controls.Add(save); buttons.Controls.Add(cancel); panel.Controls.Add(buttons); Controls.Add(panel); AcceptButton = save; CancelButton = cancel;
    }
}
