using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Blocks;
using Cosmere.Lightweave.Feedback;
using Avatar = Cosmere.Lightweave.Data.Avatar;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Cosmere.Lightweave.Hooks.Hooks;
using static Cosmere.Lightweave.Typography.Typography;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Layout;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Blocks;

[Doc(
    Id = "carousel",
    Summary = "Slide-paged carousel with optional arrows and dots.",
    WhenToUse = "Cycle through a small set of equally-weighted views.",
    SourcePath = "Lightweave/Layout/Carousel.cs",
    PreferredVariantHeight = 200f
)]
public static class Carousel {
    private const float SlideDurationSeconds = 0.28f;
    private static readonly float ArrowColumnWidth = new Rem(3.5f).ToPixels();
    private static readonly float ColumnGap = new Rem(0.875f).ToPixels();
    private static readonly float DotBarWidth = new Rem(1.75f).ToPixels();
    private static readonly float DotBarHeight = new Rem(0.1875f).ToPixels();
    private static readonly float DotGapPx = new Rem(0.375f).ToPixels();
    private static readonly float DotStripHeight = new Rem(1.125f).ToPixels();
    private static readonly Func<float, float> EaseOutCubic = t => 1f - Mathf.Pow(1f - t, 3f);

    public static LightweaveNode Create(
        [DocParam("Slide nodes shown one at a time.")]
    IReadOnlyList<LightweaveNode> slides,
        [DocParam("Index of the leftmost visible slide.")]
    int currentIndex,
        [DocParam("Callback invoked when the user changes slides.")]
    Action<int> onIndexChange,
        [DocParam("Show left/right arrow controls.")]
    bool showArrows = true,
        [DocParam("Show pagination dot strip.")]
    bool showDots = true,
        [DocParam("Number of slides visible side-by-side. Defaults to 1.")]
    int visible = 1,
        [DocParam("Wrap around at the edges. Arrows never disable; index wraps via modulo.")]
    bool loop = false,
        [DocParam("Enable arrow-key navigation while the carousel is hovered.")]
    bool keyboardEnabled = true,
        [DocParam("Override hover sound on arrows/dots. Null = component default (false).")]
    bool? playHoverSound = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Carousel", line, file);
        node.ApplyStyling("carousel", style, classes, id);

        for (int i = 0; i < slides.Count; i++) {
            node.Children.Add(slides[i]);
        }

        node.MeasureWidth = () => {
            if (slides == null || slides.Count == 0) {
                return 0f;
            }
            float maxSlideW = 0f;
            for (int i = 0; i < slides.Count; i++) {
                float w = slides[i].MeasureWidth?.Invoke() ?? 0f;
                if (w > maxSlideW) {
                    maxSlideW = w;
                }
            }
            return maxSlideW;
        };

        node.Measure = availableWidth => {
            if (slides == null || slides.Count == 0) {
                return showDots ? DotStripHeight : 0f;
            }

            int count = slides.Count;
            int visibleClamped = Mathf.Clamp(visible, 1, count);
            bool wantArrows = showArrows && count > visibleClamped;
            bool wantDots = showDots && count > visibleClamped;
            float arrowSpace = wantArrows ? (ArrowColumnWidth + ColumnGap) * 2f : 0f;
            float viewportWidth = Mathf.Max(0f, availableWidth - arrowSpace);
            float interSlideGap = visibleClamped > 1 ? ColumnGap : 0f;
            float slotWidth = visibleClamped > 0
                ? (viewportWidth - (visibleClamped - 1) * interSlideGap) / visibleClamped
                : viewportWidth;
            float dotStripTotal = wantDots ? DotStripHeight + ColumnGap : 0f;

            float maxSlideH = 0f;
            for (int i = 0; i < count; i++) {
                float h = slides[i].Measure?.Invoke(slotWidth) ?? slides[i].PreferredHeight ?? 0f;
                if (h > maxSlideH) {
                    maxSlideH = h;
                }
            }

            return maxSlideH + dotStripTotal;
        };

        node.Paint = (rect, _) => {
            if (slides == null || slides.Count == 0) {
                return;
            }

            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            int count = slides.Count;
            int visibleClamped = Mathf.Clamp(visible, 1, count);
            int maxIndex = loop ? count - 1 : Mathf.Max(0, count - visibleClamped);
            int clamped = loop
                ? ((currentIndex % count) + count) % count
                : Mathf.Clamp(currentIndex, 0, maxIndex);

            int animTarget = clamped;
            if (loop) {
                RefHandle<int> virt = UseRef(clamped);
                int prev = virt.Current;
                int realPrev = ((prev % count) + count) % count;
                int delta = clamped - realPrev;
                if (delta > count / 2) {
                    delta -= count;
                }
                if (delta < -count / 2) {
                    delta += count;
                }

                virt.Current = prev + delta;
                animTarget = virt.Current;
            }

            float animIndex = UseAnim.Animate(
                animTarget,
                SlideDurationSeconds,
                EaseOutCubic,
                line,
                file + "#carIndex"
            );

            bool wantArrows = showArrows && count > visibleClamped;
            bool wantDots = showDots && count > visibleClamped;
            float dotStripTotal = wantDots ? DotStripHeight + ColumnGap : 0f;
            Rect rowRect = new Rect(rect.x, rect.y, rect.width, Mathf.Max(0f, rect.height - dotStripTotal));
            Rect dotRect = wantDots
                ? new Rect(rect.x, rowRect.yMax + ColumnGap, rect.width, DotStripHeight)
                : default;

            float arrowSpace = wantArrows ? (ArrowColumnWidth + ColumnGap) * 2f : 0f;
            Rect leftArrow = wantArrows
                ? new Rect(rowRect.x, rowRect.y, ArrowColumnWidth, rowRect.height)
                : default;
            Rect rightArrow = wantArrows
                ? new Rect(rowRect.xMax - ArrowColumnWidth, rowRect.y, ArrowColumnWidth, rowRect.height)
                : default;
            Rect viewport = new Rect(
                rowRect.x + (wantArrows ? ArrowColumnWidth + ColumnGap : 0f),
                rowRect.y,
                Mathf.Max(0f, rowRect.width - arrowSpace),
                rowRect.height
            );

            float interSlideGap = visibleClamped > 1 ? ColumnGap : 0f;
            float slotWidth = visibleClamped > 0
                ? (viewport.width - (visibleClamped - 1) * interSlideGap) / visibleClamped
                : viewport.width;
            float stride = slotWidth + interSlideGap;

            using (ClipScope.Begin(viewport)) {
                if (loop) {
                    float wrappedAnim = ((animIndex % count) + count) % count;
                    for (int i = 0; i < count; i++) {
                        float diff = i - wrappedAnim;
                        if (diff > count / 2f) {
                            diff -= count;
                        }
                        if (diff < -count / 2f) {
                            diff += count;
                        }

                        float slideX = rtl ? -diff * stride : diff * stride;
                        if (slideX + slotWidth < 0f || slideX > viewport.width) {
                            continue;
                        }

                        Rect slideRect = new Rect(slideX, 0f, slotWidth, viewport.height);
                        slides[i].MeasuredRect = slideRect;
                        LightweaveRoot.PaintSubtree(slides[i], slideRect);
                    }
                }
                else {
                    float baseX = -animIndex * stride;
                    if (rtl) {
                        baseX = animIndex * stride;
                    }

                    for (int i = 0; i < count; i++) {
                        float slideX = rtl
                            ? baseX - i * stride
                            : baseX + i * stride;
                        if (slideX + slotWidth < 0f || slideX > viewport.width) {
                            continue;
                        }

                        Rect slideRect = new Rect(slideX, 0f, slotWidth, viewport.height);
                        slides[i].MeasuredRect = slideRect;
                        LightweaveRoot.PaintSubtree(slides[i], slideRect);
                    }
                }
            }

            bool soundEnabled = playHoverSound ?? false;

            if (wantArrows) {
                Rect leftZone = leftArrow;
                Rect rightZone = rightArrow;
                bool dimLeft = !loop && clamped == 0;
                bool dimRight = !loop && clamped == maxIndex;
                DrawArrow(leftZone, theme, true, dimLeft);
                DrawArrow(rightZone, theme, false, dimRight);

                Cosmere.Lightweave.Input.InteractionFeedback.Apply(leftZone, !dimLeft, soundEnabled);
                Cosmere.Lightweave.Input.InteractionFeedback.Apply(rightZone, !dimRight, soundEnabled);
                LightweaveHitTracker.Track(leftZone);
                LightweaveHitTracker.Track(rightZone);

                int Step(int from, int direction) {
                    if (loop) {
                        return ((from + direction) % count + count) % count;
                    }

                    int target = from + direction;
                    return Mathf.Clamp(target, 0, maxIndex);
                }

                Event e = Event.current;
                if (e.type == EventType.MouseUp && e.button == 0) {
                    if (leftZone.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(leftZone)) {
                        int next = rtl ? Step(clamped, 1) : Step(clamped, -1);
                        if (next != clamped) {
                            onIndexChange?.Invoke(next);
                            e.Use();
                        }
                    }
                    else if (rightZone.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(rightZone)) {
                        int next = rtl ? Step(clamped, -1) : Step(clamped, 1);
                        if (next != clamped) {
                            onIndexChange?.Invoke(next);
                            e.Use();
                        }
                    }
                }
            }

            if (wantDots) {
                float totalDotsWidth = count * DotBarWidth + (count - 1) * DotGapPx;
                float startX = dotRect.x + (dotRect.width - totalDotsWidth) / 2f;
                float dotY = dotRect.y + (dotRect.height - DotBarHeight) / 2f;

                Event e = Event.current;

                Color dimColor = new Color(0.769f, 0.667f, 0.510f, 0.16f);
                Color visibleColor = new Color(0.769f, 0.667f, 0.510f, 0.4f);
                Color activeColor = theme.GetColor(ThemeSlot.SurfaceAccent);

                for (int i = 0; i < count; i++) {
                    int logical = rtl ? count - 1 - i : i;
                    float dotX = startX + i * (DotBarWidth + DotGapPx);
                    Rect dot = new Rect(dotX, dotY, DotBarWidth, DotBarHeight);
                    Rect hitRect = new Rect(dot.x - DotGapPx / 2f, dotRect.y, dot.width + DotGapPx, dotRect.height);

                    bool inWindow;
                    if (loop) {
                        int rel = ((logical - clamped) % count + count) % count;
                        inWindow = rel < visibleClamped;
                    }
                    else {
                        inWindow = logical >= clamped && logical < clamped + visibleClamped;
                    }
                    bool isLead = logical == clamped;
                    Color col = isLead ? activeColor : inWindow ? visibleColor : dimColor;
                    PaintBox.Fill(dot, col);

                    Cosmere.Lightweave.Input.InteractionFeedback.Apply(hitRect, !isLead, soundEnabled);
                    LightweaveHitTracker.Track(hitRect);
                    if (e.type == EventType.MouseUp && e.button == 0 && hitRect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(hitRect)) {
                        int next = Mathf.Clamp(logical, 0, maxIndex);
                        if (next != clamped) {
                            onIndexChange?.Invoke(next);
                            e.Use();
                        }
                    }
                }
            }

            if (keyboardEnabled && count > visibleClamped && rect.Contains(Event.current.mousePosition)) {
                int KbStep(int from, int direction) {
                    if (loop) {
                        return ((from + direction) % count + count) % count;
                    }

                    int target = from + direction;
                    return Mathf.Clamp(target, 0, maxIndex);
                }

                Event ke = Event.current;
                if (ke.type == EventType.KeyDown) {
                    if (ke.keyCode == KeyCode.LeftArrow) {
                        int next = rtl ? KbStep(clamped, 1) : KbStep(clamped, -1);
                        if (next != clamped) {
                            onIndexChange?.Invoke(next);
                            ke.Use();
                        }
                    }
                    else if (ke.keyCode == KeyCode.RightArrow) {
                        int next = rtl ? KbStep(clamped, -1) : KbStep(clamped, 1);
                        if (next != clamped) {
                            onIndexChange?.Invoke(next);
                            ke.Use();
                        }
                    }
                }
            }
        };

        return node;
    }

    private static void DrawArrow(Rect rect, Theme.Theme theme, bool pointLeft, bool dimmed) {
        bool hovered = !dimmed && Mouse.IsOver(rect);
        Color bgIdle = new Color(0.0588f, 0.047f, 0.0314f, 0.5f);
        Color bgHover = new Color(0.098f, 0.078f, 0.055f, 0.7f);
        Color background = hovered ? bgHover : bgIdle;
        if (dimmed) {
            background = new Color(background.r, background.g, background.b, background.a * 0.4f);
        }
        PaintBox.Fill(rect, background);

        Color borderCol = hovered
            ? theme.GetColor(ThemeSlot.SurfaceAccent)
            : theme.GetColor(ThemeSlot.BorderDefault);
        if (dimmed) {
            borderCol = new Color(borderCol.r, borderCol.g, borderCol.b, borderCol.a * 0.4f);
        }
        PaintBox.Draw(rect, null, BorderSpec.All(new Rem(1f / 16f), borderCol), null);

        Color glyphColor = hovered
            ? theme.GetColor(ThemeSlot.SurfaceAccent)
            : theme.GetColor(ThemeSlot.TextSecondary);
        if (dimmed) {
            glyphColor = new Color(glyphColor.r, glyphColor.g, glyphColor.b, 0.25f);
        }

        IconRef icon = pointLeft ? Icons.Phosphor.CaretLeft : Icons.Phosphor.CaretRight;
        Font? iconFont = icon.ResolveFont();
        TextDraw.Draw(
            rect,
            icon.Glyph,
            FontRole.Body,
            new Rem(1.375f),
            TextAnchor.MiddleCenter,
            glyphColor,
            fontOverride: iconFont
        );
    }

    private static LightweaveNode DocsSlide(ThemeSlot bg, string labelKey) {
        return Box.Create(
            c => c.Add(
                Text.Create(
                    (string)labelKey.Translate(),
                    style: new Style { FontFamily = FontRole.BodyBold, FontSize = new Rem(1f), TextColor = ThemeSlot.TextPrimary, TextAlign = TextAlign.Center }
                )
            ),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Md),
                Background = BackgroundSpec.Of(bg),
            }
        );
    }


    private static LightweaveNode DocsPawnCard(
        string id,
        string initials,
        string name,
        string tag,
        string desc,
        bool isSelected,
        Action onClick
    ) {
        ThemeSlot borderSlot = isSelected ? ThemeSlot.SurfaceAccent : ThemeSlot.BorderDefault;
        Rem borderWidth = isSelected ? new Rem(2f / 16f) : new Rem(1f / 16f);
        Rem pillHeightRem = new Rem(1.5f);
        float pillHeightPx = pillHeightRem.ToPixels();
        float halfPillPx = pillHeightPx / 2f;

        LightweaveNode portrait = HStack.Create(
            gap: new Rem(0f),
            children: row => {
                row.AddFlex(Spacer.Fixed(new Rem(0f)));
                row.AddHug(Avatar.Create(
                    initials,
                    size: new Rem(4f),
                    accent: isSelected ? ThemeSlot.SurfaceAccent : null
                ));
                row.AddFlex(Spacer.Fixed(new Rem(0f)));
            }
        );

        LightweaveNode nameNode = Text.Create(
            name,
            style: new Style {
                FontFamily = FontRole.BodyBold,
                FontSize = new Rem(1.125f),
                TextColor = ThemeSlot.TextPrimary,
                TextAlign = TextAlign.Center,
            }
        );

        LightweaveNode tagNode = Text.Create(
            tag,
            wrap: true,
            style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.8125f),
                FontWeight = FontStyle.Italic,
                TextColor = ThemeSlot.TextSecondary,
                TextAlign = TextAlign.Center,
            }
        );

        LightweaveNode description = Text.Create(
            desc,
            wrap: true,
            style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.8125f),
                TextColor = ThemeSlot.TextSecondary,
            }
        );

        LightweaveNode card = Box.Create(
            c => c.Add(Stack.Create(
                gap: new Rem(0.5f),
                children: col => {
                    col.Add(portrait);
                    col.Add(nameNode);
                    col.Add(tagNode);
                    col.Add(description);
                }
            )),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Md),
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                Border = BorderSpec.All(borderWidth, borderSlot),
                Radius = RadiusSpec.All(RadiusScale.Sm),
            }
        );

        LightweaveNode pillRow = HStack.Create(
            gap: new Rem(0f),
            children: row => {
                row.AddFlex(Spacer.Fixed(new Rem(0f)));
                row.AddHug(Box.Create(
                    inner => {
                        if (isSelected) {
                            inner.Add(Text.Create(
                                (string)"CL_Playground_carousel_Selected".Translate(),
                                style: new Style {
                                    FontFamily = FontRole.BodyBold,
                                    FontSize = new Rem(0.6875f),
                                    TextColor = ThemeSlot.SurfaceAccent,
                                    TextAlign = TextAlign.Center,
                                }
                            ));
                        }
                    },
                    style: new Style {
                        Height = pillHeightRem,
                        Padding = new EdgeInsets(SpacingScale.Xxs, SpacingScale.Sm, SpacingScale.Xxs, SpacingScale.Sm),
                        Background = isSelected ? BackgroundSpec.Of(ThemeSlot.SurfaceRaised) : null,
                        Border = isSelected ? BorderSpec.All(new Rem(1f / 16f), ThemeSlot.SurfaceAccent) : null,
                        Radius = RadiusSpec.All(RadiusScale.Sm),
                    }
                ));
                row.AddFlex(Spacer.Fixed(new Rem(0f)));
            }
        );

        LightweaveNode wrap = NodeBuilder.New($"PawnCard:{id}", 0, "");
        wrap.ApplyStyling("pawn-card", null, null, id);
        wrap.Children.Add(card);
        wrap.Children.Add(pillRow);
        wrap.MeasureWidth = () => card.MeasureWidth?.Invoke() ?? 0f;
        wrap.Measure = w => (card.Measure?.Invoke(w) ?? card.PreferredHeight ?? 0f) + halfPillPx;
        wrap.Paint = (rect, _) => {
            Rect cardRect = new Rect(rect.x, rect.y, rect.width, rect.height - halfPillPx);
            card.MeasuredRect = cardRect;
            LightweaveRoot.PaintSubtree(card, cardRect);

            Rect pillRect = new Rect(rect.x, cardRect.yMax - halfPillPx, rect.width, pillHeightPx);
            pillRow.MeasuredRect = pillRect;
            LightweaveRoot.PaintSubtree(pillRow, pillRect);

            if (!isSelected && Mouse.IsOver(cardRect)) {
                Widgets.DrawHighlight(cardRect);
            }
            if (Widgets.ButtonInvisible(cardRect, doMouseoverSound: false)) {
                onClick?.Invoke();
            }
        };
        return wrap;
    }

    private static LightweaveNode DocsPawnInfoCard(string initials, string name, string tag, string desc, string world, string path) {
        LightweaveNode badge(string label, ThemeSlot accent) {
            return Box.Create(
                inner => inner.Add(Text.Create(
                    label,
                    style: new Style {
                        FontFamily = FontRole.BodyBold,
                        FontSize = new Rem(0.6875f),
                        TextColor = accent,
                    }
                )),
                style: new Style {
                    Padding = new EdgeInsets(SpacingScale.Xxs, SpacingScale.Sm, SpacingScale.Xxs, SpacingScale.Sm),
                    Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                    Border = BorderSpec.All(new Rem(1f / 16f), accent),
                    Radius = RadiusSpec.All(RadiusScale.Sm),
                }
            );
        }

        LightweaveNode portrait = Avatar.Create(initials, size: new Rem(3.5f), accent: ThemeSlot.SurfaceAccent);

        LightweaveNode rightCol = Stack.Create(
            gap: new Rem(0.25f),
            children: col => {
                col.Add(Heading.Create(3, name));
                col.Add(Text.Create(
                    tag,
                    style: new Style {
                        FontFamily = FontRole.Body,
                        FontSize = new Rem(0.875f),
                        FontWeight = FontStyle.Italic,
                        TextColor = ThemeSlot.TextSecondary,
                    }
                ));
                col.Add(Text.Create(
                    desc,
                    wrap: true,
                    style: new Style {
                        FontFamily = FontRole.Body,
                        FontSize = new Rem(0.875f),
                        TextColor = ThemeSlot.TextPrimary,
                    }
                ));
                col.Add(HStack.Create(
                    gap: new Rem(0.5f),
                    children: row => {
                        row.AddHug(badge(world, ThemeSlot.SurfaceAccent));
                        row.AddHug(badge(path, ThemeSlot.TextSecondary));
                        row.AddFlex(Spacer.Fixed(new Rem(0f)));
                    }
                ));
            }
        );

        return Box.Create(
            c => c.Add(HStack.Create(
                gap: new Rem(1f),
                children: row => {
                    row.AddHug(portrait);
                    row.AddFlex(rightCol);
                }
            )),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Lg),
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.SurfaceAccent),
                Radius = RadiusSpec.All(RadiusScale.Sm),
            }
        );
    }

    [DocVariant("CL_Playground_Label_Single")]
    public static DocSample DocsSingle() {
        StateHandle<int> index = UseState(0);
        return new DocSample(() =>
            Carousel.Create(
                new List<LightweaveNode> {
                DocsSlide(ThemeSlot.SurfaceRaised, "CL_Playground_carousel_Slide_First"),
                DocsSlide(ThemeSlot.SurfaceAccent, "CL_Playground_carousel_Slide_Second"),
                DocsSlide(ThemeSlot.SurfacePrimary, "CL_Playground_carousel_Slide_Third"),
                DocsSlide(ThemeSlot.SurfaceSunken, "CL_Playground_carousel_Slide_Fourth"),
                },
                index.Value,
                i => index.Set(i)
            )
        );
    }

    [DocVariant("CL_Playground_Label_Multi", Order = 1)]
    public static DocSample DocsMulti() {
        StateHandle<int> index = UseState(0);
        StateHandle<string> selectedId = UseState("vin");
        return new DocSample(
            useFullSource: true,
            helpers: new[] { nameof(DocsPawnCard), nameof(DocsPawnInfoCard) },
            build: () => {
            (string id, string initials, string name, string tag, string desc, string world, string path)[] pawns = new[] {
                ("vin",     "V",  "Vin",     "Inheritor of the storms.",   "A noble heir hidden as a thieving urchin. Pewter and tin in her veins.", "Scadrial",  "Mistborn"),
                ("kaladin", "K",  "Kaladin", "Bridge Four leads.",         "Surgebinder bonded to Syl. Carries his crew on his shoulders.",          "Roshar",    "Windrunner"),
                ("shallan", "S",  "Shallan", "Truth told through illusion.", "Scholar and liar. Patternblade. Three personae in one mind.",            "Roshar",    "Lightweaver"),
                ("kelsier", "Ks", "Kelsier", "Survivor of the Pits.",      "Mistborn revolutionary. The Lord Ruler's nemesis. Charisma weaponized.", "Scadrial",  "Mistborn"),
                ("dalinar", "D",  "Dalinar", "Unite them.",                "Highprince of war turned king. Bound to the Stormfather.",               "Roshar",    "Bondsmith"),
                ("sazed",   "Sz", "Sazed",   "A keeper of religions.",     "Terris Feruchemist. Holds the memory of six hundred faiths.",            "Scadrial",  "Feruchemist"),
            };

            List<LightweaveNode> slides = new List<LightweaveNode>(pawns.Length);
            int selectedIdx = 0;
            for (int i = 0; i < pawns.Length; i++) {
                var p = pawns[i];
                bool isSelected = selectedId.Value == p.id;
                if (isSelected) {
                    selectedIdx = i;
                }
                string capturedId = p.id;
                slides.Add(DocsPawnCard(
                    p.id, p.initials, p.name, p.tag, p.desc,
                    isSelected,
                    () => selectedId.Set(capturedId)
                ));
            }

            var sel = pawns[selectedIdx];

            return Stack.Create(
                gap: new Rem(0.75f),
                children: col => {
                    col.Add(Carousel.Create(
                        slides,
                        index.Value,
                        i => index.Set(i),
                        visible: 3
                    ));
                    col.Add(DocsPawnInfoCard(sel.initials, sel.name, sel.tag, sel.desc, sel.world, sel.path));
                }
            );
        });
    }


    [DocVariant("CL_Playground_Label_Loop", Order = 2)]
    public static DocSample DocsLoop() {
        StateHandle<int> index = UseState(0);
        return new DocSample(() =>
            Carousel.Create(
                new List<LightweaveNode> {
                DocsSlide(ThemeSlot.SurfaceRaised, "CL_Playground_carousel_Slide_First"),
                DocsSlide(ThemeSlot.SurfaceAccent, "CL_Playground_carousel_Slide_Second"),
                DocsSlide(ThemeSlot.SurfacePrimary, "CL_Playground_carousel_Slide_Third"),
                DocsSlide(ThemeSlot.SurfaceSunken, "CL_Playground_carousel_Slide_Fourth"),
                },
                index.Value,
                i => index.Set(i),
                loop: true
            )
        );
    }


    [DocVariant("CL_Playground_Label_Autoplay")]
    public static DocSample DocsAutoplay() {
        return new DocSample(
            () => BuildAutoplayDemo(),
            useFullSource: true,
            helpers: new[] { nameof(DocsSlide) }
        );
    }

    private static LightweaveNode BuildAutoplayDemo() {
        const float autoplaySeconds = 3f;
        StateHandle<int> index = UseState(0);
        RefHandle<float> lastTick = UseRef(Time.realtimeSinceStartup);

        List<LightweaveNode> slides = new List<LightweaveNode> {
            DocsSlide(ThemeSlot.SurfaceRaised, "CL_Playground_carousel_Slide_First"),
            DocsSlide(ThemeSlot.SurfaceAccent, "CL_Playground_carousel_Slide_Second"),
            DocsSlide(ThemeSlot.SurfacePrimary, "CL_Playground_carousel_Slide_Third"),
        };

        LightweaveNode carousel = Carousel.Create(slides, index.Value, i => index.Set(i));

        LightweaveNode wrapper = NodeBuilder.New("CarouselAutoplay");
        wrapper.Children.Add(carousel);
        wrapper.Measure = carousel.Measure;
        wrapper.PreferredHeight = carousel.PreferredHeight;
        wrapper.Paint = (rect, _) => {
            float now = Time.realtimeSinceStartup;
            if (Mouse.IsOver(rect)) {
                lastTick.Current = now;
            }
            else if (now - lastTick.Current >= autoplaySeconds) {
                int next = (index.Value + 1) % slides.Count;
                index.Set(next);
                lastTick.Current = now;
            }

            carousel.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(carousel, rect);
        };

        return wrapper;
    }

    [DocVariant("CL_Playground_Label_Keyboard")]
    public static DocSample DocsKeyboard() {
        return new DocSample(
            () => BuildKeyboardDemo(),
            useFullSource: true,
            helpers: new[] { nameof(DocsSlide) }
        );
    }

    private static LightweaveNode BuildKeyboardDemo() {
        StateHandle<int> index = UseState(0);

        List<LightweaveNode> slides = new List<LightweaveNode> {
            DocsSlide(ThemeSlot.SurfaceRaised, "CL_Playground_carousel_Slide_First"),
            DocsSlide(ThemeSlot.SurfaceAccent, "CL_Playground_carousel_Slide_Second"),
            DocsSlide(ThemeSlot.SurfacePrimary, "CL_Playground_carousel_Slide_Third"),
        };

        LightweaveNode carousel = Carousel.Create(
            slides,
            index.Value,
            i => index.Set(i),
            keyboardEnabled: false
        );

        LightweaveNode wrapper = NodeBuilder.New("CarouselKeyboard");
        wrapper.Children.Add(carousel);
        wrapper.Measure = carousel.Measure;
        wrapper.PreferredHeight = carousel.PreferredHeight;
        wrapper.Paint = (rect, _) => {
            UseHotkey.Use(KeyCode.LeftArrow, () => index.Set(Mathf.Max(0, index.Value - 1)));
            UseHotkey.Use(KeyCode.RightArrow, () => index.Set(Mathf.Min(slides.Count - 1, index.Value + 1)));

            carousel.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(carousel, rect);
        };

        return wrapper;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        StateHandle<int> index = UseState(0);
        return new DocSample(() => 
            Carousel.Create(
                new List<LightweaveNode> {
                DocsSlide(ThemeSlot.SurfaceRaised, "CL_Playground_carousel_Slide_First"),
                DocsSlide(ThemeSlot.SurfaceAccent, "CL_Playground_carousel_Slide_Second"),
                },
                index.Value,
                i => index.Set(i)
            )
        );
    }
}
