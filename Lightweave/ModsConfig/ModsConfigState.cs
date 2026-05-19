using System;
using System.Reflection;
using Cosmere.Lightweave.Runtime;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.ModsConfig;

internal static class ModsConfigState {
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly FieldInfo? ActiveModsWhenOpenedHashField =
        typeof(Page_ModsConfig).GetField("activeModsWhenOpenedHash", PrivateInstance);

    private static readonly FieldInfo? SaveChangesField =
        typeof(Page_ModsConfig).GetField("saveChanges", PrivateInstance);

    private static readonly FieldInfo? DiscardChangesField =
        typeof(Page_ModsConfig).GetField("discardChanges", PrivateInstance);

    public static bool HasUnsavedChanges(Page_ModsConfig page) {
        try {
            if (ActiveModsWhenOpenedHashField == null) {
                return true;
            }
            int opened = (int)(ActiveModsWhenOpenedHashField.GetValue(page) ?? 0);
            int current = ModLister.InstalledModsListHash(activeOnly: true);
            return opened != current;
        }
        catch (Exception ex) {
            LightweaveLog.Error("HasUnsavedChanges reflection failed: " + ex);
            return true;
        }
    }

    public static bool GetSaveChanges(Page_ModsConfig page) {
        return GetPrivateBool(page, SaveChangesField, "saveChanges");
    }

    public static bool GetDiscardChanges(Page_ModsConfig page) {
        return GetPrivateBool(page, DiscardChangesField, "discardChanges");
    }

    public static void SetSaveChanges(Page_ModsConfig page, bool value) {
        SetPrivateBool(page, SaveChangesField, "saveChanges", value);
    }

    public static void SetDiscardChanges(Page_ModsConfig page, bool value) {
        SetPrivateBool(page, DiscardChangesField, "discardChanges", value);
    }

    private static bool GetPrivateBool(Page_ModsConfig page, FieldInfo? field, string fieldName) {
        try {
            if (field == null) {
                LightweaveLog.Error("Page_ModsConfig." + fieldName + " field not found via reflection.");
                return false;
            }
            return (bool)(field.GetValue(page) ?? false);
        }
        catch (Exception ex) {
            LightweaveLog.Error("GetPrivateBool(" + fieldName + ") failed: " + ex);
            return false;
        }
    }

    private static void SetPrivateBool(Page_ModsConfig page, FieldInfo? field, string fieldName, bool value) {
        try {
            if (field == null) {
                LightweaveLog.Error("Page_ModsConfig." + fieldName + " field not found via reflection.");
                return;
            }
            field.SetValue(page, value);
        }
        catch (Exception ex) {
            LightweaveLog.Error("SetPrivateBool(" + fieldName + ") failed: " + ex);
        }
    }
}
