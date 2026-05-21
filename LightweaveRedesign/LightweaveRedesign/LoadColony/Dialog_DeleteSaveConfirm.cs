using System;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Overlay;
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

public class Dialog_DeleteSaveConfirm : LightweaveWindow {
    private readonly SaveFileInfo file;
    private readonly string displayName;
    private readonly Action onConfirm;

    public Dialog_DeleteSaveConfirm(SaveFileInfo file, string displayName, Action onConfirm) {
        this.file = file;
        this.displayName = displayName;
        this.onConfirm = onConfirm;
        absorbInputAroundWindow = false;
    }

    protected override float? CardWidth => 480f;
    protected override float? CardHeight => 240f;
    protected override Color? ScrimColor => new Color(0f, 0f, 0f, 0.25f);
    protected override BackgroundSpec? CardBackground => BackgroundSpec.Blur(new Color(0f, 0f, 0f, 0.95f), 10f);
    protected override float VignetteIntensity => 0.35f;
    protected override float VignetteScale => 0.9f;
    protected override EdgeInsets? CardPadding => EdgeInsets.All(SpacingScale.Md);

    public override void OnAcceptKeyPressed() {
        Confirm();
    }

    protected override LightweaveNode Body() {
        return Stack.Create(SpacingScale.Sm, c => {
            c.Add(Eyebrow.Create("CL_LoadColony_Delete".Translate()));
            c.Add(Typography.Typography.Heading.Create(3, displayName));
            c.Add(Spacer.Fixed(SpacingScale.Xs));
            c.Add(Text.Create("CL_LoadColony_Delete_Confirm".Translate()));
            c.AddFlex(Spacer.Flex());
            c.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(Spacer.Flex());
                h.AddHug(Button.Create(
                    label: "CancelButton".Translate(),
                    onClick: () => Close(),
                    variant: Variant.Secondary
                ));
                h.AddHug(Button.Create(
                    label: "CL_LoadColony_Delete".Translate(),
                    onClick: Confirm,
                    variant: Variant.Danger
                ));
            }, style: new Style { Height = new Rem(2.5f) }));
        });
    }

    private void Confirm() {
        try {
            onConfirm?.Invoke();
        }
        catch (Exception ex) {
            LightweaveLog.Error("Delete save failed: " + ex);
        }
        Close();
    }
}
