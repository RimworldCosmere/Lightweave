using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.MainMenu;

[StaticConstructorOnStartup]
public static class TitleHero {
    private const float TitleAspect = 1032f / 146f;
    private static readonly Texture2D TitleTex = ContentFinder<Texture2D>.Get("UI/HeroArt/GameTitle");
    private static readonly Vector2[] SubtitleHaloOffsets = {
        new Vector2(-1f, 0f), new Vector2(1f, 0f),
        new Vector2(0f, -1f), new Vector2(0f, 1f),
    };

    public static LightweaveNode Create() {
        return Stack.Create(
            new Rem(0.875f),
            s => {
                s.Add(CreateLogo());
                string sub = "CL_MainMenu_WordmarkSub".Translate();
                if (!string.IsNullOrEmpty(sub)) {
                    s.Add(CreateSubtitle(sub));
                }
            }
        );
    }

    private static LightweaveNode CreateLogo() {
        LightweaveNode logo = NodeBuilder.New("RimWorldLogo");
        logo.PreferredHeight = new Rem(7.2f).ToPixels();
        logo.Paint = (rect, _) => {
            if (TitleTex == null) return;
            float h = rect.height;
            float w = h * TitleAspect;
            if (w > rect.width) {
                w = rect.width;
                h = w / TitleAspect;
            }

            Rect target = new Rect(
                rect.x + (rect.width - w) * 0.5f,
                rect.y + (rect.height - h) * 0.5f,
                w,
                h
            );
            PaintBox.DrawTexture(target, TitleTex, Color.white, ScaleMode.ScaleToFit);
        };
        return logo;
    }

    private static LightweaveNode CreateSubtitle(string raw) {
        LightweaveNode node = NodeBuilder.New("RimWorldSubtitle");
        node.PreferredHeight = new Rem(1.5f).ToPixels();
        node.Paint = (rect, _) => {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

            float logoH = new Rem(7.2f).ToPixels();
            float wordmarkW = Mathf.Min(logoH * TitleAspect, rect.width);
            float wordmarkRight = rect.x + (rect.width + wordmarkW) * 0.5f;

            int pixelSize = Mathf.RoundToInt(new Rem(1.375f).ToFontPx());
            float tracking = pixelSize * 0.02f;

            Rect anchored = new Rect(rect.x, rect.y, wordmarkRight - rect.x, rect.height);

            Theme.Theme theme = RenderContext.Current.Theme;
            Color ink = theme.GetColor(ThemeSlot.TextSecondary);
            Color halo = theme.GetColor(ThemeSlot.TextShadowDeep);

            for (int i = 0; i < SubtitleHaloOffsets.Length; i++) {
                Vector2 off = SubtitleHaloOffsets[i];
                TextDraw.DrawTracked(
                    new Rect(anchored.x + off.x, anchored.y + off.y, anchored.width, anchored.height),
                    raw,
                    FontRole.Display,
                    new Rem(1.375f),
                    TextAnchor.MiddleRight,
                    halo,
                    tracking,
                    FontStyle.Italic
                );
            }

            TextDraw.DrawTracked(
                anchored,
                raw,
                FontRole.Display,
                new Rem(1.375f),
                TextAnchor.MiddleRight,
                ink,
                tracking,
                FontStyle.Italic
            );
        };
        return node;
    }
}