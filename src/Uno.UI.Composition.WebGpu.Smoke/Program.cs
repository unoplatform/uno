// Headless pixel-level verification that the NON-Skia WebGPU render backend implements the neutral drawing
// seam correctly — solid rect, path fill (stencil-then-cover) over a MANAGED IGeometry, linear gradient, and a
// GPU-resident image — each rendered offscreen via lavapipe and read back. Zero Skia in the whole path.
using System;
using System.Numerics;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu;
using WColor = Windows.UI.Color;

int fail = 0;
var dev = new WebGpuDevice();

byte[] Render(Action<WebGpuCommandRecorder> draw)
{
	var surface = new WebGpuRenderSurface(dev, 64, 64);
	var rec = new WebGpuCommandRecorder();
	draw(rec);
	var present = new WebGpuPresentSession(dev, surface);
	present.Clear(WColor.FromArgb(255, 0, 0, 0)); // opaque black
	present.Replay(rec.Finish());
	return dev.ReadPixelsRgba(surface);
}
(int r, int g, int b, int a) At(byte[] px, int x, int y) { int i = (y * 64 + x) * 4; return (px[i], px[i + 1], px[i + 2], px[i + 3]); }
void Check(string name, bool ok, object got) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}  (got {got})"); if (!ok) { fail++; } }

// 1) solid rect
var red = WColor.FromArgb(255, 255, 0, 0);
var p1 = Render(r => r.DrawRect(new Rect(0, 0, 64, 64), red, false));
var rc = At(p1, 32, 32);
Check("rect: center red", rc.r > 200 && rc.g < 60 && rc.b < 60, rc);

// 2) path fill (stencil-then-cover) consuming a MANAGED IGeometry
var pb = new ManagedDrawingBackend().CreatePathBuilder();
pb.MoveTo(new Vector2(32, 4)); pb.LineTo(new Vector2(60, 60)); pb.LineTo(new Vector2(4, 60)); pb.Close();
using var tri = pb.Build();
var green = WColor.FromArgb(255, 0, 255, 0);
var p2 = Render(r => r.DrawPath(tri, green, false));
var pin = At(p2, 32, 40); var pout = At(p2, 3, 6);
Check("path: inside green", pin.g > 200 && pin.r < 60 && pin.b < 60, pin);
Check("path: outside black", pout.r < 40 && pout.g < 40 && pout.b < 40, pout);

// 3) linear gradient (red -> blue, horizontal) via a backend-native WebGpuShader
var grad = new WebGpuShader
{
	Radial = false,
	P0 = new Vector2(0, 0), P1 = new Vector2(64, 0),
	Colors = new[] { WColor.FromArgb(255, 255, 0, 0), WColor.FromArgb(255, 0, 0, 255) },
	Stops = new[] { 0f, 1f },
	TileMode = GradientTileMode.Clamp,
	LocalMatrix = Matrix3x2.Identity,
};
var p3 = Render(r => r.DrawRect(new Rect(0, 0, 64, 64), grad, false));
var gl = At(p3, 3, 32); var gr = At(p3, 60, 32);
Check("gradient: left reddish", gl.r > 150 && gl.b < 100, gl);
Check("gradient: right bluish", gr.b > 150 && gr.r < 100, gr);

// 4) GPU image draw — upload a managed BGRA image to a wgpu texture, draw it
var blue = new SolidImage(40, 40, 0, 0, 255);
using var tex = new WebGpuImageTexture(dev, blue);
var p4 = Render(r => r.DrawImage(tex, 12, 12, default, 1f, false));
var im = At(p4, 32, 32); var iout = At(p4, 2, 2);
Check("image: center blue", im.b > 200 && im.r < 60 && im.g < 60, im);
Check("image: outside black", iout.r < 40 && iout.g < 40 && iout.b < 40, iout);

// 5) transform (Translate) — rect drawn at the origin lands in the translated region
var p5 = Render(r => { r.Translate(16, 16); r.DrawRect(new Rect(0, 0, 16, 16), red, false); });
var tin = At(p5, 20, 20); var tout = At(p5, 4, 4);
Check("transform: translated rect red", tin.r > 200 && tin.g < 60, tin);
Check("transform: origin untouched (black)", tout.r < 40 && tout.g < 40 && tout.b < 40, tout);

// 6) clip (scissor) — a full-surface draw only paints inside the clip rect
var p6 = Render(r => { r.ClipRect(new Rect(24, 24, 16, 16)); r.DrawRect(new Rect(0, 0, 64, 64), red, false); });
var cin = At(p6, 32, 32); var cout = At(p6, 4, 4);
Check("clip: inside clip red", cin.r > 200 && cin.g < 60, cin);
Check("clip: outside clip black", cout.r < 40 && cout.g < 40 && cout.b < 40, cout);

// 7) save / restore — restoring drops the clip, so a later full draw covers everything
var p7 = Render(r => { r.Save(); r.ClipRect(new Rect(0, 0, 10, 10)); r.Restore(); r.DrawRect(new Rect(0, 0, 64, 64), green, false); });
var sc = At(p7, 32, 32);
Check("save/restore: clip released → full green", sc.g > 200 && sc.r < 60, sc);

Console.WriteLine(fail == 0
	? "\nALL PASS — non-Skia render seam (rect · path · gradient · image · transform · clip · save/restore) verified headless"
	: $"\n{fail} CHECK(S) FAILED");
Environment.Exit(fail == 0 ? 0 : 1);

// A minimal neutral IImage backed by a solid BGRA buffer (the seam's decode-side currency).
sealed class SolidImage : IImage
{
	private readonly byte[] _bgra;
	public SolidImage(int w, int h, byte r, byte g, byte b)
	{
		PixelWidth = w; PixelHeight = h;
		_bgra = new byte[w * h * 4];
		for (int i = 0; i < _bgra.Length; i += 4) { _bgra[i] = b; _bgra[i + 1] = g; _bgra[i + 2] = r; _bgra[i + 3] = 255; }
	}
	public int PixelWidth { get; }
	public int PixelHeight { get; }
	public void CopyPixels(Span<byte> destination) => _bgra.AsSpan(0, Math.Min(_bgra.Length, destination.Length)).CopyTo(destination);
}
