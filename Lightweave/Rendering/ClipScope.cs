using System;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public readonly struct ClipScope : IDisposable {
    public static ClipScope Begin(Rect rect) {
        GUI.BeginClip(rect);
        return new ClipScope();
    }

    public void Dispose() {
        GUI.EndClip();
    }
}
