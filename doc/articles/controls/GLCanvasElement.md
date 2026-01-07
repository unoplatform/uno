---
uid: Uno.Controls.GLCanvasElement
---

# GLCanvasElement

> [!IMPORTANT]
> This functionality is only available on WinAppSDK and Skia Desktop (`netX.0-desktop`) targets that are running on platforms with support for hardware acceleration. On Windows and Linux, OpenGL 3.0+ is used directly and on macOS, Metal is used through the [ANGLE](https://en.wikipedia.org/wiki/ANGLE_(software)) library.

`GLCanvasElement` is a control for drawing 3D graphics with OpenGL. It can be enabled by adding the [`GLCanvas` UnoFeature](xref:Uno.Features.Uno.Sdk). The OpenGL APIs provided are provided by [Silk.NET](https://dotnet.github.io/Silk.NET/).

## Using the GLCanvasElement

To use `GLCanvasElement`, create a subclass of `GLCanvasElement` and override the abstract methods `Init`, `RenderOverride` and `OnDestroy`.

```csharp
protected GLCanvasElement(Func<Window> getWindowFunc);

protected abstract void Init(GL gl);
protected abstract void RenderOverride(GL gl);
protected abstract void OnDestroy(GL gl);
```

These three abstract methods take a `Silk.NET.OpenGL.GL` parameter that can be used to make OpenGL calls.

### The GLCanvasElement constructor

The protected constructor requires a `Func<Window>` argument that fetches the `Microsoft.UI.Xaml.Window` object that the `GLCanvasElement` belongs to. This function is required because WinUI doesn't yet provide a way to get the `Window` of a `FrameworkElement`. This parameter is ignored on Uno Platform and can be set to null. This function is only called while the `GLCanvasElement` is still in the visual tree.

### The `Init` method

The `Init` method is a regular OpenGL setup method that you can use to set up the needed OpenGL objects, like textures, Vertex Array Buffers (VAOs), Element Array Buffers (EBOs), etc.  The `OnDestroy` method is the complement of `Init` and is used to clean up any allocated resources. `Init` and `OnDestroy` might be called multiple times alternatingly. In other words, 2 `OnDestroy` calls are guaranteed to have an `Init` call in between and vice versa.

### The `RenderOverride` method

The `RenderOverride` is the main render-loop function. When adding your drawing logic in `RenderOverride`, you can assume that the OpenGL viewport rectangle is already set and its dimensions are equal to the `RenderSize` of the `GLCanvasElement`.

### macOS Specifics

On MacOS, since OpenGL support is not natively present, we use [ANGLE](https://en.wikipedia.org/wiki/ANGLE_(software)) to provide OpenGL ES support. This means that we're actually using OpenGL ES 3.00, not OpenGL. Due to the similarity between desktop OpenGL and OpenGL ES, (almost) all the OpenGL ES functions are present in the `Silk.NET.OpenGL.GL` API surface and therefore we can use the same class to represent both the OpenGL and OpenGL ES APIs. To run the same `GLCanvasElement` subclasses on all supported platforms, make sure to use a subset of functions that are present in both APIs (which is almost all of OpenGL ES).

## Invalidating the canvas

Additionally, `GLCanvasElement` has an `Invalidate` method that requests a redrawing of the `GLCanvasElement`, calling `RenderOverride` in the process. Note that `RenderOverride` will only be called once per `Invalidate` call and the output will be saved to be used in future frames. To update the output, you must call `Invalidate`. If you need to continuously update the output (e.g. in an animation), you can add an `Invalidate` call inside `RenderOverride`.

## Detecting errors

To detect errors in initializing the OpenGL environment, `GLCanvasElement` exposes an `IsGLInitializedProperty` dependency property that shows whether or nor the loading of the element and its OpenGL setup were successful. This property is only valid when the element is loaded, i.e. its `IsLoaded` property is true. When the element is not loaded, the value of `IsGLInitialized` will be null. `GLCanvasElement` implements `INotifyPropertyChanged`, so you can use this property in a data bindings, for example to set the visibility of a control as a fallback. Attempting to change this property is illegal.

## How to use Silk.NET

To learn more about using [Silk.NET](https://www.nuget.org/packages/Silk.NET.OpenGL/) as a C# binding for OpenGL, see the [examples in the Silk.NET repository](https://github.com/dotnet/Silk.NET/tree/main/examples/CSharp). Note that the windowing and inputs APIs in Silk.NET are not relevant to `GLCanvasElement`, since we only use Silk.NET as an OpenGL binding library, not a windowing library.

## Full example

To see this in action, here's a complete sample that uses `GLCanvasElement` to draw a triangle. Note how you have to be careful with surrounding all the OpenGL-related logic in platform-specific guards. This is the case for both the [XAML](platform-specific-xaml) and the [code-behind](platform-specific-csharp). For complete C# projects, visit our [GLCanvasElement Samples in the Uno.Samples repository](https://aka.platform.uno/glcanvaselement-sample).

XAML:

```xaml
<!-- GLCanvasElementExample.xaml -->
<UserControl x:Class="BlankApp.GLCanvasElementExample"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="using:BlankApp"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:skia="http://uno.ui/skia#using:UITests.Shared.Windows_UI_Composition"
             xmlns:not_skia="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:win="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:not_win="http://uno.ui/not_win"
             mc:Ignorable="skia not_win">

    <Grid>
        <skia:SimpleTriangleGlCanvasElement />
        <win:Grid>
            <local:SimpleTriangleGlCanvasElement />
        </win:Grid>
        <not_win:Grid>
            <not_skia:TextBlock Text="This sample is only supported on skia targets and WinUI." />
        </not_win:Grid>
    </Grid>
</UserControl>
```

Code-behind:

```csharp
// GLCanvasElementExample.xaml.cs
public partial class GLCanvasElementExample : UserControl
{
    public GLCanvasElementExample()
    {
        this.InitializeComponent();
    }
}
```

```csharp
// GLTriangleElement.cs
#if DESKTOP || WINDOWS
// https://learnopengl.com/Getting-started/Hello-Triangle
public class SimpleTriangleGlCanvasElement()
    // Assuming that App.xaml.cs has a static property named MainWindow
    : GLCanvasElement(() => App.MainWindow)
{
    private uint _vao;
    private uint _vbo;
    private uint _program;

    unsafe protected override void Init(GL gl)
    {
        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        float[] vertices =
        {
            0.5f, -0.5f, 0.0f,  // bottom right
            -0.5f, -0.5f, 0.0f,  // bottom left
            0.0f,  0.5f, 0.0f   // top
        };

        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(vertices), BufferUsageARB.StaticDraw);

        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 3 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);

        var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
        var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
            ? "#version 300 es"
            : "#version 330";
        var vertexCode =
        $$"""
        {{versionDef}}
        precision highp float; // for OpenGL ES compatibility

        layout (location = 0) in vec3 aPosition;
        out vec4 vertexColor;

        void main()
        {
         gl_Position = vec4(aPosition, 1.0);
         vertexColor = vec4(aPosition.x + 0.5, aPosition.y + 0.5, aPosition.z + 0.5, 1.0);
        }
        """;

        var fragmentCode =
        $$"""
        {{versionDef}}
        precision highp float; // for OpenGL ES compatibility

        out vec4 out_color;
        in vec4 vertexColor;

        void main()
        {
            out_color = vertexColor;
        }
        """;

        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexCode);
        gl.CompileShader(vertexShader);

        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
        {
            throw new Exception("Vertex shader failed to compile: " + gl.GetShaderInfoLog(vertexShader));
        }

        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentCode);
        gl.CompileShader(fragmentShader);

        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int)GLEnum.True)
        {
            throw new Exception("Fragment shader failed to compile: " + gl.GetShaderInfoLog(fragmentShader));
        }

        _program = gl.CreateProgram();
        gl.AttachShader(_program, vertexShader);
        gl.AttachShader(_program, fragmentShader);
        gl.LinkProgram(_program);

        gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            throw new Exception("Program failed to link: " + gl.GetProgramInfoLog(_program));
        }

        gl.DetachShader(_program, vertexShader);
        gl.DetachShader(_program, fragmentShader);
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }

    protected override void OnDestroy(GL gl)
    {
        gl.DeleteVertexArray(_vao);
        gl.DeleteBuffer(_vbo);
        gl.DeleteProgram(_program);
    }

    protected override void RenderOverride(GL gl)
    {
        gl.ClearColor(Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit);

        gl.UseProgram(_program);

        gl.BindVertexArray(_vao);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
    }
}
#endif
```
