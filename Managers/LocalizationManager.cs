#nullable enable
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using YamlDotNet.Serialization;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace ChallengeChest.Managers.LocalizationManager;

[PublicAPI]
public class Localizer
{
    private static readonly Dictionary<string, Dictionary<string, Func<string>>> PlaceholderProcessors = new();

    private static readonly Dictionary<string, Dictionary<string, string>> LoadedTexts = new();

    private static readonly ConditionalWeakTable<Localization, string> LocalizationLanguage = new();

    private static readonly List<WeakReference<Localization>> LocalizationObjects = [];

    private static BaseUnityPlugin? _plugin;

    private static BaseUnityPlugin Plugin
    {
        get
        {
            if (_plugin is null)
            {
                IEnumerable<TypeInfo> types;
                try
                {
                    types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
                }

                _plugin = (BaseUnityPlugin)Chainloader.ManagerObject.GetComponent(types.First(t =>
                    t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));
            }

            return _plugin;
        }
    }

    private static readonly List<string> FileExtensions = [".json", ".yml"];

    private static void UpdatePlaceholderText(Localization localization, string key)
    {
        LocalizationLanguage.TryGetValue(localization, out var language);
        var text = LoadedTexts[language][key];
        if (PlaceholderProcessors.TryGetValue(key, out var textProcessors))
        {
            text = textProcessors.Aggregate(text, (current, kv) => current.Replace("{" + kv.Key + "}", kv.Value()));
        }

        localization.AddWord(key, text);
    }

    public static void AddPlaceholder<T>(string key, string placeholder, ConfigEntry<T> config,
        Func<T, string>? convertConfigValue = null) where T : notnull
    {
        convertConfigValue ??= val => val.ToString();
        if (!PlaceholderProcessors.ContainsKey(key))
        {
            PlaceholderProcessors[key] = new Dictionary<string, Func<string>>();
        }

        void UpdatePlaceholder()
        {
            PlaceholderProcessors[key][placeholder] = () => convertConfigValue(config.Value);
            UpdatePlaceholderText(Localization.instance, key);
        }

        config.SettingChanged += (_, _) => UpdatePlaceholder();
        if (LoadedTexts.ContainsKey(Localization.instance.GetSelectedLanguage()))
        {
            UpdatePlaceholder();
        }
    }

    public static void AddText(string key, string text)
    {
        List<WeakReference<Localization>> remove = [];
        foreach (var reference in LocalizationObjects)
        {
            if (reference.TryGetTarget(out var localization))
            {
                var texts = LoadedTexts[LocalizationLanguage.GetOrCreateValue(localization)];
                if (!localization.m_translations.ContainsKey(key))
                {
                    texts[key] = text;
                    localization.AddWord(key, text);
                }
            } else
            {
                remove.Add(reference);
            }
        }

        foreach (var reference in remove)
        {
            LocalizationObjects.Remove(reference);
        }
    }

    public static void Load() => LoadLocalization(Localization.instance, Localization.instance.GetSelectedLanguage());

    private static void LoadLocalization(Localization __instance, string language)
    {
        if (!LocalizationLanguage.Remove(__instance))
        {
            LocalizationObjects.Add(new WeakReference<Localization>(__instance));
        }

        LocalizationLanguage.Add(__instance, language);

        Dictionary<string, string> localizationFiles = new();
        foreach (var file in Directory
                     .GetFiles(Path.GetDirectoryName(Paths.PluginPath)!, $"{Plugin.Info.Metadata.Name}.*",
                         SearchOption.AllDirectories).Where(f => FileExtensions.IndexOf(Path.GetExtension(f)) >= 0))
        {
            var key = Path.GetFileNameWithoutExtension(file).Split('.')[1];
            if (localizationFiles.ContainsKey(key))
            {
                // Handle duplicate key
                Debug.LogWarning(
                    $"Duplicate key {key} found for {Plugin.Info.Metadata.Name}. The duplicate file found at {file} will be skipped.");
            } else
            {
                localizationFiles[key] = file;
            }
        }

        if (LoadTranslationFromAssembly("English") is not { } englishAssemblyData)
        {
            throw new Exception(
                $"Found no English localizations in mod {Plugin.Info.Metadata.Name}. Expected an embedded resource translations/English.json or translations/English.yml.");
        }

        var localizationTexts = new DeserializerBuilder().IgnoreFields().Build()
            .Deserialize<Dictionary<string, string>?>(Encoding.UTF8.GetString(englishAssemblyData));
        if (localizationTexts is null)
        {
            throw new Exception(
                $"Localization for mod {Plugin.Info.Metadata.Name} failed: Localization file was empty.");
        }

        string? localizationData = null;
        if (language != "English")
        {
            if (localizationFiles.TryGetValue(language, out var file))
            {
                localizationData = File.ReadAllText(file);
            } else if (LoadTranslationFromAssembly(language) is { } languageAssemblyData)
            {
                localizationData = Encoding.UTF8.GetString(languageAssemblyData);
            }
        }

        if (localizationData is null && localizationFiles.TryGetValue("English", out var localizationFile))
        {
            localizationData = File.ReadAllText(localizationFile);
        }

        if (localizationData is not null)
        {
            foreach (var kv in new DeserializerBuilder().IgnoreFields().Build()
                                   .Deserialize<Dictionary<string, string>?>(localizationData)
                               ?? new Dictionary<string, string>())
            {
                localizationTexts[kv.Key] = kv.Value;
            }
        }

        LoadedTexts[language] = localizationTexts;
        foreach (var s in localizationTexts)
        {
            UpdatePlaceholderText(__instance, s.Key);
        }
    }

    static Localizer()
    {
        Harmony harmony = new("org.bepinex.helpers.LocalizationManager");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.LoadCSV)),
            postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Localizer), nameof(LoadLocalization))));
    }

    private static byte[]? LoadTranslationFromAssembly(string language)
    {
        foreach (var extension in FileExtensions)
        {
            if (ReadEmbeddedFileBytes("translations." + language + extension) is { } data)
            {
                return data;
            }
        }

        return null;
    }

    public static byte[]? ReadEmbeddedFileBytes(string resourceFileName, Assembly? containingAssembly = null)
    {
        using MemoryStream stream = new();
        containingAssembly ??= Assembly.GetCallingAssembly();
        if (containingAssembly.GetManifestResourceNames()
                .FirstOrDefault(str => str.EndsWith(resourceFileName, StringComparison.Ordinal)) is { } name)
        {
            containingAssembly.GetManifestResourceStream(name)?.CopyTo(stream);
        }

        return stream.Length == 0 ? null : stream.ToArray();
    }
}