# qhlc

Godot 4 2D RPG 模板 + **AI 可治理** 起步包（Superpowers + Cursor）。

## 要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Godot 4.7.x **.NET 版**](https://godotengine.org/download)（**不是**标准版；须含 C# 模块，与 `Godot.NET.Sdk/4.7.2` 一致）

## 快速开始

见 [docs/governance/QUICKSTART.zh-CN.md](docs/governance/QUICKSTART.zh-CN.md)。

```powershell
dotnet build qhlc.sln
./scripts/check_all.ps1
```

## 资源目录 `assets/`

美术、音频等大文件放在 `assets/`，**已在 `.gitignore` 中排除**（仅保留 `assets/.gitkeep` 占位）。团队请用网盘、对象存储或 Git LFS 等单独同步资源。

## 文档

- 设计规格：`docs/superpowers/specs/2026-10-31-ai-governance-starter-design.md`
- Superpowers 配合：`docs/governance/superpowers-integration.zh-CN.md`
