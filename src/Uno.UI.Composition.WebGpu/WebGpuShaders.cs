// The WGSL the backend compiles. ClipStructFn is prepended to most of the others (see Module call sites): it
// declares the shared ClipU binding and the coverage helpers every colour-writing shader calls.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuDevice
{
	/// <summary>
	/// Bytes in the image/op uniform block (28 floats), as declared by <see cref="ImageWgsl"/>. The pipeline
	/// layout's MinBindingSize, every bind group's entry size and the writer all have to agree: a bind group that
	/// disagrees with the layout is rejected at draw time, and one that disagrees with the struct reads garbage.
	/// </summary>
	public const int ImageUniformBytes = 112;

	/// <summary>
	/// Bytes in the composite uniform block (24 floats: opacity plus a 4x5 colour matrix), as declared by
	/// <see cref="CompositeWgsl"/> and <see cref="CompositeBlendWgsl"/>. Same agreement requirement as above.
	/// </summary>
	public const int CompositeUniformBytes = 96;

	// Prepended to every colour-writing shader. The uniform is a parameter because each shader declares the binding
	// at its own contiguous group index (colored group 0, image/gradient group 1); a group hole is rejected by
	// wgpu's auto-layout.
	private const string ClipStructFn = @"
// rects[i]/radii[i] are the nested rounded-rect clips (device space), ANDed together; ex[i]>0.5 = Difference
// (keep outside). meta.x = active count. Arbitrary path clips are applied via the shared depth buffer as an in-pass
// mask (see the main-pass clip protocol), not sampled here — so clipCov only carries the analytic rounded-rects.
// radii = per-corner X radius (TL,TR,BR,BL); radiiY = per-corner Y radius (elliptical corners; == radii for circular).
struct ClipU { rects: array<vec4<f32>, 4>, radii: array<vec4<f32>, 4>, ex: vec4<f32>, ctrl: vec4<f32>, size: vec4<f32>, xform: vec4<f32>, xoff: vec4<f32>, finv: vec4<f32>, radiiY: array<vec4<f32>, 4> };
// Arena transform: verts are stored in the recording's own (identity-baked) NDC space; xform (an NDC->NDC affine,
// M = xform.xyzw = [m00 m01 m10 m11], t = xoff.xy) maps them to the replay transform. Identity for immediate draws;
// re-stamped (a single uniform write) when a cached visual moves, so its geometry is reused, not rebuilt.
fn xformPos(clip: ClipU, pos: vec2<f32>) -> vec4<f32> {
  return vec4<f32>(clip.xform.x * pos.x + clip.xform.y * pos.y + clip.xoff.x,
                   clip.xform.z * pos.x + clip.xform.w * pos.y + clip.xoff.y, 0.0, 1.0);
}
// Maps a (moved) device fragment position back to the recording's own space so device-space fragment inputs (clip
// shape, gradient geometry) baked at identity stay correct after an arena transform re-stamp. Identity = no-op.
fn finvMap(clip: ClipU, fcRaw: vec2<f32>) -> vec2<f32> {
  return vec2<f32>(clip.finv.x * fcRaw.x + clip.finv.z * fcRaw.y + clip.xoff.z,
                   clip.finv.y * fcRaw.x + clip.finv.w * fcRaw.y + clip.xoff.w);
}
// Coverage of one rounded-rect clip (rl = L,T,R,B; rad4 = per-corner radii; ex>0.5 = Difference/keep-outside).
fn roundCov(fc: vec2<f32>, rl: vec4<f32>, radX: vec4<f32>, radY: vec4<f32>, ex: f32) -> f32 {
  let c = vec2<f32>((rl.x + rl.z) * 0.5, (rl.y + rl.w) * 0.5);
  let h = vec2<f32>((rl.z - rl.x) * 0.5, (rl.w - rl.y) * 0.5);
  let lp = fc - c;
  let rx = select(select(radX.x, radX.y, lp.x > 0.0), select(radX.w, radX.z, lp.x > 0.0), lp.y > 0.0);
  let ry = select(select(radY.x, radY.y, lp.x > 0.0), select(radY.w, radY.z, lp.x > 0.0), lp.y > 0.0);
  let r = vec2<f32>(rx, ry);
  // Elliptical corner via a first-order (gradient-normalised) implicit-ellipse distance. Degenerates EXACTLY to the
  // circular rounded-box SDF when rx == ry (and to a sharp box when r == 0), so circular clips are unchanged.
  let q = abs(lp) - h + r;
  let outside = max(q, vec2<f32>(0.0, 0.0));
  let rg = max(r, vec2<f32>(1e-6, 1e-6));
  let e = outside / rg;
  let el = length(e);
  let grad = length(outside / (rg * rg)) / max(el, 1e-6);
  let dCorner = (el - 1.0) / max(grad, 1e-6);
  let d = min(max(q.x, q.y), 0.0) + dCorner;
  let rr = clamp(0.5 - d, 0.0, 1.0);
  return select(rr, 1.0 - rr, ex > 0.5);
}
fn clipCov(fcRaw: vec2<f32>, clip: ClipU) -> f32 {
  // Fast path: no clip => full coverage, and NO finvMap (unclipped fragments must cost what they did pre-arena).
  let n = i32(clip.ctrl.x);
  if (n == 0 && clip.ctrl.y < 0.5) { return 1.0; }
  return clipCovMapped(finvMap(clip, fcRaw), clip);
}
// Same, for a caller that already mapped the fragment into the clip's space — the gradient shader needs that
// point anyway, and mapping it twice per fragment is a matrix multiply wasted on every pixel it covers.
fn clipCovMapped(fc: vec2<f32>, clip: ClipU) -> f32 {
  let n = i32(clip.ctrl.x);
  // Dedicated plain-rect clip (ctrl.y flag; min in ctrl.zw, max in size.zw): carries the clip's AABB analytically
  // so the per-op device SCISSOR is cull-only and the emit collapses SetScissorRect calls (see AabbInClipU).
  var cov = 1.0;
  if (clip.ctrl.y > 0.5) {
    let dmin = fc - vec2<f32>(clip.ctrl.z, clip.ctrl.w);
    let dmax = vec2<f32>(clip.size.z, clip.size.w) - fc;
    cov = clamp(0.5 + min(min(dmin.x, dmin.y), min(dmax.x, dmax.y)), 0.0, 1.0);
  }
  if (n == 0) { return cov; }
  // Unrolled with STATIC array indices (n is 1..4). A dynamic uniform-array index (clip.rects[i]) is a GPU perf
  // cliff on some drivers; the common single-clip case (n==1) must not cost more than one rect test.
  cov = cov * roundCov(fc, clip.rects[0], clip.radii[0], clip.radiiY[0], clip.ex.x);
  if (n > 1) { cov = cov * roundCov(fc, clip.rects[1], clip.radii[1], clip.radiiY[1], clip.ex.y); }
  if (n > 2) { cov = cov * roundCov(fc, clip.rects[2], clip.radii[2], clip.radiiY[2], clip.ex.z); }
  if (n > 3) { cov = cov * roundCov(fc, clip.rects[3], clip.radii[3], clip.radiiY[3], clip.ex.w); }
  return cov;
}
";
	// Averages each 4x4 block of the supersampled mask into one coverage value. Reads with textureLoad so no
	// sampler or filtering is involved — the average must be exact, not bilinear.
	private const string MaskDownsampleWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@group(0) @binding(0) var src: texture_2d<f32>;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) uv: vec2<f32>) -> VOut {
  var o: VOut; o.p = vec4<f32>(pos, 0.0, 1.0); o.uv = uv; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> {
  let dims = vec2<f32>(textureDimensions(src));
  let base = vec2<i32>(floor(i.uv * dims / 4.0)) * 4;
  var a = 0.0;
  for (var y = 0; y < 4; y = y + 1) {
    for (var x = 0; x < 4; x = x + 1) {
      a = a + textureLoad(src, base + vec2<i32>(x, y), 0).a;
    }
  }
  a = a / 16.0;
  return vec4<f32>(a, a, a, a);
}";
	private const string ColoredWgsl = @"
@group(0) @binding(0) var<uniform> clip: ClipU;
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>) -> VOut {
  var o: VOut; o.p = xformPos(clip, pos); o.c = col; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return vec4<f32>(i.c.rgb, i.c.a * clipCov(i.p.xy, clip)); }";
	// Stencil pass (winding only, colour masked). Binds the SHARED ClipU at group 0 for the arena vertex xform so a
	// moved path's fan follows the re-stamped transform; identity for immediate/non-arena draws (fan already NDC).
	private const string PosOnlyWgsl = @"
@group(0) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return xformPos(clip, pos); }
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";
	// TRANSFORM-TABLE variants (path fills only). Vertices are recorded-DEVICE space + a per-vertex slot index into
	// a read-only storage buffer of local->NDC affines (a=ax,ay,az,aw  b=bx,by,_,_) that fold the replay transform
	// AND the device->NDC projection. Recomputing a (tiny) entry per frame repositions a moved/resized visual without
	// re-baking or re-tessellating its fan — so a scroll or a window resize touches only the table, not the verts.
	private const string StencilTableWgsl = @"
struct Xf { a: vec4<f32>, b: vec4<f32> };
@group(0) @binding(0) var<storage, read> xf: array<Xf>;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) ti: u32) -> @builtin(position) vec4<f32> {
  let t = xf[ti];
  return vec4<f32>(pos.x * t.a.x + pos.y * t.a.y + t.a.z, pos.x * t.a.w + pos.y * t.b.x + t.b.y, 0.0, 1.0);
}
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";
	private const string CoverTableWgsl = @"
struct Xf { a: vec4<f32>, b: vec4<f32> };
@group(0) @binding(0) var<storage, read> xf: array<Xf>;
@group(1) @binding(0) var<uniform> clip: ClipU;
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>, @location(2) ti: u32) -> VOut {
  let t = xf[ti];
  var o: VOut; o.p = vec4<f32>(pos.x * t.a.x + pos.y * t.a.y + t.a.z, pos.x * t.a.w + pos.y * t.b.x + t.b.y, 0.0, 1.0); o.c = col; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return vec4<f32>(i.c.rgb, i.c.a * clipCov(i.p.xy, clip)); }";
	// Fullscreen-triangle depth writers for the in-pass path-clip mask. vs0/vs1 emit the tri at z=0/z=1; the
	// fragment writes nothing (colour masked off) — only depth (and, for the cover variants, the stencil reset).
	private const string ClipDepthWgsl = @"
@vertex fn vs0(@builtin(vertex_index) vi: u32) -> @builtin(position) vec4<f32> {
  var p = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  return vec4<f32>(p[vi], 0.0, 1.0);
}
@vertex fn vs1(@builtin(vertex_index) vi: u32) -> @builtin(position) vec4<f32> {
  var p = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  return vec4<f32>(p[vi], 1.0, 1.0);
}
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";
	// Composites a full-size layer texture into an MSAA pass. SrcOver for plain/opacity/colorfilter layers,
	// DstIn (out = dst * src.a) for mask layers. Optional color matrix (params.x) applied to the layer content.
	private const string CompositeWgsl = @"
struct CU { params: vec4<f32>, m0: vec4<f32>, m1: vec4<f32>, m2: vec4<f32>, m3: vec4<f32>, off: vec4<f32> };
@group(0) @binding(0) var src: texture_2d<f32>;
// No sampler: the fragment shader fetches exact texels (textureLoad). Declaring one anyway would be dropped
// from the derived bind group layout as unused, and the C# side would then bind against a layout without it.
@group(0) @binding(2) var<uniform> u: CU;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  // Exact texel fetch, not a filtered sample: the quad is a pixel-aligned fullscreen triangle over a
  // target-sized layer texture, so a bilinear sample would fetch and blend 4 texels to reproduce 1.
  // (`smp` stays bound — the layout is shared.)
  // params.z = size-to-content layer: src covers only the layer's sub-rect, at m0.xy in this target with
  // size m0.zw, so shift into it; outside it the layer contributes nothing (transparent SrcOver = no change).
  var lp = vec2<i32>(i.p.xy);
  if (u.params.z > 0.5) {
    lp = vec2<i32>(i.p.xy - u.m0.xy);
    if (lp.x < 0 || lp.y < 0 || lp.x >= i32(u.m0.z) || lp.y >= i32(u.m0.w)) { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }
  }
  var c = textureLoad(src, lp, 0);   // premultiplied layer content
  if (u.params.x > 0.5) {
    var s = c;
    if (c.a > 0.0) { s = vec4<f32>(c.rgb / c.a, c.a); }
    let r = vec4<f32>(dot(u.m0, s) + u.off.x, dot(u.m1, s) + u.off.y, dot(u.m2, s) + u.off.z, dot(u.m3, s) + u.off.w);
    let rc = clamp(r, vec4<f32>(0.0), vec4<f32>(1.0));
    c = vec4<f32>(rc.rgb * rc.a, rc.a);
  }
  return c * u.params.y;
}";
	private const string CompositeBlendWgsl = @"
struct CU { params: vec4<f32>, m0: vec4<f32>, m1: vec4<f32>, m2: vec4<f32>, m3: vec4<f32>, off: vec4<f32> };
@group(0) @binding(0) var src: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: CU;
@group(0) @binding(3) var dst: texture_2d<f32>;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
fn lum(c: vec3<f32>) -> f32 { return dot(c, vec3<f32>(0.3, 0.59, 0.11)); }
fn clipColor(c: vec3<f32>) -> vec3<f32> {
  let l = lum(c); let n = min(c.r, min(c.g, c.b)); let x = max(c.r, max(c.g, c.b));
  var r = c;
  if (n < 0.0) { r = l + (c - l) * l / max(l - n, 1e-6); }
  if (x > 1.0) { r = l + (r - l) * (1.0 - l) / max(x - l, 1e-6); }
  return r;
}
fn setLum(c: vec3<f32>, l: f32) -> vec3<f32> { return clipColor(c + (l - lum(c))); }
fn sat(c: vec3<f32>) -> f32 { return max(c.r, max(c.g, c.b)) - min(c.r, min(c.g, c.b)); }
fn setSat(c: vec3<f32>, s: f32) -> vec3<f32> {
  let mn = min(c.r, min(c.g, c.b)); let mx = max(c.r, max(c.g, c.b));
  if (mx > mn) { return (c - mn) * s / (mx - mn); }
  return vec3<f32>(0.0);
}
fn bsep(cb: f32, cs: f32, mode: i32) -> f32 {
  if (mode == 4)  { return cb * cs; }                                                   // Multiply
  if (mode == 13) { return cb + cs - cb * cs; }                                         // Screen
  if (mode == 14) { return min(cb, cs); }                                               // Darken
  if (mode == 15) { return max(cb, cs); }                                               // Lighten
  if (mode == 21) { return abs(cb - cs); }                                              // Difference
  if (mode == 22) { return cb + cs - 2.0 * cb * cs; }                                   // Exclusion
  if (mode == 16) { if (cb >= 1.0) { return 1.0; } if (cs <= 0.0) { return 0.0; } return 1.0 - min(1.0, (1.0 - cb) / cs); } // ColorBurn
  if (mode == 17) { if (cb <= 0.0) { return 0.0; } if (cs >= 1.0) { return 1.0; } return min(1.0, cb / (1.0 - cs)); }       // ColorDodge
  if (mode == 18) { if (cb <= 0.5) { return 2.0 * cb * cs; } return 1.0 - 2.0 * (1.0 - cb) * (1.0 - cs); }                  // Overlay = HardLight(cs,cb)
  if (mode == 20) { if (cs <= 0.5) { return 2.0 * cs * cb; } return 1.0 - 2.0 * (1.0 - cs) * (1.0 - cb); }                  // HardLight
  if (mode == 19) {                                                                     // SoftLight
    let d = select(((16.0 * cb - 12.0) * cb + 4.0) * cb, sqrt(cb), cb > 0.25);
    if (cs <= 0.5) { return cb - (1.0 - 2.0 * cs) * cb * (1.0 - cb); }
    return cb + (2.0 * cs - 1.0) * (d - cb);
  }
  return cs;
}
fn bnonsep(cb: vec3<f32>, cs: vec3<f32>, mode: i32) -> vec3<f32> {
  if (mode == 23) { return setLum(setSat(cs, sat(cb)), lum(cb)); }  // Hue
  if (mode == 24) { return setLum(setSat(cb, sat(cs)), lum(cb)); }  // Saturation
  if (mode == 25) { return setLum(cs, lum(cb)); }                   // Color
  if (mode == 26) { return setLum(cb, lum(cs)); }                   // Luminosity
  return cs;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  var s = textureSampleLevel(src, smp, i.uv, 0.0);   // premultiplied layer content
  if (u.params.x > 0.5) {
    var us = s; if (s.a > 0.0) { us = vec4<f32>(s.rgb / s.a, s.a); }
    let r = vec4<f32>(dot(u.m0, us) + u.off.x, dot(u.m1, us) + u.off.y, dot(u.m2, us) + u.off.z, dot(u.m3, us) + u.off.w);
    let rc = clamp(r, vec4<f32>(0.0), vec4<f32>(1.0));
    s = vec4<f32>(rc.rgb * rc.a, rc.a);
  }
  s = s * u.params.y;                                 // opacity (premultiplied)
  let d = textureSampleLevel(dst, smp, i.uv, 0.0);    // premultiplied destination
  let sa = s.a; let da = d.a; let mode = i32(u.params.z + 0.5);
  // Porter-Duff operators (B = source): co = Fa*Sca + Fb*Dca, ao = Fa*sa + Fb*da.
  var fa = 1.0; var fb = 1.0 - sa; var pd = true;
  if (mode == 0)       { fa = 1.0;      fb = 1.0 - sa; }   // SrcOver
  else if (mode == 1)  { fa = 1.0;      fb = 0.0; }        // Src
  else if (mode == 2)  { fa = 1.0;      fb = 1.0; }        // Plus
  else if (mode == 5)  { fa = 0.0;      fb = sa; }         // DstIn
  else if (mode == 6)  { fa = 0.0;      fb = 1.0 - sa; }   // DstOut
  else if (mode == 7)  { fa = da;       fb = 0.0; }        // SrcIn
  else if (mode == 8)  { fa = 1.0 - da; fb = 1.0; }        // DstOver
  else if (mode == 9)  { fa = 1.0 - da; fb = 0.0; }        // SrcOut
  else if (mode == 10) { fa = da;       fb = 1.0 - sa; }   // SrcATop
  else if (mode == 11) { fa = 1.0 - da; fb = sa; }         // DstATop
  else if (mode == 12) { fa = 1.0 - da; fb = 1.0 - sa; }   // Xor
  else { pd = false; }
  if (pd) {
    let co = fa * s.rgb + fb * d.rgb;
    return vec4<f32>(co, fa * sa + fb * da);
  }
  // Blend modes: source-over coverage with a per-mode blend function on un-premultiplied colours.
  let cs = select(vec3<f32>(0.0), s.rgb / sa, sa > 0.0);
  let cb = select(vec3<f32>(0.0), d.rgb / da, da > 0.0);
  var bl: vec3<f32>;
  if (mode >= 23) { bl = bnonsep(cb, cs, mode); }
  else { bl = vec3<f32>(bsep(cb.r, cs.r, mode), bsep(cb.g, cs.g, mode), bsep(cb.b, cs.b, mode)); }
  let co = (1.0 - da) * s.rgb + (1.0 - sa) * d.rgb + sa * da * bl;
  return vec4<f32>(co, sa + da * (1.0 - sa));
}";
	// Two-texture combine: out = k.x*A + k.y*B + k.z*(A*B) + k.w (premultiplied, clamped) — covers CrossFade
	// (k=(1-w,w,0,0)) and ArithmeticComposite (A=fg,B=bg, k=(s1,s2,m,off)). flag.x>0.5 = AlphaMask: A masked by B's alpha.
	private const string EffectCombineWgsl = @"
struct KU { k: vec4<f32>, flag: vec4<f32> };
@group(0) @binding(0) var a: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: KU;
@group(0) @binding(3) var b: texture_2d<f32>;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  let ca = textureSampleLevel(a, smp, i.uv, 0.0);
  let cb = textureSampleLevel(b, smp, i.uv, 0.0);
  if (u.flag.x > 0.5) { return ca * cb.a; }
  let o = u.k.x * ca + u.k.y * cb + u.k.z * (ca * cb) + vec4<f32>(u.k.w);
  return clamp(o, vec4<f32>(0.0), vec4<f32>(1.0));
}";
	// Single-input per-channel colour function on un-premultiplied colour, matching SkiaEffectFuser exactly.
	// params.x = mode (0 = Contrast, 1 = GammaTransfer), params.y = contrast value, params.z = clamp flag.
	// Contrast clamps its INPUT (Skia); Gamma clamps its RESULT. Gamma: per channel amp*pow(abs(c),exp)+off, or c if disabled.
	private const string ColorFuncWgsl = @"
struct FU { params: vec4<f32>, amp: vec4<f32>, exps: vec4<f32>, offs: vec4<f32>, dis: vec4<f32> };
@group(0) @binding(0) var input: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: FU;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  var s = textureSampleLevel(input, smp, i.uv, 0.0);
  let mode = i32(u.params.x + 0.5);
  let clampf = u.params.z > 0.5;
  if (mode == 0) {
    if (clampf) { s = clamp(s, vec4<f32>(0.0), vec4<f32>(1.0)); }
    var rgb = select(vec3<f32>(0.0), s.rgb / s.a, s.a > 0.0);
    let cc = u.params.y; let sp = 1.0 - 0.75 * cc;
    let c2 = sp - 1.0; let b2 = 4.0 - 3.0 * sp; let a2 = 2.0 * c2; let b1 = sp; let a1 = -a2;
    let low = rgb * (rgb * a1 + b1);
    let high = rgb * (rgb * a2 + b2) + c2;
    let comp = select(vec3<f32>(0.0), vec3<f32>(1.0), rgb < vec3<f32>(0.5));
    rgb = mix(low, high, comp);
    return vec4<f32>(rgb * s.a, s.a);
  }
  var c = s; if (s.a > 0.0) { c = vec4<f32>(s.rgb / s.a, s.a); }
  let g = u.amp * pow(abs(c), u.exps) + u.offs;
  c = select(g, c, u.dis > vec4<f32>(0.5));
  var o = vec4<f32>(c.rgb * c.a, c.a);
  if (clampf) { o = clamp(o, vec4<f32>(0.0), vec4<f32>(1.0)); }
  return o;
}";
	// Procedural WhiteNoise generator (no input), matching SkiaEffectFuser's hash + bilinear noise. p.xy=frequency,
	// p.zw=offset, sz.xy=surface size (pixels). coords = pixel position (uv*size).
	private const string EffectNoiseWgsl = @"
struct NU { p: vec4<f32>, sz: vec4<f32> };
@group(0) @binding(0) var<uniform> u: NU;
struct VO { @builtin(position) pos: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.pos = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
fn Hash(p: vec2<f32>) -> f32 { return fract(1e4 * sin(17.0 * p.x + p.y * 0.1) * (0.1 + abs(sin(p.y * 13.0 + p.x)))); }
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  let coords = i.uv * u.sz.xy;
  let coord = coords * 0.81 * u.p.xy + u.p.zw;
  let px00 = floor(coord - 0.5) + 0.5;
  let px11 = px00 + 1.0;
  let px10 = vec2<f32>(px11.x, px00.y);
  let px01 = vec2<f32>(px00.x, px11.y);
  let f = coord - px00;
  let r = mix(mix(Hash(px00), Hash(px10), f.x), mix(Hash(px01), Hash(px11), f.x), f.y);
  return vec4<f32>(r, r, r, 1.0);
}";
	// One separable-gaussian pass over a texture. A fullscreen triangle (from vertex_index, no vertex buffer)
	// samples the source along `dir` with per-tap gaussian weights; radius = ceil(3*sigma). Two passes
	// (dir = (1,0) then (0,1)) give a full 2D blur. Single-sample, no blend (overwrite), no depth/stencil.
	private const string BlurWgsl = @"
// ctrl.x > 0.5 => downsample (single linear tap = box-average the 2x2 source block, one pyramid level). Otherwise a
// separable FIXED 9-tap gaussian (radius 4, sigma~2) — the requested blur radius is achieved by the pyramid DEPTH
// (sigma-scaled downsample levels), not by a sigma-scaled tap count, so cost is constant instead of O(sigma). The
// FIRST (extract) pass remaps into a sub-rect of the source via srcOrigin/srcScale so only the region behind the
// acrylic element is ever processed; gaussian passes run at identity (srcOrigin=0, srcScale=1) on region textures.
struct BU { dir: vec2<f32>, texel: vec2<f32>, ctrl: vec2<f32>, srcOrigin: vec2<f32>, srcScale: vec2<f32> };
@group(0) @binding(0) var src: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> b: BU;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  let suv = b.srcOrigin + i.uv * b.srcScale;
  if (b.ctrl.x > 0.5) { return textureSampleLevel(src, smp, suv, 0.0); }
  let o1 = b.dir * b.texel; let o2 = o1 * 2.0; let o3 = o1 * 3.0; let o4 = o1 * 4.0;
  var sum = textureSampleLevel(src, smp, suv, 0.0) * 0.204164;
  sum = sum + (textureSampleLevel(src, smp, suv + o1, 0.0) + textureSampleLevel(src, smp, suv - o1, 0.0)) * 0.180174;
  sum = sum + (textureSampleLevel(src, smp, suv + o2, 0.0) + textureSampleLevel(src, smp, suv - o2, 0.0)) * 0.123832;
  sum = sum + (textureSampleLevel(src, smp, suv + o3, 0.0) + textureSampleLevel(src, smp, suv - o3, 0.0)) * 0.066282;
  sum = sum + (textureSampleLevel(src, smp, suv + o4, 0.0) + textureSampleLevel(src, smp, suv - o4, 0.0)) * 0.027631;
  return sum;
}";
	// Evaluates a linear/radial gradient per pixel. The quad is positioned in NDC; the fragment uses its
	// framebuffer position (device pixels) so the gradient geometry can be baked to device space at record time.
	private const string GradientWgsl = @"
struct Grad { header: vec4<f32>, geo: vec4<f32>, colors: array<vec4<f32>, 64>, stops: array<vec4<f32>, 16>, origin: vec4<f32> };
@group(0) @binding(0) var<uniform> g: Grad;
@group(1) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return xformPos(clip, pos); }
fn stopAt(i: i32) -> f32 { return g.stops[i / 4][i % 4]; }
@fragment fn fs(@builtin(position) fc: vec4<f32>) -> @location(0) vec4<f32> {
  // Arena: map the device fragment back to the recording's own space so the gradient geometry (baked at identity)
  // is correct after a transform re-stamp. Identity finv => gfc == fc.xy for immediate/non-arena draws.
  let gfc = finvMap(clip, fc.xy);
  var t: f32 = 0.0;
  if (g.header.x < 0.5) {
    let a = g.geo.xy; let b = g.geo.zw; let ab = b - a; let denom = dot(ab, ab);
    if (denom > 0.0) { t = dot(gfc - a, ab) / denom; }
  } else {
    // Radial: map the device delta from the (device-space) center into unit-ellipse space via M — the inverse of
    // the gradient's local->device linear map, per-axis normalized by the local radii. M carries rotation, so a
    // rotated elliptical gradient (and an off-centre focal under rotation) is exact, not axis-aligned-approximate.
    // Two-point-conical solve (matches D2D/Skia): interpolate the circle (focal,r=0)->(center,r=1); t solves
    // |pn - on*(1-t)| = t, i.e. A t^2 + B t + C = 0 with A=|on|^2-1, B=2·(pn-on)·on, C=|pn-on|^2. Handles a focal
    // ORIGIN OUTSIDE the ellipse (A>0): where the focal ray misses the ellipse (disc<0) the pixel is beyond the
    // gradient's reach → clamp to the far color, not a fabricated mid value.
    let c = g.geo.xy;
    let m = mat2x2<f32>(g.geo.z, g.geo.w, g.origin.z, g.origin.w);
    let pn = m * (gfc - c);
    let on = m * (g.origin.xy - c);
    let d0 = pn - on;
    let A = dot(on, on) - 1.0;
    let B = 2.0 * dot(d0, on);
    let C = dot(d0, d0);
    if (abs(A) < 1e-7) {
      // Focal on the ellipse boundary → the quadratic degenerates to linear.
      t = select(0.0, -C / B, abs(B) > 1e-9);
    } else {
      let disc = B * B - 4.0 * A * C;
      if (disc < 0.0) {
        t = 1.0;   // focal-ray misses the ellipse (only when the focal is outside) → clamp to the far edge color
      } else {
        let sq = sqrt(disc);
        let inv = 0.5 / A;
        let lo = min((-B - sq) * inv, (-B + sq) * inv);
        let hi = max((-B - sq) * inv, (-B + sq) * inv);
        // The pixel's circle in the (focal,0)->(center,1) pencil: take the smallest non-negative t (the first
        // circle to reach it as t grows from the focal). No non-negative root ⇒ outside the cone ⇒ far color.
        if (lo >= 0.0) { t = lo; } else if (hi >= 0.0) { t = hi; } else { t = 1.0; }
      }
    }
  }
  let tm = g.header.z;
  if (tm < 0.5) { t = clamp(t, 0.0, 1.0); }
  else if (tm < 1.5) { t = fract(t); }
  else { let f = fract(t * 0.5) * 2.0; if (f > 1.0) { t = 2.0 - f; } else { t = f; } }
  let n = i32(g.header.y);
  var col = g.colors[0];
  // Fast path for <=4 stops (the overwhelmingly common case). The general path below indexes the 64-entry
  // colour array and the packed stop array with a LOOP VARIABLE; dynamic indexing into a uniform array spills
  // on Intel-class GPUs, and this shader is fragment-bound over large areas. Constant indices avoid that.
  if (n <= 4) {
    let s0 = g.stops[0][0]; let s1 = g.stops[0][1]; let s2 = g.stops[0][2]; let s3 = g.stops[0][3];
    // Past the LAST stop is tested before before-the-first, because coincident stops satisfy both. Two stops at
    // the same offset are a hard switch (the focused TextBox border puts both at 1.0 to get an accent underline
    // under a grey ring); testing t <= s0 first makes every fragment take colors[0] and floods the shape with it.
    let sLast = select(select(select(s0, s1, n >= 2), s2, n >= 3), s3, n >= 4);
    let cLast = select(select(select(g.colors[0], g.colors[1], n >= 2), g.colors[2], n >= 3), g.colors[3], n >= 4);
    if (n >= 2 && t >= sLast) { col = cLast; }
    else if (n < 2 || t <= s0) { col = g.colors[0]; }
    else if (t <= s1) { col = mix(g.colors[0], g.colors[1], select(0.0, (t - s0) / (s1 - s0), s1 > s0)); }
    else if (n < 3) { col = g.colors[1]; }
    else if (t <= s2) { col = mix(g.colors[1], g.colors[2], select(0.0, (t - s1) / (s2 - s1), s2 > s1)); }
    else if (n < 4) { col = g.colors[2]; }
    else if (t <= s3) { col = mix(g.colors[2], g.colors[3], select(0.0, (t - s2) / (s3 - s2), s3 > s2)); }
    else { col = g.colors[3]; }
    return vec4<f32>(col.rgb, col.a * clipCovMapped(gfc, clip));
  }
  if (t >= stopAt(n - 1)) { col = g.colors[n - 1]; }
  else if (t <= stopAt(0)) { col = g.colors[0]; }
  else {
    for (var i = 0; i < n - 1; i = i + 1) {
      let s0 = stopAt(i); let s1 = stopAt(i + 1);
      if (t >= s0 && t <= s1) {
        var u = 0.0;
        if (s1 > s0) { u = (t - s0) / (s1 - s0); }
        col = mix(g.colors[i], g.colors[i + 1], u);
        break;
      }
    }
  }
  return vec4<f32>(col.rgb, col.a * clipCovMapped(gfc, clip));
}";
	// Analytic rounded-rect / border-ring fill. The SDF is evaluated in LOCAL
	// centred space (`p`/`hf`/`radii` interpolated per-vertex) so it's exact under any affine transform; the four
	// device corners only position the quad. `ihalf.x >= 0` = BORDER RING (subtract an inner rounded rect). clipCov
	// applies neutral's analytic rounded/rect clips using the device-pixel builtin position.
	private const string RoundedRectWgsl = @"
struct VSOut { @builtin(position) pos: vec4<f32>, @location(0) p: vec2<f32>, @location(1) hf: vec2<f32>, @location(2) radii: vec4<f32>, @location(3) col: vec4<f32>, @location(4) ihalf: vec2<f32>, @location(5) icenter: vec2<f32>, @location(6) iradii: vec4<f32> };
@group(0) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) cpos: vec2<f32>, @location(1) p: vec2<f32>, @location(2) hf: vec2<f32>, @location(3) radii: vec4<f32>, @location(4) col: vec4<f32>, @location(5) ihalf: vec2<f32>, @location(6) icenter: vec2<f32>, @location(7) iradii: vec4<f32>) -> VSOut {
  var o: VSOut; o.pos = vec4<f32>(cpos, 0.0, 1.0); o.p = p; o.hf = hf; o.radii = radii; o.col = col; o.ihalf = ihalf; o.icenter = icenter; o.iradii = iradii; return o;
}
fn sdRR(p: vec2<f32>, hf: vec2<f32>, radii: vec4<f32>) -> f32 {
  let rTop = select(radii.x, radii.y, p.x > 0.0); let rBot = select(radii.w, radii.z, p.x > 0.0);
  let rad = select(rTop, rBot, p.y > 0.0); let q = abs(p) - hf + vec2<f32>(rad, rad);
  return min(max(q.x, q.y), 0.0) + length(max(q, vec2<f32>(0.0, 0.0))) - rad;
}
@fragment fn fs(i: VSOut) -> @location(0) vec4<f32> {
  let d = sdRR(i.p, i.hf, i.radii); let aa = max(fwidth(d), 1e-4);
  var cov = 1.0 - smoothstep(-aa, aa, d);
  // Compute the inner-rrect SDF + its screen-space derivative in UNIFORM control flow (outside the `if`): WGSL
  // forbids fwidth/derivatives inside non-uniform control flow, and Dawn (browser WebGPU) enforces this strictly
  // even though wgpu-native (desktop) tolerated it. The result is only APPLIED when an inner rect is present.
  let di = sdRR(i.p - i.icenter, i.ihalf, i.iradii); let aai = max(fwidth(di), 1e-4);
  if (i.ihalf.x >= 0.0) { cov = cov * smoothstep(-aai, aai, di); }
  cov = cov * clipCov(i.pos.xy, clip);
  return vec4<f32>(i.col.rgb, i.col.a * cov);
}";
	// Transform-table rounded-rect: identical SDF/clip to RoundedRectWgsl, but the LOCAL (identity-baked) corners
	// `cpos` are positioned by the per-vertex slot's local->NDC affine (xf[ti]) instead of being pre-baked NDC. The
	// SDF params (p/hf/radii) are already transform-invariant local units, so a moved recording rewrites only its
	// slot. clipCov uses the final builtin position + the clip's finv (device fragment -> local clip space).
	private const string RoundedRectTableWgsl = @"
struct Xf { a: vec4<f32>, b: vec4<f32> };
struct VSOut { @builtin(position) pos: vec4<f32>, @location(0) p: vec2<f32>, @location(1) hf: vec2<f32>, @location(2) radii: vec4<f32>, @location(3) col: vec4<f32>, @location(4) ihalf: vec2<f32>, @location(5) icenter: vec2<f32>, @location(6) iradii: vec4<f32> };
@group(0) @binding(0) var<storage, read> xf: array<Xf>;
@group(1) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) cpos: vec2<f32>, @location(1) p: vec2<f32>, @location(2) hf: vec2<f32>, @location(3) radii: vec4<f32>, @location(4) col: vec4<f32>, @location(5) ihalf: vec2<f32>, @location(6) icenter: vec2<f32>, @location(7) iradii: vec4<f32>, @location(8) ti: u32) -> VSOut {
  let t = xf[ti];
  var o: VSOut; o.pos = vec4<f32>(cpos.x * t.a.x + cpos.y * t.a.y + t.a.z, cpos.x * t.a.w + cpos.y * t.b.x + t.b.y, 0.0, 1.0); o.p = p; o.hf = hf; o.radii = radii; o.col = col; o.ihalf = ihalf; o.icenter = icenter; o.iradii = iradii; return o;
}
fn sdRR(p: vec2<f32>, hf: vec2<f32>, radii: vec4<f32>) -> f32 {
  let rTop = select(radii.x, radii.y, p.x > 0.0); let rBot = select(radii.w, radii.z, p.x > 0.0);
  let rad = select(rTop, rBot, p.y > 0.0); let q = abs(p) - hf + vec2<f32>(rad, rad);
  return min(max(q.x, q.y), 0.0) + length(max(q, vec2<f32>(0.0, 0.0))) - rad;
}
@fragment fn fs(i: VSOut) -> @location(0) vec4<f32> {
  let d = sdRR(i.p, i.hf, i.radii); let aa = max(fwidth(d), 1e-4);
  var cov = 1.0 - smoothstep(-aa, aa, d);
  let di = sdRR(i.p - i.icenter, i.ihalf, i.iradii); let aai = max(fwidth(di), 1e-4);
  if (i.ihalf.x >= 0.0) { cov = cov * smoothstep(-aai, aai, di); }
  cov = cov * clipCov(i.pos.xy, clip);
  return vec4<f32>(i.col.rgb, i.col.a * cov);
}";
	private const string ImageWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
struct U { op: vec4<f32>, tint: vec4<f32>, m0: vec4<f32>, m1: vec4<f32>, m2: vec4<f32>, m3: vec4<f32>, off: vec4<f32> };
@group(0) @binding(0) var tex: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: U;
@group(1) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) uv: vec2<f32>) -> VOut { var o: VOut; o.p = xformPos(clip, pos); o.uv = uv; return o; }
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> {
  var c = textureSample(tex, smp, i.uv);   // premultiplied
  if (u.op.z > 0.5) {
    // 4x5 colour matrix (effect brush): unpremultiply -> matrix + offset -> clamp -> premultiply.
    var s = c;
    if (c.a > 0.0) { s = vec4<f32>(c.rgb / c.a, c.a); }
    let r = vec4<f32>(dot(u.m0, s) + u.off.x, dot(u.m1, s) + u.off.y, dot(u.m2, s) + u.off.z, dot(u.m3, s) + u.off.w);
    let rc = clamp(r, vec4<f32>(0.0), vec4<f32>(1.0));
    c = vec4<f32>(rc.rgb * rc.a, rc.a);
  } else if (u.op.y > 0.5) {
    // SrcIn blend-mode tint: premultiplied(filterColor) * dst.a.
    let fp = vec4<f32>(u.tint.rgb * u.tint.a, u.tint.a);
    c = fp * c.a;

  } else if (u.op.w > 0.5) {
    // Acrylic backdrop composite: blurred backdrop -> luminosity blend (tint = lum rgb/a) -> procedural grain
    // (off.x = noise opacity), opaque within the region. One draw replaces the blurred-image + luminosity overlay.
    var rgb = mix(c.rgb, u.tint.rgb, u.tint.a);
    let nz = (fract(sin(dot(floor(i.p.xy), vec2<f32>(12.9898, 78.233))) * 43758.5453) - 0.5) * 2.0 * u.off.x;
    rgb = clamp(rgb + vec3<f32>(nz), vec3<f32>(0.0), vec3<f32>(1.0));
    c = vec4<f32>(rgb, 1.0);
  }
  return c * u.op.x * clipCov(i.p.xy, clip);
}";
}
