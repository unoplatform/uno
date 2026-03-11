---
uid: Uno.Features.Gamepad
---

# Gamepad

> [!TIP]
> This article covers Uno-specific information for `Gamepad`. For a full description of the feature and instructions on using it, see [Gamepad Class](https://learn.microsoft.com/uwp/api/windows.gaming.input.gamepad).

* The `Windows.Gaming.Input.Gamepad` class allows working with the connected gamepads.

## Supported features

| Feature             | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
| ------------------- | ------- | ------- | --- | ---------- | ----- | ------------ | ------------ |
| `GetCurrentReading` | ✔       | ✔       | ✔   | ✔          | ✔     | ✖            | ✖            |
| `GamepadAdded`      | ✔       | ✔       | ✔   | ✔          | ✔     | ✖            | ✖            |
| `GamepadRemoved`    | ✔       | ✔       | ✔   | ✔          | ✔     | ✖            | ✖            |
| `Gamepads`          | ✔       | ✔       | ✔   | ✔          | ✔     | ✖            | ✖            |

## Example

### Getting the gamepads list

```csharp
private readonly object myLock = new object();
private HashSet<Gamepad> myGamepads = new HashSet<Gamepad>();

private void GetGamepads()
{
    lock (myLock)
    {
        foreach (var gamepad in Gamepad.Gamepads)
        {
            // Check if the gamepad is already in myGamepads; if it isn't, add it.
            if (!myGamepads.Contains(gamepad))
            {
                myGamepads.Add(gamepad);
            }
        }
    }   
}
```

### Reading current values

```csharp
Gamepad gamepad = myGamepads[0];

GamepadReading reading = gamepad.GetCurrentReading();

// returns a value between 0.0 and 1.0
double leftTrigger = reading.LeftTrigger;  
double rightTrigger = reading.RightTrigger;

// returns a value between -1.0 and +1.0
double leftStickX = reading.LeftThumbstickX;
double leftStickY = reading.LeftThumbstickY;   
double rightStickX = reading.RightThumbstickX; 
double rightStickY = reading.RightThumbstickY;

if (GamepadButtons.A == (reading.Buttons & GamepadButtons.A))
{
    // button A is pressed
}
```

## See Gamepad in action

* To see this API in action, visit the [Uno Gallery](https://gallery.platform.uno/) and look for Gamepad.
