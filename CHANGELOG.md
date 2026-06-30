# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Protocol Provider 三件套產生精靈：`Tools / PinionCore / NetSync / Create Protocol Provider...`
  （亦可從 Project 視窗右鍵 Create 選單開啟），一鍵產生 `Creator` + `Provider` 並自動建立 `.asset`，
  並偵測目標 asmdef 是否 reference `PinionCore.NetSync`、提供一鍵補上參考。

### Changed
- `ProtocolProvider` 現在直接實作 `PinionCore.Remote.IProtocol`；抽象方法由 `Create()` 改名為 `Get()`。
  `Server` / `Client` 的 `Protocol` 直接回傳 `Provider` 本身。
  既有子類需把 `override ... Create()` 改為 `override ... Get()`。

## [0.0.1] - 2024-10-26

### Added
- Initial release
- Soul-Ghost architecture for network synchronization
- TCP, WebSocket, and Standalone transport layers
- Position tracking with compression system
- RMI (Remote Method Invocation) support
- Unity 2022.2+ compatibility
