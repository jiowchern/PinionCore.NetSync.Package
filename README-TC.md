# PinionCore NetSync 套件封裝

PinionCore NetSync 為 Unity 專案提供傳輸層無關的狀態複寫能力。它包裝了底層的 PinionCore.Remote 傳輸堆疊，並提供易於使用的 Unity 元件，協助構建權威伺服器／客戶端體驗。

## 功能亮點

- 傳輸抽象層，內建 Standalone loopback、TCP 與 WebSocket 連接器。
- Ghost / Soul 複寫流程，可將權威端物件綁定並在遠端生成同步的 Ghost。
- Tracker 工具提供插值與壓縮（Zip 編碼、重複步進最小化）以同步 Transform 資料。
- 擴充的 Inspector 呈現協定雜湊、延遲、吞吐量與 Binder 清單。
- 基於 NUnit 的回歸測試涵蓋 Tracker 演算法與網路基礎元件。

## 安裝

### 作法一 — Git 依賴
在 `Packages/manifest.json` 直接引用此套件：

```json
{
  "dependencies": {
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.git?path=PinionCore.NetSync.Package"
  }
}
```

### 作法二 — 本機套件
若已將此儲存庫複製到 Unity 專案旁，則可加入：

```json
{
  "dependencies": {
    "com.pinioncore.netsync": "file:../PinionCore.NetSync.Package"
  }
}
```

更新 NetSync 時，也請同步更新 `PinionCore.Remote` 子模組以維持傳輸 API 一致。

## 快速上手

1. **權威端（Server）**
   - 在場景中放置 `Server` 元件。
   - 依需求加入相對應的 Listener (`Standalone.Listener`、`Tcp.TcpListener` 或 `Web.WebListener`)，並於初始化時呼叫其綁定方法（Standalone 使用 `Bind()`，TCP/Web 使用 `Bind(port)`）。
   - 監聽 `Server.BinderEvent` 以在客戶端註冊或移除 Binder 時觸發自訂行為。

2. **客戶端**
   - 新增 `Client` 元件並選擇使用的 Connector (`Standalone.Connector`、`Tcp.TcpConnector` 或 `Web.WebConnector`)。
   - 透過遊戲流程或 UI 呼叫 Connector 的 `Connect`／`Disconnect` 方法，可參考 `PinionCore.NetSync.Develop/Assets/PinionCore/Sample1/Scripts` 的範例寫法。

3. **物件同步**
   - 在權威端的 GameObject 上掛上 `Syncs.Souls.Soul`，並透過 `gameobject.Bind<T>()`（`SoulFinder` 擴充方法）註冊像是 `Syncs.Souls.Transform` 或 `Syncs.Souls.TrackerSender` 等協定物件。
   - 在遠端 Prefab 上加上 `Syncs.Ghosts.Ghost` 與對應的 Ghost 行為（例如 `Syncs.Ghosts.Transform` 或 `Syncs.Ghosts.TrackerReceiver`），並透過 `gameObject.Query<T>()` 取得資料。
   - 可利用 Tracker 的時間參數或 `Transform.SyncInterval` 控制複寫頻率。

4. **執行時診斷**
   - 於初始化前設定 `Client.EnableLog` 或 `Server.EnableLog` 以啟用記錄。
   - `Server`、`Client`、Listener 與 Connector 的 Inspector 擴充會顯示協定雜湊、Ping、吞吐量與 Binder 狀態。

## 架構導覽

- **Links** (`Runtime/Scripts/Links`)：傳輸抽象層與負責建立協定的 `ProtocolCreator`，用來橋接 PinionCore.Remote 的串流。
- **Syncs** (`Runtime/Scripts/Syncs`)：提供 Ghost / Soul 綁定工具、Tracker 壓縮與 Notifier 基礎設施。
- **Extensions**：`GameObject.Bind<T>()`、`GameObject.Unbind()`、`GameObject.Query<T>()` 與 UI Label 綁定等輔助方法。
- **Editor** (`Editor/Scripts`)：以 UI Toolkit 建置的 Inspector 擴充與相關資源。
- **Tests** (`Tests/`)：涵蓋 Tracker 取樣、壓縮與網路合約的 NUnit 測試。
- **Analyzers** (`Analyzers/`)：CI 執行的 Roslyn 分析器。

## 範例

可在 Unity Package Manager 中匯入範例場景以檢視設定：

- **Sample 1** 與開發專案場景一致，介紹 Standalone、TCP 與 WebSocket 連線流程。
- **Sample 2 – Chat** 展示協定切換與 UI 互動回饋。

## 測試

將套件嵌入專案後，可透過 Unity Test Runner（Edit Mode）執行 NUnit 測試。自動化時可使用：

```powershell
"<UnityEditorPath>\Unity.exe" -projectPath <your-project> -quit -batchmode -runTests -testPlatform EditMode -testResults Logs/editmode.xml
```

請在 CI 內執行 `Analyzers/` 的分析器，以避免 API 或程式風格回歸。

## 版本管理

- 套件版本定義於 `package.json`。
- 所有使用端都應維持 `PinionCore.Remote` 在同一個標籤。
- 將使用者可感知的變更紀錄於 `CHANGELOG.md`。

## 支援

若有問題或功能需求，請在主儲存庫的 Issue Tracker 回報，並附上 Unity 版本、傳輸類型、重現步驟與相關紀錄。

## 授權

本套件沿用儲存庫中的 `LICENSE`。
