# PinionCore.NetSync

> Unity network synchronization package based on [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) framework. Implements Remote Method Invocation (RMI) with Soul-Ghost architecture for client-server networking.

[![Unity Version](https://img.shields.io/badge/Unity-2022.2%2B-blue)](https://unity.com/)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-purple)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

**Language**: [English](README-EN.md) | [繁體中文](README-TC.md)

---

## 🎮 Online Demo

Experience PinionCore.NetSync real-time multiplayer chat:

### **👉 [https://proxy.pinioncore.dpdns.org/sample2](https://proxy.pinioncore.dpdns.org/sample2)**

**Demo Features**:
- ✅ WebGL WebSocket connection
- ✅ Real-time multiplayer synchronization
- ✅ Remote Method Invocation (RMI)
- ✅ Soul-Ghost network architecture

---

## Features

### 🏗️ Soul-Ghost Architecture
- **Soul (Server-side)**: Authoritative game state and logic
- **Ghost (Client-side)**: Receives and displays server state
- **Auto Binding**: Automatic Soul-Ghost pairing via `IObject` interface
- **Position Compression**: Tracker System reduces bandwidth with trajectory interpolation

### 🌐 Multiple Transport Layers
- **TCP**: Reliable, ordered delivery (default)
- **WebSocket**: Web-compatible, firewall-friendly (WebGL support)
- **Standalone**: Local in-memory transport (testing/single-player)

### 🚀 Modern Development Experience
- **C# Source Generators**: Auto-generate network protocol code
- **Unity MonoBehaviour Integration**: Drag-and-drop Server/Client components
- **StatusMachine Pattern**: Event-driven state management
- **Gateway Mode**: Distributed architecture with load balancing support

### 🎯 Easy to Extend
- **Protocol Interfaces**: Define shared RMI interfaces
- **Transport Abstraction**: Easily extend custom network protocols
- **Modular Design**: Clear Soul/Ghost/Links layered architecture

---

## Installation

### Unity Package Manager (Recommended)

#### Method 1: Latest Version
1. Open Unity Editor
2. Go to `Window > Package Manager`
3. Click **`+`** → **`Add package from git URL...`**
4. Enter:
   ```
   https://github.com/jiowchern/PinionCore.NetSync.Package.git
   ```
5. Click **`Add`**

#### Method 2: Specific Version (Stable)
Install a specific version using version tag:
```
https://github.com/jiowchern/PinionCore.NetSync.Package.git#v0.0.1
```

### Via manifest.json

Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.Package.git#v0.0.1"
  }
}
```

### Available Versions

- `v0.0.1` - Initial release
- Latest: `main` branch (development)

---

## Quick Start

### 1️⃣ Server Setup

```csharp
using PinionCore.NetSync;
using PinionCore.NetSync.Syncs.Souls;

public class GameServer : MonoBehaviour
{
    [SerializeField] GameObject soulPrefab;

    void Start()
    {
        // Add Server component
        var server = gameObject.AddComponent<Server>();

        // Add TCP listener
        var listener = gameObject.AddComponent<Tcp.Listener>();
        listener.Port = 7777;

        // Handle client connections
        server.BinderEvent.AddListener((command) =>
        {
            if (command.Status == Server.BinderCommand.OperatorStatus.Add)
            {
                // Client connected: instantiate Soul and bind
                var soul = Instantiate(soulPrefab).GetComponent<Soul>();
                command.Binder.Bind<IObject>(soul);
            }
            else
            {
                // Client disconnected: cleanup resources
            }
        });
    }
}
```

### 2️⃣ Client Setup

```csharp
using PinionCore.NetSync;
using PinionCore.NetSync.Syncs.Ghosts;

public class GameClient : MonoBehaviour
{
    [SerializeField] GameObject ghostPrefab;

    void Start()
    {
        // Add Client component
        var client = gameObject.AddComponent<Client>();

        // Add TCP connector
        var connector = gameObject.AddComponent<Tcp.Connector>();
        connector.Host = "127.0.0.1";
        connector.Port = 7777;

        // Listen for remote objects
        client.Queryer.QueryNotifier<IObject>().Supply += (obj) =>
        {
            // Server sent object: instantiate Ghost and bind
            var ghost = Instantiate(ghostPrefab).GetComponent<Ghost>();
            ghost.Bind(obj);
        };
    }
}
```

### 3️⃣ Create Soul Class (Server-side)

```csharp
using PinionCore.NetSync.Syncs.Souls;

public class PlayerSoul : Soul
{
    void Start()
    {
        // Server-side logic (authoritative state)
        // Automatically synchronized to all connected clients
    }

    void Update()
    {
        // Handle player movement, game logic, etc.
    }
}
```

### 4️⃣ Create Ghost Class (Client-side)

```csharp
using PinionCore.NetSync.Syncs.Ghosts;

public class PlayerGhost : Ghost
{
    void Update()
    {
        // Receive and render server state
        // TrackerReceiver automatically handles position interpolation
    }
}
```

---

## Soul-Ghost Architecture

### Core Concepts

| Component | Location | Responsibility | Features |
|-----------|----------|----------------|----------|
| **Soul** | Server | Execute game logic, maintain authoritative state | Auto-send Transform changes to all clients |
| **Ghost** | Client | Receive and render server state | TrackerReceiver handles position interpolation |
| **IObject** | Shared | Network object protocol interface | Bridge between Soul and Ghost |
| **Tracker** | System | Position compression and trajectory interpolation | Reduce bandwidth (ZipTracker) |

### Workflow

```
Server                          Network                         Client
  │                               │                               │
  ├─ Instantiate Soul             │                               │
  ├─ Bind<IObject>(soul) ────────>│                               │
  │                               ├─ Send IObject Instance ──────>│
  │                               │                               ├─ Instantiate Ghost
  │                               │                               ├─ Bind(IObject)
  │                               │                               │
  ├─ Transform Updates ──────────>│                               │
  │   (TrackerSender)             ├─ Compressed Data ────────────>│
  │                               │                               ├─ Interpolate & Render
  │                               │                               │   (TrackerReceiver)
```

### Key Benefits

1. **Server Authority**: Game logic runs on server, preventing cheating
2. **Automatic Synchronization**: Transform changes auto-sync to all clients
3. **Bandwidth Optimization**: Tracker system compresses position data
4. **Smooth Movement**: Client-side interpolation for smooth rendering

---

## Transport Layers

| Transport | Components | Use Case | Platform Support |
|-----------|-----------|----------|------------------|
| **TCP** | `Tcp.Listener`<br>`Tcp.Connector` | Reliable, ordered delivery | Standalone, Editor |
| **WebSocket** | `Web.Listener`<br>Browser built-in | WebGL platform, firewall-friendly | WebGL, Standalone |
| **Standalone** | `Standalone.Listener`<br>`Standalone.Connector` | Local simulation, unit testing | All Platforms |

### Platform Selection

```csharp
if (Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor)
{
    // WebGL platform uses WebSocket
    var state = new WebSocketState(endpoint);
}
else
{
    // Other platforms use TCP
    var state = new TcpSocketState(endpoint);
}
```

### Transport Layer Details

#### TCP Transport
- **Best for**: PC/Console games, dedicated servers
- **Features**: Reliable, ordered, connection-oriented
- **Usage**:
  ```csharp
  // Server
  var listener = gameObject.AddComponent<Tcp.Listener>();
  listener.Port = 7777;

  // Client
  var connector = gameObject.AddComponent<Tcp.Connector>();
  connector.Host = "127.0.0.1";
  connector.Port = 7777;
  ```

#### WebSocket Transport
- **Best for**: WebGL builds, browser-based games
- **Features**: Web-compatible, HTTP-based, firewall-friendly
- **Usage**:
  ```csharp
  // Server
  var listener = gameObject.AddComponent<Web.Listener>();
  listener.Port = 8080;

  // Client (WebGL uses browser WebSocket API)
  var state = new WebSocketState("ws://localhost:8080");
  ```

#### Standalone Transport
- **Best for**: Local testing, single-player mode, unit tests
- **Features**: In-memory, no network required, instant
- **Usage**:
  ```csharp
  // Server
  var listener = gameObject.AddComponent<Standalone.Listener>();

  // Client
  var connector = gameObject.AddComponent<Standalone.Connector>();
  ```

---

## Advanced Features

### Gateway Mode

For distributed architecture and load balancing:

```csharp
using PinionCore.Remote.Gateway;

IAgent agent;
if (useGateway)
{
    // Use Gateway for distributed services
    var pool = new PinionCore.Remote.Gateway.Hosts.AgentPool(protocol);
    agent = new PinionCore.Remote.Gateway.Agent(pool);
}
else
{
    // Direct connection
    agent = PinionCore.Remote.Client.Provider.CreateAgent(protocol);
}
```

**Gateway Benefits**:
- Load balancing across multiple servers
- Service discovery and routing
- Protocol version management
- Microservices architecture support

### StatusMachine Pattern

Event-driven state management for connection lifecycle:

```csharp
public class Client : MonoBehaviour, IStatus
{
    readonly StatusMachine _Machine;

    private void Start()
    {
        _Machine.Push(this);  // Push initial state
    }

    private void Update()
    {
        _Machine.Update();  // Drive state machine
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

**StatusMachine Benefits**:
- Clean state transitions
- Event-driven design (no enum/switch)
- Resource management in Enter/Leave
- Easy to test and debug

### Custom Protocol Definition

Define custom RMI interfaces:

```csharp
// Define shared interface
public interface IPlayer
{
    PinionCore.Remote.Value<string> GetName();
    void Move(float x, float y, float z);
}

// Server implementation
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

// Client binding
client.Queryer.QueryNotifier<IPlayer>().Supply += (player) =>
{
    var name = player.GetName();
    player.Move(10, 0, 10);
};
```

---

## Examples

For complete working examples, see the [development repository](https://github.com/jiowchern/PinionCore.NetSync):

### Sample1 (Basic)
**Location**: `Assets/PinionCore/Sample1/`

**Features**:
- TCP, WebSocket, Standalone transport tests
- Simple Soul-Ghost synchronization
- Basic MonoBehaviour setup

**Best for**:
- First-time users
- Understanding basic architecture
- Testing different transport layers

### Sample2-Chat (Advanced)
**Location**: `Assets/PinionCore/Sample2-Chat/`

**Features**:
- Multiplayer chat room
- StatusMachine state management
- Gateway mode switching
- TCP/WebSocket platform detection
- WebGL build support

**Key Files**:
- `Client.cs`: Main controller (IConnect, IStatus)
- `Controller.cs`: UI logic (Unity Events)
- `LoopState.cs`: Game loop state
- `TcpSocketState.cs`, `WebSocketState.cs`: Connection states

**Best for**:
- Production reference
- StatusMachine pattern learning
- WebGL deployment

---

## Best Practices

### 1. Connection Handling

✅ **Correct** - Use SocketErrorEvent:
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

❌ **Incorrect** - Polling with Ping:
```csharp
void Update()
{
    if (!_Agent.Ping()) // Don't do this!
    {
        // Handle disconnect
    }
}
```

### 2. Resource Management

Use the `_Dispose` closure pattern:

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

### 3. Avoid Static Classes

❌ **Don't**: Use static classes for game state
```csharp
public static class GameManager // Bad for networking
{
    public static int Score;
}
```

✅ **Do**: Use instance-based design
```csharp
public class GameState : Soul
{
    public int Score { get; set; }
}
```

### 4. Platform-Specific Code

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL-specific code
    var connector = new WebSocketState(endpoint);
#else
    // Standalone code
    var connector = new TcpSocketState(endpoint);
#endif
```

---

## Requirements

- **Unity**: 2022.2 or later (Unity 6000.2+ recommended)
- **.NET Standard**: 2.1
- **Platforms**: Windows, macOS, Linux, WebGL

---

## Documentation

- 📘 [Development Guide](https://github.com/jiowchern/PinionCore.NetSync/blob/main/CLAUDE.md) - Complete architecture and workflow
- 📙 [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) - Core RMI framework
- 🔗 [Development Repository](https://github.com/jiowchern/PinionCore.NetSync) - Full development environment

---

## Troubleshooting

### WebGL Build Issues

**Problem**: WebSocket connection fails in WebGL build

**Solution**: Ensure you're using WebSocket transport:
```csharp
if (Application.platform == RuntimePlatform.WebGLPlayer)
{
    var state = new WebSocketState("ws://your-server:8080");
}
```

### Connection Timeout

**Problem**: Client disconnects after a few seconds

**Solution**: Check firewall settings and ensure server is listening on correct port:
```csharp
// Server
var listener = gameObject.AddComponent<Tcp.Listener>();
listener.Port = 7777;
Debug.Log($"Server listening on port {listener.Port}");
```

### Soul-Ghost Not Syncing

**Problem**: Ghost doesn't update when Soul moves

**Solution**: Ensure both Soul and Ghost inherit from base classes:
```csharp
// Server
public class MySoul : Soul { } // Must inherit Soul

// Client
public class MyGhost : Ghost { } // Must inherit Ghost
```

---

## Performance Tips

1. **Use Tracker System**: Automatically compresses position data
2. **Batch Updates**: Group multiple small updates into one
3. **Selective Synchronization**: Only sync what's needed
4. **Connection Pooling**: Reuse connections when possible

---

## License

MIT License - see [LICENSE](LICENSE) for details

---

## Links

- 🏗️ [Development Repository](https://github.com/jiowchern/PinionCore.NetSync) - Full development environment with samples
- 🎮 [Online Demo](https://proxy.pinioncore.dpdns.org/sample2) - Try it now
- 📦 [PinionCore.Remote Framework](https://github.com/jiowchern/PinionCore.Remote) - Core RMI framework

---

## Contributing

Contributions are welcome! Please visit the [development repository](https://github.com/jiowchern/PinionCore.NetSync) for contribution guidelines.

---

**Made with ❤️ using [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote)**
