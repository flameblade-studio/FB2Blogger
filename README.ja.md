# FB2Blogger

[繁體中文](README.md) · [简体中文](README.zh-CN.md) · [English](README.en.md) · [日本語](README.ja.md)

Facebook の公式ダウンロードデータに含まれる投稿・画像・動画を整理し、Google Blogger へ移行する Windows デスクトップツールです。

> 作品を、規約が突然変わり得る一つのプラットフォームだけに預けるべきではありません。FB2Blogger は、長年のコンテンツを検索・バックアップできる自分のサイトへ戻す手助けをします。

## 開発の背景

2026 年 7 月、Flameblade Studio が運営していた Facebook ページが停止されたことをきっかけに、作者はデジタル資産を一つのプラットフォームだけに委ねないと決めました。ChatGPT Codex と協力し、3 日間で移行ツール、Blogger のバックアップ拠点、WordPress の公式サイトを構築しました。FB2Blogger は、その「デジタル主権を取り戻す」取り組みから生まれました。

本アプリが読み取るのは、利用者本人が Facebook の「個人データをダウンロード」機能から取得した ZIP だけです。Facebook への自動ログイン、スクレイピング、制限回避は行わず、停止されたアカウントやページを復旧する機能もありません。

## 主な機能

- Facebook 公式エクスポートの投稿 JSON を解析し、一部の旧形式の文字化けも修復。
- 投稿日時、本文、絵文字、ハッシュタグを保持し、ハッシュタグを Blogger ラベルへ変換。
- 画像を安全に最適化して Google Drive へ、動画を必要に応じて YouTube へアップロード。
- 下書き作成に対応し、公開前に一件ずつ確認可能。
- 非表示の識別マーカーで重複インポートを防止。
- 中断後の再開、進捗ファイル破損時のバックアップ復元に対応。
- ZIP パストラバーサルを防止し、元の ZIP と画像を変更しない設計。

## 必要なもの

1. Windows 10/11 x64。
2. Facebook から自分で取得した JSON 形式のデータ ZIP。
3. Google Cloud の Desktop OAuth Client。Blogger API v3 と Google Drive API を有効にし、動画移行時のみ YouTube Data API v3 も有効にします。
4. [GitHub Releases](https://github.com/hitoshic1982/FB2Blogger/releases/latest) から最新版 `FB2Blogger.exe` を取得し、SHA256 を確認してください。

## 基本手順

1. アプリを起動し、自分の Google OAuth Client ID を入力します。Client Secret は設定上必要な場合だけ入力します。
2. Facebook ZIP を選びます。
3. 移行先 Blogger と、下書き／公開モードを選びます。
4. 移行後にレポートを確認し、投稿・メディア・日付を抜き取り確認します。

最初はテスト用ブログまたは下書きモードを推奨します。Google API の割り当て、Facebook エクスポート形式の差、メディア量により、複数回に分ける場合があります。

## プライバシーと安全性

- OAuth 情報と更新トークンは現在の Windows ユーザーの LocalAppData のみに保存し、Windows DPAPI で暗号化します。
- リポジトリに作者の認証情報、トークン、Facebook データ、個人記事は含まれません。
- アプリは Google API と直接通信し、Flameblade Studio が内容を受け取る中継サーバーはありません。

[PRIVACY.md](PRIVACY.md) と [SECURITY.md](SECURITY.md) もご覧ください。

## ソースからビルド

```powershell
dotnet build src/FB2Blogger/FB2Blogger.csproj -c Release
dotnet run --project tests/AuditHarness/AuditHarness.csproj -c Release
```

.NET 10 SDK が必要です。Release の EXE は自己完結型の単一ファイルで、利用者が .NET Runtime を別途導入する必要はありません。

## ライセンスと責任

[MIT License](LICENSE) で公開しています。本プロジェクトは Meta、Facebook、Google、Blogger と提携または公認されたものではありません。権利を持つコンテンツだけを移行し、各サービスの規約、著作権、個人情報保護法令を守ってください。

作者：CHOU MING HUA／Flameblade Studio · [公式サイト](https://www.flamebladestudio.com.tw/)
