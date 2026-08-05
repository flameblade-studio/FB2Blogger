namespace FB2Blogger;

internal sealed class SetupDialog : Form
{
    readonly TextBox clientId = new() { Width = 470, PlaceholderText = "Google OAuth Desktop Client ID" };
    readonly TextBox secret = new() { Width = 470, PlaceholderText = "Client Secret（若有）", UseSystemPasswordChar = true };
    readonly ComboBox privacy = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly CheckBox draft = new() { Text = "文章先建立為草稿", AutoSize = true };
    public AppSettings Settings { get; }

    public SetupDialog(AppSettings settings)
    {
        Settings = settings; Text = "FB2Blogger 第一次設定"; Width = 590; Height = 390; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; Font = new("Microsoft JhengHei UI", 10);
        clientId.Text = settings.ClientId; secret.Text = settings.ClientSecret;
        privacy.Items.AddRange(["不公開（只有自己）", "知道網址的人可看", "公開"]);
        privacy.SelectedIndex = settings.VideoPrivacy switch { "public" => 2, "unlisted" => 1, _ => 0 }; draft.Checked = settings.CreateAsDraft;
        var note = new Label { AutoSize = false, Width = 520, Height = 95, Text = "只需設定一次\r\n\r\n請在 Google Cloud 建立「Desktop app」OAuth 憑證，並啟用 Blogger API、YouTube Data API、Google Drive API。授權 Token 會以 Windows 帳號加密保存。" };
        var save = new Button { Text = "儲存並連接 Google", AutoSize = true, Height = 36 };
        var cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(clientId.Text)) { MessageBox.Show("請貼上 Google OAuth Client ID。", "FB2Blogger"); return; } settings.ClientId = clientId.Text.Trim(); settings.ClientSecret = secret.Text.Trim(); settings.VideoPrivacy = privacy.SelectedIndex switch { 2 => "public", 1 => "unlisted", _ => "private" }; settings.CreateAsDraft = draft.Checked; settings.RefreshToken = ""; SettingsStore.Save(settings); DialogResult = DialogResult.OK; };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(22), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        panel.Controls.Add(note); panel.Controls.Add(clientId); panel.Controls.Add(secret);
        var row = new FlowLayoutPanel { Width = 520, Height = 40 }; row.Controls.Add(new Label { Text = "YouTube：", AutoSize = true, Padding = new(0, 8, 0, 0) }); row.Controls.Add(privacy); row.Controls.Add(draft); panel.Controls.Add(row);
        var buttons = new FlowLayoutPanel { Width = 520, Height = 50 }; buttons.Controls.Add(save); buttons.Controls.Add(cancel); panel.Controls.Add(buttons); Controls.Add(panel); AcceptButton = save; CancelButton = cancel;
    }
}
