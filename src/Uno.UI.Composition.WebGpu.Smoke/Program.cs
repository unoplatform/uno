// Headless pixel-level verification that the NON-Skia WebGPU render backend implements the neutral drawing
// seam correctly — solid rect, path fill (stencil-then-cover) over a MANAGED IGeometry, linear gradient, and a
// GPU-resident image — each rendered offscreen via lavapipe and read back. Zero Skia in the whole path.
using System;
using System.Numerics;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu;
using Windows.UI.Text;
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
var pb = new ManagedDrawingFactory().CreatePathBuilder();
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

// 3b) RADIAL gradient must be ELLIPTICAL (RadiusX != RadiusY), not circular. Center red -> edge blue.
var radial = new WebGpuShader
{
	Radial = true,
	P0 = new Vector2(32, 32), P1 = new Vector2(32, 32),   // center, focal == center
	RadiusX = 30f, RadiusY = 15f,
	Colors = new[] { WColor.FromArgb(255, 255, 0, 0), WColor.FromArgb(255, 0, 0, 255) },
	Stops = new[] { 0f, 1f },
	TileMode = GradientTileMode.Clamp,
	LocalMatrix = Matrix3x2.Identity,
};
var p3b = Render(r => r.DrawRect(new Rect(0, 0, 64, 64), radial, false));
// 15px from center along the SHORT (y) axis reaches the ellipse edge (t≈1 → blue); the same 15px along the
// LONG (x) axis is only halfway (t≈0.5 → still reddish). A CIRCULAR gradient would paint both identically.
var yEdge = At(p3b, 32, 47);   // (32, 32+15)
var xMid = At(p3b, 47, 32);    // (32+15, 32)
Check("radial: elliptical short-axis reaches edge (blue)", yEdge.b > 150 && yEdge.r < 100, yEdge);
Check("radial: elliptical long-axis still reddish (not circular)", xMid.r > yEdge.r + 40, (xMid, yEdge));

// 4) GPU image draw — upload a managed BGRA image to a wgpu texture, draw it
var blue = new SolidImage(40, 40, 0, 0, 255);
using var tex = new WebGpuImageTexture(dev, blue);
var p4 = Render(r => r.DrawImage(tex, 12, 12, default, 1f, false));
var im = At(p4, 32, 32); var iout = At(p4, 2, 2);
Check("image: center blue", im.b > 200 && im.r < 60 && im.g < 60, im);
Check("image: outside black", iout.r < 40 && iout.g < 40 && iout.b < 40, iout);

// 4b) TINTED IMAGE — a SrcIn blend-mode color filter tints a white image blue.
var white = new SolidImage(40, 40, 255, 255, 255);
using var wtex = new WebGpuImageTexture(dev, white);
var tintFilter = new WebGpuColorFilter { IsBlendMode = true, Color = WColor.FromArgb(255, 0, 0, 255), Mode = BlendMode.SrcIn };
var p4b = Render(r => r.DrawImage(wtex, 12, 12, default, tintFilter, false));
var tp = At(p4b, 32, 32); var tpout = At(p4b, 2, 2);
Check("tinted-image: white tinted blue (SrcIn) → blue", tp.b > 200 && tp.r < 60 && tp.g < 60, tp);
Check("tinted-image: outside untouched (black)", tpout.r < 40 && tpout.g < 40 && tpout.b < 40, tpout);

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

// 7b) ROUNDED-RECT CLIP — a full-surface draw clipped by a rounded rect must leave the CORNERS unpainted
//     (the old AABB-scissor clip painted them). Center stays inside → red.
var rr = new RoundRectangle
{
	Rect = new Rect(8, 8, 48, 48),
	TopLeft = new Vector2(16, 16), TopRight = new Vector2(16, 16),
	BottomRight = new Vector2(16, 16), BottomLeft = new Vector2(16, 16),
};
var prr = Render(r => { r.ClipRoundRect(rr); r.DrawRect(new Rect(0, 0, 64, 64), red, false); });
var rrCenter = At(prr, 32, 32);   // deep inside → red
var rrCorner = At(prr, 10, 10);   // inside the 8..56 AABB but outside the r=16 corner → masked (black)
Check("rounded-clip: center painted (red)", rrCenter.r > 200 && rrCenter.g < 60, rrCenter);
Check("rounded-clip: corner masked (black)", rrCorner.r < 40 && rrCorner.g < 40 && rrCorner.b < 40, rrCorner);

// 8) CROSS-BACKEND AGREEMENT — render the SAME neutral scene (black bg + green managed-geometry triangle)
//    through the Skia backend and the WebGPU backend, and assert both classify every unambiguous pixel the same.
var black = WColor.FromArgb(255, 0, 0, 0);
Uno.UI.Composition.Skia.SkiaBackend.Register();
var skImg = DrawingFactory.Current.RenderOffscreen(64, 64, s =>
{
	s.DrawRect(new Rect(0, 0, 64, 64), black, false);
	s.DrawPath(tri, green, false);
});
var skBgra = new byte[64 * 64 * 4];
skImg.CopyPixels(skBgra);
var wg = Render(r => r.DrawPath(tri, green, false));

// Classify a pixel as 'G' (green), 'K' (black), or '?' (ambiguous/AA edge). Skia readback is BGRA, WebGPU is RGBA.
char ClassSkia(int i) { int r = skBgra[i + 2], g = skBgra[i + 1], b = skBgra[i]; return (g > 150 && r < 60 && b < 60) ? 'G' : (r < 40 && g < 40 && b < 40) ? 'K' : '?'; }
char ClassWgpu(int i) { int r = wg[i], g = wg[i + 1], b = wg[i + 2]; return (g > 150 && r < 60 && b < 60) ? 'G' : (r < 40 && g < 40 && b < 40) ? 'K' : '?'; }
int compared = 0, disagree = 0;
for (int y = 0; y < 64; y += 4)
{
	for (int x = 0; x < 64; x += 4)
	{
		int i = (y * 64 + x) * 4;
		char cs = ClassSkia(i), cw = ClassWgpu(i);
		if (cs == '?' || cw == '?') { continue; }   // skip AA-boundary pixels (rasterizers differ there)
		compared++;
		if (cs != cw) { disagree++; }
	}
}
Check($"cross-backend: Skia vs WebGPU agree on {compared} sampled pixels", compared > 100 && disagree == 0, $"{disagree} disagreements / {compared}");

// 9) TEXT — glyph run → neutral IGeometry outline (IFont.BuildGlyphRunOutline) → filled through BOTH backends.
//    Exercises the font seam + fill rule (the 'A' counter must be a hole), end-to-end with zero Skia on the WebGPU side.
var font = FontProvider.Current.GetDefaultFont(new FontWeight(400), FontStretch.Normal, FontStyle.Normal, 48f);
const string text = "A";
var glyphs = new ushort[text.Length];
var gpos = new Vector2[text.Length];
float penX = 8;
for (int i = 0; i < text.Length; i++) { var gi = font.GetGlyphIndex(text[i]); glyphs[i] = gi; gpos[i] = new Vector2(penX, 0); penX += font.GetGlyphAdvance(gi); }
using var textGeom = font.BuildGlyphRunOutline(glyphs, gpos, 50f);

var skText = new byte[64 * 64 * 4];
DrawingFactory.Current.RenderOffscreen(64, 64, s => { s.DrawRect(new Rect(0, 0, 64, 64), black, false); s.DrawPath(textGeom, green, false); }).CopyPixels(skText);
var wgText = Render(r => r.DrawPath(textGeom, green, false));

int wgGreen = 0, tCompared = 0, tDisagree = 0;
for (int i = 0; i < 64 * 64; i++)
{
	int b = i * 4;
	char cs = (skText[b + 2] < 60 && skText[b + 1] > 150 && skText[b] < 60) ? 'G' : (skText[b + 2] < 40 && skText[b + 1] < 40 && skText[b] < 40) ? 'K' : '?';
	char cw = (wgText[b] < 60 && wgText[b + 1] > 150 && wgText[b + 2] < 60) ? 'G' : (wgText[b] < 40 && wgText[b + 1] < 40 && wgText[b + 2] < 40) ? 'K' : '?';
	if (cw == 'G') { wgGreen++; }
	if (cs == '?' || cw == '?') { continue; }
	tCompared++; if (cs != cw) { tDisagree++; }
}
Check("text: WebGPU rendered the glyph (non-empty)", wgGreen > 20, $"{wgGreen} green px");
Check($"text: Skia vs WebGPU agree on {tCompared} glyph pixels (incl. fill-rule counter)", tCompared > 500 && tDisagree == 0, $"{tDisagree} disagreements / {tCompared}");

// Render a neutral geometry green-on-black through BOTH backends and assert they classify every unambiguous pixel the same.
void CrossCheck(string name, IGeometry g)
{
	var sk = new byte[64 * 64 * 4];
	DrawingFactory.Current.RenderOffscreen(64, 64, s => { s.DrawRect(new Rect(0, 0, 64, 64), black, false); s.DrawPath(g, green, false); }).CopyPixels(sk);
	var w = Render(r => r.DrawPath(g, green, false));
	int cmp = 0, dis = 0;
	for (int i = 0; i < 64 * 64; i++)
	{
		int b = i * 4;
		char cs = (sk[b + 2] < 60 && sk[b + 1] > 150 && sk[b] < 60) ? 'G' : (sk[b + 2] < 40 && sk[b + 1] < 40 && sk[b] < 40) ? 'K' : '?';
		char cw = (w[b] < 60 && w[b + 1] > 150 && w[b + 2] < 60) ? 'G' : (w[b] < 40 && w[b + 1] < 40 && w[b + 2] < 40) ? 'K' : '?';
		if (cs == '?' || cw == '?') { continue; }
		cmp++; if (cs != cw) { dis++; }
	}
	Check($"{name}: Skia vs WebGPU agree ({cmp} px)", cmp > 150 && dis == 0, $"{dis} disagreements / {cmp}");
}

// 10) STROKE — managed IGeometry.GetStrokeFillGeometry rendered by both backends
var spb = new ManagedDrawingFactory().CreatePathBuilder();
spb.MoveTo(new Vector2(10, 12)); spb.LineTo(new Vector2(54, 32)); spb.LineTo(new Vector2(10, 52));
using var poly = spb.Build();
using var stroked = poly.GetStrokeFillGeometry(new StrokeStyle { Thickness = 9f, LineJoin = StrokeJoin.Miter, MiterLimit = 10f });
CrossCheck("stroke", stroked);

// 11) BOOLEAN COMBINE — managed IGeometry.Combine(Union) rendered by both backends
IGeometry Rect(float x0, float y0, float x1, float y1)
{
	var b = new ManagedDrawingFactory().CreatePathBuilder();
	b.MoveTo(new Vector2(x0, y0)); b.LineTo(new Vector2(x1, y0)); b.LineTo(new Vector2(x1, y1)); b.LineTo(new Vector2(x0, y1)); b.Close();
	return b.Build();
}
using var rA = Rect(12, 12, 40, 40);
using var rB = Rect(26, 26, 54, 54);
using var union = rA.Combine(rB, GeometryCombineMode.Union);
CrossCheck("combine-union", union);

// 12) DROP SHADOW — a blurred, tinted silhouette (offscreen coverage → separable gaussian → SrcIn composite).
using var shRect = Rect(20, 20, 44, 44);   // (20,20)-(44,44), edge at x=44
var pShadow = Render(r => r.DrawShadow(shRect, WColor.FromArgb(255, 255, 0, 0), 4f, 4f, false));
var shIn = At(pShadow, 32, 32);     // inside silhouette → full coverage → red
var shEdge = At(pShadow, 47, 32);   // ~3px past the edge → blur falloff → partial red
var shFar = At(pShadow, 62, 32);    // far outside → ~0
Check("shadow: inside silhouette red", shIn.r > 150 && shIn.g < 80 && shIn.b < 80, shIn);
Check("shadow: blur falloff outside (partial red)", shEdge.r > 20 && shEdge.r < shIn.r, (shEdge, shIn));
Check("shadow: far outside black", shFar.r < 40, shFar);

// 13) MASK LAYER — SaveLayer() source + SaveLayer(DstIn) mask: source survives only where the mask is opaque.
var pMask = Render(r =>
{
	r.SaveLayer();                                                      // L1 source
	r.DrawRect(new Rect(0, 0, 64, 64), red, false);                    // red everywhere
	r.SaveLayer(BlendMode.DstIn);                                       // L2 mask (DstIn)
	r.DrawRect(new Rect(16, 16, 32, 32), WColor.FromArgb(255, 255, 255, 255), false); // white center
	r.Restore();                                                        // mask DstIn onto source
	r.Restore();                                                        // source onto frame
});
var mIn = At(pMask, 32, 32); var mOut = At(pMask, 4, 4);
Check("mask-layer: inside mask red", mIn.r > 200 && mIn.g < 60, mIn);
Check("mask-layer: outside mask black", mOut.r < 40 && mOut.g < 40 && mOut.b < 40, mOut);

// 14) COLOR-FILTER LAYER — SaveLayer(IColorFilter) with a color matrix mapping red→green at composite.
var swapMatrix = new float[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 };
var pCf = Render(r =>
{
	r.SaveLayer(new WebGpuColorFilter { Matrix = swapMatrix });
	r.DrawRect(new Rect(0, 0, 64, 64), red, false);
	r.Restore();
});
var cf = At(pCf, 32, 32);
Check("colorfilter-layer: red mapped to green", cf.g > 200 && cf.r < 60, cf);

// 15) EFFECT-SHADOW LAYER — SaveLayer(IEffectFilter): a drop shadow derived from the layer content.
var dropShadow = new WebGpuEffectFilter { Dx = 10, Dy = 10, SigmaX = 3, SigmaY = 3, Color = WColor.FromArgb(255, 0, 0, 255) };
var pFx = Render(r =>
{
	r.SaveLayer(dropShadow);
	r.DrawRect(new Rect(12, 12, 20, 20), WColor.FromArgb(255, 0, 255, 0), false);   // green content (12,12)-(32,32)
	r.Restore();
});
var fxContent = At(pFx, 20, 20);   // content on top → green
var fxShadow = At(pFx, 40, 40);    // offset by (10,10) → blurred blue shadow
var fxEmpty = At(pFx, 2, 2);       // far → black
Check("effect-shadow: content on top (green)", fxContent.g > 150 && fxContent.b < 100, fxContent);
Check("effect-shadow: shadow offset present (blue)", fxShadow.b > 40 && fxShadow.g < 150, fxShadow);
Check("effect-shadow: empty area black", fxEmpty.r < 40 && fxEmpty.g < 40 && fxEmpty.b < 40, fxEmpty);

// 16) BACKDROP / ACRYLIC — the content behind is captured, gaussian-blurred, and redrawn in the effect region.
var acrylic = new WebGpuEffectFilter { SigmaX = 5, SigmaY = 5, Color = WColor.FromArgb(0, 0, 0, 0) };
var pBd = Render(r =>
{
	r.DrawRect(new Rect(24, 24, 16, 16), red, false);   // sharp red block (24,24)-(40,40)
	r.ClipRect(new Rect(0, 0, 64, 64));                 // effect region
	r.DrawEffectBackdrop(acrylic, 1f);
});
var bdCenter = At(pBd, 32, 32);   // blurred red center → still red-ish
var bdEdge = At(pBd, 45, 32);     // 5px past the sharp edge → blur spread → partial red (was black)
Check("backdrop: center still red-ish (blurred)", bdCenter.r > 80, bdCenter);
Check("backdrop: blur spreads content past the edge", bdEdge.r > 15 && bdEdge.r < bdCenter.r, (bdEdge, bdCenter));

// 17) EXACT PATH CLIP — ClipPath(triangle) masks a full-surface draw to the triangle (not its bounding box).
var clipTri = new ManagedDrawingFactory().CreatePathBuilder();
clipTri.MoveTo(new Vector2(32, 8)); clipTri.LineTo(new Vector2(56, 56)); clipTri.LineTo(new Vector2(8, 56)); clipTri.Close();
using var clipTriGeom = clipTri.Build();
var pClipPath = Render(r => { r.ClipPath(clipTriGeom); r.DrawRect(new Rect(0, 0, 64, 64), red, false); });
var cpIn = At(pClipPath, 32, 40);   // inside triangle → red
var cpCorner = At(pClipPath, 6, 10); // inside the bbox but outside the triangle → clipped (black)
Check("clip-path: inside triangle red", cpIn.r > 200 && cpIn.g < 60, cpIn);
Check("clip-path: outside triangle (in bbox) black", cpCorner.r < 40 && cpCorner.g < 40 && cpCorner.b < 40, cpCorner);

// 18) RETAINED RECORDING (per-visual GPU cache) — record a rect once, replay it at two offsets in one frame,
//     then again in a second frame (cache reuse). Renders through the ReplayRef + persistent-geometry path.
byte[] RenderRetained(IRenderData child, Action<WebGpuCommandRecorder> frame)
{
	var surface = new WebGpuRenderSurface(dev, 64, 64);
	var rec = new WebGpuCommandRecorder();
	frame(rec);
	var present = new WebGpuPresentSession(dev, surface);
	present.Clear(WColor.FromArgb(255, 0, 0, 0));
	present.Replay(rec.Finish());
	return dev.ReadPixelsRgba(surface);
}
var childRec = new WebGpuCommandRecorder();
childRec.DrawRect(new Rect(0, 0, 16, 16), red, false);   // recorded at identity
var childData = childRec.Finish();
Action<WebGpuCommandRecorder> twoOffsets = r =>
{
	r.Save(); r.Translate(8, 8); r.Replay(childData); r.Restore();     // rect -> (8,8)-(24,24)
	r.Save(); r.Translate(40, 40); r.Replay(childData); r.Restore();   // rect -> (40,40)-(56,56)
};
var pRet1 = RenderRetained(childData, twoOffsets);
var pRet2 = RenderRetained(childData, twoOffsets);   // second frame: cache reuse
foreach (var (label, px) in new[] { ("frame1", pRet1), ("frame2", pRet2) })
{
	var a = At(px, 16, 16); var b = At(px, 48, 48); var mid = At(px, 32, 32);
	Check($"retained {label}: first replay red", a.r > 200 && a.g < 60, a);
	Check($"retained {label}: second replay red", b.r > 200 && b.g < 60, b);
	Check($"retained {label}: gap black", mid.r < 40 && mid.g < 40 && mid.b < 40, mid);
}

// 19) DRAW COALESCING — three same-clip rects in one frame coalesce into a single draw; all must render.
var pCo = Render(r =>
{
	r.DrawRect(new Rect(4, 4, 12, 12), red, false);
	r.DrawRect(new Rect(26, 26, 12, 12), WColor.FromArgb(255, 0, 255, 0), false);
	r.DrawRect(new Rect(48, 48, 12, 12), WColor.FromArgb(255, 0, 0, 255), false);
});
var co1 = At(pCo, 10, 10); var co2 = At(pCo, 32, 32); var co3 = At(pCo, 54, 54);
Check("coalesce: rect 1 red", co1.r > 200 && co1.g < 60, co1);
Check("coalesce: rect 2 green", co2.g > 200 && co2.r < 60, co2);
Check("coalesce: rect 3 blue", co3.b > 200 && co3.r < 60, co3);

// 20) SKIA-LESS DRAWING PATH — WebGpuDrawingFactory paired with the managed geometry engine: managed geometry
//     (CreatePathBuilder) + a WebGPU gradient shader + WebGPU offscreen rasterization (RenderOffscreen), all
//     without SkiaSharp. Proves a WebGPU app can link zero Skia for its drawing.
var skiaLess = new WebGpuDrawingFactory(dev, new ManagedDrawingFactory());
var slPath = skiaLess.CreatePathBuilder();                       // managed geometry, no Skia
slPath.MoveTo(new Vector2(32, 4)); slPath.LineTo(new Vector2(60, 60)); slPath.LineTo(new Vector2(4, 60)); slPath.Close();
using var slTri = slPath.Build();
var slColors = new[] { WColor.FromArgb(255, 255, 0, 0), WColor.FromArgb(255, 0, 0, 255) };
var slStops = new[] { 0f, 1f };
var slGrad = skiaLess.CreateLinearGradientShader(new Vector2(0, 0), new Vector2(64, 0), slColors, slStops, GradientTileMode.Clamp, Matrix3x2.Identity);
var slImg = skiaLess.RenderOffscreen(64, 64, s =>                // WebGPU offscreen, no Skia
{
	s.DrawRect(new Rect(0, 0, 64, 64), slGrad, false);          // gradient background (WebGpuShader)
	s.DrawPath(slTri, WColor.FromArgb(255, 0, 255, 0), false);  // green triangle on top
});
var slBgra = new byte[64 * 64 * 4];
slImg.CopyPixels(slBgra);                                        // BGRA: [B, G, R, A]
int SL(int x, int y) => (y * 64 + x) * 4;
int slInside = SL(32, 40), slCorner = SL(3, 6);
Check("skia-less: triangle interior green", slBgra[slInside + 1] > 150 && slBgra[slInside] < 60 && slBgra[slInside + 2] < 60, (slBgra[slInside], slBgra[slInside + 1], slBgra[slInside + 2]));
Check("skia-less: gradient corner reddish", slBgra[slCorner + 2] > 150 && slBgra[slCorner] < 100, (slBgra[slCorner], slBgra[slCorner + 1], slBgra[slCorner + 2]));

Console.WriteLine(fail == 0
	? "\nALL PASS — non-Skia render seam verified headless (primitives + text + stroke + boolean); Skia vs WebGPU agree on every neutral scene"
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
