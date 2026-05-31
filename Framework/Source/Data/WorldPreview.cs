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
    private const float StartingAltitude = 550f;
    private const float MaxAltitude = 1100f;
    private const float ZoomStep = 0.1f;

    public static LightweaveNode Create(
        [DocParam("Fired when the hovered tile changes. The argument is the tile under the cursor.", TypeOverride = "Action<PlanetTile>?", DefaultOverride = "null")]
        Action<PlanetTile>? onHover = null,
        [DocParam("Fired on left-click over a valid tile (a click that did not drag-rotate). The argument is the picked tile.", TypeOverride = "Action<PlanetTile>?", DefaultOverride = "null")]
        Action<PlanetTile>? onPick = null,
        [DocParam("Live predicate. While it returns true the globe is suppressed (not rendered or interacted with) and the loading message is shown instead. Checked every repaint, so the caller can flip a background flag without forcing a rebuild.", TypeOverride = "Func<bool>?", DefaultOverride = "null")]
        Func<bool>? loading = null,
        [DocParam("Centered message drawn over the sunken surface while loading is true. The caller translates it.", TypeOverride = "string?", DefaultOverride = "null")]
        string? loadingText = null,
        [DocParam("Color multiplier applied when blitting the rendered globe. Defaults to white (no tint).", TypeOverride = "Color?", DefaultOverride = "null")]
        Color? tint = null,
        [DocParam("Corner rounding for the rendered globe and the placeholder.")]
        RadiusScale radius = RadiusScale.Md,
        [DocParam("When true (default) the primitive resets the world renderer to None on unmount, so the planet stops drawing once the surface is gone. Pass false when the embedding surface owns the world lifecycle across mounts (e.g. a tabbed wizard that keeps a generated world alive while the user visits other tabs) — otherwise vanilla's main-menu loop nulls the game the moment the globe unmounts.", DefaultOverride = "true")]
        bool restoreRenderModeOnUnmount = true,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("WorldPreview", line, file);
        node.ApplyStyling("world-preview", style, classes, id);

        Color resolvedTint = tint ?? Color.white;
        RadiusSpec radiusSpec = RadiusSpec.All(radius);

        RefHandle<RenderTexture?> rtRef = UseRef<RenderTexture?>(null);
        RefHandle<int> lastHoverTile = UseRef(-1);
        RefHandle<bool> didDrag = UseRef(false);
        RefHandle<Quaternion> sphereRotation = UseRef(Quaternion.identity);
        RefHandle<float> altitude = UseRef(StartingAltitude);

        UseEffect(() => {
            return () => {
                RestorePreviewMode(restoreRenderModeOnUnmount);
                ReleaseTexture(rtRef);
                lastHoverTile.Current = -1;
            };
        }, []);

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

            if (current.type == EventType.ScrollWheel) {
                altitude.Current = Mathf.Clamp(
                    altitude.Current * (1f + current.delta.y * ZoomStep),
                    WorldCameraDriver.MinAltitude,
                    MaxAltitude);
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0) {
                didDrag.Current = false;
            }

            if (current.type == EventType.MouseDrag && current.button == 0) {
                Vector2 dragDelta = current.delta;
                dragDelta.x *= -1f;
                Vector2 raw = dragDelta / GenWorldUI.CurUITileSize() * (0.273f * Prefs.MapDragSensitivity);
                Quaternion rot = sphereRotation.Current;
                rot *= Quaternion.AngleAxis(raw.x, camera.transform.up);
                rot *= Quaternion.AngleAxis(-raw.y, camera.transform.right);
                sphereRotation.Current = rot;
                didDrag.Current = true;
                current.Use();
                return;
            }

            float nx = (current.mousePosition.x - rect.x) / rect.width;
            float ny = 1f - (current.mousePosition.y - rect.y) / rect.height;
            camera.aspect = rect.width / rect.height;
            Ray ray = camera.ViewportPointToRay(new Vector3(nx, ny, 0f));
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

            RenderTexture target = EnsureTexture(rtRef, rect);

            Quaternion rotation = Quaternion.Inverse(sphereRotation.Current);
            Vector3 forward = rotation * Vector3.forward;
            camera.transform.rotation = rotation;
            camera.transform.position = -forward * altitude.Current;

            bool cameraWasActive = camera.gameObject.activeSelf;
            bool cameraWasEnabled = camera.enabled;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            CameraClearFlags previousClear = camera.clearFlags;
            Color previousBackground = camera.backgroundColor;
            WorldRenderMode previousMode = world.renderer.wantedMode;

            camera.gameObject.SetActive(true);
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderContext.Current.Theme.GetColor(ThemeSlot.SurfaceSunken);
            camera.targetTexture = target;
            camera.aspect = (float)target.width / target.height;
            world.renderer.wantedMode = WorldRenderMode.Planet;

            // Mirror World.WorldUpdate's render passes (minus debug noise) so the preview shows the
            // full map, not just bare planet layers: feature labels, faction/settlement pins, and
            // expandable-object meshes. Each queues camera-space mesh/text draws that the manual
            // camera.Render() below captures into the RenderTexture; DrawWorldLayers alone omits them.
            ExpandableWorldObjectsUtility.ExpandableWorldObjectsUpdate();
            if (ModsConfig.OdysseyActive) {
                ExpandableLandmarksUtility.ExpandableLandmarksUpdate();
            }
            world.renderer.DrawWorldLayers();
            world.dynamicDrawManager.DrawDynamicWorldObjects();
            world.features.UpdateFeatures();
            camera.Render();

            camera.targetTexture = previousTarget;
            camera.clearFlags = previousClear;
            camera.backgroundColor = previousBackground;
            RenderTexture.active = previousActive;
            // Restore the global render mode so vanilla's WorldUpdate -> CheckActivateWorldCamera
            // does NOT auto-render the planet full-screen behind the embedding surface. The globe
            // lives only in this RenderTexture; leaving wantedMode=Planet was what doubled the
            // render and stranded the app in world-map mode.
            world.renderer.wantedMode = previousMode;
            camera.enabled = cameraWasEnabled;
            if (!cameraWasActive) {
                camera.gameObject.SetActive(false);
            }

            PaintBox.DrawTexture(rect, target, resolvedTint);
        };

        return node;
    }

    private static void RestorePreviewMode(bool resetMode) {
        Camera? camera = Find.WorldCamera;
        if (camera != null) {
            camera.enabled = true;
            camera.targetTexture = null;
        }
        if (!resetMode) {
            return;
        }
        World? world = Find.World;
        if (world == null) {
            return;
        }
        world.renderer.wantedMode = WorldRenderMode.None;
        world.renderer.CheckActivateWorldCamera();
    }

    private static RenderTexture EnsureTexture(RefHandle<RenderTexture?> rtRef, Rect rect) {
        int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
        int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
        RenderTexture? existing = rtRef.Current;
        if (existing != null && existing.width == width && existing.height == height) {
            return existing;
        }

        if (existing != null) {
            existing.Release();
            UnityEngine.Object.Destroy(existing);
        }

        RenderTexture created = new RenderTexture(width, height, 24);
        created.Create();
        rtRef.Current = created;
        return created;
    }

    private static void ReleaseTexture(RefHandle<RenderTexture?> rtRef) {
        RenderTexture? existing = rtRef.Current;
        if (existing == null) {
            return;
        }
        existing.Release();
        UnityEngine.Object.Destroy(existing);
        rtRef.Current = null;
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
