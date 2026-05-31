using System;
using System.Collections.Generic;
using System.Reflection;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Runtime;

namespace Cosmere.Lightweave.Playground;

internal static class DocReflection {
    private const BindingFlags MemberFlags =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly IReadOnlyList<PlaygroundVariant> EmptyVariants = Array.Empty<PlaygroundVariant>();
    private static readonly IReadOnlyList<PlaygroundState> EmptyStates = Array.Empty<PlaygroundState>();

    private const string TypographyPageId = "typography";
    private static readonly string[] TypographyMemberIds =
        ["heading", "text", "label", "caption", "richtext", "code", "icon"];

    private static Dictionary<string, Type>? primitiveIndex;
    private static readonly object indexLock = new object();

    internal static Type? GetPrimitiveType(string id) {
        EnsureIndexBuilt();
        return primitiveIndex!.TryGetValue(id, out Type? t) ? t : null;
    }


    internal static float? GetPreferredVariantHeight(string id) {
        Type? primitive = GetPrimitiveType(id);
        DocAttribute? doc = primitive?.GetCustomAttribute<DocAttribute>();
        if (doc == null) {
            return null;
        }
        return doc.PreferredVariantHeight > 0f ? doc.PreferredVariantHeight : null;
    }

    internal static (IReadOnlyList<PlaygroundVariant> variants, IReadOnlyList<PlaygroundState> states) BuildSamplesById(
        string id
    ) {
        if (id == TypographyPageId) {
            return (BuildTypographyVariants(), EmptyStates);
        }

        Type? primitive = GetPrimitiveType(id);
        if (primitive == null) {
            return (EmptyVariants, EmptyStates);
        }

        return (BuildVariants(primitive), BuildStates(primitive));
    }

    internal static PlaygroundDocs? BuildDocsById(string id) {
        if (id == TypographyPageId) {
            return new PlaygroundDocs(HideUsage: true, HideSource: true);
        }

        Type? primitive = GetPrimitiveType(id);
        if (primitive == null) {
            return null;
        }

        PlaygroundDocs docs = Build(primitive);
        if (docs.UsageCode == null
            && docs.Composition == null
            && docs.ApiReference == null
            && !docs.HideUsage
            && !docs.HideSource) {
            return null;
        }

        return docs;
    }

    internal static IEnumerable<KeyValuePair<string, Type>> EnumerateRegistered() {
        EnsureIndexBuilt();
        return primitiveIndex!;
    }

    private static void EnsureIndexBuilt() {
        if (primitiveIndex != null) {
            return;
        }

        lock (indexLock) {
            if (primitiveIndex != null) {
                return;
            }

            Dictionary<string, Type> map = new Dictionary<string, Type>(StringComparer.Ordinal);
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < asms.Length; a++) {
                Type[] types;
                try {
                    types = asms[a].GetTypes();
                }
                catch (ReflectionTypeLoadException ex) {
                    types = ex.Types ?? Array.Empty<Type>();
                }

                for (int t = 0; t < types.Length; t++) {
                    Type type = types[t];
                    if (type == null) {
                        continue;
                    }

                    DocAttribute? doc = type.GetCustomAttribute<DocAttribute>(false);
                    if (doc == null || string.IsNullOrEmpty(doc.Id)) {
                        continue;
                    }

                    map[doc.Id] = type;
                }
            }

            primitiveIndex = map;
        }
    }

    internal static PlaygroundDocs Build(Type primitive) {
        DocAttribute? root = primitive.GetCustomAttribute<DocAttribute>();
        if (root == null) {
            return new PlaygroundDocs();
        }

        IReadOnlyList<CompositionLine> composition = BuildComposition(primitive);
        IReadOnlyList<ApiGroup> apiGroups = BuildApiGroups(primitive);
        string? usage = BuildUsageCode(primitive);

        return new PlaygroundDocs(
            UsageCode: usage,
            Composition: composition.Count > 0 ? composition : null,
            ApiReference: apiGroups.Count > 0 ? apiGroups : null,
            ShowRtl: root.ShowRtl,
            HideUsage: root.HideUsage,
            HideSource: root.HideSource
        );
    }

    internal static IReadOnlyList<PlaygroundVariant> BuildVariants(Type primitive) {
        return BuildSamples<DocVariantAttribute, PlaygroundVariant>(
            primitive,
            (attr, sample, factory) => new PlaygroundVariant(attr.LabelKey, () => (factory() ?? sample).Build(), sample.Code) {
                HideCode = attr.HideCode,
            },
            attr => attr.Order
        );
    }

    internal static IReadOnlyList<PlaygroundState> BuildStates(Type primitive) {
        return BuildSamples<DocStateAttribute, PlaygroundState>(
            primitive,
            (attr, sample, factory) => new PlaygroundState(attr.LabelKey, () => (factory() ?? sample).Build(), sample.Code) {
                HideCode = attr.HideCode,
            },
            attr => attr.Order
        );
    }

    private static IReadOnlyList<TItem> BuildSamples<TAttr, TItem>(
        Type primitive,
        Func<TAttr, DocSample, Func<DocSample?>, TItem> map,
        Func<TAttr, int> orderOf
    ) where TAttr : Attribute {
        List<(TAttr attr, MemberInfo member)> sources = GetCachedSources<TAttr>(primitive, orderOf);
        if (sources.Count == 0) {
            return Array.Empty<TItem>();
        }

        List<TItem> result = new List<TItem>(sources.Count);
        for (int i = 0; i < sources.Count; i++) {
            TAttr attr = sources[i].attr;
            MemberInfo member = sources[i].member;
            DocSample fallback = GetCachedFallback(member);
            Func<DocSample?> factory = () => InvokeForSample(member);
            result.Add(map(attr, fallback, factory));
        }

        return result;
    }

    private static readonly Dictionary<(Type, Type), object> CachedSources = new Dictionary<(Type, Type), object>();
    private static readonly Dictionary<MemberInfo, DocSample> CachedFallbacks = new Dictionary<MemberInfo, DocSample>();
    private static readonly LightweaveNode PlaceholderNode = NodeBuilder.New("DocSamplePlaceholder", 0, string.Empty);

    private static List<(TAttr attr, MemberInfo member)> GetCachedSources<TAttr>(Type primitive, Func<TAttr, int> orderOf) where TAttr : Attribute {
        (Type, Type) key = (primitive, typeof(TAttr));
        if (CachedSources.TryGetValue(key, out object? cached)) {
            return (List<(TAttr, MemberInfo)>)cached;
        }

        List<(TAttr attr, MemberInfo member)> sources = new List<(TAttr attr, MemberInfo member)>();
        MethodInfo[] methods = primitive.GetMethods(MemberFlags);
        for (int i = 0; i < methods.Length; i++) {
            TAttr? attr = methods[i].GetCustomAttribute<TAttr>();
            if (attr == null) {
                continue;
            }

            sources.Add((attr, methods[i]));
        }

        FieldInfo[] fields = primitive.GetFields(MemberFlags);
        for (int i = 0; i < fields.Length; i++) {
            TAttr? attr = fields[i].GetCustomAttribute<TAttr>();
            if (attr == null) {
                continue;
            }

            sources.Add((attr, fields[i]));
        }

        PropertyInfo[] props = primitive.GetProperties(MemberFlags);
        for (int i = 0; i < props.Length; i++) {
            TAttr? attr = props[i].GetCustomAttribute<TAttr>();
            if (attr == null) {
                continue;
            }

            sources.Add((attr, props[i]));
        }

        sources.Sort((a, b) => orderOf(a.attr).CompareTo(orderOf(b.attr)));
        CachedSources[key] = sources;
        return sources;
    }

    private static DocSample GetCachedFallback(MemberInfo member) {
        if (CachedFallbacks.TryGetValue(member, out DocSample? cached)) {
            return cached;
        }

        DocSample? sample = InvokeForSample(member);
        DocSample fallback = sample ?? new DocSample(() => PlaceholderNode);
        CachedFallbacks[member] = fallback;
        return fallback;
    }

    private static DocSample? InvokeForSample(MemberInfo member) {
        try {
            object? value;
            if (member is MethodInfo m) {
                value = m.Invoke(null, null);
            }
            else if (member is FieldInfo f) {
                value = f.GetValue(null);
            }
            else if (member is PropertyInfo p) {
                value = p.GetValue(null);
            }
            else {
                return null;
            }

            return value as DocSample;
        }
        catch (TargetInvocationException ex) {
            LightweaveLog.Error($"DocSample provider {member.DeclaringType?.Name}.{member.Name} threw: {ex.InnerException ?? ex}");
            return null;
        }
        catch (Exception ex) {
            LightweaveLog.Error($"Failed to read DocSample from {member.DeclaringType?.Name}.{member.Name}: {ex}");
            return null;
        }
    }

    private static IReadOnlyList<CompositionLine> BuildComposition(Type primitive) {
        DocAttribute? rootDoc = primitive.GetCustomAttribute<DocAttribute>();
        string rootName = rootDoc != null && rootDoc.Label.Length > 0
            ? rootDoc.Label
            : primitive.Name;
        List<CompositionLine> lines = new List<CompositionLine> {
            new CompositionLine(0, rootName),
        };

        Dictionary<string, List<MethodInfo>> children = new Dictionary<string, List<MethodInfo>>(StringComparer.Ordinal);
        bool hasAnySlot = false;
        MethodInfo[] methods = primitive.GetMethods(MemberFlags);
        for (int i = 0; i < methods.Length; i++) {
            DocAttribute? d = methods[i].GetCustomAttribute<DocAttribute>();
            if (d == null || !d.Slot) continue;
            hasAnySlot = true;
            string parent = d.ParentSlot ?? "";
            if (!children.TryGetValue(parent, out List<MethodInfo>? list)) {
                list = new List<MethodInfo>();
                children[parent] = list;
            }
            list.Add(methods[i]);
        }

        if (!hasAnySlot) {
            return Array.Empty<CompositionLine>();
        }

        AppendChildren(rootName, "", 1, children, lines);
        return lines;
    }

    private static void AppendChildren(
        string rootName,
        string parentName,
        int indent,
        Dictionary<string, List<MethodInfo>> children,
        List<CompositionLine> lines
    ) {
        if (!children.TryGetValue(parentName, out List<MethodInfo>? kids)) return;
        for (int i = 0; i < kids.Count; i++) {
            MethodInfo mi = kids[i];
            DocAttribute? d = mi.GetCustomAttribute<DocAttribute>();
            string label;
            if (d != null && d.Label.Length > 0) {
                int parenIdx = d.Label.IndexOf('(');
                label = parenIdx > 0 ? d.Label.Substring(0, parenIdx) : d.Label;
                if (label.EndsWith(".Create")) {
                    label = label.Substring(0, label.Length - 7);
                }
            }
            else {
                label = $"{rootName}.{mi.Name}";
            }
            lines.Add(new CompositionLine(indent, label));
            AppendChildren(rootName, mi.Name, indent + 1, children, lines);
        }
    }

    private static IReadOnlyList<ApiGroup> BuildApiGroups(Type primitive) {
        List<ApiParam> mainParams = new List<ApiParam>();
        List<ApiGroup> slotGroups = new List<ApiGroup>();
        HashSet<string> mainSeen = new HashSet<string>(StringComparer.Ordinal);
        MethodInfo[] methods = primitive.GetMethods(MemberFlags);
        for (int m = 0; m < methods.Length; m++) {
            MethodInfo method = methods[m];
            DocAttribute? d = method.GetCustomAttribute<DocAttribute>();
            bool isSlot = d != null && d.Slot;

            ParameterInfo[] parms = method.GetParameters();
            List<ApiParam> slotParams = isSlot ? new List<ApiParam>() : null!;
            HashSet<string> slotSeen = isSlot ? new HashSet<string>(StringComparer.Ordinal) : null!;

            for (int p = 0; p < parms.Length; p++) {
                ParameterInfo pi = parms[p];
                DocParamAttribute? pa = pi.GetCustomAttribute<DocParamAttribute>();
                if (pa == null) continue;
                string paramName = pi.Name ?? "";

                HashSet<string> seen = isSlot ? slotSeen : mainSeen;
                if (seen.Contains(paramName)) continue;
                seen.Add(paramName);

                string typeStr = pa.TypeOverride.Length > 0 ? pa.TypeOverride : FormatType(pi);
                string defaultStr;
                if (pa.DefaultOverride.Length > 0) {
                    defaultStr = pa.DefaultOverride;
                }
                else if (pi.HasDefaultValue) {
                    defaultStr = FormatDefault(pi.DefaultValue);
                }
                else {
                    defaultStr = "-";
                }

                ApiParam entry = new ApiParam(paramName, typeStr, defaultStr, pa.Description);
                if (isSlot) {
                    slotParams.Add(entry);
                }
                else {
                    mainParams.Add(entry);
                }
            }

            if (isSlot && slotParams.Count > 0) {
                string label = d != null && d.Label.Length > 0 ? d.Label : $"{primitive.Name}.{method.Name}()";
                slotGroups.Add(new ApiGroup(label, slotParams));
            }
        }

        DocAttribute? root = primitive.GetCustomAttribute<DocAttribute>();
        Type? target = root?.Target;
        if (target != null) {
            BindingFlags vflags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            string targetMember = root!.TargetMember;
            Type? cursor = target;
            while (cursor != null && cursor != typeof(object)) {
                PropertyInfo[] props = cursor.GetProperties(vflags);
                for (int i = 0; i < props.Length; i++) {
                    PropertyInfo prop = props[i];
                    DocOverrideAttribute? doa = prop.GetCustomAttribute<DocOverrideAttribute>();
                    if (doa == null) continue;
                    if (mainSeen.Contains(prop.Name)) continue;
                    mainSeen.Add(prop.Name);

                    string typeStr = doa.TypeOverride.Length > 0 ? doa.TypeOverride : FormatTypeName(prop.PropertyType);
                    string defaultStr = doa.DefaultOverride.Length > 0 ? doa.DefaultOverride : "-";
                    mainParams.Add(new ApiParam(prop.Name, typeStr, defaultStr, doa.Description));
                }

                MethodInfo[] tmethods = cursor.GetMethods(vflags);
                for (int i = 0; i < tmethods.Length; i++) {
                    MethodInfo mi = tmethods[i];
                    if (mi.IsSpecialName) continue;
                    DocOverrideAttribute? doa = mi.GetCustomAttribute<DocOverrideAttribute>();
                    if (doa == null) continue;
                    if (mainSeen.Contains(mi.Name)) continue;
                    mainSeen.Add(mi.Name);

                    string typeStr = doa.TypeOverride.Length > 0 ? doa.TypeOverride : FormatTypeName(mi.ReturnType) + "()";
                    string defaultStr = doa.DefaultOverride.Length > 0 ? doa.DefaultOverride : "-";
                    mainParams.Add(new ApiParam(mi.Name, typeStr, defaultStr, doa.Description));
                }

                if (targetMember.Length > 0) {
                    for (int i = 0; i < tmethods.Length; i++) {
                        MethodInfo mi = tmethods[i];
                        if (mi.IsSpecialName) continue;
                        if (!string.Equals(mi.Name, targetMember, StringComparison.Ordinal)) continue;

                        ParameterInfo[] tparms = mi.GetParameters();
                        for (int p = 0; p < tparms.Length; p++) {
                            ParameterInfo pi = tparms[p];
                            DocParamAttribute? pa = pi.GetCustomAttribute<DocParamAttribute>();
                            if (pa == null) continue;
                            string paramName = pi.Name ?? "";
                            if (mainSeen.Contains(paramName)) continue;
                            mainSeen.Add(paramName);

                            string typeStr = pa.TypeOverride.Length > 0 ? pa.TypeOverride : FormatType(pi);
                            string defaultStr;
                            if (pa.DefaultOverride.Length > 0) {
                                defaultStr = pa.DefaultOverride;
                            }
                            else if (pi.HasDefaultValue) {
                                defaultStr = FormatDefault(pi.DefaultValue);
                            }
                            else {
                                defaultStr = "-";
                            }

                            mainParams.Add(new ApiParam(paramName, typeStr, defaultStr, pa.Description));
                        }
                    }
                }

                cursor = cursor.BaseType;
            }
        }

        List<ApiGroup> groups = new List<ApiGroup>();
        if (mainParams.Count > 0) {
            groups.Add(new ApiGroup(string.Empty, mainParams));
        }

        groups.AddRange(slotGroups);
        return groups;
    }

    private static string FormatType(ParameterInfo pi) {
        Type t = pi.ParameterType;
        if (t.IsArray) {
            string inner = t.GetElementType()?.Name ?? "object";
            return $"{inner}[]";
        }

        return t.Name;
    }


    private static string FormatTypeName(Type t) {
        if (t.IsArray) {
            string inner = t.GetElementType()?.Name ?? "object";
            return $"{inner}[]";
        }

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
            return FormatTypeName(t.GetGenericArguments()[0]) + "?";
        }

        return t.Name;
    }

    private static string FormatDefault(object? v) {
        if (v == null) return "null";
        if (v is string s) return $"\"{s}\"";
        if (v is bool b) return b ? "true" : "false";
        return v.ToString() ?? "";
    }

    private static string? BuildUsageCode(Type primitive) {
        return GetUsageSample(primitive)?.Code;
    }

    private static DocSample? GetUsageSample(Type primitive) {
        MethodInfo[] methods = primitive.GetMethods(MemberFlags);
        for (int i = 0; i < methods.Length; i++) {
            if (methods[i].GetCustomAttribute<DocUsageAttribute>() == null) continue;
            try {
                object? result = methods[i].Invoke(null, null);
                if (result is DocSample sample) return sample;
            }
            catch (TargetInvocationException ex) {
                LightweaveLog.Error($"DocUsage provider {primitive.Name}.{methods[i].Name} threw: {ex.InnerException ?? ex}");
            }
            catch (Exception ex) {
                LightweaveLog.Error($"Failed to read DocUsage from {primitive.Name}.{methods[i].Name}: {ex}");
            }
        }

        FieldInfo[] fields = primitive.GetFields(MemberFlags);
        for (int i = 0; i < fields.Length; i++) {
            if (fields[i].GetCustomAttribute<DocUsageAttribute>() == null) continue;
            try {
                object? value = fields[i].GetValue(null);
                if (value is DocSample sample) return sample;
            }
            catch (Exception ex) {
                LightweaveLog.Error($"Failed to read DocUsage field {primitive.Name}.{fields[i].Name}: {ex}");
            }
        }

        PropertyInfo[] props = primitive.GetProperties(MemberFlags);
        for (int i = 0; i < props.Length; i++) {
            if (props[i].GetCustomAttribute<DocUsageAttribute>() == null) continue;
            try {
                object? value = props[i].GetValue(null);
                if (value is DocSample sample) return sample;
            }
            catch (TargetInvocationException ex) {
                LightweaveLog.Error($"DocUsage property {primitive.Name}.{props[i].Name} threw: {ex.InnerException ?? ex}");
            }
            catch (Exception ex) {
                LightweaveLog.Error($"Failed to read DocUsage property {primitive.Name}.{props[i].Name}: {ex}");
            }
        }

        return null;
    }

    private static IReadOnlyList<PlaygroundVariant> BuildTypographyVariants() {
        List<PlaygroundVariant> variants = new List<PlaygroundVariant>(TypographyMemberIds.Length);
        for (int i = 0; i < TypographyMemberIds.Length; i++) {
            string memberId = TypographyMemberIds[i];
            Type? primitive = GetPrimitiveType(memberId);
            if (primitive == null) {
                continue;
            }

            DocSample? sample = GetUsageSample(primitive);
            if (sample == null) {
                continue;
            }

            DocSample resolved = sample;
            variants.Add(new PlaygroundVariant(
                "CL_Playground_" + memberId + "_Title",
                () => resolved.Build(),
                resolved.Code
            ));
        }

        return variants;
    }
}
