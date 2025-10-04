using System;
namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Message sent by the IDE to indicate that a target application has just been launched.
/// The dev-server will correlate this registration with a later runtime connection (`AppLaunchMessage` with `Step = Connected`) using the MVID, Platform and IsDebug.
/// </summary>
/// <param name="Mvid">The MVID (Module Version ID) of the head application assembly.</param>
/// <param name="Platform">The target platform (case-sensitive, e.g. "Wasm", "Android").</param>
/// <param name="IsDebug">Whether the app was launched under a debugger (Debug configuration).</param>
public record AppLaunchRegisterIdeMessage(Guid Mvid, string Platform, bool IsDebug) : IdeMessage(WellKnownScopes.DevServerChannel);
