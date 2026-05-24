using System;
using System.Collections.Generic;
using CryptikLemur.RimLogging;
using CryptikLemur.RimLogging.Bundle;
using CryptikLemur.RimLogging.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using LogEntry = CryptikLemur.RimLogging.LogEntry;

namespace Cosmere.Lightweave.Redesign.Logging;

internal static class LogBundleShare {
    public static async void Upload(LightweaveLogSink sink, LogViewerState state, Action invalidate) {
        state.Uploading = true;
        invalidate();
        try {
            IReadOnlyList<LogEntry> entries = sink.Snapshot();
            BundlePayload payload = BundlerSessionFactory.BuildForRunningSession(entries);
            ProxyClient proxy = new ProxyClient(LoggingMod.Settings.proxyUrl);
            ProxyResult result = await proxy.UploadAsync(payload).ConfigureAwait(false);
            if (result.Success && !string.IsNullOrEmpty(result.GistUrl)) {
                GUIUtility.systemCopyBuffer = result.GistUrl;
                Messages.Message(
                    (string)"CL_LogViewer_BundleShared".Translate(result.GistUrl!.Named("URL")),
                    MessageTypeDefOf.PositiveEvent,
                    false
                );
            }
            else {
                RedesignLog.Error($"Bug bundle upload failed: {result.ErrorMessage ?? "(no error message)"}");
            }
        }
        catch (Exception ex) {
            RedesignLog.Error($"Bug bundle upload failed: {ex}");
        }
        finally {
            state.Uploading = false;
            invalidate();
        }
    }
}
