# Post-merge regression review: Skia vs WebGPU (196-sample shard, headless lavapipe)
166/196 match <1% diff; 0 samples >20%. All divergences are known backend differences, not merge regressions:
- CompositionEffectBrush: WebGPU renders 2 specific effects blank (effect-coverage gap; ~14 others match).
- Popup_HVAlignments (17.7%): translucent-fill alpha shade slightly more saturated on WebGPU (cosmetic).
- BorderVisualClipping (4.5%): analytic-rrect border-ring AA/thickness.
- TextBlock_LayoutAlignment (5.4%): glyph rasterization AA; layout/positioning identical.
- Mini player: LibVLC-missing placeholder (environmental).
Conclusion: merge preserved rendering; neutral seam consistent across backends.
