#!/usr/bin/env python3
"""Generates the PowerID app icon assets (PNG tiles + multi-resolution .ico).

Mirrors generate_icon.swift from the macOS app: a blue-to-cyan gradient square
with a white lightning-bolt glyph, exported at every size Windows' packaging
and taskbar/Explorer icon pipeline expects.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

ASSETS_DIR = Path(__file__).parent / "PowerID" / "Assets"

GRADIENT_START = (33, 150, 243)   # DodgerBlue-ish
GRADIENT_END = (0, 229, 255)      # Cyan


def make_gradient_square(size: int, corner_radius_ratio: float = 0.22) -> Image.Image:
    base = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    gradient = Image.new("RGBA", (size, size))
    for y in range(size):
        for x in range(size):
            t = (x + y) / (2 * size)
            r = int(GRADIENT_START[0] + (GRADIENT_END[0] - GRADIENT_START[0]) * t)
            g = int(GRADIENT_START[1] + (GRADIENT_END[1] - GRADIENT_START[1]) * t)
            b = int(GRADIENT_START[2] + (GRADIENT_END[2] - GRADIENT_START[2]) * t)
            gradient.putpixel((x, y), (r, g, b, 255))

    mask = Image.new("L", (size, size), 0)
    mask_draw = ImageDraw.Draw(mask)
    radius = int(size * corner_radius_ratio)
    mask_draw.rounded_rectangle([0, 0, size - 1, size - 1], radius=radius, fill=255)

    base.paste(gradient, (0, 0), mask)
    return base


def draw_bolt(image: Image.Image) -> None:
    size = image.width
    draw = ImageDraw.Draw(image)
    cx, cy = size / 2, size / 2
    scale = size * 0.28
    points = [
        (cx + 0.15 * scale, cy - 1.1 * scale),
        (cx - 0.75 * scale, cy + 0.15 * scale),
        (cx - 0.05 * scale, cy + 0.15 * scale),
        (cx - 0.15 * scale, cy + 1.1 * scale),
        (cx + 0.75 * scale, cy - 0.15 * scale),
        (cx + 0.05 * scale, cy - 0.15 * scale),
    ]
    draw.polygon(points, fill=(255, 255, 255, 235))


def render_icon(size: int) -> Image.Image:
    image = make_gradient_square(size)
    draw_bolt(image)
    return image


def render_wide(width: int, height: int) -> Image.Image:
    base = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    gradient = Image.new("RGBA", (width, height))
    for y in range(height):
        for x in range(width):
            t = (x / width + y / height) / 2
            r = int(GRADIENT_START[0] + (GRADIENT_END[0] - GRADIENT_START[0]) * t)
            g = int(GRADIENT_START[1] + (GRADIENT_END[1] - GRADIENT_START[1]) * t)
            b = int(GRADIENT_START[2] + (GRADIENT_END[2] - GRADIENT_START[2]) * t)
            gradient.putpixel((x, y), (r, g, b, 255))

    mask = Image.new("L", (width, height), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle([0, 0, width - 1, height - 1], radius=int(height * 0.12), fill=255)
    base.paste(gradient, (0, 0), mask)

    bolt = render_icon(int(height * 0.7))
    base.paste(bolt, (int((height - bolt.height) / 2), int((height - bolt.height) / 2)), bolt)
    return base


def main() -> None:
    ASSETS_DIR.mkdir(parents=True, exist_ok=True)

    plain_sizes = {
        "StoreLogo.png": 50,
        "Square44x44Logo.png": 44,
        "Square150x150Logo.png": 150,
    }
    for filename, size in plain_sizes.items():
        render_icon(size).save(ASSETS_DIR / filename)
        print(f"Wrote {filename} ({size}x{size})")

    render_wide(310, 150).save(ASSETS_DIR / "Wide310x150Logo.png")
    print("Wrote Wide310x150Logo.png (310x150)")

    render_wide(620, 300).save(ASSETS_DIR / "SplashScreen.png")
    print("Wrote SplashScreen.png (620x300)")

    ico_sizes = [16, 24, 32, 48, 64, 128, 256]
    ico_images = [render_icon(size) for size in ico_sizes]
    ico_images[-1].save(
        ASSETS_DIR / "AppIcon.ico",
        sizes=[(s, s) for s in ico_sizes],
        append_images=ico_images[:-1],
    )
    print("Wrote AppIcon.ico (16-256px)")


if __name__ == "__main__":
    main()
