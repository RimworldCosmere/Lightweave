using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using static Cosmere.Lightweave.Typography.Typography;
using Cosmere.Lightweave.Data;

namespace Cosmere.Lightweave.Layout;

[Doc(
    Id = "settings-section",
    Summary = "Settings form section: inline eyebrow + caption header above a divider, with a stack of SettingRow children beneath.",
    WhenToUse = "Wrap a group of related SettingRow primitives in a labeled section inside a settings form tab body.",
    SourcePath = "Lightweave/Layout/SettingsSection.cs"
)]
public static class SettingsSection {
    public static LightweaveNode Create(
        [DocParam("Eyebrow label rendered in uppercase mono on the left of the header.")]
        string title,
        [DocParam("Builder callback to populate the rows.")]
        Action<StackBuilder>? rows = null,
        [DocParam("Optional caption rendered next to the eyebrow.")]
        string? description = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string[]? mergedClasses = StyleExtensions.PrependClass("settings-section", classes);

        return Stack.Create(
            gap: SpacingScale.Md,
            children: section => {
                section.Add(BuildHeader(title, description));
                section.Add(Divider.Horizontal());
                section.Add(Stack.Create(SpacingScale.None, rowsStack => {
                    rows?.Invoke(rowsStack);
                }));
            },
            style: style,
            classes: mergedClasses,
            id: id,
            line: line,
            file: file
        );
    }

    private static LightweaveNode BuildHeader(string title, string? description) {
        return HStack.Create(SpacingScale.Md, header => {
            header.AddHug(Eyebrow.Create(
                title,
                style: new Style { FontSize = new Rem(1f) }
            ));
            if (!string.IsNullOrEmpty(description)) {
                header.AddFlex(Text.Create(
                    description!,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.8125f), TextColor = ThemeSlot.TextSecondary }
                ));
            }
        });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => SettingsSection.Create(
            title: "THEME",
            description: "Visual identity of the shell.",
            rows: r => {
                r.Add(SettingRow.Create("Color palette", Checkbox.Create("Cosmere", true, _ => { }), description: "Pick the active theme."));
                r.Add(SettingRow.Create("Font size", Checkbox.Create("100%", false, _ => { }), defaultValue: "default: 100%"));
            }
        ));
    }

    [DocVariant("no-description")]
    public static DocSample DocsNoDescription() {
        return new DocSample(() => SettingsSection.Create(
            title: "DIAGNOSTICS",
            rows: r => {
                r.Add(SettingRow.Create("Performance metrics", Checkbox.Create("Show", false, _ => { })));
            }
        ));
    }
}
