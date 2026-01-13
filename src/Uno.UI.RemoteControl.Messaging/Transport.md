# Frame Transport

## Purpose
`IFrameTransport` defines how frames are carried between the runtime client and the dev server.
It exists to keep the protocol (frames + messages) independent from the underlying transport.

## Current behavior
The default transport is WebSocket. This keeps existing workflows unchanged while allowing
alternative transports to be added without changing client/server logic.

## Planned extensions
An in-process transport (for embedded dev server hosting) will be added in a later phase.
The `FrameTransportPair.Create()` helper is intended to provide a paired in-process transport.
