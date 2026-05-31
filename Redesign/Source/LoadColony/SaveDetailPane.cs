using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Redesign.MainMenu;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using RimWorld;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Typography.Typography;
using Cosmere.Lightweave.Surfaces;
using Display = Cosmere.Lightweave.Typography.Display;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.LoadColony;

public static class SaveDetailPane {
    public static LightweaveNode Create(
        SaveFileInfo? file,
        SaveStatusInspector.SaveStatus? status,
        Action onClose,
        Action onAfterDelete
    ) {
        if (file == null || status == null) {
            return BuildEmpty();
        }

        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.Md, s => {
                s.AddFlex(ScrollArea.Create(
                    content: BuildBody(status)
                ));
                s.Add(BuildActionBar(file, onClose, onAfterDelete));
            })),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Lg),
            }
        );
    }

    private static LightweaveNode BuildBody(SaveStatusInspector.SaveStatus status) {
        return Stack.Create(SpacingScale.Lg, s => {
            s.Add(HStack.Create(SpacingScale.Lg, h => {
                h.Add(BuildPreview(status), new Rem(13.75f).ToPixels());
                h.AddFlex(Stack.Create(SpacingScale.Xs, t => {
                    t.Add(BuildStatusEyebrow(status));
                    t.Add(Display.Create(status.DisplayName, style: new Style { TextAlign = TextAlign.Start, FontSize = new Rem(3.5f) }, level: 1));
                    t.Add(Text.Create(BuildSubtitle(status), style: new Style { TextColor = ThemeSlot.TextMuted, FontSize = new Rem(0.95f) }));
                    t.Add(Text.Create(BuildStory(status), style: new Style { TextColor = ThemeSlot.TextSecondary, FontSize = new Rem(0.95f) }));
                }));
            }));
            s.Add(BuildStatGrid(status));
            s.Add(BuildConditionsSection(status));
            s.Add(BuildBadgeRow(status));
        });
    }

    private static string BuildStory(SaveStatusInspector.SaveStatus status) {
        if (status.Sidecar == null) {
            return string.Empty;
        }
        List<string> parts = new List<string>();
        if (!string.IsNullOrEmpty(status.Sidecar.ColonyName)) {
            parts.Add(status.Sidecar.ColonyName + ".");
        }
        if (status.Sidecar.ColonistCount > 0) {
            parts.Add(status.Sidecar.ColonistCount + (status.Sidecar.ColonistCount == 1 ? " colonist" : " colonists") + ".");
        }
        if (status.Sidecar.AnimalCount > 0) {
            parts.Add(status.Sidecar.AnimalCount + (status.Sidecar.AnimalCount == 1 ? " animal" : " animals") + ".");
        }
        if (status.Sidecar.Permadeath) {
            parts.Add("Permadeath.");
        }
        if (status.Sidecar.MoodAveragePercent > 0) {
            parts.Add("Mood " + status.Sidecar.MoodAveragePercent + "%.");
        }
        return string.Join(" ", parts);
    }

    private static LightweaveNode BuildConditionsSection(SaveStatusInspector.SaveStatus status) {
        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(Eyebrow.Create(
                "CL_LoadColony_Conditions".Translate(),
                style: new Style { TextColor = ThemeSlot.TextMuted, TextAlign = TextAlign.Start, LetterSpacing = Tracking.Widest }
            ));
            s.Add(HStack.Create(SpacingScale.Sm, h => {
                bool any = false;

                if (status.Sidecar != null) {
                    string[] threats = string.IsNullOrEmpty(status.Sidecar.ActiveThreat)
                        ? Array.Empty<string>()
                        : status.Sidecar.ActiveThreat.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string threat in threats) {
                        h.AddHug(BuildConditionTag(threat, ThemeSlot.StatusDanger));
                        any = true;
                    }

                    if (status.Sidecar.ThreatScale > 1.5f) {
                        h.AddHug(BuildConditionTag("Heightened threat", ThemeSlot.StatusWarning));
                        any = true;
                    }

                    if (status.Sidecar.Permadeath) {
                        h.AddHug(BuildConditionTag("Permadeath", ThemeSlot.StatusDanger));
                        any = true;
                    }
                }

                if (!any) {
                    h.AddHug(BuildConditionTag("CL_LoadColony_Conditions_None".Translate(), ThemeSlot.TextMuted));
                }

                h.AddFlex(Spacer.Flex());
            }));
        });
    }

    private static LightweaveNode BuildConditionTag(string label, ThemeSlot tone) {
        return Chip.Create(label, variant: SlotToVariant(tone), state: true, showDot: true);
    }

    private static ChipVariant SlotToVariant(ThemeSlot slot) {
        switch (slot) {
            case ThemeSlot.StatusDanger: return ChipVariant.Error;
            case ThemeSlot.StatusWarning: return ChipVariant.Warn;
            case ThemeSlot.StatusSuccess: return ChipVariant.Info;
            case ThemeSlot.StatusInfo: return ChipVariant.Debug;
            default: return ChipVariant.None;
        }
    }

    private static LightweaveNode BuildPreview(SaveStatusInspector.SaveStatus status) {
        LightweaveNode node = NodeBuilder.New("SavePreview");
        node.PreferredHeight = new Rem(8.75f).ToPixels();
        node.Paint = (rect, _) => {
            PaintBox.Draw(
                rect,
                BackgroundSpec.Of(ThemeSlot.MapPreviewTint),
                BorderSpec.All(new Rem(0.0625f), ThemeSlot.BorderSubtle),
                RadiusSpec.All(RadiusScale.Sm)
            );

            string cacheKey = status.FileName + "|" + status.LastWriteTime.Ticks.ToString();
            Texture2D? thumb = MainMenu.ColonyScreenshotCache.GetOrLoad(cacheKey, status.Sidecar);
            if (thumb != null) {
                Rect inner = rect.ContractedBy(new Rem(0.0625f).ToPixels());
                PaintBox.DrawTexture(inner, thumb, Color.white, ScaleMode.ScaleAndCrop);
            }
        };
        return node;
    }

    

    

    

    

    private static LightweaveNode BuildStatusEyebrow(SaveStatusInspector.SaveStatus status) {
        string label;
        ThemeSlot tone;
        if (status.Compatibility == SaveStatusInspector.SaveCompatibility.Incompatible) {
            label = "CL_LoadColony_Status_Incompatible".Translate();
            tone = ThemeSlot.StatusDanger;
        }
        else if (status.ModMatch == SaveStatusInspector.ModMatchKind.Mismatch) {
            label = "CL_LoadColony_Status_ModsChanged".Translate();
            tone = ThemeSlot.StatusWarning;
        }
        else if (status.Compatibility == SaveStatusInspector.SaveCompatibility.Match) {
            label = "CL_LoadColony_Status_Ready".Translate();
            tone = ThemeSlot.StatusSuccess;
        }
        else {
            label = "CL_LoadColony_Status_Differs".Translate();
            tone = ThemeSlot.StatusWarning;
        }

        int? day = status.Sidecar?.DaysSurvived;
        string? dayLabel = day.HasValue && day.Value > 0
            ? ((string)"CL_LoadColony_DayShort".Translate(day.Value.Named("DAY"))).ToUpperInvariant()
            : null;

        return HStack.Create(SpacingScale.Sm, h => {
            if (dayLabel != null) {
                h.AddHug(Eyebrow.Create(dayLabel, style: new Style {
                    TextColor = ThemeSlot.TextMuted,
                    LetterSpacing = Tracking.Wide,
                }));
            }
            h.AddHug(Chip.Create(label, variant: SlotToVariant(tone), state: true, showDot: false));
            h.AddFlex(Spacer.Flex());
        });
    }

    private static string BuildSubtitle(SaveStatusInspector.SaveStatus status) {
        List<string> parts = new List<string>();
        if (status.Sidecar != null) {
            if (status.Sidecar.DaysSurvived > 0) {
                parts.Add("CL_LoadColony_DayShort".Translate(status.Sidecar.DaysSurvived.Named("DAY")).Resolve());
            }
            if (!string.IsNullOrEmpty(status.Sidecar.Quadrum)) {
                parts.Add(status.Sidecar.Quadrum);
            }
            if (status.Sidecar.InGameYear > 0) {
                parts.Add(status.Sidecar.InGameYear.ToString(CultureInfo.InvariantCulture));
            }
        }
        parts.Add(SaveMetadata.FormatRelative(status.LastWriteTime));
        return string.Join("  ·  ", parts);
    }

    private static List<KeyValueRow> BuildStatRows(SaveStatusInspector.SaveStatus status) {
        return new List<KeyValueRow> {
            new KeyValueRow(
                "CL_LoadColony_Stat_Biome".Translate(),
                status.Sidecar?.Biome ?? "—"
            ),
            new KeyValueRow(
                "CL_LoadColony_Stat_Climate".Translate(),
                status.Sidecar?.Climate ?? "—"
            ),
            new KeyValueRow(
                "CL_LoadColony_Stat_Colonists".Translate(),
                status.Sidecar != null ? status.Sidecar.ColonistCount.ToString(CultureInfo.InvariantCulture) : "—"
            ),
            new KeyValueRow(
                "CL_LoadColony_Stat_Wealth".Translate(),
                FormatWealth(status.Sidecar?.Wealth)
            ),
        };
    }

    private static LightweaveNode BuildStatGrid(SaveStatusInspector.SaveStatus status) {
        List<KeyValueRow> rows = BuildStatRows(status);
        return HStack.Create(SpacingScale.None, h => {
            for (int i = 0; i < rows.Count; i++) {
                KeyValueRow row = rows[i];
                bool isFirst = i == 0;
                h.AddFlex(BuildStatCell(row.Label, row.Value, isFirst));
            }
        });
    }

    private static LightweaveNode BuildStatCell(string label, string value, bool isFirst = false) {
        Rem hairline = new Rem(0.0625f);
        Rem leftBorder = isFirst ? hairline : new Rem(0f);
        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.Sm, s => {
                s.Add(Eyebrow.Create(label, style: new Style { TextColor = ThemeSlot.MetadataLabel, FontSize = new Rem(0.7f), LetterSpacing = Tracking.Wide }));
                s.Add(Text.Create(
                    string.IsNullOrEmpty(value) ? "—" : value,
                    style: new Style {
                        FontFamily = FontRole.Display,
                        FontSize = new Rem(1.25f),
                        TextColor = ThemeSlot.TextPrimary,
                    }
                ));
            })),
            style: new Style {
                Padding = EdgeInsets.All(new Rem(1f)),
                Border = new BorderSpec(Top: hairline, Right: hairline, Bottom: hairline, Left: leftBorder, Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    

    private static string FormatWealth(float? wealth) {
        if (!wealth.HasValue || wealth.Value <= 0f) {
            return "—";
        }
        return wealth.Value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static LightweaveNode BuildBadgeRow(SaveStatusInspector.SaveStatus status) {
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(BuildConditionTag(BuildVersionLabel(status), VersionTone(status)));
            h.AddHug(BuildConditionTag(BuildModLabel(status), ModTone(status)));
            h.AddFlex(Spacer.Flex());
        });
    }

    

    

    private static string BuildVersionLabel(SaveStatusInspector.SaveStatus status) {
        if (status.Compatibility == SaveStatusInspector.SaveCompatibility.Match) {
            return "CL_LoadColony_Pill_VersionMatch".Translate();
        }
        if (string.IsNullOrEmpty(status.GameVersion)) {
            return "CL_LoadColony_Pill_VersionUnknown".Translate();
        }
        return "CL_LoadColony_Pill_VersionShort".Translate(status.GameVersion.Named("VER")).Resolve();
    }

    private static ThemeSlot VersionTone(SaveStatusInspector.SaveStatus status) {
        return status.Compatibility switch {
            SaveStatusInspector.SaveCompatibility.Match => ThemeSlot.StatusSuccess,
            SaveStatusInspector.SaveCompatibility.DifferentBuild => ThemeSlot.StatusWarning,
            SaveStatusInspector.SaveCompatibility.DifferentVersion => ThemeSlot.StatusWarning,
            SaveStatusInspector.SaveCompatibility.Incompatible => ThemeSlot.StatusDanger,
            _ => ThemeSlot.TextMuted,
        };
    }

    private static string BuildModLabel(SaveStatusInspector.SaveStatus status) {
        if (status.ModMatch == SaveStatusInspector.ModMatchKind.Match) {
            return "CL_LoadColony_Pill_ModsReady".Translate();
        }
        int missing = status.MissingModNames.Count;
        return "CL_LoadColony_Pill_ModsChanged".Translate(missing.Named("COUNT")).Resolve();
    }

    private static ThemeSlot ModTone(SaveStatusInspector.SaveStatus status) {
        return status.ModMatch switch {
            SaveStatusInspector.ModMatchKind.Match => ThemeSlot.StatusSuccess,
            SaveStatusInspector.ModMatchKind.Mismatch => ThemeSlot.StatusWarning,
            _ => ThemeSlot.TextMuted,
        };
    }

    

    private static LightweaveNode BuildActionBar(SaveFileInfo file, Action onClose, Action onAfterDelete) {
        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
        return Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(BuildLoadButton(fileName, onClose));
                h.Add(BuildSecondaryButton(
                    label: "CL_LoadColony_Rename".Translate(),
                    onClick: () => RenameSave(file, onAfterDelete),
                    textColor: null
                ), new Rem(8f).ToPixels());
                h.Add(BuildSecondaryButton(
                    label: "CL_LoadColony_Duplicate".Translate(),
                    onClick: () => DuplicateSave(file, onAfterDelete),
                    textColor: null
                ), new Rem(8f).ToPixels());
                h.Add(BuildSecondaryButton(
                    label: "CL_LoadColony_Delete".Translate(),
                    onClick: () => ConfirmDelete(file, onAfterDelete),
                    textColor: ThemeSlot.StatusDanger
                ), new Rem(8f).ToPixels());
            }, style: new Style { Height = new Rem(2.75f) })),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Md),
                Border = new BorderSpec(Top: new Rem(0.0625f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildSecondaryButton(string label, Action onClick, ThemeSlot? textColor) {
        Style style = new Style {
            Width = Length.Stretch,
            Height = new Rem(2.75f),
            LetterSpacing = Tracking.Wide,
        };
        if (textColor.HasValue) {
            style = style with { TextColor = new ColorRef.Token(textColor.Value) };
        }
        return Button.Create(
            label: label.ToUpperInvariant(),
            onClick: onClick,
            variant: Variant.Secondary,
            style: style
        );
    }

    private static LightweaveNode BuildLoadButton(string fileName, Action? onClose) {
        return Button.Create(
            label: ((string)"CL_LoadColony_Load".Translate()).ToUpperInvariant(),
            onClick: () => {
                onClose?.Invoke();
                GameDataSaveLoader.CheckVersionAndLoadGame(fileName);
            },
            variant: Variant.Primary,
            leading: Glyph.Create(
                Icons.Phosphor.Play,
                style: new Style { TextColor = ThemeSlot.TextOnAccent }
            ),
            style: new Style {
                Width = Length.Stretch,
                Height = new Rem(2.75f),
                LetterSpacing = Tracking.Widest,
            }
        );
    }

    private static void RenameSave(SaveFileInfo file, Action onAfter) {
        string current = Path.GetFileNameWithoutExtension(file.FileName);
        Find.WindowStack.Add(new Dialog_RenameSaveFile(file, current, onAfter));
    }

    private static void DuplicateSave(SaveFileInfo file, Action onAfter) {
        try {
            string dir = file.FileInfo.DirectoryName ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(file.FileName);
            string ext = file.FileInfo.Extension;
            string candidate = Path.Combine(dir, baseName + "_copy" + ext);
            int n = 2;
            while (File.Exists(candidate)) {
                candidate = Path.Combine(dir, baseName + "_copy" + n + ext);
                n++;
            }
            File.Copy(file.FileInfo.FullName, candidate);
            onAfter?.Invoke();
        }
        catch (System.Exception ex) {
            RedesignLog.Warning($"Duplicate save failed: {ex.Message}");
        }
    }

private static void ConfirmDelete(SaveFileInfo file, Action onAfterDelete) {
        string display = Path.GetFileNameWithoutExtension(file.FileName);
        Find.WindowStack.Add(new Dialog_DeleteSaveConfirm(file, display, () => {
            try {
                file.FileInfo.Delete();
                SaveSidecar.Delete(file.FileInfo.FullName);
                SaveStatusInspector.Invalidate(file.FileInfo.FullName);
                onAfterDelete?.Invoke();
            }
            catch (Exception ex) {
                RedesignLog.Error("delete failed: " + ex);
            }
        }));
    }

    private static LightweaveNode BuildEmpty() {
        return Container.Create(
            child: Stack.Create(SpacingScale.Sm, s => {
                s.Add(Eyebrow.Create("CL_LoadColony_Empty_Eyebrow".Translate()));
                s.Add(Text.Create(
                    "CL_LoadColony_Empty_Body".Translate(),
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
