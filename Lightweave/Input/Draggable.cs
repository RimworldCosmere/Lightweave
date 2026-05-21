using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "draggable",
    Summary = "Wraps a child so users can drag it around inside the wrapper's bounds.",
    WhenToUse = "Demo surfaces or playground stages where the user should be able to reposition an element with the mouse. The wrapper takes its allocated rect as the dragging arena and clamps the child inside.",
    SourcePath = "Lightweave/Input/Draggable.cs"
)]
public static class Draggable {
    public static LightweaveNode Create(
        [DocParam("Child rendered inside the wrapper. The child's own Style.Width / Style.Height drive its size; the wrapper fills the rect the parent gives it.")]
        LightweaveNode child,
        [DocParam("Optional initial offset in pixels relative to the wrapper's top-left.")]
        Vector2? initialOffset = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Vector2 seedOffset = initialOffset ?? Vector2.zero;

        LightweaveNode node = NodeBuilder.New("Draggable", line, file);
        node.ApplyStyling("draggable", style, classes, id);
        node.Children.Add(child);

        node.Paint = (rect, paintChildren) => {
            RefHandle<Vector2> offset = UseRef(seedOffset, line, file + "#offset");
            RefHandle<bool> dragging = UseRef(false, line, file + "#dragging");
            RefHandle<Vector2> grabMouse = UseRef(Vector2.zero, line, file + "#grabMouse");
            RefHandle<Vector2> grabOffset = UseRef(Vector2.zero, line, file + "#grabOffset");

            Style childStyle = child.GetResolvedStyle();
            float cw = childStyle.Width.HasValue && !childStyle.Width.Value.IsGrower
                ? childStyle.Width.Value.ToPixels(rect.width, 0f)
                : (child.MeasureWidth?.Invoke() ?? rect.width);
            float ch = childStyle.Height.HasValue && !childStyle.Height.Value.IsGrower
                ? childStyle.Height.Value.ToPixels(rect.height, 0f)
                : (child.Measure?.Invoke(cw) ?? child.PreferredHeight ?? rect.height);

            float maxX = Mathf.Max(0f, rect.width - cw);
            float maxY = Mathf.Max(0f, rect.height - ch);

            Vector2 off = offset.Current;
            off.x = Mathf.Clamp(off.x, 0f, maxX);
            off.y = Mathf.Clamp(off.y, 0f, maxY);
            if (off != offset.Current) {
                offset.Current = off;
            }

            Rect childRect = new Rect(rect.x + off.x, rect.y + off.y, rect.width, rect.height);
            child.MeasuredRect = childRect;
            Rect hitRect = new Rect(rect.x + off.x, rect.y + off.y, cw, ch);

            paintChildren();

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && hitRect.Contains(e.mousePosition)) {
                dragging.Current = true;
                grabMouse.Current = e.mousePosition;
                grabOffset.Current = off;
                ActiveDragRegistry.Acquire(RenderContext.Current.RootId);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && dragging.Current) {
                Vector2 delta = e.mousePosition - grabMouse.Current;
                Vector2 next = grabOffset.Current + delta;
                next.x = Mathf.Clamp(next.x, 0f, maxX);
                next.y = Mathf.Clamp(next.y, 0f, maxY);
                offset.Current = next;
                e.Use();
            }
            else if ((e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && dragging.Current) {
                dragging.Current = false;
                ActiveDragRegistry.Release();
                if (e.type == EventType.MouseUp) {
                    e.Use();
                }
            }
        };

        return node;
    }
}
