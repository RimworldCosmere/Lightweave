using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Cosmere.Lightweave.Surfaces;
using LText = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Tokens;

[Doc(
    Id = "fonts",
    Summary = "Font roles (Body, Heading, Mono, etc.) routed through the active theme.",
    WhenToUse = "Resolve a Font via Theme.GetFont(FontRole) in a Paint callback; never assign GameFont or Verse-side fonts directly.",
    SourcePath = "Lightweave/Tokens/FontRole.cs",
    Category = "Foundation",
    PreferredVariantHeight = 120f,
    HideUsage = true,
    HideSource = true
)]
public static class FontsDoc {

    

    [DocVariant("CL_Playground_fonts_Roles")]
    public static DocSample DocsRoles() {
        return new DocSample(() => {
            Rem rowGap = new Rem(0.5f);
            float labelColumnWidthPx = new Rem(11f).ToPixels();
            float rowHeightPx = new Rem(2.75f).ToPixels();
            Theme.Theme theme = RenderContext.Current.Theme;
            FontRole[] roles = {
                FontRole.Body,
                FontRole.BodyBold,
                FontRole.Heading,
                FontRole.Display,
                FontRole.Label,
                FontRole.Caption,
                FontRole.Mono,
            };
            return Stack.Create(
                gap: rowGap,
                children: col => {
                    for (int i = 0; i < roles.Length; i++) {
                        FontRole role = roles[i];
                        string fontName = theme.GetFont(role)?.name ?? "(unresolved)";
                        col.Add(HStack.Create(
                            gap: rowGap,
                            children: row => {
                                row.Add(Stack.Create(
                                    gap: new Rem(0.125f),
                                    children: labelCol => {
                                        labelCol.Add(LText.Create(
                                            role.ToString(),
                                            style: new Style {
                                                FontFamily = FontRole.Mono,
                                                FontSize = new Rem(0.8125f),
                                                TextColor = ThemeSlot.TextPrimary,
                                            }
                                        ));
                                        labelCol.Add(LText.Create(
                                            fontName,
                                            style: new Style {
                                                FontFamily = FontRole.Mono,
                                                FontSize = new Rem(0.6875f),
                                                TextColor = ThemeSlot.TextMuted,
                                            }
                                        ));
                                    }
                                ), labelColumnWidthPx);
                                row.AddFlex(LText.Create(
                                    (string)"CL_Playground_fonts_Pangram".Translate(),
                                    style: new Style {
                                        FontFamily = role,
                                        FontSize = new Rem(1.125f),
                                        TextColor = ThemeSlot.TextPrimary,
                                    }
                                ));
                            }
                        ), rowHeightPx);
                    }
                }
            );
        });
    }

    [DocVariant("CL_Playground_fonts_SizeLadder")]
    public static DocSample DocsSizeLadder() {
        return new DocSample(() => {
            Rem rowGap = new Rem(0.5f);
            float labelColumnWidthPx = new Rem(12f).ToPixels();
            Theme.Theme theme = RenderContext.Current.Theme;
            string bodyFontName = theme.GetFont(FontRole.Body)?.name ?? "(unresolved)";
            (string LabelKey, Rem Step)[] steps = {
                ("CL_Playground_fonts_Size_Caption", new Rem(0.6875f)),
                ("CL_Playground_fonts_Size_Small", new Rem(0.8125f)),
                ("CL_Playground_fonts_Size_Body", new Rem(0.9375f)),
                ("CL_Playground_fonts_Size_Lead", new Rem(1.125f)),
                ("CL_Playground_fonts_Size_Heading", new Rem(1.375f)),
                ("CL_Playground_fonts_Size_Display", new Rem(1.75f)),
            };
            return Stack.Create(
                gap: rowGap,
                children: col => {
                    col.Add(LText.Create(
                        $"FontRole.Body  ({bodyFontName})",
                        style: new Style {
                            FontFamily = FontRole.Mono,
                            FontSize = new Rem(0.6875f),
                            TextColor = ThemeSlot.TextMuted,
                        }
                    ));
                    for (int i = 0; i < steps.Length; i++) {
                        (string labelKey, Rem step) = steps[i];
                        int samplePx = Mathf.RoundToInt(step.ToFontPx());
                        float rowHeightPx = Mathf.Max(new Rem(1.75f).ToPixels(), step.ToFontPx() + new Rem(0.5f).ToPixels());
                        string labelText = $"{(string)labelKey.Translate()}  ({step.Value:0.###}rem / {samplePx}px)";
                        col.Add(HStack.Create(
                            gap: rowGap,
                            children: row => {
                                row.Add(LText.Create(
                                    labelText,
                                    style: new Style {
                                        FontFamily = FontRole.Mono,
                                        FontSize = new Rem(0.6875f),
                                        TextColor = ThemeSlot.TextMuted,
                                    }
                                ), labelColumnWidthPx);
                                row.AddFlex(LText.Create(
                                    (string)"CL_Playground_fonts_Pangram".Translate(),
                                    style: new Style {
                                        FontFamily = FontRole.Body,
                                        FontSize = step,
                                        TextColor = ThemeSlot.TextPrimary,
                                    }
                                ));
                            }
                        ), rowHeightPx);
                    }
                }
            );
        });
    }


    [DocVariant("CL_Playground_fonts_HeadingHierarchy")]
    public static DocSample DocsHeadingHierarchy() {
        return new DocSample(() =>
            Stack.Create(
                gap: new Rem(0.5f),
                children: col => {
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Heading_Display".Translate(),
                        style: new Style {
                            FontFamily = FontRole.Display,
                            FontSize = new Rem(1.75f),
                            TextColor = ThemeSlot.TextPrimary,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Heading_Section".Translate(),
                        style: new Style {
                            FontFamily = FontRole.Heading,
                            FontSize = new Rem(1.375f),
                            TextColor = ThemeSlot.TextPrimary,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Heading_Subhead".Translate(),
                        style: new Style {
                            FontFamily = FontRole.BodyBold,
                            FontSize = new Rem(1f),
                            TextColor = ThemeSlot.TextSecondary,
                        }
                    ));
                }
            )
        );
    }

    [DocVariant("CL_Playground_fonts_BodyParagraph")]
    public static DocSample DocsBodyParagraph() {
        return new DocSample(() =>
            Stack.Create(
                gap: new Rem(0.5f),
                children: col => {
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Body_Paragraph".Translate(),
                        wrap: true,
                        style: new Style {
                            FontFamily = FontRole.Body,
                            FontSize = new Rem(0.9375f),
                            TextColor = ThemeSlot.TextSecondary,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Body_Caption".Translate(),
                        wrap: true,
                        style: new Style {
                            FontFamily = FontRole.Caption,
                            FontSize = new Rem(0.8125f),
                            TextColor = ThemeSlot.TextMuted,
                        }
                    ));
                }
            )
        );
    }

    [DocVariant("CL_Playground_fonts_MetadataAndCaption")]
    public static DocSample DocsMetadataAndCaption() {
        return new DocSample(() =>
            Stack.Create(
                gap: new Rem(0.25f),
                children: col => {
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Metadata_Label".Translate(),
                        style: new Style {
                            FontFamily = FontRole.Label,
                            FontSize = new Rem(0.75f),
                            TextColor = ThemeSlot.MetadataLabel,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Metadata_Value".Translate(),
                        style: new Style {
                            FontFamily = FontRole.BodyBold,
                            FontSize = new Rem(1.125f),
                            TextColor = ThemeSlot.TextPrimary,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_fonts_Metadata_Caption".Translate(),
                        wrap: true,
                        style: new Style {
                            FontFamily = FontRole.Caption,
                            FontSize = new Rem(0.8125f),
                            TextColor = ThemeSlot.TextMuted,
                        }
                    ));
                }
            )
        );
    }

    [DocVariant("CL_Playground_fonts_MonoCodeBlock")]
    public static DocSample DocsMonoCodeBlock() {
        Style codeBlockStyle = new Style {
            Padding = EdgeInsets.All(new Rem(0.75f)),
            Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Sm),
        };

        return new DocSample(() =>
            Box.Create(
                code => code.Add(Stack.Create(
                    gap: new Rem(0.25f),
                    children: lines => {
                        lines.Add(LText.Create(
                            (string)"CL_Playground_fonts_Code_Sample".Translate(),
                            style: new Style {
                                FontFamily = FontRole.Mono,
                                FontSize = new Rem(0.9375f),
                                TextColor = ThemeSlot.TextPrimary,
                            }
                        ));
                        lines.Add(LText.Create(
                            (string)"CL_Playground_fonts_Code_Comment".Translate(),
                            style: new Style {
                                FontFamily = FontRole.Mono,
                                FontSize = new Rem(0.8125f),
                                TextColor = ThemeSlot.TextMuted,
                            }
                        ));
                    }
                )),
                style: codeBlockStyle
            )
        );
    }

    

    

    
}
