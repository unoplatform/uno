---
uid: Uno.Controls.GLCanvasElement
---

## Uno.WinUI.Graphics3D.GLCanvasElement

> [!IMPORTANT]
> This functionality is only available on WinUI, Android and Skia Desktop (`netX.0-desktop`) and targets that are running on platforms with OpenGL support. This is also not available on MacOS.

`GLCanvasElement` is a `Grid` for drawing 3D graphics with OpenGL. This class comes as a part of the `Uno.WinUI.Graphics3D` package.

To use `GLCanvasElement`, create a subclass of `GLCanvasElement` and override the abstract methods `Init`, `RenderOverride` and `OnDestroy`.

```csharp
protected GLCanvasElement(uint width, uint height, Func<Window> getWindowFunc);

protected abstract void Init(GL gl);
protected abstract void RenderOverride(GL gl);
protected abstract void OnDestroy(GL gl);
```

The protected constructor has `width` and `height` parameters, which decide the resolution of the offscreen framebuffer that the `GLCanvasElement` will draw onto. Note that these parameters are unrelated to the final size of the drawing in the window. After drawing (using `RenderOverride`) is done, the output is resized to fit the arranged size of the `GLCanvasElement`. You can control the final size just like any other `Grid`, e.g. using `MeasureOverride`, `ArrangeOverride`, the `Width/Height` properties, etc.

On WinUI, the protected constructor additionally requires a `Func<Window>` argument that fetches the `Microsoft.UI.Xaml.Window` object that the `GLCanvasElement` belongs to. This function is required because WinUI doesn't provide a way to get the `Window` of a `FrameworkElement`. This paramater is ignored on Uno Platform and can be set to null. This function is only called while the `GLCanvasElement` is loaded.

The 3 abstract methods above all take a `Silk.NET.OpenGL.GL` parameter that can be used to make OpenGL calls.

The `Init` method is a regular OpenGL setup method that you can use to set up the needed OpenGL objects, like textures, Vertex Array Buffers (VAOs), Element Array Buffers (EBOs), etc.

The `OnDestroy` method is the complement of `Init` and is used to clean up any allocated resources.

The `RenderOverride` is the main render-loop function. When adding your drawing logic in `RenderOverride`, you can assume that the OpenGL viewport rectangle is already set and its dimensions are equal to the `resolution` parameter provided to the `GLCanvasElement` constructor.

To learn more about using [Silk.NET](https://www.nuget.org/packages/Silk.NET.OpenGL/) as a C# binding for OpenGL, see the examples in the Silk.NET repository [here](https://github.com/dotnet/Silk.NET/tree/main/examples/CSharp). Note that the windowing and inputs APIs in Silk.NET are not relevant to `GLCanvasElement`, since we only use Silk.NET as an OpenGL binding library, not a windowing library.

Additionally, `GLCanvasElement` has an `Invalidate` method that requests a redrawing of the `GLCanvasElement`, calling `RenderOverride` in the process. Note that `RenderOverride` will only be called once per `Invalidate` call and the output will be saved to be used in future frames. To update the output, you must call `Invalidate`. If you need to continuously update the output (e.g. in an animation), you can add an `Invalidate` call inside `RenderOverride`.

## Full example

To see this in action, here's a complete sample that uses `GLCanvasElement` to draw a triangle. Note how you have to be careful with surrounding all the OpenGL-related logic in platform-specific guards. This is the case for both the [XAML](platform-specific-xaml) and the [code-behind](platform-specific-csharp).

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
# __SKIA__ || WINAPPSDK
public class SimpleTriangleGlCanvasElement()
#if __SKIA__
        : GLCanvasElement(1200, 800, null)
#elif WINAPPSDK
        // getWindowFunc is usually implemented by having a static property that stores the Window object when creating it (usually in App.cs) and then fetching it in getWindowFunc
        : GLCanvasElement(1200, 800, /* your getWindowFunc */)
#endif
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

        // string.Empty is added so that the version line is not interpreted as a preprocessor command
        var vertexCode =
        $$"""
        {{string.Empty}}#version 330

        layout (location = 0) in vec3 aPosition;
        out vec4 vertexColor;

        void main()
        {
         gl_Position = vec4(aPosition, 1.0);
         vertexColor = vec4(aPosition.x + 0.5, aPosition.y + 0.5, aPosition.z + 0.5, 1.0);
        }
        """;

        // string.Empty is added so that the version line is not interpreted as a preprocessor command
        var fragmentCode =
        $$"""
        {{string.Empty}}#version 330

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
