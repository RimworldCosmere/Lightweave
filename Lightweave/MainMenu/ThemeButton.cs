using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Hooks;
using static Cosmere.Lightweave.Hooks.Hooks;
using Cosmere.Lightweave.Overlay;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.MainMenu;

public static class ThemeButton {
    public static LightweaveNode Create(
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string hookFile = file + "#theme-button";
        StateHandle<bool> open = UseState(false, line, hookFile);
        StateHandle<Rect> anchor = UseState(Rect.zero, line + 1, hookFile);

        string label = (string)"CL_MainMenu_Theme_Label".Translate();

        LightweaveNode node = NodeBuilder.New("ThemeButton", line, file);
        node.ApplyStyling("theme-button", style, classes, id);
        node.PreferredHeight = new Rem(2f).ToPixels();

        LightweaveNode trigger = FootLink.Create(
            label: label,
            onClick: () => open.Set(!open.Value),
            indicateMenu: true,
            expanded: open.Value
        );

        LightweaveNode popover = Popover.Create(
            isOpen: open.Value,
            anchorRect: anchor.Value,
            placement: PopoverPlacement.Top,
            content: ThemePopover.Create(() => open.Set(false)),
            onDismiss: () => open.Set(false),
            preferredSize: new Vector2(new Rem(16f).ToPixels(), -1f)
        );

        node.MeasureWidth = () => trigger.MeasureWidth?.Invoke() ?? new Rem(8f).ToPixels();
        node.Children.Add(trigger);
        node.Children.Add(popover);

        node.Paint = (rect, _) => {
            anchor.Set(rect);
            trigger.MeasuredRect = rect;
            popover.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(trigger, rect);
            LightweaveRoot.PaintSubtree(popover, rect);
        };

        return node;
    }
}
