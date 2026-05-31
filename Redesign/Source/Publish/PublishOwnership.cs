using System.Collections.Generic;
using Steamworks;
using Verse.Steam;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Caches Steam Workshop ownership per PublishedFileId so the publish gate can offer
/// "Update" only on items the local user owns. The SteamUGC details query is async;
/// <see cref="IsOwned"/> stays synchronous by returning the cached verdict and kicking
/// off the query once per id. While a query is pending the verdict is "not owned", so the
/// button stays hidden until Steam confirms ownership. Steam pumps the CallResult through
/// its per-frame RunCallbacks, so completion lands on the main thread - no locking needed.
/// This is the integration layer (verified by in-game smoke test); the pure gate decision
/// lives in <see cref="PublishEligibility"/> and is unit-tested.
/// </summary>
public static class PublishOwnership {
    private enum OwnershipState {
        Pending,
        Owned,
        NotOwned,
        Unavailable,
    }

    private const uint CacheMaxAgeSeconds = 300u;

    private static readonly Dictionary<ulong, OwnershipState> States = [];
    private static readonly Dictionary<ulong, ulong> HandleToId = [];
    private static readonly Dictionary<ulong, CallResult<SteamUGCQueryCompleted_t>> LiveQueries = [];

    public static bool IsOwned(PublishedFileId_t id) {
        if (id == PublishedFileId_t.Invalid) {
            return false;
        }

        ulong key = id.m_PublishedFileId;
        if (States.TryGetValue(key, out OwnershipState state)) {
            return state == OwnershipState.Owned;
        }

        States[key] = OwnershipState.Pending;
        BeginQuery(id);
        return false;
    }

    private static void BeginQuery(PublishedFileId_t id) {
        if (!SteamManager.Initialized) {
            States[id.m_PublishedFileId] = OwnershipState.Unavailable;
            return;
        }

        PublishedFileId_t[] ids = [id];
        UGCQueryHandle_t handle = SteamUGC.CreateQueryUGCDetailsRequest(ids, 1u);
        if (handle == UGCQueryHandle_t.Invalid) {
            States[id.m_PublishedFileId] = OwnershipState.Unavailable;
            return;
        }

        SteamUGC.SetAllowCachedResponse(handle, CacheMaxAgeSeconds);
        SteamAPICall_t call = SteamUGC.SendQueryUGCRequest(handle);

        CallResult<SteamUGCQueryCompleted_t> query = CallResult<SteamUGCQueryCompleted_t>.Create(OnQueryCompleted);
        query.Set(call, OnQueryCompleted);

        HandleToId[handle.m_UGCQueryHandle] = id.m_PublishedFileId;
        LiveQueries[handle.m_UGCQueryHandle] = query;
    }

    private static void OnQueryCompleted(SteamUGCQueryCompleted_t result, bool ioFailure) {
        ulong handleKey = result.m_handle.m_UGCQueryHandle;
        if (!HandleToId.TryGetValue(handleKey, out ulong id)) {
            SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
            return;
        }

        HandleToId.Remove(handleKey);
        LiveQueries.Remove(handleKey);
        States[id] = ResolveState(result, ioFailure);
        SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
    }

    private static OwnershipState ResolveState(SteamUGCQueryCompleted_t result, bool ioFailure) {
        if (ioFailure || result.m_eResult != EResult.k_EResultOK || result.m_unNumResultsReturned < 1u) {
            return OwnershipState.Unavailable;
        }

        if (!SteamUGC.GetQueryUGCResult(result.m_handle, 0u, out SteamUGCDetails_t details)) {
            return OwnershipState.Unavailable;
        }

        ulong localUser = SteamUser.GetSteamID().m_SteamID;
        return details.m_ulSteamIDOwner == localUser ? OwnershipState.Owned : OwnershipState.NotOwned;
    }
}
