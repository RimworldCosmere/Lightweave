using System;
using System.Collections.Generic;
using System.Linq;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;
using Display = Cosmere.Lightweave.Typography.Display;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.ModsConfig;

public static class ModDetailPane {
    private static readonly Rem ThumbSize = new Rem(6f);

    public static LightweaveNode Create(ModMetaData? mod, RimWorld.Page_ModsConfig page, Action onClose) {
        if (mod == null) {
            return BuildEmpty();
        }

        int conflicts = ModConflicts.CountFor(mod);

        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.None, s => {
                s.Add(BuildHeader(mod));
                if (conflicts > 0) {
                    s.Add(BuildConflictPanel(mod));
                }
                s.AddFlex(BuildDescriptionScroll(mod));
                s.Add(BuildDependencies(mod));
                s.Add(BuildActionBar(mod, page, onClose));
            })),
            style: new Style {
                Padding = EdgeInsets.Zero,
            }
        );
    }

    private static LightweaveNode BuildDescriptionScroll(ModMetaData mod) {
        string description = mod.Description ?? string.Empty;
        return Box.Create(
            children: c => c.Add(ScrollArea.Create(content: Stack.Create(SpacingScale.Sm, s => {
                s.Add(Eyebrow.Create(
                    "CL_ModsConfig_Description".Translate(),
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.8f),
                        LetterSpacing = Tracking.Of(0.16f),
                        TextColor = ThemeSlot.TextMuted,
                    }
                ));
                if (!string.IsNullOrWhiteSpace(description)) {
                    s.Add(Text.Create(
                        description,
                        wrap: true,
                        style: new Style { TextColor = ThemeSlot.TextSecondary, FontSize = new Rem(1f) }
                    ));
                }
                else {
                    s.Add(Text.Create(
                        "CL_ModsConfig_Description_None".Translate(),
                        style: new Style { TextColor = ThemeSlot.TextMuted, FontSize = new Rem(1f) }
                    ));
                }
            }))),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Md, Right: SpacingScale.Lg, Bottom: SpacingScale.Md, Left: SpacingScale.Lg),
            }
        );
    }

    public static LightweaveNode BuildHeader(ModMetaData mod, Action<StackBuilder>? trailing = null) {
        return Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.Md, h => {
                h.Add(BuildThumbnail(mod), ThumbSize.ToPixels());
                h.AddFlex(Stack.Create(SpacingScale.Xxs, s => {
                    ModKind kind = ModKindResolver.Resolve(mod);
                    s.Add(HStack.Create(SpacingScale.None, hh => {
                        hh.AddHug(BuildKindTag(kind));
                        hh.AddFlex(Spacer.Flex());
                    }));
                    s.Add(Display.Create(
                        mod.Name ?? mod.PackageId ?? string.Empty,
                        level: 3,
                        wrap: true,
                        style: new Style {
                            TextAlign = TextAlign.Start,
                            FontWeight = UnityEngine.FontStyle.Bold,
                            LetterSpacing = Tracking.Of(0.04f),
                        }
                    ));
                    string author = mod.AuthorsString ?? string.Empty;
                    if (!string.IsNullOrEmpty(author)) {
                        bool hasVersion = !string.IsNullOrEmpty(mod.ModVersion);
                        string byline = hasVersion
                            ? "CL_ModsConfig_AuthorByline".Translate(author.Named("AUTHOR"), mod.ModVersion.Named("VERSION")).Resolve()
                            : "CL_ModsConfig_AuthorByline_NoVersion".Translate(author.Named("AUTHOR")).Resolve();
                        s.Add(Text.Create(
                            byline,
                            style: new Style {
                                FontFamily = FontRole.Mono,
                                FontSize = new Rem(0.875f),
                                TextColor = ThemeSlot.TextMuted,
                            }
                        ));
                    }
                }));
                if (trailing != null) {
                    h.AddHug(Stack.Create(SpacingScale.Xs, trailing));
                }
            })),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Lg, Right: SpacingScale.Lg, Bottom: SpacingScale.Md, Left: SpacingScale.Lg),
                Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildThumbnail(ModMetaData mod) {
        LightweaveNode node = NodeBuilder.New("ModDetailThumb:" + mod.PackageId);
        float size = ThumbSize.ToPixels();
        node.PreferredHeight = size;
        node.MeasureWidth = () => size;
        node.Paint = (rect, _) => {
            float sq = Mathf.Min(rect.width, rect.height);
            Rect r = new Rect(rect.x, rect.y, sq, sq);
            Texture2D? preview = mod.PreviewImage;
            if (preview != null) {
                PaintBox.DrawTexture(r, preview, Color.white, ScaleMode.ScaleToFit);
            }
            else {
                PaintBox.Draw(
                    r,
                    BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                    BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
                    null
                );
            }
        };
        return node;
    }

    private static LightweaveNode BuildKindTag(ModKind kind) {
        string labelKey = ModKindResolver.LabelKey(kind);
        ThemeSlot tone = ModKindResolver.Tone(kind);
        LightweaveNode node = NodeBuilder.New("ModKindTag");
        float h = new Rem(1.5f).ToPixels();
        float padX = new Rem(0.6f).ToPixels();
        Rem fontSize = new Rem(0.75f);
        node.PreferredHeight = h;
        string label = ((string)labelKey.Translate()).ToUpperInvariant();

        node.MeasureWidth = () => {
            return Mathf.Ceil(TextDraw.Measure(label, FontRole.Mono, fontSize).x + padX * 2f);
        };

        node.Paint = (rect, _) => {
            float natH = h;
            Rect chip = new Rect(rect.x, rect.y + (rect.height - natH) / 2f, rect.width, natH);
            PaintBox.Draw(chip, null, BorderSpec.All(new Rem(1f / 16f), tone), null);
            TextDraw.Draw(chip, label, FontRole.Mono, fontSize, TextAnchor.MiddleCenter, tone);
        };
        return node;
    }

    private static LightweaveNode BuildConflictPanel(ModMetaData mod) {
        LightweaveNode panel = Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.Sm, s => {
                s.Add(Eyebrow.Create(
                    "CL_ModsConfig_Conflict_Eyebrow".Translate(),
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.75f),
                        LetterSpacing = Tracking.Of(0.16f),
                        TextColor = ThemeSlot.StatusDanger,
                    }
                ));
                s.Add(Text.Create(
                    "CL_ModsConfig_Conflict_Body".Translate(),
                    wrap: true,
                    style: new Style {
                        TextColor = ThemeSlot.TextPrimary,
                    }
                ));
                s.Add(Button.Create(
                    label: "CL_ModsConfig_Conflict_AutoResolve".Translate(),
                    onClick: () => AutoResolve(mod),
                    variant: Variant.Secondary
                ));
            })),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Md),
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceTranslucent),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.StatusDanger),
            }
        );

        return Box.Create(
            children: c => c.Add(panel),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Sm, Right: SpacingScale.Lg, Left: SpacingScale.Lg),
            }
        );
    }

    private static LightweaveNode BuildDependencies(ModMetaData mod) {
        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.Xs, s => {
                s.Add(Eyebrow.Create(
                    "CL_ModsConfig_Dependencies".Translate(),
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.8f),
                        LetterSpacing = Tracking.Of(0.16f),
                        TextColor = ThemeSlot.TextMuted,
                    }
                ));
                bool hasAny = mod.Dependencies != null && mod.Dependencies.Count > 0;
                if (hasAny) {
                    foreach (ModRequirement req in mod.Dependencies!) {
                        s.Add(BuildDepRow(req));
                    }
                }
                else {
                    s.Add(Text.Create(
                        "CL_ModsConfig_Dep_None".Translate().Resolve(),
                        style: new Style {
                            FontSize = new Rem(0.875f),
                            TextColor = ThemeSlot.TextMuted,
                        }
                    ));
                }
            })),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Md, Right: SpacingScale.Lg, Bottom: SpacingScale.Md, Left: SpacingScale.Lg),
                Border = new BorderSpec(Top: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildDepRow(ModRequirement req) {
        LightweaveNode node = NodeBuilder.New("DepRow:" + (req.packageId ?? req.displayName ?? "?"));
        node.PreferredHeight = new Rem(1.75f).ToPixels();
        node.Paint = (rect, _) => {
            PaintBox.Draw(
                rect,
                null,
                new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                null
            );

            string name = req.displayName ?? req.packageId ?? string.Empty;
            TextDraw.Draw(rect, name, FontRole.Body, new Rem(0.825f), TextAnchor.MiddleLeft, ThemeSlot.TextSecondary);

            bool installed = req.IsSatisfied;
            string status = installed
                ? (string)"CL_ModsConfig_Dep_Installed".Translate()
                : (string)"CL_ModsConfig_Dep_Missing".Translate();
            ThemeSlot slot = installed ? ThemeSlot.StatusSuccess : ThemeSlot.StatusDanger;
            string dot = installed ? "● " : "▲ ";
            TextDraw.Draw(rect, dot + status, FontRole.Mono, new Rem(0.65f), TextAnchor.MiddleRight, slot);
        };
        return node;
    }

    private static LightweaveNode BuildActionBar(ModMetaData mod, RimWorld.Page_ModsConfig page, Action onClose) {
        bool active = mod.Active;
        bool toggleLocked = ModKindResolver.IsLocked(mod);
        bool isWorkshopMod = mod.OnSteamWorkshop && SteamManager.Initialized;
        bool canUninstall = !toggleLocked && !mod.Official;
        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.Xs, s => {
                s.Add(Eyebrow.Create(
                    "CL_ModsConfig_Actions".Translate(),
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.8f),
                        LetterSpacing = Tracking.Of(0.16f),
                        TextColor = ThemeSlot.TextMuted,
                    }
                ));
                string enableLabel = active
                    ? ((string)"CL_ModsConfig_Action_Disable".Translate()).ToUpperInvariant()
                    : ((string)"CL_ModsConfig_Action_Enable".Translate()).ToUpperInvariant();
                s.Add(Button.Create(
                    label: enableLabel,
                    onClick: () => Verse.ModsConfig.SetActive(mod, !active),
                    variant: Variant.Secondary,
                    disabled: toggleLocked,
                    style: new Style { Width = Length.Stretch }
                ));
                if (isWorkshopMod) {
                    s.Add(Button.Create(
                        label: ((string)"CL_ModsConfig_Action_Workshop".Translate()).ToUpperInvariant(),
                        onClick: () => SteamUtility.OpenWorkshopPage(new Steamworks.PublishedFileId_t(mod.GetPublishedFileId().m_PublishedFileId)),
                        variant: Variant.Secondary,
                        style: new Style { Width = Length.Stretch }
                    ));
                }
                else if (!string.IsNullOrEmpty(mod.Url)) {
                    s.Add(Button.Create(
                        label: ((string)"CL_ModsConfig_Action_Url".Translate()).ToUpperInvariant(),
                        onClick: () => Application.OpenURL(mod.Url),
                        variant: Variant.Secondary,
                        style: new Style { Width = Length.Stretch }
                    ));
                }
                if (canUninstall) {
                    string modName = mod.Name ?? mod.PackageId ?? string.Empty;
                    string removalLabel = isWorkshopMod
                        ? ((string)"CL_ModsConfig_Action_Unsubscribe".Translate()).ToUpperInvariant()
                        : ((string)"CL_ModsConfig_Action_Uninstall".Translate()).ToUpperInvariant();
                    s.Add(Button.Create(
                        label: removalLabel,
                        onClick: () => ConfirmRemoval(mod, isWorkshopMod),
                        variant: Variant.Secondary,
                        style: new Style { Width = Length.Stretch, TextColor = ThemeSlot.StatusDanger }
                    ));
                }
            })),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Md, Right: SpacingScale.Lg, Bottom: SpacingScale.Md, Left: SpacingScale.Lg),
                Border = new BorderSpec(Top: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static void AutoResolve(ModMetaData mod) {
        Verse.ModsConfig.TrySortMods();
    }

    


    private static void ConfirmRemoval(ModMetaData mod, bool isWorkshopMod) {
        string modName = mod.Name ?? mod.PackageId ?? string.Empty;
        string promptKey = isWorkshopMod
            ? "CL_ModsConfig_Confirm_Unsubscribe"
            : "CL_ModsConfig_Confirm_Uninstall";
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
            promptKey.Translate(modName.Named("MOD")),
            () => RemoveMod(mod, isWorkshopMod),
            destructive: true
        ));
    }

    private static void RemoveMod(ModMetaData mod, bool isWorkshopMod) {
        mod.enabled = false;
        if (isWorkshopMod) {
            System.Reflection.MethodInfo? unsubscribe = typeof(Workshop).GetMethod(
                "Unsubscribe",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(WorkshopUploadable) },
                null
            );
            unsubscribe?.Invoke(null, new object[] { mod });
            return;
        }
        System.Reflection.MethodInfo? deleteContent = typeof(ModMetaData).GetMethod(
            "DeleteContent",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        deleteContent?.Invoke(mod, null);
    }

    private static LightweaveNode BuildEmpty() {
        return Container.Create(
            child: Stack.Create(SpacingScale.Sm, s => {
                s.Add(Eyebrow.Create("CL_ModsConfig_Empty_Eyebrow".Translate()));
                s.Add(Text.Create(
                    "CL_ModsConfig_Empty_Body".Translate(),
                    wrap: true,
                    style: new Style { TextColor = ThemeSlot.TextSecondary }
                ));
            }),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Lg),
            }
        );
    }
}
