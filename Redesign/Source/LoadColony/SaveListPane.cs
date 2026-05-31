using System;
using System.Collections.Generic;
using System.IO;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Redesign.MainMenu;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Typography.Typography;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Data;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.LoadColony;

public static class SaveListPane {
    private static readonly Rem RowHeight = new Rem(5.0f);

    public static LightweaveNode Create(
        List<SaveFileInfo> files,
        string? selected,
        Action<string> onSelect,
        string filter,
        Action<string> onFilterChange
    ) {
        IReadOnlyList<string> filterValues = new[] { "all", "manual", "auto" };

        int autoCount = 0;
        for (int i = 0; i < files.Count; i++) {
            if (IsAutosave(files[i].FileName)) {
                autoCount++;
            }
        }
        string allCountStr = files.Count.ToString();
        string manualCountStr = (files.Count - autoCount).ToString();
        string autoCountStr = autoCount.ToString();

        string LabelFor(string f) {
            return f switch {
                "manual" => ((string)"CL_LoadColony_Filter_Manual".Translate()).ToUpperInvariant(),
                "auto" => ((string)"CL_LoadColony_Filter_Auto".Translate()).ToUpperInvariant(),
                _ => ((string)"CL_LoadColony_Filter_All".Translate()).ToUpperInvariant(),
            };
        }

        string CountFor(string f) {
            return f switch {
                "manual" => manualCountStr,
                "auto" => autoCountStr,
                _ => allCountStr,
            };
        }

        LightweaveNode filterRow = Box.Create(
            children: c => c.Add(Segmented.Create<string>(
                value: filter,
                items: filterValues,
                labelFn: LabelFor,
                onChange: onFilterChange,
                countFn: CountFor,
                bordered: false,
                style: new Style { Width = Length.Stretch, Height = Length.Rem(2.33f) }
            )),
            style: new Style {
                Border = new BorderSpec(Bottom: new Rem(0.0625f), Color: ThemeSlot.BorderSubtle),
            }
        );

        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.None, s => {
                s.Add(filterRow);
                s.AddFlex(ScrollArea.Create(
                    content: BuildList(files, selected, onSelect)
                ));
            })),
            style: new Style {
                Padding = EdgeInsets.Zero,
                Border = new BorderSpec(Right: new Rem(0.0625f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildList(
        List<SaveFileInfo> files,
        string? selected,
        Action<string> onSelect
    ) {
        return Stack.Create(SpacingScale.None, s => {
            if (files == null || files.Count == 0) {
                s.Add(BuildEmptyState());
                return;
            }
            for (int i = 0; i < files.Count; i++) {
                SaveFileInfo file = files[i];
                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                bool isSelected = string.Equals(fileName, selected, StringComparison.OrdinalIgnoreCase);
                s.Add(BuildRow(file, fileName, isSelected, () => onSelect(fileName)));
                if (i < files.Count - 1) {
                    s.Add(Divider.Horizontal());
                }
            }
        });
    }

    private static LightweaveNode BuildRow(SaveFileInfo file, string fileName, bool isSelected, Action onClick) {
        bool isAuto = IsAutosave(fileName);
        LightweaveNode node = NodeBuilder.New("SaveListRow:" + fileName);
        node.PreferredHeight = RowHeight.ToPixels();
        node.Paint = (rect, _) => {
            InteractionState state = InteractionState.Resolve(rect, null, false);

            if (isSelected) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.ActiveTint), null, null);
            }
            else if (state.Hovered) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.HoverTint), null, null);
            }

            if (isSelected) {
                Rect stripe = new Rect(rect.x, rect.y, Spacing.StripeWidth.ToPixels(), rect.height);
                PaintBox.Draw(stripe, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
            }

            float padX = SpacingScale.Md.ToPixels();
            float padY = SpacingScale.Sm.ToPixels();
            Rect content = new Rect(rect.x + padX, rect.y + padY, rect.width - padX * 2f, rect.height - padY * 2f);

            SaveStatusInspector.SaveStatus status = SaveStatusInspector.Inspect(file);
            string display = status.DisplayName;
            string detail = ResolveDetail(status);

            float chipReserve = 0f;
            if (isAuto) {
                LightweaveNode autoChip = Chip.Create(
                    (string)"CL_LoadColony_AutoChip".Translate(),
                    variant: ChipVariant.None,
                    state: false,
                    showDot: false
                );
                float chipW = autoChip.MeasureWidth?.Invoke() ?? new Rem(3f).ToPixels();
                float chipH = autoChip.PreferredHeight ?? new Rem(1.25f).ToPixels();
                Rect chipRect = new Rect(
                    content.xMax - chipW,
                    content.y + (content.height - chipH) * 0.5f,
                    chipW,
                    chipH
                );
                autoChip.Layout?.Invoke(chipRect);
                autoChip.Draw?.Invoke(chipRect);
                chipReserve = chipW + SpacingScale.Sm.ToPixels();
            }

            float labelWidth = Mathf.Max(0f, content.width - chipReserve);

            Rem titleSize = new Rem(1.175f);
            int titlePx = Mathf.RoundToInt(titleSize.ToFontPx());
            Rem detailSize = new Rem(0.7625f);
            int detailPx = Mathf.RoundToInt(detailSize.ToFontPx());

            float titleCursor = content.x;
            if (isSelected) {
                float starW = TextDraw.Measure("★", FontRole.Display, titleSize).x;
                Rect starRect = new Rect(titleCursor, content.y, starW + 2f, titlePx + 6f);
                TextDraw.Draw(starRect, "★", FontRole.Display, titleSize, TextAnchor.UpperLeft, ThemeSlot.SurfaceAccent, FontStyle.Normal, TextClipping.Clip);
                titleCursor += starW + new Rem(0.4f).ToPixels();
            }
            Rect titleRect = new Rect(titleCursor, content.y, Mathf.Max(0f, content.xMax - chipReserve - titleCursor), titlePx + 6f);
            TextDraw.Draw(titleRect, display, FontRole.Display, titleSize, TextAnchor.UpperLeft, ThemeSlot.TextPrimary, FontStyle.Normal, TextClipping.Clip);

            Rect detailRect = new Rect(content.x, titleRect.yMax + 2f, labelWidth, detailPx + 4f);
            TextDraw.Draw(detailRect, detail, FontRole.Body, detailSize, TextAnchor.UpperLeft, ThemeSlot.TextMuted, FontStyle.Normal, TextClipping.Clip);

            if (status.ModMatch == SaveStatusInspector.ModMatchKind.Mismatch) {
                int count = status.MissingModNames.Count;
                if (count > 0) {
                    string warn = "CL_LoadColony_Status_ModsMissing".Translate(count.Named("COUNT"));
                    Rect warnRect = new Rect(content.x, detailRect.yMax + 2f, labelWidth, detailPx + 4f);
                    TextDraw.Draw(warnRect, warn, FontRole.Body, detailSize, TextAnchor.UpperLeft, ThemeSlot.StatusWarning, FontStyle.Normal, TextClipping.Clip);
                }
            }

            InteractionFeedback.Apply(rect, true, true);

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                onClick?.Invoke();
                e.Use();
            }
        };
        return node;
    }

    private static bool IsAutosave(string fileName) {
        return !string.IsNullOrEmpty(fileName)
            && fileName.StartsWith("Autosave", StringComparison.OrdinalIgnoreCase);
    }

    private static LightweaveNode BuildEmptyState() {
        return Container.Create(
            child: Stack.Create(SpacingScale.Xs, s => {
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

    private static string ResolveDetail(SaveStatusInspector.SaveStatus status) {
        SaveSidecarData? sc = status.Sidecar;
        List<string> parts = new List<string>(3);
        if (sc != null) {
            if (sc.DaysSurvived > 0) {
                parts.Add("CL_LoadColony_DayShort".Translate(sc.DaysSurvived.Named("DAY")).Resolve());
            }
            else if (!string.IsNullOrEmpty(sc.Quadrum) && sc.InGameYear > 0) {
                parts.Add(sc.Quadrum + " " + sc.InGameYear);
            }
            if (sc.ColonistCount > 0) {
                parts.Add("CL_LoadColony_ColonistsShort".Translate(sc.ColonistCount.Named("COUNT")).Resolve());
            }
        }
        parts.Add(SaveMetadata.FormatRelative(status.LastWriteTime));
        return Verse.ColoredText.StripTags(string.Join("  ·  ", parts));
    }
}
