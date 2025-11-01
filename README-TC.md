# PinionCore.NetSync

> Unity 網路同步套件 - 基於 [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) 框架，提供 Remote Method Invocation (RMI) 與 Soul-Ghost 架構的客戶端-伺服器網路通訊。

[![Unity Version](https://img.shields.io/badge/Unity-2022.2%2B-blue)](https://unity.com/)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-purple)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

**語言**: [English](README-EN.md) | [繁體中文](README-TC.md)

---

## 🎮 線上 Demo

體驗 PinionCore.NetSync 即時多人聊天範例：

### **👉 [https://proxy.pinioncore.dpdns.org/sample2](https://proxy.pinioncore.dpdns.org/sample2)**

**Demo 展示特性**:
- ✅ WebGL 平台的 WebSocket 連接
- ✅ 即時多人同步
- ✅ Remote Method Invocation (RMI) 遠端方法呼叫
- ✅ Soul-Ghost 網路架構

---

## 特性

### 🏗️ Soul-Ghost 架構
- **Soul（伺服器端）**：執行遊戲邏輯，維護權威狀態
- **Ghost（客戶端）**：接收並渲染伺服器狀態
- **自動綁定**：透過 `IObject` 介面自動配對 Soul 與 Ghost
- **位置壓縮**：Tracker System 減少頻寬消耗，自動插值處理

### 🌐 多傳輸層支援
- **TCP**：可靠、有序傳輸（預設）
- **WebSocket**：WebGL 平台、穿越防火牆
- **Standalone**：本地模擬、單元測試（無需網路）

### 🚀 現代化開發體驗
- **C# Source Generators**：自動生成網路協議程式碼
- **Unity MonoBehaviour 整合**：拖拽式設定 Server/Client 組件
- **StatusMachine 狀態管理**：事件驅動設計，避免 enum/switch 判斷
- **Gateway 模式**：支援多服務分佈式架構與負載平衡

### 🎯 易於擴展
- **Protocol 介面**：定義共享 RMI 介面
- **傳輸層抽象**：輕鬆擴充自訂網路協議
- **模組化設計**：清晰的 Soul/Ghost/Links 分層架構

---

## 安裝方式

### Unity Package Manager（推薦）

#### 方法 1：安裝最新版本
1. 開啟 Unity Editor
2. 前往 `Window > Package Manager`
3. 點擊 **`+`** → **`Add package from git URL...`**
4. 輸入：
   ```
   https://github.com/jiowchern/PinionCore.NetSync.Package.git
   ```
5. 點擊 **`Add`**

#### 方法 2：安裝特定版本（穩定）
使用版本標籤安裝特定版本：
```
https://github.com/jiowchern/PinionCore.NetSync.Package.git#v0.0.1
```

### 透過 manifest.json

在專案的 `Packages/manifest.json` 中加入：

```json
{
  "dependencies": {
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.Package.git#v0.0.1"
  }
}
```

### 可用版本

- `v0.0.1` - 初始發布版本
- Latest: `main` 分支（開發中）

---

## 快速開始

### 1️⃣ 伺服器端設定

```csharp
using PinionCore.NetSync;
using PinionCore.NetSync.Syncs.Souls;

public class GameServer : MonoBehaviour
{
    [SerializeField] GameObject soulPrefab;

    void Start()
    {
        // 添加 Server 組件
        var server = gameObject.AddComponent<Server>();

        // 添加 TCP 監聽器
        var listener = gameObject.AddComponent<Tcp.Listener>();
        listener.Port = 7777;

        // 處理客戶端連接
        server.BinderEvent.AddListener((command) =>
        {
            if (command.Status == Server.BinderCommand.OperatorStatus.Add)
            {
                // 客戶端連接：實例化 Soul 並綁定
                var soul = Instantiate(soulPrefab).GetComponent<Soul>();
                command.Binder.Bind<IObject>(soul);
            }
            else
            {
                // 客戶端斷線：清理資源
            }
        });
    }
}
```

### 2️⃣ 客戶端設定

```csharp
using PinionCore.NetSync;
using PinionCore.NetSync.Syncs.Ghosts;

public class GameClient : MonoBehaviour
{
    [SerializeField] GameObject ghostPrefab;

    void Start()
    {
        // 添加 Client 組件
        var client = gameObject.AddComponent<Client>();

        // 添加 TCP 連接器
        var connector = gameObject.AddComponent<Tcp.Connector>();
        connector.Host = "127.0.0.1";
        connector.Port = 7777;

        // 監聽遠端物件
        client.Queryer.QueryNotifier<IObject>().Supply += (obj) =>
        {
            // 伺服器發送物件：實例化 Ghost 並綁定
            var ghost = Instantiate(ghostPrefab).GetComponent<Ghost>();
            ghost.Bind(obj);
        };
    }
}
```

### 3️⃣ 建立 Soul 類別（伺服器端）

```csharp
using PinionCore.NetSync.Syncs.Souls;

public class PlayerSoul : Soul
{
    void Start()
    {
        // 伺服器端邏輯（權威狀態）
        // 自動同步到所有連接的客戶端
    }

    void Update()
    {
        // 處理玩家移動、遊戲邏輯等
    }
}
```

### 4️⃣ 建立 Ghost 類別（客戶端）

```csharp
using PinionCore.NetSync.Syncs.Ghosts;

public class PlayerGhost : Ghost
{
    void Update()
    {
        // 接收並渲染伺服器狀態
        // TrackerReceiver 自動處理位置插值
    }
}
```

---

## Soul-Ghost 架構

### 核心概念

| 組件 | 位置 | 職責 | 特性 |
|-----|------|------|------|
| **Soul** | 伺服器端 | 執行遊戲邏輯，維護權威狀態 | 自動發送 Transform 變化到所有客戶端 |
| **Ghost** | 客戶端 | 接收並渲染伺服器狀態 | 透過 TrackerReceiver 處理位置插值 |
| **IObject** | 共享介面 | 定義網路物件協議 | Soul 與 Ghost 的橋樑 |
| **Tracker** | 系統層 | 位置壓縮與軌跡插值 | 減少頻寬消耗（ZipTracker） |

### 工作流程

```
伺服器                          網路                           客戶端
  │                               │                               │
  ├─ 實例化 Soul                  │                               │
  ├─ Bind<IObject>(soul) ────────>│                               │
  │                               ├─ 發送 IObject 實例 ──────────>│
  │                               │                               ├─ 實例化 Ghost
  │                               │                               ├─ Bind(IObject)
  │                               │                               │
  ├─ Transform 更新 ─────────────>│                               │
  │   (TrackerSender)             ├─ 壓縮資料 ───────────────────>│
  │                               │                               ├─ 插值與渲染
  │                               │                               │   (TrackerReceiver)
```

### 主要優勢

1. **伺服器權威**：遊戲邏輯在伺服器執行，防止作弊
2. **自動同步**：Transform 變化自動同步到所有客戶端
3. **頻寬最佳化**：Tracker 系統壓縮位置資料
4. **流暢移動**：客戶端插值處理，確保流暢渲染

---

## 傳輸層

| 傳輸層 | 組件 | 適用場景 | 平台支援 |
|-------|-----|---------|---------|
| **TCP** | `Tcp.Listener`<br>`Tcp.Connector` | 可靠、有序傳輸 | Standalone, Editor |
| **WebSocket** | `Web.Listener`<br>瀏覽器內建 | WebGL 平台、穿越防火牆 | WebGL, Standalone |
| **Standalone** | `Standalone.Listener`<br>`Standalone.Connector` | 本地模擬、單元測試 | All Platforms |

### 平台選擇

```csharp
if (Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor)
{
    // WebGL 平台使用 WebSocket
    var state = new WebSocketState(endpoint);
}
else
{
    // 其他平台使用 TCP
    var state = new TcpSocketState(endpoint);
}
```

### 傳輸層詳細說明

#### TCP 傳輸
- **適用於**：PC/主機遊戲、專用伺服器
- **特性**：可靠、有序、連接導向
- **使用方式**：
  ```csharp
  // 伺服器
  var listener = gameObject.AddComponent<Tcp.Listener>();
  listener.Port = 7777;

  // 客戶端
  var connector = gameObject.AddComponent<Tcp.Connector>();
  connector.Host = "127.0.0.1";
  connector.Port = 7777;
  ```

#### WebSocket 傳輸
- **適用於**：WebGL 建置、瀏覽器遊戲
- **特性**：Web 相容、基於 HTTP、穿越防火牆
- **使用方式**：
  ```csharp
  // 伺服器
  var listener = gameObject.AddComponent<Web.Listener>();
  listener.Port = 8080;

  // 客戶端（WebGL 使用瀏覽器 WebSocket API）
  var state = new WebSocketState("ws://localhost:8080");
  ```

#### Standalone 傳輸
- **適用於**：本地測試、單人模式、單元測試
- **特性**：記憶體內、無需網路、即時
- **使用方式**：
  ```csharp
  // 伺服器
  var listener = gameObject.AddComponent<Standalone.Listener>();

  // 客戶端
  var connector = gameObject.AddComponent<Standalone.Connector>();
  ```

---

## 進階功能

### Gateway 模式

用於分佈式架構與負載平衡：

```csharp
using PinionCore.Remote.Gateway;

IAgent agent;
if (useGateway)
{
    // 使用 Gateway 進行分佈式服務
    var pool = new PinionCore.Remote.Gateway.Hosts.AgentPool(protocol);
    agent = new PinionCore.Remote.Gateway.Agent(pool);
}
else
{
    // 直接連接
    agent = PinionCore.Remote.Client.Provider.CreateAgent(protocol);
}
```

**Gateway 優勢**：
- 多伺服器負載平衡
- 服務發現與路由
- 協議版本管理
- 微服務架構支援

### StatusMachine 模式

事件驅動的連接生命週期狀態管理：

```csharp
public class Client : MonoBehaviour, IStatus
{
    readonly StatusMachine _Machine;

    private void Start()
    {
        _Machine.Push(this);  // 推送初始狀態
    }

    private void Update()
    {
        _Machine.Update();  // 驅動狀態機
    }

    void IConnect.Connect(string endpoint, bool gate)
    {
        var connectingState = new TcpSocketState(endpoint);
        connectingState.SuccessEvent += (stream) =>
        {
            _ToSetupMode(stream, gate);
        };
        _Machine.Push(connectingState);
    }
}
```

**StatusMachine 優勢**：
- 清晰的狀態轉換
- 事件驅動設計（無 enum/switch）
- 在 Enter/Leave 管理資源
- 易於測試與除錯

### 自訂協議定義

定義自訂 RMI 介面：

```csharp
// 定義共享介面
public interface IPlayer
{
    PinionCore.Remote.Value<string> GetName();
    void Move(float x, float y, float z);
}

// 伺服器實作
public class PlayerSoul : Soul, IPlayer
{
    PinionCore.Remote.Value<string> IPlayer.GetName()
    {
        return "PlayerName";
    }

    void IPlayer.Move(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);
    }
}

// 客戶端綁定
client.Queryer.QueryNotifier<IPlayer>().Supply += (player) =>
{
    var name = player.GetName();
    player.Move(10, 0, 10);
};
```

---

## 範例專案

完整的工作範例請參閱[開發儲存庫](https://github.com/jiowchern/PinionCore.NetSync)：

### Sample1（基礎範例）
**位置**: `Assets/PinionCore/Sample1/`

**功能**：
- TCP、WebSocket、Standalone 傳輸測試
- 簡單的 Soul-Ghost 同步
- 基本 MonoBehaviour 設定

**適合**：
- 初次使用者
- 理解基本架構
- 測試不同傳輸層

### Sample2-Chat（進階範例）
**位置**: `Assets/PinionCore/Sample2-Chat/`

**功能**：
- 多人聊天室
- StatusMachine 狀態管理
- Gateway 模式切換
- TCP/WebSocket 平台偵測
- WebGL 建置支援

**關鍵檔案**：
- `Client.cs`：主控制器（IConnect, IStatus）
- `Controller.cs`：UI 邏輯（Unity Events）
- `LoopState.cs`：遊戲循環狀態
- `TcpSocketState.cs`, `WebSocketState.cs`：連接狀態

**適合**：
- 實際專案參考
- StatusMachine 模式學習
- WebGL 部署

---

## 最佳實踐

### 1. 連接處理

✅ **正確** - 使用 SocketErrorEvent：
```csharp
void Enter()
{
    _Peer.SocketErrorEvent += _OnSocketError;
}

void Leave()
{
    _Peer.SocketErrorEvent -= _OnSocketError;
}
```

❌ **錯誤** - 使用 Ping 輪詢：
```csharp
void Update()
{
    if (!_Agent.Ping()) // 不要這樣做！
    {
        // 處理斷線
    }
}
```

### 2. 資源管理

使用 `_Dispose` 閉包模式：

```csharp
Action _Dispose = () => { };

void Start()
{
    var resource = CreateResource();
    _Dispose = () => resource.Cleanup();
}

void OnDestroy()
{
    _Dispose();
}
```

### 3. 避免 Static 類別

❌ **不要**：使用 static 類別儲存遊戲狀態
```csharp
public static class GameManager // 網路遊戲中不佳
{
    public static int Score;
}
```

✅ **應該**：使用實例化設計
```csharp
public class GameState : Soul
{
    public int Score { get; set; }
}
```

### 4. 平台專屬程式碼

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL 專屬程式碼
    var connector = new WebSocketState(endpoint);
#else
    // Standalone 程式碼
    var connector = new TcpSocketState(endpoint);
#endif
```

---

## 系統需求

- **Unity**：2022.2 或更新版本（建議 Unity 6000.2+）
- **.NET Standard**：2.1
- **平台**：Windows、macOS、Linux、WebGL

---

## 文檔

- 📘 [開發指南](https://github.com/jiowchern/PinionCore.NetSync/blob/main/CLAUDE.md) - 完整架構與工作流程
- 📙 [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) - 核心 RMI 框架
- 🔗 [開發儲存庫](https://github.com/jiowchern/PinionCore.NetSync) - 完整開發環境

---

## 疑難排解

### WebGL 建置問題

**問題**：WebGL 建置中 WebSocket 連接失敗

**解決方案**：確保使用 WebSocket 傳輸：
```csharp
if (Application.platform == RuntimePlatform.WebGLPlayer)
{
    var state = new WebSocketState("ws://your-server:8080");
}
```

### 連接逾時

**問題**：客戶端幾秒後斷線

**解決方案**：檢查防火牆設定，確保伺服器在正確埠口監聽：
```csharp
// 伺服器
var listener = gameObject.AddComponent<Tcp.Listener>();
listener.Port = 7777;
Debug.Log($"伺服器監聽埠口 {listener.Port}");
```

### Soul-Ghost 未同步

**問題**：Soul 移動時 Ghost 未更新

**解決方案**：確保 Soul 和 Ghost 都繼承基礎類別：
```csharp
// 伺服器
public class MySoul : Soul { } // 必須繼承 Soul

// 客戶端
public class MyGhost : Ghost { } // 必須繼承 Ghost
```

---

## 效能優化建議

1. **使用 Tracker 系統**：自動壓縮位置資料
2. **批次更新**：將多個小更新合併為一個
3. **選擇性同步**：只同步必要的資料
4. **連接池**：盡可能重複使用連接

---

## 授權

MIT License - 詳見 [LICENSE](LICENSE)

---

## 連結

- 🏗️ [開發儲存庫](https://github.com/jiowchern/PinionCore.NetSync) - 包含範例的完整開發環境
- 🎮 [線上 Demo](https://proxy.pinioncore.dpdns.org/sample2) - 立即試用
- 📦 [PinionCore.Remote 框架](https://github.com/jiowchern/PinionCore.Remote) - 核心 RMI 框架

---

## 貢獻

歡迎貢獻！請造訪[開發儲存庫](https://github.com/jiowchern/PinionCore.NetSync)查看貢獻指南。

---

**使用 [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) 精心製作 ❤️**
