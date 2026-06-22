using System;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Rendering;

public static class PaintBox {
    private static readonly Color HighlightTint = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color HighlightTintMouseover = new Color(1f, 1f, 1f, 0.12f);

    public static void DrawHighlight(Rect rect, RadiusSpec? radius = null, bool mouseover = false) {
        Color tint = mouseover ? HighlightTintMouseover : HighlightTint;
        Draw(rect, BackgroundSpec.Of(tint), null, radius);
    }

    public static void DrawHighlightIfMouseover(Rect rect, RadiusSpec? radius = null) {
        if (Mouse.IsOver(rect)) {
            DrawHighlight(rect, radius, true);
        }
    }


    public static void Fill(Rect rect, Color color) {
        Rect r = RectSnap.Snap(rect);
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
    }

    public static void DrawTexture(Rect rect, Texture texture, Color tint, ScaleMode scaleMode = ScaleMode.StretchToFill) {
        Rect r = RectSnap.Snap(rect);
        GUI.DrawTexture(r, texture, scaleMode, true, 0, tint, Vector4.zero, Vector4.zero);
    }


    public static void DrawLine(Vector2 start, Vector2 end, Color color, float width) {
        Widgets.DrawLine(start, end, color, width);
    }

    public static void DrawRotatedLine(Vector2 start, Vector2 end, Color color, float width) {
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.001f) {
            return;
        }

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        Color saved = GUI.color;
        GUI.color = color;
        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, length, width), Texture2D.whiteTexture);
        GUI.matrix = savedMatrix;
        GUI.color = saved;
    }

    private static Texture2D? diagonalHatchTex;

    private static Texture2D DiagonalHatchTexture() {
        if (diagonalHatchTex != null) {
            return diagonalHatchTex;
        }

        const int size = 24;
        const int band = 12;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false) {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };
        Color on = Color.white;
        Color off = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                int d = (((x + y) % size) + size) % size;
                tex.SetPixel(x, y, d < band ? on : off);
            }
        }

        tex.Apply();
        diagonalHatchTex = tex;
        return tex;
    }

    public static void HatchDiagonal(Rect rect, Color color, float tilePx) {
        if (rect.width <= 0f || rect.height <= 0f || tilePx <= 0f) {
            return;
        }

        Texture2D tex = DiagonalHatchTexture();
        Color saved = GUI.color;
        GUI.color = color;
        Rect texCoords = new Rect(0f, 0f, rect.width / tilePx, rect.height / tilePx);
        GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
        GUI.color = saved;
    }

    public static void FillSolid(Rect rect, Color color) {
        Widgets.DrawBoxSolid(RectSnap.Snap(rect), color);
    }


    public static bool ButtonImage(Rect rect, Texture2D texture, Color baseColor, Color hoverColor) {
        return Widgets.ButtonImage(rect, texture, baseColor, hoverColor, true, null);
    }

    public static void Draw(Rect rect, BackgroundSpec? bg, BorderSpec? border, RadiusSpec? radius) {
        Rect r = RectSnap.Snap(rect);
        Direction dir = RenderContext.Current.Direction;

        Vector4 rad = radius?.ResolveVector(dir) ?? Vector4.zero;
        Vector4 bw = border?.ResolveVector(dir) ?? Vector4.zero;
        bool hasBorder = border != null &&
                         border.Value.Color != null &&
                         (bw.x > 0f || bw.y > 0f || bw.z > 0f || bw.w > 0f);
        bool rounded = rad.x > 0f || rad.y > 0f || rad.z > 0f || rad.w > 0f;
        bool bgVisible = IsBgVisible(bg);

        // Opaque solid fill behind a rounded border: draw an outer rounded rect
        // in the border color, then an inset rounded rect in the fill color.
        // Both use Unity's GPU rounded-rect rasterizer, so the fill's corner
        // curve matches the border's exactly and the fill can never poke past
        // the border into a square corner notch. The hollow-ring path below
        // bakes its own arc, which does not line up pixel-for-pixel with Unity's
        // fill arc and leaves a muddy corner wedge where the darker fill shows
        // through at the corner tip.
        if (rounded && hasBorder && IsOpaqueSolid(bg, out Color fillColor)) {
            Color borderColor = ResolveColor(border!.Value.Color!);
            DrawRoundedFilledBorder(r, bw, rad, fillColor, borderColor);
            return;
        }

        if (bgVisible) {
            DrawFill(r, bg, rad);
        }
        if (hasBorder) {
            Color bc = ResolveColor(border!.Value.Color!);
            if (rounded) {
                DrawRoundedBorderRing(r, bw, rad, bc);
            }
            else if (border!.Value.Style == BorderStyleKind.Dashed) {
                DrawDashedRectStroke(r, bw, bc);
            }
            else {
                DrawRectStroke(r, bw, bc);
            }
        }
    }

    // Outer rounded rect in the border color, then an inset rounded rect in the
    // fill color. Used only for opaque fills, where painting the border color
    // edge-to-edge underneath is invisible once the fill covers the interior.
    private static void DrawRoundedFilledBorder(Rect r, Vector4 bw, Vector4 rad, Color fill, Color border) {
        // Fill the rounded silhouette from baked corner discs plus straight bands.
        // The discs share the exact supersampled arc of the border ring drawn on
        // top, so the fill never pokes past the border (no notch) and no Unity GPU
        // rounded-rect is used here (no stray corner pixel).
        if (TryFillRoundedSolid(r, fill, rad)) {
            DrawRoundedBorderRing(r, bw, rad, border);
            return;
        }

        // Per-corner or zero radii can't tile cleanly into corner squares plus
        // straight bands, so fall back to Unity's GPU rounded rects (outer border
        // color, inset fill). This path can leave a faint stray corner pixel under
        // a dark border, but it has no notch and only the uniform case (every
        // shipping caller) reaches the baked path above.
        Texture2D whiteFallback = Texture2D.whiteTexture;
        GUI.DrawTexture(r, whiteFallback, ScaleMode.StretchToFill, true, 0, border, Vector4.zero, rad);

        float l = bw.x;
        float t = bw.y;
        float ri = bw.z;
        float bo = bw.w;
        Rect innerRect = new Rect(
            r.x + l,
            r.y + t,
            Mathf.Max(0f, r.width - l - ri),
            Mathf.Max(0f, r.height - t - bo)
        );
        if (innerRect.width <= 0f || innerRect.height <= 0f) {
            return;
        }

        float maxInner = Mathf.Min(innerRect.width, innerRect.height) * 0.5f;
        Vector4 innerRad = new Vector4(
            Mathf.Clamp(rad.x - Mathf.Max(l, t), 0f, maxInner),
            Mathf.Clamp(rad.y - Mathf.Max(ri, t), 0f, maxInner),
            Mathf.Clamp(rad.z - Mathf.Max(ri, bo), 0f, maxInner),
            Mathf.Clamp(rad.w - Mathf.Max(l, bo), 0f, maxInner)
        );
        GUI.DrawTexture(innerRect, whiteFallback, ScaleMode.StretchToFill, true, 0, fill, Vector4.zero, innerRad);
    }

    // Fills a uniform-radius rounded silhouette with one solid color from baked
    // corner discs plus straight bands. No Unity GPU rounded-rect is used, so a
    // translucent fill leaves no stray partial-alpha pixel just outside the arc
    // (which reads as a dark speck once a dark border is drawn over the corner).
    // The bands tile the rect without overlap, so a translucent color blends each
    // pixel exactly once. Returns false for non-uniform or zero radius so the
    // caller falls back to Unity's rasterizer.
    private static bool TryFillRoundedSolid(Rect r, Color color, Vector4 rad) {
        int maxR = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(r.width, r.height) * 0.5f));
        int rTL = Mathf.Clamp(Mathf.RoundToInt(rad.x), 0, maxR);
        int rTR = Mathf.Clamp(Mathf.RoundToInt(rad.y), 0, maxR);
        int rBR = Mathf.Clamp(Mathf.RoundToInt(rad.z), 0, maxR);
        int rBL = Mathf.Clamp(Mathf.RoundToInt(rad.w), 0, maxR);

        if (rTL <= 0 && rTR <= 0 && rBR <= 0 && rBL <= 0) {
            return false;
        }

        // The 4-disc + 3-rect tiling below is gap-free AND overlap-free only when
        // each edge's two corners share a radius - which holds for every shape the
        // framework actually produces (All/Top/Bottom/Left/Right, where the odd
        // corner out is always zero). A pair that is both non-zero and unequal
        // would leave an uncovered sliver beside the shorter corner, so defer those
        // exotic radii to Unity's rasterizer. Overlap-free matters because a
        // translucent fill would double-blend any seam into a visible darker line.
        bool topPairOk = rTL == rTR || rTL == 0 || rTR == 0;
        bool botPairOk = rBL == rBR || rBL == 0 || rBR == 0;
        if (!topPairOk || !botPairOk) {
            return false;
        }

        Texture2D white = Texture2D.whiteTexture;

        if (rTL > 0) {
            GUI.DrawTexture(new Rect(r.x, r.y, rTL, rTL), RoundedBorderTextureCache.QuarterDisc(rTL, RoundedBorderTextureCache.Corner.TopLeft), ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        if (rTR > 0) {
            GUI.DrawTexture(new Rect(r.xMax - rTR, r.y, rTR, rTR), RoundedBorderTextureCache.QuarterDisc(rTR, RoundedBorderTextureCache.Corner.TopRight), ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        if (rBR > 0) {
            GUI.DrawTexture(new Rect(r.xMax - rBR, r.yMax - rBR, rBR, rBR), RoundedBorderTextureCache.QuarterDisc(rBR, RoundedBorderTextureCache.Corner.BottomRight), ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        if (rBL > 0) {
            GUI.DrawTexture(new Rect(r.x, r.yMax - rBL, rBL, rBL), RoundedBorderTextureCache.QuarterDisc(rBL, RoundedBorderTextureCache.Corner.BottomLeft), ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }

        int topInset = Mathf.Max(rTL, rTR);
        int botInset = Mathf.Max(rBL, rBR);

        float topW = r.width - rTL - rTR;
        if (topInset > 0 && topW > 0f) {
            GUI.DrawTexture(new Rect(r.x + rTL, r.y, topW, topInset), white, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }

        float botW = r.width - rBL - rBR;
        if (botInset > 0 && botW > 0f) {
            GUI.DrawTexture(new Rect(r.x + rBL, r.yMax - botInset, botW, botInset), white, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }

        float midH = r.height - topInset - botInset;
        if (midH > 0f) {
            GUI.DrawTexture(new Rect(r.x, r.y + topInset, r.width, midH), white, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }

        return true;
    }

    private static bool IsOpaqueSolid(BackgroundSpec? bg, out Color color) {
        color = default;
        if (bg is BackgroundSpec.Solid solid) {
            Color c = ResolveColor(solid.Color);
            if (c.a >= 0.999f) {
                color = c;
                return true;
            }
        }
        return false;
    }

    public static void DrawShadow(Rect rect, ShadowSpec? spec) {
        if (spec == null) {
            return;
        }
        DrawShadowResolved(rect, spec, RenderContext.Current.Theme, isInset: false);
    }

    public static void DrawInsetHighlight(Rect rect, ShadowSpec? spec) {
        if (spec == null) {
            return;
        }
        DrawShadowResolved(rect, spec, RenderContext.Current.Theme, isInset: true);
    }

    private static void DrawShadowResolved(Rect rect, ShadowSpec spec, Theme.Theme theme, bool isInset) {
        switch (spec) {
            case ShadowSpec.SlotRef slotRef: {
                ShadowSpec? resolved = theme.GetShadow(slotRef.Slot);
                if (resolved != null) {
                    DrawShadowResolved(rect, resolved, theme, isInset);
                }
                break;
            }
            case ShadowSpec.Layered layered: {
                for (int i = 0; i < layered.Stops.Length; i++) {
                    DrawShadowResolved(rect, layered.Stops[i], theme, isInset);
                }
                break;
            }
            case ShadowSpec.Drop drop: {
                if (!isInset) {
                    DrawDropShadow(rect, drop);
                }
                break;
            }
            case ShadowSpec.Inset inset: {
                if (isInset) {
                    DrawInsetEdge(rect, inset);
                }
                break;
            }
        }
    }

    private static void DrawDropShadow(Rect rect, ShadowSpec.Drop drop) {
        float blur = Mathf.Max(1f, drop.BlurPx);
        float spread = Mathf.Max(0f, drop.SpreadPx);
        float margin = blur + spread;
        int bw = Mathf.Max(1, Mathf.RoundToInt(rect.width + 2f * spread));
        int bh = Mathf.Max(1, Mathf.RoundToInt(rect.height + 2f * spread));
        Rect shadowRect = new Rect(
            rect.x - margin + drop.OffsetPx.x,
            rect.y - margin + drop.OffsetPx.y,
            rect.width + margin * 2f,
            rect.height + margin * 2f
        );
        Texture2D tex = ShadowTextureCache.Drop(bw, bh, blur);
        Color color = ResolveColor(drop.Color);
        GUI.DrawTexture(
            shadowRect,
            tex,
            ScaleMode.StretchToFill,
            true,
            0,
            color,
            Vector4.zero,
            Vector4.zero
        );
    }

    private static void DrawInsetEdge(Rect rect, ShadowSpec.Inset inset) {
        int heightPx = Mathf.Max(1, Mathf.RoundToInt(inset.HeightPx));
        Texture2D tex = ShadowTextureCache.InsetEdge(heightPx);
        Color color = ResolveColor(inset.Color);
        Rect r;
        if (inset.Edge == InsetEdge.Top) {
            r = new Rect(rect.x, rect.y, rect.width, heightPx);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        else {
            r = new Rect(rect.x, rect.yMax - heightPx, rect.width, heightPx);
            Color saved = GUI.color;
            GUI.color = color;
            Matrix4x4 savedMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(180f, new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f));
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true, 0);
            GUI.matrix = savedMatrix;
            GUI.color = saved;
        }
    }

    private static void DrawFill(Rect r, BackgroundSpec? bg, Vector4 rad) {
        if (bg is BackgroundSpec.Solid solid) {
            Color c = ResolveColor(solid.Color);
            if (!TryFillRoundedSolid(r, c, rad)) {
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, c, Vector4.zero, rad);
            }
        }
        else if (bg is BackgroundSpec.Textured tex) {
            Color c = tex.Tint != null ? ResolveColor(tex.Tint) : Color.white;
            GUI.DrawTexture(r, tex.Texture, tex.Mode, true, 0, c, Vector4.zero, rad);
        }
        else if (bg is BackgroundSpec.Gradient grad) {
            Color c = grad.Tint != null ? ResolveColor(grad.Tint) : Color.white;
            DrawGradientTexture(r, grad.GradientTex, c, rad);
        }
        else if (bg is BackgroundSpec.GradientSlots vgrad) {
            Texture2D gradTex = GradientTextureCache.Vertical(ResolveColor(vgrad.Top), ResolveColor(vgrad.Bottom));
            Color c = vgrad.Tint != null ? ResolveColor(vgrad.Tint) : Color.white;
            DrawGradientTexture(r, gradTex, c, rad);
        }
        else if (bg is BackgroundSpec.Blurred blurred) {
            Color? tint = blurred.Tint != null ? ResolveColor(blurred.Tint) : null;
            bool tintOpaque = tint.HasValue && tint.Value.a >= 0.999f;
            bool radiusUniform = Mathf.Approximately(rad.x, rad.y)
                                 && Mathf.Approximately(rad.y, rad.z)
                                 && Mathf.Approximately(rad.z, rad.w);

            // The blur is only ever visible through a translucent tint, and the
            // shader can only clip it to the silhouette with a single shared
            // corner radius. Two cases make the blur pure liability:
            //   - an opaque tint (every theme's SurfaceSunken is alpha 1.0) hides
            //     the blur entirely, so drawing it is wasted GrabPasses, and
            //   - a non-uniform radius (e.g. a code block's bottom-only rounding)
            //     forces the blur square, poking blurred content into the rounded
            //     corner notches as stray specks just outside each arc.
            // In either case skip the blur and let the baked per-corner tint fill
            // own the rounded silhouette.
            if (!tintOpaque && radiusUniform) {
                BackdropBlur.Draw(r, blurred.BlurSizePx, cornerRadiusPx: rad.x);
            }

            if (tint.HasValue) {
                Color c = tint.Value;
                if (!TryFillRoundedSolid(r, c, rad)) {
                    GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, c, Vector4.zero, rad);
                }
            }
        }
    }


    // GUI.DrawTexture's rounded-rect overload (the one taking borderRadiuses) applies only the
    // uniform tint alpha and discards the texture's PER-PIXEL alpha, so a translucent gradient
    // (e.g. AccentSoft at 8%) renders fully opaque and washes out anything drawn over it. When the
    // rect has no corner radius we can use the plain alpha-blending overload, which honors the
    // texture's own alpha. Rounded gradients still fall back to the masked overload.
    private static void DrawGradientTexture(Rect r, Texture2D tex, Color tint, Vector4 rad) {
        bool noRadius = rad.x <= 0f && rad.y <= 0f && rad.z <= 0f && rad.w <= 0f;
        if (noRadius) {
            Color saved = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
            GUI.color = saved;
            return;
        }

        GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true, 0, tint, Vector4.zero, rad);
    }

    // Returns the uniform corner radius in pixels when all four corners share it,
    // else 0. The backdrop-blur shader rounds with a single radius, so a
    // non-uniform rect falls back to a square blur rather than a distorted corner.
    

    // Draws a hollow rounded border by compositing four baked corner arcs with
    // four solid straight edges. Unity's GUI.DrawTexture border path ignores
    // borderRadiuses once a border width is set, so it cannot draw a rounded
    // ring directly; per-corner masks from RoundedBorderTextureCache supply the
    // arcs while the straight runs stay crisp solid strips. Falls back to a
    // square stroke when the edge widths are not uniform (no current caller
    // needs an asymmetric rounded border).
    private static void DrawRoundedBorderRing(Rect r, Vector4 bw, Vector4 rad, Color color) {
        if (!Mathf.Approximately(bw.x, bw.y)
            || !Mathf.Approximately(bw.y, bw.z)
            || !Mathf.Approximately(bw.z, bw.w)) {
            DrawRectStroke(r, bw, color);
            return;
        }

        int b = Mathf.Max(1, Mathf.RoundToInt(bw.x));
        int maxR = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(r.width, r.height) * 0.5f));
        int rTL = Mathf.Min(Mathf.RoundToInt(rad.x), maxR);
        int rTR = Mathf.Min(Mathf.RoundToInt(rad.y), maxR);
        int rBR = Mathf.Min(Mathf.RoundToInt(rad.z), maxR);
        int rBL = Mathf.Min(Mathf.RoundToInt(rad.w), maxR);

        if (rTL > 0) {
            Texture2D tex = RoundedBorderTextureCache.QuarterRing(rTL, b, RoundedBorderTextureCache.Corner.TopLeft);
            GUI.DrawTexture(new Rect(r.x, r.y, rTL, rTL), tex, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        if (rTR > 0) {
            Texture2D tex = RoundedBorderTextureCache.QuarterRing(rTR, b, RoundedBorderTextureCache.Corner.TopRight);
            GUI.DrawTexture(new Rect(r.xMax - rTR, r.y, rTR, rTR), tex, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        if (rBR > 0) {
            Texture2D tex = RoundedBorderTextureCache.QuarterRing(rBR, b, RoundedBorderTextureCache.Corner.BottomRight);
            GUI.DrawTexture(new Rect(r.xMax - rBR, r.yMax - rBR, rBR, rBR), tex, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        if (rBL > 0) {
            Texture2D tex = RoundedBorderTextureCache.QuarterRing(rBL, b, RoundedBorderTextureCache.Corner.BottomLeft);
            GUI.DrawTexture(new Rect(r.x, r.yMax - rBL, rBL, rBL), tex, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }

        Texture2D white = Texture2D.whiteTexture;
        float topW = r.width - rTL - rTR;
        if (topW > 0f) {
            GUI.DrawTexture(new Rect(r.x + rTL, r.y, topW, b), white, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        float botW = r.width - rBL - rBR;
        if (botW > 0f) {
            GUI.DrawTexture(new Rect(r.x + rBL, r.yMax - b, botW, b), white, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        float leftH = r.height - rTL - rBL;
        if (leftH > 0f) {
            GUI.DrawTexture(new Rect(r.x, r.y + rTL, b, leftH), white, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
        float rightH = r.height - rTR - rBR;
        if (rightH > 0f) {
            GUI.DrawTexture(new Rect(r.xMax - b, r.y + rTR, b, rightH), white, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
        }
    }

    private static void DrawRectStroke(Rect r, Vector4 bw, Color color) {
        Color saved = GUI.color;
        GUI.color = color;
        Texture2D tex = Texture2D.whiteTexture;

        float left = bw.x;
        float top = bw.y;
        float right = bw.z;
        float bottom = bw.w;

        if (top > 0f) {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, top), tex);
        }

        if (bottom > 0f) {
            GUI.DrawTexture(new Rect(r.x, r.yMax - bottom, r.width, bottom), tex);
        }

        if (left > 0f) {
            GUI.DrawTexture(new Rect(r.x, r.y + top, left, Mathf.Max(0f, r.height - top - bottom)), tex);
        }

        if (right > 0f) {
            GUI.DrawTexture(new Rect(r.xMax - right, r.y + top, right, Mathf.Max(0f, r.height - top - bottom)), tex);
        }

        GUI.color = saved;
    }

    // Dashed square stroke: each edge is a run of short segments (dash 4px / gap 3px).
    // Allocation-free — only GUI.DrawTexture calls in the loops. Used for square borders
    // whose BorderSpec.Style is Dashed; rounded dashed borders fall back to the solid ring.
    private static void DrawDashedRectStroke(Rect r, Vector4 bw, Color color) {
        const float dash = 4f;
        const float gap = 3f;
        const float period = dash + gap;

        Color saved = GUI.color;
        GUI.color = color;
        Texture2D tex = Texture2D.whiteTexture;

        float left = bw.x;
        float top = bw.y;
        float right = bw.z;
        float bottom = bw.w;

        if (top > 0f) {
            for (float x = r.x; x < r.xMax; x += period) {
                GUI.DrawTexture(new Rect(x, r.y, Mathf.Min(dash, r.xMax - x), top), tex);
            }
        }
        if (bottom > 0f) {
            for (float x = r.x; x < r.xMax; x += period) {
                GUI.DrawTexture(new Rect(x, r.yMax - bottom, Mathf.Min(dash, r.xMax - x), bottom), tex);
            }
        }

        float innerTop = r.y + top;
        float innerBottom = r.yMax - bottom;
        if (left > 0f) {
            for (float y = innerTop; y < innerBottom; y += period) {
                GUI.DrawTexture(new Rect(r.x, y, left, Mathf.Min(dash, innerBottom - y)), tex);
            }
        }
        if (right > 0f) {
            for (float y = innerTop; y < innerBottom; y += period) {
                GUI.DrawTexture(new Rect(r.xMax - right, y, right, Mathf.Min(dash, innerBottom - y)), tex);
            }
        }

        GUI.color = saved;
    }

    private static Color ResolveColor(ColorRef cref) {
        return cref switch {
            ColorRef.Literal l => l.Value,
            ColorRef.Token t => RenderContext.Current.Theme.GetColor(t.Slot),
            _ => throw new InvalidOperationException($"Unknown ColorRef subtype: {cref?.GetType().Name ?? "null"}"),
        };
    }


    private static bool IsBgVisible(BackgroundSpec? bg) {
        if (bg == null) {
            return false;
        }
        if (bg is BackgroundSpec.Solid solid) {
            Color c = ResolveColor(solid.Color);
            return c.a > 0f;
        }
        return true;
    }

    
}