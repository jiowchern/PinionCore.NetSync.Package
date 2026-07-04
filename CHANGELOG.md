# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Protocol Provider 三件套產生精靈：`Tools / PinionCore / NetSync / Create Protocol Provider...`
  （亦可從 Project 視窗右鍵 Create 選單開啟），一鍵產生 `Creator` + `Provider` 並自動建立 `.asset`，
  並偵測目標 asmdef 是否 reference `PinionCore.NetSync`、提供一鍵補上參考。
- Gateway 分散式路由閘道元件（`PinionCore.NetSync.Gateways` 命名空間，封裝 `PinionCore.Remote.Gateway`）：
  - `GatewayRouter` + `GatewayRouterEndpoint`：中央路由器，提供 Registry / Session 兩個端點，
    依 Group 與協議版本自動路由，事件驅動不需 Update。
  - `GatewayRegistry`：與 `Server` 同物件使用，向 Router 註冊 Group 並把路由來的玩家連線自動餵給 `Server`。
  - `GatewayClient`：取代 `Client` 的閘道客戶端（`Gateway.Agent` + `AgentPool`），
    經 Router 同時取得多個服務的代理物件，用法與 `Client` 相同。
- 共用介面：`IConnectableAgent`（Connector 的連線目標：`Client` / `GatewayClient` / `GatewayRegistry`）、
  `IListenableHost`（Listener 的掛載宿主：`Server` / `GatewayRouterEndpoint`）、
  `IQueryerHost`（Ghost 查詢入口：`Client` / `GatewayClient`）。
- Gateway 全流程整合測試 `Tests/GatewayTests.cs`（Standalone 傳輸：Router → Registry 註冊 → 客戶端連線 → 跨路由 RMI）。

### Changed
- Tcp / Web / Standalone 的 Connector 與 Listener 改為透過 `IConnectableAgent` / `IListenableHost`
  尋找目標元件（原本寫死 `GetComponent<Client>` / `GetComponent<Server>`），並移除對應的
  `RequireComponent` 屬性。既有場景不受影響。
- `GhostProvider` 的 `Client` 欄位未指派時，會自動改抓同物件上的 `IQueryerHost`
  （`Client` 或 `GatewayClient`）。
- 最低 Unity 版本需求由 `2022.2` 提升至 `6000.4`（Unity 6）。
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
