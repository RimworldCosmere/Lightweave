using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Typography.Typography;
using Verse;
using Cosmere.Lightweave.Surfaces;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Tokens;

[Doc(
    Id = "responsive",
    Summary = "Display-resolution tiers (720p, 1080p, 1440p, 4K) resolved from the screen width.",
    WhenToUse = "Read RenderContext.Current.Breakpoint or use Breakpoints.Pick<T> when layout should adapt to the monitor resolution.",
    SourcePath = "Lightweave/Tokens/Breakpoint.cs",
    Category = "Foundation",
    PreferredVariantHeight = 96f,
    HideUsage = true,
    HideSource = true
)]
public static class BreakpointsDoc {

    [DocVariant("CL_Playground_responsive_Readout")]
    public static DocSample DocsReadout() {
        return new DocSample(() => {
            Breakpoint bp = RenderContext.Current.Breakpoint;
            float minPx = bp switch {
                Breakpoint.P1080 => Breakpoints.P1080MinPx,
                Breakpoint.P1440 => Breakpoints.P1440MinPx,
                Breakpoint.P2160 => Breakpoints.P2160MinPx,
                _ => 0f,
            };
            string key = bp switch {
                Breakpoint.P1080 => "CL_Playground_Breakpoint_P1080",
                Breakpoint.P1440 => "CL_Playground_Breakpoint_P1440",
                Breakpoint.P2160 => "CL_Playground_Breakpoint_P2160",
                _ => "CL_Playground_Breakpoint_P720",
            };
            string label = $"{(string)key.Translate()} ({Mathf.RoundToInt(minPx)}px+)";
            return Box.Create(
                inner => inner.Add(Text.Create(
                    label,
                    style: new Style {
                        FontFamily = FontRole.BodyBold,
                        FontSize = new Rem(1f),
                        TextColor = ThemeSlot.TextPrimary,
                        TextAlign = TextAlign.Center,
                    }
                )),
                style: new Style {
                    Padding = EdgeInsets.All(new Rem(0.5f)),
                }
            );
        });
    }

    [DocVariant("CL_Playground_responsive_Ladder")]
    public static DocSample DocsLadder() {
        return new DocSample(() => {
            Breakpoint current = RenderContext.Current.Breakpoint;
            Rem gap = new Rem(0.25f);
            RadiusSpec radius = RadiusSpec.All(RadiusScale.Sm);
            Breakpoint[] all = {
                Breakpoint.P720,
                Breakpoint.P1080,
                Breakpoint.P1440,
                Breakpoint.P2160,
            };
            return HStack.Create(
                gap: gap,
                children: row => {
                    for (int i = 0; i < all.Length; i++) {
                        Breakpoint bp = all[i];
                        bool isActive = bp == current;
                        ThemeSlot bgSlot = isActive ? ThemeSlot.SurfaceAccent : ThemeSlot.SurfaceRaised;
                        ThemeSlot fgSlot = isActive ? ThemeSlot.TextOnAccent : ThemeSlot.TextMuted;
                        Style cellStyle = new Style {
                            Padding = EdgeInsets.Vertical(new Rem(0.375f)),
                            Background = BackgroundSpec.Of(bgSlot),
                            Radius = radius,
                        };
                        row.AddFlex(Box.Create(
                            cell => cell.Add(Text.Create(
                                bp.ToString(),
                                style: new Style {
                                    FontFamily = FontRole.BodyBold,
                                    FontSize = new Rem(0.8125f),
                                    TextColor = fgSlot,
                                    TextAlign = TextAlign.Center,
                                }
                            )),
                            style: cellStyle
                        ));
                    }
                }
            );
        });
    }


    [DocVariant("CL_Playground_responsive_ResponsivePadding")]
    public static DocSample DocsResponsivePadding() {
        return new DocSample(() => {
            Rem pad = Breakpoints.Pick(
                p720: new Rem(0.5f),
                p1080: new Rem(1.25f),
                p1440: new Rem(2f),
                p2160: new Rem(3f)
            );
            string padLabel = $"Pick = {pad.Value:0.##}rem";
            return Box.Create(
                inner => inner.Add(Text.Create(
                    padLabel,
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.875f),
                        TextColor = ThemeSlot.TextPrimary,
                        TextAlign = TextAlign.Center,
                    }
                )),
                style: new Style {
                    Padding = EdgeInsets.All(pad),
                    Background = BackgroundSpec.Of(ThemeSlot.SurfaceAccent),
                    Radius = RadiusSpec.All(RadiusScale.Sm),
                }
            );
        });
    }

    [DocVariant("CL_Playground_responsive_ResponsiveColumns")]
    public static DocSample DocsResponsiveColumns() {
        return new DocSample(() => {
            int columnCount = Breakpoints.Pick(
                p720: 1,
                p1080: 2,
                p1440: 3,
                p2160: 4
            );
            return HStack.Create(
                gap: new Rem(0.5f),
                children: row => {
                    for (int i = 0; i < columnCount; i++) {
                        int idx = i + 1;
                        row.AddFlex(Box.Create(
                            cell => cell.Add(Text.Create(
                                (string)"CL_Playground_responsive_Sample_Card".Translate(idx.Named("INDEX")),
                                style: new Style {
                                    FontFamily = FontRole.BodyBold,
                                    FontSize = new Rem(0.9375f),
                                    TextColor = ThemeSlot.TextOnAccent,
                                    TextAlign = TextAlign.Center,
                                }
                            )),
                            style: new Style {
                                Padding = EdgeInsets.All(new Rem(0.75f)),
                                Background = BackgroundSpec.Of(ThemeSlot.SurfaceAccent),
                                Radius = RadiusSpec.All(RadiusScale.Sm),
                            }
                        ));
                    }
                }
            );
        });
    }

    [DocVariant("CL_Playground_responsive_ResponsiveFont")]
    public static DocSample DocsResponsiveFont() {
        return new DocSample(() => {
            Rem headingSize = Breakpoints.Pick(
                p720: new Rem(1f),
                p1080: new Rem(1.375f),
                p1440: new Rem(1.75f),
                p2160: new Rem(2.125f)
            );
            return Stack.Create(
                gap: new Rem(0.5f),
                children: col => {
                    col.Add(Text.Create(
                        (string)"CL_Playground_responsive_Sample_Heading".Translate(),
                        style: new Style {
                            FontFamily = FontRole.Display,
                            FontSize = headingSize,
                            TextColor = ThemeSlot.TextPrimary,
                        }
                    ));
                    col.Add(Text.Create(
                        (string)"CL_Playground_responsive_Sample_Body".Translate(),
                        wrap: true,
                        style: new Style {
                            FontFamily = FontRole.Body,
                            FontSize = new Rem(0.9375f),
                            TextColor = ThemeSlot.TextSecondary,
                        }
                    ));
                }
            );
        });
    }

    

    

    

    

    
}
