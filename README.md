# PinionCore NetSync

Unity network synchronization package based on [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) framework. Implements Remote Method Invocation (RMI) with Soul-Ghost architecture for client-server networking.

## Features

- **Soul-Ghost Architecture**: Server-authoritative networking with client-side prediction
- **Multiple Transport Layers**: TCP, WebSocket, and Standalone (local mode) support
- **Position Tracking & Compression**: Efficient bandwidth usage with trajectory compression
- **Unity 2022.2+ Compatible**: Built as a Unity Package Manager (UPM) package
- **RMI Pattern**: Remote Method Invocation for easy network communication

## Installation

### Via Git URL (Unity 2022.2+)

Add the following to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.Package.git"
  }
}
```

Or use the Unity Package Manager:
1. Window > Package Manager
2. Click "+" > "Add package from git URL..."
3. Enter: `https://github.com/jiowchern/PinionCore.NetSync.Package.git`

### Specific Version

```json
"com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.Package.git#v0.0.1"
```

## Quick Start

### Server Setup

```csharp
using PinionCore.NetSync.Links;

// Create a TCP server
var server = gameObject.AddComponent<Server>();
server.EnableLog = true; // Optional: enable logging

// Start listening
var listener = gameObject.AddComponent<Tcp.Listener>();
listener.Port = 7777;
```

### Client Setup

```csharp
using PinionCore.NetSync.Links;

// Create a TCP client
var client = gameObject.AddComponent<Client>();
client.EnableLog = true; // Optional: enable logging

// Connect to server
var connector = gameObject.AddComponent<Tcp.Connector>();
connector.Host = "127.0.0.1";
connector.Port = 7777;
```

### Soul-Ghost Synchronization

**Server Side (Soul)**:
```csharp
using PinionCore.NetSync.Syncs.Souls;

public class MyGameObject : Soul
{
    // Server-authoritative logic
    void Start()
    {
        // Automatically synchronized to clients
    }
}
```

**Client Side (Ghost)**:
```csharp
using PinionCore.NetSync.Syncs.Ghosts;

public class MyGameObjectGhost : Ghost
{
    // Receives state from server
    void Update()
    {
        // Render server state
    }
}
```

## Architecture

### Soul-Ghost Pattern

- **Soul (Server)**: Authoritative game state and logic
- **Ghost (Client)**: Receives and displays server state
- **Tracker System**: Position compression and interpolation

### Transport Layers

1. **TCP**: Reliable, ordered delivery
2. **WebSocket**: Web-compatible, firewall-friendly
3. **Standalone**: Local in-memory transport (testing/single-player)

## Documentation

For detailed architecture and development guide, see the [development repository](https://github.com/jiowchern/PinionCore.NetSync).

## Requirements

- Unity 2022.2 or later
- .NET Standard 2.1

## License

MIT License - see [LICENSE](LICENSE) for details

## Links

- [PinionCore.Remote](https://github.com/jiowchern/PinionCore.Remote) - Core RMI framework
- [Development Repository](https://github.com/jiowchern/PinionCore.NetSync) - Full development environment
