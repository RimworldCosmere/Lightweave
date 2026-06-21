using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Doc;

public static partial class Doc {
    public static LightweaveNode Section(
        string anchorId,
        LightweaveNode heading,
        LightweaveNode content,
        DocContext ctx,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode inner = Layout.Stack.Create(
            SpacingScale.Md,
            s => {
                s.Add(heading);
                s.Add(content);
            }
        );

        LightweaveNode node = NodeBuilder.New($"Doc.Section:{anchorId}", line, file);
        node.Children.Add(inner);
        node.Measure = w => inner.Measure?.Invoke(w) ?? inner.PreferredHeight ?? 0f;
        node.Paint = (rect, paintChildren) => {
            ctx.AnchorOffsets[anchorId] = rect.y;
            inner.MeasuredRect = rect;
            paintChildren();
        };
        return node;
    }

    public static LightweaveNode TableOfContents(
        string titleLabel,
        IReadOnlyList<TocEntry> entries,
        DocContext ctx,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Doc.TableOfContents", line, file);

        float titleSizePx = new Rem(0.75f).ToFontPx();
        float titleHeightPx = new Rem(1.5f).ToPixels();
        float minEntryHeightPx = new Rem(1.5f).ToPixels();
        float entryFontPx = new Rem(0.8125f).ToFontPx();
        float gapBetween = new Rem(0.35f).ToPixels();
        float indentPerLevel = new Rem(0.75f).ToPixels();
        float titleToList = new Rem(0.5f).ToPixels();
        const float labelLeftPad = 8f;

        float LabelWidth(float totalWidth, TocEntry entry) {
            float indent = Mathf.Max(0, entry.Level - 2) * indentPerLevel;
            return Mathf.Max(1f, totalWidth - indent - labelLeftPad);
        }

        GUIStyle EntryStyle(Font font) {
            GUIStyle style = GuiStyleCache.GetOrCreate(font, Mathf.RoundToInt(entryFontPx), FontStyle.Normal);
            style.wordWrap = true;
            return style;
        }

        node.Measure = availableWidth => {
            Theme.Theme theme = RenderContext.Current.Theme;
            GUIStyle entryStyle = EntryStyle(theme.GetFont(FontRole.Body));
            float total = titleHeightPx + titleToList;
            for (int i = 0; i < entries.Count; i++) {
                float wrapped = TextMeasureCache.Height(entryStyle, entries[i].Label, LabelWidth(availableWidth, entries[i]));
                total += Mathf.Max(minEntryHeightPx, wrapped);
                if (i < entries.Count - 1) {
                    total += gapBetween;
                }
            }

            return total;
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Font font = theme.GetFont(FontRole.Body);
            Event e = Event.current;

            GUIStyle titleStyle = GuiStyleCache.GetOrCreate(theme, FontRole.Body, Mathf.RoundToInt(titleSizePx), FontStyle.Bold);
            titleStyle.alignment = TextAnchor.MiddleLeft;
            Color saved = GUI.color;

            Rect titleRect = new Rect(rect.x, rect.y, rect.width, titleHeightPx);
            GUI.color = theme.GetColor(ThemeSlot.TextPrimary);
            GUI.Label(RectSnap.Snap(titleRect), titleLabel, titleStyle);

            float scrollTop = ctx.Scroll.Position.y;
            string? active = null;
            float bestOffset = float.NegativeInfinity;
            for (int i = 0; i < entries.Count; i++) {
                if (ctx.AnchorOffsets.TryGetValue(entries[i].AnchorId, out float off)) {
                    if (off <= scrollTop + 24f && off > bestOffset) {
                        active = entries[i].AnchorId;
                        bestOffset = off;
                    }
                }
            }

            if (active == null && entries.Count > 0) {
                active = entries[0].AnchorId;
            }

            float y = rect.y + titleHeightPx + titleToList;
            GUIStyle entryStyle = EntryStyle(font);
            entryStyle.alignment = TextAnchor.UpperLeft;

            for (int i = 0; i < entries.Count; i++) {
                TocEntry entry = entries[i];
                float indent = Mathf.Max(0, entry.Level - 2) * indentPerLevel;
                float labelWidth = LabelWidth(rect.width, entry);
                float wrapped = TextMeasureCache.Height(entryStyle, entry.Label, labelWidth);
                float rowH = Mathf.Max(minEntryHeightPx, wrapped);

                Rect rowRect = new Rect(rect.x, y, rect.width, rowH);
                float textY = y + (rowH - wrapped) * 0.5f;
                Rect labelRect = new Rect(rect.x + indent + labelLeftPad, textY, labelWidth, wrapped);

                bool isActive = entry.AnchorId == active;
                bool hovering = rowRect.Contains(e.mousePosition);

                if (isActive) {
                    Rect bar = new Rect(rect.x, y, 2f, rowH);
                    GUI.color = theme.GetColor(ThemeSlot.SurfaceAccent);
                    GUI.DrawTexture(bar, Texture2D.whiteTexture);
                }

                Color textColor = isActive
                    ? theme.GetColor(ThemeSlot.TextPrimary)
                    : hovering
                        ? theme.GetColor(ThemeSlot.TextPrimary)
                        : theme.GetColor(ThemeSlot.TextMuted);
                GUI.color = textColor;
                GUI.Label(RectSnap.Snap(labelRect), entry.Label, entryStyle);

                if (hovering && e.type == EventType.MouseDown && e.button == 0) {
                    if (ctx.AnchorOffsets.TryGetValue(entry.AnchorId, out float off)) {
                        float target = Mathf.Max(0f, off - new Rem(1f).ToPixels());
                        ctx.Scroll.Position = new Vector2(ctx.Scroll.Position.x, target);
                    }

                    e.Use();
                }

                y += rowH + gapBetween;
            }

            GUI.color = saved;
        };

        return node;
    }
}
