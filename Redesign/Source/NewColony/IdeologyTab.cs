using System.Collections.Generic;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Typography;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Avatar = Cosmere.Lightweave.Data.Avatar;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

// The "Choose your ideoligion" tab: a fixed-width belief rail (mode selection + current summary)
// beside a scrolling preset gallery grouped by IdeoPresetCategoryDef. Content is real vanilla
// Ideology defs; the chosen mode is applied by NewColonyLauncher.ApplyIdeology at colony start.
public static class IdeologyTab {
    public static LightweaveNode Build(Hooks.Hooks.StateHandle<IdeologyParams> ideology) {
        Hooks.Hooks.StateHandle<bool> editorOpen = Hooks.Hooks.UseState(false);
        Hooks.Hooks.StateHandle<bool> wizardOpen = Hooks.Hooks.UseState(false);
        Hooks.Hooks.StateHandle<int> wizardStep = Hooks.Hooks.UseState(0);
        Hooks.Hooks.StateHandle<IdeoSubPicker> sub = Hooks.Hooks.UseState(IdeoSubPicker.None);
        Hooks.Hooks.StateHandle<int> rev = Hooks.Hooks.UseState(0);
        Hooks.Hooks.StateHandle<SectionLock> locks = Hooks.Hooks.UseState(SectionLock.None);
        Hooks.Hooks.StateHandle<IdeoDetailSection> editorTab = Hooks.Hooks.UseState(IdeoDetailSection.Memes);

        IdeologyParams value = ideology.Value;
        bool worldReady = NewColonyLauncher.WorldReady;

        // The draft ideoligion is a live Ideo in Find.IdeoManager; (re)seed it whenever the world
        // becomes ready or the chosen mode/preset changes. Idempotent + self-healing after a Game rebuild.
        Hooks.Hooks.UseEffect(() => {
            IdeoDraft.EnsureForMode(value.Mode, value.PresetDefName);
            return null;
        }, [worldReady, value.Mode, value.PresetDefName ?? string.Empty]);

        if (!worldReady) {
            return BuildGate();
        }

        LightweaveNode tab = HStack.Create(SpacingScale.None, h => {
            h.Add(BuildRail(ideology, editorOpen, wizardOpen, wizardStep), new Rem(23.75f).ToPixels());
            h.AddHug(Divider.Vertical());
            h.AddFlex(BuildGallery(ideology));
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });

        LightweaveNode overlays = IdeologyEditor.Build(editorOpen, sub, rev, locks, editorTab);
        LightweaveNode wizard = IdeoWizard.Build(wizardOpen, wizardStep, sub, rev, locks, editorTab);

        LightweaveNode root = NodeBuilder.New("IdeologyTabRoot", 0, nameof(IdeologyTab));
        root.Children.Add(tab);
        root.Children.Add(overlays);
        root.Children.Add(wizard);
        root.MeasureWidth = () => tab.MeasureWidth?.Invoke() ?? 0f;
        root.Measure = w => tab.Measure?.Invoke(w) ?? tab.PreferredHeight ?? 0f;
        root.Paint = (rect, _) => {
            tab.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(tab, rect);
            overlays.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(overlays, rect);
            wizard.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(wizard, rect);
        };
        return root;
    }

    // Shown while the world is still generating - the live draft ideo cannot exist until the world
    // (and player faction) do, so the editor and faction rail have nothing to read yet.
    private static LightweaveNode BuildGate() {
        return Stack.Create(SpacingScale.Md, s => {
            s.AddFlex(Spacer.Flex());
            s.Add(CenterRow(Spinner.Create(size: new Rem(2f))));
            s.Add(CenterRow(Text.Create("CL_NewColony_Ideology_PreparingWorld".Translate(), style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.875f),
                TextColor = ThemeSlot.TextMuted,
            })));
            s.AddFlex(Spacer.Flex());
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode CenterRow(LightweaveNode child) {
        return HStack.Create(SpacingScale.None, h => {
            h.AddFlex(Spacer.Flex());
            h.AddHug(child);
            h.AddFlex(Spacer.Flex());
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildRail(
        Hooks.Hooks.StateHandle<IdeologyParams> ideology,
        Hooks.Hooks.StateHandle<bool> editorOpen,
        Hooks.Hooks.StateHandle<bool> wizardOpen,
        Hooks.Hooks.StateHandle<int> wizardStep
    ) {
        IdeologyParams value = ideology.Value;

        LightweaveNode topInner = Stack.Create(SpacingScale.Sm, s => {
            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Ideology_BeliefHeading".Translate()));
            s.Add(Text.Create("CL_NewColony_Ideology_BeliefCaption".Translate(), wrap: true, style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.8125f),
                TextColor = ThemeSlot.TextSecondary,
            }));

            s.Add(BuildModeRow(ideology, wizardOpen, wizardStep, IdeoMode.Inactive, Icons.Phosphor.X,
                "CL_NewColony_Ideology_Mode_InactiveName".Translate(),
                "CL_NewColony_Ideology_Mode_InactiveSub".Translate()));

            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Ideology_CustomHeading".Translate(), accent: (ColorRef)ThemeSlot.TextMuted));

            s.Add(BuildModeRow(ideology, wizardOpen, wizardStep, IdeoMode.CustomFluid, Icons.Phosphor.DropHalf,
                "CL_NewColony_Ideology_Mode_FluidName".Translate(),
                "CL_NewColony_Ideology_Mode_FluidSub".Translate()));
            s.Add(BuildModeRow(ideology, wizardOpen, wizardStep, IdeoMode.CustomFixed, Icons.Phosphor.SlidersHorizontal,
                "CL_NewColony_Ideology_Mode_FixedName".Translate(),
                "CL_NewColony_Ideology_Mode_FixedSub".Translate()));
        }, style: new Style {
            Padding = EdgeInsets.All(new Rem(1.5f)),
        });

        LightweaveNode scroll = ScrollArea.Create(topInner, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
        });

        // No selection yet (Inactive): the scrollable mode list fills the whole pane.
        if (value.Mode == IdeoMode.Inactive) {
            return scroll;
        }

        // Pin the chosen ideology to the bottom of the rail: heading + summary card + (custom) editor button.
        LightweaveNode footer = Stack.Create(SpacingScale.Sm, f => {
            f.Add(NewColonyControls.SectionLabel("CL_NewColony_Ideology_SelectedHeading".Translate()));
            f.Add(BuildCurrentCard(value));

            if (value.Mode is IdeoMode.CustomFluid or IdeoMode.CustomFixed) {
                f.Add(Button.Create(
                    (string)"CL_NewColony_Ideology_Editor_Open".Translate(),
                    () => editorOpen.Set(true),
                    Variant.Primary,
                    leading: Glyph.Create(Icons.Phosphor.PencilSimple, new Style { FontSize = new Rem(1f) }),
                    style: new Style { Width = Length.Stretch }));
            }
        }, style: new Style {
            Padding = new EdgeInsets(new Rem(1.25f), new Rem(1.5f), new Rem(1.5f), new Rem(1.5f)),
        });

        return Stack.Create(SpacingScale.None, root => {
            root.AddFlex(scroll);
            root.Add(Divider.Horizontal());
            root.Add(footer);
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildModeRow(
        Hooks.Hooks.StateHandle<IdeologyParams> ideology,
        Hooks.Hooks.StateHandle<bool> wizardOpen,
        Hooks.Hooks.StateHandle<int> wizardStep,
        IdeoMode mode,
        IconRef glyph,
        string name,
        string sub
    ) {
        bool selected = ideology.Value.Mode == mode;

        LightweaveNode badge = Avatar.Create(
            string.Empty,
            size: new Rem(2.625f),
            accent: selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary,
            background: selected ? ThemeSlot.AccentSoft : ThemeSlot.Glass1,
            border: ThemeSlot.BorderFaint,
            icon: glyph,
            iconScale: 0.62f);

        LightweaveNode textCol = Stack.Create(SpacingScale.Xxs, c => {
            c.Add(Text.Create(name, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.0625f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            c.Add(Text.Create(sub, wrap: true, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.625f),
                LetterSpacing = Tracking.Of(0.04f),
                TextColor = ThemeSlot.TextMuted,
            }));
        });

        LightweaveNode child = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(badge);
            h.AddFlex(textCol);
        }, style: new Style { Width = Length.Stretch });

        return SelectableSurface.Create(
            child: child,
            selected: selected,
            variant: SelectableSurfaceVariant.ListRow,
            accent: ThemeSlot.SurfaceAccent,
            trailingCaret: false,
            onSelect: () => {
                IdeologyParams next = ideology.Value;
                next.Mode = mode;
                next.PresetDefName = null;
                ideology.Set(next);

                // Selecting either custom mode opens the 3-step wizard at the first step.
                if (mode is IdeoMode.CustomFluid or IdeoMode.CustomFixed) {
                    wizardStep.Set(0);
                    wizardOpen.Set(true);
                }
            },
            padding: new EdgeInsets(SpacingScale.Sm, new Rem(0.875f), SpacingScale.Sm, new Rem(0.875f)),
            style: new Style { Width = Length.Stretch }
        );
    }

    private static LightweaveNode BuildCurrentCard(IdeologyParams value) {
        IdeoPresetDef? preset = value.Mode == IdeoMode.Preset
            ? NewColonyData.FindIdeoPreset(value.PresetDefName)
            : null;

        Ideo? live = value.Mode is IdeoMode.CustomFluid or IdeoMode.CustomFixed
            ? IdeoDraft.Active()
            : null;

        string name = preset != null
            ? preset.LabelCap.ToString()
            : live != null && !live.name.NullOrEmpty()
                ? live.name
                : (string)"CL_NewColony_Ideology_Custom".Translate();

        MemeDef? structure = preset != null ? StructureMemeOf(preset) : StructureMemeOfIdeo(live);
        List<string> memeNames = preset != null ? NonStructureMemeLabels(preset) : NonStructureMemeLabelsOfIdeo(live);
        string subtitle = IdeologyDecisions.ComposeSelectionSubtitle(structure?.LabelCap.ToString(), memeNames)
            .ToUpperInvariant();

        Texture2D? cardIcon = preset != null ? FirstMemeIcon(preset) : live?.Icon;

        LightweaveNode badge = Avatar.Create(
            string.Empty,
            size: new Rem(2.875f),
            accent: ThemeSlot.SurfaceAccent,
            background: ThemeSlot.AccentSoft,
            border: ThemeSlot.BorderFaint,
            texture: cardIcon,
            icon: cardIcon == null ? Icons.Phosphor.FlagBannerFold : (IconRef?)null,
            iconScale: 0.8f,
            tintTexture: false);

        LightweaveNode header = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(badge);
            h.AddFlex(Stack.Create(SpacingScale.Xxs, c => {
                c.Add(Text.Create(name, style: new Style {
                    FontFamily = FontRole.Display,
                    FontSize = new Rem(1.375f),
                    LetterSpacing = Tracking.Of(0.03f),
                    TextColor = ThemeSlot.TextPrimary,
                }));
                if (subtitle.Length > 0) {
                    c.Add(Text.Create(subtitle, wrap: true, style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.625f),
                        LetterSpacing = Tracking.Of(0.1f),
                        TextColor = ThemeSlot.SurfaceAccent,
                    }));
                }
            }));
        }, style: new Style { Width = Length.Stretch });

        return header;
    }

    private static MemeDef? StructureMemeOfIdeo(Ideo? ideo) {
        if (ideo == null) {
            return null;
        }
        foreach (MemeDef meme in ideo.memes) {
            if (meme.category == MemeCategory.Structure) {
                return meme;
            }
        }
        return null;
    }

    private static List<string> NonStructureMemeLabelsOfIdeo(Ideo? ideo) {
        List<string> labels = new List<string>();
        if (ideo == null) {
            return labels;
        }
        foreach (MemeDef meme in ideo.memes) {
            if (meme.category != MemeCategory.Structure) {
                labels.Add(meme.LabelCap.ToString());
            }
        }
        return labels;
    }

    private static LightweaveNode BuildGallery(Hooks.Hooks.StateHandle<IdeologyParams> ideology) {
        LightweaveNode inner = Stack.Create(SpacingScale.Lg, g => {
            foreach (IdeoPresetCategoryDef category in NewColonyData.IdeoPresetCategories()) {
                List<IdeoPresetDef> presets = NewColonyData.IdeoPresetsInCategory(category);
                if (presets.Count == 0) {
                    continue;
                }
                g.Add(BuildTierSection(category, presets, ideology));
            }
        }, style: new Style {
            Padding = new EdgeInsets(new Rem(1.75f), new Rem(2f), new Rem(1.75f), new Rem(2f)),
        });

        return ScrollArea.Create(inner, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
        });
    }

    private static LightweaveNode BuildTierSection(
        IdeoPresetCategoryDef category,
        List<IdeoPresetDef> presets,
        Hooks.Hooks.StateHandle<IdeologyParams> ideology
    ) {
        return Stack.Create(SpacingScale.Md, s => {
            s.Add(HStack.Create(SpacingScale.Md, hd => {
                hd.AddHug(Text.Create(category.LabelCap.ToString(), style: new Style {
                    FontFamily = FontRole.Display,
                    FontSize = new Rem(1.375f),
                    LetterSpacing = Tracking.Of(0.03f),
                    TextColor = ThemeSlot.TextPrimary,
                }));
                if (!category.description.NullOrEmpty()) {
                    hd.AddFlex(Text.Create(category.description, truncate: true, style: new Style {
                        FontFamily = FontRole.Body,
                        FontSize = new Rem(0.78f),
                        TextColor = ThemeSlot.TextMuted,
                    }));
                }
            }, align: FlexAlign.End, style: new Style { Width = Length.Stretch }));
            s.Add(Divider.Horizontal(style: new Style { Background = BackgroundSpec.Of(ThemeSlot.DividerFaint) }));
            s.Add(BuildPresetGrid(presets, ideology));
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildPresetGrid(
        List<IdeoPresetDef> presets,
        Hooks.Hooks.StateHandle<IdeologyParams> ideology
    ) {
        return Wrap.Create(
            SpacingScale.Sm,
            new Rem(20f),
            grid => {
                for (int i = 0; i < presets.Count; i++) {
                    grid.Add(BuildPresetTile(presets[i], ideology));
                }
            },
            stretch: true,
            style: new Style { Width = Length.Stretch }
        );
    }

    private static LightweaveNode BuildPresetTile(
        IdeoPresetDef preset,
        Hooks.Hooks.StateHandle<IdeologyParams> ideology
    ) {
        IdeologyParams value = ideology.Value;
        bool selected = value.Mode == IdeoMode.Preset && value.PresetDefName == preset.defName;

        LightweaveNode iconCluster = BuildMemeIcons(preset, selected);

        LightweaveNode col = Stack.Create(SpacingScale.Sm, s => {
            s.Add(Text.Create(preset.LabelCap.ToString(), wrap: true, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.125f),
                LetterSpacing = Tracking.Of(0.05f),
                TextColor = selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextPrimary,
            }));
            s.Add(iconCluster);
        }, style: new Style { Width = Length.Stretch });

        return SelectableSurface.Create(
            child: col,
            selected: selected,
            variant: SelectableSurfaceVariant.ListRow,
            accent: ThemeSlot.SurfaceAccent,
            trailingCaret: false,
            tooltipContent: () => BuildPresetTooltip(preset),
            id: preset.defName,
            onSelect: () => {
                IdeologyParams next = ideology.Value;
                next.Mode = IdeoMode.Preset;
                next.PresetDefName = preset.defName;
                ideology.Set(next);
            },
            padding: new EdgeInsets(SpacingScale.Xs, SpacingScale.Sm, SpacingScale.Xs, SpacingScale.Sm),
            style: new Style { Width = Length.Stretch }
        );
    }

    // All meme icons for the preset, shown as a cluster of small badges (vanilla shows every meme's
    // icon, not just one). Classic presets carry no memes, so fall back to the preset icon / glyph.
    private static LightweaveNode BuildMemeIcons(IdeoPresetDef preset, bool selected) {
        ThemeSlot accent = selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary;
        ThemeSlot background = selected ? ThemeSlot.AccentSoft : ThemeSlot.Glass1;

        if (preset.memes.Count == 0) {
            return MemeChip(preset.Icon, accent, background);
        }

        return HStack.Create(SpacingScale.Xxs, h => {
            for (int i = 0; i < preset.memes.Count; i++) {
                h.AddHug(MemeChip(preset.memes[i].Icon, accent, background));
            }
        }, align: FlexAlign.Center);
    }

    private static LightweaveNode MemeChip(Texture2D? icon, ThemeSlot accent, ThemeSlot background) {
        return Avatar.Create(
            string.Empty,
            size: new Rem(4f),
            accent: accent,
            background: background,
            border: ThemeSlot.BorderFaint,
            texture: icon,
            icon: icon == null ? Icons.Phosphor.FlagBannerFold : (IconRef?)null,
            iconScale: 0.62f,
            tintTexture: false);
    }

    // Vanilla-style rich tooltip: preset name + description, then each meme as a gold title with its
    // own description. Colored titles via the SurfaceAccent slot (callers never call .Translate()).
    private static LightweaveNode BuildPresetTooltip(IdeoPresetDef preset) {
        return Stack.Create(SpacingScale.Sm, t => {
            t.Add(Text.Create(preset.LabelCap.ToString(), wrap: true, style: new Style {
                FontFamily = FontRole.BodyBold,
                FontSize = new Rem(0.9375f),
                TextColor = ThemeSlot.SurfaceAccent,
            }));
            if (!preset.description.NullOrEmpty()) {
                t.Add(Text.Create(preset.description, wrap: true, richText: true, style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.8125f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }
            for (int i = 0; i < preset.memes.Count; i++) {
                MemeDef meme = preset.memes[i];
                t.Add(Text.Create(meme.LabelCap.ToString(), wrap: true, style: new Style {
                    FontFamily = FontRole.BodyBold,
                    FontSize = new Rem(0.875f),
                    TextColor = ThemeSlot.SurfaceAccent,
                }));
                if (!meme.description.NullOrEmpty()) {
                    t.Add(Text.Create(meme.description, wrap: true, richText: true, style: new Style {
                        FontFamily = FontRole.Body,
                        FontSize = new Rem(0.78f),
                        TextColor = ThemeSlot.TextSecondary,
                    }));
                }
            }
        });
    }

    // The ideoligion's representative icon: first non-structure meme, else any meme, else the preset.
    private static Texture2D? FirstMemeIcon(IdeoPresetDef preset) {
        for (int i = 0; i < preset.memes.Count; i++) {
            if (preset.memes[i].category != MemeCategory.Structure && preset.memes[i].Icon != null) {
                return preset.memes[i].Icon;
            }
        }
        for (int i = 0; i < preset.memes.Count; i++) {
            if (preset.memes[i].Icon != null) {
                return preset.memes[i].Icon;
            }
        }
        return preset.Icon;
    }

    private static MemeDef? StructureMemeOf(IdeoPresetDef preset) {
        for (int i = 0; i < preset.memes.Count; i++) {
            if (preset.memes[i].category == MemeCategory.Structure) {
                return preset.memes[i];
            }
        }
        return null;
    }

    private static List<string> NonStructureMemeLabels(IdeoPresetDef preset) {
        List<string> labels = new List<string>();
        for (int i = 0; i < preset.memes.Count; i++) {
            MemeDef meme = preset.memes[i];
            if (meme.category != MemeCategory.Structure) {
                labels.Add(meme.LabelCap.ToString());
            }
        }
        return labels;
    }
}
