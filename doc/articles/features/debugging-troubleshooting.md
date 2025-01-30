---
uid: Uno.Debugging.Troubleshooting
---

# Debugging Troubleshooting

This section covers common issues along with simple workarounds to resolve them. If you're hitting roadblocks, you might find a solution here!

## Issue: Breakpoint Misfires on a Line with Multiple Statements

**Problem:**  
When setting a breakpoint on a line with multiple statements, such as:

```csharp
int x = 1; int y = 2;
```

or

```csharp
public object MyObject { get => _myObject; set => _myObject = value; }
```

On **Mobile (Android and iOS)** or **WebAssembly (WASM)**, Mono might only hit the first or last statement due to how it handles sequence points. This makes debugging on such lines unreliable.

**Workaround:**
To ensure proper breakpoint hits, split each statement onto its own line:

```csharp
int x = 1;
int y = 2;
```

```csharp
public object MyObject 
{
    get => _myObject;
    set => _myObject = value;
}
```

Now, Mono will correctly hit each breakpoint as expected.
