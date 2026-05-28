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

        if (bgVisible) {
            DrawFill(r, bg, rad);
        }
        if (hasBorder) {
            Color bc = ResolveColor(border!.Value.Color!);
            if (rounded) {
                DrawRoundedBorderRing(r, bw, rad, bc);
            }
            else {
                DrawRectStroke(r, bw, bc);
            }
        }
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
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, c, Vector4.zero, rad);
        }
        else if (bg is BackgroundSpec.Textured tex) {
            Color c = tex.Tint != null ? ResolveColor(tex.Tint) : Color.white;
            GUI.DrawTexture(r, tex.Texture, tex.Mode, true, 0, c, Vector4.zero, rad);
        }
        else if (bg is BackgroundSpec.Gradient grad) {
            Color c = grad.Tint != null ? ResolveColor(grad.Tint) : Color.white;
            GUI.DrawTexture(r, grad.GradientTex, ScaleMode.StretchToFill, true, 0, c, Vector4.zero, rad);
        }
        else if (bg is BackgroundSpec.Blurred blurred) {
            BackdropBlur.Draw(r, blurred.BlurSizePx);
            if (blurred.Tint != null) {
                Color c = ResolveColor(blurred.Tint);
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, c, Vector4.zero, rad);
            }
        }
    }

    

    // Draws explicit colored slabs along the four straight edges of a rounded
    // bordered rect. The two-rect overlay (outer rounded fill + inner rounded
    // fill) leaves a 1px-ish border visible in theory, but Unity's rounded-
    // texture path can chew the leftmost column of the outer when the rect's
    // straight edge meets the rounded corner. These edge slabs sit in the
    // straight sections only (not over the rounded corners) so they paint
    // crisp 1px borders without disturbing the corners.
    

    // Draws a rounded-corner border outline with a transparent interior. Unity's
    // GUI.DrawTexture borderWidths arg renders only the ring (the outer bw pixels)
    // following the corner radius, so the corner arcs connect to the straight edges
    // cleanly. Used for rounded borders over a non-opaque fill, where the opaque
    // DrawSolidRounded base path can't be used (it would tint the translucent fill).
    // Paints 1px hard strips along the four straight edges of a rounded border,
    // between the corner tangents. Unity's borderWidths-based ring rendering
    // AA-rasterizes both the outer and inner curve; at small radii the inner
    // AA halo on the straight edges reads as a second border line (double-
    // border artifact). These strips replace the AA-soft straight-edge slice
    // of the ring with a crisp 1px slab, so only the corner arcs keep the
    // ring's natural AA.
    

    private static void DrawRoundedBorderRing(Rect r, Vector4 bw, Vector4 rad, Color color) {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, bw, rad);
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