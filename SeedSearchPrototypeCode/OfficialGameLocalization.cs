using System.Text.Json;
using Godot;

namespace SeedSearchPrototype;

/// <summary>
/// Reads entity names straight from the game's own localization files in the
/// loaded resource pack. No names are copied into this mod, so the strings
/// follow the installed game version and language automatically.
/// </summary>
public static class OfficialGameLocalization
{
    private static readonly Dictionary<string, string> Names =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] Tables =
    {
        "relics", "cards", "potions", "events", "ancients", "encounters",
        "characters", "acts",
    };

    private static readonly string[] CharacterSuffixes =
    {
        "IRONCLAD", "SILENT", "REGENT", "DEFECT", "NECROBINDER", "ANY",
    };

    private static string _loadedLanguage = "";

    public static void Attach()
    {
        Localization.NameResolver = Name;
        Localization.LoadLanguage = TryLoad;
    }

    public static void TryLoad(string language)
    {
        var code = string.IsNullOrWhiteSpace(language) ? "eng" : language.Trim().ToLowerInvariant();
        if (code == _loadedLanguage && Names.Count > 0)
        {
            return;
        }

        Names.Clear();
        _loadedLanguage = code;
        var foundAny = false;
        foreach (var table in Tables)
        {
            var path = $"res://localization/{code}/{table}.json";
            if (!Godot.FileAccess.FileExists(path))
            {
                continue;
            }

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                continue;
            }

            try
            {
                foundAny = true;
                using var doc = JsonDocument.Parse(file.GetAsText());
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    AddTitleKey(property.Name, property.Value.GetString());
                }
            }
            catch (Exception exception)
            {
                MainFile.Logger.Warn(
                    $"Could not read official localization table {path}: {exception.Message}");
            }
        }

        if (!foundAny)
        {
            Names.Clear();
            _loadedLanguage = "";
        }
    }

    public static string Name(string id, string fallback)
    {
        if (Names.Count == 0)
        {
            return fallback;
        }

        return Names.TryGetValue(Normalize(id), out var name) ? name : fallback;
    }

    private static void AddTitleKey(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            (!key.EndsWith(".title", StringComparison.Ordinal) &&
             !key.EndsWith(".name", StringComparison.Ordinal)))
        {
            return;
        }

        var keySuffix = key.EndsWith(".title", StringComparison.Ordinal) ? ".title" : ".name";
        var prefix = key[..^keySuffix.Length];
        var normalized = Normalize(prefix);
        Names.TryAdd(normalized, value);

        if (normalized.EndsWith("boss", StringComparison.Ordinal) && normalized.Length > 4)
        {
            Names.TryAdd(normalized[..^4], value);
        }

        foreach (var suffix in CharacterSuffixes)
        {
            var shortSuffix = suffix.ToLowerInvariant();
            if (normalized.EndsWith(shortSuffix, StringComparison.Ordinal) &&
                normalized.Length > shortSuffix.Length)
            {
                Names.TryAdd(normalized[..^shortSuffix.Length], value);
            }
        }
    }

    private static string Normalize(string id) =>
        new string(id.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
