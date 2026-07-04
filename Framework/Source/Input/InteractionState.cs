using Cosmere.Lightweave.Runtime;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Input;

public readonly record struct InteractionState(bool Hovered, bool Pressed, bool Focused, bool Disabled) {
    public static InteractionState Resolve(Rect rect, string? focusName, bool disabled) {
        LightweaveHitTracker.Track(rect);
        RenderContext ctx = RenderContext.Current;
        bool effectiveDisabled = disabled || ctx.ForceDisabled;
        if (effectiveDisabled) {
            if (Mouse.IsOver(rect)) {
                CursorOverrides.MarkDisabledHover();
            }

            return new InteractionState(false, false, false, true);
        }

        if (ctx.ForceHovered || ctx.ForcePressed || ctx.ForceFocused) {
            bool forcedHover = ctx.ForceHovered || ctx.ForcePressed;
            bool forcedFocus = ctx.ForceFocused || (focusName != null && GUI.GetNameOfFocusedControl() == focusName);
            return new InteractionState(forcedHover, ctx.ForcePressed, forcedFocus, false);
        }

        bool hovered = Mouse.IsOver(rect);
        if (hovered && ctx.OverlayContentDepth == 0) {
            UnityEngine.Event evt = UnityEngine.Event.current;
            if (evt != null) {
                Vector2 screenPos = GUIUtility.GUIToScreenPoint(evt.mousePosition);
                if (HoverBlockRegistry.IsBlocked(ctx.RootId, screenPos)) {
                    hovered = false;
                }
            }
        }
        bool pressed = hovered && UnityEngine.Input.GetMouseButton(0);
        bool focusedDefault = focusName != null && GUI.GetNameOfFocusedControl() == focusName;
        return new InteractionState(hovered, pressed, focusedDefault, false);
    }
}