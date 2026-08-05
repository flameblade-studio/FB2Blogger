# FB2Blogger
<p align="center">
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/ci.yml"><img alt="Windows CI" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/ci.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/releases"><img alt="Latest release" src="https://img.shields.io/github/v/release/hitoshic1982/FB2Blogger?label=release"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue.svg"></a>
</p>


[繁體中文](README.md) · [简体中文](README.zh-CN.md) · [English](README.en.md) · [日本語](README.ja.md)

一款 Windows 桌面工具，用于整理 Facebook 官方下载资料中的帖子、图片和视频，并迁移到 Google Blogger。

> 内容不应只存放在一个规则随时可能改变的平台。FB2Blogger 帮助创作者把多年积累的内容带回可搜索、可备份、可长期经营的网站。

## 项目由来

2026 年 7 月，炎剑文化工作室的创作者在 Facebook 粉丝专页被停权后，决定不再把数字资产的命运交给单一平台。借助 ChatGPT Codex，三天内完成了迁移工具、Blogger 备份站和 WordPress 品牌主站。FB2Blogger 正是这次数字主权自救行动的成果之一。

本工具只读取由你本人通过 Facebook“下载你的信息”取得的 ZIP。它不会登录、抓取或绕过 Facebook，也无法恢复被停权的账号或专页。

## 主要功能

- 解析 Facebook 官方导出的帖子 JSON，并修复部分旧版编码乱码。
- 保留时间、正文、Emoji 和 Hashtag；Hashtag 可转换为 Blogger 标签。
- 图片经过安全优化后上传 Google Drive，视频可上传 YouTube。
- 支持先建立草稿，便于逐篇检查。
- 使用隐藏识别标记避免重复导入。
- 保存迁移进度，可在中断后继续，并能从备份恢复损坏的进度文件。
- 阻止恶意 ZIP 路径穿越，不修改原始 ZIP 和原图。

## 使用准备

1. Windows 10/11 x64。
2. 从 Facebook 下载自己的数据，建议选择 JSON 并包含帖子、照片和视频。
3. 在 Google Cloud 建立 Desktop OAuth Client，启用 Blogger API v3、Google Drive API；迁移视频时再启用 YouTube Data API v3。
4. 从 [GitHub Releases](https://github.com/hitoshic1982/FB2Blogger/releases/latest) 下载最新版 `FB2Blogger.exe` 并核对 SHA256。

## 基本步骤

1. 启动程序，填写你自己的 Google OAuth Client ID；只有 OAuth 配置需要时才填写 Client Secret。
2. 选择 Facebook ZIP。
3. 选择目标 Blogger 博客以及公开或草稿模式。
4. 开始迁移，完成后查看报告并抽查文章、媒体和日期。

建议先使用测试博客或草稿模式。Google API 配额、Facebook 导出格式差异以及大量媒体上传可能需要分批处理。

## 隐私与安全

- OAuth 凭证和刷新令牌仅存放于当前 Windows 用户的 LocalAppData，并使用 Windows DPAPI 加密。
- 仓库不包含作者的密钥、令牌、Facebook 导出文件或个人文章。
- 软件直接连接 Google API；炎剑文化工作室不会通过中转服务器收集你的内容。

详见 [PRIVACY.md](PRIVACY.md) 与 [SECURITY.md](SECURITY.md)。

## 从源代码构建

```powershell
dotnet build src/FB2Blogger/FB2Blogger.csproj -c Release
dotnet run --project tests/AuditHarness/AuditHarness.csproj -c Release
```

需要 .NET 10 SDK。Release 提供自包含单文件 EXE，普通用户无需安装 .NET Runtime。

## 开源与责任

本项目采用 [MIT License](LICENSE)。本工具与 Meta、Facebook、Google 或 Blogger 没有隶属或背书关系。请只迁移你有权处理的内容，并遵守平台条款、版权和个人信息法规。

## 自由赞助

FB2Blogger 的全部迁移功能均依 MIT 许可证免费开放，不会因是否赞助而限制功能。如果它帮助你保存了珍贵文章并减少手动整理时间，欢迎自愿支持炎剑文化工作室继续维护开源工具：

- [Buy Me a Coffee](https://buymeacoffee.com/flameblade_studio)
- [PayPal.Me](https://www.paypal.com/paypalme/flamebladestudio)

不赞助也完全没有关系；报告问题、改进文档或提交 PR，同样是重要的支持。

作者：CHOU MING HUA／炎剑文化工作室 · [官方网站](https://www.flamebladestudio.com.tw/)

