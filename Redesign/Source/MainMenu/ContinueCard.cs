using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Redesign.LoadColony;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Typography.Typography;
using Display = Cosmere.Lightweave.Typography.Display;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using HotkeyBadge = Cosmere.Lightweave.Typography.HotkeyBadge;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class ContinueCard {
    public static LightweaveNode Create(
        SaveMetadata.LatestSave? save
    ) {
        if (save == null) {
            return BuildWelcome();
        }
        return BuildContinue(save);
    }

    private static LightweaveNode BuildContinue(SaveMetadata.LatestSave save) {
        return Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.None, h => {
                h.AddFlex(BuildContinueContent(save));
                h.Add(BuildContinueButton(save), new Rem(15.5f).ToPixels());
            })),
            style: new Style {
                Background = BackgroundSpec.Blur(ThemeSlot.Glass3),
                Border = BorderSpec.All(new Rem(0.0625f), ThemeSlot.BorderSubtle),
                Radius = RadiusSpec.All(RadiusScale.Lg),
            }
        );
    }

    private static LightweaveNode BuildContinueContent(SaveMetadata.LatestSave save) {
        return Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.Md, h => {
                h.Add(BuildThumbnail(save), new Rem(4.5f).ToPixels());
                h.AddFlex(BuildBody(save));
            })),
            style: new Style {
                Padding = new EdgeInsets(Top: new Rem(0.75f), Bottom: new Rem(0.75f), Left: new Rem(1.5f), Right: new Rem(1.5f)),
            }
        );
    }

    private static LightweaveNode BuildThumbnail(SaveMetadata.LatestSave save) {
        LightweaveNode node = NodeBuilder.New("ContinueCard:Thumbnail");
        node.PreferredHeight = new Rem(4.5f).ToPixels();
        node.Paint = (rect, _) => {
            PaintBox.Fill(rect, new Color(0.122f, 0.086f, 0.067f, 1f));

            Texture2D? shot = ColonyScreenshotCache.GetOrLoad(save);
            if (shot != null) {
                PaintBox.DrawTexture(rect, shot, Color.white, ScaleMode.ScaleAndCrop);
            }
            else {
                DrawScanlines(rect);
            }
        };
        return node;
    }

    private static void DrawScanlines(Rect rect) {
        Color baseFill = new Color(0.122f, 0.086f, 0.067f, 1f);
        PaintBox.Fill(rect, baseFill);

        Color highlight = new Color(0.353f, 0.255f, 0.157f, 1f);
        int steps = 5;
        for (int i = 0; i < steps; i++) {
            float t = (i + 1) / (float)steps;
            float radius = Mathf.Min(rect.width, rect.height) * (0.85f * t);
            float alpha = 0.45f * (1f - t);
            Color c = highlight;
            c.a = alpha;
            float cx = rect.x + rect.width * 0.30f;
            float cy = rect.y + rect.height * 0.30f;
            Rect r = new Rect(cx - radius * 0.5f, cy - radius * 0.5f, radius, radius);
            PaintBox.Fill(r, c);
        }

        Color line = new Color(1f, 0.784f, 0.549f, 1f);
        line.a = 0.10f;
        float step = new Rem(0.25f).ToPixels();
        for (float y = rect.y + step; y < rect.yMax; y += step) {
            Rect r = new Rect(rect.x + 2f, y, rect.width - 4f, 1f);
            PaintBox.Fill(r, line);
        }
    }

    private static LightweaveNode BuildBody(SaveMetadata.LatestSave save) {
        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(BuildEyebrowLine(save));
            s.Add(BuildTitleLine(save));
            s.Add(BuildStatsRow(save));
        });
    }

    private static LightweaveNode BuildEyebrowLine(SaveMetadata.LatestSave save) {
        LightweaveNode node = NodeBuilder.New("ContinueCard:Eyebrow");
        node.PreferredHeight = new Rem(1.5f).ToPixels();
        node.Paint = (rect, _) => {
            Rem fontSize = new Rem(0.6875f);
            int px = Mathf.RoundToInt(fontSize.ToFontPx());
            string parts = BuildEyebrowText(save);
            string upper = parts.ToUpperInvariant();
            float tracking = Mathf.Max(0, Mathf.RoundToInt(px * 0.18f));
            TextDraw.DrawTracked(rect, upper, FontRole.Mono, fontSize, TextAnchor.MiddleLeft, ThemeSlot.SurfaceAccent, tracking);
        };
        return node;
    }

    private static string BuildEyebrowText(SaveMetadata.LatestSave save) {
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        parts.Add("CL_MainMenu_Continue".Translate());
        if (save.Sidecar != null) {
            if (save.Sidecar.InGameYear > 0) {
                parts.Add("CL_MainMenu_YearAD".Translate(save.Sidecar.InGameYear.Named("YEAR")));
            }
            if (!string.IsNullOrEmpty(save.Sidecar.Quadrum)) {
                parts.Add(save.Sidecar.Quadrum);
            }
            if (!string.IsNullOrEmpty(save.Sidecar.Biome)) {
                parts.Add(save.Sidecar.Biome);
            }
        }
        return string.Join(" · ", parts);
    }

    private static LightweaveNode BuildTitleLine(SaveMetadata.LatestSave save) {
        LightweaveNode node = NodeBuilder.New("ContinueCard:Title");
        Rem fontSize = new Rem(2.875f);
        int px = Mathf.RoundToInt(fontSize.ToFontPx());
        node.PreferredHeight = Mathf.Ceil(px * 1.35f);
        node.Paint = (rect, _) => {
            string title = save.DisplayName ?? string.Empty;
            if (title.Length == 0) {
                return;
            }
            float tracking = Mathf.Max(0, Mathf.RoundToInt(px * 0.02f));
            TextDraw.DrawTracked(
                rect,
                title,
                FontRole.Display,
                fontSize,
                TextAnchor.MiddleLeft,
                ThemeSlot.TextPrimary,
                tracking,
                FontStyle.Normal,
                LightweaveFonts.IMFellEnglishSC
            );
        };
        return node;
    }

    private static LightweaveNode BuildStatsRow(SaveMetadata.LatestSave save) {
        return HStack.Create(new Rem(1.375f), h => {
            void AddStat(string value, string label, ThemeSlot valueColor) {
                LightweaveNode column = BuildStatColumn(value, label, valueColor);
                h.AddFlex(column);
            }
            void AddSep() {
                h.AddHug(BuildStatSeparator());
            }

            if (save.Sidecar == null) {
                string when = SaveMetadata.FormatRelative(save.LastWriteTime);
                AddStat(when, "saved", ThemeSlot.TextPrimary);
                if (save.Permadeath) {
                    AddSep();
                    AddStat((string)"CL_MainMenu_Stat_Permadeath".Translate(), string.Empty, ThemeSlot.AccentSoft);
                }
                return;
            }

            SaveSidecarData sidecar = save.Sidecar!;
            int threatPct = Mathf.RoundToInt(sidecar.ThreatScale * 100f);
            string wealth = FormatWealth(sidecar.Wealth);

            AddStat(sidecar.ColonistCount.ToString(), (string)"CL_MainMenu_Stat_Colonists".Translate(), ThemeSlot.TextPrimary);
            AddSep();
            AddStat(sidecar.AnimalCount.ToString(), (string)"CL_MainMenu_Stat_Animals".Translate(), ThemeSlot.TextPrimary);
            AddSep();
            AddStat(wealth, (string)"CL_MainMenu_Stat_Wealth".Translate(), ThemeSlot.TextPrimary);
            AddSep();
            AddStat(sidecar.DaysSurvived + " days", "alive", ThemeSlot.TextPrimary);
            AddSep();
            AddStat(threatPct + "%", "difficulty", ThemeSlot.TextPrimary);
            if (save.Permadeath) {
                AddSep();
                AddStat((string)"CL_MainMenu_Stat_Permadeath".Translate(), string.Empty, ThemeSlot.AccentSoft);
            }
        });
    }
    private static LightweaveNode BuildStatSeparator() {
        LightweaveNode node = NodeBuilder.New("ContinueCard:StatSep");
        node.PreferredHeight = new Rem(1.625f).ToPixels();
        node.MeasureWidth = () => new Rem(0.5f).ToPixels();
        node.Paint = (rect, _) => {
            float valueH = new Rem(1.625f).ToPixels();
            Rect r = new Rect(rect.x, rect.y, rect.width, valueH);
            TextDraw.Draw(r, "·", FontRole.Mono, new Rem(1.25f), TextAnchor.MiddleCenter, ThemeSlot.TextMuted);
        };
        return node;
    }


    private static LightweaveNode BuildStatColumn(string value, string label, ThemeSlot valueColor) {
        LightweaveNode node = NodeBuilder.New($"StatColumn:{value}");
        node.PreferredHeight = (new Rem(1.625f).ToPixels()) + (new Rem(0.125f).ToPixels()) + (new Rem(1.0f).ToPixels());
        Rem valueSize = new Rem(1.3333f);
        Rem labelSize = new Rem(0.875f);
        node.Paint = (rect, _) => {
            float valueH = new Rem(1.625f).ToPixels();
            float gap = new Rem(0.125f).ToPixels();

            Rect valueRect = new Rect(rect.x, rect.y, rect.width, valueH);
            TextDraw.Draw(valueRect, value, FontRole.Mono, valueSize, TextAnchor.UpperLeft, valueColor);

            if (!string.IsNullOrEmpty(label)) {
                Rect labelRect = new Rect(rect.x, rect.y + valueH + gap, rect.width, rect.height - valueH - gap);
                TextDraw.Draw(labelRect, label, FontRole.Mono, labelSize, TextAnchor.UpperLeft, ThemeSlot.TextMuted);
            }
        };

        node.MeasureWidth = () => {
            float valueW = TextDraw.Measure(value, FontRole.Mono, valueSize).x;
            float labelW = 0f;
            if (!string.IsNullOrEmpty(label)) {
                labelW = TextDraw.Measure(label, FontRole.Mono, labelSize).x;
            }
            return Mathf.Max(valueW, labelW);
        };

        node.Measure = availableWidth => new Rem(2.5f).ToPixels();
        node.PreferredHeight = new Rem(2.5f).ToPixels();

        return node;
    }

    

    private static string FormatWealth(float wealth) {
        if (wealth >= 1000f) {
            return Mathf.RoundToInt(wealth / 1000f) + "k";
        }
        return Mathf.RoundToInt(wealth).ToString();
    }

    private static LightweaveNode BuildContinueButton(SaveMetadata.LatestSave save) {
        LightweaveNode node = NodeBuilder.New("ContinueCard:ResumeButton");
        node.Style = new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
            LetterSpacing = Tracking.Widest,
        };
        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            InteractionState state = InteractionState.Resolve(rect, null, false);

            Color top = theme.GetColor(ThemeSlot.SurfaceAccent);
            Color.RGBToHSV(top, out float hue, out float sat, out float val);
            Color bottom = Color.HSVToRGB(hue, sat, val * 0.78f);
            bottom.a = top.a;

            BackgroundSpec.Gradient bg = new BackgroundSpec.Gradient(GradientTextureCache.Vertical(top, bottom));
            RadiusSpec radius = RadiusSpec.Right(RadiusScale.Lg);
            PaintBox.Draw(rect, bg, null, radius);

            float overlay = VariantPalette.OverlayAlpha(state);
            if (overlay > 0f) {
                Color overlayColor = InteractionFeedback.OverlayColor(theme, state, overlay);
                PaintBox.Draw(rect, BackgroundSpec.Of(overlayColor), null, radius);
            }

            Color inset = theme.GetColor(ThemeSlot.BorderDefault);
            inset.a = 0.4f;
            Rect leftStroke = new Rect(rect.x, rect.y, 1f, rect.height);
            PaintBox.Fill(leftStroke, inset);

            if (Event.current.type == EventType.Repaint) {
                Font? font = LightweaveFonts.CarlitoBold ?? LightweaveFonts.CarlitoRegular;
                Rem fontSizeRem = new Rem(1.25f);

                Style resolved = node.GetResolvedStyle();
                float letterSpacing = resolved.LetterSpacing.HasValue
                    ? Mathf.Max(0, Mathf.RoundToInt(resolved.LetterSpacing.Value.ToPixels(fontSizeRem.ToFontPx())))
                    : 0f;

                Color inkColor = theme.GetColor(ThemeSlot.TextOnAccent);
                string text = "▶  " + ((string)"CL_MainMenu_Continue_Action".Translate()).ToUpperInvariant();

                TextDraw.DrawTracked(
                    rect,
                    text,
                    FontRole.BodyBold,
                    fontSizeRem,
                    TextAnchor.MiddleCenter,
                    inkColor,
                    letterSpacing,
                    FontStyle.Normal,
                    font
                );
            }

            InteractionFeedback.Apply(rect, true, true);

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                MainMenuActions.ContinueLatestSave(save.FileName);
                e.Use();
            }
        };
        return node;
    }

    private static LightweaveNode BuildWelcome() {
        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.Xs, s => {
                s.Add(Eyebrow.Create("CL_MainMenu_Welcome_Eyebrow".Translate()));
                s.Add(Display.Create("CL_MainMenu_Welcome".Translate(), style: new Style { TextAlign = TextAlign.Start }, level: 3));
                s.Add(Text.Create(
                    "CL_MainMenu_Welcome_Hint".Translate(),
                    wrap: true,
                    style: new Style { TextColor = ThemeSlot.TextSecondary }
                ));
            })),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Md),
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                Border = BorderSpec.All(new Rem(0.0625f), ThemeSlot.BorderSubtle),
                Radius = RadiusSpec.All(RadiusScale.Md),
            }
        );
    }


}
