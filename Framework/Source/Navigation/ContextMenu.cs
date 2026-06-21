using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Cosmere.Lightweave.Layout;
using static Cosmere.Lightweave.Typography.Typography;
using Cosmere.Lightweave.Surfaces;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Navigation;

[Doc(
    Id = "contextmenu",
    Summary = "Right-click target that opens a Menu at the cursor.",
    WhenToUse = "Expose secondary actions on a region without consuming bar space.",
    SourcePath = "Lightweave/Navigation/ContextMenu.cs"
)]
public static class ContextMenu {
    public static LightweaveNode Create(
        [DocParam("Region that captures the right-click.")]
        LightweaveNode child,
        [DocParam("Menu rows to display when triggered.")]
        IReadOnlyList<MenuEntry> items,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.StateHandle<bool> isOpen = Hooks.Hooks.UseState(false, line, file + "#open");
        Hooks.Hooks.StateHandle<Vector2> anchorPos = Hooks.Hooks.UseState(Vector2.zero, line, file + "#pos");

        LightweaveNode node = NodeBuilder.New("ContextMenu", line, file);
        node.ApplyStyling("context-menu", style, classes, id);
        node.Children.Add(child);
        if (child.Measure != null) {
            node.Measure = child.Measure;
        }
        else if (child.PreferredHeight.HasValue) {
            node.PreferredHeight = child.PreferredHeight.Value;
        }
        node.MeasureWidth = () => child.MeasureWidth?.Invoke() ?? 0f;

        node.Paint = (rect, _) => {
            child.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(child, rect);

            Event e = Event.current;
            RenderContext ctx = RenderContext.Current;
            bool occludedByOverlay = ctx.OverlayContentDepth == 0
                && HoverBlockRegistry.IsBlocked(GUIUtility.GUIToScreenPoint(e.mousePosition));
            if (e.type == EventType.MouseUp && e.button == 1 && rect.Contains(e.mousePosition) && !occludedByOverlay) {
                anchorPos.Set(e.mousePosition);
                isOpen.Set(true);
                e.Use();
            }

            if (isOpen.Value) {
                Vector2 pos = anchorPos.Value;
                Rect anchorRect = new Rect(pos.x, pos.y, 0f, 0f);
                LightweaveNode menu = Menu.Create(
                    true,
                    anchorRect,
                    items,
                    () => isOpen.Set(false),
                    MenuAnchor.Left,
                    MenuDirection.Down,
                    "context"
                );
                menu.MeasuredRect = rect;
                LightweaveRoot.PaintSubtree(menu, rect);
            }
        };
        return node;
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => {
            List<MenuEntry> items = new List<MenuEntry> {
                MenuEntry.Of((string)"CL_Playground_ContextMenu_Inspect".Translate(), () => { }),
                MenuEntry.Of((string)"CL_Playground_ContextMenu_Rename".Translate(), () => { }),
                MenuEntry.Of((string)"CL_Playground_ContextMenu_Duplicate".Translate(), () => { }),
                MenuEntry.Divider(),
                MenuEntry.Of((string)"CL_Playground_ContextMenu_Delete".Translate(), () => { }),
            };

            LightweaveNode target = Box.Create(
                c => c.Add(
                    Caption.Create((string)"CL_Playground_ContextMenu_RightClick".Translate())
                ),
                style: new Style {
                    Padding = EdgeInsets.All(SpacingScale.Sm),
                    Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                    Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault),
                    Radius = RadiusSpec.All(RadiusScale.Sm),
                }
            );

            return ContextMenu.Create(target, items);
        });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => {
            List<MenuEntry> items = new List<MenuEntry> {
                MenuEntry.Of("Inspect", () => { }),
                MenuEntry.Of("Rename", () => { }),
                MenuEntry.Divider(),
                MenuEntry.Of("Delete", () => { }),
            };

            LightweaveNode target = Box.Create(
                c => c.Add(Caption.Create("Right-click here")),
                style: new Style {
                    Padding = EdgeInsets.All(SpacingScale.Sm),
                    Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                    Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault),
                    Radius = RadiusSpec.All(RadiusScale.Sm),
                }
            );

            return ContextMenu.Create(target, items);
        });
    }
}