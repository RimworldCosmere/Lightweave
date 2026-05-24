using System;
using System.Reflection;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

internal static class GuiClipReflection {
    private static readonly Rect FallbackRect = new Rect(-1e6f, -1e6f, 2e6f, 2e6f);

    private static bool resolved;
    private static MethodInfo? visibleRectGetter;

    private static void Resolve() {
        if (resolved) {
            return;
        }
        resolved = true;
        try {
            Type? guiClip = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
            if (guiClip == null) {
                return;
            }
            PropertyInfo? prop = guiClip.GetProperty(
                "visibleRect",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (prop == null) {
                return;
            }
            visibleRectGetter = prop.GetGetMethod(true);
        }
        catch {
            visibleRectGetter = null;
        }
    }

    public static Rect VisibleRect {
        get {
            Resolve();
            if (visibleRectGetter == null) {
                return FallbackRect;
            }
            try {
                object? value = visibleRectGetter.Invoke(null, null);
                if (value is Rect r) {
                    return r;
                }
            }
            catch {
                // fall through to fallback
            }
            return FallbackRect;
        }
    }
}
