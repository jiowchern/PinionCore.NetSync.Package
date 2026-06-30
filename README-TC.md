# PinionCore NetSync

> 為 Unity 打造的權威式狀態同步套件 — 用「寫本機程式」的方式寫網路邏輯。

PinionCore NetSync 建立在 [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) 的 RMI 框架之上，
把底層的串流、序列化與封包處理全部封裝起來，只對外暴露幾個 Unity 元件與 ScriptableObject 資產。
你在伺服器端寫的是**權威物件（Soul）的普通 C# 邏輯**，客戶端則自動取得對應的**代理物件（Ghost）**，
中間不需要手寫任何封包、序列化或 RPC 派送程式碼。

- **語言 / Language**：[English](./README-EN.md) ｜ 繁體中文
- **Unity**：2022.2 以上（開發於 Unity 6000.2）
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
封包、序列化、連線管理全部由套件處理。物件的生成與銷毀也由 `SoulProvider` / `GhostProvider` 依事件自動完成。

### 4. 傳輸層可隨意抽換，同一份遊戲程式碼通吃
| 傳輸層 | 元件 | 適用場景 |
|--------|------|----------|
| **Standalone** | `Standalone.Connector` / `Standalone.Listener` | 行程內回送：編輯器內測試、單機模擬、單元測試，**完全不開 socket** |
| **TCP** | `Tcp.TcpConnector` / `Tcp.TcpListener` | 可靠、有序的預設傳輸 |
| **WebSocket** | `Web.WebConnector` / `Web.WebListener` | WebGL 平台、需穿越防火牆 |

切換傳輸層只要換一個元件，遊戲邏輯一行都不用改。

### 5. 反應式物件生命週期，不需手寫 spawn 訊息
伺服器產生／銷毀一個權威物件時，客戶端會透過 `INotifier<T>` 的 `Supply` / `Unsupply` 事件
**自動實例化或銷毀**對應的 Ghost。物件進出視野、玩家上下線，全部事件驅動。

### 6. 設定即資產（ScriptableObject）
協議（`ProtocolProvider`）與連線端點（`ConnectionConfig`）都是可共用、可序列化的 `.asset`：
- 同一顆協議資產同時指派給 Server 與 Client → 兩端必定使用同一份協議版本。
- 同一顆連線資產同時指派給 Listener 與 Connector → 連接埠不會對不上。
- 端點、協議的調整不必動程式碼、不必重新編譯。

### 7. 內建執行時診斷
`Server`、`Client`、各 Listener / Connector 的 Inspector 直接顯示協議雜湊、Ping、收送位元組數，
方便確認兩端協議是否一致、連線是否正常。

---

## 快速上手

以下用一個自訂的玩家物件 `IPlayer`（一個會同步的血量 + 一個遠端方法）走完整個流程。

### Step 0 — 安裝
在 Unity 專案的 `Packages/manifest.json` 加入相依：

```jsonc
{
  "dependencies": {
    // Git 引用
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.git?path=PinionCore.NetSync.Package"
    // 或本機路徑引用：
    // "com.pinioncore.netsync": "file:../PinionCore.NetSync.Package"
  }
}
```

### Step 1 — 用介面定義你的協議
協議只是一個繼承 `IObject` 的 C# 介面。用 `Property<T>` 表示要同步的狀態，
用回傳 `Value<T>` 的方法表示客戶端可呼叫、伺服器執行的 RMI：

```csharp
using PinionCore.NetSync.Syncs.Protocols; // IObject
using PinionCore.Remote;

public interface IPlayer : IObject
{
    Property<int> Hp { get; }       // 狀態：數值變動自動複寫到客戶端
    Value<bool> Hurt(int amount);   // RMI：客戶端呼叫、伺服器執行
}
```

> **重點**：協議的 Source Generator 只掃描**它所在的 assembly**。
> 請把協議介面宣告在**你自己的專案 assembly** 裡——在此宣告的每個協議介面都會被自動納入，
> 它們所繼承的 `IObject` 也會一併納入。

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

**伺服器端**：一個實作 `IPlayer` 的 `MonoBehaviour`，在 `Start()` 用 `gameObject.Bind<IPlayer>(this)`
註冊為權威物件（`SoulFinder` 擴充方法）：

```csharp
using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.NetSync.Syncs.Souls; // SoulFinder
using PinionCore.Remote;
using UnityEngine;

public class PlayerSoul : MonoBehaviour, IPlayer
{
    readonly Property<int> _Hp = new Property<int>();

    Property<int> IPlayer.Hp => _Hp;
    Property<int> IObject.Id => new Property<int>(gameObject.GetInstanceID());

    ISoul _Soul;

    void Start()
    {
        _Hp.Value = 100;
        _Soul = gameObject.Bind<IPlayer>(this); // 註冊權威物件
    }

    // 當客戶端呼叫時，這段在伺服器上執行 (RMI)
    Value<bool> IPlayer.Hurt(int amount)
    {
        _Hp.Value -= amount;   // 變動自動複寫到所有客戶端
        return _Hp.Value > 0;  // 隱含轉型為 Value<bool>
    }

    void OnDestroy() => gameObject.Unbind(_Soul);
}
```

**客戶端**：繼承 `GhostMonoBehaviour<IPlayer>`，套件會自動在代理物件供應／移除時回呼，
你只要讀取屬性或呼叫方法：

```csharp
using PinionCore.NetSync.Syncs.Ghosts; // GhostMonoBehaviour
using UnityEngine;

public class PlayerGhost : GhostMonoBehaviour<IPlayer>
{
    protected override void _OnSupply(IPlayer player)
    {
        Debug.Log($"玩家加入，目前 HP = {player.Hp.Value}");

        var result = player.Hurt(10);                  // 呼叫 RMI → 在伺服器執行
        result.OnValue += alive => Debug.Log($"存活：{alive}");
    }

    protected override void _OnUnsupply(IPlayer player) { }
}
```

把 `PlayerSoul` 與 `Soul` 一起放進 **Soul 預置物**；把 `PlayerGhost` 與 `Ghost` 一起放進 **Ghost 預置物**。

### Step 4 — 架設伺服器場景
在一個 GameObject 上：

1. 加入 `Server` 元件，把 `GameProtocol.asset` 指派到它的 **Provider** 欄位。
2. 加入一個 Listener（例如 `Tcp.TcpListener`），建立一顆 `Tcp Connection Config` 資產
   （右鍵 → Create → `PinionCore/NetSync/Tcp Connection Config`，設定 Port），指派到 Listener 的 **Config** 欄位，
   並在初始化時呼叫 `Bind()`。
3. 加入 `Syncs.Souls.SoulProvider`，把 `Server` 指給它，並指定上面的 **Soul 預置物**。

當客戶端連上時，`SoulProvider` 會自動為該連線實例化一份 Soul 預置物；斷線時自動銷毀。

### Step 5 — 架設客戶端場景
在一個 GameObject 上：

1. 加入 `Client` 元件，把**同一顆** `GameProtocol.asset` 指派到 **Provider** 欄位。
2. 加入對應的 Connector（例如 `Tcp.TcpConnector`），指派一顆 `Tcp Connection Config`
   （Host + Port，需與伺服器一致）到 **Config** 欄位，並在需要連線時呼叫 `Connect()`。
3. 加入 `Syncs.Ghosts.GhostProvider`，把 `Client` 指給它，並指定上面的 **Ghost 預置物**。

伺服器送來物件時，`GhostProvider` 會自動實例化 Ghost 預置物；物件移除時自動銷毀。

### Step 6 — 執行
先跑伺服器場景（`Bind()`），再跑客戶端場景（`Connect()`）。
若要在單一場景內快速驗證、不開 socket，把傳輸層換成 `Standalone.Listener` / `Standalone.Connector` 即可。

> 想直接取得代理物件而不繼承 `GhostMonoBehaviour<T>`？可用 `gameObject.Query<IPlayer>()`
> （`GhostFinder` 擴充方法）拿到 `INotifier<IPlayer>`，再自行訂閱 `Supply` / `Unsupply`。

---

## 架構一覽

```
Runtime/Scripts/
├── Links/                 連線與傳輸層
│   ├── Server / Client    PinionCore.Remote 的 Unity 進入點 (MonoBehaviour)
│   ├── ProtocolProvider   協議來源抽象 (ScriptableObject)
│   ├── ConnectionConfig   連線端點抽象 (ScriptableObject)
│   ├── Standalone/        行程內回送傳輸
│   ├── Tcp/               TCP 傳輸 + TcpConnectionConfig
│   └── Web/               WebSocket 傳輸 + WebConnectionConfig
└── Syncs/                 Soul–Ghost 同步系統
    ├── Protocols/         IObject 等協議介面
    ├── Souls/             伺服器端權威物件 (Soul, SoulProvider)
    └── Ghosts/            客戶端代理物件 (Ghost, GhostMonoBehaviour, GhostProvider)
```

- **Soul**：伺服器端權威物件，執行真正的遊戲邏輯。
- **Ghost**：客戶端代理物件，反映伺服器狀態。
- **SoulProvider / GhostProvider**：依連線與物件事件，自動實例化／銷毀 Soul / Ghost 預置物。
- **擴充方法**：`gameObject.Bind<T>()`、`gameObject.Unbind()`、`gameObject.Query<T>()`。

---

## 範例

透過 Unity Package Manager 匯入內附範例：

- **Sample 1**：Standalone / TCP / WebSocket 三種傳輸層的基本連線。
- **Sample 2 – Chat**：聊天室應用，示範以介面定義的登入、玩家與聊天協議。

線上體驗 Sample 2：<https://proxy.pinioncore.dpdns.org/sample2>

---

## 授權

本套件採用 [MIT License](./LICENSE)。
