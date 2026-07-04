using System.Collections.Generic;
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
using static Cosmere.Lightweave.Hooks.Hooks;
using Avatar = Cosmere.Lightweave.Data.Avatar;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

// The progressive "Create custom ideoligion" wizard. Selecting a custom mode in the Ideology tab opens
// this instead of dropping straight into the editor: step 0 picks the structure, step 1 picks a single
// starting belief, step 2 is the full editor detail (reused from IdeologyEditor). The draft Ideo is the
// player's live primary (seeded minimal by IdeoDraft.EnsureForMode); each step mutates it in place via
// IdeoDraftMutations and bumps rev so the tree re-renders. The step-2 sub-pickers are the same Modal
// overlays the editor uses, layered as siblings here.
public static class IdeoWizard {
    private const int StepCount = 3;

    // Top-spacer height (in rem) inside the 1.875rem connector column. Places the 1px rule on the
    // circle's visual midline; measured in-game (the two-line step label drops the circle below the
    // row's geometric center, so a plain centered rule reads ~7.5px high).
    private const float StepConnectorOffset = 1.40625f;

    public static LightweaveNode Build(
        StateHandle<bool> wizardOpen,
        StateHandle<int> step,
        StateHandle<IdeoSubPicker> sub,
        StateHandle<int> rev,
        StateHandle<SectionLock> locks,
        StateHandle<IdeoDetailSection> tab
    ) {
        // The deity being edited by the Deity modal in the wizard's Customize step. Created above the
        // modals so the step-3 deity pencil (in BuildDetail) and the modal body share it.
        StateHandle<IdeoFoundation_Deity.Deity?> deityTarget = UseState<IdeoFoundation_Deity.Deity?>(null);

        // Picker steps (Structure, Starting meme) sit at 80rem (~1280px); the wider Customize editor
        // step at 82.5rem (~1320px, the mock's editor width).
        Rem wizardWidth = step.Value == StepCount - 1 ? new Rem(82.5f) : new Rem(80f);
        LightweaveNode wizard = Modal.Create(
            wizardOpen.Value,
            () => BuildWizard(wizardOpen, step, sub, rev, locks, tab, deityTarget),
            () => Close(wizardOpen, step, sub),
            width: wizardWidth,
            height: new Rem(72f));

        LightweaveNode structurePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Structure,
            () => IdeologyEditor.BuildStructurePicker(sub, rev),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(42f),
            height: new Rem(30f));

        LightweaveNode memePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Meme,
            () => IdeologyEditor.BuildMemePicker(sub, rev),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(46f),
            height: new Rem(32f));

        LightweaveNode stylePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Style,
            () => IdeologyEditor.BuildStylePicker(sub, rev),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(42f),
            height: new Rem(30f));

        LightweaveNode preceptPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Precept,
            () => IdeologyEditor.BuildPreceptPicker(sub, rev),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode iconPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Icon,
            () => IdeologyEditor.BuildIconPicker(sub, rev),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(46f),
            height: new Rem(38f));

        LightweaveNode deityEditor = Modal.Create(
            sub.Value == IdeoSubPicker.Deity,
            () => IdeologyEditor.BuildDeityEditor(sub, rev, deityTarget),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(36f),
            height: new Rem(30f));

        LightweaveNode node = NodeBuilder.New("IdeoWizardOverlays", 0, nameof(IdeoWizard));
        node.Children.Add(wizard);
        node.Children.Add(structurePicker);
        node.Children.Add(memePicker);
        node.Children.Add(stylePicker);
        node.Children.Add(preceptPicker);
        node.Children.Add(iconPicker);
        node.Children.Add(deityEditor);
        node.MeasureWidth = () => 0f;
        node.Measure = _ => 0f;
        node.Paint = (rect, _) => {
            PaintOverlay(wizard, rect);
            PaintOverlay(structurePicker, rect);
            PaintOverlay(memePicker, rect);
            PaintOverlay(stylePicker, rect);
            PaintOverlay(preceptPicker, rect);
            PaintOverlay(iconPicker, rect);
            PaintOverlay(deityEditor, rect);
        };
        return node;
    }

    private static void PaintOverlay(LightweaveNode modal, Rect rect) {
        modal.MeasuredRect = rect;
        LightweaveRoot.PaintSubtree(modal, rect);
    }

    private static void Close(StateHandle<bool> wizardOpen, StateHandle<int> step, StateHandle<IdeoSubPicker> sub) {
        sub.Set(IdeoSubPicker.None);
        step.Set(0);
        wizardOpen.Set(false);
    }

    private static void Bump(StateHandle<int> rev) {
        rev.Set(rev.Value + 1);
    }

    private static LightweaveNode BuildWizard(
        StateHandle<bool> wizardOpen,
        StateHandle<int> step,
        StateHandle<IdeoSubPicker> sub,
        StateHandle<int> rev,
        StateHandle<SectionLock> locks,
        StateHandle<IdeoDetailSection> tab,
        StateHandle<IdeoFoundation_Deity.Deity?> deityTarget
    ) {
        int current = Mathf.Clamp(step.Value, 0, StepCount - 1);

        // Faint dividers (mock uses a near-invisible hairline); the Divider default BorderSubtle reads
        // as a bright line on the dark panel.
        Style hairline = new Style { Background = BackgroundSpec.Of(ThemeSlot.BorderFaint) };
        return Stack.Create(SpacingScale.None, root => {
            root.Add(BuildHeader(wizardOpen, step, sub, current));
            root.Add(Divider.Horizontal(style: hairline));
            root.AddFlex(BuildBody(current, sub, rev, locks, tab, deityTarget));
            root.Add(Divider.Horizontal(style: hairline));
            root.Add(BuildFooter(wizardOpen, step, sub, rev, locks, current));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
        });
    }

    private static LightweaveNode BuildHeader(StateHandle<bool> wizardOpen, StateHandle<int> step, StateHandle<IdeoSubPicker> sub, int current) {
        LightweaveNode titles = Stack.Create(SpacingScale.Xxs, t => {
            t.Add(Text.Create("CL_NewColony_Ideology_Wizard_Eyebrow".Translate(), style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.6875f),
                LetterSpacing = Tracking.Of(0.28f),
                TextColor = ThemeSlot.SurfaceAccent,
            }));
            t.Add(Text.Create(StepTitle(current), style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.625f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            t.Add(Text.Create(StepSubtitle(current), wrap: true, style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.875f),
                TextColor = ThemeSlot.TextSecondary,
            }));
        });

        LightweaveNode close = IconButton.Create(
            Glyph.Create(Icons.Phosphor.X, new Style { FontSize = new Rem(1.1f) }),
            () => Close(wizardOpen, step, sub),
            Variant.Secondary,
            tooltipKey: "CL_NewColony_Ideology_Wizard_Cancel");

        LightweaveNode topRow = HStack.Create(SpacingScale.Sm, h => {
            h.AddFlex(titles);
            h.AddHug(close);
        }, align: FlexAlign.Start, style: new Style { Width = Length.Stretch });

        LightweaveNode titleBlock = Stack.Create(SpacingScale.None, s => {
            s.Add(topRow);
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.5f), new Rem(1.5f), new Rem(1.25f), new Rem(1.5f)),
        });

        // Step indicator sits in a full-width sunken bar (mock .nc-overlay-subhead: shelf-tint fill,
        // full-bleed) rather than floating inside the title padding.
        LightweaveNode stepBar = Stack.Create(SpacingScale.None, s => {
            s.Add(BuildStepIndicator(current));
        }, style: new Style {
            Width = Length.Stretch,
            Background = BackgroundSpec.Of(ThemeSlot.ShelfTint),
            // Faint hairline above the step shelf (mock .nc-overlay-subhead has a top rule); the
            // divider in BuildWizard already closes the shelf below it.
            Border = new BorderSpec(Top: new Rem(1f / 16f), Color: ThemeSlot.BorderFaint),
            Padding = new EdgeInsets(new Rem(0.875f), new Rem(1.875f), new Rem(0.875f), new Rem(1.875f)),
        });

        return Stack.Create(SpacingScale.None, s => {
            s.Add(titleBlock);
            s.Add(stepBar);
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildStepIndicator(int current) {
        return HStack.Create(SpacingScale.None, h => {
            for (int i = 0; i < StepCount; i++) {
                h.AddHug(StepGroup(i, current));
                if (i < StepCount - 1) {
                    // Flex connectors on both gaps justify the three step groups across the full
                    // width: structure hugs the left, customize hugs the right, and the middle group
                    // lands centered because the two flex rules between them grow equally. The strip
                    // gap is None so the flex connectors own all the horizontal spacing.
                    h.AddFlex(StepConnector(i < current));
                }
            }
        }, align: FlexAlign.Center, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode StepGroup(int index, int current) {
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(StepNumber(index, current));
            h.AddHug(StepText(index, current));
        }, align: FlexAlign.Center);
    }

    // Circular number badge: gold-filled with dark ink when active, accent-soft check when done,
    // faint-outlined number otherwise (mock .nc-wiz-step-no, border-radius 50%).
    private static LightweaveNode StepNumber(int index, int current) {
        bool done = index < current;
        bool active = index == current;
        if (done) {
            return Avatar.Create(string.Empty, size: new Rem(1.875f),
                accent: ThemeSlot.SurfaceAccent, background: ThemeSlot.AccentSoft,
                border: ThemeSlot.AccentGlow, icon: Icons.Phosphor.Check, iconScale: 0.55f,
                radius: RadiusScale.Full);
        }
        if (active) {
            return Avatar.Create((index + 1).ToString(), size: new Rem(1.875f),
                accent: ThemeSlot.TextOnAccent, background: ThemeSlot.SurfaceAccent,
                border: ThemeSlot.SurfaceAccent, radius: RadiusScale.Full,
                initialsFont: FontRole.Mono);
        }
        return Avatar.Create((index + 1).ToString(), size: new Rem(1.875f),
            accent: ThemeSlot.TextMuted, background: ThemeSlot.Glass1,
            border: ThemeSlot.BorderFaint, radius: RadiusScale.Full,
            initialsFont: FontRole.Mono);
    }

    // Two-line label: serif display step name over a mono uppercase hint (mock .nc-wiz-step-name +
    // .nc-wiz-step-hint).
    private static LightweaveNode StepText(int index, int current) {
        bool active = index == current;
        bool done = index < current;
        ThemeSlot nameColor = active ? ThemeSlot.TextPrimary : done ? ThemeSlot.TextSecondary : ThemeSlot.TextMuted;
        return Stack.Create(SpacingScale.None, t => {
            t.Add(Text.Create(StepLabel(index), style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.0625f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = nameColor,
            }));
            t.Add(Text.Create(StepHint(index).ToUpperInvariant(), style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.5625f),
                LetterSpacing = Tracking.Of(0.14f),
                TextColor = ThemeSlot.TextMuted,
            }));
        });
    }

    private static LightweaveNode StepConnector(bool done) {
        // The rule lives in a fixed circle-height (1.875rem) column that the row centers vertically; a
        // fixed top spacer then places the 1px rule at a deterministic offset (a single bottom flex eats
        // the slack). A symmetric two-flex centering ignored a between-flex shim, so the offset is
        // controlled explicitly here. StepConnectorOffset was measured so the rule lands on the circle's
        // true midline (the two-line label makes the step group taller, so the circle sits below the
        // row's geometric center).
        // The Stack only honors a fixed item height when it is passed as the Add(node, heightPx) arg;
        // a style-only Height is treated as Hug and collapses to 0. So the spacer and rule heights are
        // passed explicitly. Offset spacer + 1px rule + flex tail places the rule on the circle midline.
        // The rule sits inside an HStack with 2rem spacer boxes on each side so it does not butt against
        // the adjacent step chips (Stack does not apply child Margin, hence the explicit pad boxes).
        LightweaveNode ruleRow = HStack.Create(SpacingScale.None, h => {
            h.Add(Box.Create(), new Rem(2f).ToPixels());
            h.AddFlex(Box.Create(style: new Style {
                Width = Length.Stretch,
                Background = BackgroundSpec.Of(done ? ThemeSlot.SurfaceAccent : ThemeSlot.BorderFaint),
            }));
            h.Add(Box.Create(), new Rem(2f).ToPixels());
        }, style: new Style { Width = Length.Stretch });
        return Stack.Create(SpacingScale.None, s => {
            s.Add(Box.Create(style: new Style { Width = Length.Stretch }), new Rem(StepConnectorOffset).ToPixels());
            s.Add(ruleRow, new Rem(1f / 16f).ToPixels());
            s.AddFlex(Box.Create());
        }, style: new Style { Height = Length.Rem(1.875f) });
    }

    private static LightweaveNode BuildBody(int current, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, StateHandle<IdeoDetailSection> tab, StateHandle<IdeoFoundation_Deity.Deity?> deityTarget) {
        if (current == 0) {
            return BuildStructureStep(rev);
        }
        if (current == 1) {
            return BuildBeliefStep(rev);
        }
        // BuildDetail owns its own outer body ScrollArea (#17): the whole identity/rows/narrative/
        // rail+pane column scrolls as one unit, so the step host must not wrap it again.
        return IdeologyEditor.BuildDetail(sub, rev, locks, tab, deityTarget);
    }

    private static LightweaveNode BuildStructureStep(StateHandle<int> rev) {
        Ideo? ideo = IdeoDraft.Active();
        MemeDef? current = IdeologyEditor.StructureMeme(ideo);

        return ScrollArea.Create(
            Wrap.Create(SpacingScale.Md, new Rem(18f), grid => {
                foreach (MemeDef structure in IdeologyEditor.Structures()) {
                    bool selected = current == structure;
                    MemeDef captured = structure;
                    grid.Add(SelectableSurface.Create(
                        child: StructureCard(structure, selected),
                        selected: selected,
                        variant: SelectableSurfaceVariant.Tile,
                        padding: EdgeInsets.All(new Rem(1.25f)),
                        tooltipContent: () => Text.Create(structure.description ?? string.Empty, wrap: true),
                        onSelect: () => {
                            if (ideo != null) {
                                IdeoDraftMutations.SwapStructure(ideo, captured);
                                Bump(rev);
                            }
                        },
                        id: "wiz_struct_" + structure.defName));
                }
            }, stretch: true, style: new Style {
                Width = Length.Stretch,
                // Mock .nc-overlay-body: 26px vertical / 30px horizontal breathing room around the grid.
                Padding = new EdgeInsets(new Rem(1.625f), new Rem(1.875f), new Rem(1.625f), new Rem(1.875f)),
            }),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    // Vertical structure tile: centered icon badge over a serif name and a muted, centered description
    // (mock .nc-struct-tile). The outer SelectableSurface supplies the card frame + selected accent.
    private static LightweaveNode StructureCard(MemeDef structure, bool selected) {
        return Stack.Create(SpacingScale.Sm, c => {
            c.Add(HStack.Create(SpacingScale.None, row => {
                row.AddFlex(Box.Create());
                row.AddHug(Avatar.Create(string.Empty, size: new Rem(4f),
                    accent: selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary,
                    background: selected ? ThemeSlot.AccentSoft : ThemeSlot.Glass1,
                    border: ThemeSlot.BorderFaint, texture: structure.Icon,
                    icon: structure.Icon == null ? Icons.Phosphor.Sparkle : (IconRef?)null,
                    iconScale: 1f, tintTexture: false));
                row.AddFlex(Box.Create());
            }, style: new Style { Width = Length.Stretch }));
            c.Add(Text.Create(structure.LabelCap, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.1875f),
                LetterSpacing = Tracking.Of(0.03f),
                TextColor = selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextPrimary,
                TextAlign = TextAlign.Center,
            }));
            c.Add(Text.Create(structure.description ?? string.Empty, wrap: true, style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.71875f),
                TextColor = ThemeSlot.TextSecondary,
                TextAlign = TextAlign.Center,
            }));
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildBeliefStep(StateHandle<int> rev) {
        Ideo? ideo = IdeoDraft.Active();
        List<MemeDef> memes = IdeologyEditor.NormalMemes();

        // Mock groups the starting-meme grid into impact tiers (Impact: Low/Medium/High), each under a
        // mono eyebrow with a trailing rule. Empty tiers are dropped.
        return ScrollArea.Create(
            Stack.Create(SpacingScale.Lg, col => {
                AddMemeTier(col, ideo, memes, 1, "CL_NewColony_Ideology_Wizard_MemeTier_Low", rev);
                AddMemeTier(col, ideo, memes, 2, "CL_NewColony_Ideology_Wizard_MemeTier_Medium", rev);
                AddMemeTier(col, ideo, memes, 3, "CL_NewColony_Ideology_Wizard_MemeTier_High", rev);
            }, style: new Style {
                Width = Length.Stretch,
                // Mock .nc-overlay-body: 26px vertical / 30px horizontal breathing room around the grid.
                Padding = new EdgeInsets(new Rem(1.625f), new Rem(1.875f), new Rem(1.625f), new Rem(1.875f)),
            }),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    // impact 1 -> Low, 2 -> Medium, 3+ -> High (mock MEME_COST low/medium/high).
    private static int MemeTier(int impact) {
        if (impact <= 1) {
            return 1;
        }
        if (impact == 2) {
            return 2;
        }
        return 3;
    }

    private static void AddMemeTier(StackBuilder col, Ideo? ideo, List<MemeDef> memes, int tier, string labelKey, StateHandle<int> rev) {
        List<LightweaveNode> cards = new List<LightweaveNode>();
        for (int i = 0; i < memes.Count; i++) {
            MemeDef meme = memes[i];
            if (MemeTier(meme.impact) != tier) {
                continue;
            }
            bool selected = ideo != null && ideo.memes.Contains(meme);
            MemeDef captured = meme;
            cards.Add(SelectableSurface.Create(
                child: BeliefCard(meme, selected),
                selected: selected,
                variant: SelectableSurfaceVariant.Tile,
                padding: EdgeInsets.All(new Rem(0.875f)),
                // No tooltip: the card already shows the full description inline.
                onSelect: () => {
                    if (ideo != null) {
                        // Single-select: choosing a belief replaces any existing starting meme.
                        IdeoDraftMutations.SetStartingMeme(ideo, captured);
                        Bump(rev);
                    }
                },
                id: "wiz_meme_" + meme.defName));
        }

        if (cards.Count == 0) {
            return;
        }

        col.Add(Stack.Create(SpacingScale.Sm, s => {
            s.Add(NewColonyControls.SectionLabel((string)labelKey.Translate(), trailingRule: true));
            // Mock .nc-meme-grid is a 3-up responsive grid; 22rem min yields three columns at the
            // step's ~80rem width and reflows narrower on smaller surfaces.
            s.Add(Wrap.Create(SpacingScale.Sm, new Rem(22f), g => g.AddRange(cards), stretch: true,
                style: new Style { Width = Length.Stretch }));
        }, style: new Style { Width = Length.Stretch }));
    }

    // Horizontal belief tile: icon badge beside a serif name + muted description (mock .nc-meme-tile).
    private static LightweaveNode BeliefCard(MemeDef meme, bool selected) {
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Avatar.Create(string.Empty, size: new Rem(4f),
                accent: selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary,
                background: selected ? ThemeSlot.AccentSoft : ThemeSlot.Glass1,
                border: ThemeSlot.BorderFaint, texture: meme.Icon,
                icon: meme.Icon == null ? Icons.Phosphor.Sparkle : (IconRef?)null,
                iconScale: 0.62f, tintTexture: false));
            h.AddFlex(Stack.Create(SpacingScale.None, t => {
                t.Add(Text.Create(meme.LabelCap, style: new Style {
                    FontFamily = FontRole.Display,
                    FontSize = new Rem(1f),
                    LetterSpacing = Tracking.Of(0.02f),
                    TextColor = selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextPrimary,
                }));
                t.Add(Text.Create(meme.description ?? string.Empty, wrap: true, style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.71875f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }));
        }, align: FlexAlign.Start, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildFooter(
        StateHandle<bool> wizardOpen,
        StateHandle<int> step,
        StateHandle<IdeoSubPicker> sub,
        StateHandle<int> rev,
        StateHandle<SectionLock> locks,
        int current
    ) {
        // Mock .nc-btn: mono, uppercase, wide tracking, fixed height — applied to every footer button so
        // the ghost Cancel, the secondary Randomize and the primary CTA share one baseline.
        Style footerLabel = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.6875f),
            LetterSpacing = Tracking.Of(0.18f),
            TextColor = ThemeSlot.TextSecondary,
            Height = Length.Rem(3f),
        };
        // Same metrics, but no TextColor so the Primary variant's accent ink (TextOnAccent) still wins.
        Style footerPrimaryLabel = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.6875f),
            LetterSpacing = Tracking.Of(0.18f),
            Height = Length.Rem(3f),
        };

        bool first = current == 0;
        bool last = current == StepCount - 1;

        // The player must make the step's choice before advancing (mock canNext): a structure on step 1,
        // a starting meme on step 2. The final Customize step can always commit.
        Ideo? draft = IdeoDraft.Active();
        bool canAdvance = current switch {
            0 => IdeologyEditor.StructureMeme(draft) != null,
            1 => IdeologyEditor.NonStructureMemeCount(draft) > 0,
            _ => true,
        };

        return HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Button.Create(
                first
                    ? ((string)"CL_NewColony_Ideology_Wizard_Cancel".Translate()).ToUpperInvariant()
                    : ((string)"CL_NewColony_Ideology_Wizard_Back".Translate()).ToUpperInvariant(),
                () => {
                    if (first) {
                        Close(wizardOpen, step, sub);
                    }
                    else {
                        step.Set(current - 1);
                    }
                },
                Variant.Ghost,
                leading: first ? null : Glyph.Create(Icons.Phosphor.ArrowLeft, new Style { FontSize = new Rem(0.75f) }),
                style: footerLabel));

            h.AddHug(Button.Create(
                ((string)"CL_NewColony_Ideology_Wizard_Randomize".Translate()).ToUpperInvariant(),
                () => RandomizeStep(current, locks, rev),
                Variant.Secondary,
                leading: Glyph.Create(Icons.Phosphor.ArrowsClockwise, new Style { FontSize = new Rem(0.75f) }),
                style: footerLabel));

            // Customize step: the "N locked · skipped" hint sits next to Randomize, mirroring the editor.
            if (last) {
                LightweaveNode? lockHint = IdeologyEditor.LockHint(locks);
                if (lockHint != null) {
                    h.AddHug(lockHint);
                }
            }

            h.AddFlex(Box.Create());

            // Mock meme overlay: the running complexity meter sits between the flexible gap and the CTA.
            if (current == 1) {
                h.AddHug(IdeologyEditor.ComplexityMeter(draft));
            }

            // Customize step: the overall-impact readout sits before the Create CTA.
            if (last) {
                h.AddHug(IdeologyEditor.ImpactReadout(draft));
            }

            if (last) {
                h.AddHug(Button.Create(
                    ((string)"CL_NewColony_Ideology_Wizard_Create".Translate()).ToUpperInvariant(),
                    () => Close(wizardOpen, step, sub),
                    Variant.Primary,
                    leading: Glyph.Create(Icons.Phosphor.Check, new Style { FontSize = new Rem(0.875f) }),
                    style: footerPrimaryLabel));
            }
            else {
                h.AddHug(Button.Create(
                    ((string)"CL_NewColony_Ideology_Wizard_Next".Translate()).ToUpperInvariant(),
                    () => step.Set(current + 1),
                    Variant.Primary,
                    trailing: Glyph.Create(Icons.Phosphor.ArrowRight, new Style { FontSize = new Rem(0.875f) }),
                    disabled: !canAdvance,
                    style: footerPrimaryLabel));
            }
        }, align: FlexAlign.Stretch, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1f), new Rem(1.5f), new Rem(1.25f), new Rem(1.5f)),
        });
    }

    private static void RandomizeStep(int current, StateHandle<SectionLock> locks, StateHandle<int> rev) {
        Ideo? ideo = IdeoDraft.Active();
        if (ideo == null) {
            return;
        }

        if (current == 0) {
            List<MemeDef> structures = IdeologyEditor.Structures();
            if (structures.Count > 0) {
                IdeoDraftMutations.SwapStructure(ideo, structures.RandomElement());
            }
        }
        else if (current == 1) {
            List<MemeDef> memes = IdeologyEditor.NormalMemes();
            if (memes.Count > 0) {
                IdeoDraftMutations.SetStartingMeme(ideo, memes.RandomElement());
            }
        }
        else {
            IdeoDraftMutations.RandomizeAll(ideo, locks.Value);
        }
        Bump(rev);
    }

    private static string StepTitle(int step) {
        return step switch {
            0 => (string)"CL_NewColony_Ideology_Wizard_Step1_Title".Translate(),
            1 => (string)"CL_NewColony_Ideology_Wizard_Step2_Title".Translate(),
            _ => (string)"CL_NewColony_Ideology_Wizard_Step3_Title".Translate(),
        };
    }

    private static string StepSubtitle(int step) {
        return step switch {
            0 => (string)"CL_NewColony_Ideology_Wizard_Step1_Sub".Translate(),
            1 => (string)"CL_NewColony_Ideology_Wizard_Step2_Sub".Translate(),
            _ => (string)"CL_NewColony_Ideology_Wizard_Step3_Sub".Translate(),
        };
    }

    private static string StepLabel(int step) {
        return step switch {
            0 => (string)"CL_NewColony_Ideology_Wizard_StepLabel_Structure".Translate(),
            1 => (string)"CL_NewColony_Ideology_Wizard_StepLabel_Belief".Translate(),
            _ => (string)"CL_NewColony_Ideology_Wizard_StepLabel_Customize".Translate(),
        };
    }

    private static string StepHint(int step) {
        return step switch {
            0 => (string)"CL_NewColony_Ideology_Wizard_StepHint_Structure".Translate(),
            1 => (string)"CL_NewColony_Ideology_Wizard_StepHint_Belief".Translate(),
            _ => (string)"CL_NewColony_Ideology_Wizard_StepHint_Customize".Translate(),
        };
    }
}
