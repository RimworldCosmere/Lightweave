using System;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public readonly struct ScaleScope : IDisposable {
    private readonly Matrix4x4 savedMatrix;

    private ScaleScope(Matrix4x4 saved) {
        savedMatrix = saved;
    }

    public static ScaleScope Around(Vector2 scale, Vector2 pivot) {
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.ScaleAroundPivot(scale, pivot);
        return new ScaleScope(saved);
    }

    public void Dispose() {
        GUI.matrix = savedMatrix;
    }
}
