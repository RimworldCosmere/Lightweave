using System.Runtime.CompilerServices;
using System;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Verse;

namespace Cosmere.Lightweave.Playground;

public enum PlaygroundTheme {
    Default,
    Cosmere,
    Scadrial,
    Roshar,
}

public static class PlaygroundHeader {
    private static readonly PlaygroundTheme[] ThemeOptions = {
        PlaygroundTheme.Default,
        PlaygroundTheme.Cosmere,
        PlaygroundTheme.Scadrial,
        PlaygroundTheme.Roshar,
    };

    public static LightweaveNode Create(
        Hooks.Hooks.StateHandle<PlaygroundTheme> theme,
        Action? onClose = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode controls = BuildControls(theme);

        return WindowHeader.Create(
            title: (string)"CL_Playground_Header_Brand".Translate(),
            subtitle: (string)"CL_Playground_Header_Subtitle".Translate(),
            actions: controls,
            onClose: onClose,
            draggable: true,
            drawDivider: true,
            style: style,
            classes: classes,
            id: id,
            line: line,
            file: file
        );
    }

    private static LightweaveNode BuildControls(
        Hooks.Hooks.StateHandle<PlaygroundTheme> theme
    ) {
        LightweaveNode themeDropdown = Dropdown.Create<PlaygroundTheme>(
            value: theme.Value,
            options: ThemeOptions,
            labelFn: ThemeLabel,
            onChange: next => theme.Set(next),
            variant: DropdownVariant.Input,
            inputVariant: Variant.Secondary
        );

        bool tourActive = PlaygroundTour.IsActive;
        LightweaveNode tourButton = Button.Create(
            tourActive
                ? (string)"CL_Playground_Header_Tour_Stop".Translate()
                : (string)"CL_Playground_Header_Tour_Start".Translate(),
            () => {
                if (PlaygroundTour.IsActive) {
                    PlaygroundTour.Stop();
                } else {
                    PlaygroundTour.Start();
                }
            },
            tourActive ? Variant.Danger : Variant.Secondary
        );

        return Layout.HStack.Create(
            SpacingScale.Sm,
            r => {
                r.Add(themeDropdown, 240f);
                r.Add(tourButton, 130f);
            }
        );
    }

    private static string ThemeLabel(PlaygroundTheme value) {
        return value switch {
            PlaygroundTheme.Cosmere => (string)"CL_Playground_Header_Theme_Cosmere".Translate(),
            PlaygroundTheme.Scadrial => (string)"CL_Playground_Header_Theme_Scadrial".Translate(),
            PlaygroundTheme.Roshar => (string)"CL_Playground_Header_Theme_Roshar".Translate(),
            _ => (string)"CL_Playground_Header_Theme_Default".Translate(),
        };
    }
}
