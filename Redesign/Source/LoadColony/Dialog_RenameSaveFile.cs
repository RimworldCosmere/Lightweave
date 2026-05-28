using System;
using System.IO;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Overlay;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Display = Cosmere.Lightweave.Typography.Display;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.LoadColony;

public class Dialog_RenameSaveFile : LightweaveWindow {
    private readonly SaveFileInfo file;
    private readonly string originalName;
    private readonly Action onAfter;

    private string newName;
    private string? validationError;
    private bool focusRequested;

    public Dialog_RenameSaveFile(SaveFileInfo file, string currentName, Action onAfter) {
        this.file = file;
        this.originalName = currentName;
        this.newName = currentName;
        this.onAfter = onAfter;
        absorbInputAroundWindow = false;
    }

    protected override float? CardWidth => 520f;
    protected override float? CardHeight => 320f;
    protected override Color? ScrimColor => new Color(0f, 0f, 0f, 0.25f);
    protected override BackgroundSpec? CardBackground => BackgroundSpec.Of(ThemeSlot.WindowSurface);
    protected override float VignetteIntensity => 0.35f;
    protected override float VignetteScale => 0.9f;
    protected override EdgeInsets? CardPadding => EdgeInsets.All(SpacingScale.Md);

    public override void OnAcceptKeyPressed() {
        TryCommit();
    }

    protected override LightweaveNode Body() {
        UseFocus.FocusHandle focus = UseFocus.Use();
        if (!focusRequested) {
            focus.Request();
            focusRequested = true;
        }

        return Stack.Create(SpacingScale.Sm, c => {
            c.Add(Eyebrow.Create("CL_LoadColony_Rename".Translate()));
            c.Add(Typography.Typography.Heading.Create(3, originalName));
            c.Add(Text.Create(
                "CL_LoadColony_Rename_Description".Translate(),
                style: new Style { TextColor = new ColorRef.Token(ThemeSlot.TextSecondary) }
            ));
            c.Add(Spacer.Fixed(SpacingScale.Xs));
            c.Add(TextField.Create(
                value: newName,
                onChange: v => {
                    newName = v;
                    validationError = null;
                },
                placeholder: originalName,
                focus: focus
            ));
            if (!string.IsNullOrEmpty(validationError)) {
                c.Add(Text.Create(
                    validationError!,
                    style: new Style { TextColor = new ColorRef.Token(ThemeSlot.StatusDanger) }
                ));
            }
            c.AddFlex(Spacer.Flex());
            c.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(Spacer.Flex());
                h.AddHug(Button.Create(
                    label: "CancelButton".Translate(),
                    onClick: () => Close(),
                    variant: Variant.Secondary
                ));
                h.AddHug(Button.Create(
                    label: "CL_LoadColony_Rename".Translate(),
                    onClick: TryCommit,
                    variant: Variant.Primary
                ));
            }, style: new Style { Height = new Rem(2.5f) }));
        });
    }

    private void TryCommit() {
        string trimmed = (newName ?? string.Empty).Trim();
        if (trimmed.Length == 0) {
            validationError = "NameIsInvalid".Translate();
            return;
        }
        if (trimmed == originalName) {
            Close();
            return;
        }
        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
            validationError = "NameIsInvalid".Translate();
            return;
        }
        try {
            string dir = file.FileInfo.DirectoryName ?? string.Empty;
            string ext = file.FileInfo.Extension;
            string newPath = Path.Combine(dir, trimmed + ext);
            if (File.Exists(newPath)) {
                validationError = "NameIsInvalid".Translate();
                return;
            }
            string oldSidecar = SaveSidecar.PathFor(file.FileInfo.FullName);
            file.FileInfo.MoveTo(newPath);
            if (File.Exists(oldSidecar)) {
                string newSidecar = SaveSidecar.PathFor(newPath);
                try {
                    File.Move(oldSidecar, newSidecar);
                }
                catch (Exception sidecarEx) {
                    RedesignLog.Warning($"Rename sidecar failed: {sidecarEx.Message}");
                }
            }
            onAfter?.Invoke();
            Close();
        }
        catch (Exception ex) {
            RedesignLog.Warning($"Rename save failed: {ex.Message}");
            validationError = ex.Message;
        }
    }
}
