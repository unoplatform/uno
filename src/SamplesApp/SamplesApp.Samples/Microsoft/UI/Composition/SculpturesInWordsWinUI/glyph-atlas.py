# Regenerates Assets/Sculptures/glyph-atlas.png, the atlas GlyphSprites.cs samples.
#
# No public Composition brush tints an image, so the glyph sheet is baked once per ink step and a
# letter picks the brush that already carries its glyph and its grey. Layout must stay in step with
# the constants in GlyphSprites.cs: 61 glyphs, 8 columns of 32px cells, 24 ink steps stacked down.
#
#   python glyph-atlas.py
from PIL import Image, ImageDraw, ImageFont

# OpenSans ships with the SamplesApp under the SIL Open Font License, so the baked glyphs carry a
# licence the repository already redistributes - a system font would not.
FONT = "../../../../Assets/Fonts/OpenSans/OpenSans-Regular.ttf"
ALPHABET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.,;:'\"!?-"
CELL, TINTS, COLUMNS = 32, 24, 8
INK_DARK, INK_PALE = 22, 245

chars = list(dict.fromkeys(ALPHABET))
rows = (len(chars) + COLUMNS - 1) // COLUMNS
tile_w, tile_h = COLUMNS * CELL, rows * CELL

font = ImageFont.truetype(FONT, int(CELL * 0.95))
ascent, descent = font.getmetrics()
# One baseline for every glyph: the figure tilts letters, and a per-glyph baseline would wobble.
baseline = (CELL - (ascent + descent)) * 0.5 + ascent

atlas = Image.new("RGBA", (tile_w, tile_h * TINTS), (0, 0, 0, 0))
draw = ImageDraw.Draw(atlas)
for tint in range(TINTS):
    v = round(INK_DARK + (INK_PALE - INK_DARK) * tint / (TINTS - 1))
    for i, ch in enumerate(chars):
        x = (i % COLUMNS) * CELL + CELL * 0.5
        y = (i // COLUMNS) * CELL + baseline + tint * tile_h
        draw.text((x, y), ch, font=font, fill=(v, v, min(255, v + 10), 255), anchor="ms")

atlas.save("../../../../Assets/Sculptures/glyph-atlas.png", optimize=True)
print(f"{len(chars)} glyphs, {TINTS} ink steps -> {tile_w}x{tile_h * TINTS}")
