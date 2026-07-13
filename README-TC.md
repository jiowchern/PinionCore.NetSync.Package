# PinionCore NetSync

> 為 Unity 打造的權威式狀態同步套件 — 用「寫本機程式」的方式寫網路邏輯。

PinionCore NetSync 建立在 [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) 的 RMI 框架之上，
把底層的串流、序列化與封包處理全部封裝起來，只對外暴露幾個 Unity 元件與 ScriptableObject 資產。
你在伺服器端寫的是**權威物件（Soul）的普通 C# 邏輯**，客戶端則自動取得對應的**代理物件（Ghost）**，
中間不需要手寫任何封包、序列化或 RPC 派送程式碼。

- **語言 / Language**：[English](./README-EN.md) ｜ 繁體中文
- **Unity**：6000.4 以上（開發於 Unity 6000.2）
- **平台**：Standalone、WebGL（WebGL 使用 WebSocket）

---

## 為什麼選擇 NetSync

### 1. 像寫本機程式一樣寫網路邏輯（Soul–Ghost + RMI）
伺服器端的權威物件（**Soul**）就是一般的 C# 類別，遊戲邏輯寫在哪、狀態就在哪。
客戶端會自動收到對應的代理物件（**Ghost**），讀取屬性、呼叫方法就像操作本機物件一樣。
你**不需要**自己定義訊息格式、寫 `if (msgType == ...)` 的派送邏輯，或手動處理序列化。

### 2. 介面即協議，編譯期就型別安全
協議用一個普通的 **C# 介面**定義即可；Source Generator 會在編譯時自動產生對應的序列化協議。
- 屬性用 `Property<T>` 宣告 → 數值變動自動複寫到所有客戶端。
- 方法回傳 `Value<T>` → 即為跨網路的 Remote Method Invocation。
- 協議帶有 **VersionCode**：伺服器與客戶端握手時自動比對，協議不一致會立刻被擋下，而不是執行到一半才出現詭異錯誤。

### 3. 專注遊戲邏輯，不寫網路樣板
你只需要三件事：**(1)** 用介面定義「要同步什麼」、**(2)** 在 Soul 端設定狀態、**(3)** 在 Ghost 端讀取。
封包、序列化、連線管理全部由套件處理。連線（session）的生命週期則由 `Server.BinderEvent`
一個事件承接——每次上線／斷線都會觸發。

### 4. 傳輸層可隨意抽換，同一份遊戲程式碼通吃
| 傳輸層 | 元件 | 適用場景 |
|--------|------|----------|
| **Direct** | `Direct.DirectConnector` / `Direct.DirectClient` | 零序列化直通：ghost 即 Soul 本體（共用參考），**不需協議資產**；編輯器最速迭代、單元測試。不驗證可序列化性，上線前仍須以其他傳輸整合測試 |
| **Standalone** | `Standalone.Connector` / `Standalone.Listener` | 行程內回送：編輯器內測試、單機模擬、單元測試，**完全不開 socket**（序列化管線照跑） |
| **TCP** | `Tcp.TcpConnector` / `Tcp.TcpListener` | 可靠、有序的預設傳輸 |
| **WebSocket** | `Web.WebConnector` / `Web.WebListener` | WebGL 平台、需穿越防火牆 |

切換傳輸層只要換一個元件，遊戲邏輯一行都不用改。

### 5. 反應式物件生命週期，不需手寫 spawn 訊息
伺服器 `Bind` / `Unbind` 一個權威物件時，客戶端會透過 `INotifier<T>` 的 `Supply` / `Unsupply` 事件
**自動取得或移除**對應的 Ghost。物件進出視野、玩家上下線，全部事件驅動。

### 6. 設定即資產（ScriptableObject）
協議（`ProtocolProvider`）與連線端點（`ConnectionConfig`）都是可共用、可序列化的 `.asset`：
- 同一顆協議資產同時指派給 Server 與 Client → 兩端必定使用同一份協議版本。
- 同一顆連線資產同時指派給 Listener 與 Connector → 連接埠不會對不上。
- 端點、協議的調整不必動程式碼、不必重新編譯。

### 7. 內建執行時診斷
`Server`、`Client`、各 Listener / Connector 的 Inspector 直接顯示協議雜湊、Ping、連線狀態與即時流量
——收送位元組總數加上每秒傳輸速率，方便確認兩端協議是否一致、連線是否正常。
另有[執行時主控台](#執行時客戶端主控台)，可從遊戲內的命令列直接呼叫任何協議方法。

---

## 快速上手

以下用一個自訂的玩家物件 `IPlayer`（一個會同步的血量 + 一個遠端方法）走完整個流程。

### Step 0 — 安裝
在 Unity 專案的 `Packages/manifest.json` 加入相依：

```jsonc
{
  "dependencies": {
    // Git 引用
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.Package.git"
    // 或本機路徑引用：
    // "com.pinioncore.netsync": "file:../PinionCore.NetSync.Package"
  }
}
```

### Step 1 — 用介面定義你的協議
協議只是一個繼承 `Protocolable` 的 C# 介面。用 `Property<T>` 表示要同步的狀態，
用回傳 `Value<T>` 的方法表示客戶端可呼叫、伺服器執行的 RMI：

```csharp
using PinionCore.Remote;

public interface IPlayer : Protocolable
{
    Property<int> Hp { get; }       // 狀態：數值變動自動複寫到客戶端
    Value<bool> Hurt(int amount);   // RMI：客戶端呼叫、伺服器執行
}
```

> **重點**：協議的 Source Generator 只掃描**它所在的 assembly**。
> 請把協議介面宣告在**你自己的專案 assembly** 裡——在此宣告的每個協議介面都會被自動納入。

### Step 2 — 產生協議三件套（Creator + Provider + 資產）
協議需要三樣東西：觸發 Source Generator 的 `Creator`、把它包成可指派資產的 `Provider`（`ScriptableObject`），
以及 Provider 的 `.asset` 實例。最簡單的方式是用內建精靈一鍵產生：

> **選單** `Tools / PinionCore / NetSync / Create Protocol Provider...`
> （或在 Project 視窗右鍵 → `Create / PinionCore / NetSync / Protocol Provider (三件套)`）
> 填入名稱（例如 `Game`）與目標資料夾，按「建立三件套」。精靈會產生 `GameProtocolCreator.cs`、
> `GameProtocolProvider.cs`，並在編譯完成後自動建立 `GameProtocol.asset`。
> 若目標資料夾所屬的 asmdef 尚未 reference `PinionCore.NetSync`，精靈會提示並提供一鍵補上。

精靈產生的程式碼如下（也可手動照抄建立）：

```csharp
// GameProtocolCreator.cs — 觸發 Source Generator，掃描本 assembly 的協議介面
public static partial class GameProtocolCreator
{
    public static PinionCore.Remote.IProtocol Create()
    {
        PinionCore.Remote.IProtocol protocol = null;
        _Create(ref protocol);
        return protocol;
    }

    [PinionCore.Remote.Protocol.Creator]
    static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
}
```

```csharp
// GameProtocolProvider.cs — 包成可指派給 Server / Client 的資產
using UnityEngine;

[CreateAssetMenu(menuName = "PinionCore/NetSync/Game Protocol Provider", fileName = "GameProtocol")]
public class GameProtocolProvider : PinionCore.NetSync.ProtocolProvider
{
    readonly PinionCore.Remote.IProtocol _Protocol;

    public GameProtocolProvider()
    {
        _Protocol = GameProtocolCreator.Create();
    }

    public override PinionCore.Remote.IProtocol Get() => _Protocol;
}
```

> **重點**：`Creator` 與你的協議介面必須位於**同一個 assembly**——Source Generator 只掃描當前組件。

### Step 3 — 實作 Soul（伺服器）與 Ghost（客戶端）

**伺服器端**：實作 `IPlayer` 的普通 C# 類別就是權威物件（Soul）：

```csharp
using PinionCore.Remote;

public class Player : IPlayer
{
    readonly Property<int> _Hp = new Property<int>(100);

    Property<int> IPlayer.Hp => _Hp;

    // 當客戶端呼叫時，這段在伺服器上執行 (RMI)
    Value<bool> IPlayer.Hurt(int amount)
    {
        _Hp.Value -= amount;   // 變動自動複寫到所有客戶端
        return _Hp.Value > 0;  // 隱含轉型為 Value<bool>
    }
}
```

連線的生命週期由 `Server.BinderEvent`（`UnityEvent<Server.BinderCommand>`）承接：客戶端連上時以
`Status == Add` 觸發並帶著該連線的 `ISessionBinder`，在這裡用 `binder.Bind<T>()` 決定要讓這條連線
看見哪些權威物件；斷線時以 `Status == Remove` 再次觸發：

```csharp
using System.Collections.Generic;
using PinionCore.NetSync;
using UnityEngine;

public class PlayerSessions : MonoBehaviour
{
    public Server Server;

    readonly Dictionary<PinionCore.Remote.ISessionBinder, Player> _Players
        = new Dictionary<PinionCore.Remote.ISessionBinder, Player>();

    void OnEnable()  => Server.BinderEvent.AddListener(_OnBinder);
    void OnDisable() => Server.BinderEvent.RemoveListener(_OnBinder);

    void _OnBinder(Server.BinderCommand command)
    {
        if (command.Status == Server.BinderCommand.OperatorStatus.Add)
        {
            var player = new Player();
            command.Binder.Bind<IPlayer>(player); // 註冊權威物件，客戶端立即收到 Supply
            _Players.Add(command.Binder, player);
        }
        else // Remove——連線已結束，其上綁定的物件會隨連線自動釋放
        {
            _Players.Remove(command.Binder);
        }
    }
}
```

連線存活期間可以隨時對它的 binder 呼叫 `Bind<T>()` / `Unbind()`——每次 `Bind` 會在該客戶端觸發
`Supply`，每次 `Unbind` 觸發 `Unsupply`。

**客戶端**：直接向 `Client.Queryer` 查詢協議介面，訂閱 `Supply` / `Unsupply`：

```csharp
using UnityEngine;

public class PlayerGhost : MonoBehaviour
{
    public PinionCore.NetSync.Client Client;

    void Start()
    {
        Client.Queryer.QueryNotifier<IPlayer>().Supply += _OnSupply;
        Client.Queryer.QueryNotifier<IPlayer>().Unsupply += _OnUnsupply;
    }

    void _OnSupply(IPlayer player)
    {
        Debug.Log($"玩家加入，目前 HP = {player.Hp.Value}");

        var result = player.Hurt(10);                  // 呼叫 RMI → 在伺服器執行
        result.OnValue += alive => Debug.Log($"存活：{alive}");
    }

    void _OnUnsupply(IPlayer player) { }
}
```

### Step 4 — 架設伺服器場景
在一個 GameObject 上：

1. 加入 `Server` 元件，把 `GameProtocol.asset` 指派到它的 **Provider** 欄位。
2. 加入一個 Listener（例如 `Tcp.TcpListener`），建立一顆 `Tcp Connection Config` 資產
   （右鍵 → Create → `PinionCore/NetSync/Tcp Connection Config`，設定 Port），指派到 Listener 的 **Config** 欄位，
   並在初始化時呼叫 `Bind()`——或掛上 `Kits.TcpStartToBind`，Start 時自動 Bind、物件銷毀時自動關閉。
3. 加入上面的 `PlayerSessions` 元件，把 `Server` 指給它。

當客戶端連上時，`Server.BinderEvent` 會以 `Add` 帶著該連線的 binder 觸發；斷線時以 `Remove` 再次觸發。

### Step 5 — 架設客戶端場景
在一個 GameObject 上：

1. 加入 `Client` 元件，把**同一顆** `GameProtocol.asset` 指派到 **Provider** 欄位。
2. 加入對應的 Connector（例如 `Tcp.TcpConnector`），指派一顆 `Tcp Connection Config`
   （Host + Port，需與伺服器一致）到 **Config** 欄位，並在需要連線時呼叫 `Connect()`。
3. 把上面的 `PlayerGhost` 掛上，並把 `Client` 指給它。

連線建立後，伺服器 `Bind` 的物件會透過 `Supply` 事件送達；`Unbind` 時觸發 `Unsupply`。

### Step 6 — 執行
先跑伺服器場景（`Bind()`），再跑客戶端場景（`Connect()`）。
若要在單一場景內快速驗證、不開 socket，把傳輸層換成 `Standalone.Listener` / `Standalone.Connector` 即可
——注意 `Standalone.Connector.Connect(listener)` 的參數是目標 `Standalone.Listener` 而非 Config 資產
（掛 `Kits.StandaloneStartToConnect` 可在 Start 時自動接上這一對）。

### 附註 — Direct 直通模式（最速迭代）
連協議資產都不想等時，可改用 `Direct` 傳輸:客戶端物件掛 `Direct.DirectClient` + `Direct.DirectConnector`
（`DirectClient` **不需要** Provider),呼叫 `DirectConnector.Connect(server)` 直接接上目標 `Server`
（掛 `Kits.DirectStartToConnect` 可在 Start 自動接上;跨場景目標可用 `Direct.DirectServerLocator` 查找）。
`Server.BinderEvent` 與 `Queryer.QueryNotifier<T>()` 的用法完全不變,但 Supply 取得的 ghost
**就是伺服器端 Soul 實例本身**（零序列化、共用參考,方法呼叫是同步 .NET 呼叫）。

注意:
- Direct 模式**不驗證可序列化性**——在此能跑的協議介面不代表可遠端化,上線前仍須以 Standalone/TCP 整合測試。
- `Server.Provider` 仍必須指派(Server 啟動時無條件建立序列化引擎)。
- `Ping` 恆為 0,`VersionCodeError` / `ErrorMethod` / `Exception` 事件永不觸發,依賴這些訊號的斷線偵測在此模式下無效。

---

## 架構一覽

```
Runtime/Scripts/
├── Links/                 連線與傳輸層
│   ├── Server / Client    PinionCore.Remote 的 Unity 進入點 (MonoBehaviour)
│   ├── ProtocolProvider   協議來源抽象 (ScriptableObject)
│   ├── ConnectionConfig   連線端點抽象 (ScriptableObject)
│   ├── Direct/            零序列化直通 (DirectClient, DirectConnector, DirectServerLocator)
│   ├── Standalone/        行程內回送傳輸
│   ├── Tcp/               TCP 傳輸 + TcpConnectionConfig
│   ├── Web/               WebSocket 傳輸 + WebConnectionConfig
│   ├── Gateway/           分散式路由閘道 (GatewayRouter, GatewayRegistry, GatewayClient)
│   └── Kit/               自動接線輔助元件 (TcpStartToBind, WebStartToBind, Standalone kits)
└── Console/               遊戲內執行時主控台 (ConsoleView, ClientConsole)
```

- **Soul**：伺服器端權威物件（實作協議介面的普通 C# 物件），執行真正的遊戲邏輯，用 `ISessionBinder.Bind<T>()` 註冊。
- **Ghost**：客戶端代理物件，反映伺服器狀態，透過 `Queryer.QueryNotifier<T>()` 取得。
- **Server.BinderEvent**：連線生命週期的進入點——每條連線上線／斷線時以 `Add` / `Remove`
  帶著該連線的 `ISessionBinder` 觸發。
- **Kits**（`PinionCore.NetSync.Kits`）：消除啟動樣板的小型 MonoBehaviour——
  `TcpStartToBind` / `WebStartToBind` / `StandaloneStartToBind` 在 Start 呼叫 `Bind()`、銷毀時關閉 Listener；
  `StandaloneStartToConnect` 在 Start 把 `Standalone.Connector` 接上它的 `Listener`。

---

## Gateway（分散式路由閘道）

單一 `Server` 不敷使用時，可改用 Gateway 三層架構：客戶端只需**一條連線**連上中央 Router，
就能同時與多個遊戲服務通訊。這是
[PinionCore.Remote.Gateway](https://github.com/jiowchern/PinionCore.Remote) 的 Unity 封裝，
元件位於 `PinionCore.NetSync.Gateways` 命名空間。

| 角色 | 元件 | 職責 |
|------|------|------|
| **Router（路由）** | `GatewayRouter` + `GatewayRouterEndpoint` | 中央路由器。`Registry` 端點接受遊戲服務註冊、`Session` 端點接受客戶端連線，依 **Group** 與**協議版本**自動路由（同 Group 負載平衡、不同 Group 全部連上、版本不符互相隔離）。事件驅動，不需 Update。 |
| **Registry（服務註冊）** | `GatewayRegistry` | 與 `Server` 掛在同一個 GameObject。向 Router 註冊自己的 Group，並把 Router 轉送來的玩家連線自動餵給 `Server`。 |
| **Client（客戶端）** | `GatewayClient` | 取代 `Client`。連上 Router 的 Session 端點後，經路由同時取得多個服務的代理物件；用法與 `Client` 相同（`Queryer.QueryNotifier<T>()`）。 |

既有的傳輸層元件**全部可以直接複用**：

- Listener（`Tcp.TcpListener` / `Web.WebListener` / `Standalone.Listener`）可掛向任何 `IListenableHost` —— `Server` 或 `GatewayRouterEndpoint`。
- Connector（`Tcp.TcpConnector` / `Web.WebConnector` / `Standalone.Connector`）可連向任何 `IConnectableAgent` —— `Client`、`GatewayClient` 或 `GatewayRegistry`。

### 一鍵生成（建議入口）

Gateway 不需要撰寫任何程式碼，直接在 **Hierarchy 視窗右鍵**一鍵生成接好線的物件：

> **GameObject → PinionCore → NetSync →**
> - **Gateway Router** —— `GatewayRouter` + Registry / Session 兩個端點子物件
>   （各含 `GatewayRouterEndpoint` + `TcpListener` + 自動 Bind 的 Kit）
> - **Gateway Service (Server + Registry)** —— `Server` + `GatewayRegistry` + `TcpConnector`
> - **Gateway Client (TCP / WebSocket / Standalone)** —— `GatewayClient` + 對應 Connector
>
> 生成後只需：指派協議資產、指派 Listener / Connector 的 Config
> （Standalone 則把 Connector 的 Listener 指向 Router Session 端點的 `Standalone.Listener`），即可運作。
> 專案中若只有**一顆** ProtocolProvider 資產，生成時會自動指派。

### 架設 Router

Router 的監聽架構與 `Server` 完全一致：`GatewayRouterEndpoint` 就是一個 `IListenableHost`，
掛上任一傳輸層的 Listener（`Tcp.TcpListener` / `Web.WebListener` / `Standalone.Listener`）即可：

1. 建立 GameObject，加入 `GatewayRouter`。
2. 建立兩個**子物件**，各加入 `GatewayRouterEndpoint`，`Endpoint` 分別設為 **Registry** 與 **Session**
   （未指派 `Router` 欄位時會自動往父物件尋找）。
3. 在兩個子物件上各加入 Listener + Config 資產，並呼叫 `Bind()`
   （或掛 `Kits.TcpStartToBind` 等 Kit 讓 Start 自動 Bind）。

```
GatewayRouter (GameObject)
├── RegistryEndpoint：GatewayRouterEndpoint(Registry) + TcpListener(Port 20001)
└── SessionEndpoint： GatewayRouterEndpoint(Session)  + TcpListener(Port 20002) + WebListener(Port 20003,供 WebGL)
```

同一個端點可**並掛多個 Listener**（例如 Session 同時開 TCP 與 WebSocket），
也可全部換成 `Standalone.Listener` 做單場景測試。

### 架設遊戲服務（向 Router 註冊）

在同一個 GameObject 上：

1. `Server` —— 指派協議資產，`BinderEvent` 的連線處理照常使用。**不需要**再掛對外的 Listener，玩家連線由 Router 轉送進來。
2. `GatewayRegistry` —— 指派**同一顆**協議資產、設定 `Group`。
3. Connector（例如 `Tcp.TcpConnector`，Config 指向 Router 的 **Registry** 端點）—— 呼叫 `Connect()` 完成註冊。

`Group` 的意義：**相同 Group** 的多個服務視為同類，Router 以輪詢做負載平衡；
**不同 Group** 視為不同服務（例如大廳、戰鬥、聊天），客戶端會同時連上每個 Group。

### 架設客戶端

在同一個 GameObject 上：

1. `GatewayClient` —— 指派**同一顆**協議資產。
2. Connector（例如 WebGL 用 `Web.WebConnector`，Config 指向 Router 的 **Session** 端點）—— 呼叫 `Connect()`。

```csharp
// 與 Client 相同的查詢方式
gatewayClient.Queryer.QueryNotifier<IPlayer>().Supply += player => { /* ... */ };
```

### 注意事項

- 協議的 `VersionCode` 是 Router 路由的隔離依據：`GatewayRegistry` 與 `GatewayClient` 必須使用**同一份協議**，
  否則互相看不見。這也讓新舊版本服務可以同時掛在同一個 Router 上逐步升級。
- Router 本身不需要協議資產，可獨立佈署於任何 Unity 執行個體（headless 亦可）。
- 完整流程可參考套件測試 `Tests/GatewayTests.cs`：包含 Standalone 傳輸與真實 TCP 連線
  兩種端對端測試，皆跑通 Router → Registry 註冊 → 客戶端連線 → 跨路由 RMI。

---

## 執行時客戶端主控台

`PinionCore.NetSync.Consoles` 命名空間提供遊戲內主控台（IMGUI）——不寫任何 UI 就能驅動客戶端：
連線、查看狀態，甚至**從命令列直接呼叫協議方法、讀取同步屬性**。在 GameObject 上加兩個元件：

1. **`ConsoleView`** —— 畫面上的主控台視窗。欄位：`Title`、`MaxLineCount`、`ShowPinionCoreLog`
   （把 `PinionCore.Utility.Log` 的訊息鏡射到視窗）、`Visible`、`WindowId`（同場景多個視窗時需錯開）。
   支援已註冊指令名稱的 **Tab 補完**。
2. **`ClientConsole`** —— 把客戶端接上視窗。指派 `View`、`Protocol`（ProtocolProvider 資產）與
   `QueryerHost`（任何 `IQueryerHost`——`Client` 或 `Gateways.GatewayClient`）。
   選填指派同物件上的 Connector（`TcpConnector`、`WebConnector` 或 `StandaloneConnector`）即可解鎖連線指令。

內建指令（依指派的 Connector 註冊）：

| 指令 | 需要 | 效果 |
|------|------|------|
| `ping` / `hash` | — | 顯示目前 Ping ／ 協議版本雜湊 |
| `connect <ip> <port>` / `connect-config` | `TcpConnector` | 以位址或指派的 Config 資產做 TCP 連線 |
| `connect-web <url>` / `connect-web-config` | `WebConnector` | WebSocket 連線（WebGL 請用這組——瀏覽器不支援 TCP） |
| `connect-standalone` | `StandaloneConnector` | 連上指派的 `StandaloneListener`，或依 `StandaloneSceneName` / `StandaloneObjectName` 查找 |
| `disconnect` / `status` | 任一 Connector | 斷線 ／ 顯示連線狀態 |

除此之外，`ClientConsole` 會監看**協議中的所有介面**：Ghost 送達時自動註冊「`介面名.成員名`」指令，
可互動式呼叫遠端方法、讀取同步屬性——在客戶端 UI 還沒做出來之前就能對伺服器做煙霧測試。

---

## 範例

透過 Unity Package Manager 匯入內附範例：

- **Sample 1**：Standalone / TCP / WebSocket 三種傳輸層的基本連線。
- **Sample 2 – Chat**：聊天室應用，示範以介面定義的登入、玩家與聊天協議。

線上體驗 Sample 2：<https://proxy.pinioncore.dpdns.org/sample2>

---

## 授權

本套件採用 [MIT License](./LICENSE)。
