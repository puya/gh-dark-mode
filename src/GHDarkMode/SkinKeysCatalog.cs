using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GHDarkMode;

/// <summary>
/// Loads embedded skin-keys-manifest.json: curated favorites first, then remaining XML color keys.
/// Regenerate the manifest with scripts/extract_skin_keys_from_xml.py when Grasshopper adds keys.
/// </summary>
internal static class SkinKeysCatalog
{
    private const string EmbeddedLogicalName = "GHDarkMode.Resources.skin-keys-manifest.json";

    private static readonly object Sync = new();
    private static IReadOnlyList<SkinKeyEntry>? _merged;

    internal sealed record SkinKeyEntry(int Index, string Label, string XmlKey);

    internal static IReadOnlyList<SkinKeyEntry> GetMergedEntries()
    {
        lock (Sync)
        {
            if (_merged is not null)
                return _merged;

            ManifestDto? dto = LoadManifest();
            if (dto is null)
            {
                _merged = new List<SkinKeyEntry>
                {
                    new(0, "canvas_backcolor", "canvas_backcolor"),
                };
                return _merged;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<SkinKeyEntry>();
            int idx = 0;

            foreach (ManifestFavoriteDto fav in dto.Favorites ?? Array.Empty<ManifestFavoriteDto>())
            {
                if (string.IsNullOrWhiteSpace(fav.Key))
                    continue;
                if (!seen.Add(fav.Key))
                    continue;
                string label = string.IsNullOrWhiteSpace(fav.Label) ? fav.Key : fav.Label;
                list.Add(new SkinKeyEntry(idx++, label, fav.Key));
            }

            foreach (string key in (dto.Keys ?? Array.Empty<string>()).OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))
                    continue;
                list.Add(new SkinKeyEntry(idx++, key, key));
            }

            _merged = list;
            return _merged;
        }
    }

    internal static string GetXmlKey(int index)
    {
        IReadOnlyList<SkinKeyEntry> entries = GetMergedEntries();
        if (index < 0 || index >= entries.Count)
            return entries[0].XmlKey;
        return entries[index].XmlKey;
    }

    private static ManifestDto? LoadManifest()
    {
        Assembly asm = typeof(SkinKeysCatalog).Assembly;
        Stream? stream = asm.GetManifestResourceStream(EmbeddedLogicalName);
        if (stream is null)
        {
            string? fallbackName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("skin-keys-manifest.json", StringComparison.OrdinalIgnoreCase));
            if (fallbackName is not null)
                stream = asm.GetManifestResourceStream(fallbackName);
        }

        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<ManifestDto>(json, SerializerOptions);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class ManifestDto
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("favorites")]
        public ManifestFavoriteDto[]? Favorites { get; set; }

        [JsonPropertyName("keys")]
        public string[]? Keys { get; set; }
    }

    private sealed class ManifestFavoriteDto
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }
}
