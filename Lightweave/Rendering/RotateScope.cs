using System;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public readonly struct RotateScope : IDisposable {
    private readonly Matrix4x4 savedMatrix;

    private RotateScope(Matrix4x4 saved) {
        savedMatrix = saved;
    }

    public static RotateScope Around(float angleDegrees, Vector2 pivot) {
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(angleDegrees, pivot);
        return new RotateScope(saved);
    }

    public void Dispose() {
        GUI.matrix = savedMatrix;
    }
}
