using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using static Cosmere.Lightweave.Hooks.Hooks;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Data;

[Doc(
    Id = "world-preview",
    Summary = "Renders the live RimWorld planet (the current Find.World) into the node rect and reports the tile under the cursor or click.",
    WhenToUse = "Embed an interactive globe inside a panel: a new-colony world step, a site picker, or any surface that needs the real planet rather than a static image. Falls back to a sunken placeholder when no world is generated yet.",
    SourcePath = "Lightweave/Data/WorldPreview.cs"
)]
public static class WorldPreview {
    // Open two zoom-ticks backed off the camera's default altitude so the whole planet sits
    // comfortably in the pane instead of filling it. ZoomStep (0.1) is applied multiplicatively
    // per scroll tick, so two ticks out is the default * 1.1 * 1.1.
    private const float StartingAltitude = 550f * 1.1f * 1.1f;
    private const float MaxAltitude = 1100f;
    private const float ZoomStep = 0.1f;
    private const float RollSpeed = 0.5f;

    public static LightweaveNode Create(
        [DocParam("Fired when the hovered tile changes. The argument is the tile under the cursor.", TypeOverride = "Action<PlanetTile>?", DefaultOverride = "null")]
        Action<PlanetTile>? onHover = null,
        [DocParam("Fired on left-click over a valid tile (a click that did not drag-rotate). The argument is the picked tile.", TypeOverride = "Action<PlanetTile>?", DefaultOverride = "null")]
        Action<PlanetTile>? onPick = null,
        [DocParam("Live predicate. While it returns true the globe is suppressed (not rendered or interacted with) and the loading message is shown instead. Checked every repaint, so the caller can flip a background flag without forcing a rebuild.", TypeOverride = "Func<bool>?", DefaultOverride = "null")]
        Func<bool>? loading = null,
        [DocParam("Centered message drawn over the sunken surface while loading is true. The caller translates it.", TypeOverride = "string?", DefaultOverride = "null")]
        string? loadingText = null,
        [DocParam("Corner rounding for the loading/empty placeholder. The live globe renders as a rectangular viewport like the vanilla world map.")]
        RadiusScale radius = RadiusScale.Md,
        [DocParam("When true (default) the primitive resets the world renderer to None on unmount, so the planet stops drawing once the surface is gone. Pass false when the embedding surface owns the world lifecycle across mounts (e.g. a tabbed wizard that keeps a generated world alive while the user visits other tabs).", DefaultOverride = "true")]
        bool restoreRenderModeOnUnmount = true,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("WorldPreview", line, file);
        node.ApplyStyling("world-preview", style, classes, id);

        RadiusSpec radiusSpec = RadiusSpec.All(radius);

        RefHandle<int> lastHoverTile = UseRef(-1);
        RefHandle<bool> didDrag = UseRef(false);
        RefHandle<Quaternion> sphereRotation = UseRef(Quaternion.identity);
        RefHandle<float> altitude = UseRef(StartingAltitude);

        UseEffect(() => {
            return () => {
                RestorePreviewMode(restoreRenderModeOnUnmount);
                lastHoverTile.Current = -1;
            };
        }, []);

        // Accumulate globe rotation from a drag delta. RefHandle writes don't invalidate the render
        // cache, so rotating the globe doesn't bump the hook version - but the drag still walks the
        // full tree once per MouseDrag event via LightweaveRoot.Render (no fast-path bypass).
        void PumpDrag(Event ev) {
            if (ev.type != EventType.MouseDrag || ev.button != 0) {
                return;
            }
            Camera? cam = Find.WorldCamera;
            if (cam == null) {
                return;
            }
            Vector2 dragDelta = ev.delta;
            dragDelta.x *= -1f;
            Vector2 raw = dragDelta / GenWorldUI.CurUITileSize() * (0.273f * Prefs.MapDragSensitivity);
            Quaternion rot = sphereRotation.Current;
            rot *= Quaternion.AngleAxis(raw.x, cam.transform.up);
            rot *= Quaternion.AngleAxis(-raw.y, cam.transform.right);
            sphereRotation.Current = rot;
            didDrag.Current = true;
            ev.Use();
        }

        node.MeasureWidth = () => {
            Style resolved = node.GetResolvedStyle();
            if (resolved.Width is { Mode: Length.Kind.Rem } fixedWidth) {
                return fixedWidth.ToPixels(0f, 0f);
            }
            return new Rem(20f).ToPixels();
        };
        node.Measure = _ => {
            Style resolved = node.GetResolvedStyle();
            if (resolved.Height is { Mode: Length.Kind.Rem } fixedHeight) {
                return fixedHeight.ToPixels(0f, 0f);
            }
            return new Rem(14f).ToPixels();
        };

        node.Layout = rect => {
            node.MeasuredRect = rect;

            Event current = Event.current;
            if (current == null || current.type == EventType.Repaint) {
                return;
            }
            if (loading != null && loading()) {
                return;
            }

            World? world = Find.World;
            if (world == null) {
                return;
            }
            Camera? camera = Find.WorldCamera;
            if (camera == null) {
                return;
            }
            if (!Mouse.IsOver(rect) || !LightweaveHitTracker.IsTopmost(rect)) {
                return;
            }

            // The wheel is captured and Use()d at the top of LightweaveRoot.Render, so current.type is
            // already Used here - a live current.type == ScrollWheel check never fires. Pull the stashed
            // delta instead; our Mouse.IsOver + IsTopmost gate above is the under-cursor guard.
            if (LightweaveScrollView.TryConsumeWheel(out float wheelDeltaY)) {
                altitude.Current = Mathf.Clamp(
                    altitude.Current * (1f + wheelDeltaY * ZoomStep),
                    WorldCameraDriver.MinAltitude,
                    MaxAltitude);
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0) {
                didDrag.Current = false;
            }

            if (current.type == EventType.MouseDrag && current.button == 0) {
                PumpDrag(current);
                return;
            }

            // Pick exactly the way the vanilla world map does: aim the WorldCamera at the pane's
            // real screen-pixel rect, then ScreenPointToRay through the live mouse. This keeps the
            // tile we report identical to the WorldDrawLayer_MouseTile highlight, which is driven by
            // GenWorld.MouseTile() (also ScreenPointToRay) when the globe renders below.
            Rect prevPixelRect = camera.pixelRect;
            camera.pixelRect = PaneScreenRect(rect);
            Ray ray = camera.ScreenPointToRay(UI.MousePositionOnUI * Prefs.UIScale);
            camera.pixelRect = prevPixelRect;

            WorldTerrainColliderManager.EnsureRaycastCollidersUpdated();
            if (!Physics.Raycast(ray, out RaycastHit hit, 1500f, WorldCameraManager.WorldLayerMask)) {
                return;
            }

            PlanetTile tile = world.renderer.GetTileFromRayHit(hit);
            if (!tile.Valid) {
                return;
            }

            if (tile.tileId != lastHoverTile.Current) {
                lastHoverTile.Current = tile.tileId;
                onHover?.Invoke(tile);
            }

            if (current.type == EventType.MouseUp && current.button == 0 && !didDrag.Current) {
                Find.WorldSelector.SelectedTile = tile;
                onPick?.Invoke(tile);
                current.Use();
            }
        };

        node.Draw = rect => {
            if (loading != null && loading()) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.SurfaceSunken), null, radiusSpec);
                if (!string.IsNullOrEmpty(loadingText)) {
                    TextDraw.Draw(rect, loadingText!, FontRole.Body, new Rem(13f / 16f),
                        TextAnchor.MiddleCenter, ThemeSlot.TextMuted);
                }
                return;
            }

            World? world = Find.World;
            Camera? camera = world != null ? Find.WorldCamera : null;

            if (world == null || camera == null) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.SurfaceSunken), null, radiusSpec);
                return;
            }

            // Q/E roll the view around the camera's forward axis (tilt the horizon) - the one
            // rotational axis drag doesn't cover. Polled here in Draw, not Layout: node.Layout is
            // never invoked on the EventType.Layout pass (LightweaveRoot skips Paint on Layout), and
            // on every other event it would fire only when that event happens (mouse move, etc), so
            // holding a key with the mouse still would never roll. node.Draw runs on every Repaint -
            // once per frame, continuously while the globe is shown - which is exactly the per-frame
            // cadence a held-key roll needs. UnityEngine.Input.GetKey reads live OS key state, so the
            // mouse need not be over the globe. camera.transform.forward is last frame's orientation
            // (set below), matching the incremental axis the drag rotation uses; we apply before
            // reading sphereRotation for this frame's render so the roll is visible the same frame.
            float roll = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.Q)) {
                roll -= RollSpeed;
            }
            if (UnityEngine.Input.GetKey(KeyCode.E)) {
                roll += RollSpeed;
            }
            if (roll != 0f) {
                sphereRotation.Current *= Quaternion.AngleAxis(roll, camera.transform.forward);
            }

            Quaternion rotation = Quaternion.Inverse(sphereRotation.Current);
            Vector3 forward = rotation * Vector3.forward;
            camera.transform.rotation = rotation;
            camera.transform.position = -forward * altitude.Current;

            // Vanilla layers two cameras: WorldSkyboxCamera (depth 0, clears to SkyColor and draws the
            // WorldSkybox layer = the starfield/backdrop) then WorldCamera (depth 1, clearFlags=Depth,
            // draws the globe on top WITHOUT clearing the skybox color). The skybox layer carries real
            // geometry, so we must render both passes - a flat SkyColor clear loses the backdrop. The
            // skybox camera is a child of the world camera, so it inherits the rotation/position we set
            // below and the backdrop tracks the globe exactly like the real world map.
            Camera? skybox = WorldCameraManager.WorldSkyboxCamera;

            bool cameraWasActive = camera.gameObject.activeSelf;
            bool cameraWasEnabled = camera.enabled;
            RenderTexture previousTarget = camera.targetTexture;
            CameraClearFlags previousClear = camera.clearFlags;
            Rect previousPixelRect = camera.pixelRect;
            WorldRenderMode previousMode = world.renderer.wantedMode;

            bool skyboxWasEnabled = skybox != null && skybox.enabled;
            RenderTexture? skyboxPreviousTarget = skybox != null ? skybox.targetTexture : null;
            Rect skyboxPreviousPixelRect = skybox != null ? skybox.pixelRect : default;

            Rect paneRect = PaneScreenRect(rect);

            // Render both cameras straight into the pane's on-screen pixel rect (no RenderTexture):
            // every vanilla overlay that maps through the camera - expandable faction/settlement
            // icons (WorldToScreenPoint), the mouse-over tile, the selected tile - then lands inside
            // the pane with zero re-projection. Both cameras have enabled=false so Unity does NOT
            // auto-render them fullscreen; we drive Render() manually below, scoped to paneRect.
            camera.gameObject.SetActive(true);
            camera.enabled = false;
            camera.targetTexture = null;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.pixelRect = paneRect;
            if (skybox != null) {
                skybox.enabled = false;
                skybox.targetTexture = null;
                skybox.pixelRect = paneRect;
            }
            world.renderer.wantedMode = WorldRenderMode.Planet;

            // Mirror World.WorldUpdate's render passes (minus debug noise) so the pane shows the full
            // map: feature labels, faction/settlement pins, expandable-object meshes, the mouse-over
            // and selected-tile highlights. Each queues camera-space draws that camera.Render()
            // captures; the on-GUI pass paints the expandable icons that DrawDynamicWorldObjects skips.
            // The expandable-pin fade (transitionPct) is animated by the *Update calls below off
            // WorldCameraDriver.CurrentZoom, which reads the driver's own altitude field. We drive the
            // camera transform directly and never touch the driver, so its altitude stays stale (pinned
            // at the closest zoom), CurrentZoom reports VeryClose, and transitionPct decays to 0 - which
            // hides every expandable pin regardless of the showImportant/showBases/showExpandingLandmarks
            // toggles. Sync the driver altitude to the pane's zoom so CurrentZoom tracks what the user
            // sees and the toggles gate actually-visible icons.
            WorldCameraDriver? driver = Find.WorldCameraDriver;
            if (driver != null) {
                driver.altitude = altitude.Current;
            }
            ExpandableWorldObjectsUtility.ExpandableWorldObjectsUpdate();
            if (ModsConfig.OdysseyActive) {
                ExpandableLandmarksUtility.ExpandableLandmarksUpdate();
            }
            WorldTerrainColliderManager.EnsureRaycastCollidersUpdated();
            world.renderer.DrawWorldLayers();
            world.dynamicDrawManager.DrawDynamicWorldObjects();
            world.features.UpdateFeatures();
            // Skybox first (clears the pane to SkyColor + draws the backdrop), then the world on top
            // with a depth-only clear so the backdrop survives behind the globe.
            if (skybox != null) {
                skybox.Render();
            }
            camera.Render();
            // ExpandableWorldObjectsOnGUI draws faction/settlement pins in screen-GUI space from the
            // camera projection; nothing scopes them to the pane, so they spill past its edges. Clip
            // to the pane rect. scrollOffset = -rect.position cancels BeginClip's coordinate
            // translation so the absolute-positioned pins still land where the projection put them
            // while being clipped to the pane.
            GUI.BeginClip(rect, new Vector2(-rect.x, -rect.y), Vector2.zero, false);
            ExpandableWorldObjectsUtility.ExpandableWorldObjectsOnGUI();
            // Landmarks have a SEPARATE on-GUI pass from world objects, and WorldUpdate calls both.
            // Without this the Landmarks toggle gates nothing because the icons are never painted at all.
            if (ModsConfig.OdysseyActive) {
                ExpandableLandmarksUtility.ExpandableLandmarksOnGUI();
            }
            GUI.EndClip();

            if (skybox != null) {
                skybox.pixelRect = skyboxPreviousPixelRect;
                skybox.targetTexture = skyboxPreviousTarget;
                skybox.enabled = skyboxWasEnabled;
            }
            camera.pixelRect = previousPixelRect;
            camera.targetTexture = previousTarget;
            camera.clearFlags = previousClear;
            // Restore the global render mode so vanilla's WorldUpdate -> CheckActivateWorldCamera does
            // NOT auto-render the planet full-screen behind the window. The globe lives only in this
            // pane; leaving wantedMode=Planet was what doubled the render and stranded the app on the
            // world map.
            world.renderer.wantedMode = previousMode;
            camera.enabled = cameraWasEnabled;
            if (!cameraWasActive) {
                camera.gameObject.SetActive(false);
            }
        };

        return node;
    }

    private static Rect PaneScreenRect(Rect guiRect) {
        float scale = Prefs.UIScale;
        return new Rect(
            guiRect.x * scale,
            Screen.height - guiRect.yMax * scale,
            guiRect.width * scale,
            guiRect.height * scale);
    }

    private static void RestorePreviewMode(bool resetMode) {
        Camera? camera = Find.WorldCamera;
        if (camera != null) {
            camera.targetTexture = null;
        }
        World? world = Find.World;
        if (world == null) {
            return;
        }
        if (resetMode) {
            world.renderer.wantedMode = WorldRenderMode.None;
        }
        // Drives WorldCamera.gameObject active off WorldRendered (wantedMode != None). In our flow
        // wantedMode is None between frames, so this deactivates the camera and guarantees the globe
        // never renders full-screen once the preview is gone.
        world.renderer.CheckActivateWorldCamera();
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => WorldPreview.Create(
            style: new Style { Width = Length.Rem(20f), Height = Length.Rem(14f) }));
    }

    [DocState("CL_Playground_WorldPreview_NoWorld")]
    public static DocSample DocsNoWorld() {
        return new DocSample(() => WorldPreview.Create(
            style: new Style { Width = Length.Rem(20f), Height = Length.Rem(14f) }));
    }
}
