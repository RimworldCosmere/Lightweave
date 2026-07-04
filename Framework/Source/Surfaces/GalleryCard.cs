using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Adapter;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using UnityEngine;
using LwText = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Surfaces;

[Doc(
    Id = "gallery-card",
    Summary = "A gallery item card: leading icon badge, title + wrapping description, optional trailing remove-x, square corners. Washes accent when selected, brightens its border on hover.",
    WhenToUse = "The reusable unit of a 3-up gallery grid (a meme, a precept, a style). Pair with AddTile for the trailing add cell. For a bare list row with a leading accent bar use SelectableSurface(ListRow).",
    SourcePath = "Lightweave/Surfaces/GalleryCard.cs"
)]
public static class GalleryCard {
    private static readonly Rem MinHeight = new Rem(5.75f);
    private static readonly EdgeInsets Pad = new EdgeInsets(new Rem(0.8125f), new Rem(0.875f), new Rem(0.8125f), new Rem(0.875f));
    private static readonly Rem AvatarSize = new Rem(2.75f);
    private static readonly Rem RowGap = new Rem(0.75f);
    private static readonly Rem BodyGap = new Rem(0.3125f);
    private static readonly Rem NameSize = new Rem(1.1875f);
    private static readonly Rem DescSize = new Rem(0.719f);
    private static readonly Rem RemoveSize = new Rem(1.5f);
    private static readonly Rem RemoveInset = new Rem(0.4375f);

    public static LightweaveNode Create(
        [DocParam("Card title. Caller translates.")]
        string title,
        [DocParam("Supporting description, wrapped under the title. Caller translates.", TypeOverride = "string?", DefaultOverride = "null")]
        string? description = null,
        [DocParam("Leading icon glyph. Drawn in an Avatar badge tinted with the accent.", TypeOverride = "IconRef?", DefaultOverride = "null")]
        IconRef? icon = null,
        [DocParam("Optional bitmap shown in the badge instead of a glyph (e.g. a def icon).", TypeOverride = "Texture2D?", DefaultOverride = "null")]
        Texture2D? iconTexture = null,
        [DocParam("Accent slot for the badge, selected fill, and selected border. Defaults to SurfaceAccent.", TypeOverride = "ThemeSlot?", DefaultOverride = "null")]
        ThemeSlot? accent = null,
        [DocParam("Selected look: accent-soft fill + accent-glow border.")]
        bool selected = false,
        [DocParam("Invoked when the card body is clicked. Null makes the card non-selectable.", TypeOverride = "Action?", DefaultOverride = "null")]
        Action? onSelect = null,
        [DocParam("Invoked when the trailing remove-x is clicked. Null hides the x.", TypeOverride = "Action?", DefaultOverride = "null")]
        Action? onRemove = null,
        [DocParam("Tooltip key for the remove-x.", TypeOverride = "string?", DefaultOverride = "null")]
        string? removeTooltipKey = null,
        [DocParam("Optional hover tooltip for the whole card.", TypeOverride = "string?", DefaultOverride = "null")]
        string? tooltip = null,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'gallery-card' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        ThemeSlot accentSlot = accent ?? ThemeSlot.SurfaceAccent;

        LightweaveNode node = NodeBuilder.New($"GalleryCard:{title}", line, file);
        node.ApplyStyling("gallery-card", style, classes, id);
        node.PreferredHeight = MinHeight.ToPixels();

        LightweaveNode avatar = Data.Avatar.Create(
            string.Empty,
            size: AvatarSize,
            accent: accentSlot,
            background: ThemeSlot.SurfaceSunken,
            border: ThemeSlot.BorderSubtle,
            icon: icon,
            texture: iconTexture,
            radius: RadiusScale.None
        );
        node.Children.Add(avatar);

        LightweaveNode body = Stack.Create(BodyGap, b => {
            b.Add(LwText.Create(title, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = NameSize,
                TextColor = ThemeSlot.TextPrimary,
            }));
            if (!string.IsNullOrEmpty(description)) {
                b.Add(LwText.Create(description!, wrap: true, style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = DescSize,
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }
        });
        node.Children.Add(body);

        LightweaveNode? removeBtn = onRemove == null
            ? null
            : IconButton.Create(
                Glyph.Create(Icons.Phosphor.X, new Style { FontSize = new Rem(0.875f) }),
                onRemove,
                Variant.Quiet,
                iconSize: new Rem(0.875f),
                tooltipKey: removeTooltipKey
            );
        if (removeBtn != null) {
            node.Children.Add(removeBtn);
        }

        (float left, float top, float right, float bottom) PadPx() {
            return Pad.Resolve(RenderContext.Current.Direction);
        }

        node.MeasureWidth = () => {
            (float left, float top, float right, float bottom) = PadPx();
            float bodyW = body.MeasureWidth?.Invoke() ?? 0f;
            return Mathf.Ceil(left + AvatarSize.ToPixels() + RowGap.ToPixels() + bodyW + right);
        };

        node.Measure = availableWidth => {
            (float left, float top, float right, float bottom) = PadPx();
            float bodyW = Mathf.Max(0f, availableWidth - left - AvatarSize.ToPixels() - RowGap.ToPixels() - right);
            float bodyH = body.Measure?.Invoke(bodyW) ?? body.PreferredHeight ?? 0f;
            float contentH = Mathf.Max(AvatarSize.ToPixels(), bodyH);
            return Mathf.Max(MinHeight.ToPixels(), top + contentH + bottom);
        };

        node.Layout = rect => {
            node.MeasuredRect = rect;
            (float left, float top, float right, float bottom) = PadPx();
            float avatarPx = AvatarSize.ToPixels();

            avatar.MeasuredRect = new Rect(rect.x + left, rect.y + top, avatarPx, avatarPx);

            float bodyX = rect.x + left + avatarPx + RowGap.ToPixels();
            float bodyW = Mathf.Max(0f, rect.xMax - right - bodyX);
            float bodyH = body.Measure?.Invoke(bodyW) ?? body.PreferredHeight ?? 0f;
            body.MeasuredRect = new Rect(bodyX, rect.y + top, bodyW, bodyH);

            Rect removeRect = default;
            if (removeBtn != null) {
                float btnPx = RemoveSize.ToPixels();
                float inset = RemoveInset.ToPixels();
                removeRect = new Rect(rect.xMax - inset - btnPx, rect.y + inset, btnPx, btnPx);
                removeBtn.MeasuredRect = removeRect;
            }

            if (!string.IsNullOrEmpty(tooltip)) {
                string tip = tooltip!;
                AsTooltip.Attach(rect, () => LwText.Create(tip, wrap: true, richText: true), key: (id ?? title).GetHashCode());
            }

            Event? ev = Event.current;
            if (onSelect != null
                && ev != null
                && ev.type == EventType.MouseUp
                && ev.button == 0
                && rect.Contains(ev.mousePosition)
                && !(removeBtn != null && removeRect.Contains(ev.mousePosition))
                && LightweaveHitTracker.IsTopmost(rect)) {
                onSelect.Invoke();
                ev.Use();
            }
        };

        node.Draw = rect => {
            InteractionState state = InteractionState.Resolve(rect, null, false);

            BackgroundSpec bg = selected ? BackgroundSpec.Of(ThemeSlot.AccentSoft) : BackgroundSpec.Of(ThemeSlot.Glass1);
            ThemeSlot borderSlot = selected
                ? ThemeSlot.AccentGlow
                : state.Hovered
                    ? ThemeSlot.BorderHover
                    : ThemeSlot.BorderDefault;

            PaintBox.Draw(rect, bg, BorderSpec.All(new Rem(1f / 16f), borderSlot), RadiusSpec.None);
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => GalleryCard.Create(
            "Individualist",
            "Members value personal freedom and self-reliance.",
            Icons.Phosphor.User,
            onRemove: () => { }
        ));
    }

    [DocVariant("CL_Playground_GalleryCard_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => GalleryCard.Create(
            "Individualist",
            "Members value personal freedom and self-reliance.",
            Icons.Phosphor.User,
            onRemove: () => { }
        ));
    }

    [DocVariant("CL_Playground_GalleryCard_Selected")]
    public static DocSample DocsSelected() {
        return new DocSample(() => GalleryCard.Create(
            "Collectivist",
            "The colony comes before the self.",
            Icons.Phosphor.UsersThree,
            selected: true,
            onSelect: () => { },
            onRemove: () => { }
        ));
    }

    [DocState("CL_Playground_Label_Hover", HideCode = true)]
    public static DocSample DocsHover() {
        return new DocSample(() => GalleryCard.Create(
            "Raider",
            "Violence is a legitimate way to acquire resources.",
            Icons.Phosphor.Skull,
            onSelect: () => { },
            onRemove: () => { }
        ));
    }
}
