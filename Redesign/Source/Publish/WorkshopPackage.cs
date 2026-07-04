using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Steamworks;
using Verse;
using Verse.Steam;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// A <see cref="WorkshopUploadable"/> that uploads only the included subset of a mod's files,
/// applying the sidecar's Title/Preview/Tags overrides. Mirrors PublisherPlus' mechanism: stage
/// the included files into a temp dir, point Steam at it, and drive vanilla
/// <c>Verse.Steam.Workshop.Upload</c> directly. The real <see cref="ModMetaData"/> is the
/// <c>PublishedFileId</c> owner, so <see cref="SetPublishedFileId"/> writes back to the mod root,
/// never the temp copy.
/// </summary>
public sealed class WorkshopPackage : WorkshopUploadable {

    private static readonly MethodInfo? UploadMethod =
        typeof(Verse.Steam.Workshop).GetMethod("Upload", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(WorkshopUploadable) }, null);

    private readonly ModMetaData mod;
    private readonly PublishSidecar sidecar;
    private WorkshopItemHook? hook;
    private DirectoryInfo? stagedDir;

    public WorkshopPackage(ModMetaData mod, PublishSidecar sidecar) {
        this.mod = mod;
        this.sidecar = sidecar;
    }

    /// <summary>True when the upload mechanism is available (via reflection on vanilla Workshop.Upload).</summary>
    public static bool MechanismAvailable => UploadMethod != null;

    public List<string> ResolveIncludedFiles() {
        SteamIgnoreMatcher baseline = SteamIgnoreMatcher.ForModRoot(this.mod.RootDir.FullName);
        List<string> allFiles = PublishStager.EnumerateRelativeFiles(this.mod.RootDir.FullName, baseline);
        return PublishSelection.SelectFilesForUpload(
            allFiles,
            relPath => this.sidecar.ResolveInclusion(relPath, baseline)
        );
    }

    public bool CanToUploadToWorkshop() {
        return true;
    }

    public void PrepareForWorkshopUpload() {
        this.stagedDir = PublishStager.Stage(this.mod, ResolveIncludedFiles());
    }

    public PublishedFileId_t GetPublishedFileId() {
        return this.mod.GetPublishedFileId();
    }

    public void SetPublishedFileId(PublishedFileId_t pfid) {
        this.mod.SetPublishedFileId(pfid);
    }

    public string GetWorkshopName() {
        if (!string.IsNullOrEmpty(this.sidecar.TitleOverride)) {
            return this.sidecar.TitleOverride!;
        }

        return this.mod.GetWorkshopName();
    }

    public string GetWorkshopDescription() {
        return this.mod.GetWorkshopDescription();
    }

    public string GetWorkshopPreviewImagePath() {
        if (string.IsNullOrEmpty(this.sidecar.PreviewOverride)) {
            return this.mod.GetWorkshopPreviewImagePath();
        }

        string preview = this.sidecar.PreviewOverride!;
        if (Path.IsPathRooted(preview)) {
            return preview;
        }

        return Path.Combine(this.mod.RootDir.FullName, preview);
    }

    public IList<string> GetWorkshopTags() {
        if (this.sidecar.Tags.Count > 0) {
            return this.sidecar.Tags;
        }

        return this.mod.GetWorkshopTags();
    }

    public DirectoryInfo GetWorkshopUploadDirectory() {
        if (this.stagedDir != null) {
            return this.stagedDir;
        }

        return PublishStager.Stage(this.mod, ResolveIncludedFiles());
    }

    public WorkshopItemHook GetWorkshopItemHook() {
        return this.hook ??= new WorkshopItemHook(this);
    }

    public IEnumerable<System.Version> SupportedVersions => this.mod.SupportedVersionsReadOnly;

    /// <summary>Invokes vanilla <c>Workshop.Upload</c> for this package via reflection.</summary>
    public void Submit() {
        UploadMethod?.Invoke(null, new object[] { this });
    }
}
