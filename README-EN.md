# PinionCore NetSync Package

PinionCore NetSync delivers a transport-agnostic state replication layer for Unity projects. It wraps the lower-level PinionCore.Remote transport stack and exposes Unity-friendly components for building authoritative server/client experiences.

## Features

- Transport abstraction with built-in Standalone loopback, TCP, and WebSocket connectors.
- Ghost/Soul replication pipeline for binding authoritative objects and projecting synchronized "ghosts" on remote clients.
- Tracker utilities that interpolate and compress transform data (zip encoding, repeat minimization).
- Inspector extensions for monitoring protocol hashes, latency, throughput, and binder membership.
- NUnit-based regression tests for trackers and networking primitives.

## Installation

### Option 1 — Git dependency
Reference the package directly in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.pinioncore.netsync": "https://github.com/jiowchern/PinionCore.NetSync.git?path=PinionCore.NetSync.Package"
  }
}
```

### Option 2 — Local package
If this repository is cloned next to your Unity project, add:

```json
{
  "dependencies": {
    "com.pinioncore.netsync": "file:../PinionCore.NetSync.Package"
  }
}
```

Whenever you update NetSync, update the `PinionCore.Remote` submodule as well to keep transport APIs in sync.

## Quick start

1. **Authoritative side (server)**
   - Place a `Server` component in the scene.
   - Add the listener that matches your transport (`Standalone.Listener`, `Tcp.TcpListener`, or `Web.WebListener`) and call its bind method during initialization (`Bind()` for Standalone, `Bind(port)` for TCP/Web).
   - Subscribe to `Server.BinderEvent` to react whenever clients register or unregister binders.

2. **Client side**
   - Add a `Client` component and choose a connector (`Standalone.Connector`, `Tcp.TcpConnector`, or `Web.WebConnector`).
   - Invoke the connector’s `Connect`/`Disconnect` methods from gameplay code or UI (see `PinionCore.NetSync.Develop/Assets/PinionCore/Sample1/Scripts` for usage patterns).

3. **Synchronizing objects**
   - Attach `Syncs.Souls.Soul` to authoritative GameObjects and use `gameobject.Bind<T>()` (`SoulFinder`) to register protocol objects such as `Syncs.Souls.Transform` or `Syncs.Souls.TrackerSender`.
   - On remote prefabs add `Syncs.Ghosts.Ghost` and the corresponding ghost behaviours (for example `Syncs.Ghosts.Transform` or `Syncs.Ghosts.TrackerReceiver`). Access incoming data through `gameObject.Query<T>()`.
   - Tune replication cadence with tracker intervals or the `Transform.SyncInterval` property.

4. **Runtime diagnostics**
   - Enable logging by setting `Client.EnableLog` or `Server.EnableLog` before initialization.
   - Inspector extensions for `Server`, `Client`, listeners, and connectors expose protocol hashes, ping, throughput, and binder membership.

## Architecture

- **Links** (`Runtime/Scripts/Links`): transport abstractions and `ProtocolCreator` that bridge to PinionCore.Remote streams.
- **Syncs** (`Runtime/Scripts/Syncs`): Ghost/Soul binding utilities, tracker compression, and notifier infrastructure.
- **Extensions**: helper methods such as `GameObject.Bind<T>()`, `GameObject.Unbind()`, `GameObject.Query<T>()`, and UI label binding utilities.
- **Editor** (`Editor/Scripts`): UI Toolkit inspectors and supporting assets.
- **Tests** (`Tests/`): NUnit fixtures covering tracker sampling, compression, and networking contracts.
- **Analyzers** (`Analyzers/`): Roslyn analyzers executed by CI.

## Samples

Import the sample scenes from the Unity Package Manager window to review ready-made setups:

- **Sample 1** mirrors the development project scenes and walks through Standalone, TCP, and WebSocket connectors.
- **Sample 2 – Chat** showcases protocol switching and UI feedback.

## Testing

When the package is embedded in a project, run the NUnit suite via the Unity Test Runner (Edit Mode). For automation, use:

```powershell
"<UnityEditorPath>\Unity.exe" -projectPath <your-project> -quit -batchmode -runTests -testPlatform EditMode -testResults Logs/editmode.xml
```

Ensure the analyzers in `Analyzers/` are executed as part of your CI to catch API and style regressions.

## Versioning

- The package version is defined in `package.json`.
- Keep `PinionCore.Remote` on the same tag across all consumers.
- Document user-facing changes in `CHANGELOG.md`.

## Support

Report issues and feature requests in the main repository tracker. Include Unity version, transport type, reproduction steps, and relevant logs.

## License

The package inherits the repository’s `LICENSE`.
