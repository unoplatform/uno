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
	public const int ImageUniformBytes = 144;   // op+tint+m0..m3+off (112) + edge + ctrl2 (32); match ImageWgsl
	/// Bytes in the composite uniform block (24 floats: opacity plus a 4x5 colour matrix), as declared by
	/// <see cref="CompositeWgsl"/> and <see cref="CompositeBlendWgsl"/>. Same agreement requirement as above.
	/// </summary>
	public const int CompositeUniformBytes = 96;

	// Prepended to every colour-writing shader. The uniform is a parameter because each shader declares the binding
	// at its own contiguous group index (colored group 0, image/gradient group 1); a group hole is rejected by
	// wgpu's auto-layout.
	private const string ClipStructFn = @"
// Per-draw clip + placement. ctrl.x = entry count; ctrl.y > 0.5 = a plain rect clip (min ctrl.zw, max size.zw);
// xform/xoff.xy = the op's pixel-space transform and finv/xoff.zw its inverse, which maps a fragment back into the
// recording's space -- where the rect and the entries live. own.x > 0.5 = the op's own shape is a coverage texture (an
// atlas page or a mask of its own), sampled by per-vertex uv: the innermost clip, carried in the vertices so many
// small shapes (a glyph run) still draw as one; own.y > 0.5 = that texture is drawn scaled or rotated, so it filters.
// Each entry is reached through its 2x3 (q = m.xz*p.x + m.yw*p.y + t.xy) and is one of two kinds. t.w < 0.5: a
// rounded rect in its OWN space (rect = L,T,R,B; radX/radY = per-corner radii TL,TR,BR,BL), exact under any affine.
// t.w > 0.5: a path mask, q being its texel and rect its slot (x, y, w, h) in clipMask, which one texture holds for
// all of a draw's masks. Either kind: t.z > 0.5 = Difference (keep the outside). k.x = the entry's units per device
// pixel (constant under an affine, so computed once per draw), k.y > 0.5 = every corner is circular. Nesting has no cap:
// the uniform holds the first four entries, read at constant indices (a uniform read is far cheaper than a storage read
// on integrated GPUs), and a draw with more carries the rest in clipMore. inner = the largest axis-aligned rect (in the
// recording's space) that every clip covers in full, so the fragments inside it, most of a clipped shape, skip the
// entry maths altogether.
struct ClipEntry { m: vec4<f32>, t: vec4<f32>, rect: vec4<f32>, radX: vec4<f32>, radY: vec4<f32>, k: vec4<f32> };
struct ClipU { ctrl: vec4<f32>, size: vec4<f32>, xform: vec4<f32>, xoff: vec4<f32>, finv: vec4<f32>, own: vec4<f32>, inner: vec4<f32>, entries: array<ClipEntry, 4> };
// The pass projection: basis.xy = the target's top-left in device pixels, basis.zw = its size. Bound at group 0 of
// every colour pipeline, so vertices are uploaded in pixels and a resize or a size-to-content layer re-targets
// cached geometry for free.
struct PassU { basis: vec4<f32> };
@group(0) @binding(0) var<uniform> proj: PassU;
fn project(p: vec2<f32>) -> vec4<f32> {
  return vec4<f32>((p.x - proj.basis.x) / proj.basis.z * 2.0 - 1.0, 1.0 - (p.y - proj.basis.y) / proj.basis.w * 2.0, 0.0, 1.0);
}
// Places a vertex: the op's pixel-space transform (xform = [m00 m01 m10 m11], xoff.xy = translation; identity for
// geometry built where it lands), then the pass projection. Re-stamped as one uniform write when a cached visual
// moves, so its geometry is reused, not rebuilt.
fn place(pos: vec2<f32>) -> vec4<f32> {
  return project(vec2<f32>(clip.xform.x * pos.x + clip.xform.y * pos.y + clip.xoff.x,
                           clip.xform.z * pos.x + clip.xform.w * pos.y + clip.xoff.y));
}
// Coverage of one clip entry at p (recording space). ddx/ddy = how p moves per fragment step, so the entry's own
// pixel scale follows from its matrix and the SDF distance converts to pixels without derivative builtins.
fn maskCov(e: ClipEntry, q: vec2<f32>) -> f32 {
  let t = floor(q);
  if (t.x < 0.0 || t.y < 0.0 || t.x >= e.rect.z || t.y >= e.rect.w) { return 0.0; }
  return textureLoad(clipMask, vec2<i32>(t + e.rect.xy), 0).r;
}
fn entryCov(e: ClipEntry, p: vec2<f32>) -> f32 {
  let q = vec2<f32>(e.m.x * p.x + e.m.z * p.y + e.t.x, e.m.y * p.x + e.m.w * p.y + e.t.y);
  if (e.t.w > 0.5) { let a = maskCov(e, q); return select(a, 1.0 - a, e.t.z > 0.5); }
  let c = (e.rect.xy + e.rect.zw) * 0.5;
  let h = (e.rect.zw - e.rect.xy) * 0.5;
  let lp = q - c;
  let rx = select(select(e.radX.x, e.radX.y, lp.x > 0.0), select(e.radX.w, e.radX.z, lp.x > 0.0), lp.y > 0.0);
  let ry = select(select(e.radY.x, e.radY.y, lp.x > 0.0), select(e.radY.w, e.radY.z, lp.x > 0.0), lp.y > 0.0);
  let r = vec2<f32>(rx, ry);
  let qq = abs(lp) - h + r;
  // A pixel more than a pixel inside the corner box is fully covered: most of a clipped shape's pixels, spared the distance.
  let excl = e.t.z > 0.5;
  if (max(qq.x, qq.y) <= -e.k.x) { return select(1.0, 0.0, excl); }
  var d: f32;
  if (e.k.y > 0.5) {
    // Circular corners (or none): the rounded-box distance, one square root.
    d = min(max(qq.x, qq.y), 0.0) + length(max(qq, vec2<f32>(0.0, 0.0))) - rx;
  } else {
    // Elliptical corner via a first-order (gradient-normalised) implicit-ellipse distance.
    let outside = max(qq, vec2<f32>(0.0, 0.0));
    let rg = max(r, vec2<f32>(1e-6, 1e-6));
    let el = length(outside / rg);
    let grad = length(outside / (rg * rg)) / max(el, 1e-6);
    d = min(max(qq.x, qq.y), 0.0) + (el - 1.0) / max(grad, 1e-6);
  }
  let cov = clamp(0.5 - d / e.k.x, 0.0, 1.0);
  return select(cov, 1.0 - cov, excl);
}
// The op's own coverage texture at uv, or 1 when the shape is carried by the geometry itself.
fn covTex(uv: vec2<f32>) -> f32 {
  if (clip.own.x > 0.5) {
    // A mask drawn 1:1 reads its texel outright: a filtered fetch at a texel centre is not exactly that texel on
    // hardware with 8-bit sub-texel precision, and mixes in 1/256 of the neighbour. Only a mask drawn scaled or
    // rotated (own.y) is filtered.
    if (clip.own.y > 0.5) { return textureSampleLevel(coverageTex, covSmp, uv, 0.0).a; }
    return textureLoad(coverageTex, vec2<i32>(uv * vec2<f32>(textureDimensions(coverageTex))), 0).a;
  }
  return 1.0;
}
// Coverage at rp, the fragment's position in the recording's space (the vertex position interpolated, exact under
// an affine placement), times the op's own coverage texture.
fn clipCov(rp: vec2<f32>, uv: vec2<f32>) -> f32 {
  let own = covTex(uv);
  if (clip.ctrl.x < 0.5 && clip.ctrl.y < 0.5) { return own; }
  return own * clipCovMapped(rp);
}
fn clipCovMapped(fc: vec2<f32>) -> f32 {
  if (all(fc >= clip.inner.xy) && all(fc <= clip.inner.zw)) { return 1.0; }
  // Dedicated plain-rect clip (ctrl.y flag; min in ctrl.zw, max in size.zw): carries the clip's AABB analytically
  // so the per-op device SCISSOR is cull-only and the emit collapses SetScissorRect calls (see AabbInClipU).
  var cov = 1.0;
  if (clip.ctrl.y > 0.5) {
    let dmin = fc - vec2<f32>(clip.ctrl.z, clip.ctrl.w);
    let dmax = vec2<f32>(clip.size.z, clip.size.w) - fc;
    cov = clamp(0.5 + min(min(dmin.x, dmin.y), min(dmax.x, dmax.y)), 0.0, 1.0);
  }
  let n = u32(clip.ctrl.x);   // ctrl.x is the live count
  if (n == 0u) { return cov; }
  cov = cov * entryCov(clip.entries[0], fc);
  if (n > 1u) { cov = cov * entryCov(clip.entries[1], fc); }
  if (n > 2u) { cov = cov * entryCov(clip.entries[2], fc); }
  if (n > 3u) { cov = cov * entryCov(clip.entries[3], fc); }
  for (var i = 4u; i < n; i = i + 1u) { cov = cov * entryCov(clipMore[i - 4u], fc); }
  return cov;
}
";
	// The two coverage passes over a sheet of slots. Accumulate: one quad per edge spanning the rows it crosses and
	// everything to its right up to its slot's bound (ext), so an edge contributes the partial area of the pixel it
	// passes through and a full +/-1 beyond, and additive blending sums the SIGNED covered area per pixel. Resolve:
	// every slot in one draw, the fill rule and Difference flag riding the quad's vertices.
	private const string CoverageSheetWgsl = @"
struct CovU { size: vec4<f32> };
@group(0) @binding(0) var<storage, read> edges: array<vec4<f32>>;   // x0,y0,x1,y1 in sheet pixels
@group(0) @binding(1) var<storage, read> ext: array<f32>;           // per edge: its slot's right bound
@group(0) @binding(2) var<uniform> cov: CovU;                       // size.xy = sheet size in px
struct CovOut { @builtin(position) p: vec4<f32>, @location(0) e: vec4<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> CovOut {
  let e = edges[vi / 6u];
  let ci = vi % 6u;
  var xs = array<f32, 6>(0.0, 1.0, 1.0, 0.0, 1.0, 0.0);
  var ys = array<f32, 6>(0.0, 0.0, 1.0, 0.0, 1.0, 1.0);
  let x = mix(floor(min(e.x, e.z)), ext[vi / 6u], xs[ci]);
  let y = mix(floor(min(e.y, e.w)), ceil(max(e.y, e.w)), ys[ci]);
  var o: CovOut;
  o.p = vec4<f32>(x / cov.size.x * 2.0 - 1.0, 1.0 - y / cov.size.y * 2.0, 0.0, 1.0);
  o.e = e;
  return o;
}
fn ramp(c: f32, x: f32) -> f32 {
  if (x <= c - 1.0) { return x; }
  if (x >= c) { return c - 0.5; }
  let t = x - (c - 1.0);
  return (c - 1.0) + t - 0.5 * t * t;
}
@fragment fn fs(i: CovOut) -> @location(0) vec4<f32> {
  let e = i.e;
  let dy = e.w - e.y;
  if (abs(dy) < 1e-7) { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }
  let py = floor(i.p.y);
  let ya = max(min(e.y, e.w), py);
  let yb = min(max(e.y, e.w), py + 1.0);
  if (yb <= ya) { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }
  let xa = e.x + (e.z - e.x) * (ya - e.y) / dy;
  let xb = e.x + (e.z - e.x) * (yb - e.y) / dy;
  let c = floor(i.p.x) + 1.0;
  let lo = min(xa, xb); let hi = max(xa, xb);
  var avg = clamp(c - xa, 0.0, 1.0);
  if (hi - lo > 1e-6) { avg = (ramp(c, hi) - ramp(c, lo)) / (hi - lo); }
  let s = select(-1.0, 1.0, dy > 0.0);
  return vec4<f32>((yb - ya) * avg * s, 0.0, 0.0, 0.0);
}";
	private const string CoverageResolveSheetWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) t: vec2<f32>, @location(1) f: vec2<f32> };
@group(0) @binding(0) var acc: texture_2d<f32>;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) t: vec2<f32>, @location(2) f: vec2<f32>) -> VOut {
  var o: VOut; o.p = vec4<f32>(pos, 0.0, 1.0); o.t = t; o.f = f; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> {
  var a = abs(textureLoad(acc, vec2<i32>(i.t), 0).r);
  if (i.f.x > 0.5) { a = a - 2.0 * floor(a * 0.5); a = min(a, 2.0 - a); }
  a = min(a, 1.0);
  if (i.f.y > 0.5) { a = 1.0 - a; }
  return vec4<f32>(a, a, a, a);
}";

	private const string ColoredWgsl = @"
@group(1) @binding(0) var<uniform> clip: ClipU;
@group(1) @binding(4) var<storage, read> clipMore: array<ClipEntry>;
@group(1) @binding(1) var clipMask: texture_2d<f32>;
@group(1) @binding(2) var coverageTex: texture_2d<f32>;
@group(1) @binding(3) var covSmp: sampler;
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32>, @location(1) uv: vec2<f32>, @location(2) rp: vec2<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>, @location(2) uv: vec2<f32>) -> VOut {
  var o: VOut; o.p = place(pos); o.c = col; o.uv = uv; o.rp = pos; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return vec4<f32>(i.c.rgb, i.c.a * clipCov(i.rp, i.uv)); }";
	// Draws a texture over the whole target (a fullscreen triangle, exact texel fetch): the effect evaluator's final
	// draw. Optional colour matrix (params.x); params.z = a sub-rect at m1.xy of size m0.zw.
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
  // params.z = size-to-content layer: src holds the layer's sub-rect at slot m1.xy with size m0.zw, shifted by
  // m0.xy from this target's pixels; outside the slot the layer contributes nothing (transparent SrcOver = no change).
  var lp = vec2<i32>(i.p.xy);
  if (u.params.z > 0.5) {
    lp = vec2<i32>(i.p.xy - u.m0.xy);
    let rel = lp - vec2<i32>(u.m1.xy);
    if (rel.x < 0 || rel.y < 0 || rel.x >= i32(u.m0.z) || rel.y >= i32(u.m0.w)) { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }
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
	// Evaluates a linear/radial gradient per pixel. The fragment uses its framebuffer position (device pixels) so
	// the gradient geometry can be baked to device space at record time.
	private const string GradientWgsl = @"
struct Grad { header: vec4<f32>, geo: vec4<f32>, colors: array<vec4<f32>, 64>, stops: array<vec4<f32>, 16>, origin: vec4<f32>, ramp: array<vec4<f32>, 8> };
@group(1) @binding(0) var<uniform> g: Grad;
@group(2) @binding(0) var<uniform> clip: ClipU;
@group(2) @binding(4) var<storage, read> clipMore: array<ClipEntry>;
@group(2) @binding(1) var clipMask: texture_2d<f32>;
@group(2) @binding(2) var coverageTex: texture_2d<f32>;
@group(2) @binding(3) var covSmp: sampler;
struct GVOut { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32>, @location(1) rp: vec2<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) uv: vec2<f32>) -> GVOut { var o: GVOut; o.p = place(pos); o.uv = uv; o.rp = pos; return o; }
fn stopAt(i: i32) -> f32 { return g.stops[i / 4][i % 4]; }
@fragment fn fs(i: GVOut) -> @location(0) vec4<f32> {
  // The gradient geometry lives in the recording's space, where rp is.
  let gfc = i.rp;
  var t: f32 = 0.0;
  if (g.header.x < 0.5) {
    t = dot(gfc - g.geo.xy, g.geo.zw);   // geo.zw = direction / |direction|^2
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
    if (dot(on, on) < 1e-12) {
      t = length(pn);   // focal at the centre: concentric circles, exactly what the solve below yields
    } else if (abs(A) < 1e-7) {
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
  // Fast path for <=4 stops (the overwhelmingly common case): each interval's colour is t * scale + bias from the
  // uniform's ramp, picked at constant indices (a loop variable into a uniform array spills on Intel-class GPUs).
  // Past the LAST stop is tested before before-the-first, because coincident stops satisfy both: two stops at
  // the same offset are a hard switch (the focused TextBox border puts both at 1.0 for an accent underline).
  if (n <= 4) {
    let s = g.stops[0];
    let sLast = select(select(select(s.x, s.y, n >= 2), s.z, n >= 3), s.w, n >= 4);
    let cLast = select(select(select(g.colors[0], g.colors[1], n >= 2), g.colors[2], n >= 3), g.colors[3], n >= 4);
    if (n >= 2 && t >= sLast) { col = cLast; }
    else if (n < 2 || t <= s.x) { col = g.colors[0]; }
    else {
      let i1 = t > s.y && n >= 3;
      let i2 = t > s.z && n >= 4;
      let sc = select(select(g.ramp[0], g.ramp[2], i1), g.ramp[4], i2);
      let bi = select(select(g.ramp[1], g.ramp[3], i1), g.ramp[5], i2);
      col = t * sc + bi;
    }
    return vec4<f32>(col.rgb, col.a * covTex(i.uv) * clipCovMapped(gfc));
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
  return vec4<f32>(col.rgb, col.a * covTex(i.uv) * clipCovMapped(gfc));
}";
	// Analytic rounded-rect / border-ring fill. The SDF is evaluated in LOCAL
	// centred space (`p`/`hf`/`radii` interpolated per-vertex) so it's exact under any affine transform; the four
	// pixel corners only position the quad. `ihalf.x >= 0` = BORDER RING (subtract an inner rounded rect). clipCov
	// applies neutral's analytic rounded/rect clips using the device-pixel builtin position.
	private const string RoundedRectWgsl = @"
struct VSOut { @builtin(position) pos: vec4<f32>, @location(0) p: vec2<f32>, @location(1) hf: vec2<f32>, @location(2) radii: vec4<f32>, @location(3) col: vec4<f32>, @location(4) ihalf: vec2<f32>, @location(5) icenter: vec2<f32>, @location(6) iradii: vec4<f32>, @location(7) rp: vec2<f32> };
@group(1) @binding(0) var<uniform> clip: ClipU;
@group(1) @binding(4) var<storage, read> clipMore: array<ClipEntry>;
@group(1) @binding(1) var clipMask: texture_2d<f32>;
@group(1) @binding(2) var coverageTex: texture_2d<f32>;
@group(1) @binding(3) var covSmp: sampler;
@vertex fn vs(@location(0) cpos: vec2<f32>, @location(1) p: vec2<f32>, @location(2) hf: vec2<f32>, @location(3) radii: vec4<f32>, @location(4) col: vec4<f32>, @location(5) ihalf: vec2<f32>, @location(6) icenter: vec2<f32>, @location(7) iradii: vec4<f32>) -> VSOut {
  var o: VSOut; o.pos = place(cpos); o.p = p; o.hf = hf; o.radii = radii; o.col = col; o.ihalf = ihalf; o.icenter = icenter; o.iradii = iradii; o.rp = cpos; return o;
}
fn sdRR(p: vec2<f32>, hf: vec2<f32>, radii: vec4<f32>) -> f32 {
  let rTop = select(radii.x, radii.y, p.x > 0.0); let rBot = select(radii.w, radii.z, p.x > 0.0);
  let rad = select(rTop, rBot, p.y > 0.0); let q = abs(p) - hf + vec2<f32>(rad, rad);
  return min(max(q.x, q.y), 0.0) + length(max(q, vec2<f32>(0.0, 0.0))) - rad;
}
@fragment fn fs(i: VSOut) -> @location(0) vec4<f32> {
  // Analytic box-filter coverage: a pixel whose centre sits d (in pixels) from a straight edge is covered by
  // 0.5 - d, which is exactly 1 for a pixel lying fully inside up to its own edge. A smoothstep over +/-fwidth(d)
  // instead ramps across two pixels and reads 0.84375 there, so a 1px border -- whose outer and inner ramps land
  // in the SAME pixel and multiply -- came out at 0.84375^2 = 0.711 and never reached the requested colour.
  // sxy is local units per screen pixel, taken from the derivatives of the linearly interpolated p -- exact and
  // constant across the primitive. Differentiating the SDF instead reads a gradient slightly over 1 wherever a
  // 2x2 quad straddles a corner or the axis switch in max(q.x, q.y), which cost those pixels their last LSB.
  // Dividing by it keeps d in pixels when a replay transform scales the baked geometry.
  let sxy = max(max(length(vec2<f32>(dpdx(i.p.x), dpdy(i.p.x))), length(vec2<f32>(dpdx(i.p.y), dpdy(i.p.y)))), 1e-4);
  let d = sdRR(i.p, i.hf, i.radii);
  var cov = clamp(0.5 - d / sxy, 0.0, 1.0);
  // sxy above stays outside the `if`: WGSL forbids derivatives in non-uniform control flow, and Dawn (browser
  // WebGPU) enforces that strictly even though wgpu-native (desktop) tolerated it. The inner rect only gets
  // APPLIED when one is present.
  let di = sdRR(i.p - i.icenter, i.ihalf, i.iradii);
  if (i.ihalf.x >= 0.0) { cov = cov * clamp(0.5 + di / sxy, 0.0, 1.0); }
  cov = cov * clipCov(i.rp, vec2<f32>(0.0));
  return vec4<f32>(i.col.rgb, i.col.a * cov);
}";
	private const string ImageWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32>, @location(1) rp: vec2<f32> };
// edge = the quad's own uv rect (u0,v0,u1,v1); ctrl2.x > 0.5 = antialias the quad's edges analytically.
struct U { op: vec4<f32>, tint: vec4<f32>, m0: vec4<f32>, m1: vec4<f32>, m2: vec4<f32>, m3: vec4<f32>, off: vec4<f32>, edge: vec4<f32>, ctrl2: vec4<f32> };
@group(1) @binding(0) var tex: texture_2d<f32>;
@group(1) @binding(1) var smp: sampler;
@group(1) @binding(2) var<uniform> u: U;
@group(2) @binding(0) var<uniform> clip: ClipU;
@group(2) @binding(4) var<storage, read> clipMore: array<ClipEntry>;
@group(2) @binding(1) var clipMask: texture_2d<f32>;
@group(2) @binding(2) var coverageTex: texture_2d<f32>;
@group(2) @binding(3) var covSmp: sampler;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) uv: vec2<f32>) -> VOut { var o: VOut; o.p = place(pos); o.uv = uv; o.rp = pos; return o; }
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> {
  // Analytic box-filter coverage of the quad's own edges, in pixels via the uv derivatives -- the rounded-rect
  // treatment, so a rotated image is not hard-edged at one sample. Gated by a uniform (so the derivatives sit in
  // uniform control flow): off for an atlas or mask quad, which carries its coverage in the texture, and for a quad
  // on whole pixels, which has no edge to soften.
  var cov = 1.0;
  if (u.ctrl2.x > 0.5) {
    let du = max(length(vec2<f32>(dpdx(i.uv.x), dpdy(i.uv.x))), 1e-6);
    let dv = max(length(vec2<f32>(dpdx(i.uv.y), dpdy(i.uv.y))), 1e-6);
    let cx = clamp(min(i.uv.x - u.edge.x, u.edge.z - i.uv.x) / du + 0.5, 0.0, 1.0);
    let cy = clamp(min(i.uv.y - u.edge.y, u.edge.w - i.uv.y) / dv + 0.5, 0.0, 1.0);
    cov = cx * cy;
  }
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
  return c * u.op.x * cov * clipCov(i.rp, vec2<f32>(0.0));
}";
}
