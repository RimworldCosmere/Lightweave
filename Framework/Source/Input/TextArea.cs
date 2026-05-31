using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "textarea",
    Summary = "Multi-line editable text input that grows with content.",
    WhenToUse = "Capture longer prose: notes, descriptions, multi-line input.",
    SourcePath = "Lightweave/Input/TextArea.cs"
)]
public static class TextArea {
    public static LightweaveNode Create(
        [DocParam("Current text value.")]
        string value,
        [DocParam("Invoked with the committed text after focus is lost.")]
        Action<string> onChange,
        [DocParam("Optional placeholder rendered when the buffer is empty.")]
        string? placeholder = null,
        [DocParam("Minimum number of visible rows before content grows.")]
        int minRows = 3,
        [DocParam("Maximum number of visible rows before scrolling.")]
        int maxRows = 8,
        [DocParam("Renders the value without an editable surface.")]
        bool readOnly = false,
        [DocParam("Disables interaction and applies disabled styling.")]
        bool disabled = false,
        [DocParam("Optional key disambiguating multiple instances declared on the same line.")]
        object? instanceKey = null,
        Variant variant = default,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string keySuffix = instanceKey == null ? string.Empty : "#" + instanceKey;
        string focusKey = file + "#ta_focus" + keySuffix;
        string bufferKey = file + "#ta_buffer" + keySuffix;
        string wasFocusedKey = file + "#ta_wasFocused" + keySuffix;

        LightweaveNode node = NodeBuilder.New("TextArea", line, file);
        node.ApplyStyling("text-area", style, classes, id);
        float lineHeightPx = new Rem(1.5f).ToPixels();

        node.MeasureWidth = () => {
            float padX = InputSurface.PaddingX.ToPixels();
            return Mathf.Ceil(new Rem(20f).ToPixels() + padX * 2f);
        };

        node.Measure = availableWidth => {
            string current = value ?? string.Empty;
            float padX = InputSurface.PaddingX.ToPixels();
            float innerWidth = Mathf.Max(1f, availableWidth - padX * 2f);
            int measureSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
            int rows;
            float rowHeight;
            RenderContext rcx = RenderContext.Current;
            if (rcx != null) {
                Font measureFont = rcx.Theme.GetFont(FontRole.Body);
                rows = CountVisualRows(current, measureFont, measureSize, innerWidth);
                rowHeight = RowLineHeight(measureFont, measureSize);
            }
            else {
                rows = CountRows(current);
                rowHeight = lineHeightPx;
            }

            int clamped = Mathf.Clamp(rows, Mathf.Max(1, minRows), Mathf.Max(minRows, maxRows));
            return SelectorTrigger.Height.ToPixels() + (clamped - 1) * rowHeight + DescenderPad(measureSize);
        };

        node.Paint = (rect, paintChildren) => {
            Theme.Theme theme = RenderContext.Current.Theme;

            Hooks.Hooks.RefHandle<string> focusNameRef = Hooks.Hooks.UseRef<string>("", line, focusKey);
            if (string.IsNullOrEmpty(focusNameRef.Current)) {
                focusNameRef.Current = "lw_ta_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            string focusName = focusNameRef.Current;

            Hooks.Hooks.StateHandle<string> buffer = Hooks.Hooks.UseState(value ?? string.Empty, line, bufferKey);
            Hooks.Hooks.RefHandle<bool> wasFocused = Hooks.Hooks.UseRef(false, line, wasFocusedKey);

            string effectiveText = readOnly ? (value ?? string.Empty) : (buffer.Value ?? string.Empty);

            float padX = InputSurface.PaddingX.ToPixels();
            Font measureFont = theme.GetFont(FontRole.Body);
            int measureSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
            float lineHeight = RowLineHeight(measureFont, measureSize);
            float descenderPad = DescenderPad(measureSize);
            float measureWidth = Mathf.Max(1f, rect.width - padX * 2f);
            int contentRows = CountVisualRows(effectiveText, measureFont, measureSize, measureWidth);
            int clampedRows = Mathf.Clamp(contentRows, Mathf.Max(1, minRows), Mathf.Max(minRows, maxRows));
            Style resolvedStyle = node.GetResolvedStyle();
            bool growVertical = resolvedStyle.Height is { IsGrower: true };
            float rowBasedHeight = SelectorTrigger.Height.ToPixels() + (clampedRows - 1) * lineHeight + descenderPad;
            float resolvedHeight = growVertical ? Mathf.Max(rowBasedHeight, rect.height) : rowBasedHeight;
            Rect surfaceRect = new Rect(rect.x, rect.y, rect.width, resolvedHeight);

            InteractionState state = InteractionState.Resolve(surfaceRect, focusName, disabled);
            InputSurface.DrawInputChrome(surfaceRect, state, variant);

            float topPad = (SelectorTrigger.Height.ToPixels() - lineHeight) / 2f;
            float innerHeight = growVertical
                ? Mathf.Max(lineHeight, surfaceRect.height - topPad * 2f)
                : clampedRows * lineHeight + descenderPad;
            Rect inner = new Rect(
                surfaceRect.x + padX,
                surfaceRect.y + topPad,
                surfaceRect.width - padX * 2f,
                innerHeight
            );

            bool showPlaceholder =
                !state.Focused && string.IsNullOrEmpty(effectiveText) && !string.IsNullOrEmpty(placeholder);

            if (showPlaceholder) {
                InputSurface.DrawPlaceholder(inner, placeholder, theme, TextAnchor.UpperLeft);
            }

            if (disabled) {
                InputSurface.DrawReadOnlyValue(inner, effectiveText, theme, TextAnchor.UpperLeft);
            }
            else if (readOnly) {
                Font roFont = theme.GetFont(FontRole.Body);
                int roSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
                Color roTextColor = theme.GetColor(ThemeSlot.TextPrimary);
                GUIStyle roStyle = InputSurface.ConfigureChromelessTextAreaStyle(roFont, roSize, roTextColor);
                GUI.SetNextControlName(focusName);
                GUI.TextArea(RectSnap.Snap(inner), effectiveText, roStyle);
            }
            else {
                Font taFont = theme.GetFont(FontRole.Body);
                int taSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
                Color taTextColor = theme.GetColor(ThemeSlot.TextPrimary);
                GUIStyle taStyle = InputSurface.ConfigureChromelessTextAreaStyle(taFont, taSize, taTextColor);
                GUI.SetNextControlName(focusName);
                string next = GUI.TextArea(RectSnap.Snap(inner), buffer.Value ?? string.Empty, taStyle);
                if (next != buffer.Value) {
                    buffer.Set(next);
                }
            }

            Event evt = Event.current;
            if (!disabled &&
                evt.type == EventType.MouseDown &&
                evt.button == 0 &&
                !surfaceRect.Contains(evt.mousePosition)) {
                if (GUI.GetNameOfFocusedControl() == focusName) {
                    GUI.FocusControl(null);
                }
            }

            bool isFocusedNow = GUI.GetNameOfFocusedControl() == focusName;
            bool focusLost = wasFocused.Current && !isFocusedNow;
            wasFocused.Current = isFocusedNow;

            if (focusLost && !readOnly) {
                onChange?.Invoke(buffer.Value ?? string.Empty);
                RenderContext.Current.Hooks.Invalidate();
            }

            paintChildren();
        };

        return node;
    }

    private static int CountRows(string text) {
        if (string.IsNullOrEmpty(text)) {
            return 1;
        }

        int rows = 1;
        for (int i = 0; i < text.Length; i++) {
            if (text[i] == '\n') {
                rows++;
            }
        }

        return rows;
    }

    private static readonly GUIContent MeasureContent = new GUIContent();

    private static int CountVisualRows(string text, Font? font, int pixelSize, float width) {
        if (string.IsNullOrEmpty(text)) {
            return 1;
        }

        if (width <= 1f) {
            return CountRows(text);
        }

        GUIStyle measureStyle = InputSurface.ConfigureChromelessTextAreaStyle(font, pixelSize, Color.white);
        MeasureContent.text = text;
        float totalHeight = measureStyle.CalcHeight(MeasureContent, width);
        float fontLineHeight = measureStyle.lineHeight > 0f ? measureStyle.lineHeight : pixelSize;
        return Mathf.Max(1, Mathf.RoundToInt(totalHeight / fontLineHeight));
    }

    private static float RowLineHeight(Font? font, int pixelSize) {
        GUIStyle style = InputSurface.ConfigureChromelessTextAreaStyle(font, pixelSize, Color.white);
        return style.lineHeight > 0f ? style.lineHeight : pixelSize;
    }

    private static float DescenderPad(int pixelSize) {
        return Mathf.Max(2f, pixelSize * 0.25f);
    }

[DocVariant("CL_Playground_Label_Primary")]
    public static DocSample DocsPrimary() {
        StateHandle<string> s = UseState("The Stormfather rumbled as Kaladin drew in the Light, and the spren scattered across the chasm like windblown leaves. He clenched the spear, knowing the next breath might be his last as the bridge crews charged the Parshendi line.");
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v),
            (string)"CL_Playground_Controls_TextArea_Placeholder".Translate(),
            2,
            3,
            variant: Variant.Primary
        ));
    }

    [DocVariant("CL_Playground_Label_Secondary")]
    public static DocSample DocsSecondary() {
        StateHandle<string> s = UseState("The Stormfather rumbled as Kaladin drew in the Light, and the spren scattered across the chasm like windblown leaves. He clenched the spear, knowing the next breath might be his last as the bridge crews charged the Parshendi line.");
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v),
            (string)"CL_Playground_Controls_TextArea_Placeholder".Translate(),
            2,
            3,
            variant: Variant.Secondary
        ));
    }

    [DocVariant("CL_Playground_Label_Ghost")]
    public static DocSample DocsGhost() {
        StateHandle<string> s = UseState("The Stormfather rumbled as Kaladin drew in the Light, and the spren scattered across the chasm like windblown leaves. He clenched the spear, knowing the next breath might be his last as the bridge crews charged the Parshendi line.");
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v),
            (string)"CL_Playground_Controls_TextArea_Placeholder".Translate(),
            2,
            3,
            variant: Variant.Ghost
        ));
    }

    [DocVariant("CL_Playground_Label_Danger")]
    public static DocSample DocsDanger() {
        StateHandle<string> s = UseState("The Stormfather rumbled as Kaladin drew in the Light, and the spren scattered across the chasm like windblown leaves. He clenched the spear, knowing the next breath might be his last as the bridge crews charged the Parshendi line.");
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v),
            (string)"CL_Playground_Controls_TextArea_Placeholder".Translate(),
            2,
            3,
            variant: Variant.Danger
        ));
    }

    [DocVariant("CL_Playground_Label_Frosted")]
    public static DocSample DocsFrosted() {
        StateHandle<string> s = UseState("The Stormfather rumbled as Kaladin drew in the Light, and the spren scattered across the chasm like windblown leaves. He clenched the spear, knowing the next breath might be his last as the bridge crews charged the Parshendi line.");
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v),
            (string)"CL_Playground_Controls_TextArea_Placeholder".Translate(),
            2,
            3,
            variant: Variant.Frosted
        ));
    }

    [DocVariant("CL_Playground_Label_Filled")]
    public static DocSample DocsFilled() {
        StateHandle<string> s = UseState("The Stormfather rumbled as Kaladin drew in the Light, and the spren scattered across the chasm like windblown leaves. He clenched the spear, knowing the next breath might be his last as the bridge crews charged the Parshendi line.");
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v),
            (string)"CL_Playground_Controls_TextArea_Placeholder".Translate(),
            2,
            3
        ));
    }

    [DocVariant("CL_Playground_Label_Empty")]
    public static DocSample DocsEmpty() {
        StateHandle<string> s = UseState(string.Empty);
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v),
            (string)"CL_Playground_Controls_TextArea_Placeholder".Translate(),
            2,
            3
        ));
    }

    private static LightweaveNode AllVariantsRow() {
        return HStack.Create(
            SpacingScale.Sm,
            row => {
                row.AddHug(Create("Primary", _ => { }, null, 2, 3, instanceKey: "ta_v_primary", variant: Variant.Primary));
                row.AddHug(Create("Secondary", _ => { }, null, 2, 3, instanceKey: "ta_v_secondary", variant: Variant.Secondary));
                row.AddHug(Create("Ghost", _ => { }, null, 2, 3, instanceKey: "ta_v_ghost", variant: Variant.Ghost));
                row.AddHug(Create("Danger", _ => { }, null, 2, 3, instanceKey: "ta_v_danger", variant: Variant.Danger));
                row.AddHug(Create("Frosted", _ => { }, null, 2, 3, instanceKey: "ta_v_frosted", variant: Variant.Frosted));
            }
        );
    }

    [DocState("CL_Playground_Label_Default", HideCode = true)]
    public static DocSample DocsDefault() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Hover", HideCode = true)]
    public static DocSample DocsHover() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Active", HideCode = true)]
    public static DocSample DocsActive() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Focus", HideCode = true)]
    public static DocSample DocsFocus() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Disabled", HideCode = true)]
    public static DocSample DocsDisabled() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        StateHandle<string> s = UseState("Notes about the bond.");
        return new DocSample(() => Create(s.Value, v => s.Set(v)));
    }
}