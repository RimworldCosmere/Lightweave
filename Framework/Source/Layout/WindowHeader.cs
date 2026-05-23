using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Display = Cosmere.Lightweave.Typography.Display;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Glyph = Cosmere.Lightweave.Typography.Glyph;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Layout;

public static class WindowHeader {
    public static LightweaveNode Create(
        string? crumb = null,
        string? title = null,
        string? subtitle = null,
        LightweaveNode? actions = null,
        LightweaveNode? headerContent = null,
        EdgeInsets? headerContentPadding = null,
        IReadOnlyList<WindowHeaderTab>? secondaryActions = null,
        Action? onClose = null,
        bool draggable = true,
        bool drawDivider = true,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        bool hasCrumb = !string.IsNullOrEmpty(crumb);
        bool hasClose = onClose != null;
        bool hasCrumbRow = hasCrumb;
        bool hasTitle = !string.IsNullOrEmpty(title);
        bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
        bool hasTitleBlock = hasTitle || hasSubtitle;
        bool hasActions = actions != null;
        bool closeInTitleRow = hasClose && !hasCrumbRow;
        bool hasTitleRow = hasTitleBlock || hasActions || closeInTitleRow;
        bool hasHeaderContent = headerContent != null;
        IReadOnlyList<WindowHeaderTab> secondary = secondaryActions ?? Array.Empty<WindowHeaderTab>();
        bool hasSecondary = secondary.Count > 0;

        Rem padX = new Rem(1.375f);

        LightweaveNode inner = Stack.Create(SpacingScale.None, s => {
            if (hasCrumbRow) {
                s.Add(HStack.Create(SpacingScale.Sm, row => {
                    if (hasCrumb) {
                        row.AddHug(Eyebrow.Create(crumb!, style: new Style {
                            FontFamily = FontRole.Mono,
                            FontSize = new Rem(0.7f),
                            LetterSpacing = Tracking.Of(0.18f),
                            TextColor = ThemeSlot.TextMuted,
                        }));
                    }
                    row.AddFlex(Spacer.Flex());
                    if (hasClose) {
                        row.AddHug(BuildCloseAction(onClose));
                    }
                }, style: new Style {
                    Padding = new EdgeInsets(
                        Top: new Rem(0.875f),
                        Right: padX,
                        Bottom: new Rem(0f),
                        Left: padX
                    ),
                }));
            }

            if (hasTitleRow) {
                s.Add(HStack.Create(SpacingScale.Lg, row => {
                    if (hasTitleBlock) {
                        row.AddFlex(Stack.Create(SpacingScale.Xs, titleStack => {
                            if (hasTitle) {
                                titleStack.Add(Display.Create(title!.ToUpperInvariant(), level: 2, style: new Style {
                                    FontFamily = FontRole.Display,
                                    FontSize = new Rem(2.25f),
                                    LetterSpacing = Tracking.Of(0.06f),
                                    TextColor = ThemeSlot.TextPrimary,
                                }));
                            }
                            if (hasSubtitle) {
                                titleStack.Add(Text.Create(subtitle!, wrap: true, style: new Style {
                                    FontFamily = FontRole.Display,
                                    FontWeight = FontStyle.Italic,
                                    FontSize = new Rem(0.97f),
                                    LetterSpacing = Tracking.Of(0.02f),
                                    TextColor = ThemeSlot.TextSecondary,
                                }));
                            }
                        }));
                    }
                    else {
                        row.AddFlex(Spacer.Flex());
                    }
                    if (hasActions) {
                        row.AddHug(actions!);
                    }
                    if (closeInTitleRow) {
                        row.AddHug(BuildCloseAction(onClose));
                    }
                }, style: new Style {
                    Padding = new EdgeInsets(
                        Top: hasCrumbRow ? new Rem(0.875f) : new Rem(1.25f),
                        Right: padX,
                        Bottom: new Rem(1.125f),
                        Left: padX
                    ),
                }));
            }

            if (hasHeaderContent) {
                s.Add(Box.Create(
                    children: b => b.Add(headerContent!),
                    style: new Style {
                        Padding = headerContentPadding ?? new EdgeInsets(
                            Top: new Rem(0f),
                            Right: padX,
                            Bottom: new Rem(0.875f),
                            Left: padX
                        ),
                    }
                ));
            }

            if (hasSecondary) {
                s.Add(Divider.Horizontal());
                s.Add(HStack.Create(SpacingScale.Md, row => {
                    for (int i = 0; i < secondary.Count; i++) {
                        row.AddHug(BuildTabPill(secondary[i]));
                    }
                    row.AddFlex(Spacer.Flex());
                }, style: new Style {
                    Padding = new EdgeInsets(
                        Top: new Rem(0.625f),
                        Right: padX,
                        Bottom: new Rem(0.75f),
                        Left: padX
                    ),
                }));
            }

            if (drawDivider) {
                s.Add(Divider.Horizontal());
            }
        });

        LightweaveNode root = Box.Create(
            children: b => b.Add(inner),
            style: style,
            classes: StyleExtensions.PrependClass("window-header", classes),
            id: id,
            line: line,
            file: file
        );

        Action<Rect, Action>? innerPaint = root.Paint;
        root.Paint = (rect, paintChildren) => {
            LightweaveWindowContext.PublishHeader(rect, draggable, ownsClose: onClose != null);
            innerPaint?.Invoke(rect, paintChildren);
        };

        return root;
    }

    private static LightweaveNode BuildCloseAction(Action? onClick) {
        LightweaveNode node = NodeBuilder.New("WindowHeader.Close", 0, "");
        node.ApplyStyling("window-header-close", null, null, null);
        float sizePx = new Rem(1.75f).ToPixels();
        node.PreferredHeight = sizePx;

        Color restColor = new Color(0.722f, 0.706f, 0.671f, 0.92f);
        Color hoverColor = new Color(0.910f, 0.890f, 0.847f, 1f);
        Color restBorder = new Color(0.557f, 0.541f, 0.510f, 0.45f);
        Color hoverBorder = new Color(0.722f, 0.706f, 0.671f, 0.75f);
        Color hoverBg = new Color(0.157f, 0.125f, 0.086f, 0.50f);

        LightweaveNode BuildGlyph(Color color) {
            return Glyph.Create(Icons.Phosphor.X, style: new Style {
                FontSize = new Rem(0.8125f),
                TextColor = color,
            });
        }

        node.MeasureWidth = () => sizePx;

        node.Paint = (rect, _) => {
            float size = Mathf.Min(sizePx, Mathf.Min(rect.width, rect.height));
            Rect square = new Rect(
                rect.x + (rect.width - size) / 2f,
                rect.y + (rect.height - size) / 2f,
                size,
                size
            );

            LightweaveHitTracker.Track(square);
            InteractionState state = InteractionState.Resolve(square, null, false);

            BackgroundSpec? hoverBgSpec = state.Hovered ? BackgroundSpec.Of(hoverBg) : null;
            PaintBox.Draw(
                square,
                hoverBgSpec,
                BorderSpec.All(new Rem(1f / 16f), state.Hovered ? hoverBorder : restBorder),
                null
            );

            LightweaveNode glyph = BuildGlyph(state.Hovered ? hoverColor : restColor);
            LightweaveRoot.PaintSubtree(glyph, square);

            InteractionFeedback.Apply(square, true, true);
            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && square.Contains(e.mousePosition)) {
                onClick?.Invoke();
                SoundDefOf.Click.PlayOneShotOnCamera();
                e.Use();
            }
            MouseoverSounds.DoRegion(square);
        };
        return node;
    }

    private static LightweaveNode BuildTabPill(WindowHeaderTab tab) {
        LightweaveNode node = NodeBuilder.New($"WindowHeader.Tab:{tab.Label}", 0, "");
        node.ApplyStyling("window-header-tab", null, null, null);
        node.PreferredHeight = new Rem(1.625f).ToPixels();
        float tabPadPx = SpacingScale.Xxs.ToPixels();

        LightweaveNode BuildLabelNode(Color color) {
            return Eyebrow.Create(tab.Label, style: new Style {
                FontFamily = FontRole.BodyBold,
                FontSize = new Rem(0.8f),
                LetterSpacing = Tracking.Of(0.12f),
                TextAlign = TextAlign.Center,
                TextColor = color,
            });
        }

        node.MeasureWidth = () => {
            LightweaveNode probe = BuildLabelNode(Color.white);
            float textW = probe.MeasureWidth?.Invoke() ?? 0f;
            return textW + tabPadPx * 2f;
        };

        node.Paint = (rect, _) => {
            LightweaveHitTracker.Track(rect);
            InteractionState state = InteractionState.Resolve(rect, null, false);
            Theme.Theme theme = RenderContext.Current.Theme;
            ThemeSlot fgSlot = tab.IsActive
                ? ThemeSlot.SurfaceAccent
                : (state.Hovered ? ThemeSlot.TextPrimary : ThemeSlot.TextMuted);
            LightweaveNode painted = BuildLabelNode(theme.GetColor(fgSlot));
            LightweaveRoot.PaintSubtree(painted, rect);
            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                tab.OnClick?.Invoke();
                SoundDefOf.Click.PlayOneShotOnCamera();
                e.Use();
            }
            MouseoverSounds.DoRegion(rect);
        };
        return node;
    }
}
