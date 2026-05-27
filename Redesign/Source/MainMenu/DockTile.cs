using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class DockTile {
    private static readonly Rem TileHeight = new Rem(4f);
    private static readonly Rem LabelLineHeight = new Rem(1.3f);
    private static readonly Rem HintLineHeight = new Rem(1.25f);
    private static readonly Rem LabelHintGap = new Rem(0.1f);

    public static LightweaveNode Create(
        string label,
        string hotkey,
        Action onClick,
        bool disabled = false,
        bool chevron = false,
        bool expanded = false,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        if (!disabled && !string.IsNullOrEmpty(hotkey) && TryParseHotkey(hotkey, out KeyCode code)) {
            UseHotkey.Use(code, onClick);
        }

        Hooks.Hooks.RefHandle<float> flipRatio = Hooks.Hooks.UseRef<float>(expanded ? 1f : 0f, line, file + "#dock-flip");

        LightweaveNode body = NodeBuilder.New($"DockTile:body:{label}", line, file);
        body.Paint = (rect, _) => {
            if (chevron) {
                float target = expanded ? 1f : 0f;
                float dt = Time.unscaledDeltaTime;
                flipRatio.Current = Mathf.MoveTowards(flipRatio.Current, target, dt / 0.16f);
            }
            DrawLabel(rect, label, disabled);
            if (chevron) {
                DrawChevron(rect, disabled, flipRatio.Current);
            }
            else {
                DrawHotkeyHint(rect, hotkey, disabled);
            }
        };

        Style baseStyle = new Style { Width = Length.Stretch, Height = TileHeight };
        Style merged = style.HasValue ? Style.Merge(baseStyle, style.Value) : baseStyle;

        return Button.Create(
            label: string.Empty,
            onClick: onClick,
            variant: Variant.Frosted,
            disabled: disabled,
            style: merged,
            classes: StyleExtensions.PrependClass("dock-tile", classes),
            id: id,
            body: body,
            line: line,
            file: file
        );
    }

    private static bool TryParseHotkey(string hotkey, out KeyCode code) {
        if (hotkey.Length == 1) {
            char c = char.ToUpperInvariant(hotkey[0]);
            if (c >= 'A' && c <= 'Z') {
                code = (KeyCode)((int)KeyCode.A + (c - 'A'));
                return true;
            }
            if (c >= '0' && c <= '9') {
                code = (KeyCode)((int)KeyCode.Alpha0 + (c - '0'));
                return true;
            }
        }
        code = KeyCode.None;
        return false;
    }

    private static void DrawLabel(Rect tile, string label, bool disabled) {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        string upper = (label ?? string.Empty).ToUpperInvariant();
        Rem fontSize = new Rem(0.875f);
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        float labelH = LabelLineHeight.ToPixels();
        float hintH = HintLineHeight.ToPixels();
        float gap = LabelHintGap.ToPixels();
        float blockH = labelH + gap + hintH;
        float blockY = tile.y + (tile.height - blockH) * 0.5f;
        Rect labelRect = new Rect(tile.x, blockY, tile.width, labelH);

        ThemeSlot fgSlot = disabled ? ThemeSlot.TextMuted : ThemeSlot.TextPrimary;

        float tracking = Mathf.Max(1, Mathf.RoundToInt(pixelSize * 0.04f));
        TextDraw.DrawTracked(labelRect, upper, FontRole.Body, fontSize, TextAnchor.MiddleCenter, fgSlot, tracking);
    }

    private static void DrawHotkeyHint(Rect tile, string key, bool disabled) {
        if (string.IsNullOrEmpty(key)) {
            return;
        }

        if (Event.current.type != EventType.Repaint) {
            return;
        }

        string label = "[" + key.ToUpperInvariant() + "]";
        Rem fontSize = new Rem(0.9f);
        Vector2 size = TextDraw.Measure(label, FontRole.Mono, fontSize);
        float labelH = LabelLineHeight.ToPixels();
        float hintH = HintLineHeight.ToPixels();
        float gap = LabelHintGap.ToPixels();
        float blockH = labelH + gap + hintH;
        float blockY = tile.y + (tile.height - blockH) * 0.5f;
        float x = tile.x + (tile.width - size.x) * 0.5f;
        float y = blockY + labelH + gap;
        Rect hint = new Rect(x, y, size.x, hintH);

        ThemeSlot fgSlot = disabled ? ThemeSlot.TextMuted : ThemeSlot.TextSecondary;
        TextDraw.Draw(hint, label, FontRole.Mono, fontSize, TextAnchor.MiddleLeft, fgSlot);
    }

    

    private static void DrawChevron(Rect tile, bool disabled, float flipRatio) {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        ThemeSlot fgSlot = disabled ? ThemeSlot.TextMuted : ThemeSlot.TextSecondary;
        Color tint = theme.GetColor(fgSlot);

        float chevronSize = new Rem(1.0f).ToPixels();
        float labelH = LabelLineHeight.ToPixels();
        float hintH = HintLineHeight.ToPixels();
        float gap = LabelHintGap.ToPixels();
        float blockH = labelH + gap + hintH;
        float blockY = tile.y + (tile.height - blockH) * 0.5f;
        float cx = tile.x + tile.width * 0.5f;
        float yTop = blockY + labelH + gap + (hintH - chevronSize) * 0.5f;
        Rect chevronRect = RectSnap.Snap(new Rect(cx - chevronSize * 0.5f, yTop, chevronSize, chevronSize));

        float scaleY = 1f - 2f * flipRatio;
        Texture2D tex = ChevronTextureCache.Down(strokePx: 38f, armSpan: 0.7f, armHeight: 0.42f);
        using (ScaleScope.Around(new Vector2(1f, scaleY), chevronRect.center)) {
            PaintBox.DrawTexture(chevronRect, tex, tint);
        }
    }

    
}
