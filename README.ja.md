# FB2Blogger
<p align="center">
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/ci.yml"><img alt="Cross-platform CI" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/ci.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/codeql.yml"><img alt="CodeQL" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/codeql.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/security-audit.yml"><img alt="Security Audit / NuGet" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/security-audit.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/secret-defense.yml"><img alt="Secret Defense / Gitleaks" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/secret-defense.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/releases"><img alt="Latest release" src="https://img.shields.io/github/v/release/hitoshic1982/FB2Blogger?label=release"></a>
  <a href="LICENSE"><img alt="MIT" src="https://img.shields.io/badge/license-MIT-blue.svg"></a>
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&amp;logoColor=white">
  <img alt="Four interface languages" src="https://img.shields.io/badge/interface%20languages-4-informational">
</p>

> 本プロジェクトは[炎剣オープンソース・ソフトウェア・ファミリー品質基準](docs/RELEASE-PROCESS.md)に従い、実証済みの検証、4 言語同期、追跡可能なリリース、既存機能の維持を徹底します。

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

## 3 OS 対応の進捗

- **Windows：**従来の完全版 WinForms アプリは、既存機能、LocalAppData の保存場所、DPAPI による認証情報保護をそのまま維持します。
- **macOS／Linux：**Avalonia の基礎プレビューでは、Facebook ZIP の選択、安全な展開、オフライン解析まで行えます。Google 認証、メディアのアップロード、Blogger 公開は未接続で、正式版ではありません。
- **共通基盤：**モデル、解析、安全な展開、記事内容の生成、移行状態、OS に応じたデータ保存先を `FB2Blogger.Core` に分離し、GitHub Actions で Windows・macOS・Linux ごとにビルドとテストを行います。

CI 成功は macOS／Linux 実機検証の代わりにはなりません。プロジェクト所有者の手元には現在 Windows PC しかないため、正式対応には各 OS の協力者による実環境テスト記録が必要です。正確な機能表と安全設計は[クロスプラットフォーム基盤文書](docs/CROSS-PLATFORM.md)をご覧ください。

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
dotnet build src/FB2Blogger.Core/FB2Blogger.Core.csproj -c Release
dotnet run --project tests/CoreHarness/CoreHarness.csproj -c Release
dotnet build src/FB2Blogger.Desktop/FB2Blogger.Desktop.csproj -c Release
dotnet build src/FB2Blogger/FB2Blogger.csproj -c Release
dotnet run --project tests/AuditHarness/AuditHarness.csproj -c Release
```

.NET 10 SDK が必要です。現在の正式 Release は Windows 向け自己完結型の単一 EXE のままで、Avalonia プレビューはまだ正式ダウンロードとして提供しません。

## ライセンスと責任

[MIT License](LICENSE) で公開しています。本プロジェクトは Meta、Facebook、Google、Blogger と提携または公認されたものではありません。権利を持つコンテンツだけを移行し、各サービスの規約、著作権、個人情報保護法令を守ってください。

## 任意のご支援

FB2Blogger の移行機能はすべて MIT ライセンスのもとで無償公開されており、支援の有無によって機能が制限されることはありません。大切な記事の保存や手作業の削減に役立った場合は、炎剣文化工作室によるオープンソース保守を任意でご支援いただけます。

- [Buy Me a Coffee](https://buymeacoffee.com/flameblade_studio)
- [PayPal.Me](https://www.paypal.com/paypalme/flamebladestudio)

ご支援は必須ではありません。不具合報告、文書の改善、プルリクエストも同じく大切な貢献です。

作者：CHOU MING HUA／Flameblade Studio · [公式サイト](https://www.flamebladestudio.com.tw/)
