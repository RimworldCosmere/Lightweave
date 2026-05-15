# Lightweave

A composable IMGUI framework for RimWorld mods. Provides nodes, layout, theming, typography, navigation, and adapters that bridge nodes into vanilla surfaces (gizmos, ITabs, MainTabs, FloatMenuOptions, ChoiceLetters, tooltips).

Built for and consumed by the [RimWorld: Cosmere](https://github.com/RimworldCosmere/RimworldCosmere) mod suite, but designed to be a shared dependency for any mod that wants composable, themed UI.

## Install for players

Subscribe on the [Steam Workshop](#) or download the latest release from the [GitHub releases page](https://github.com/RimworldCosmere/Lightweave/releases/latest). Mods that depend on Lightweave will list it as a required dependency.

## Use in your mod

```xml
<!-- About/About.xml -->
<modDependencies>
    <li>
        <packageId>Cosmere.Lightweave</packageId>
        <displayName>Lightweave</displayName>
        <downloadUrl>https://github.com/RimworldCosmere/Lightweave/releases/latest</downloadUrl>
    </li>
</modDependencies>
<forceLoadAfter>
    <li>Cosmere.Lightweave</li>
</forceLoadAfter>
```

```xml
<!-- YourMod.csproj -->
<ItemGroup>
    <PackageReference Include="Cosmere.Lightweave" Version="*" />
</ItemGroup>
```

## Develop

```sh
dotnet build Lightweave.sln
```

## Framework rules

Lightweave is two things at once, in this order:

1. A **consumable IMGUI primitive framework** (`Layout/`, `Input/`, `Feedback/`, `Navigation/`, `Overlay/`, `Typography/`, `Data/`, `Doc/`) — reusable, themed, documented, composable.
2. A **new RimWorld UI implementation** (`MainMenu/`, `LoadColony/`, `ModsConfig/`, `Options/`, `Playground/`) that consumes those primitives.

The framework comes first. Feature dirs are the proving ground — if a feature can't be expressed in primitives, the framework is missing something. Every feature mistake is a framework gap.

### Framework-first composition

Raw `Widgets.*` / `GUI.*` / `UnityEngine.GUI*` / `Listing_Standard` calls live **only** in `Rendering/`, `Patch/`, `Adapter/`, `Polyfills/`. Everywhere else — primitives and feature dirs — must compose existing primitives or call into `Rendering/` helpers.

If you're about to write `Widgets.X` or `new Color(...)` inside a feature dir, stop. Either a primitive should own that, or an existing primitive is missing a prop. If a primitive does not exist for what you need, **build the primitive first, then resume the feature**.

### Primitive API surface

Every public primitive in `Layout/Input/Feedback/Navigation/Overlay/Typography/Data` MUST have:

- A `[Doc(Id, Summary, WhenToUse, SourcePath, ...)]` attribute on the class.
- A static `Create(...)` entry point in a `public static class <Name>`.
- `Style? style = null, string[]? classes = null, string? id = null` in the signature.
- `[CallerLineNumber] int line = 0, [CallerFilePath] string file = ""` at the end of the signature.
- `node.ApplyStyling("<id>", style, classes, id)` immediately after `NodeBuilder.New(...)`.
- A `MeasureWidth` callback returning the natural content width.
- A `Measure` callback (or `PreferredHeight`) so vertical layouts can size it.
- At least one `[DocUsage]` sample, plus `[DocVariant]` for each variant and `[DocState]` for each interaction state.

One primitive per file. File name matches class name exactly. Variants are enums named `<Name>Variant` in a sibling file.

### Sizing default: hug content

Primitives hug content on both axes by default. They only fill the allocated rect when the caller passes `Style.Width(Length.Grower)` or `Style.Height(Length.Grower)`. If you're sizing from `allocatedRect.width`/`.height` without first checking `IsGrower`, you've forced fill — fix it.

### Paint callback: zero allocations

The `node.Paint = (rect, paintChildren) => {...}` callback runs every frame. Inside it:

- No `new Color(...)`, `new Rect[](...)`, `new List<...>()`, `new GUIStyle()`, string concat, LINQ, or `ToString()` on hot values.
- No `ContentFinder<Texture2D>.Get(...)` — cache textures in a `[StaticConstructorOnStartup]` holder.
- Cache `GUIStyle` instances via `GuiStyleCache.GetOrCreate(font, pixelSize)`.
- Cache gradient textures via `GradientTextureCache`.
- Allocate temporaries in the closure of `Create()`, not inside `Paint`.

### Colors: always theme slots

Inside primitives: `Theme.Theme theme = RenderContext.Current.Theme; theme.GetColor(slot)` for every color. `Color.white`, `Color.black`, `new Color(0.79f, 0.65f, 0.37f)` inside a primitive is wrong. Add a `ThemeSlot` enum value, resolve it in each theme, route the color through `theme.GetColor`.

The only exception is transient alpha multipliers (overlay alpha, hover wash) where the base color already came from a slot.

### Localization: callers translate

Primitives are string-agnostic. They take `string` and render it. **Callers are responsible for `.Translate()`**. A primitive must never call `.Translate()` internally. This keeps primitives reusable across contexts where the string source varies.

### Breaking changes

Primitives can break their API freely. The rules:

1. Fix every caller in the same commit.
2. Fix every `[DocUsage]`/`[DocVariant]`/`[DocState]` sample on the changed primitive and on any primitive that composes it.
3. Build the solution clean, zero new warnings.
4. Open the Playground, verify the primitive still renders correctly in every state.

There is no compatibility layer. The framework is internal to the mod and all consumers are owned.

### Canonical primitive skeleton

```csharp
using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.<Category>;

[Doc(
    Id = "<lowercase-name>",
    Summary = "<one-line summary>",
    WhenToUse = "<when a consumer should reach for this>",
    SourcePath = "Lightweave/<Category>/<Name>.cs"
)]
public static class <Name> {
    public static LightweaveNode Create(
        // primitive-specific args first
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("<Name>", line, file);
        node.ApplyStyling("<lowercase-name>", style, classes, id);

        node.MeasureWidth = () => 0f;
        node.Measure = availableWidth => 0f;

        node.Paint = (rect, paintChildren) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Style s = node.GetResolvedStyle();
            // all colors via theme.GetColor(slot)
            // all sizes via Rem / SpacingScale / RadiusScale
            // no allocations in this scope
            paintChildren();
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() =>
        new DocSample(() => Create(/* canonical args */));
}
```

### Verification before declaring a primitive done

1. `dotnet build Lightweave.sln` clean, zero new warnings.
2. Launch RimWorld, open the Playground page for the primitive.
3. Screenshot every `[DocVariant]` and `[DocState]` entry.
4. Share screenshots before declaring the change done.

IMGUI state-bleed bugs (`GUI.color` leak, `Text.Font` leftover, missing scope restore) only surface at runtime. The Playground is the canary.

## License

MIT. See [LICENSE](LICENSE).
