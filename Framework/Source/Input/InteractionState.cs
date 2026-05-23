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

        if (ctx.ForceHovered || ctx.ForcePressed) {
            bool focusedForced = focusName != null && GUI.GetNameOfFocusedControl() == focusName;
            return new InteractionState(true, ctx.ForcePressed, focusedForced, false);
        }

        bool hovered = Mouse.IsOver(rect);
        if (hovered && ctx.OverlayContentDepth == 0) {
            UnityEngine.Event evt = UnityEngine.Event.current;
            if (evt != null) {
                Vector2 screenPos = GUIUtility.GUIToScreenPoint(evt.mousePosition);
                if (HoverBlockRegistry.IsBlocked(screenPos)) {
                    hovered = false;
                }
            }
        }
        bool pressed = hovered && UnityEngine.Input.GetMouseButton(0);
        bool focusedDefault = focusName != null && GUI.GetNameOfFocusedControl() == focusName;
        return new InteractionState(hovered, pressed, focusedDefault, false);
    }
}