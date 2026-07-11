# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed
- 移除 `Syncs` 的 Soul–Ghost component 同步層：`SoulProvider`、`GhostProvider`、`SoulFinder`、
  `GhostFinder`、`Soul`、`Ghost`、`GhostMonoBehaviour`、`Souls.Transform`、`Ghosts.Transform`、
  `Viewport`，以及協議介面 `IObject`、`ITransform`。
  此層試圖在 PinionCore.Remote 之上重建 Netcode 式的 per-GameObject component 同步
  （EntityId 關聯 + prefab 配對 + per-entity notifier 過濾），與 Remote「介面即同步單位」的模型重疊且擴展性差。
  改為直接使用 `ISessionBinder.Bind<T>()` / `Queryer.QueryNotifier<T>()`。
- Hierarchy 選單生成 Gateway Service / Gateway Client 時不再附掛 `SoulProvider` / `GhostProvider`。

### Changed
- 更新 `Runtime/Plugins` 內的 PinionCore DLL（2026-07-11 版）：序列化 wire format 壓縮改版
  （欄位 bitmask 取代 count+index、sealed/struct 宣告型別省略 runtime type-id、稠密陣列省略元素 index、
  `char[]` 移除多餘長度前綴），`PackageCallMethod` 類小訊息約縮 40%。
  **Breaking**：新舊版 DLL 的 wire format 不相容，伺服器與客戶端必須同時更新。
  另外，協議型別不需繼承時建議標 `sealed`，可獲得與 struct 相同的編碼密度。
- `User` / `UserProvider` 自 `Syncs` 移至 `Sessions` 資料夾，
  命名空間由 `PinionCore.NetSync.Syncs.Souls` 改為 `PinionCore.NetSync.Sessions`（asset GUID 不變，場景引用不受影響）。
- 協議介面不再需要繼承 `IObject`，直接繼承 `PinionCore.Remote.Protocolable` 即可。
- Tcp / Web / Standalone 的 Connector 與 Listener 改為透過 `IConnectableAgent` / `IListenableHost`
  尋找目標元件（原本寫死 `GetComponent<Client>` / `GetComponent<Server>`），並移除對應的
  `RequireComponent` 屬性。既有場景不受影響。
- `Server.BinderEvent`、`TcpConnector.ConnectResultEvent` / `ConnectBreakEvent` 等 UnityEvent
  欄位加上預設初始化，執行時期以 `AddComponent` 建立元件不再是 null。
- 最低 Unity 版本需求由 `2022.2` 提升至 `6000.4`（Unity 6）。
- `ProtocolProvider` 現在直接實作 `PinionCore.Remote.IProtocol`；抽象方法由 `Create()` 改名為 `Get()`。
  `Server` / `Client` 的 `Protocol` 直接回傳 `Provider` 本身。
  既有子類需把 `override ... Create()` 改為 `override ... Get()`。

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
- Hierarchy 右鍵選單 `GameObject → PinionCore → NetSync →`：
  一鍵生成 Gateway Router（含 Registry / Session 端點 + TcpListener + 自動 Bind Kit）/
  Gateway Service / Gateway Client（TCP / WebSocket / Standalone）已接線物件。
  專案中僅有一顆 ProtocolProvider 資產時自動指派。
- `Kits.StandaloneStartToConnect`：Start 時自動呼叫 `Standalone.Connector.Connect()`。
- Gateway 全流程整合測試 `Tests/GatewayTests.cs`：Standalone 傳輸與真實 TCP
  兩種端對端測試（Router → Registry 註冊 → 客戶端連線 → 跨路由 RMI）。

## [0.0.1] - 2024-10-26

### Added
- Initial release
- Soul-Ghost architecture for network synchronization
- TCP, WebSocket, and Standalone transport layers
- Position tracking with compression system
- RMI (Remote Method Invocation) support
- Unity 2022.2+ compatibility
