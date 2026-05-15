using System;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public readonly struct TintScope : IDisposable {
    private readonly Color savedColor;

    private TintScope(Color saved) {
        savedColor = saved;
    }

    public static TintScope Opacity(float opacity) {
        Color saved = GUI.color;
        GUI.color = new Color(saved.r, saved.g, saved.b, saved.a * opacity);
        return new TintScope(saved);
    }

    public static TintScope Multiply(Color tint) {
        Color saved = GUI.color;
        GUI.color = new Color(saved.r * tint.r, saved.g * tint.g, saved.b * tint.b, saved.a * tint.a);
        return new TintScope(saved);
    }

    public static TintScope Replace(Color color) {
        Color saved = GUI.color;
        GUI.color = color;
        return new TintScope(saved);
    }

    public void Dispose() {
        GUI.color = savedColor;
    }
}
