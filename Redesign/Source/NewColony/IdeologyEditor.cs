using System.Collections.Generic;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Typography;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Cosmere.Lightweave.Hooks.Hooks;
using Avatar = Cosmere.Lightweave.Data.Avatar;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public enum IdeoSubPicker {
    None,
    Structure,
    Meme,
    Style,
    Precept,
    Xeno,
    Ritual,
    Role,
    Relic,
    Building,
    Animal,
    Culture,
    Apparel,
    Icon,
    Deity,
}

// The "Customize ideoligion" editor, rendered as in-surface Modal overlays inside the Ideology tab's
// own hook tree. The tab runs AFTER world generation, so a live Ideo exists as the player's primary
// (created by IdeoDraft) and the editor reads/mutates it directly through IdeoDraftMutations -
// mirroring vanilla Page_ConfigureIdeo. The rev hook is bumped after each mutation so the tab tree
// re-renders. Sub-pickers nest as further Modal overlays.
public static class IdeologyEditor {
    public static LightweaveNode Build(
        StateHandle<bool> editorOpen,
        StateHandle<IdeoSubPicker> sub,
        StateHandle<int> rev,
        StateHandle<SectionLock> locks,
        StateHandle<IdeoDetailSection> tab,
        Ideo? target = null
    ) {
        // The deity being edited by the Deity modal. Set by a deity chip's pencil, read by BuildDeityEditor.
        // Created here (above both modals) so the pencil in BuildAttributeRow and the modal body share it.
        StateHandle<IdeoFoundation_Deity.Deity?> deityTarget = UseState<IdeoFoundation_Deity.Deity?>(null);

        LightweaveNode editor = Modal.Create(
            editorOpen.Value,
            () => BuildEditor(editorOpen, sub, rev, locks, tab, deityTarget, target),
            () => Close(editorOpen, sub),
            width: new Rem(120f),
            height: new Rem(72f));

        LightweaveNode structurePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Structure,
            () => BuildStructurePicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(42f),
            height: new Rem(30f));

        LightweaveNode memePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Meme,
            () => BuildMemePicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(46f),
            height: new Rem(32f));

        LightweaveNode stylePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Style,
            () => BuildStylePicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(42f),
            height: new Rem(30f));

        LightweaveNode preceptPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Precept,
            () => BuildPreceptPicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode ritualPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Ritual,
            () => BuildRitualPicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode rolePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Role,
            () => BuildRolePicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode relicPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Relic,
            () => BuildRelicPicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode buildingPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Building,
            () => BuildBuildingPicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode animalPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Animal,
            () => BuildAnimalPicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode xenotypePicker = Modal.Create(
            sub.Value == IdeoSubPicker.Xeno,
            () => BuildXenotypePicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode apparelPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Apparel,
            () => BuildApparelPicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(48f),
            height: new Rem(34f));

        LightweaveNode iconPicker = Modal.Create(
            sub.Value == IdeoSubPicker.Icon,
            () => BuildIconPicker(sub, rev, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(46f),
            height: new Rem(38f));

        LightweaveNode deityEditor = Modal.Create(
            sub.Value == IdeoSubPicker.Deity,
            () => BuildDeityEditor(sub, rev, deityTarget, target),
            () => sub.Set(IdeoSubPicker.None),
            width: new Rem(36f),
            height: new Rem(30f));

        LightweaveNode node = NodeBuilder.New("IdeoEditorOverlays", 0, nameof(IdeologyEditor));
        node.Children.Add(editor);
        node.Children.Add(structurePicker);
        node.Children.Add(memePicker);
        node.Children.Add(stylePicker);
        node.Children.Add(preceptPicker);
        node.Children.Add(ritualPicker);
        node.Children.Add(rolePicker);
        node.Children.Add(relicPicker);
        node.Children.Add(buildingPicker);
        node.Children.Add(animalPicker);
        node.Children.Add(xenotypePicker);
        node.Children.Add(apparelPicker);
        node.Children.Add(iconPicker);
        node.Children.Add(deityEditor);
        node.MeasureWidth = () => 0f;
        node.Measure = _ => 0f;
        node.Paint = (rect, _) => {
            PaintOverlay(editor, rect);
            PaintOverlay(structurePicker, rect);
            PaintOverlay(memePicker, rect);
            PaintOverlay(stylePicker, rect);
            PaintOverlay(preceptPicker, rect);
            PaintOverlay(ritualPicker, rect);
            PaintOverlay(rolePicker, rect);
            PaintOverlay(relicPicker, rect);
            PaintOverlay(buildingPicker, rect);
            PaintOverlay(animalPicker, rect);
            PaintOverlay(xenotypePicker, rect);
            PaintOverlay(apparelPicker, rect);
            PaintOverlay(iconPicker, rect);
            PaintOverlay(deityEditor, rect);
        };
        return node;
    }

    private static void PaintOverlay(LightweaveNode modal, Rect rect) {
        modal.MeasuredRect = rect;
        LightweaveRoot.PaintSubtree(modal, rect);
    }

    private static void Close(StateHandle<bool> editorOpen, StateHandle<IdeoSubPicker> sub) {
        sub.Set(IdeoSubPicker.None);
        editorOpen.Set(false);
    }

    private static void Bump(StateHandle<int> rev) {
        rev.Set(rev.Value + 1);
    }

    private static LightweaveNode BuildEditor(StateHandle<bool> editorOpen, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, StateHandle<IdeoDetailSection> tab, StateHandle<IdeoFoundation_Deity.Deity?> deityTarget, Ideo? target = null) {
        return Stack.Create(SpacingScale.None, root => {
            root.Add(BuildHeader(editorOpen, sub, rev, target));
            root.Add(Divider.Horizontal());
            // Editing a faction's ideo is a focused, body-only pane (no faction reference rail); the
            // player-draft editor keeps the two-pane grid (rail + detail).
            if (target != null) {
                // BuildDetail owns its own internal well ScrollArea (scroll inversion); no outer wrap.
                root.AddFlex(BuildDetail(sub, rev, locks, tab, deityTarget, target));
            }
            else {
                root.AddFlex(BuildGrid(sub, rev, locks, tab, deityTarget, target));
            }
            root.Add(Divider.Horizontal());
            root.Add(BuildFooter(editorOpen, sub, rev, locks, target));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
        });
    }

    private static LightweaveNode BuildHeader(StateHandle<bool> editorOpen, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) {
        bool faction = target != null;
        string eyebrow = (faction
            ? "CL_NewColony_Ideology_Editor_EditEyebrow"
            : "CL_NewColony_Ideology_Editor_Eyebrow").Translate();
        string title = faction
            ? (string)"CL_NewColony_Ideology_Editor_EditTitle".Translate(IdeoName(target).Named("IDEO"))
            : (string)"CL_NewColony_Ideology_Editor_Title".Translate();
        string subtitle = (faction
            ? "CL_NewColony_Ideology_Editor_EditSubtitle"
            : "CL_NewColony_Ideology_Editor_Subtitle").Translate();

        LightweaveNode titles = Stack.Create(SpacingScale.Xxs, t => {
            t.Add(Text.Create(eyebrow, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.6875f),
                LetterSpacing = Tracking.Of(0.28f),
                TextColor = ThemeSlot.SurfaceAccent,
            }));
            t.Add(Text.Create(title, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.625f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            t.Add(Text.Create(subtitle, wrap: true, style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.875f),
                TextColor = ThemeSlot.TextSecondary,
            }));
        });

        Style headerAction = new Style {
            Height = new Rem(1.875f),
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.6875f),
            LetterSpacing = Tracking.Of(0.14f),
            TextColor = ThemeSlot.TextSecondary,
        };

        LightweaveNode close = IconButton.Create(
            Glyph.Create(Icons.Phosphor.X, new Style { FontSize = new Rem(1.1f) }),
            () => Close(editorOpen, sub),
            Variant.Secondary,
            tooltipKey: "CL_NewColony_Ideology_Editor_Back");

        LightweaveNode actions = HStack.Create(SpacingScale.Sm, a => {
            // Load/Save operate on the player's saved ideoligion presets (and AssignDraft re-points the
            // player primary), so they only make sense for the draft. Hide them when editing a faction's
            // live ideo in place.
            if (!faction) {
                a.AddHug(Button.Create(
                    ((string)"CL_NewColony_Ideology_Editor_Load".Translate()).ToUpperInvariant(),
                    () => Find.WindowStack.Add(new Dialog_IdeoList_Load(loaded => {
                        if (loaded != null) {
                            loaded.Fluid = IdeoDraft.Active()?.Fluid ?? loaded.Fluid;
                            IdeoDraft.AssignDraft(loaded);
                            Bump(rev);
                        }
                    })),
                    Variant.Ghost, style: headerAction));
                a.AddHug(Button.Create(
                    ((string)"CL_NewColony_Ideology_Editor_Save".Translate()).ToUpperInvariant(),
                    () => {
                        Ideo? live = IdeoDraft.Active();
                        if (live != null) {
                            Find.WindowStack.Add(new Dialog_IdeoList_Save(live));
                        }
                    },
                    Variant.Ghost, style: headerAction));
            }
            a.AddHug(close);
        }, align: FlexAlign.Center);

        return HStack.Create(SpacingScale.Sm, h => {
            h.AddFlex(titles);
            h.AddHug(actions);
        }, align: FlexAlign.Start, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.5f), new Rem(1.5f), new Rem(1f), new Rem(1.5f)),
        });
    }

    private static LightweaveNode BuildGrid(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, StateHandle<IdeoDetailSection> tab, StateHandle<IdeoFoundation_Deity.Deity?> deityTarget, Ideo? target = null) {
        return HStack.Create(SpacingScale.None, h => {
            h.Add(ScrollArea.Create(
                BuildRail(target),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }), new Rem(30f).ToPixels());
            h.AddHug(Divider.Vertical());
            // BuildDetail owns its own internal well ScrollArea (scroll inversion); no outer wrap.
            h.AddFlex(BuildDetail(sub, rev, locks, tab, deityTarget, target));
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildRail(Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();

        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Ideology_Editor_RailCustom".Translate()));
            s.Add(FactionRow(ideo, selected: true));

            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Ideology_Editor_RailFactions".Translate(),
                accent: (ColorRef)ThemeSlot.TextMuted));

            bool any = false;
            if (Current.Game != null && Find.IdeoManager != null) {
                foreach (Ideo other in Find.IdeoManager.IdeosListForReading) {
                    if (other == ideo) {
                        continue;
                    }
                    any = true;
                    s.Add(FactionRow(other, selected: false));
                }
            }

            if (!any) {
                s.Add(Text.Create("CL_NewColony_Ideology_Editor_Faction_Caption".Translate(), wrap: true, style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.75f),
                    TextColor = ThemeSlot.TextMuted,
                }));
            }
        }, style: new Style {
            Padding = new EdgeInsets(new Rem(1.25f), new Rem(1.25f), new Rem(1.25f), new Rem(1.5f)),
        });
    }

    // A read-only reference row for an ideoligion (the player's draft when selected, or a generated
    // faction ideoligion otherwise).
    private static LightweaveNode FactionRow(Ideo? ideo, bool selected) {
        Faction? faction = selected ? null : FactionForIdeo(ideo);
        Texture2D? tex = faction?.def?.FactionIcon ?? ideo?.Icon;
        bool factionEmblem = faction?.def?.FactionIcon != null;

        LightweaveNode badge = Avatar.Create(
            string.Empty,
            size: new Rem(4f),
            accent: selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary,
            background: selected ? ThemeSlot.AccentSoft : ThemeSlot.Glass1,
            border: ThemeSlot.BorderFaint,
            texture: tex,
            icon: tex == null ? Icons.Phosphor.Compass : (IconRef?)null,
            iconScale: 0.62f,
            tintTexture: factionEmblem);

        LightweaveNode row = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(badge);
            h.AddFlex(Text.Create(IdeoName(ideo), style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.875f),
                TextColor = selected ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary,
            }));
        }, style: new Style { Width = Length.Stretch });

        return SelectableSurface.Create(
            child: row,
            selected: selected,
            variant: SelectableSurfaceVariant.ListRow,
            accent: ThemeSlot.SurfaceAccent,
            trailingCaret: false,
            style: new Style { Width = Length.Stretch });
    }

    // An Ideo isn't owned by a single faction, but in New Colony each generated faction has its primary
    // ideoligion - so the reference rail maps an ideo back to that faction to show the real faction emblem.
    private static Faction? FactionForIdeo(Ideo? ideo) {
        if (ideo == null) {
            return null;
        }
        FactionManager? mgr = Find.FactionManager;
        if (mgr == null) {
            return null;
        }
        foreach (Faction f in mgr.AllFactionsListForReading) {
            if (f.ideos != null && f.ideos.PrimaryIdeo == ideo) {
                return f;
            }
        }
        return null;
    }

    internal static LightweaveNode BuildDetail(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, StateHandle<IdeoDetailSection> section, StateHandle<IdeoFoundation_Deity.Deity?> deityTarget, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();

        // The whole editor body scrolls as one unit (#17): identity header, attribute rows, narrative,
        // and the rail+pane all live inside a single outer ScrollArea so the content can be taller than
        // the modal without spilling past the pinned footer. The rail and pane render at their natural
        // height (no inner ScrollArea, no Height.Stretch) and the outer scroll covers them.
        LightweaveNode top = Stack.Create(SpacingScale.Lg, t => {
            t.Add(BuildIdentityHeader(ideo, sub, rev, locks));
            t.Add(BuildAttributeRow(ideo, sub, rev, locks, deityTarget));
            // Narrative gets its own breathing room (~10px top/bottom) so it separates from the rows
            // above and the belief pane below.
            t.Add(Box.Create(
                children: c => c.Add(BuildNarrativeSection(ideo, rev)),
                style: new Style { Width = Length.Stretch, Padding = new EdgeInsets(new Rem(0.625f), new Rem(0f), new Rem(0.625f), new Rem(0f)) }));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.25f), new Rem(1.875f), new Rem(1f), new Rem(1.875f)),
        });

        // Detail rail + pane: the rail lists the belief sections and the preference sections under two
        // eyebrows; the pane renders the active section. Both render natural-height; the outer scroll
        // owns the overflow. A MinHeight floor keeps the split tall enough to read when the active
        // section is short.
        LightweaveNode railPane = Box.Create(
            children: c => c.Add(BuildDetailSplit(ideo, sub, rev, locks, section)),
            style: new Style {
                Width = Length.Stretch,
                MinHeight = new Rem(26f),
                Padding = new EdgeInsets(new Rem(0f), new Rem(1.875f), new Rem(1.375f), new Rem(1.875f)),
            });

        LightweaveNode body = Stack.Create(SpacingScale.None, d => {
            d.Add(top);
            d.Add(railPane);
        }, style: new Style { Width = Length.Stretch });

        return ScrollArea.Create(body, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildDetailSplit(Ideo? ideo, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, StateHandle<IdeoDetailSection> section) {
        // Rail and pane render at natural height; the outer BuildDetail ScrollArea owns the overflow.
        // FlexAlign.Stretch makes the shorter column match the taller one so both backgrounds fill the
        // row, but neither carries an inner scroller (that would nest inside the body scroll).
        LightweaveNode rail = Box.Create(
            children: c => c.Add(BuildDetailRail(ideo, section)),
            style: new Style {
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault),
            });

        LightweaveNode pane = Box.Create(
            children: c => c.Add(BuildSectionPane(ideo, sub, rev, locks, section.Value)),
            style: new Style {
                Width = Length.Stretch,
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault),
            });

        return HStack.Create(SpacingScale.None, h => {
            h.Add(rail, new Rem(14.25f).ToPixels());
            h.AddFlex(pane);
        }, align: FlexAlign.Stretch, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildDetailRail(Ideo? ideo, StateHandle<IdeoDetailSection> section) {
        List<SideNavItem> belief = new List<SideNavItem>(EditorTabs.Length);
        for (int i = 0; i < EditorTabs.Length; i++) {
            IdeoEditorTab t = EditorTabs[i];
            int count = TabCount(ideo, t);
            belief.Add(new SideNavItem(
                IdeoDetailSections.Id(IdeoDetailSections.FromBeliefTab(t)),
                TabLabel(t),
                SectionIcon(IdeoDetailSections.FromBeliefTab(t)),
                count > 0 ? count : (int?)null));
        }

        List<SideNavItem> preferences = [
            new SideNavItem(
                IdeoDetailSections.Id(IdeoDetailSection.Xenotypes),
                (string)"CL_NewColony_Ideology_Editor_Section_Xenotypes".Translate(),
                SectionIcon(IdeoDetailSection.Xenotypes),
                PreferenceCount(ideo, IdeoDetailSection.Xenotypes)),
            new SideNavItem(
                IdeoDetailSections.Id(IdeoDetailSection.Apparel),
                (string)"CL_NewColony_Ideology_Editor_Section_Apparel".Translate(),
                SectionIcon(IdeoDetailSection.Apparel),
                PreferenceCount(ideo, IdeoDetailSection.Apparel)),
            new SideNavItem(
                IdeoDetailSections.Id(IdeoDetailSection.Appearance),
                (string)"CL_NewColony_Ideology_Editor_Section_Appearance".Translate(),
                SectionIcon(IdeoDetailSection.Appearance),
                null),
        ];

        List<SideNavGroup> groups = [
            new SideNavGroup(belief, (string)"CL_NewColony_Ideology_Editor_Rail_Belief".Translate()),
            new SideNavGroup(preferences, (string)"CL_NewColony_Ideology_Editor_Rail_Preferences".Translate()),
        ];

        // No ScrollArea here: the rail renders at its natural list height and the outer BuildDetail
        // scroll owns the overflow (#17). Wrapping it in an inner scroll would nest a 0-height viewport
        // inside the hugging body scroll.
        // Wrap in a Box with 1rem right padding (#8): SideNav paints its rows edge-to-edge across its
        // own rect and does not read Style.Padding, so the inset has to come from a padded parent.
        return Box.Create(
            children: c => c.Add(SideNav.Create(
                groups,
                IdeoDetailSections.Id(section.Value),
                id => section.Set(IdeoDetailSections.Parse(id)),
                style: new Style { Width = Length.Stretch })),
            style: new Style {
                Width = Length.Stretch,
                Padding = new EdgeInsets(new Rem(0f), new Rem(1f), new Rem(0f), new Rem(0f)),
            });
    }

    private static IconRef SectionIcon(IdeoDetailSection section) {
        return section switch {
            IdeoDetailSection.Memes => Icons.Phosphor.Asterisk,
            IdeoDetailSection.Precepts => Icons.Phosphor.ListChecks,
            IdeoDetailSection.Rituals => Icons.Phosphor.Book,
            IdeoDetailSection.Roles => Icons.Phosphor.UserCircle,
            IdeoDetailSection.Relics => Icons.Phosphor.Diamond,
            IdeoDetailSection.Buildings => Icons.Phosphor.Buildings,
            IdeoDetailSection.Animals => Icons.Phosphor.PawPrint,
            IdeoDetailSection.Xenotypes => Icons.Phosphor.Dna,
            IdeoDetailSection.Apparel => Icons.Phosphor.TShirt,
            IdeoDetailSection.Appearance => Icons.Phosphor.Smiley,
            _ => Icons.Phosphor.Asterisk,
        };
    }

    private static int? PreferenceCount(Ideo? ideo, IdeoDetailSection section) {
        if (ideo == null) {
            return null;
        }

        int count = section switch {
            IdeoDetailSection.Xenotypes => IdeoDraftMutations.CountPrecepts<Precept_Xenotype>(ideo),
            IdeoDetailSection.Apparel => IdeoDraftMutations.CountPrecepts<Precept_Apparel>(ideo),
            _ => 0,
        };
        return count > 0 ? count : (int?)null;
    }

    private static LightweaveNode BuildSectionPane(Ideo? ideo, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, IdeoDetailSection section) {
        IdeoEditorTab? belief = IdeoDetailSections.BeliefTab(section);
        if (belief.HasValue) {
            return BuildWell(ideo, sub, rev, locks, belief.Value);
        }

        LightweaveNode content = section switch {
            IdeoDetailSection.Xenotypes => BuildPreferenceList<Precept_Xenotype>(
                ideo, rev,
                "CL_NewColony_Ideology_Editor_Section_Xenotypes",
                "CL_NewColony_Ideology_Editor_AddXenotype",
                IdeoSubPicker.Xeno, sub),
            IdeoDetailSection.Apparel => BuildPreferenceList<Precept_Apparel>(
                ideo, rev,
                "CL_NewColony_Ideology_Editor_Section_Apparel",
                "CL_NewColony_Ideology_Editor_AddApparel",
                IdeoSubPicker.Apparel, sub),
            IdeoDetailSection.Appearance => BuildAppearancePane(ideo),
            _ => EmptyLabel(),
        };

        // Natural height: the outer BuildDetail ScrollArea owns the overflow (#17).
        return Box.Create(
            children: c => c.Add(content),
            style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode PaneHeader(string titleKey, int count) {
        return HStack.Create(SpacingScale.Sm, h => {
            string title = count > 0
                ? (string)"CL_NewColony_Ideology_Editor_PaneHeaderCount".Translate(titleKey.Translate().Named("SECTION"), count.Named("COUNT"))
                : (string)titleKey.Translate();
            h.AddHug(Text.Create(title.ToUpperInvariant(), style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.6875f),
                LetterSpacing = Tracking.Of(0.18f),
                TextColor = ThemeSlot.TextMuted,
            }));
            h.AddFlex(Box.Create(style: new Style {
                Height = Length.Rem(1f / 16f),
                Background = BackgroundSpec.Of(ThemeSlot.BorderFaint),
            }));
        }, align: FlexAlign.Center, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildPreferenceList<T>(Ideo? ideo, StateHandle<int> rev, string titleKey, string addKey, IdeoSubPicker picker, StateHandle<IdeoSubPicker> sub) where T : Precept {
        List<T> precepts = [];
        if (ideo != null) {
            IdeoDraftMutations.CollectPrecepts(ideo, precepts);
        }

        return Stack.Create(SpacingScale.Md, s => {
            s.Add(PaneHeader(titleKey, precepts.Count));
            s.Add(Wrap.Create(SpacingScale.Xs, new Rem(2.5f), w => {
                for (int i = 0; i < precepts.Count; i++) {
                    T captured = precepts[i];
                    w.Add(RemovableChip(
                        captured.Icon,
                        captured.LabelCap,
                        captured.def.description,
                        () => {
                            if (ideo != null) {
                                IdeoDraftMutations.RemovePrecept(ideo, captured);
                                Bump(rev);
                            }
                        }));
                }
                w.Add(AddButton((string)addKey.Translate(), () => sub.Set(picker)));
            }, flow: true));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.125f), new Rem(1.25f), new Rem(1.25f), new Rem(1.25f)),
        });
    }

    private static LightweaveNode BuildAppearancePane(Ideo? ideo) {
        return Stack.Create(SpacingScale.Md, s => {
            s.Add(PaneHeader("CL_NewColony_Ideology_Editor_Section_Appearance", 0));
            s.Add(Layout.Grid.Create(
                [new GridTrack.Fr(1f), new GridTrack.Fr(1f)],
                gap: SpacingScale.Sm,
                children: cells => {
                    cells.Add(AppearanceCard(
                        ideo,
                        "CL_NewColony_Ideology_Editor_Appearance_HairBeard",
                        ideo?.style?.NumHairAndBeardStylesAvailable ?? 0,
                        StyleItemTab.HairAndBeard));
                    cells.Add(AppearanceCard(
                        ideo,
                        "CL_NewColony_Ideology_Editor_Appearance_Tattoo",
                        ideo?.style?.NumTattooStylesAvailable ?? 0,
                        StyleItemTab.Tattoo));
                },
                style: new Style { Width = Length.Stretch }));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.125f), new Rem(1.25f), new Rem(1.25f), new Rem(1.25f)),
        });
    }

    private static LightweaveNode AppearanceCard(Ideo? ideo, string labelKey, int inUse, StyleItemTab tab) {
        LightweaveNode body = Stack.Create(SpacingScale.Xs, b => {
            b.Add(Text.Create((string)labelKey.Translate(), style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(0.9375f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            b.Add(Text.Create(
                (string)"CL_NewColony_Ideology_Editor_Appearance_InUse".Translate(inUse.Named("COUNT")),
                style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.625f),
                    LetterSpacing = Tracking.Of(0.06f),
                    TextColor = ThemeSlot.TextMuted,
                }));
        });

        return SelectableSurface.Create(
            child: body,
            onSelect: () => {
                if (ideo != null) {
                    IdeoDraftMutations.OpenAppearanceEditor(ideo, tab);
                }
            },
            variant: SelectableSurfaceVariant.Tile,
            tooltip: "CL_NewColony_Ideology_Editor_Appearance_Edit".Translate(),
            padding: new EdgeInsets(new Rem(0.75f), new Rem(0.875f), new Rem(0.75f), new Rem(0.875f)),
            style: new Style {
                Width = Length.Stretch,
                Background = BackgroundSpec.Of(ThemeSlot.Glass1),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault),
            });
    }

    private static LightweaveNode BuildIdentityHeader(Ideo? ideo, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks) {
        MemeDef? structure = StructureMeme(ideo);

        // The big symbol badge is the icon affordance - clicking it opens the icon shape + color picker.
        // A corner swatch shows the ideo's identity color (the picker changes it).
        LightweaveNode bigBadge = SelectableSurface.Create(
            child: Avatar.Create(
                string.Empty,
                size: new Rem(4f),
                accent: ThemeSlot.SurfaceAccent,
                background: ThemeSlot.AccentSoft,
                border: ThemeSlot.BorderFaint,
                texture: ideo?.Icon ?? structure?.Icon,
                icon: ideo == null && structure == null ? Icons.Phosphor.Compass : (IconRef?)null,
                iconScale: 0.62f,
                tintTexture: ideo?.Icon != null,
                tintColor: ideo?.Color,
                cornerDot: ideo?.Color,
                chrome: false),
            onSelect: () => {
                if (ideo != null) {
                    sub.Set(IdeoSubPicker.Icon);
                }
            },
            variant: SelectableSurfaceVariant.Tile,
            tooltip: "CL_NewColony_Ideology_Editor_EditIcon".Translate(),
            padding: EdgeInsets.All(new Rem(0.25f)),
            style: new Style { Background = BackgroundSpec.Of(ThemeSlot.Glass1) });

        LightweaveNode nameField = TextField.Create(
            IdeoName(ideo),
            value => {
                if (ideo != null) {
                    IdeoDraftMutations.Rename(ideo, value);
                    Bump(rev);
                }
            },
            instanceKey: ideo,
            variant: Variant.Ghost,
            style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.5f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = ThemeSlot.TextPrimary,
                Width = Length.Stretch,
            });

        LightweaveNode nameLine = HStack.Create(SpacingScale.Sm, h => {
            h.AddFlex(HStack.Create(SpacingScale.Xs, f => {
                f.AddFlex(nameField);
                f.AddHug(SectionLockToggle(SectionLock.Name, locks));
                f.AddHug(SectionRandomize(() => {
                    if (ideo != null) {
                        IdeoDraftMutations.RandomizeTextSymbols(ideo, locks.Value);
                        Bump(rev);
                    }
                }, disabled: (locks.Value & SectionLock.Name) != 0));
            }, align: FlexAlign.Center, style: new Style {
                Width = Length.Stretch,
                Padding = new EdgeInsets(new Rem(0.25f), new Rem(0.5f), new Rem(0.25f), new Rem(0.625f)),
                Background = BackgroundSpec.Of(ThemeSlot.Glass1),
            }));
            h.AddHug(BuildAmbiencePill(ideo));
        }, align: FlexAlign.Stretch, style: new Style { Width = Length.Stretch });

        LightweaveNode miniGrid = Layout.Grid.Create(
            [new GridTrack.Fr(1f), new GridTrack.Fr(1f), new GridTrack.Fr(1f)],
            gap: SpacingScale.Sm,
            children: cells => {
                cells.Add(MiniField(
                    "CL_NewColony_Ideology_Editor_Adjective",
                    ideo?.adjective ?? string.Empty,
                    SectionLock.Adjective, locks,
                    value => { if (ideo != null) { IdeoDraftMutations.SetAdjective(ideo, value); Bump(rev); } },
                    ideo != null));
                cells.Add(MiniField(
                    "CL_NewColony_Ideology_Editor_MemberNoun",
                    ideo?.memberName ?? string.Empty,
                    SectionLock.MemberNoun, locks,
                    value => { if (ideo != null) { IdeoDraftMutations.SetMemberName(ideo, value); Bump(rev); } },
                    ideo != null));
                cells.Add(MiniField(
                    "CL_NewColony_Ideology_Editor_RitualRoom",
                    ideo?.RitualSeatDef?.LabelCap ?? string.Empty,
                    SectionLock.RitualRoom, locks,
                    null,
                    false));
            },
            style: new Style { Width = Length.Stretch });

        LightweaveNode idCol = Stack.Create(SpacingScale.Sm, n => {
            n.Add(nameLine);
            n.Add(miniGrid);
        }, style: new Style { Width = Length.Stretch });

        return HStack.Create(SpacingScale.Md, h => {
            h.AddHug(bigBadge);
            h.AddFlex(idCol);
        }, style: new Style { Width = Length.Stretch }, align: FlexAlign.Start);
    }

    private static LightweaveNode BuildAmbiencePill(Ideo? ideo) {
        // The preview sustainer is owned + frame-maintained by AmbiencePreview (pumped from
        // NewColonyWindow.WindowUpdate); this just reflects its state and toggles it. The glyph is Pause
        // while playing, Play otherwise. The label stacks RITUAL / AMBIENCE on two lines per the mock.
        bool playing = AmbiencePreview.IsPlaying;

        LightweaveNode label = Stack.Create(SpacingScale.None, l => {
            l.Add(Text.Create(((string)"CL_NewColony_Ideology_Editor_RitualAmbience_L1".Translate()).ToUpperInvariant(), style: AmbienceLabelStyle()));
            l.Add(Text.Create(((string)"CL_NewColony_Ideology_Editor_RitualAmbience_L2".Translate()).ToUpperInvariant(), style: AmbienceLabelStyle()));
        });

        LightweaveNode body = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Avatar.Create(
                string.Empty,
                size: new Rem(1.875f),
                accent: ThemeSlot.TextOnAccent,
                background: ThemeSlot.SurfaceAccent,
                border: ThemeSlot.SurfaceAccent,
                icon: playing ? Icons.Phosphor.Pause : Icons.Phosphor.Play,
                iconScale: 0.45f,
                radius: RadiusScale.Full));
            h.AddHug(label);
        }, align: FlexAlign.Center);

        return SelectableSurface.Create(
            child: body,
            onSelect: () => {
                if (ideo != null) {
                    AmbiencePreview.Toggle(ideo);
                }
            },
            variant: SelectableSurfaceVariant.Tile,
            tooltip: "CL_NewColony_Ideology_Editor_RitualAmbience_Tip".Translate(),
            padding: new EdgeInsets(new Rem(0.3125f), new Rem(0.625f), new Rem(0.3125f), new Rem(0.625f)),
            style: new Style {
                Background = BackgroundSpec.Of(ThemeSlot.AccentSoft),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.SurfaceAccent),
            });
    }

    private static Style AmbienceLabelStyle() {
        return new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.5625f),
            LetterSpacing = Tracking.Of(0.08f),
            TextColor = ThemeSlot.TextSecondary,
        };
    }

    // One compact identity field: an eyebrow key over a value, with a lock + pencil cluster (NO dice -
    // re-rolling is driven by the name field's symbol regenerate, not per mini-field). The pencil swaps
    // the value text for an inline TextField; a null onCommit (e.g. the derived ritual room) disables it.
    private static LightweaveNode MiniField(string keyKey, string value, SectionLock lockFlag, StateHandle<SectionLock> locks, System.Action<string>? onCommit, bool editable) {
        StateHandle<bool> editing = UseState(false);
        bool locked = (locks.Value & lockFlag) != 0;
        bool canEdit = editable && onCommit != null && !locked;

        LightweaveNode valueRow;
        if (editing.Value && canEdit) {
            valueRow = TextField.Create(
                value,
                committed => {
                    onCommit!(committed);
                    editing.Set(false);
                },
                instanceKey: keyKey,
                variant: Variant.Ghost,
                style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.875f),
                    TextColor = ThemeSlot.TextPrimary,
                    Width = Length.Stretch,
                });
        }
        else {
            valueRow = HStack.Create(SpacingScale.Xxs, h => {
                h.AddFlex(Text.Create(value, style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.875f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
                h.AddHug(SectionLockToggle(lockFlag, locks));
                h.AddHug(IconButton.Create(
                    Glyph.Create(Icons.Phosphor.PencilSimple, new Style {
                        FontSize = new Rem(0.625f),
                        TextColor = canEdit ? ThemeSlot.TextMuted : ThemeSlot.BorderDefault,
                    }),
                    () => editing.Set(true),
                    disabled: !canEdit,
                    iconSize: new Rem(0.75f),
                    tooltipKey: "CL_NewColony_Ideology_Editor_EditField"));
            }, align: FlexAlign.Center, style: new Style { Width = Length.Stretch });
        }

        return Stack.Create(SpacingScale.Xxs, s => {
            s.Add(Text.Create(
                ((string)keyKey.Translate()).ToUpperInvariant(),
                style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.59375f),
                    LetterSpacing = Tracking.Of(0.12f),
                    TextColor = ThemeSlot.TextMuted,
                }));
            s.Add(valueRow);
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(0.5f), new Rem(0.75f), new Rem(0.5f), new Rem(0.75f)),
            Background = BackgroundSpec.Of(ThemeSlot.Glass1),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderFaint),
        });
    }

    

    


    // Shows the live ideo's generated narrative. When its section lock is on, "Randomize all" keeps
    // this text (and skips the meme/precept levers' description side-effect) rather than re-rolling it.
    // The generated scripture, shown with a left accent rule (mock .scripture). The narrative is derived
    // from memes/precepts and isn't separately editable, so it carries an always-on "LOCKED" marker
    // instead of a toggle; it regenerates naturally when the beliefs change.
    private static LightweaveNode BuildNarrativeSection(Ideo? ideo, StateHandle<int> rev) {
        StateHandle<bool> editing = UseState(false);
        string narrative = ideo?.description ?? string.Empty;

        LightweaveNode header = HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(NewColonyControls.SectionLabel((string)"CL_NewColony_Ideology_Editor_Section_Narrative".Translate(), trailingRule: false));
            h.AddFlex(Box.Create(style: new Style {
                Height = Length.Rem(1f / 16f),
                Background = BackgroundSpec.Of(ThemeSlot.BorderFaint),
            }));
            h.AddHug(Button.Create(
                ((string)(editing.Value
                    ? "CL_NewColony_Ideology_Editor_NarrativeDone"
                    : "CL_NewColony_Ideology_Editor_NarrativeEdit").Translate()).ToUpperInvariant(),
                () => editing.Set(!editing.Value),
                Variant.Ghost,
                leading: Glyph.Create(
                    editing.Value ? Icons.Phosphor.Check : Icons.Phosphor.PencilSimple,
                    new Style { FontSize = new Rem(0.75f) }),
                style: new Style {
                    Height = new Rem(1.875f),
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.6875f),
                    TextColor = ThemeSlot.SurfaceAccent,
                }));
        }, align: FlexAlign.Center, style: new Style { Width = Length.Stretch });

        LightweaveNode body;
        if (editing.Value) {
            body = TextArea.Create(
                narrative,
                value => { if (ideo != null) { IdeoDraftMutations.SetNarrative(ideo, value); Bump(rev); } },
                instanceKey: ideo,
                style: new Style {
                    Width = Length.Stretch,
                    MinHeight = new Rem(4.625f),
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.875f),
                    TextColor = ThemeSlot.TextPrimary,
                });
        }
        else if (string.IsNullOrEmpty(narrative)) {
            body = EmptyLabel();
        }
        else {
            body = HStack.Create(SpacingScale.Sm, h => {
                h.AddHug(Box.Create(style: new Style {
                    Width = Length.Rem(2f / 16f),
                    Height = Length.Stretch,
                    Background = BackgroundSpec.Of(ThemeSlot.AccentGlow),
                }));
                h.AddFlex(Text.Create(narrative, wrap: true, richText: true, style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.875f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }, align: FlexAlign.Stretch, style: new Style { Width = Length.Stretch });
        }

        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(header);
            s.Add(body);
        }, style: new Style { Width = Length.Stretch });
    }

    

    

    // Rituals/roles/relics/buildings/animals are all Precepts living in the same PreceptsListForReading
    // as the issue-stance precepts. Each owns its own tab now, so the Precepts tab excludes them (via
    // IsTypedSectionPrecept) and each typed tab renders only its own subclass.
    private static bool IsTypedSectionPrecept(Precept precept) {
        return precept is Precept_Ritual
            || precept is Precept_Role
            || precept is Precept_Relic
            || precept is Precept_Building
            || precept is Precept_Animal;
    }

    private static int TypedPreceptCount<T>(Ideo? ideo) where T : Precept {
        if (ideo == null) {
            return 0;
        }
        int count = 0;
        foreach (Precept precept in ideo.PreceptsListForReading) {
            if (precept is T && precept.def.visible) {
                count++;
            }
        }
        return count;
    }

    private static int PlainPreceptCount(Ideo? ideo) {
        if (ideo == null) {
            return 0;
        }
        int count = 0;
        foreach (Precept precept in ideo.PreceptsListForReading) {
            if (precept.def.visible && !IsTypedSectionPrecept(precept)) {
                count++;
            }
        }
        return count;
    }

    // Shared body for the precept-backed tabs: a wrap of PreceptRows over just this subclass plus an
    // add button opening the matching picker. No per-section lock or randomize - those precepts are
    // still covered by the global Precepts lock + randomize.
    

    

    

    

    

    

    // The segmented strip after Styles. Memes leads, then Precepts, then the five typed tabs. Count
    // badges come from the live ideo; picking a tab bumps the hook store (re-render), no rev needed.
    private static readonly IdeoEditorTab[] EditorTabs = [
        IdeoEditorTab.Memes,
        IdeoEditorTab.Precepts,
        IdeoEditorTab.Rituals,
        IdeoEditorTab.Roles,
        IdeoEditorTab.Relics,
        IdeoEditorTab.Buildings,
        IdeoEditorTab.Animals,
    ];

    

    // Shared "OVERALL IMPACT n · word" cluster used by both footers (standalone editor + wizard step 3).
    internal static LightweaveNode ImpactReadout(Ideo? ideo) {
        int score = IdeoComplexity.Score(NonStructureMemeCount(ideo), ideo?.PreceptsListForReading.Count ?? 0);
        IdeoImpact level = IdeoComplexity.Label(score);
        ThemeSlot color = level switch {
            IdeoImpact.Low => ThemeSlot.TextMuted,
            IdeoImpact.Medium => ThemeSlot.SurfaceAccent,
            _ => ThemeSlot.StatusWarning,
        };
        string mark = level switch {
            IdeoImpact.Low => "I",
            IdeoImpact.Medium => "II",
            _ => "III",
        };
        Style caption = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.6875f),
            LetterSpacing = Tracking.Of(0.12f),
            TextColor = ThemeSlot.TextMuted,
        };
        return HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(Text.Create(((string)"CL_NewColony_Ideology_Editor_OverallImpactLabel".Translate()).ToUpperInvariant(), style: caption));
            h.AddHug(Text.Create(mark, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(0.9375f),
                LetterSpacing = Tracking.Of(0.04f),
                TextColor = color,
            }));
            h.AddHug(Text.Create(("· " + ImpactLabel(score)).ToUpperInvariant(), style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.6875f),
                LetterSpacing = Tracking.Of(0.12f),
                TextColor = color,
            }));
        }, align: FlexAlign.Center);
    }

    // "N locked · skipped" footer caption. Counts every set section lock the player can toggle (all
    // flags except Narrative, which has no toggle). Returns null when nothing is locked.
    internal static LightweaveNode? LockHint(StateHandle<SectionLock> locks) {
        int count = 0;
        foreach (SectionLock flag in System.Enum.GetValues(typeof(SectionLock))) {
            if (flag != SectionLock.None && flag != SectionLock.Narrative && (locks.Value & flag) != 0) {
                count++;
            }
        }
        if (count == 0) {
            return null;
        }
        return HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(Glyph.Create(Icons.Phosphor.LockSimple, new Style {
                FontSize = new Rem(0.6875f),
                TextColor = ThemeSlot.SurfaceAccent,
            }));
            h.AddHug(Text.Create(((string)"CL_NewColony_Ideology_Editor_LockHint".Translate(count.Named("COUNT"))).ToUpperInvariant(), style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.625f),
                LetterSpacing = Tracking.Of(0.12f),
                TextColor = ThemeSlot.TextMuted,
            }));
        }, align: FlexAlign.Center);
    }

    // The three attribute cards under the identity header: Structure, Deities, Styles. Each is a bordered
    // dark box with a header (label + lock + shuffle) over its body. Folds in the old in-header structure
    // picker, the deities row, and the styles section.
    private static LightweaveNode BuildAttributeRow(Ideo? ideo, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, StateHandle<IdeoFoundation_Deity.Deity?> deityTarget) {
        MemeDef? structure = StructureMeme(ideo);

        LightweaveNode cultureValue = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Dropdown.Create(
                ideo?.culture,
                Cultures(),
                c => c != null ? c.LabelCap.ToString() : (string)"CL_NewColony_Ideology_Editor_NoCulture".Translate(),
                c => {
                    if (ideo != null && c != null) {
                        IdeoDraftMutations.SetCulture(ideo, c);
                        Bump(rev);
                    }
                },
                variant: DropdownVariant.Button,
                buttonStyle: Variant.Ghost,
                instanceKey: ideo));
            h.AddHug(Text.Create((string)"CL_NewColony_Ideology_Editor_CultureCaption".Translate(), style: new Style {
                FontFamily = FontRole.Mono, FontSize = new Rem(0.59375f), TextColor = ThemeSlot.TextMuted,
            }));
        }, align: FlexAlign.Center);

        LightweaveNode cultureRow = AttrRow(Icons.Phosphor.Globe, "CL_NewColony_Ideology_Editor_Section_Culture",
            cultureValue, SectionLock.Culture, locks,
            () => { if (ideo != null) { IdeoDraftMutations.RandomizeCulture(ideo, locks.Value); Bump(rev); } });

        LightweaveNode structureTrigger = SelectableSurface.Create(
            child: HStack.Create(SpacingScale.Sm, h => {
                if (structure?.Icon != null) {
                    h.AddHug(Avatar.Create(string.Empty, size: new Rem(1.625f),
                        accent: ThemeSlot.SurfaceAccent, background: ThemeSlot.AccentSoft, border: ThemeSlot.BorderFaint,
                        texture: structure.Icon, iconScale: 0.7f, tintTexture: false));
                }
                h.AddHug(Text.Create(
                    structure != null ? structure.LabelCap.ToString() : (string)"CL_NewColony_Ideology_Editor_NoStructure".Translate(),
                    style: new Style { FontFamily = FontRole.Display, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextPrimary }));
                h.AddHug(Glyph.Create(Icons.Phosphor.CaretDown, new Style { FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted }));
            }, align: FlexAlign.Center),
            onSelect: () => sub.Set(IdeoSubPicker.Structure),
            variant: SelectableSurfaceVariant.Tile,
            tooltip: "CL_NewColony_Ideology_Editor_Picker_Structure_Title".Translate(),
            padding: new EdgeInsets(new Rem(0.25f), new Rem(0.625f), new Rem(0.25f), new Rem(0.4375f)),
            style: new Style { Background = BackgroundSpec.Of(ThemeSlot.Glass1), Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault) });

        LightweaveNode structureValue = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(structureTrigger);
            h.AddHug(Text.Create((string)"CL_NewColony_Ideology_Editor_StructureCaption".Translate(), style: new Style {
                FontFamily = FontRole.Mono, FontSize = new Rem(0.59375f), TextColor = ThemeSlot.TextMuted,
            }));
        }, align: FlexAlign.Center);

        LightweaveNode structureRow = AttrRow(Icons.Phosphor.Compass, "CL_NewColony_Ideology_Editor_Section_Structure",
            structureValue, SectionLock.Structure, locks,
            () => { if (ideo != null) { IdeoDraftMutations.RandomizeStructure(ideo); Bump(rev); } });

        IdeoFoundation_Deity? deityFoundation = ideo?.foundation as IdeoFoundation_Deity;
        bool hasDeitySupport = deityFoundation != null && ideo != null && ideo.DeityCountRange.max > 0;
        int deityCount = deityFoundation?.DeitiesListForReading.Count ?? 0;
        LightweaveNode deitiesValue = Wrap.Create(SpacingScale.Xs, new Rem(2.5f), w => {
            if (deityFoundation != null) {
                foreach (IdeoFoundation_Deity.Deity deity in deityFoundation.DeitiesListForReading) {
                    IdeoFoundation_Deity.Deity captured = deity;
                    w.Add(DeityChip(captured,
                        () => {
                            deityTarget.Set(captured);
                            sub.Set(IdeoSubPicker.Deity);
                        },
                        () => {
                            if (ideo != null) {
                                IdeoDraftMutations.RemoveDeity(ideo, captured);
                                Bump(rev);
                            }
                        }));
                }
            }
            if (ideo != null && IdeoDraftMutations.CanAddDeity(ideo)) {
                w.Add(AddButton((string)"CL_NewColony_Ideology_Editor_AddDeity".Translate(), () => {
                    IdeoDraftMutations.AddDeity(ideo);
                    Bump(rev);
                }));
            }
            else if (!hasDeitySupport && deityCount == 0) {
                // Foundations like Animism venerate spirits, not named gods (DeityCountRange.max == 0).
                // Show the placeholder so the row reads as intentional rather than broken/empty.
                w.Add(Text.Create((string)"CL_NewColony_Ideology_Editor_Deities_Empty".Translate(), style: new Style {
                    FontFamily = FontRole.Body, FontSize = new Rem(0.8125f), TextColor = ThemeSlot.TextMuted,
                }));
            }
        }, flow: true);

        LightweaveNode deitiesRow = AttrRow(Icons.Phosphor.Crown, "CL_NewColony_Ideology_Editor_Section_Deities",
            deitiesValue, SectionLock.Deities, locks,
            () => { if (ideo != null) { IdeoDraftMutations.RegenerateDeities(ideo); Bump(rev); } });

        LightweaveNode stylesValue = Wrap.Create(SpacingScale.Xs, new Rem(2.5f), w => {
            if (ideo?.thingStyleCategories != null) {
                foreach (ThingStyleCategoryWithPriority style in ideo.thingStyleCategories) {
                    StyleCategoryDef captured = style.category;
                    w.Add(RemovableChip(captured.Icon, captured.LabelCap, captured.description, () => {
                        IdeoDraftMutations.ToggleStyle(ideo, captured);
                        Bump(rev);
                    }));
                }
            }
            w.Add(AddButton((string)"CL_NewColony_Ideology_Editor_AddStyle".Translate(), () => sub.Set(IdeoSubPicker.Style)));
        }, flow: true);

        LightweaveNode stylesRow = AttrRow(Icons.Phosphor.Star, "CL_NewColony_Ideology_Editor_Section_Styles",
            stylesValue, SectionLock.Styles, locks,
            () => { if (ideo != null) { IdeoDraftMutations.RandomizeStyles(ideo); Bump(rev); } });

        return Stack.Create(SpacingScale.None, s => {
            s.Add(cultureRow);
            s.Add(structureRow);
            s.Add(deitiesRow);
            s.Add(stylesRow);
        }, style: new Style { Width = Length.Stretch });
    }

    // One inline attribute row: a fixed-width lead (icon + eyebrow), a flexible value region, and a
    // trailing lock + dice cluster. A top border separates rows; the first row's border is suppressed
    // by the zero-gap Stack that hosts them (the visual rule comes from the row's own top border).
    private static LightweaveNode AttrRow(IconRef leadIcon, string eyebrowKey, LightweaveNode value, SectionLock lockFlag, StateHandle<SectionLock> locks, System.Action onRandomize) {
        LightweaveNode lead = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Glyph.Create(leadIcon, new Style { FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextSecondary }));
            h.AddHug(Text.Create(
                ((string)eyebrowKey.Translate()).ToUpperInvariant(),
                style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.625f),
                    LetterSpacing = Tracking.Of(0.1f),
                    TextColor = ThemeSlot.TextMuted,
                }));
        }, align: FlexAlign.Center);

        LightweaveNode tools = HStack.Create(SpacingScale.Xxs, h => {
            h.AddHug(SectionLockToggle(lockFlag, locks));
            h.AddHug(SectionRandomize(onRandomize, disabled: (locks.Value & lockFlag) != 0));
        }, align: FlexAlign.Center);

        return HStack.Create(SpacingScale.Md, h => {
            // Lead column width = badge(4.5rem) + identity HStack gap(Md 1rem) so the row value boxes
            // share a left edge with the name field and mini-grid above them (#4).
            h.Add(lead, new Rem(5.5f).ToPixels());
            h.AddFlex(value);
            h.AddHug(tools);
        }, align: FlexAlign.Start, style: new Style {
            Width = Length.Stretch,
            MinHeight = new Rem(3.25f),
            Padding = new EdgeInsets(new Rem(0.6875f), new Rem(0f), new Rem(0.6875f), new Rem(0f)),
            Border = new BorderSpec(Top: new Rem(1f / 16f), Color: ThemeSlot.BorderFaint),
        });
    }

    private static IReadOnlyList<CultureDef> Cultures() {
        return DefDatabase<CultureDef>.AllDefsListForReading;
    }

    private static LightweaveNode AttributeCard(string labelKey, SectionLock section, StateHandle<SectionLock> locks, System.Action onShuffle, LightweaveNode body, LightweaveNode? addAffordance) {
        bool locked = (locks.Value & section) != 0;
        LightweaveNode header = HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(NewColonyControls.SectionLabel((string)labelKey.Translate(), trailingRule: false));
            h.AddFlex(Box.Create());
            h.AddHug(SectionLockToggle(section, locks));
            h.AddHug(SectionRandomize(onShuffle, disabled: locked));
        }, align: FlexAlign.Center, style: new Style { Width = Length.Stretch });

        return Box.Create(
            children: c => c.Add(Stack.Create(new Rem(0.5625f), s => {
                s.Add(header);
                s.Add(body);
                if (addAffordance != null) {
                    s.Add(addAffordance);
                }
            }, style: new Style { Width = Length.Stretch })),
            style: new Style {
                Width = Length.Stretch,
                Padding = new EdgeInsets(new Rem(0.6875f), new Rem(0.875f), new Rem(0.6875f), new Rem(0.875f)),
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault),
                Radius = RadiusSpec.None,
            });
    }

    private static string WellDescKey(IdeoEditorTab t) {
        return t switch {
            IdeoEditorTab.Memes => "CL_NewColony_Ideology_Editor_WellDesc_Memes",
            IdeoEditorTab.Precepts => "CL_NewColony_Ideology_Editor_WellDesc_Precepts",
            IdeoEditorTab.Rituals => "CL_NewColony_Ideology_Editor_WellDesc_Rituals",
            IdeoEditorTab.Roles => "CL_NewColony_Ideology_Editor_WellDesc_Roles",
            IdeoEditorTab.Relics => "CL_NewColony_Ideology_Editor_WellDesc_Relics",
            IdeoEditorTab.Buildings => "CL_NewColony_Ideology_Editor_WellDesc_Buildings",
            IdeoEditorTab.Animals => "CL_NewColony_Ideology_Editor_WellDesc_Animals",
            _ => "CL_NewColony_Ideology_Editor_WellDesc_Memes",
        };
    }

    // The scrolling content well below the tab strip: a fixed bordered box (border-top reads continuous
    // with the strip) whose inner ScrollArea holds the per-tab header (description + lock + randomize,
    // plus the Memes impact pill) and the gallery grid.
    private static LightweaveNode BuildWell(Ideo? ideo, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, IdeoEditorTab t) {
        bool locked = (locks.Value & TabLockFlag(t)) != 0;

        LightweaveNode header = HStack.Create(SpacingScale.Md, h => {
            h.AddFlex(Text.Create((string)WellDescKey(t).Translate(), wrap: true, style: new Style {
                FontFamily = FontRole.Body, FontSize = new Rem(0.78125f), TextColor = ThemeSlot.TextMuted,
            }));
            h.AddHug(HStack.Create(SpacingScale.Xxs, a => {
                if (t == IdeoEditorTab.Memes) {
                    int score = IdeoComplexity.Score(NonStructureMemeCount(ideo), ideo?.PreceptsListForReading.Count ?? 0);
                    a.AddHug(CountTag.Create(
                        (string)"CL_NewColony_Ideology_Editor_MemesImpact".Translate(score.Named("SCORE"), IdeoComplexity.MaxScore.Named("MAX")),
                        CountTagTone.Accent));
                }
                a.AddHug(SectionLockToggle(TabLockFlag(t), locks));
                a.AddHug(SectionRandomize(() => {
                    if (ideo != null) {
                        RandomizeTab(ideo, t);
                        Bump(rev);
                    }
                }, disabled: locked));
            }, align: FlexAlign.Center));
        }, align: FlexAlign.Start, style: new Style { Width = Length.Stretch });

        LightweaveNode content = Stack.Create(SpacingScale.Md, s => {
            s.Add(header);
            s.Add(BuildGallery(ideo, sub, rev, t));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.125f), new Rem(1.25f), new Rem(1.25f), new Rem(1.25f)),
        });

        // Natural height: the outer BuildDetail ScrollArea owns the overflow (#17). The well keeps its
        // sunken-panel chrome but renders the gallery at full height instead of in an inner scroll.
        return Box.Create(
            children: c => c.Add(content),
            style: new Style {
                Width = Length.Stretch,
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault),
                Radius = RadiusSpec.None,
            });
    }

    private static (System.Func<Precept, bool> match, string addKey, IdeoSubPicker picker) GalleryPreceptSpec(IdeoEditorTab t) {
        switch (t) {
            case IdeoEditorTab.Rituals:
                return (p => p is Precept_Ritual, "CL_NewColony_Ideology_Editor_AddRitual", IdeoSubPicker.Ritual);
            case IdeoEditorTab.Roles:
                return (p => p is Precept_Role, "CL_NewColony_Ideology_Editor_AddRole", IdeoSubPicker.Role);
            case IdeoEditorTab.Relics:
                return (p => p is Precept_Relic, "CL_NewColony_Ideology_Editor_AddRelic", IdeoSubPicker.Relic);
            case IdeoEditorTab.Buildings:
                return (p => p is Precept_Building, "CL_NewColony_Ideology_Editor_AddBuilding", IdeoSubPicker.Building);
            case IdeoEditorTab.Animals:
                return (p => p is Precept_Animal, "CL_NewColony_Ideology_Editor_AddAnimal", IdeoSubPicker.Animal);
            default:
                return (p => !IsTypedSectionPrecept(p), "CL_NewColony_Ideology_Editor_AddPrecept", IdeoSubPicker.Precept);
        }
    }

    // The per-tab content: a 3-up grid of gallery cards plus a trailing add tile. No ghost slots, no caps.
    private static LightweaveNode BuildGallery(Ideo? ideo, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, IdeoEditorTab t) {
        return Layout.Grid.Create(
            [new GridTrack.Fr(1f), new GridTrack.Fr(1f), new GridTrack.Fr(1f)],
            gap: SpacingScale.Sm,
            children: cells => {
                if (t == IdeoEditorTab.Memes) {
                    if (ideo != null) {
                        foreach (MemeDef meme in ideo.memes) {
                            if (meme.category == MemeCategory.Structure) {
                                continue;
                            }
                            MemeDef captured = meme;
                            cells.Add(GalleryCard.Create(
                                meme.LabelCap,
                                meme.description,
                                iconTexture: meme.Icon,
                                onRemove: () => {
                                    IdeoDraftMutations.RemoveMeme(ideo, captured);
                                    Bump(rev);
                                },
                                removeTooltipKey: "CL_NewColony_Ideology_Editor_Remove"));
                        }
                    }
                    cells.Add(AddTile.Create((string)"CL_NewColony_Ideology_Editor_AddMeme".Translate(), () => sub.Set(IdeoSubPicker.Meme)));
                    return;
                }

                (System.Func<Precept, bool> match, string addKey, IdeoSubPicker picker) = GalleryPreceptSpec(t);
                if (ideo != null) {
                    foreach (Precept precept in ideo.PreceptsListForReading) {
                        if (!precept.def.visible || !match(precept)) {
                            continue;
                        }
                        Precept captured = precept;
                        string title = precept.def.issue != null
                            ? precept.def.issue.LabelCap.ToString()
                            : precept.LabelCap.ToString();
                        cells.Add(GalleryCard.Create(
                            title,
                            precept.def.description,
                            icon: precept.Icon == null ? Icons.Phosphor.Sparkle : (IconRef?)null,
                            iconTexture: precept.Icon,
                            onRemove: () => {
                                IdeoDraftMutations.RemovePrecept(ideo, captured);
                                Bump(rev);
                            },
                            removeTooltipKey: "CL_NewColony_Ideology_Editor_Remove"));
                    }
                }
                cells.Add(AddTile.Create((string)addKey.Translate(), () => sub.Set(picker)));
            },
            style: new Style { Width = Length.Stretch });
    }

    // Only the meme and precept tabs carry a lock (they own the randomize affordance); the typed
    // precept tabs return null so no glyph renders. Maps tab -> SectionLock flag bidirectionally.
    // Every tab now owns a lock (each section has its own randomize affordance in the well header).
    private static SectionLock TabLockFlag(IdeoEditorTab t) {
        return t switch {
            IdeoEditorTab.Memes => SectionLock.Memes,
            IdeoEditorTab.Precepts => SectionLock.Precepts,
            IdeoEditorTab.Rituals => SectionLock.Rituals,
            IdeoEditorTab.Roles => SectionLock.Roles,
            IdeoEditorTab.Relics => SectionLock.Relics,
            IdeoEditorTab.Buildings => SectionLock.Buildings,
            IdeoEditorTab.Animals => SectionLock.Animals,
            _ => SectionLock.None,
        };
    }

    private static void RandomizeTab(Ideo ideo, IdeoEditorTab t) {
        switch (t) {
            case IdeoEditorTab.Memes:
                IdeoDraftMutations.RandomizeMemes(ideo);
                break;
            case IdeoEditorTab.Precepts:
                IdeoDraftMutations.RandomizePrecepts(ideo);
                break;
            case IdeoEditorTab.Rituals:
                IdeoDraftMutations.RandomizeRituals(ideo);
                break;
            case IdeoEditorTab.Roles:
                IdeoDraftMutations.RandomizeRoles(ideo);
                break;
            case IdeoEditorTab.Relics:
                IdeoDraftMutations.RandomizeRelics(ideo);
                break;
            case IdeoEditorTab.Buildings:
                IdeoDraftMutations.RandomizeBuildings(ideo);
                break;
            case IdeoEditorTab.Animals:
                IdeoDraftMutations.RandomizeAnimals(ideo);
                break;
        }
    }

    private static bool? TabLockState(IdeoEditorTab t, StateHandle<SectionLock> locks) {
        SectionLock flag = TabLockFlag(t);
        return flag == SectionLock.None ? (bool?)null : (locks.Value & flag) != 0;
    }

    private static void ToggleTabLock(IdeoEditorTab t, StateHandle<SectionLock> locks) {
        SectionLock flag = TabLockFlag(t);
        if (flag == SectionLock.None) {
            return;
        }
        bool locked = (locks.Value & flag) != 0;
        locks.Set(locked ? locks.Value & ~flag : locks.Value | flag);
    }

    private static string TabLabel(IdeoEditorTab t) {
        return t switch {
            IdeoEditorTab.Memes => (string)"CL_NewColony_Ideology_Editor_Section_Memes".Translate(),
            IdeoEditorTab.Precepts => (string)"CL_NewColony_Ideology_Editor_Section_Precepts".Translate(),
            IdeoEditorTab.Rituals => (string)"CL_NewColony_Ideology_Editor_Section_Rituals".Translate(),
            IdeoEditorTab.Roles => (string)"CL_NewColony_Ideology_Editor_Section_Roles".Translate(),
            IdeoEditorTab.Relics => (string)"CL_NewColony_Ideology_Editor_Section_Relics".Translate(),
            IdeoEditorTab.Buildings => (string)"CL_NewColony_Ideology_Editor_Section_Buildings".Translate(),
            IdeoEditorTab.Animals => (string)"CL_NewColony_Ideology_Editor_Section_Animals".Translate(),
            _ => string.Empty,
        };
    }

    private static int TabCount(Ideo? ideo, IdeoEditorTab t) {
        return t switch {
            IdeoEditorTab.Memes => NonStructureMemeCount(ideo),
            IdeoEditorTab.Precepts => PlainPreceptCount(ideo),
            IdeoEditorTab.Rituals => TypedPreceptCount<Precept_Ritual>(ideo),
            IdeoEditorTab.Roles => TypedPreceptCount<Precept_Role>(ideo),
            IdeoEditorTab.Relics => TypedPreceptCount<Precept_Relic>(ideo),
            IdeoEditorTab.Buildings => TypedPreceptCount<Precept_Building>(ideo),
            IdeoEditorTab.Animals => TypedPreceptCount<Precept_Animal>(ideo),
            _ => 0,
        };
    }

    

    // Deities are read-only here (vanilla rolls them from the structure meme); the only affordance is
    // regenerate. Lives in the identity header, not a tab.
    

    private static LightweaveNode DeityChip(IdeoFoundation_Deity.Deity deity, System.Action onEdit, System.Action onRemove) {
        string name = deity.name ?? string.Empty;
        return Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.Xs, h => {
                if (deity.Icon != null) {
                    h.AddHug(Avatar.Create(string.Empty, size: new Rem(1.375f),
                        accent: ThemeSlot.SurfaceAccent, background: ThemeSlot.AccentSoft, border: ThemeSlot.BorderFaint,
                        texture: deity.Icon, iconScale: 0.7f, tintTexture: false, radius: RadiusScale.Full));
                }
                h.AddHug(Text.Create(name, style: new Style {
                    FontFamily = FontRole.Display,
                    FontSize = new Rem(0.8125f),
                    TextColor = ThemeSlot.TextPrimary,
                }));
                h.AddHug(IconButton.Create(
                    Glyph.Create(Icons.Phosphor.PencilSimple, new Style { FontSize = new Rem(0.625f) }),
                    onEdit,
                    iconSize: new Rem(0.75f),
                    tooltipKey: "CL_NewColony_Ideology_Editor_EditDeity"));
                h.AddHug(IconButton.Create(
                    Glyph.Create(Icons.Phosphor.X, new Style { FontSize = new Rem(0.625f), TextColor = ThemeSlot.TextMuted }),
                    onRemove,
                    iconSize: new Rem(0.75f),
                    tooltipKey: "CL_NewColony_Ideology_Editor_RemoveDeity"));
            }, align: FlexAlign.Center)),
            style: new Style {
                Padding = new EdgeInsets(new Rem(0.25f), new Rem(0.4375f), new Rem(0.25f), new Rem(0.3125f)),
                Background = BackgroundSpec.Of(ThemeSlot.Glass1),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderFaint),
            });
    }

    // Picker for one precept subclass: lists every allowed PreceptDef of that class (reusing the
    // meme-allow filter from the Precepts picker), toggling membership by def. Note: for the
    // ThingDef-keyed classes (relic/building/animal) a def's instance auto-rolls its ThingDef on add,
    // so this picks the category, not the specific item.
    private static LightweaveNode BuildTypedPreceptPicker(
        StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, System.Type preceptClass,
        string titleKey, string subtitleKey, string idPrefix, Ideo? target
    ) {
        Ideo? ideo = target ?? IdeoDraft.Active();
        HashSet<string> present = PresentMemeDefNames(ideo);

        return PickerShell(titleKey.Translate(), subtitleKey.Translate(), sub, grid => {
            foreach (PreceptDef pd in DefDatabase<PreceptDef>.AllDefsListForReading) {
                if (!pd.visible || pd.preceptClass == null || !preceptClass.IsAssignableFrom(pd.preceptClass)) {
                    continue;
                }

                List<string>? required = RequiredMemeDefNames(pd);
                bool allowed = IdeoPreceptRules.IsAllowed(required, present);
                Precept? owned = OwnedPrecept(ideo, pd);
                bool selected = owned != null;
                if (!allowed && !selected) {
                    continue;
                }

                PreceptDef capturedDef = pd;
                Precept? capturedOwned = owned;
                grid.Add(SelectableSurface.Create(
                    child: PickerTile(null, pd.LabelCap, selected),
                    selected: selected,
                    variant: SelectableSurfaceVariant.Tile,
                    tooltipContent: () => Text.Create(pd.description ?? string.Empty, wrap: true),
                    onSelect: () => {
                        if (ideo != null) {
                            if (capturedOwned != null) {
                                IdeoDraftMutations.RemovePrecept(ideo, capturedOwned);
                            }
                            else {
                                IdeoDraftMutations.AddPrecept(ideo, capturedDef);
                            }
                            Bump(rev);
                        }
                    },
                    id: idPrefix + pd.defName));
            }
        });
    }

    internal static LightweaveNode BuildRitualPicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) =>
        BuildTypedPreceptPicker(sub, rev, typeof(Precept_Ritual),
            "CL_NewColony_Ideology_Editor_Picker_Ritual_Title", "CL_NewColony_Ideology_Editor_Picker_Ritual_Subtitle", "ideo_ritual_", target);

    internal static LightweaveNode BuildRolePicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) =>
        BuildTypedPreceptPicker(sub, rev, typeof(Precept_Role),
            "CL_NewColony_Ideology_Editor_Picker_Role_Title", "CL_NewColony_Ideology_Editor_Picker_Role_Subtitle", "ideo_role_", target);

    internal static LightweaveNode BuildRelicPicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) =>
        BuildTypedPreceptPicker(sub, rev, typeof(Precept_Relic),
            "CL_NewColony_Ideology_Editor_Picker_Relic_Title", "CL_NewColony_Ideology_Editor_Picker_Relic_Subtitle", "ideo_relic_", target);

    internal static LightweaveNode BuildBuildingPicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) =>
        BuildTypedPreceptPicker(sub, rev, typeof(Precept_Building),
            "CL_NewColony_Ideology_Editor_Picker_Building_Title", "CL_NewColony_Ideology_Editor_Picker_Building_Subtitle", "ideo_building_", target);

    internal static LightweaveNode BuildAnimalPicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) =>
        BuildTypedPreceptPicker(sub, rev, typeof(Precept_Animal),
            "CL_NewColony_Ideology_Editor_Picker_Animal_Title", "CL_NewColony_Ideology_Editor_Picker_Animal_Subtitle", "ideo_animal_", target);

    internal static LightweaveNode BuildXenotypePicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) =>
        BuildTypedPreceptPicker(sub, rev, typeof(Precept_Xenotype),
            "CL_NewColony_Ideology_Editor_Picker_Xenotype_Title", "CL_NewColony_Ideology_Editor_Picker_Xenotype_Subtitle", "ideo_xenotype_", target);

    internal static LightweaveNode BuildApparelPicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) =>
        BuildTypedPreceptPicker(sub, rev, typeof(Precept_Apparel),
            "CL_NewColony_Ideology_Editor_Picker_Apparel_Title", "CL_NewColony_Ideology_Editor_Picker_Apparel_Subtitle", "ideo_apparel_", target);

    internal static LightweaveNode BuildIconPicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();

        StateHandle<IdeoIconDef?> pendingIcon = UseState(ideo?.iconDef);
        StateHandle<ColorDef?> pendingColor = UseState(ideo?.colorDef);

        List<ColorDef> allColors = DefDatabase<ColorDef>.AllDefsListForReading;
        List<ColorDef> ideoColors = new List<ColorDef>(allColors.Count);
        for (int i = 0; i < allColors.Count; i++) {
            if (allColors[i].colorType == ColorType.Ideo) {
                ideoColors.Add(allColors[i]);
            }
        }

        Color[] palette = new Color[ideoColors.Count];
        for (int i = 0; i < ideoColors.Count; i++) {
            palette[i] = ideoColors[i].color;
        }

        Color previewColor = pendingColor.Value?.color ?? Color.white;

        LightweaveNode colorPicker = ColorPicker.Create(
            previewColor,
            picked => {
                for (int i = 0; i < ideoColors.Count; i++) {
                    if (ideoColors[i].color == picked) {
                        pendingColor.Set(ideoColors[i]);
                        return;
                    }
                }
            },
            palette: palette,
            style: new Style { Width = Length.Stretch });

        List<IdeoIconDef> icons = DefDatabase<IdeoIconDef>.AllDefsListForReading;
        LightweaveNode shapeGrid = Wrap.Create(SpacingScale.Xs, new Rem(2.75f), w => {
            for (int i = 0; i < icons.Count; i++) {
                IdeoIconDef captured = icons[i];
                bool selected = captured == pendingIcon.Value;
                w.Add(IconButton.Create(
                    Avatar.Create(string.Empty, size: new Rem(1.75f),
                        texture: captured.Icon, iconScale: 1f, tintColor: previewColor, chrome: false),
                    () => pendingIcon.Set(captured),
                    iconSize: new Rem(1.75f),
                    active: selected,
                    id: "ideo_icon_" + captured.defName));
            }
        }, flow: true, style: new Style { Width = Length.Stretch });

        return Stack.Create(SpacingScale.Md, root => {
            root.Add(PickerHeader(
                (string)"CL_NewColony_Ideology_Editor_Picker_Add".Translate(),
                (string)"CL_NewColony_Ideology_Editor_Picker_Icon_Title".Translate(),
                (string)"CL_NewColony_Ideology_Editor_Picker_Icon_Subtitle".Translate()));
            root.Add(Divider.Horizontal());
            root.AddFlex(ScrollArea.Create(
                Stack.Create(SpacingScale.Md, body => {
                    body.Add(Text.Create((string)"CL_NewColony_Ideology_Editor_Picker_Icon_Color".Translate(), style: new Style {
                        FontFamily = FontRole.Mono, FontSize = new Rem(0.625f),
                        LetterSpacing = Tracking.Of(0.28f), TextColor = ThemeSlot.TextMuted,
                    }));
                    body.Add(colorPicker);
                    body.Add(Text.Create((string)"CL_NewColony_Ideology_Editor_Picker_Icon_Shape".Translate(), style: new Style {
                        FontFamily = FontRole.Mono, FontSize = new Rem(0.625f),
                        LetterSpacing = Tracking.Of(0.28f), TextColor = ThemeSlot.TextMuted,
                    }));
                    body.Add(shapeGrid);
                }, style: new Style { Width = Length.Stretch }),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }));
            root.Add(Divider.Horizontal());
            root.Add(HStack.Create(SpacingScale.Sm, f => {
                f.AddFlex(Box.Create());
                f.AddHug(Button.Create((string)"CL_NewColony_Ideology_Editor_Picker_Close".Translate(),
                    () => sub.Set(IdeoSubPicker.None), Variant.Ghost));
                f.AddHug(Button.Create((string)"CL_NewColony_Ideology_Editor_Apply".Translate(),
                    () => {
                        if (ideo != null && pendingIcon.Value != null && pendingColor.Value != null) {
                            IdeoDraftMutations.SetIcon(ideo, pendingIcon.Value, pendingColor.Value);
                            Bump(rev);
                        }
                        sub.Set(IdeoSubPicker.None);
                    },
                    Variant.Primary));
            }, style: new Style { Width = Length.Stretch }));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = EdgeInsets.All(new Rem(1.5f)),
        });
    }

    internal static LightweaveNode BuildDeityEditor(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<IdeoFoundation_Deity.Deity?> deityTarget, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();
        IdeoFoundation_Deity.Deity? deity = deityTarget.Value;

        StateHandle<string> name = UseState(deity?.name ?? string.Empty);
        StateHandle<string> title = UseState(deity?.type ?? string.Empty);
        StateHandle<Gender> gender = UseState(deity?.gender ?? Gender.None);

        Gender[] genders = [Gender.None, Gender.Male, Gender.Female];

        return Stack.Create(SpacingScale.Md, root => {
            root.Add(PickerHeader(
                (string)"CL_NewColony_Ideology_Editor_Picker_Add".Translate(),
                (string)"CL_NewColony_Ideology_Editor_Picker_Deity_Title".Translate(),
                (string)"CL_NewColony_Ideology_Editor_Picker_Deity_Subtitle".Translate()));
            root.Add(Divider.Horizontal());
            root.Add(Stack.Create(SpacingScale.Md, body => {
                body.Add(DeityField((string)"CL_NewColony_Ideology_Editor_DeityName".Translate(),
                    TextField.Create(name.Value, value => name.Set(value), instanceKey: deity,
                        style: new Style { Width = Length.Stretch })));
                body.Add(DeityField((string)"CL_NewColony_Ideology_Editor_DeityTitle".Translate(),
                    TextField.Create(title.Value, value => title.Set(value), instanceKey: deity,
                        style: new Style { Width = Length.Stretch })));
                body.Add(DeityField((string)"CL_NewColony_Ideology_Editor_DeityGender".Translate(),
                    Segmented.Create(gender.Value, genders, g => g.GetLabel().CapitalizeFirst(), g => gender.Set(g))));
            }, style: new Style { Width = Length.Stretch }));
            root.AddFlex(Box.Create());
            root.Add(Divider.Horizontal());
            root.Add(HStack.Create(SpacingScale.Sm, f => {
                f.AddFlex(Box.Create());
                f.AddHug(Button.Create((string)"CL_NewColony_Ideology_Editor_Picker_Close".Translate(),
                    () => sub.Set(IdeoSubPicker.None), Variant.Ghost));
                f.AddHug(Button.Create((string)"CL_NewColony_Ideology_Editor_Apply".Translate(),
                    () => {
                        if (ideo != null && deity != null) {
                            IdeoDraftMutations.SetDeityFields(ideo, deity, name.Value.Trim(), title.Value.Trim(), gender.Value);
                            Bump(rev);
                        }
                        sub.Set(IdeoSubPicker.None);
                    },
                    Variant.Primary));
            }, style: new Style { Width = Length.Stretch }));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = EdgeInsets.All(new Rem(1.5f)),
        });
    }

    private static LightweaveNode DeityField(string label, LightweaveNode control) {
        return Stack.Create(SpacingScale.Xxs, s => {
            s.Add(Text.Create(label, style: new Style {
                FontFamily = FontRole.Mono, FontSize = new Rem(0.625f),
                LetterSpacing = Tracking.Of(0.18f), TextColor = ThemeSlot.TextMuted,
            }));
            s.Add(control);
        }, style: new Style { Width = Length.Stretch });
    }

    // A full-width precept row: icon, issue title, precept-choice subline, and a remove button - matching the
    // mock's "Pain / IDEALIZED" cards rather than a cramped wrap of pills.
    // A precept card sized to fill its responsive grid cell: vanilla icon, issue title, precept-choice
    // subline, remove button, and a description tooltip - matching the mock's "Pain / IDEALIZED" cards.
    

    // Mirrors vanilla IdeoUIUtility.GetIconAndLabelColor(PreceptImpact): the precept value line is
    // tinted by its issue impact - High reads as the gold accent, Medium stays primary, Low greys out.
    // Verified visually in the editor (Gauranlen=High shows gold, neutral issues grey); the mapping
    // itself isn't unit-tested because its PreceptImpact input and ThemeSlot output are RimWorld /
    // framework enums unavailable to the pure-BCL Tests assembly.
    


    private static LightweaveNode AddButton(string label, System.Action onClick) {
        return Button.Create(
            label.ToUpperInvariant(),
            onClick,
            Variant.Ghost,
            leading: Glyph.Create(Icons.Phosphor.Plus, new Style { FontSize = new Rem(0.75f) }),
            style: new Style {
                Height = new Rem(1.875f),
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.6875f),
                TextColor = ThemeSlot.SurfaceAccent,
            });
    }

    private static LightweaveNode SectionRandomize(System.Action onClick, bool disabled = false) {
        return IconButton.Create(
            Glyph.Create(Icons.Phosphor.ArrowsClockwise, new Style { FontSize = new Rem(0.8125f) }),
            onClick,
            disabled: disabled,
            tooltipKey: "CL_NewColony_Ideology_Editor_RandomizeSection");
    }

    // Lock toggle that sits next to a section title. Locked => filled lock glyph + accent ring (active),
    // and the caller disables that section's randomize + skips it in Randomize all.
    // Lock toggle that sits next to a section title. Unlocked => open padlock in muted; locked => closed
    // padlock in accent. The caller disables that section's randomize + skips it in Randomize all.
    private static LightweaveNode SectionLockToggle(SectionLock section, StateHandle<SectionLock> locks) {
        bool locked = (locks.Value & section) != 0;
        return IconButton.Create(
            Glyph.Create(locked ? Icons.Phosphor.LockSimple : Icons.Phosphor.LockSimpleOpen,
                new Style {
                    FontSize = new Rem(0.8125f),
                    TextColor = locked ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted,
                }),
            () => locks.Set(locked ? locks.Value & ~section : locks.Value | section),
            tooltipKey: locked
                ? "CL_NewColony_Ideology_Editor_SectionLockOn"
                : "CL_NewColony_Ideology_Editor_SectionLockOff");
    }

    // Bordered count pill shown on the right of the MEMES header ("2 / 10"), matching the mock. The cap is
    // a display affordance (the mock's "/ 10"), not an enforced game rule - complexity governs the real limit.
    

    private static LightweaveNode RemovableChip(Texture2D? icon, string label, string? tooltip, System.Action onRemove) {
        LightweaveNode content = HStack.Create(SpacingScale.Xs, m => {
            if (icon != null) {
                m.AddHug(Avatar.Create(string.Empty, size: new Rem(1.5f),
                    accent: ThemeSlot.SurfaceAccent, background: ThemeSlot.Glass1,
                    border: ThemeSlot.BorderFaint, texture: icon, iconScale: 0.72f, tintTexture: false,
                    radius: RadiusScale.Sm));
            }
            m.AddHug(Text.Create(label, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(0.8125f),
                LetterSpacing = Tracking.Of(0.01f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            m.AddHug(IconButton.Create(
                Glyph.Create(Icons.Phosphor.X, new Style { FontSize = new Rem(0.625f) }),
                onRemove,
                iconSize: new Rem(0.75f),
                tooltipKey: "CL_NewColony_Ideology_Editor_Remove"));
        }, align: FlexAlign.Center);

        LightweaveNode box = Box.Create(
            children: c => c.Add(content),
            style: new Style {
                Padding = new EdgeInsets(new Rem(0.25f), new Rem(0.5f), new Rem(0.25f), new Rem(0.3125f)),
                Background = BackgroundSpec.Of(ThemeSlot.Glass1),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderFaint),
            });

        return string.IsNullOrEmpty(tooltip)
            ? box
            : Tooltip.Create(box, tooltip!, TooltipSide.Top);
    }

    

    private static LightweaveNode BuildFooter(StateHandle<bool> editorOpen, StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, StateHandle<SectionLock> locks, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();

        Style footerLabel = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.6875f),
            LetterSpacing = Tracking.Of(0.12f),
            TextColor = ThemeSlot.TextSecondary,
        };

        LightweaveNode? lockHint = LockHint(locks);

        return HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Button.Create(
                ((string)"CL_NewColony_Ideology_Editor_Back".Translate()).ToUpperInvariant(),
                () => Close(editorOpen, sub), Variant.Ghost, style: footerLabel));
            h.AddHug(Button.Create(((string)"CL_NewColony_Ideology_Editor_RandomizeAll".Translate()).ToUpperInvariant(),
                () => {
                    Ideo? live = target ?? IdeoDraft.Active();
                    if (live != null) {
                        IdeoDraftMutations.RandomizeAll(live, locks.Value);
                        Bump(rev);
                    }
                }, Variant.Secondary,
                leading: Glyph.Create(Icons.Phosphor.ArrowsClockwise, new Style { FontSize = new Rem(0.75f) }),
                style: footerLabel));
            if (lockHint != null) {
                h.AddHug(lockHint);
            }
            h.AddFlex(Box.Create());
            h.AddHug(ImpactReadout(ideo));
            h.AddHug(Button.Create((string)"CL_NewColony_Ideology_Editor_Done".Translate(),
                () => Close(editorOpen, sub), Variant.Primary));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1f), new Rem(1.5f), new Rem(1.25f), new Rem(1.5f)),
        });
    }

    internal static LightweaveNode BuildStructurePicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();
        MemeDef? current = StructureMeme(ideo);

        return PickerShell(
            "CL_NewColony_Ideology_Editor_Picker_Structure_Title".Translate(),
            "CL_NewColony_Ideology_Editor_Picker_Structure_Subtitle".Translate(),
            sub,
            grid => {
                foreach (MemeDef structure in Structures()) {
                    bool selected = current == structure;
                    MemeDef captured = structure;
                    grid.Add(SelectableSurface.Create(
                        child: PickerTile(structure.Icon, structure.LabelCap, selected),
                        selected: selected,
                        variant: SelectableSurfaceVariant.Tile,
                        tooltipContent: () => Text.Create(structure.description ?? string.Empty, wrap: true),
                        onSelect: () => {
                            if (ideo != null) {
                                IdeoDraftMutations.SwapStructure(ideo, captured);
                                Bump(rev);
                            }
                            sub.Set(IdeoSubPicker.None);
                        },
                        id: "ideo_struct_" + structure.defName));
                }
            });
    }

    internal static LightweaveNode BuildMemePicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();

        return PickerShell(
            "CL_NewColony_Ideology_Editor_Picker_Meme_Title".Translate(),
            "CL_NewColony_Ideology_Editor_Picker_Meme_Subtitle".Translate(),
            sub,
            grid => {
                foreach (MemeDef meme in NormalMemes()) {
                    bool selected = ideo != null && ideo.memes.Contains(meme);
                    MemeDef captured = meme;
                    grid.Add(SelectableSurface.Create(
                        child: PickerTile(meme.Icon, meme.LabelCap, selected),
                        selected: selected,
                        variant: SelectableSurfaceVariant.Tile,
                        tooltipContent: () => Text.Create(meme.description ?? string.Empty, wrap: true),
                        onSelect: () => {
                            if (ideo != null) {
                                if (selected) {
                                    IdeoDraftMutations.RemoveMeme(ideo, captured);
                                }
                                else {
                                    IdeoDraftMutations.AddMeme(ideo, captured);
                                }
                                Bump(rev);
                            }
                        },
                        id: "ideo_meme_" + meme.defName));
                }
            });
    }

    internal static LightweaveNode BuildStylePicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();

        return PickerShell(
            "CL_NewColony_Ideology_Editor_Picker_Style_Title".Translate(),
            "CL_NewColony_Ideology_Editor_Picker_Style_Subtitle".Translate(),
            sub,
            grid => {
                foreach (StyleCategoryDef cat in DefDatabase<StyleCategoryDef>.AllDefsListForReading) {
                    bool selected = HasStyle(ideo, cat);
                    StyleCategoryDef captured = cat;
                    grid.Add(SelectableSurface.Create(
                        child: PickerTile(cat.Icon, cat.LabelCap, selected),
                        selected: selected,
                        variant: SelectableSurfaceVariant.Tile,
                        onSelect: () => {
                            if (ideo != null) {
                                IdeoDraftMutations.ToggleStyle(ideo, captured);
                                Bump(rev);
                            }
                        },
                        id: "ideo_style_" + cat.defName));
                }
            });
    }

    internal static LightweaveNode BuildPreceptPicker(StateHandle<IdeoSubPicker> sub, StateHandle<int> rev, Ideo? target = null) {
        Ideo? ideo = target ?? IdeoDraft.Active();
        HashSet<string> present = PresentMemeDefNames(ideo);

        return PickerShell(
            "CL_NewColony_Ideology_Editor_Picker_Precept_Title".Translate(),
            "CL_NewColony_Ideology_Editor_Picker_Precept_Subtitle".Translate(),
            sub,
            grid => {
                foreach (PreceptDef pd in DefDatabase<PreceptDef>.AllDefsListForReading) {
                    if (!pd.visible || pd.issue == null) {
                        continue;
                    }

                    List<string>? required = RequiredMemeDefNames(pd);
                    bool allowed = IdeoPreceptRules.IsAllowed(required, present);
                    Precept? owned = OwnedPrecept(ideo, pd);
                    bool selected = owned != null;
                    if (!allowed && !selected) {
                        continue;
                    }

                    PreceptDef capturedDef = pd;
                    Precept? capturedOwned = owned;
                    grid.Add(SelectableSurface.Create(
                        child: PickerTile(null, pd.issue.LabelCap + ": " + pd.LabelCap, selected),
                        selected: selected,
                        variant: SelectableSurfaceVariant.Tile,
                        tooltipContent: () => Text.Create(pd.description ?? string.Empty, wrap: true),
                        onSelect: () => {
                            if (ideo != null) {
                                if (capturedOwned != null) {
                                    IdeoDraftMutations.RemovePrecept(ideo, capturedOwned);
                                }
                                else {
                                    IdeoDraftMutations.AddPrecept(ideo, capturedDef);
                                }
                                Bump(rev);
                            }
                        },
                        id: "ideo_precept_" + pd.defName));
                }
            });
    }

    private static LightweaveNode PickerShell(string title, string subtitle, StateHandle<IdeoSubPicker> sub, System.Action<List<LightweaveNode>> tiles) {
        return Stack.Create(SpacingScale.Md, root => {
            root.Add(PickerHeader(
                "CL_NewColony_Ideology_Editor_Picker_Add".Translate(),
                title,
                subtitle));
            root.Add(Divider.Horizontal());
            root.AddFlex(ScrollArea.Create(
                Wrap.Create(SpacingScale.Sm, new Rem(8.5f), tiles, stretch: true, style: new Style { Width = Length.Stretch }),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }));
            root.Add(Divider.Horizontal());
            root.Add(HStack.Create(SpacingScale.Sm, f => {
                f.AddFlex(Box.Create());
                f.AddHug(Button.Create((string)"CL_NewColony_Ideology_Editor_Picker_Close".Translate(),
                    () => sub.Set(IdeoSubPicker.None), Variant.Ghost));
            }, style: new Style { Width = Length.Stretch }));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = EdgeInsets.All(new Rem(1.5f)),
        });
    }

    private static LightweaveNode PickerHeader(string eyebrow, string title, string subtitle) {
        return Stack.Create(SpacingScale.Xxs, t => {
            t.Add(Text.Create(eyebrow, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.625f),
                LetterSpacing = Tracking.Of(0.28f),
                TextColor = ThemeSlot.TextMuted,
            }));
            t.Add(Text.Create(title, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.25f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            t.Add(Text.Create(subtitle, wrap: true, style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.8125f),
                TextColor = ThemeSlot.TextSecondary,
            }));
        });
    }

    internal static LightweaveNode PickerTile(Texture2D? icon, string label, bool selected) {
        return Stack.Create(SpacingScale.Xs, t => {
            t.Add(Avatar.Create(string.Empty, size: new Rem(2.5f),
                accent: selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary,
                background: selected ? ThemeSlot.AccentSoft : ThemeSlot.Glass1,
                border: ThemeSlot.BorderFaint, texture: icon,
                icon: icon == null ? Icons.Phosphor.Sparkle : (IconRef?)null,
                iconScale: 0.66f, tintTexture: false));
            t.Add(Text.Create(label, wrap: true, style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(0.75f),
                TextColor = selected ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary,
            }));
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode EmptyLabel() {
        return Text.Create("CL_NewColony_Ideology_Editor_Empty".Translate(), style: new Style {
            FontFamily = FontRole.Body,
            FontSize = new Rem(0.8125f),
            TextColor = ThemeSlot.TextMuted,
        });
    }

    internal static List<MemeDef> Structures() {
        List<MemeDef> result = new List<MemeDef>();
        foreach (MemeDef meme in DefDatabase<MemeDef>.AllDefsListForReading) {
            if (meme.category == MemeCategory.Structure) {
                result.Add(meme);
            }
        }
        return result;
    }

    internal static List<MemeDef> NormalMemes() {
        FactionDef? playerDef = Faction.OfPlayer?.def;
        List<MemeDef> result = new List<MemeDef>();
        foreach (MemeDef meme in DefDatabase<MemeDef>.AllDefsListForReading) {
            if (meme.category != MemeCategory.Normal) {
                continue;
            }
            if (playerDef != null && !IdeoUtility.IsMemeAllowedFor(meme, playerDef)) {
                continue;
            }
            result.Add(meme);
        }
        return result;
    }

    // Sum of the ideo's non-structure meme impacts (mock memeComplexity: low=1, medium=2, high=3).
    internal static int MemeComplexity(Ideo? ideo) {
        if (ideo == null) {
            return 0;
        }
        int sum = 0;
        for (int i = 0; i < ideo.memes.Count; i++) {
            MemeDef meme = ideo.memes[i];
            if (meme.category == MemeCategory.Normal) {
                sum += meme.impact;
            }
        }
        return sum;
    }

    private static string ImpactWordKey(int complexity) {
        if (complexity <= 3) {
            return "CL_NewColony_Ideology_Editor_Impact_Low";
        }
        if (complexity <= 6) {
            return "CL_NewColony_Ideology_Editor_Impact_Medium";
        }
        return "CL_NewColony_Ideology_Editor_Impact_High";
    }

    // The "COMPLEXITY n / 10 · LABEL" readout from the mock's meme overlay footer (.nc-ideo-cx): mono
    // muted caption with a display-weight number, then the impact band word.
    internal static LightweaveNode ComplexityMeter(Ideo? ideo) {
        int complexity = MemeComplexity(ideo);
        string word = ((string)ImpactWordKey(complexity).Translate()).ToUpperInvariant();
        Style caption = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(11f / 16f),
            LetterSpacing = Tracking.Of(0.12f),
            TextColor = ThemeSlot.TextMuted,
        };
        return HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(Text.Create(((string)"CL_NewColony_Ideology_Wizard_Complexity".Translate()).ToUpperInvariant(), style: caption));
            h.AddHug(Text.Create(complexity + " / 10", style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.125f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            h.AddHug(Text.Create("· " + word, style: caption));
        }, align: FlexAlign.Center);
    }

    internal static MemeDef? StructureMeme(Ideo? ideo) {
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

    internal static int NonStructureMemeCount(Ideo? ideo) {
        if (ideo == null) {
            return 0;
        }
        int count = 0;
        foreach (MemeDef meme in ideo.memes) {
            if (meme.category != MemeCategory.Structure) {
                count++;
            }
        }
        return count;
    }

    private static HashSet<string> PresentMemeDefNames(Ideo? ideo) {
        HashSet<string> names = new HashSet<string>();
        if (ideo != null) {
            foreach (MemeDef meme in ideo.memes) {
                names.Add(meme.defName);
            }
        }
        return names;
    }

    private static List<string>? RequiredMemeDefNames(PreceptDef pd) {
        if (pd.requiredMemes == null || pd.requiredMemes.Count == 0) {
            return null;
        }
        List<string> names = new List<string>(pd.requiredMemes.Count);
        foreach (MemeDef req in pd.requiredMemes) {
            names.Add(req.defName);
        }
        return names;
    }

    private static Precept? OwnedPrecept(Ideo? ideo, PreceptDef pd) {
        if (ideo == null) {
            return null;
        }
        foreach (Precept precept in ideo.PreceptsListForReading) {
            if (precept.def == pd) {
                return precept;
            }
        }
        return null;
    }

    private static bool HasStyle(Ideo? ideo, StyleCategoryDef cat) {
        if (ideo?.thingStyleCategories == null) {
            return false;
        }
        foreach (ThingStyleCategoryWithPriority style in ideo.thingStyleCategories) {
            if (style.category == cat) {
                return true;
            }
        }
        return false;
    }

    internal static string IdeoName(Ideo? ideo) {
        return ideo == null || ideo.name.NullOrEmpty()
            ? (string)"CL_NewColony_Ideology_Editor_Unnamed".Translate()
            : ideo.name;
    }

    private static string IdentitySubline(MemeDef? structure) {
        return structure != null ? structure.LabelCap.ToString() : (string)"CL_NewColony_Ideology_Editor_NoStructure".Translate();
    }

    private static string ImpactLabel(int score) {
        IdeoImpact impact = IdeoComplexity.Label(score);
        switch (impact) {
            case IdeoImpact.Low:
                return (string)"CL_NewColony_Ideology_Editor_Impact_Low".Translate();
            case IdeoImpact.Medium:
                return (string)"CL_NewColony_Ideology_Editor_Impact_Medium".Translate();
            default:
                return (string)"CL_NewColony_Ideology_Editor_Impact_High".Translate();
        }
    }
}
