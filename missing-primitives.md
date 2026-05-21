# Missing primitives for Cryptiklemur.RimLogging.UI

Generated 2026-05-20 while implementing Phase 8 (Lightweave UI module) of the RimLogging framework.

Decision: shipping Phase 8 with pragmatic substitutions using existing primitives. This file documents the gaps so they can be filled in Lightweave properly later.

## Hard gaps (no substitute that fits the UX intent)

### `Layout/SplitPane`

**Need:** resizable horizontal split with a draggable divider between two children. RimLogging viewer wants left-channel-tree | center-log-list-with-filter | right-detail with user-draggable column widths.

**Current substitute:** `HStack.Add(node, widthPx)` + `HStack.AddFlex(node)` (confirmed: HStack supports mixed fixed/flex widths; `Row` splits evenly so it does NOT work for asymmetric splits). No user-draggable divider. Column widths are baked at layout time.

**Suggested API:**
```csharp
SplitPane.Create(
    Orientation orientation,           // Horizontal | Vertical
    LightweaveNode first,
    LightweaveNode second,
    float initialFraction = 0.5f,      // first-pane fraction of total
    float minFirstPx = 100f,
    float minSecondPx = 100f,
    Action<float>? onFractionChanged = null,
    Style? style = null, string[]? classes = null, string? id = null,
    [CallerLineNumber] int line = 0,
    [CallerFilePath] string file = "")
```

Three-pane usage builds `SplitPane(SplitPane(left, center), right)` or accept an optional 3-child variant.

**Implementation hints:** divider is a thin draggable Row using a `Hooks.UseDrag()` style hook. Persist fraction via Hook state if `onFractionChanged` is null; route to caller if provided.

---

### `Data/VirtualizedList` (or `LazyList`)

**Need:** scroll-windowed list that only paints visible rows. RimLogging UISink caps the ring at 4096 entries; filter results can be 0-4096. Paint-cost for ~4k labels per frame is plausibly tolerable in Unity but with three filter panes redrawing simultaneously the budget gets tight.

**Current substitute:** `ScrollArea` + `Each` + the existing `Data/List`. Paints every row regardless of viewport. Acceptable up to ~1-2k rows; degrades past that.

**Suggested API:**
```csharp
VirtualizedList.Create<T>(
    IReadOnlyList<T> items,
    float rowHeight,                              // fixed-height variant first; dynamic later
    Func<T, int, LightweaveNode> renderRow,
    int? overscan = 5,                            // rows to render above/below viewport
    Action<int>? onScrolledToIndex = null,
    Style? style = null, string[]? classes = null, string? id = null,
    [CallerLineNumber] int line = 0,
    [CallerFilePath] string file = "")
```

Performance contract: O(visibleRows) per frame in `Layout` and `Draw`, NOT O(totalItems). Recompute viewport indices on scroll-state change, not every event.

**Implementation hints:** wrap a ScrollArea; compute `firstVisibleIndex = scrollY / rowHeight - overscan`. Render that slice. Add total-content-height spacer so scrollbar reflects full list length.

---

## Soft gaps (existing primitives can substitute, but a dedicated one would be cleaner)

### Toggle-style `Chip`

**Need:** Level-toggle UI like `[Trace] [Debug] [Info] [Warn] [Error]` where each is a clickable, color-coded, on/off pill. Used by `FilterBar`.

**Current substitute:** `Button` with `Pill` styling or `Tag` + custom variant. Works but verbose at every call site.

**Suggested API:**
```csharp
Chip.Create(
    string label,
    bool selected,
    Action onToggle,
    ChipVariant variant = ChipVariant.Default,    // Default | Severity | Filter
    Style? style = null, string[]? classes = null, string? id = null,
    [CallerLineNumber] int line = 0,
    [CallerFilePath] string file = "")
```

Variants map to color tokens (e.g. `Severity` picks color by associated `LogLevel`).

---

## Verified-present primitives consumed by RimLogging.UI

These exist in Lightweave and are used as-is:

- `Data/Tree` — channel tree pane
- `Data/List` — log row stack inside ScrollArea (pre-VirtualizedList)
- `Layout/ScrollArea` — wraps the log list and detail pane
- `Layout/HStack`, `Row`, `Column`, `Container`, `Card`, `Spacer`, `Divider` — pane composition
- `Layout/WindowHeader`, `WindowBody`, `WindowFooter` — outer window framing
- `Input/TextField`, `SearchField` — filter DSL editor + search box
- `Input/Button`, `IconButton`, `Checkbox`, `Dropdown` — controls
- `Feedback/Toast` — bundle-upload success / error
- `Feedback/Pill`, `Tag`, `Badge` — used as chip substitute (see soft gap above)
- `Overlay/Tooltip` — hover help on settings rows and DSL grammar hints
- `Hooks/UseAnim`, `UseFocus`, `UseHotkey` — DSL editor focus, keyboard shortcuts

## Consumer impact summary

With the two hard gaps and one soft gap stubbed:

- Three-pane layout: fixed-width columns, no user resize.
- Log list: full-population, fine to ~1-2k entries, may stutter past that.
- Level toggle row: works, but call sites are ~5 lines each instead of 1.

When SplitPane + VirtualizedList land in Lightweave, the RimLogging UI module is a focused refactor (LogViewerWindow composition + LogListPane internals) — none of the surrounding panes or filter wiring change.
