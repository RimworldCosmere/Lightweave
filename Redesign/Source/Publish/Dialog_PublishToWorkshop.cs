using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;
using static Cosmere.Lightweave.Hooks.Hooks;
using Alert = Cosmere.Lightweave.Feedback.Alert;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Wizard for publishing or updating a local mod to the Steam Workshop. Replicates
/// PublisherPlus parity: interactive file exclusion (CheckboxTree over the mod's files),
/// per-publish Title/Tags/Preview overrides, and a changelog note — all persisted to the
/// mod's <c>_LightweavePublish.xml</c> sidecar before the staged upload runs.
/// </summary>
public class Dialog_PublishToWorkshop : LightweaveWindow {
    private const int StepFilesMeta = 0;
    private const int StepConfirm = 1;
    private const int StepProgress = 2;
    private const int StepResult = 3;

    private readonly ModMetaData mod;
    private readonly PublishSidecar sidecar;
    private readonly string modRoot;

    public Dialog_PublishToWorkshop(ModMetaData mod) {
        this.mod = mod;
        this.modRoot = mod.RootDir.FullName;
        this.sidecar = PublishSidecar.Load(this.modRoot);
        absorbInputAroundWindow = false;
    }

    protected override float? CardWidth => 660f;
    protected override float? CardHeight => 820f;
    protected override Color? ScrimColor => new Color(0f, 0f, 0f, 0.35f);
    protected override BackgroundSpec? CardBackground => BackgroundSpec.Of(ThemeSlot.WindowSurface);
    protected override float VignetteIntensity => 0.35f;
    protected override float VignetteScale => 0.9f;
    protected override EdgeInsets? CardPadding => EdgeInsets.All(SpacingScale.Md);
    protected override Vector2 MinWindowSize => new Vector2(480f, 600f);

    protected override Rect? DragRegion(Rect inRect) {
        return new Rect(inRect.x, inRect.y, inRect.width, new Rem(4.5f).ToPixels());
    }

    protected override LightweaveNode Body() {
        StateHandle<int> step = UseState(StepFilesMeta);

        bool alreadyPublished = mod.GetPublishedFileId() != Steamworks.PublishedFileId_t.Invalid;
        string defaultTitle = mod.GetWorkshopName();
        StateHandle<string> title = UseState(string.IsNullOrEmpty(sidecar.TitleOverride) ? defaultTitle : sidecar.TitleOverride!);
        StateHandle<string> preview = UseState(sidecar.PreviewOverride ?? string.Empty);
        StateHandle<string> changelog = UseState(sidecar.LastChangelog ?? string.Empty);
        StateHandle<string> newTag = UseState(string.Empty);
        StateHandle<List<string>> tags = UseState(InitialTags());

        SteamIgnoreMatcher baseline = UseMemo(() => SteamIgnoreMatcher.ForModRoot(modRoot), [modRoot]);
        List<string> files = UseMemo(() => PublishStager.EnumerateRelativeFiles(modRoot, baseline), [modRoot]);
        TreeData tree = UseMemo(() => BuildTreeData(files), [files]);
        Dictionary<string, bool> initialInclude = UseMemo(() => ResolveInitialInclude(files, baseline), [files]);
        StateHandle<Dictionary<string, bool>> include = UseState(initialInclude);

        int effectiveStep = step.Value;
        if (effectiveStep == StepProgress && PublishSession.Succeeded.HasValue) {
            effectiveStep = StepResult;
        }

        return Stack.Create(SpacingScale.Sm, c => {
            c.Add(Eyebrow.Create((alreadyPublished
                ? "CL_Publish_Window_Eyebrow_Update"
                : "CL_Publish_Window_Eyebrow_Publish").Translate()));
            c.Add(Heading.Create(3, title.Value.NullOrEmpty() ? defaultTitle : title.Value));
            c.Add(Spacer.Fixed(SpacingScale.Xs));

            switch (effectiveStep) {
                case StepFilesMeta:
                    c.AddFlex(FilesMetaStep(title, preview, tags, newTag, tree, include));
                    c.Add(FooterFilesMeta(step));
                    break;
                case StepConfirm:
                    c.AddFlex(ConfirmStep(changelog, files, include));
                    c.Add(FooterConfirm(step, () => DoPublish(step, title, preview, tags, changelog, include, defaultTitle)));
                    break;
                case StepProgress:
                    c.AddFlex(ProgressStep());
                    break;
                default:
                    c.AddFlex(ResultStep());
                    c.Add(FooterResult());
                    break;
            }
        });
    }

    private LightweaveNode FilesMetaStep(
        StateHandle<string> title,
        StateHandle<string> preview,
        StateHandle<List<string>> tags,
        StateHandle<string> newTag,
        TreeData tree,
        StateHandle<Dictionary<string, bool>> include
    ) {
        return Stack.Create(SpacingScale.Sm, c => {
            c.Add(FieldLabel("CL_Publish_Field_Title".Translate()));
            c.Add(TextField.Create(
                value: title.Value,
                onChange: next => title.Set(next),
                placeholder: mod.GetWorkshopName()
            ));

            c.Add(FieldLabel("CL_Publish_Field_Preview".Translate()));
            c.Add(TextField.Create(
                value: preview.Value,
                onChange: next => preview.Set(next),
                placeholder: "CL_Publish_Field_Preview_Placeholder".Translate()
            ));

            c.Add(FieldLabel("CL_Publish_Field_Tags".Translate()));
            c.Add(TagsEditor(tags, newTag));

            c.Add(FieldLabel("CL_Publish_Field_Files".Translate()));
            c.AddFlex(ScrollArea.Create(
                CheckboxTree.Create(
                    roots: tree.Roots,
                    isChecked: key => include.Value.TryGetValue(key, out bool v) && v,
                    onToggle: (node, value) => ToggleNode(node, value, tree, include),
                    stateOf: node => StateOf(node, tree, include)
                ),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch, MinHeight = new Rem(14f) }
            ));
        });
    }

    private LightweaveNode TagsEditor(StateHandle<List<string>> tags, StateHandle<string> newTag) {
        return Stack.Create(SpacingScale.Xs, c => {
            List<string> current = tags.Value;
            if (current.Count > 0) {
                c.Add(Wrap.Create(
                    gap: new Rem(0.4f),
                    minChildWidth: new Rem(7f),
                    children: list => {
                        for (int i = 0; i < current.Count; i++) {
                            string tag = current[i];
                            list.Add(Chip.Create(
                                label: tag,
                                interactive: true,
                                state: true,
                                onToggle: _ => RemoveTag(tags, tag)
                            ));
                        }
                    },
                    lineHeight: new Rem(2f)
                ));
            }

            c.Add(HStack.Create(SpacingScale.Xs, h => {
                h.AddFlex(TextField.Create(
                    value: newTag.Value,
                    onChange: next => newTag.Set(next),
                    placeholder: "CL_Publish_Field_Tags_Placeholder".Translate()
                ));
                h.AddHug(Button.Create(
                    label: "CL_Publish_Field_Tags_Add".Translate(),
                    onClick: () => AddTag(tags, newTag),
                    variant: Variant.Secondary
                ));
            }, style: new Style { Height = new Rem(2.5f) }));
        });
    }

    private LightweaveNode ConfirmStep(
        StateHandle<string> changelog,
        List<string> files,
        StateHandle<Dictionary<string, bool>> include
    ) {
        int selectedCount = 0;
        for (int i = 0; i < files.Count; i++) {
            if (include.Value.TryGetValue(files[i], out bool v) && v) {
                selectedCount++;
            }
        }

        return Stack.Create(SpacingScale.Sm, c => {
            c.Add(Alert.Create(
                title: (string)"CL_Publish_Confirm_Summary".Translate(selectedCount.Named("COUNT"), files.Count.Named("TOTAL")),
                description: "CL_Publish_Confirm_Hint".Translate(),
                variant: AlertVariant.Info
            ));
            c.Add(Spacer.Fixed(SpacingScale.Xs));
            c.Add(FieldLabel("CL_Publish_Field_Changelog".Translate()));
            c.AddFlex(TextArea.Create(
                value: changelog.Value,
                onChange: next => changelog.Set(next),
                placeholder: "CL_Publish_Field_Changelog_Placeholder".Translate(),
                minRows: 4,
                maxRows: 12
            ));
        });
    }

    private LightweaveNode ProgressStep() {
        WorkshopInteractStage stage = Workshop.CurStage;
        string label = stage switch {
            WorkshopInteractStage.CreatingItem => (string)"CL_Publish_Progress_Creating".Translate(),
            WorkshopInteractStage.SubmittingItem => (string)"CL_Publish_Progress_Uploading".Translate(),
            _ => (string)"CL_Publish_Progress_Staging".Translate(),
        };

        float progress = UploadProgress(stage);

        return Stack.Create(SpacingScale.Md, c => {
            c.AddFlex(Spacer.Flex());
            c.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(Spacer.Flex());
                h.AddHug(Spinner.Create(new Rem(2f)));
                h.AddFlex(Spacer.Flex());
            }, style: new Style { Height = new Rem(2.5f) }));
            c.Add(Text.Create(label, style: new Style { TextAlign = TextAlign.Center }));
            c.Add(ProgressBar.Create(progress, label: null, variant: ProgressBarVariant.Accent));
            c.AddFlex(Spacer.Flex());
        });
    }

    private LightweaveNode ResultStep() {
        bool succeeded = PublishSession.Succeeded == true;
        string detail = PublishSession.ErrorDetail ?? string.Empty;
        string title = (succeeded ? "CL_Publish_Result_Success_Title" : "CL_Publish_Result_Error_Title").Translate();
        string description = succeeded
            ? (string)"CL_Publish_Result_Success_Body".Translate()
            : (PublishSession.NeedsLegalAgreement
                ? (string)"CL_Publish_Result_Legal_Body".Translate()
                : (string)"CL_Publish_Result_Error_Body".Translate(detail.Named("DETAIL")));

        return Stack.Create(SpacingScale.Md, c => {
            c.AddFlex(Spacer.Flex());
            c.Add(Alert.Create(
                title: title,
                description: description,
                variant: succeeded ? AlertVariant.Success : AlertVariant.Danger
            ));
            c.AddFlex(Spacer.Flex());
        });
    }

    private LightweaveNode FooterFilesMeta(StateHandle<int> step) {
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Button.Create(
                label: "CL_Publish_Button_Cancel".Translate(),
                onClick: () => Close(),
                variant: Variant.Ghost
            ));
            h.AddFlex(Spacer.Flex());
            h.AddHug(Button.Create(
                label: "CL_Publish_Button_Next".Translate(),
                onClick: () => step.Set(StepConfirm),
                variant: Variant.Primary
            ));
        }, style: new Style { Height = new Rem(2.5f) });
    }

    private LightweaveNode FooterConfirm(StateHandle<int> step, Action onPublish) {
        bool busy = Workshop.CurStage != WorkshopInteractStage.None;
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(Button.Create(
                label: "CL_Publish_Button_Back".Translate(),
                onClick: () => step.Set(StepFilesMeta),
                variant: Variant.Ghost
            ));
            h.AddFlex(Spacer.Flex());
            h.AddHug(Button.Create(
                label: "CL_Publish_Button_Publish".Translate(),
                onClick: onPublish,
                variant: Variant.Primary,
                disabled: busy
            ));
        }, style: new Style { Height = new Rem(2.5f) });
    }

    private LightweaveNode FooterResult() {
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddFlex(Spacer.Flex());
            h.AddHug(Button.Create(
                label: "CL_Publish_Button_Close".Translate(),
                onClick: () => {
                    PublishSession.Reset();
                    Close();
                },
                variant: Variant.Primary
            ));
        }, style: new Style { Height = new Rem(2.5f) });
    }

    private void DoPublish(
        StateHandle<int> step,
        StateHandle<string> title,
        StateHandle<string> preview,
        StateHandle<List<string>> tags,
        StateHandle<string> changelog,
        StateHandle<Dictionary<string, bool>> include,
        string defaultTitle
    ) {
        if (!WorkshopPackage.MechanismAvailable) {
            PublishSession.Begin(mod.PackageId);
            PublishSession.Complete(false, (string)"CL_Publish_Result_Mechanism_Body".Translate(), false);
            step.Set(StepProgress);
            return;
        }

        if (Workshop.CurStage != WorkshopInteractStage.None) {
            Messages.Message((string)"CL_Publish_InProgress".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (!SteamManager.Initialized) {
            PublishSession.Begin(mod.PackageId);
            PublishSession.Complete(false, (string)"CL_Publish_Result_NoSteam_Body".Translate(), false);
            step.Set(StepProgress);
            return;
        }

        PersistSidecar(title, preview, tags, changelog, include, defaultTitle);

        PublishSession.Begin(mod.PackageId);
        try {
            new WorkshopPackage(mod, sidecar).Submit();
        }
        catch (Exception ex) {
            RedesignLog.Error("Workshop upload failed to start for " + mod.PackageId + ": " + ex);
            PublishSession.Complete(false, ex.Message, false);
        }

        step.Set(StepProgress);
    }

    private void PersistSidecar(
        StateHandle<string> title,
        StateHandle<string> preview,
        StateHandle<List<string>> tags,
        StateHandle<string> changelog,
        StateHandle<Dictionary<string, bool>> include,
        string defaultTitle
    ) {
        string titleValue = title.Value?.Trim() ?? string.Empty;
        sidecar.TitleOverride = titleValue.Length == 0 || titleValue == defaultTitle ? null : titleValue;

        string previewValue = preview.Value?.Trim() ?? string.Empty;
        sidecar.PreviewOverride = previewValue.Length == 0 ? null : previewValue;

        string changelogValue = changelog.Value?.Trim() ?? string.Empty;
        sidecar.LastChangelog = changelogValue.Length == 0 ? null : changelogValue;

        sidecar.Tags.Clear();
        sidecar.Tags.AddRange(tags.Value);

        sidecar.Overrides.Clear();
        foreach (KeyValuePair<string, bool> entry in include.Value) {
            sidecar.Overrides[entry.Key] = entry.Value;
        }

        try {
            sidecar.Save(modRoot);
        }
        catch (Exception ex) {
            RedesignLog.Error("Failed to write publish sidecar for " + mod.PackageId + ": " + ex);
        }
    }

    private List<string> InitialTags() {
        if (sidecar.Tags.Count > 0) {
            return new List<string>(sidecar.Tags);
        }

        List<string> result = new List<string>();
        IList<string> modTags = mod.GetWorkshopTags();
        if (modTags != null) {
            for (int i = 0; i < modTags.Count; i++) {
                result.Add(modTags[i]);
            }
        }

        return result;
    }

    private static void AddTag(StateHandle<List<string>> tags, StateHandle<string> newTag) {
        string value = newTag.Value?.Trim() ?? string.Empty;
        if (value.Length == 0) {
            return;
        }

        List<string> next = new List<string>(tags.Value);
        if (!next.Contains(value)) {
            next.Add(value);
        }

        tags.Set(next);
        newTag.Set(string.Empty);
    }

    private static void RemoveTag(StateHandle<List<string>> tags, string tag) {
        List<string> next = new List<string>(tags.Value);
        next.Remove(tag);
        tags.Set(next);
    }

    private static TriState StateOf(
        CheckboxTreeNode node,
        TreeData tree,
        StateHandle<Dictionary<string, bool>> include
    ) {
        Dictionary<string, bool> map = include.Value;
        if (tree.FolderLeaves.TryGetValue(node.Key, out List<string> leaves)) {
            return PublishSelection.DeriveFolderState(leaves, key => map.TryGetValue(key, out bool v) && v);
        }

        return map.TryGetValue(node.Key, out bool included) && included ? TriState.Checked : TriState.Unchecked;
    }

    private static void ToggleNode(
        CheckboxTreeNode node,
        bool value,
        TreeData tree,
        StateHandle<Dictionary<string, bool>> include
    ) {
        Dictionary<string, bool> next = new Dictionary<string, bool>(include.Value);
        if (tree.FolderLeaves.TryGetValue(node.Key, out List<string> leaves)) {
            for (int i = 0; i < leaves.Count; i++) {
                next[leaves[i]] = value;
            }
        }
        else {
            next[node.Key] = value;
        }

        include.Set(next);
    }

    private static Dictionary<string, bool> ResolveInitialInclude(
        IReadOnlyList<string> files,
        SteamIgnoreMatcher baseline
    ) {
        Dictionary<string, bool> map = new Dictionary<string, bool>();
        for (int i = 0; i < files.Count; i++) {
            map[files[i]] = !baseline.IsIgnored(files[i]);
        }

        return map;
    }

    private static float UploadProgress(WorkshopInteractStage stage) {
        if (stage != WorkshopInteractStage.SubmittingItem) {
            return 0f;
        }

        try {
            Workshop.GetUpdateStatus(out _, out float percent);
            return percent;
        }
        catch (Exception) {
            return 0f;
        }
    }

    private static LightweaveNode FieldLabel(string text) {
        return Text.Create(text, style: new Style { TextColor = ThemeSlot.TextSecondary });
    }

    private static TreeData BuildTreeData(IReadOnlyList<string> relPaths) {
        Dictionary<string, List<string>> folderLeaves = new Dictionary<string, List<string>>();
        DirNode root = new DirNode(string.Empty);
        for (int i = 0; i < relPaths.Count; i++) {
            string path = relPaths[i];
            string[] parts = path.Split('/');
            DirNode current = root;
            string prefix = string.Empty;
            for (int p = 0; p < parts.Length - 1; p++) {
                prefix = prefix.Length == 0 ? parts[p] : prefix + "/" + parts[p];
                if (!current.Dirs.TryGetValue(parts[p], out DirNode child)) {
                    child = new DirNode(prefix);
                    current.Dirs[parts[p]] = child;
                }

                current = child;
            }

            current.Files.Add(path);
        }

        List<CheckboxTreeNode> roots = ConvertChildren(root, folderLeaves);
        return new TreeData(roots, folderLeaves);
    }

    private static List<CheckboxTreeNode> ConvertChildren(
        DirNode dir,
        Dictionary<string, List<string>> folderLeaves
    ) {
        List<CheckboxTreeNode> result = new List<CheckboxTreeNode>();
        foreach (KeyValuePair<string, DirNode> sub in dir.Dirs) {
            List<CheckboxTreeNode> children = ConvertChildren(sub.Value, folderLeaves);
            List<string> leaves = new List<string>();
            CollectLeaves(sub.Value, leaves);
            folderLeaves[sub.Value.Path] = leaves;
            result.Add(new CheckboxTreeNode(sub.Key, sub.Value.Path, children));
        }

        for (int i = 0; i < dir.Files.Count; i++) {
            string file = dir.Files[i];
            int slash = file.LastIndexOf('/');
            string label = slash >= 0 ? file.Substring(slash + 1) : file;
            result.Add(new CheckboxTreeNode(label, file));
        }

        return result;
    }

    private static void CollectLeaves(DirNode dir, List<string> accumulator) {
        foreach (KeyValuePair<string, DirNode> sub in dir.Dirs) {
            CollectLeaves(sub.Value, accumulator);
        }

        for (int i = 0; i < dir.Files.Count; i++) {
            accumulator.Add(dir.Files[i]);
        }
    }

    private sealed class TreeData {
        public TreeData(List<CheckboxTreeNode> roots, Dictionary<string, List<string>> folderLeaves) {
            this.Roots = roots;
            this.FolderLeaves = folderLeaves;
        }

        public List<CheckboxTreeNode> Roots { get; }

        public Dictionary<string, List<string>> FolderLeaves { get; }
    }

    private sealed class DirNode {
        public DirNode(string path) {
            this.Path = path;
        }

        public string Path { get; }

        public SortedDictionary<string, DirNode> Dirs { get; } =
            new SortedDictionary<string, DirNode>(StringComparer.OrdinalIgnoreCase);

        public List<string> Files { get; } = new List<string>();
    }
}
