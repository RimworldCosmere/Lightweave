using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using static Cosmere.Lightweave.Typography.Typography;

namespace Cosmere.Lightweave.Layout;

[Doc(
    Id = "setting-row",
    Summary = "A single settings row: label + description + optional EDITED badge on the left, control in the middle, default value on the right, with a bottom divider.",
    WhenToUse = "Inside a settings form section to render one tweakable preference per row in a consistent grid.",
    SourcePath = "Lightweave/Layout/SettingRow.cs"
)]
public static class SettingRow {
    public static LightweaveNode Create(
        [DocParam("Primary label for the setting.")]
        string label,
        [DocParam("Control element rendered in the middle column (Dropdown, Checkbox, Slider, etc).")]
        LightweaveNode control,
        [DocParam("Optional secondary description rendered beneath the label.")]
        string? description = null,
        [DocParam("Optional default value rendered right-aligned (e.g. \"default: 100%\").")]
        string? defaultValue = null,
        [DocParam("If true, an EDITED chip is rendered inline with the label.")]
        bool edited = false,
        [DocParam("Optional override for the EDITED chip text.")]
        string? editedLabel = null,
        [DocParam("Width of the middle control column in rems. Defaults to 16rem.")]
        float controlColumnRem = 16f,
        [DocParam("Width of the right default-value column in rems. Defaults to 10rem.")]
        float defaultColumnRem = 10f,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string[]? mergedClasses = StyleExtensions.PrependClass("setting-row", classes);

        Style rowStyle = new Style {
            Padding = new EdgeInsets(Top: SpacingScale.Lg, Bottom: SpacingScale.Lg),
            Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
        };
        Style finalStyle = style.HasValue ? Style.Merge(rowStyle, style.Value) : rowStyle;

        return Box.Create(
            children: kids => {
                kids.Add(HStack.Create(
                    gap: SpacingScale.Lg,
                    children: row => {
                        row.AddFlex(BuildLeftCell(label, description, edited, editedLabel));
                        row.Add(BuildControlCell(control), new Rem(controlColumnRem).ToPixels());
                        row.Add(BuildDefaultCell(defaultValue), new Rem(defaultColumnRem).ToPixels());
                    }
                ));
            },
            style: finalStyle,
            classes: mergedClasses,
            id: id,
            line: line,
            file: file
        );
    }

    private static LightweaveNode BuildLeftCell(string label, string? description, bool edited, string? editedLabel) {
        return Stack.Create(SpacingScale.Xxs, col => {
            col.Add(HStack.Create(SpacingScale.Sm, row => {
                row.AddHug(Text.Create(
                    label,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextPrimary }
                ));
                if (edited) {
                    row.AddHug(Chip.Create(
                        label: editedLabel ?? "EDITED",
                        on: true,
                        tone: ChipTone.Warn,
                        showDot: false
                    ));
                }
            }));
            if (!string.IsNullOrEmpty(description)) {
                col.Add(Text.Create(
                    description!,
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.8125f), TextColor = ThemeSlot.TextSecondary }
                ));
            }
        });
    }

    private static LightweaveNode BuildControlCell(LightweaveNode control) {
        return Stack.Create(SpacingScale.None, col => {
            col.Add(control);
        });
    }

    private static LightweaveNode BuildDefaultCell(string? defaultValue) {
        if (string.IsNullOrEmpty(defaultValue)) {
            return Stack.Create(SpacingScale.None, _ => { });
        }
        return Text.Create(
            defaultValue!,
            style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.8125f),
                TextColor = ThemeSlot.TextMuted,
                TextAlign = TextAlign.End,
            }
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => SettingRow.Create(
            label: "Reduce motion",
            control: Checkbox.Create("Enabled", false, _ => { }),
            description: "Disable transitions and tween animations across the shell.",
            defaultValue: "default: off"
        ));
    }

    [DocVariant("with-edited")]
    public static DocSample DocsWithEdited() {
        return new DocSample(() => SettingRow.Create(
            label: "Theme",
            control: Text.Create("Cosmere"),
            description: "Visual identity of the shell.",
            defaultValue: "default: Roshar",
            edited: true
        ));
    }

    [DocVariant("no-default")]
    public static DocSample DocsNoDefault() {
        return new DocSample(() => SettingRow.Create(
            label: "Performance overlay",
            control: Checkbox.Create("Show metrics", false, _ => { }),
            description: "Render diagnostics overlay in the corner of every Lightweave window."
        ));
    }
}
