using System.Collections.Generic;
using Cosmere.Lightweave.Blocks;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using Verse;
using Display = Cosmere.Lightweave.Typography.Display;
using Image = Cosmere.Lightweave.Data.Image;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class StorytellerTab {
    public static LightweaveNode Build(
        Hooks.Hooks.StateHandle<string?> tellerDefName,
        Hooks.Hooks.StateHandle<string?> diffDefName
    ) {
        List<StorytellerDef> tellers = NewColonyData.Storytellers();
        List<DifficultyDef> diffs = NewColonyData.Difficulties();

        int initialPage = 0;
        for (int i = 0; i < tellers.Count; i++) {
            if (tellers[i].defName == tellerDefName.Value) {
                initialPage = i;
                break;
            }
        }
        Hooks.Hooks.StateHandle<int> page = Hooks.Hooks.UseState(initialPage);

        return Stack.Create(SpacingScale.Lg, s => {
            s.AddFlex(BuildCarousel(tellers, tellerDefName, page));
            s.Add(BuildDifficultySection(diffs, diffDefName));
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildCarousel(
        List<StorytellerDef> tellers,
        Hooks.Hooks.StateHandle<string?> tellerDefName,
        Hooks.Hooks.StateHandle<int> page
    ) {
        List<LightweaveNode> slides = [];
        foreach (StorytellerDef def in tellers) {
            slides.Add(BuildTellerSlide(def, tellerDefName));
        }

        return Carousel.Create(
            slides,
            page.Value,
            page.Set,
            visible: 3,
            loop: true,
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        );
    }

    private static LightweaveNode BuildTellerSlide(StorytellerDef def, Hooks.Hooks.StateHandle<string?> tellerDefName) {
        string name = def.defName;
        bool isSelected = name == tellerDefName.Value;

        LightweaveNode metaInner = Stack.Create(SpacingScale.Xxs, m => {
            m.Add(Display.Create(def.label.CapitalizeFirst(), level: 4));
            if (!def.description.NullOrEmpty()) {
                m.Add(Text.Create(def.description, wrap: true, style: new Style {
                    FontSize = new Rem(0.82f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }
        }, style: new Style { Width = Length.Stretch });

        LightweaveNode meta = Stack.Create(SpacingScale.None, p => {
            p.AddFlex(ScrollArea.Create(metaInner, id: "teller-meta-" + name, style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
            }));
        }, style: new Style {
            Width = Length.Stretch,
            Background = BackgroundSpec.Of(ThemeSlot.GlassFrost),
            Border = new BorderSpec(Top: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            Padding = EdgeInsets.All(new Rem(0.875f)),
        });

        LightweaveNode child = Stack.Create(SpacingScale.None, c => {
            c.AddFlex(Image.Create(def.portraitLargeTex, fit: ImageFit.Contain, style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
            }));
            c.Add(meta, new Rem(8.5f).ToPixels());
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });

        return SelectableSurface.Create(
            child: child,
            selected: isSelected,
            onSelect: () => tellerDefName.Set(name),
            padding: EdgeInsets.All(SpacingScale.None),
            style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
            }
        );
    }

    private static LightweaveNode BuildDifficultySection(
        List<DifficultyDef> diffs,
        Hooks.Hooks.StateHandle<string?> diffDefName
    ) {
        return Stack.Create(SpacingScale.Md, s => {
            s.Add(BuildDifficultyHeader(diffs, diffDefName));
            s.Add(BuildDifficultyGrid(diffs, diffDefName));
        }, style: new Style {
            Width = Length.Stretch,
            Border = new BorderSpec(Top: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            Padding = new EdgeInsets(Top: new Rem(1f), Bottom: new Rem(0.25f)),
        });
    }

    private static LightweaveNode BuildDifficultyHeader(
        List<DifficultyDef> diffs,
        Hooks.Hooks.StateHandle<string?> diffDefName
    ) {
        DifficultyDef? selected = NewColonyData.FindDifficulty(diffDefName.Value);

        return HStack.Create(SpacingScale.Md, h => {
            h.AddFlex(NewColonyControls.SectionLabel("CL_NewColony_Difficulty_Heading".Translate(), trailingRule: false));
            if (selected != null) {
                int pct = NewColonyFormat.DifficultyPct(selected.threatScale);
                h.AddHug(HStack.Create(SpacingScale.Sm, g => {
                    g.AddHug(Display.Create(selected.label.CapitalizeFirst(), level: 4));
                    g.AddHug(Text.Create(pct + "%", style: new Style {
                        FontFamily = FontRole.Mono,
                        TextColor = NewColonyFormat.DifficultyColor(pct),
                    }));
                }));
            }
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildDifficultyGrid(
        List<DifficultyDef> diffs,
        Hooks.Hooks.StateHandle<string?> diffDefName
    ) {
        return Grid.Create(
            [new GridTrack.Repeat(6, new GridTrack.Fr(1f))],
            gap: SpacingScale.Sm,
            children: w => {
                foreach (DifficultyDef def in diffs) {
                    w.Add(BuildDifficultyCard(def, diffDefName));
                }
            },
            style: new Style { Width = Length.Stretch }
        );
    }

    private static LightweaveNode BuildDifficultyCard(DifficultyDef def, Hooks.Hooks.StateHandle<string?> diffDefName) {
        string name = def.defName;
        bool isSelected = name == diffDefName.Value;
        int pct = NewColonyFormat.DifficultyPct(def.threatScale);

        LightweaveNode child = Stack.Create(SpacingScale.Xxs, c => {
            c.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(Display.Create(def.label.CapitalizeFirst(), level: 4, style: new Style {
                    FontSize = new Rem(1f),
                }));
                h.AddHug(Text.Create(pct + "%", style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.7f),
                    TextColor = NewColonyFormat.DifficultyColor(pct),
                }));
            }, style: new Style { Width = Length.Stretch }));
            string lead = NewColonyFormat.LeadParagraph(def.description);
            if (!lead.NullOrEmpty()) {
                c.Add(Text.Create(lead, wrap: true, richText: true, style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.66f),
                    TextColor = ThemeSlot.TextMuted,
                }));
            }
        });

        return SelectableSurface.Create(
            child: child,
            selected: isSelected,
            tooltipContent: def.description.NullOrEmpty() ? null : () => BuildDescriptionTooltip(def.description),
            onSelect: () => diffDefName.Set(name),
            style: new Style { Width = Length.Stretch }
        );
    }


    private static LightweaveNode BuildDescriptionTooltip(string? description) {
        System.Collections.Generic.List<NewColonyThresholds.TipSegment> segments =
            NewColonyThresholds.ParseTipSegments(description);

        return Stack.Create(SpacingScale.Sm, t => {
            foreach (NewColonyThresholds.TipSegment seg in segments) {
                if (seg.IsTitle) {
                    t.Add(Text.Create(seg.Text, wrap: true, richText: true, style: new Style {
                        FontFamily = FontRole.BodyBold,
                        FontSize = new Rem(0.9375f),
                        TextColor = ThemeSlot.SurfaceAccent,
                    }));
                }
                else {
                    t.Add(Text.Create(seg.Text, wrap: true, richText: true, style: new Style {
                        FontFamily = FontRole.Body,
                        FontSize = new Rem(0.8125f),
                        TextColor = ThemeSlot.TextSecondary,
                    }));
                }
            }
        });
    }
}
