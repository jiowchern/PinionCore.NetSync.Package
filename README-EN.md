# PinionCore NetSync

> An authoritative state-replication package for Unity — write networked logic as if it were local.

PinionCore NetSync is built on top of the [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) RMI
framework. It wraps the low-level stream, serialization, and packet plumbing and exposes just a few Unity
components and ScriptableObject assets. On the server you write **plain C# logic on authoritative objects (Souls)**;
clients automatically receive matching **proxy objects (Ghosts)** — with no hand-written packets, serialization,
or RPC dispatch in between.

- **Language**：English ｜ [繁體中文](./README-TC.md)
- **Unity**：6000.4 or newer (developed on Unity 6000.2)
- **Platforms**：Standalone, WebGL (WebGL uses WebSocket)

---

## Why NetSync

### 1. Write networked logic as if it were local (Soul–Ghost + RMI)
An authoritative object on the server (**Soul**) is just a normal C# class — your game logic and its state live
together. Clients automatically receive a matching proxy (**Ghost**); reading its properties and calling its
methods feels exactly like working with a local object. You **never** define message formats, write
`if (msgType == ...)` dispatch code, or handle serialization yourself.

### 2. An interface *is* the protocol — type-safe at compile time
Define your protocol with an ordinary **C# interface**; a Source Generator emits the matching wire protocol at
compile time.
- Declare state as `Property<T>` → value changes replicate to every client automatically.
- Methods returning `Value<T>` → become network-wide Remote Method Invocations.
- Every protocol carries a **VersionCode**: server and client compare it during the handshake, so a mismatched
  protocol is rejected immediately instead of failing mysteriously mid-session.

### 3. Focus on game logic, not networking boilerplate
You only do three things: **(1)** define *what* to sync with an interface, **(2)** set the state on the Soul side,
**(3)** read it on the Ghost side. Packets, serialization, and connection management are handled for you, and
object creation/destruction is driven automatically by `SoulProvider` / `GhostProvider`.

### 4. Swap the transport freely; the same game code runs everywhere
| Transport | Components | Best for |
|-----------|-----------|----------|
| **Standalone** | `Standalone.Connector` / `Standalone.Listener` | In-process loopback: in-editor testing, single-player simulation, unit tests — **no sockets at all** |
| **TCP** | `Tcp.TcpConnector` / `Tcp.TcpListener` | Reliable, ordered default transport |
| **WebSocket** | `Web.WebConnector` / `Web.WebListener` | WebGL builds, firewall traversal |

Switching transport means swapping one component — not a single line of game logic changes.

### 5. Reactive object lifecycle — no manual spawn messages
When the server creates or destroys an authoritative object, clients **instantiate or destroy** the matching Ghost
automatically through `INotifier<T>`'s `Supply` / `Unsupply` events. Objects entering/leaving view, players
joining/leaving — all event-driven.

### 6. Configuration as assets (ScriptableObject)
Both the protocol (`ProtocolProvider`) and the connection endpoint (`ConnectionConfig`) are shareable,
serializable `.asset` files:
- Assign the same protocol asset to both Server and Client → they are guaranteed to use the same protocol version.
- Assign the same connection asset to a Listener and a Connector → ports can't drift out of sync.
- Tweaking endpoints or protocols needs no code change and no recompile.

### 7. Built-in runtime diagnostics
The Inspectors for `Server`, `Client`, and each Listener / Connector surface the protocol hash, ping, and
bytes sent/received — handy for confirming both ends agree on the protocol and the link is healthy.

---

## Quick Start

The walkthrough below uses a custom player object `IPlayer` (one replicated health value + one remote method).

### Step 0 — Install
Add the dependency to your Unity project's `Packages/manifest.json`:

```jsonc
{
  "dependencies": {
    // Git reference
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.git?path=PinionCore.NetSync.Package"
    // or a local path reference:
    // "com.pinioncore.netsync": "file:../PinionCore.NetSync.Package"
  }
}
```

### Step 1 — Define your protocol with an interface
A protocol is just a C# interface that inherits `IObject`. Use `Property<T>` for state to replicate, and methods
returning `Value<T>` for RMIs the client invokes and the server executes:

```csharp
using PinionCore.NetSync.Syncs.Protocols; // IObject
using PinionCore.Remote;

public interface IPlayer : IObject
{
    Property<int> Hp { get; }       // state: changes replicate to clients automatically
    Value<bool> Hurt(int amount);   // RMI: client calls, server executes
}
```

> **Important**: the protocol Source Generator only scans the **assembly it lives in**. Declare your protocol
> interfaces in **your own project assembly** — every protocol interface declared there is included automatically,
> along with the `IObject` they inherit.

### Step 2 — Generate the protocol trio (Creator + Provider + asset)
A protocol needs three things: a `Creator` that triggers the Source Generator, a `Provider` (`ScriptableObject`)
that wraps it into an assignable asset, and an `.asset` instance of that Provider. The easiest way is the built-in
wizard:

> **Menu** `Tools / PinionCore / NetSync / Create Protocol Provider...`
> (or right-click in the Project window → `Create / PinionCore / NetSync / Protocol Provider (三件套)`)
> Enter a name (e.g. `Game`) and a target folder, then click "建立三件套". The wizard generates
> `GameProtocolCreator.cs` and `GameProtocolProvider.cs`, and after the recompile finishes it creates
> `GameProtocol.asset` automatically. If the target folder's asmdef doesn't yet reference `PinionCore.NetSync`,
> the wizard warns you and offers to add the reference with one click.

The generated code looks like this (you can also write it by hand):

```csharp
// GameProtocolCreator.cs — triggers the Source Generator over this assembly's interfaces
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
// GameProtocolProvider.cs — wraps the protocol into an asset assignable to Server / Client
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

> **Important**: the `Creator` and your protocol interfaces must live in the **same assembly** — the Source
> Generator only scans the current assembly.

### Step 3 — Implement the Soul (server) and Ghost (client)

**Server side**: a `MonoBehaviour` that implements `IPlayer` and registers itself as an authoritative object in
`Start()` with `gameObject.Bind<IPlayer>(this)` (the `SoulFinder` extension):

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
        _Soul = gameObject.Bind<IPlayer>(this); // register the authoritative object
    }

    // Runs on the server when a client invokes it (RMI)
    Value<bool> IPlayer.Hurt(int amount)
    {
        _Hp.Value -= amount;   // the change replicates to every client automatically
        return _Hp.Value > 0;  // implicitly wrapped into Value<bool>
    }

    void OnDestroy() => gameObject.Unbind(_Soul);
}
```

**Client side**: derive from `GhostMonoBehaviour<IPlayer>`; the package calls you back when the proxy is supplied or
removed, and you just read properties or call methods:

```csharp
using PinionCore.NetSync.Syncs.Ghosts; // GhostMonoBehaviour
using UnityEngine;

public class PlayerGhost : GhostMonoBehaviour<IPlayer>
{
    protected override void _OnSupply(IPlayer player)
    {
        Debug.Log($"player joined with HP = {player.Hp.Value}");

        var result = player.Hurt(10);                  // RMI → executes on the server
        result.OnValue += alive => Debug.Log($"still alive: {alive}");
    }

    protected override void _OnUnsupply(IPlayer player) { }
}
```

Put `PlayerSoul` together with `Soul` on the **Soul prefab**; put `PlayerGhost` together with `Ghost` on the
**Ghost prefab**.

### Step 4 — Set up the server scene
On a GameObject:

1. Add the `Server` component and assign `GameProtocol.asset` to its **Provider** field.
2. Add a listener (e.g. `Tcp.TcpListener`), create a `Tcp Connection Config` asset
   (Create → `PinionCore/NetSync/Tcp Connection Config`, set the Port), assign it to the listener's **Config**
   field, and call `Bind()` during initialization.
3. Add `Syncs.Souls.SoulProvider`, point its `Server` field at the server, and assign the **Soul prefab** above.

When a client connects, `SoulProvider` automatically instantiates one Soul prefab for that session, and destroys
it on disconnect.

### Step 5 — Set up the client scene
On a GameObject:

1. Add the `Client` component and assign the **same** `GameProtocol.asset` to its **Provider** field.
2. Add the matching connector (e.g. `Tcp.TcpConnector`), assign a `Tcp Connection Config` (Host + Port matching the
   server) to its **Config** field, and call `Connect()` when you want to connect.
3. Add `Syncs.Ghosts.GhostProvider`, point its `Client` field at the client, and assign the **Ghost prefab** above.

When the server supplies an object, `GhostProvider` instantiates the Ghost prefab automatically and destroys it on
removal.

### Step 6 — Run
Start the server scene first (`Bind()`), then the client scene (`Connect()`). To verify everything in a single
scene without opening sockets, swap the transport for `Standalone.Listener` / `Standalone.Connector`.

> Prefer to grab the proxy without deriving from `GhostMonoBehaviour<T>`? Use `gameObject.Query<IPlayer>()`
> (the `GhostFinder` extension) to get an `INotifier<IPlayer>` and subscribe to `Supply` / `Unsupply` yourself.

---

## Architecture at a glance

```
Runtime/Scripts/
├── Links/                 Connection & transport
│   ├── Server / Client    Unity entry points for PinionCore.Remote (MonoBehaviour)
│   ├── ProtocolProvider   Protocol-source abstraction (ScriptableObject)
│   ├── ConnectionConfig   Endpoint abstraction (ScriptableObject)
│   ├── Standalone/        In-process loopback transport
│   ├── Tcp/               TCP transport + TcpConnectionConfig
│   └── Web/               WebSocket transport + WebConnectionConfig
└── Syncs/                 Soul–Ghost synchronization
    ├── Protocols/         Protocol interfaces such as IObject
    ├── Souls/             Server-side authoritative objects (Soul, SoulProvider)
    └── Ghosts/            Client-side proxy objects (Ghost, GhostMonoBehaviour, GhostProvider)
```

- **Soul**: the server-side authoritative object that runs the real game logic.
- **Ghost**: the client-side proxy that reflects server state.
- **SoulProvider / GhostProvider**: instantiate/destroy Soul / Ghost prefabs in response to session and object events.
- **Extension methods**: `gameObject.Bind<T>()`, `gameObject.Unbind()`, `gameObject.Query<T>()`.

---

## Samples

Import the bundled samples from the Unity Package Manager:

- **Sample 1**: basic connection across the Standalone / TCP / WebSocket transports.
- **Sample 2 – Chat**: a chat application demonstrating interface-defined login, player, and chat protocols.

Try Sample 2 online: <https://proxy.pinioncore.dpdns.org/sample2>

---

## License

This package is released under the [MIT License](./LICENSE).
