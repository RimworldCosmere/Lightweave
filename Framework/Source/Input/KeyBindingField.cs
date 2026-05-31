using System;
using System.Runtime.CompilerServices;
using System.Text;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Hooks;
using static Cosmere.Lightweave.Hooks.Hooks;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Input;

public readonly record struct KeyBinding(KeyCode Key, KeyModifiers Modifiers);

[Doc(
    Id = "keybinding",
    Summary = "Captures a single keyboard shortcut with optional modifiers.",
    WhenToUse = "Bind a hotkey: click to record, then press the desired combination.",
    SourcePath = "Lightweave/Input/KeyBindingField.cs"
)]
public static class KeyBindingField {
    private const string ClearGlyph = "×";

    public static LightweaveNode Create(
        [DocParam("Currently bound key combination.")]
        KeyBinding value,
        [DocParam("Invoked with the new binding when a combination is recorded.")]
        Action<KeyBinding> onChange,
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
        string recordingKey = file + "#kbf_recording" + keySuffix;

        LightweaveNode node = NodeBuilder.New("KeyBindingField", line, file);
        node.ApplyStyling("key-binding-field", style, classes, id);
        node.PreferredHeight = SelectorTrigger.Height.ToPixels();

        node.MeasureWidth = () => {
            float padX = SpacingScale.Sm.ToPixels();
            float glyphSize = new Rem(1f).ToPixels();
            float labelMin = new Rem(8f).ToPixels();
            return Mathf.Ceil(padX + labelMin + SpacingScale.Xs.ToPixels() + glyphSize + padX);
        };

        node.Paint = (rect, paintChildren) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;

            Hooks.Hooks.StateHandle<bool> recording = Hooks.Hooks.UseState(false, line, recordingKey);

            RenderContext rc = RenderContext.Current;
            bool effectiveDisabled = disabled || rc.ForceDisabled;
            bool isRecording = recording.Value && !effectiveDisabled;
            bool mouseOverField = Mouse.IsOver(rect);
            if (effectiveDisabled && mouseOverField) {
                CursorOverrides.MarkDisabledHover();
            }

            bool hovered = !effectiveDisabled && (mouseOverField || rc.ForceHovered || rc.ForcePressed);
            bool pressed = !effectiveDisabled && ((mouseOverField && UnityEngine.Input.GetMouseButton(0)) || rc.ForcePressed);
            bool focused = isRecording || (!effectiveDisabled && rc.ForceFocused);

            InteractionState state = new InteractionState(hovered, pressed, focused, effectiveDisabled);

            InputSurface.DrawInputChrome(rect, state, variant);

            float padX = SpacingScale.Sm.ToPixels();
            float glyphSize = new Rem(1f).ToPixels();
            bool hasBinding = value.Key != KeyCode.None;
            bool showClear = hasBinding && !effectiveDisabled && !isRecording;

            bool clearOnRight = dir == Direction.Ltr;
            Rect clearRect = clearOnRight
                ? new Rect(rect.xMax - padX - glyphSize, rect.y, glyphSize, rect.height)
                : new Rect(rect.x + padX, rect.y, glyphSize, rect.height);

            float leftLabelX = clearOnRight ? rect.x + padX :
                showClear ? clearRect.xMax + SpacingScale.Xs.ToPixels() : rect.x + padX;
            float rightLabelX = clearOnRight
                ? showClear ? clearRect.x - SpacingScale.Xs.ToPixels() : rect.xMax - padX
                : rect.xMax - padX;

            Rect labelRect = new Rect(
                leftLabelX,
                rect.y,
                Mathf.Max(0f, rightLabelX - leftLabelX),
                rect.height
            );

            DrawLabel(labelRect, theme, value, isRecording, effectiveDisabled);

            if (showClear) {
                DrawClearButton(clearRect, theme, onChange);
            }

            Event e = Event.current;

            if (!effectiveDisabled &&
                !isRecording &&
                e.type == EventType.MouseDown &&
                e.button == 0 &&
                rect.Contains(e.mousePosition) &&
                !(showClear && clearRect.Contains(e.mousePosition))) {
                recording.Set(true);
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                e.Use();
            }

            if (isRecording && e.type == EventType.MouseDown && !rect.Contains(e.mousePosition)) {
                recording.Set(false);
            }

            bool isKeyDown = e.type == EventType.KeyDown && e.keyCode != KeyCode.None;
            if (isRecording && isKeyDown) {
                if (e.keyCode == KeyCode.Escape && !e.control && !e.shift && !e.alt) {
                    recording.Set(false);
                    e.Use();
                }
                else if (!IsModifierOnly(e.keyCode)) {
                    KeyModifiers mods = KeyModifiers.None;
                    if (e.control || e.command) {
                        mods |= KeyModifiers.Control;
                    }

                    if (e.shift) {
                        mods |= KeyModifiers.Shift;
                    }

                    if (e.alt) {
                        mods |= KeyModifiers.Alt;
                    }

                    onChange?.Invoke(new KeyBinding(e.keyCode, mods));
                    RenderContext.Current.Hooks.Invalidate();
                    recording.Set(false);
                    e.Use();
                }
            }

            paintChildren();
        };

        return node;
    }

    private static void DrawLabel(Rect rect, Theme.Theme theme, KeyBinding value, bool recording, bool disabled) {
        Font font = theme.GetFont(FontRole.Body);
        int pixelSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
        FontStyle fontStyle = recording ? FontStyle.Italic : FontStyle.Normal;
        GUIStyle style = GuiStyleCache.GetOrCreate(font, pixelSize, fontStyle);
        style.alignment = TextAnchor.MiddleCenter;

        string text;
        ThemeSlot colorSlot;
        if (recording) {
            text = "CL_KeyBindingField_Recording".Translate();
            colorSlot = ThemeSlot.TextMuted;
        }
        else if (value.Key == KeyCode.None) {
            text = (string)"CL_KeyBindingField_Unbound".Translate();
            colorSlot = ThemeSlot.TextMuted;
        }
        else {
            text = FormatBinding(value);
            colorSlot = disabled ? ThemeSlot.TextMuted : ThemeSlot.TextPrimary;
        }

        TextDraw.DrawWithStyle(rect, text, style, theme.GetColor(colorSlot));
    }

    private static void DrawClearButton(Rect rect, Theme.Theme theme, Action<KeyBinding>? onChange) {
        bool hovered = Mouse.IsOver(rect);

        Font font = theme.GetFont(FontRole.Body);
        int pixelSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
        GUIStyle style = GuiStyleCache.GetOrCreate(font, pixelSize);
        style.alignment = TextAnchor.MiddleCenter;

        TextDraw.DrawWithStyle(rect, ClearGlyph, style, theme.GetColor(hovered ? ThemeSlot.TextPrimary : ThemeSlot.TextMuted));

        Event e = Event.current;
        if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(rect)) {
            onChange?.Invoke(new KeyBinding(KeyCode.None, KeyModifiers.None));
            RenderContext.Current.Hooks.Invalidate();
            e.Use();
        }
    }

    private static bool IsModifierOnly(KeyCode key) {
        return key == KeyCode.LeftControl ||
               key == KeyCode.RightControl ||
               key == KeyCode.LeftShift ||
               key == KeyCode.RightShift ||
               key == KeyCode.LeftAlt ||
               key == KeyCode.RightAlt ||
               key == KeyCode.LeftCommand ||
               key == KeyCode.RightCommand ||
               key == KeyCode.LeftWindows ||
               key == KeyCode.RightWindows ||
               key == KeyCode.None;
    }

    private static string FormatBinding(KeyBinding b) {
        StringBuilder sb = new StringBuilder();
        if ((b.Modifiers & KeyModifiers.Control) != 0) {
            sb.Append("Ctrl+");
        }

        if ((b.Modifiers & KeyModifiers.Shift) != 0) {
            sb.Append("Shift+");
        }

        if ((b.Modifiers & KeyModifiers.Alt) != 0) {
            sb.Append("Alt+");
        }

        sb.Append(b.Key.ToString());
        return sb.ToString();
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        StateHandle<KeyBinding> s = UseState(new KeyBinding(KeyCode.F, KeyModifiers.Control));
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v)
        ));
    }

    [DocVariant("CL_Playground_Label_Primary")]
    public static DocSample DocsPrimary() {
        StateHandle<KeyBinding> s = UseState(new KeyBinding(KeyCode.F, KeyModifiers.Control));
        return new DocSample(() => Create(s.Value, v => s.Set(v), variant: Variant.Primary));
    }

    [DocVariant("CL_Playground_Label_Ghost")]
    public static DocSample DocsGhost() {
        StateHandle<KeyBinding> s = UseState(new KeyBinding(KeyCode.F, KeyModifiers.Control));
        return new DocSample(() => Create(s.Value, v => s.Set(v), variant: Variant.Ghost));
    }

    [DocVariant("CL_Playground_Label_Danger")]
    public static DocSample DocsDanger() {
        StateHandle<KeyBinding> s = UseState(new KeyBinding(KeyCode.F, KeyModifiers.Control));
        return new DocSample(() => Create(s.Value, v => s.Set(v), variant: Variant.Danger));
    }

    [DocVariant("CL_Playground_Label_Frosted")]
    public static DocSample DocsFrosted() {
        StateHandle<KeyBinding> s = UseState(new KeyBinding(KeyCode.F, KeyModifiers.Control));
        return new DocSample(() => Create(s.Value, v => s.Set(v), variant: Variant.Frosted));
    }

    private static LightweaveNode AllVariantsRow() {
        return HStack.Create(
            SpacingScale.Lg,
            row => {
                row.AddHug(Create(new KeyBinding(KeyCode.F, KeyModifiers.Control), _ => { }, instanceKey: "kbf_v_primary", variant: Variant.Primary));
                row.AddHug(Create(new KeyBinding(KeyCode.F, KeyModifiers.Control), _ => { }, instanceKey: "kbf_v_ghost", variant: Variant.Ghost));
                row.AddHug(Create(new KeyBinding(KeyCode.F, KeyModifiers.Control), _ => { }, instanceKey: "kbf_v_danger", variant: Variant.Danger));
                row.AddHug(Create(new KeyBinding(KeyCode.F, KeyModifiers.Control), _ => { }, instanceKey: "kbf_v_frosted", variant: Variant.Frosted));
                row.AddHug(Create(new KeyBinding(KeyCode.None, KeyModifiers.None), _ => { }, instanceKey: "kbf_v_unbound"));
            }
        );
    }

    [DocState("CL_Playground_Label_Default", HideCode = true)]
    public static DocSample DocsDefaultState() {
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
        StateHandle<KeyBinding> s = UseState(new KeyBinding(KeyCode.F, KeyModifiers.Control));
        return new DocSample(() => Create(
            s.Value,
            v => s.Set(v)
        ));
    }
}