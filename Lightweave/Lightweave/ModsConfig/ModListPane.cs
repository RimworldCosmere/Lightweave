using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.ModsConfig;

public static class ModListPane {
    private static readonly Rem RowHeight = new Rem(2.6f);
    private static readonly Rem HeaderHeight = new Rem(2.0f);
    private static readonly Rem PaddingX = new Rem(1.0f);
    private static readonly Rem StripeWidth = new Rem(0.15f);


    private static string? dragPressedPackageId;
    private static string? dragSourcePackageId;
    private static float dragPressedY;
    private static bool dragActive;
    private static int dragHoverInsertSlot = -1;

    private static readonly Rem ColOrder = new Rem(2.5f);
    private static readonly Rem ColCheck = new Rem(1.75f);
    private static readonly Rem ColAuthor = new Rem(13f);
    private static readonly Rem ColVersion = new Rem(5.625f);
    private static readonly Rem ColStatus = new Rem(5.625f);

    public static LightweaveNode Create(
        List<ModMetaData> mods,
        string? selected,
        Action<string> onSelect,
        RimWorld.Page_ModsConfig page,
        ModsTab tab,
        Action<ModsTab> onTabChange,
        string query,
        Action<string> onQueryChange
    ) {
        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.None, s => {
                s.Add(BuildSearchBar(query, onQueryChange, mods.Count, tab, onTabChange));
                s.Add(BuildColumnHeader());
                s.AddFlex(ScrollArea.Create(content: BuildList(mods, selected, onSelect, query)));
            })),
            style: new Style {
                Padding = EdgeInsets.Zero,
                Border = new BorderSpec(Right: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildSearchBar(string query, Action<string> onQueryChange, int matchCount, ModsTab tab, Action<ModsTab> onTabChange) {
        bool hasQuery = !string.IsNullOrEmpty(query);
        bool showWorkshop = SteamManager.Initialized;
        ModsTab[] tabs = new[] { ModsTab.Installed, ModsTab.LoadOrder };
        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.Sm, s => {
                s.Add(HStack.Create(SpacingScale.Sm, h => {
                    h.AddFlex(SearchField.Create(
                        value: query,
                        onChange: onQueryChange,
                        placeholder: "CL_ModsConfig_Search_Placeholder".Translate(),
                        variant: SearchFieldVariant.Borderless
                    ));
                    if (hasQuery) {
                        string matchText = (matchCount == 1
                            ? "CL_ModsConfig_Search_MatchCount".Translate(matchCount.Named("COUNT"))
                            : "CL_ModsConfig_Search_MatchCountPlural".Translate(matchCount.Named("COUNT"))).Resolve();
                        h.AddHug(Text.Create(
                            matchText,
                            style: new Style {
                                FontFamily = FontRole.Mono,
                                FontSize = new Rem(0.65f),
                                LetterSpacing = Tracking.Of(0.14f),
                                TextColor = ThemeSlot.TextMuted,
                            }
                        ));
                    }
                    h.AddHug(Segmented.Create<ModsTab>(
                        value: tab,
                        items: tabs,
                        labelFn: t => t == ModsTab.Installed
                            ? (string)"CL_ModsConfig_Tab_Installed".Translate()
                            : (string)"CL_ModsConfig_Tab_LoadOrder".Translate(),
                        onChange: onTabChange
                    ));
                    if (showWorkshop) {
                        h.AddHug(Button.Create(
                            label: ((string)"CL_ModsConfig_Workshop_Button".Translate()).ToUpperInvariant(),
                            onClick: () => SteamUtility.OpenSteamWorkshopPage(),
                            variant: Variant.Secondary
                        ));
                    }
                }));
            })),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Sm, Right: SpacingScale.Md, Bottom: SpacingScale.Sm, Left: SpacingScale.Md),
                Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildColumnHeader() {
        LightweaveNode node = NodeBuilder.New("ModListHeader");
        node.PreferredHeight = HeaderHeight.ToPixels();
        node.Paint = (rect, _) => {
            PaintBox.Draw(
                rect,
                null,
                new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                null
            );

            ColumnRects cols = ComputeColumns(rect);
            Rem fontSize = new Rem(0.8f);

            TextDraw.Draw(cols.Order, "CL_ModsConfig_Col_Order".Translate(), FontRole.Mono, fontSize, TextAnchor.MiddleLeft, ThemeSlot.MetadataLabel);
            TextDraw.Draw(cols.Name, ((string)"CL_ModsConfig_Col_Name".Translate()).ToUpperInvariant(), FontRole.Mono, fontSize, TextAnchor.MiddleLeft, ThemeSlot.MetadataLabel);
            TextDraw.Draw(cols.Author, ((string)"CL_ModsConfig_Col_Author".Translate()).ToUpperInvariant(), FontRole.Mono, fontSize, TextAnchor.MiddleLeft, ThemeSlot.MetadataLabel);
            TextDraw.Draw(cols.Version, ((string)"CL_ModsConfig_Col_Version".Translate()).ToUpperInvariant(), FontRole.Mono, fontSize, TextAnchor.MiddleLeft, ThemeSlot.MetadataLabel);
            TextDraw.Draw(cols.Status, ((string)"CL_ModsConfig_Col_Status".Translate()).ToUpperInvariant(), FontRole.Mono, fontSize, TextAnchor.MiddleRight, ThemeSlot.MetadataLabel);
        };
        return node;
    }

    private static LightweaveNode BuildList(
        List<ModMetaData> mods,
        string? selected,
        Action<string> onSelect,
        string query
    ) {
        if (mods == null || mods.Count == 0) {
            return Stack.Create(SpacingScale.None, s => {
                if (!string.IsNullOrEmpty(query)) {
                    s.Add(BuildSearchEmptyState(query));
                }
                else {
                    s.Add(BuildEmptyState());
                }
            });
        }

        float rowH = RowHeight.ToPixels();
        int count = mods.Count;
        LightweaveNode node = NodeBuilder.New("ModList");

        for (int i = 0; i < count; i++) {
            ModMetaData mod = mods[i];
            int loadOrder = i + 1;
            bool zebra = (i % 2) == 1;
            bool isSelected = string.Equals(mod.PackageId, selected, StringComparison.OrdinalIgnoreCase);
            bool isDragSource = dragActive
                && dragSourcePackageId != null
                && string.Equals(mod.PackageId, dragSourcePackageId, StringComparison.OrdinalIgnoreCase);
            node.Children.Add(BuildRow(mod, loadOrder, isSelected, zebra, isDragSource));
        }

        float totalHeight = count * rowH;
        node.PreferredHeight = totalHeight;
        node.Measure = _ => totalHeight;

        node.Paint = (rect, paintChildren) => {
            for (int i = 0; i < count; i++) {
                node.Children[i].MeasuredRect = new Rect(rect.x, rect.y + i * rowH, rect.width, rowH);
            }

            Event e = Event.current;
            bool inList = rect.Contains(e.mousePosition);
            int hoverIdx = -1;
            if (inList) {
                hoverIdx = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.y - rect.y) / rowH), 0, count - 1);
            }

            if (inList && e.type == EventType.MouseDown && e.button == 0 && hoverIdx >= 0) {
                ModMetaData hovMod = mods[hoverIdx];
                Rect rowR = new Rect(rect.x, rect.y + hoverIdx * rowH, rect.width, rowH);
                ColumnRects cols = ComputeColumns(rowR);
                if (!cols.Check.Contains(e.mousePosition)) {
                    dragPressedPackageId = hovMod.PackageId;
                    dragSourcePackageId = hovMod.PackageId;
                    dragPressedY = e.mousePosition.y;
                    if (dragActive) {
                        ActiveDragRegistry.Release();
                    }
                    dragActive = false;
                    dragHoverInsertSlot = -1;
                }
            }

            if (e.type == EventType.MouseDrag && !dragActive
                && dragPressedPackageId != null
                && Mathf.Abs(e.mousePosition.y - dragPressedY) > 4f) {
                ModMetaData? src = mods.Find(m => string.Equals(m.PackageId, dragPressedPackageId, StringComparison.OrdinalIgnoreCase));
                if (src != null && src.Active && !ModKindResolver.IsLocked(src)) {
                    dragActive = true;
                    ActiveDragRegistry.Acquire();
                }
            }

            if (dragActive && inList) {
                float localY = e.mousePosition.y - rect.y;
                dragHoverInsertSlot = Mathf.Clamp(Mathf.RoundToInt(localY / rowH), 0, count);
            }

            paintChildren();

            if (e.type == EventType.Repaint && dragActive && dragHoverInsertSlot >= 0) {
                float indY = rect.y + dragHoverInsertSlot * rowH - 1f;
                Rect indicator = new Rect(
                    rect.x + StripeWidth.ToPixels(),
                    indY,
                    rect.width - StripeWidth.ToPixels(),
                    2f
                );
                PaintBox.Draw(indicator, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
            }

            if (e.type == EventType.MouseUp && e.button == 0) {
                if (dragActive && dragSourcePackageId != null && dragHoverInsertSlot >= 0) {
                    TryReorderToSlot(dragSourcePackageId, dragHoverInsertSlot);
                    ResetDragState();
                    e.Use();
                }
                else if (dragPressedPackageId != null && inList && hoverIdx >= 0) {
                    string clickedPid = mods[hoverIdx].PackageId;
                    ResetDragState();
                    onSelect(clickedPid);
                    e.Use();
                }
                else {
                    ResetDragState();
                }
            }
        };

        return node;
    }

    private static LightweaveNode BuildRow(ModMetaData mod, int loadOrder, bool isSelected, bool zebra, bool isDragSource) {
        LightweaveNode node = NodeBuilder.New("ModListRow:" + mod.PackageId);
        node.PreferredHeight = RowHeight.ToPixels();
        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            InteractionState state = InteractionState.Resolve(rect, null, false);

            if (isSelected) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.SurfaceRaised), null, null);
            }
            else if (zebra) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.SurfaceTranslucent), null, null);
            }

            if (state.Hovered && !ActiveDragRegistry.IsActive) {
                Color hoverWashBase = theme.GetColor(ThemeSlot.SurfaceTranslucentDark);
                Color hoverWash = new Color(hoverWashBase.r, hoverWashBase.g, hoverWashBase.b, 0.35f);
                PaintBox.Draw(rect, BackgroundSpec.Of(hoverWash), null, null);
            }

            PaintBox.Draw(
                rect,
                null,
                new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                null
            );

            if (isSelected) {
                Rect stripe = new Rect(rect.x, rect.y, StripeWidth.ToPixels(), rect.height);
                PaintBox.Draw(stripe, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
            }

            ColumnRects cols = ComputeColumns(rect);

            if (isDragSource) {
                using (TintScope.Opacity(0.45f)) {
                    DrawOrder(cols.Order, loadOrder, theme);
                    DrawCheckbox(cols.Check, mod, theme);
                    DrawName(cols.Name, mod, theme);
                    DrawAuthor(cols.Author, mod, theme);
                    DrawVersion(cols.Version, mod, theme);
                    DrawStatus(cols.Status, mod, theme);
                }
            }
            else {
                DrawOrder(cols.Order, loadOrder, theme);
                DrawCheckbox(cols.Check, mod, theme);
                DrawName(cols.Name, mod, theme);
                DrawAuthor(cols.Author, mod, theme);
                DrawVersion(cols.Version, mod, theme);
                DrawStatus(cols.Status, mod, theme);
            }

            if (!ActiveDragRegistry.IsActive) {
                InteractionFeedback.Apply(rect, true, true);
            }
        };
        return node;
    }

    private static void ResetDragState() {
        if (dragActive) {
            ActiveDragRegistry.Release();
        }
        dragPressedPackageId = null;
        dragSourcePackageId = null;
        dragActive = false;
        dragHoverInsertSlot = -1;
    }

    private static void TryReorderToSlot(string fromPkg, int slot) {
        List<ModMetaData> active = Verse.ModsConfig.ActiveModsInLoadOrder.ToList();
        int fromIdx = active.FindIndex(m => string.Equals(m.PackageId, fromPkg, StringComparison.OrdinalIgnoreCase));
        if (fromIdx < 0) return;
        if (slot == fromIdx || slot == fromIdx + 1) return;
        ForceReorderActive(fromIdx, slot);
    }

    private static void ForceReorderActive(int modIndex, int newIndex) {
        System.Reflection.BindingFlags privStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.FieldInfo? dataField = typeof(Verse.ModsConfig).GetField("data", privStatic);
        object? data = dataField?.GetValue(null);
        if (data == null) return;
        System.Reflection.FieldInfo? activeModsField = data.GetType().GetField("activeMods", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (activeModsField?.GetValue(data) is not List<string> activeMods) return;
        if (modIndex < 0 || modIndex >= activeMods.Count) return;
        int clamped = Mathf.Clamp(newIndex, 0, activeMods.Count);
        string packageId = activeMods[modIndex];
        activeMods.Insert(clamped, packageId);
        activeMods.RemoveAt(modIndex < clamped ? modIndex : modIndex + 1);
        System.Reflection.FieldInfo? dirtyField = typeof(Verse.ModsConfig).GetField("activeModsInLoadOrderCachedDirty", privStatic);
        dirtyField?.SetValue(null, true);
    }

    private static void DrawOrder(Rect r, int order, Theme.Theme theme) {
        TextDraw.Draw(r, order.ToString("D2"), FontRole.Mono, new Rem(0.75f), TextAnchor.MiddleLeft, ThemeSlot.TextMuted);
    }

    private static void DrawCheckbox(Rect r, ModMetaData mod, Theme.Theme theme) {
        float boxSize = new Rem(1.0f).ToPixels();
        Rect box = new Rect(
            r.x,
            r.y + (r.height - boxSize) / 2f,
            boxSize,
            boxSize
        );
        bool toggleLocked = ModKindResolver.IsLocked(mod);
        bool hovered = !toggleLocked && Mouse.IsOver(box);
        Checkbox.DrawBox(box, mod.Active, toggleLocked, hovered);
        Event e = Event.current;
        if (!toggleLocked && e.type == EventType.MouseUp && e.button == 0 && box.Contains(e.mousePosition)) {
            Verse.ModsConfig.SetActive(mod, !mod.Active);
            e.Use();
        }
    }

    private static void DrawName(Rect r, ModMetaData mod, Theme.Theme theme) {
        Rem nameSize = new Rem(1.0f);

        ModKind kind = ModKindResolver.Resolve(mod);
        bool showDlc = kind == ModKind.Core || kind == ModKind.Expansion;
        bool showLib = kind == ModKind.Library;
        bool locked = ModKindResolver.IsLocked(mod);

        float tagGap = new Rem(0.375f).ToPixels();
        float tagH = new Rem(1.25f).ToPixels();
        float tagPad = new Rem(0.375f).ToPixels();

        string name = mod.Name ?? mod.PackageId ?? string.Empty;
        Vector2 measured = TextDraw.Measure(name, FontRole.Body, nameSize);
        float maxNameW = r.width - (showDlc || showLib ? new Rem(2.5f).ToPixels() : 0f) - (locked ? new Rem(1.0f).ToPixels() : 0f);
        float nameW = Mathf.Min(measured.x, maxNameW);

        Rect nameRect = new Rect(r.x, r.y, nameW, r.height);
        TextDraw.Draw(nameRect, name, FontRole.Body, nameSize, TextAnchor.MiddleLeft, ThemeSlot.TextPrimary, FontStyle.Normal, TextClipping.Clip);

        float cursorX = r.x + nameW + tagGap;
        float tagY = r.y + (r.height - tagH) / 2f;

        if (showDlc) {
            string tagText = (string)"CL_ModsConfig_Tag_Dlc".Translate();
            cursorX = DrawTagChip(cursorX, tagY, tagH, tagPad, tagText, ThemeSlot.SurfaceAccent, theme);
            cursorX += tagGap;
        }
        else if (showLib) {
            string tagText = (string)"CL_ModsConfig_Tag_Lib".Translate();
            cursorX = DrawTagChip(cursorX, tagY, tagH, tagPad, tagText, ThemeSlot.StatusSuccess, theme);
            cursorX += tagGap;
        }

        if (locked) {
            Rect glyphRect = new Rect(cursorX, r.y, new Rem(1.0f).ToPixels(), r.height);
            TextDraw.Draw(glyphRect, "🔒", FontRole.Body, new Rem(0.75f), TextAnchor.MiddleLeft, ThemeSlot.TextMuted);
        }
    }

    private static float DrawTagChip(float x, float y, float h, float padX, string text, ThemeSlot toneSlot, Theme.Theme theme) {
        Rem fontSize = new Rem(0.7f);
        Vector2 size = TextDraw.Measure(text, FontRole.Mono, fontSize);
        float w = size.x + padX * 2f;
        Rect chip = new Rect(x, y, w, h);
        PaintBox.Draw(chip, null, BorderSpec.All(new Rem(1f / 16f), toneSlot), null);
        TextDraw.Draw(chip, text, FontRole.Mono, fontSize, TextAnchor.MiddleCenter, toneSlot);
        return x + w;
    }

    private static void DrawAuthor(Rect r, ModMetaData mod, Theme.Theme theme) {
        string author = mod.AuthorsString ?? string.Empty;
        if (string.IsNullOrEmpty(author)) {
            return;
        }
        TextDraw.Draw(r, author, FontRole.Mono, new Rem(0.85f), TextAnchor.MiddleLeft, ThemeSlot.TextSecondary, FontStyle.Normal, TextClipping.Clip);
    }

    private static void DrawVersion(Rect r, ModMetaData mod, Theme.Theme theme) {
        string version = string.IsNullOrEmpty(mod.ModVersion) ? "—" : mod.ModVersion;
        TextDraw.Draw(r, version, FontRole.Mono, new Rem(0.85f), TextAnchor.MiddleLeft, ThemeSlot.TextMuted, FontStyle.Normal, TextClipping.Clip);
    }

    private static void DrawStatus(Rect r, ModMetaData mod, Theme.Theme theme) {
        string text;
        ThemeSlot slot;
        if (!mod.Active) {
            text = (string)"CL_ModsConfig_Row_Status_Disabled".Translate();
            slot = ThemeSlot.TextMuted;
        }
        else {
            int conflicts = ModConflicts.CountFor(mod);
            if (conflicts > 0) {
                text = "CL_ModsConfig_Row_Status_Conflict".Translate(conflicts.Named("COUNT")).Resolve();
                slot = ThemeSlot.StatusDanger;
            }
            else {
                text = (string)"CL_ModsConfig_Row_Status_Ok".Translate();
                slot = ThemeSlot.StatusSuccess;
            }
        }
        TextDraw.Draw(r, text, FontRole.Mono, new Rem(0.85f), TextAnchor.MiddleRight, slot);
    }

    private static ColumnRects ComputeColumns(Rect rect) {
        float padX = PaddingX.ToPixels();
        float colOrderW = ColOrder.ToPixels();
        float colCheckW = ColCheck.ToPixels();
        float colAuthorW = ColAuthor.ToPixels();
        float colVersionW = ColVersion.ToPixels();
        float colStatusW = ColStatus.ToPixels();
        float gap = new Rem(0.5f).ToPixels();

        float x = rect.x + padX;
        Rect order = new Rect(x, rect.y, colOrderW, rect.height);
        x += colOrderW + gap;
        Rect check = new Rect(x, rect.y, colCheckW, rect.height);
        x += colCheckW + gap;

        float right = rect.xMax - padX;
        Rect status = new Rect(right - colStatusW, rect.y, colStatusW, rect.height);
        Rect version = new Rect(status.x - gap - colVersionW, rect.y, colVersionW, rect.height);
        Rect author = new Rect(version.x - gap - colAuthorW, rect.y, colAuthorW, rect.height);

        float nameW = Mathf.Max(0f, author.x - gap - x);
        Rect name = new Rect(x, rect.y, nameW, rect.height);

        return new ColumnRects(order, check, name, author, version, status);
    }

    private static LightweaveNode BuildEmptyState() {
        return Container.Create(
            child: Stack.Create(SpacingScale.Xs, s => {
                s.Add(Eyebrow.Create("CL_ModsConfig_Empty_Eyebrow".Translate()));
                s.Add(Text.Create(
                    "CL_ModsConfig_Empty_Body".Translate(),
                    wrap: true,
                    style: new Style { TextColor = ThemeSlot.TextSecondary }
                ));
            }),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Lg),
            }
        );
    }


    private static LightweaveNode BuildSearchEmptyState(string query) {
        return Container.Create(
            child: Stack.Create(SpacingScale.Xs, s => {
                s.Add(Eyebrow.Create("CL_ModsConfig_Search_EmptyEyebrow".Translate()));
                s.Add(Text.Create(
                    "CL_ModsConfig_Search_NoMatch".Translate(query.Named("QUERY")),
                    wrap: true,
                    style: new Style { TextColor = ThemeSlot.TextSecondary }
                ));
            }),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Lg),
            }
        );
    }

    private readonly struct ColumnRects {
        public readonly Rect Order;
        public readonly Rect Check;
        public readonly Rect Name;
        public readonly Rect Author;
        public readonly Rect Version;
        public readonly Rect Status;

        public ColumnRects(Rect order, Rect check, Rect name, Rect author, Rect version, Rect status) {
            Order = order;
            Check = check;
            Name = name;
            Author = author;
            Version = version;
            Status = status;
        }
    }
}
