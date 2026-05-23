using System;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Overlay;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.ModsConfig;

public class Dialog_ModsConfigRestart : LightweaveWindow {
    public Dialog_ModsConfigRestart() {
        absorbInputAroundWindow = false;
    }

    protected override float? CardWidth => 520f;
    protected override float? CardHeight => 240f;
    protected override Color? ScrimColor => new Color(0f, 0f, 0f, 0.25f);
    protected override BackgroundSpec? CardBackground => BackgroundSpec.Blur(new Color(0f, 0f, 0f, 0.95f), 10f);
    protected override float VignetteIntensity => 0.35f;
    protected override float VignetteScale => 0.9f;
    protected override EdgeInsets? CardPadding => EdgeInsets.All(SpacingScale.Md);

    public override void OnAcceptKeyPressed() {
        DoRestart();
    }

    protected override LightweaveNode Body() {
        return Stack.Create(SpacingScale.Sm, c => {
            c.Add(Eyebrow.Create("CL_ModsConfig_Restart_Eyebrow".Translate()));
            c.Add(Typography.Typography.Heading.Create(3, "CL_ModsConfig_Restart_Heading".Translate()));
            c.Add(Spacer.Fixed(SpacingScale.Xs));
            c.Add(Text.Create("CL_ModsConfig_Restart_Body".Translate()));
            c.AddFlex(Spacer.Flex());
            c.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(Spacer.Flex());
                h.AddHug(Button.Create(
                    label: "CL_ModsConfig_Restart_Later".Translate(),
                    onClick: () => Close(),
                    variant: Variant.Ghost
                ));
                h.AddHug(Button.Create(
                    label: "CL_ModsConfig_Restart_Now".Translate(),
                    onClick: DoRestart,
                    variant: Variant.Primary
                ));
            }, style: new Style { Height = new Rem(2.5f) }));
        });
    }

    private void DoRestart() {
        try {
            GenCommandLine.Restart();
        }
        catch (Exception ex) {
            RedesignLog.Error("Restart failed: " + ex);
        }
        Close();
    }
}
