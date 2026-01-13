# Frame Transport

## Purpose
`IFrameTransport` defines how frames are carried between the runtime client and the dev server.
It exists to keep the protocol (frames + messages) independent from the underlying transport.

## Current behavior
The default transport is WebSocket. This keeps existing workflows unchanged while allowing
alternative transports to be added without changing client/server logic.

## Available transports
An in-process transport (for embedded dev server hosting) is available via `InProcessFrameTransport`.
`FrameTransportPair.Create()` returns a disposable pair that manages the lifetime of both transports.

## Example
```csharp
using var pair = FrameTransportPair.Create();
var (peer1, peer2) = pair;

await peer1.SendAsync(Frame.Create(1, "scope", "name", new { Value = 42 }), CancellationToken.None);

var received = await peer2.ReceiveAsync(CancellationToken.None);
```
