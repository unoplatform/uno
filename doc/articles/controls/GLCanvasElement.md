---
uid: Uno.Controls.GLCanvasElement
---

## GLCanvasElement

> [!IMPORTANT]
> This functionality is only available on Skia Desktop (`netX.0-desktop`) targets that are running with hardware acceleration. This is also not available on MacOS.

`GLCanvasElement` is a `FrameworkElement` for drawing 3D graphics with OpenGL.

To use `GLCanvasElement`, create a subclass of `GLCanvasElement` and override the abstract methods `Init`, `RenderOverride` and `OnDestroy`.

```csharp
protected GLCanvasElement(Size resolution);

protected abstract void Init(GL gl);
protected abstract void RenderOverride(GL gl);
protected abstract void OnDestroy(GL gl);
```

The protected constructor has a `Size` parameter, which decides the resolution of the offscreen framebuffer that the `GLCanvasElement` will draw onto. Note that the `resolution` parameter is unrelated to the final size of the drawing in the Uno window. After drawing (using `RenderOverride`) is done, the output is resized to fit the arranged size of the `GLCanvasElement`. You can control the final size just like any other `UIelement`, e.g. using `MeasureOverride`, `ArrangeOverride`, the `Width/Height` properties, etc.

The 3 abstract methods above all take a `Silk.NET.OpenGL.GL` parameter that can be used to make OpenGL calls.

> [!IMPORTANT]
> If your application uses `GLCanvasElement`, you will need to add a PackageReference to `Silk.NET.OpenGL`.

The `Init` method is a regular OpenGL setup method that you can use to set up the needed OpenGL objects, like textures, Vertex Array Buffers (VAOs), Element Array Buffers (EBOs), etc.

The `OnDestroy` method is the complement of `Init` and is used to clean up any allocated resources.

> [!IMPORTANT]
> `Init` and `OnDestroy` might be called multiple times in pairs. Every call to `OnDestroy` will be preceded by a call to `Init`.

The `RenderOverride` is the main render-loop function. When adding your drawing logic in `RenderOverride`, you can assume that the OpenGL viewport rectangle is already set and its dimensions are equal to the `resolution` parameter provided to the `GLCanvasElement` constructor. Due to the fact that both `GLCanvasElement` and the Skia rendering engine used by Uno both use OpenGL, you must make sure to restore all the OpenGL state values to their original values at the end of `RenderOverride`. For example, make sure to save the values for the initially-bound VAO if you intend to bind your own VAO and bind the original VAO at the end of `RenderOverride`.

```csharp
protected override void RenderOverride(GL gl)
{
    var oldVAO = gl.GetInteger(GLEnum.VertexArrayBinding);
    gl.BindVertexArray(myVAO);

    // draw with myVAO

    gl.BindVertexArray(oldVAO);
}
```

Similarly, make sure to disable depth testing at the end if you choose to enable it. To reduce bugs, some of the more common OpenGL state variables are restored automatically for you, but don't depend on this behaviour.

To learn more about using Silk.NET as a C# binding for OpenGL, see the examples in the Silk.NET repository [here](https://github.com/dotnet/Silk.NET/tree/main/examples/CSharp). Note that the windowing and inputs APIs in Silk.NET are not relevant to `GLCanvasElement`, since Uno Platform has its own support for input and windowing.

Additionally, `GLCanvasElement` has an `Invalidate` method that can be used at any time to tell the Uno Platform runtime to redraw the `GLCanvasElement`, calling `RenderOverride` in the process. Note that `RenderOverride` will only be called once per `Invalidate` call and the output will be saved to be used in future frames. To update the output, you must call `Invalidate`. If you need to continuously update the output (e.g. in an animation), you can add an `Invalidate` call inside `RenderOverride`.

By default, a `GLCanvasElement` takes all the available space given to it in the `Measure` cycle. If you want to customize how much space the element takes, you can override its `MeasureOverride` and `ArrangeOverride` methods.

Note that since a `GLCanvasElement` takes as much space as it can, it's not allowed to place a `GLCanvasElement` inside a `StackPanel`, a `Grid` with `Auto` sizing, or any other element that provides its child(ren) with infinite space. To work around this, you can explicitly set the `Width` and/or `Height` of the `GLCanvasElement`.

## Full example

To see this in action, here's a complete sample that uses `GLCanvasElement` to draw a triangle. Note how you have to be careful with surrounding all the OpenGL-related logic in platform-specific guards. This is the case for both the [XAML](platform-specific-xaml) and the [code-behind](platform-specific-csharp).

XAML:

```xaml
<!-- GLCanvasElementExample.xaml -->
<UserControl x:Class="BlankApp.GLCanvasElementExample"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="using:BlankApp"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:skia="http://uno.ui/skia"
             xmlns:not_skia="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             mc:Ignorable="d"
             d:DesignHeight="300"
             d:DesignWidth="400">

    <skia:GLTriangleElement />
    <not_skia:TextBlock Text="This sample is only supported on skia." />
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
// GLTriangleElement.skia.cs <-- NOTICE the `.skia`
public class GLTriangleElement() : GLCanvasElement(new Size(1200, 800))
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

        const string vertexCode = @"
#version 330 core

layout (location = 0) in vec3 aPosition;
out vec4 vertexColor;

void main()
{
gl_Position = vec4(aPosition, 1.0);
vertexColor = vec4(aPosition.x + 0.5, aPosition.y + 0.5, aPosition.z + 0.5, 1.0);
}";

        const string fragmentCode = @"
#version 330 core

out vec4 out_color;
in vec4 vertexColor;

void main()
{
out_color = vertexColor;
}";

        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexCode);
        gl.CompileShader(vertexShader);

        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int) GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + gl.GetShaderInfoLog(vertexShader));

        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentCode);
        gl.CompileShader(fragmentShader);

        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int) GLEnum.True)
            throw new Exception("Fragment shader failed to compile: " + gl.GetShaderInfoLog(fragmentShader));

        _program = gl.CreateProgram();
        gl.AttachShader(_program, vertexShader);
        gl.AttachShader(_program, fragmentShader);
        gl.LinkProgram(_program);

        gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int) GLEnum.True)
            throw new Exception("Program failed to link: " + gl.GetProgramInfoLog(_program));

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
```
