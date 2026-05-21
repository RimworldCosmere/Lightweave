using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Runtime;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Overlay;

internal sealed class GlobalOverlayContainer : Window {
    private readonly List<(Action draw, RenderContext ctx)> pending = new List<(Action, RenderContext)>();

    public GlobalOverlayContainer() {
        doCloseX = false;
        doCloseButton = false;
        draggable = false;
        resizeable = false;
        drawShadow = false;
        doWindowBackground = false;
        forcePause = false;
        preventCameraMotion = false;
        absorbInputAroundWindow = false;
        focusWhenOpened = false;
        closeOnAccept = false;
        closeOnCancel = false;
        closeOnClickedOutside = false;
        soundAppear = null;
        soundClose = null;
        layer = WindowLayer.Super;
    }

    public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

    public override void SetInitialSizeAndPosition() {
        windowRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
    }

    public void Enqueue(Action draw, RenderContext ctx) {
        pending.Add((draw, ctx));
    }

    public override void WindowOnGUI() {
        if (Find.UIRoot.screenshotMode.Active) {
            return;
        }

        windowRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
        GUI.BeginGroup(windowRect);
        try {
            DoWindowContents(windowRect.AtZero());
        }
        catch (Exception ex) {
            Log.Error($"[Lightweave] global overlay window failed: {ex}");
        }
        finally {
            GUI.EndGroup();
        }
    }

    public override void DoWindowContents(Rect inRect) {
        if (pending.Count == 0) {
            return;
        }

        for (int i = 0; i < pending.Count; i++) {
            (Action draw, RenderContext ctx) = pending[i];
            RenderContext.Push(ctx);
            try {
                draw();
            }
            catch (Exception ex) {
                Log.Error($"[Lightweave] global overlay draw failed: {ex}");
            }
            finally {
                RenderContext.Clear();
            }
        }

        pending.Clear();
    }
}
