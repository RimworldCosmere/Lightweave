using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Feedback;

[Doc(
    Id = "alert",
    Summary = "Inline tinted callout with a leading status glyph, title, optional description, and optional trailing actions.",
    WhenToUse = "Communicate state to the player at the spot it applies — drift from defaults, validation failure, completed action, or contextual info.",
    SourcePath = "Lightweave/Feedback/Alert.cs"
)]
public static class Alert {
    public static LightweaveNode Create(
        [DocParam("Bolded title. Caller is responsible for translation.")]
        string title,
        [DocParam("Optional descriptive body line under the title. Caller is responsible for translation.")]
        string? description = null,
        [DocParam("Variant drives the icon, fill tint, and accent stripe color.")]
        AlertVariant variant = AlertVariant.Info,
        [DocParam("Optional trailing nodes (typically Button.Create(...) results). Rendered right-aligned.", TypeOverride = "LightweaveNode[]?", DefaultOverride = "null")]
        LightweaveNode[]? actions = null,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'alert' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Alert", line, file);
        node.ApplyStyling("alert", style, classes, id);

        ThemeSlot accentSlot = variant switch {
            AlertVariant.Success => ThemeSlot.StatusSuccess,
            AlertVariant.Warning => ThemeSlot.StatusWarning,
            AlertVariant.Danger => ThemeSlot.StatusDanger,
            _ => ThemeSlot.StatusInfo,
        };
        ThemeSlot fillSlot = variant switch {
            AlertVariant.Success => ThemeSlot.StatusSuccessSoft,
            AlertVariant.Warning => ThemeSlot.StatusWarningSoft,
            AlertVariant.Danger => ThemeSlot.StatusDangerSoft,
            _ => ThemeSlot.StatusInfoSoft,
        };
        IconRef icon = variant switch {
            AlertVariant.Success => Phosphor.CheckCircle,
            AlertVariant.Warning => Phosphor.Warning,
            AlertVariant.Danger => Phosphor.WarningCircle,
            _ => Phosphor.Info,
        };

        LightweaveNode body = Stack.Create(SpacingScale.Xxs, s => {
            s.Add(Text.Create(
                title,
                wrap: true,
                style: new Style {
                    FontFamily = FontRole.BodyBold,
                    TextColor = ThemeSlot.TextPrimary,
                }
            ));
            if (!string.IsNullOrEmpty(description)) {
                s.Add(Text.Create(
                    description!,
                    wrap: true,
                    style: new Style {
                        FontSize = new Rem(0.875f),
                        TextColor = ThemeSlot.TextSecondary,
                    }
                ));
            }
        });

        LightweaveNode row = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Glyph.Create(
                icon,
                style: new Style {
                    FontSize = new Rem(1.125f),
                    TextColor = accentSlot,
                }
            ));
            h.AddFlex(body);
            if (actions != null && actions.Length > 0) {
                h.AddHug(HStack.Create(SpacingScale.Xs, a => {
                    for (int i = 0; i < actions.Length; i++) {
                        a.AddHug(actions[i]);
                    }
                }));
            }
        });

        LightweaveNode wrapper = Box.Create(
            children: c => c.Add(row),
            style: new Style {
                Width = Length.Stretch,
                Background = BackgroundSpec.Of(fillSlot),
                Padding = new EdgeInsets(
                    Top: SpacingScale.Sm,
                    Bottom: SpacingScale.Sm,
                    Left: SpacingScale.Md,
                    Right: SpacingScale.Sm
                ),
                Border = new BorderSpec(Left: new Rem(3f / 16f), Color: accentSlot),
                Radius = RadiusSpec.All(RadiusScale.Sm),
            }
        );

        node.Children.Add(wrapper);
        node.MeasureWidth = () => wrapper.MeasureWidth?.Invoke() ?? 0f;
        node.Measure = availableWidth => wrapper.Measure?.Invoke(availableWidth) ?? wrapper.PreferredHeight ?? 0f;
        node.Paint = (rect, paintChildren) => {
            wrapper.MeasuredRect = rect;
            paintChildren();
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create(
            title: "3 settings differ from defaults",
            description: "Reset individual rows to return them to the shipped values."
        ));
    }

    [DocVariant("CL_Playground_Alert_Variant_Info")]
    public static DocSample DocsInfo() {
        return new DocSample(() => Create(
            title: "Heads up",
            description: "These changes only apply after restart.",
            variant: AlertVariant.Info
        ));
    }

    [DocVariant("CL_Playground_Alert_Variant_Success")]
    public static DocSample DocsSuccess() {
        return new DocSample(() => Create(
            title: "Settings saved",
            description: "Your changes are now active.",
            variant: AlertVariant.Success
        ));
    }

    [DocVariant("CL_Playground_Alert_Variant_Warning")]
    public static DocSample DocsWarning() {
        return new DocSample(() => Create(
            title: "Mod list has unsaved changes",
            description: "Save before quitting or your edits will be lost.",
            variant: AlertVariant.Warning
        ));
    }

    [DocVariant("CL_Playground_Alert_Variant_Danger")]
    public static DocSample DocsDanger() {
        return new DocSample(() => Create(
            title: "Cannot load save",
            description: "Three required mods are missing from the mod list.",
            variant: AlertVariant.Danger
        ));
    }

    [DocState("CL_Playground_Alert_State_TitleOnly")]
    public static DocSample DocsTitleOnly() {
        return new DocSample(() => Create(
            title: "Autosave just ran",
            variant: AlertVariant.Info
        ));
    }

    [DocState("CL_Playground_Alert_State_LongCopy")]
    public static DocSample DocsLongCopy() {
        return new DocSample(() => Create(
            title: "Compatibility note",
            description: "This mod overrides Core's pawn rendering pipeline. Other mods that touch the same draw path may render incorrectly or fail to apply their patches when ordered after this one.",
            variant: AlertVariant.Info
        ));
    }
}
