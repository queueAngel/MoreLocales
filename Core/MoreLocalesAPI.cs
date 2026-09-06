using MoreLocales.Common;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using static MoreLocales.Core.CultureNamePlus;
using static Terraria.Localization.GameCulture;
using static Terraria.Localization.GameCulture.CultureName;

namespace MoreLocales.Core;

#region Substructures
/// <summary>
/// Allows you to do stuff before drawing a certain culture's button.<para/>
/// Return <see langword="true"/> to draw the button normally, using the <see cref="DrawData"/> provided.<br/>
/// Return <see langword="null"/> to stop only the drawing of this button's panel.<br/>
/// Return <see langword="false"/> to stop the drawing of this button entirely.<para/>
/// The only fields the instance of <see cref="DrawData"/> already has set are <see cref="DrawData.texture"/>, <see cref="DrawData.position"/> and <see cref="DrawData.color"/> (Set to <see cref="Color.White"/>)<br/>
/// Nothing that you change in this <see cref="DrawData"/> instance will affect other steps.
/// </summary>
public delegate bool? ButtonPanelDraw(ref DrawData drawData);
/// <summary>
/// A structure used to provide information about a certain <see cref="MoreLocalesCulture"/>'s grammar.<br/>
/// Used mainly for advanced localization features like adjective form inflection and adjective ordering.<para/>
/// <b>Note:</b> Setting the pluralization rule to <see cref="PluralizationStyle.Custom"/> requires you to also set <see cref="GrammarData.CustomPluralizationRule"/>
/// </summary>
/// <param name="pluralizationStyle">
/// <inheritdoc cref="GrammarData.PluralizationRule"/>
/// </param>
/// <param name="customPluralizationRule">
/// <inheritdoc cref="GrammarData.CustomPluralizationRule"/>
/// </param>
/// <param name="adjectiveOrder">
/// <inheritdoc cref="GrammarData.AdjectiveOrder"/>
/// </param>
public readonly struct GrammarData(PluralizationStyle pluralizationStyle = PluralizationStyle.Simple, Func<int, int, int, int> customPluralizationRule = null,
    AdjectiveOrder? adjectiveOrder = null)
{
    /// <summary>
    /// Creates a default <see cref="GrammarData"/> instance.
    /// </summary>
    public GrammarData() : this(PluralizationStyle.Simple, null, null) { }
    /// <summary>
    /// The pluralization style that should be used for this <see cref="MoreLocalesCulture"/>.<para/>
    /// If the value of this is <see cref="PluralizationStyle.Custom"/>, setting the value of <see cref="CustomPluralizationRule"/> <b>is mandatory.</b>
    /// </summary>
    public readonly PluralizationStyle PluralizationRule = pluralizationStyle;
    /// <summary>
    /// The pluralization rule function for a <see cref="MoreLocalesCulture"/> with a <see cref="PluralizationRule"/> of value <see cref="PluralizationStyle.Custom"/>.<para/>
    /// This function should take in 'count, mod10, mod100' as parameters, and return the index of the final pluralization type.<br/>
    /// If your culture represents a language that already exists, refer to this list to learn how to write this function: <see href="https://docs.translatehouse.org/projects/localization-guide/en/latest/l10n/pluralforms.html"/>
    /// </summary>
    public readonly Func<int, int, int, int> CustomPluralizationRule = customPluralizationRule;
    /// <summary>
    /// The adjective-noun order formatter for this <see cref="MoreLocalesCulture"/>.
    /// </summary>
    public readonly AdjectiveOrder AdjectiveOrder = adjectiveOrder ?? AdjectiveOrder.BeforeWithSpace;
    /// <summary>
    /// Makes a new <see cref="GrammarData"/> instance with only <see cref="GrammarData.PluralizationRule"/> and <see cref="GrammarData.AdjectiveOrder"/> set.
    /// </summary>
    /// <param name="pluralizationStyle">
    /// <inheritdoc cref="GrammarData.PluralizationRule"/>
    /// </param>
    /// <param name="adjectiveOrder">
    /// <inheritdoc cref="GrammarData.AdjectiveOrder"/>
    /// </param>
    /// <returns></returns>
    public static GrammarData StyleOrder(PluralizationStyle pluralizationStyle, AdjectiveOrder adjectiveOrder)
        => new(pluralizationStyle: pluralizationStyle, adjectiveOrder: adjectiveOrder);
}
/// <summary>
/// A structure used for control over the drawing of the language button for a certain culture.<para/>
/// Can control basic stuff (using the basic fields) and also more advanced stuff at the different button draw steps (using the delegate fields).
/// </summary>
public struct LanguageButtonDrawData(Asset<Texture2D> sheet = null, int? sheetFrameCount = null,
    int? sheetFrame = null, ButtonPanelDraw hijackPanelDraw = null)
{
    /// <summary>
    /// The sheet where the language symbol graphic will be taken from. If this field is left null, this will be Flags.png
    /// </summary>
    public Asset<Texture2D> Sheet = sheet;
    /// <summary>
    /// The amount of vertical frames in <see cref="Sheet"/>.<para/>
    /// Defaults to 28 if <see cref="Sheet"/> is also null, otherwise defaults to 1.
    /// </summary>
    public readonly int SheetFrameCount = sheetFrameCount.HasValue ? Math.Max(sheetFrameCount.Value, 1) : sheet is null ? BetterLangMenuV2.FlagsCount : 1;
    /// <summary>
    /// The index of the vertical frame that this language symbol should use. Defaults to 0.
    /// </summary>
    public readonly int SheetFrame = sheetFrame.HasValue ? Math.Max(sheetFrame.Value, 0) : 0;
    /// <summary>
    /// Allows you to do a range of things before drawing the main button panel. Leave null to not do anything.
    /// </summary>
    public readonly ButtonPanelDraw HijackPanelDraw = hijackPanelDraw;
}
/// <summary>
/// Flags enum telling the mod how to render language names in the language menu.
/// </summary>
[Flags]
public enum LanguageNameFormatFlags : ushort
{
    /// <summary>
    /// No flags. Will render as:<para/>
    /// <b>NativeName TranslatedName</b>
    /// </summary>
    None = 0,
    /// <summary>
    /// The native name will be ordered after the translated name.
    /// </summary>
    NativeAfter = 1,
    /// <summary>
    /// The native name will have boundaries around it.
    /// </summary>
    BoundariesNative = 2,
    /// <summary>
    /// The translated name will have boundaries around it.
    /// </summary>
    BoundariesTranslated = 4,

    // Combos

    /// <inheritdoc/>
    BoundariesNativeAfter = NativeAfter | BoundariesNative,
    /// <inheritdoc/>
    BoundariesTranslatedBefore = NativeAfter | BoundariesTranslated,
    /// <inheritdoc/>
    BoundariesBoth = BoundariesNative | BoundariesTranslated,
    /// <inheritdoc/>
    NativeAfterBoundariesBoth = NativeAfter | BoundariesBoth,

}
/// <summary>
/// Simple formatter struct telling the mod how to render language names in the language menu and what boundary type to use.
/// </summary>
/// <param name="flags"></param>
/// <param name="boundary"></param>
public struct LanguageNameFormat(LanguageNameFormatFlags flags, BoundaryType boundary)
{
    private static readonly LanguageNameFormat _default = new(LanguageNameFormatFlags.BoundariesTranslated, BoundaryType.Parentheses);
    /// <summary>
    /// Language name formatter where the translated name has parentheses around it, i. e. <c>'NativeName (TranslatedName)'</c>
    /// </summary>
    public static LanguageNameFormat Default => _default;
    /// <summary>
    /// Flags telling the mod how to render language names in the language menu.
    /// </summary>
    public LanguageNameFormatFlags Flags = flags;
    /// <summary>
    /// Which type of boundary should be used for rendering boundaries according to <see cref="Flags"/>.
    /// </summary>
    public BoundaryType Boundary = boundary;
    /// <summary>
    /// Extracts the parameters related to language name formatting from this <see cref="LanguageNameFormat"/> instance.<br/>
    /// These are used, for example, with <see cref="Format(string, string, in bool, in bool, in bool, in BoundaryType)"/>.<br/>
    /// Alternatively, just use the instanced version, <see cref="Format(string, string)"/>.
    /// </summary>
    public readonly void Deconstruct(out bool nativeAfter, out bool boundariesNative, out bool boundariesTranslated, out BoundaryType boundary)
    {
        nativeAfter = (Flags & LanguageNameFormatFlags.NativeAfter) != 0;
        boundariesNative = (Flags & LanguageNameFormatFlags.BoundariesNative) != 0;
        boundariesTranslated = (Flags & LanguageNameFormatFlags.BoundariesTranslated) != 0;
        boundary = Boundary;
    }
    /// <summary>
    /// Formats the native and translated names of a language using this <see cref="LanguageNameFormat"/> instance.
    /// </summary>
    /// <param name="native">The native name of the language.</param>
    /// <param name="translated">The translated name of the language.</param>
    /// <returns>The formatted name.</returns>
    public readonly string Format(string native, string translated)
    {
        Deconstruct(out bool nativeAfter, out bool boundariesNative, out bool boundariesTranslated, out BoundaryType boundary);
        return Format(native, translated, in nativeAfter, in boundariesNative, in boundariesTranslated, in boundary);
    }
    /// <summary>
    /// Formats a language name using the provided parameters, which can be desconstructed from a <see cref="LanguageNameFormat"/> instance using, for example, <see cref="Deconstruct(out bool, out bool, out bool, out BoundaryType)"/>.
    /// </summary>
    /// <param name="native">The native name of the language.</param>
    /// <param name="translated">The translated name of the language.</param>
    /// <param name="nativeAfter">Whether or not the native language should come after.</param>
    /// <param name="boundariesNative">Whether or not to put the specified boundary around the native name.</param>
    /// <param name="boundariesTranslated">Whether or not to put the specified boundary around the translated name.</param>
    /// <param name="boundary">The boundary that should be applied if appropriate.</param>
    /// <returns>The formatted name.</returns>
    public static string Format(string native, string translated, in bool nativeAfter, in bool boundariesNative, in bool boundariesTranslated, in BoundaryType boundary)
    {
        if (boundariesNative)
            native = TextHelper.FormatWithBoundary(native, boundary);
        if (boundariesTranslated)
            translated = TextHelper.FormatWithBoundary(translated, boundary);
        return nativeAfter ? $"{translated} {native}" : $"{native} {translated}";
    }
}
#endregion
/// <summary>
/// A structure used to significantly extend the functionality of <see cref="GameCulture"/>.<br/>
/// Cultures registered through <see cref="MoreLocales"/> will create localization keys inside the <see cref="MoreLocalesCulture.Mod"/>'s localization file.<br/>
/// These keys are needed for correct display inside <see cref="MoreLocales"/>'s UI.
/// </summary>
public struct MoreLocalesCulture(GameCulture culture, string name, int fallback = 1,
    bool subtitle = false, bool description = false, string nativeName = null, LanguageNameFormat? langNameFormat = null, GrammarData? grammarData = null,
    Func<bool> available = null, LanguageButtonDrawData buttonDrawData = new(), Mod mod = null)
{
    /// <summary>
    /// The child culture of this <see cref="MoreLocalesCulture"/>.
    /// </summary>
    public readonly GameCulture Culture = culture;
    /// <summary>
    /// The internal name of this <see cref="MoreLocalesCulture"/>. Used for certain language info lookups.
    /// </summary>
    public readonly string Name = name;
    /// <summary>
    /// The fallback culture of this <see cref="MoreLocalesCulture"/>.<br/>
    /// If localizations for this culture aren't found, localizations from the fallback culture will be used instead.
    /// </summary>
    public readonly int FallbackCulture = fallback;
    /// <summary>
    /// Used for display in <see cref="BetterLangMenuUI"/>.<br/>
    /// If this is true for a custom culture, <see cref="MoreLocales"/> will search for (or create) a subtitle key using <see cref="Mod.GetLocalization(string, Func{string})"/> using the "Cultures.{Name}.Subtitle" suffix.
    /// </summary>
    public readonly bool HasSubtitle = subtitle;
    /// <summary>
    /// Used for hover text in <see cref="BetterLangMenuUI"/>.<br/>
    /// If this is true for a custom culture, the mod will search for (or create) a description key using <see cref="Mod.GetLocalization(string, Func{string})"/> using the "Cultures.{Name}.Description" suffix.
    /// </summary>
    public readonly bool HasDescription = description;
    /// <summary>
    /// The name of this culture in its own language.
    /// </summary>
    public readonly string NativeName = nativeName ?? name;
    /// <summary>
    /// The way language names will be formatted in the language menu when this language is active.
    /// </summary>
    public readonly LanguageNameFormat LanguageNameFormat = langNameFormat ?? LanguageNameFormat.Default;
    /// <inheritdoc cref="Core.GrammarData"/>
    public readonly GrammarData GrammarData = grammarData ?? new();
    /// <summary>
    /// Whether or not this culture should be visible on the language menu. Defaults to null (always available).
    /// </summary>
    public readonly Func<bool> Available = available;
    /// <inheritdoc cref="LanguageButtonDrawData"/>
    public LanguageButtonDrawData ButtonDrawData = buttonDrawData;
    /// <summary>
    /// The parent mod for this <see cref="MoreLocalesCulture"/>. Null if this represents a vanilla culture.
    /// </summary>
    public readonly Mod Mod = mod;
    /// <summary>
    /// Whether or not this <see cref="MoreLocalesCulture"/> was registered by an external source that is not Terraria nor <see cref="MoreLocales"/>.
    /// </summary>
    public readonly bool OtherCustom => Mod != null && Mod != MoreLocales.Instance;
    /// <summary>
    /// Whether or not this <see cref="MoreLocalesCulture"/> was registered as part of the set of languages defined by <see cref="MoreLocales"/> in <see cref="CultureNamePlus"/>.
    /// </summary>
    public readonly bool NativeCustom => Mod == MoreLocales.Instance;
    /// <summary>
    /// Whether or not this <see cref="MoreLocalesCulture"/> was registered as part of the set of languages defined by Terraria in <see cref="CultureName"/>.
    /// </summary>
    public readonly bool Vanilla => Mod is null;

    internal readonly Mod FunctionalOwner => Vanilla ? MoreLocales.Instance : Mod;
}

/// <summary>
/// <see href="https://bit.ly/458nsBZ"/>
/// </summary>
public readonly ref struct Ref<T>(ref T value)
{
    /// <summary>
    /// The reference contained in this.
    /// </summary>
    public readonly ref T Value = ref value;
}
/// <summary>
/// Contains methods to interface with the cultures API extended by MoreLocales.
/// </summary>
public static class MoreLocalesAPI
{
    private const string customCultureDataName = "LocalizationPlusData.dat";
    private static int loadedCulture = 9999;
    internal static int cachedVanillaCulture = 1; // english by default
    internal static MoreLocalesCulture[] extraCulturesV2 = new MoreLocalesCulture[29]; // entry 0 is a dummy default entry
    internal static Dictionary<Mod, ulong> _localizationFlags = [];
    private static int _registeredCount = 1; // starts at one because CultureName.English is 1
    internal static Dictionary<Type, int> _autoloadedCulturesRegistry;
    internal static Mod[] _modsThatAddCustomCultures;
    internal static HashSet<Mod> _protectedMods = [];
    /// <summary>
    /// Mods in this collection are protected from getting their localization files automatically marked as '.legacy' by tModLoader if they contain localization files without en-US counterparts.<para/>
    /// Useful for making mods that register a culture to add localizations to vanilla.<para/>
    /// Add your mod to this set using <see cref="ProtectFilesFromLegacyMarking(Mod)"/>.
    /// </summary>
    public static IReadOnlySet<Mod> ProtectedMods => _protectedMods;
    /// <summary>
    /// Gets a reference to the currently active <see cref="MoreLocalesCulture"/>.
    /// </summary>
    public static ref MoreLocalesCulture ActiveCulture => ref extraCulturesV2[LanguageManager.Instance.ActiveCulture.LegacyId];
    /// <summary>
    /// Returns a reference to the requested <see cref="MoreLocalesCulture"/> based on its <see cref="GameCulture.LegacyId"/>.
    /// </summary>
    public static ref MoreLocalesCulture GetCulture(int legacyID) => ref extraCulturesV2[legacyID];
    /// <summary>
    /// Returns a reference to the requested autoloaded <see cref="MoreLocalesCulture"/> based on its <see cref="Type"/>.
    /// </summary>
    public static ref MoreLocalesCulture GetCulture<T>() where T : ModCulture
    {
        return ref GetCulture(_autoloadedCulturesRegistry[typeof(T)]);
    }
    /// <summary>
    /// Attempts to get a reference to the requested autoloaded <see cref="MoreLocalesCulture"/> based on its <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="T">A type inheriting from <see cref="ModCulture"/></typeparam>
    /// <param name="culture">
    /// A <see cref="Ref{T}"/> containing a reference to the requested <see cref="MoreLocalesCulture"/>.<br/>
    /// If this method fails to find the requested culture, the value of this will be a reference to a default <see cref="MoreLocalesCulture"/>.
    /// </param>
    /// <returns>Whether or not the requested <see cref="MoreLocalesCulture"/> was found.</returns>
    public static bool TryGetCulture<T>(out Ref<MoreLocalesCulture> culture) where T : ModCulture
    {
        if (_autoloadedCulturesRegistry.TryGetValue(typeof(T), out var c))
        {
            culture = new(ref GetCulture(c));
            return true;
        }
        culture = new(ref extraCulturesV2[0]);
        return false;
    }
    /// <summary>
    /// Returns a reference to the <see cref="MoreLocalesCulture"/> that contains this <see cref="GameCulture"/>.
    /// </summary>
    public static ref MoreLocalesCulture GetCultureExtra(this GameCulture culture) => ref GetCulture(culture.LegacyId);
    internal static void DoLoad()
    {
        IL_LanguageManager.ReloadLanguage += AddFallbacks;
        On_Main.SaveSettings += Save;

        _registerNative = true;
        RegisterVanillaCultures();
        RegisterNativeCustomCultures();
        _registerNative = false;
    }
    internal static void InitModLocalizationFlags()
    {
        // checks every mod's localization capabilities and registers them into _localizationFlags

        var mods = ModLoader.Mods;
        for (int i = 0; i < mods.Length; i++)
        {
            SingleModLocalizationFlags(mods[i]);
        }
    }
    private static void SingleModLocalizationFlags(Mod mod)
    {
        _localizationFlags[mod] = 0ul;
        HashSet<GameCulture> cultures = [];

        var files = mod.GetLocalizationFiles();

        if (files is null)
        {
            MoreLocales.Instance.Logger.Warn($"Couldn't get localization files for mod {mod}.");
            return;
        }

        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];

            if (!LocalizationLoader.TryGetCultureAndPrefixFromPath(file.Name, out var culture, out _))
                continue;

            if (cultures.Contains(culture))
                continue;

            ulong place = 1ul << culture.LegacyId;
            _localizationFlags[mod] |= place;

            cultures.Add(culture);
        }
    }
    /// <summary>
    /// Checks if this mod has localizations for a given <see cref="GameCulture"/> using its <see cref="GameCulture.LegacyId"/>.
    /// </summary>
    /// <param name="mod">The mod.</param>
    /// <param name="legacyID">The legacy ID for the localizable culture.</param>
    /// <returns>Whether or not this mod has localizations for the given culture.</returns>
    public static bool HasLocalizationsFor(this Mod mod, int legacyID)
    {
        if (_localizationFlags.TryGetValue(mod, out ulong flags))
            return (flags & (1ul << legacyID)) != 0;
        return false;
    }
    /// <summary>
    /// Checks if this mod has localizations for a given <see cref="GameCulture"/>.
    /// </summary>
    /// <param name="mod">The mod.</param>
    /// <param name="culture">The <see cref="GameCulture"/> instance.</param>
    /// <returns>Whether or not this mod has localizations for the given culture.</returns>
    public static bool HasLocalizationsFor(this Mod mod, GameCulture culture) => HasLocalizationsFor(mod, culture.LegacyId);
    /// <summary>
    /// Checks if this mod has localizations for a given <see cref="MoreLocalesCulture"/>.
    /// </summary>
    /// <param name="mod">The mod.</param>
    /// <param name="culture">The <see cref="MoreLocalesCulture"/> instance.</param>
    /// <returns>Whether or not this mod has localizations for the given culture.</returns>
    public static bool HasLocalizationsFor(this Mod mod, ref MoreLocalesCulture culture) => HasLocalizationsFor(mod, culture.Culture);
    /// <summary>
    /// Checks if this mod has localizations for a given <see cref="GameCulture"/> using its <see cref="GameCulture.Name"/> (language code).
    /// </summary>
    /// <param name="mod">The mod.</param>
    /// <param name="langCode">The language code.</param>
    /// <returns></returns>
    public static bool HasLocalizationsFor(this Mod mod, string langCode) => HasLocalizationsFor(mod, FromName(langCode));
    /// <summary>
    /// Checks if this mod has localizations for a given <see cref="ModCulture"/> type.
    /// </summary>
    /// <param name="mod">The mod.</param>
    /// <typeparam name="TCulture">The ModCulture type.</typeparam>
    /// <returns>Whether or not this mod has localizations for the given culture.</returns>
    public static bool HasLocalizationsFor<TCulture>(this Mod mod) where TCulture : ModCulture => HasLocalizationsFor(mod, ref GetCulture<TCulture>());
    internal static void InitCustomCultureModsArray()
    {
        HashSet<Mod> mods = new(extraCulturesV2.Length) { MoreLocales.Instance };
        for (int i = 0; i < extraCulturesV2.Length; i++)
        {
            ref MoreLocalesCulture culture = ref extraCulturesV2[i];
            if (culture.Mod != null && !mods.Contains(culture.Mod))
                mods.Add(culture.Mod);
        }
        _modsThatAddCustomCultures = [.. mods];
    }
    private static void RegisterVanillaCultures()
    {
        var basicRomance = new GrammarData(adjectiveOrder: AdjectiveOrder.AfterWithSpace);

        RegisterCulture(nameof(English),
            nativeName: "English",
            buttonDrawData: new(sheetFrame: (int)English));

        RegisterCulture(nameof(German),
            nativeName: "Deutsch",
            buttonDrawData: new(sheetFrame: (int)German));

        RegisterCulture(nameof(Italian),
            nativeName: "Italiano",
            grammarData: basicRomance,
            buttonDrawData: new(sheetFrame: (int)Italian));

        RegisterCulture(nameof(French),
            nativeName: "Français",
            grammarData: GrammarData.StyleOrder(PluralizationStyle.SimpleWithSingularZero, AdjectiveOrder.AfterWithSpace),
            buttonDrawData: new(sheetFrame: (int)French));

        RegisterCulture(nameof(Spanish),
            nativeName: "Español",
            grammarData: basicRomance,
            buttonDrawData: new(sheetFrame: (int)Spanish));

        RegisterCulture(nameof(Russian),
            nativeName: "Русский",
            grammarData: new(PluralizationStyle.RussianThreeway),
            buttonDrawData: new(sheetFrame: (int)Russian));

        RegisterCulture(nameof(Chinese),
            nativeName: "中文",
            grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.Before),
            buttonDrawData: new(sheetFrame: (int)Chinese));

        RegisterCulture(nameof(Portuguese),
            nativeName: "Português",
            grammarData: basicRomance,
            buttonDrawData: new(sheetFrame: (int)Portuguese));

        RegisterCulture(nameof(Polish),
            nativeName: "Polski",
            grammarData: new(PluralizationStyle.PolishThreeway),
            buttonDrawData: new(sheetFrame: (int)Polish));
    }
    private static void RegisterNativeCustomCultures()
    {
        Mod mod = MoreLocales.Instance;

        var basicRomance = new GrammarData(adjectiveOrder: AdjectiveOrder.AfterWithSpace);

        // MoreLocales provides you with this extension method: Mod.RegisterCulture, for simplicity (mod parameter automatically gets filled).

        mod.RegisterCulture(nameof(BritishEnglish),
            "en-GB",
            nativeName: "English",
            buttonDrawData: new(sheetFrame: (int)BritishEnglish));

        mod.RegisterCulture(nameof(Japanese),
            "ja-JP",
            nativeName: "日本語",
            grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.Before),
            buttonDrawData: new(sheetFrame: (int)Japanese));

        mod.RegisterCulture(nameof(Korean),
            "ko-KR",
            nativeName: "한국어",
            grammarData: new(PluralizationStyle.None),
            buttonDrawData: new(sheetFrame: (int)Korean));

        mod.RegisterCulture(nameof(TraditionalChinese),
            "zh-Hant",
            (int)Chinese,
            nativeName: "中文",
            grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.Before),
            buttonDrawData: new(sheetFrame: (int)TraditionalChinese));

        mod.RegisterCulture(nameof(Turkish),
            "tr-TR",
            nativeName: "Türkçe",
            grammarData: new(PluralizationStyle.Custom, CultureHelper.turkishPlural),
            buttonDrawData: new(sheetFrame: (int)Turkish));

        mod.RegisterCulture(nameof(Thai),
            "th-TH",
            nativeName: "ภาษาไทย",
            grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.After),
            buttonDrawData: new(sheetFrame: (int)Thai));

        mod.RegisterCulture(nameof(Ukrainian),
            "uk-UA",
            (int)Russian,
            nativeName: "Українська",
            grammarData: new(PluralizationStyle.RussianThreeway),
            buttonDrawData: new(sheetFrame: (int)Ukrainian));

        mod.RegisterCulture(nameof(MexicanSpanish),
            "es-MX",
            (int)Spanish,
            nativeName: "Español",
            grammarData: basicRomance,
            buttonDrawData: new(sheetFrame: (int)MexicanSpanish));

        mod.RegisterCulture(nameof(Czech),
            "cs-CZ",
            nativeName: "Čeština",
            grammarData: new(PluralizationStyle.Custom, CultureHelper.czechPlural),
            buttonDrawData: new(sheetFrame: (int)Czech));

        // hungarian's adjective agreement rules are a little weird, but irrelevant for the mod
        mod.RegisterCulture(nameof(Hungarian),
            "hu-HU",
            nativeName: "Magyar",
            grammarData: new(PluralizationStyle.SimpleWithSingularZero),
            buttonDrawData: new(sheetFrame: (int)Hungarian));

        mod.RegisterCulture(nameof(PortugalPortuguese),
            "pt-PT",
            (int)Portuguese,
            nativeName: "Português",
            grammarData: basicRomance,
            buttonDrawData: new(sheetFrame: (int)PortugalPortuguese));

        mod.RegisterCulture(nameof(Swedish),
            "sv-SE",
            nativeName: "Svenska",
            buttonDrawData: new(sheetFrame: (int)Swedish));

        mod.RegisterCulture(nameof(Dutch),
            "nl-NL",
            nativeName: "Nederlands",
            buttonDrawData: new(sheetFrame: (int)Dutch));

        mod.RegisterCulture(nameof(Danish),
            "da-DK",
            nativeName: "Dansk",
            grammarData: new(PluralizationStyle.SimpleWithSingularZero),
            buttonDrawData: new(sheetFrame: (int)Danish));

        mod.RegisterCulture(nameof(Vietnamese),
            "vi-VN",
            hasSubtitle: false,
            nativeName: "Tiếng Việt",
            grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.AfterWithSpace),
            buttonDrawData: new(sheetFrame: (int)Vietnamese));

        mod.RegisterCulture(nameof(Finnish),
            "fi-FI",
            nativeName: "Suomi",
            buttonDrawData: new(sheetFrame: (int)Finnish));

        mod.RegisterCulture(nameof(Romanian),
            "ro-RO",
            grammarData: new(PluralizationStyle.Custom, CultureHelper.romanianPlural, AdjectiveOrder.AfterWithSpace),
            nativeName: "Română",
            buttonDrawData: new(sheetFrame: (int)Romanian));

        mod.RegisterCulture(nameof(Indonesian),
            "id-ID",
            nativeName: "Bahasa Indonesia",
            grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.AfterWithSpace),
            buttonDrawData: new(sheetFrame: (int)Indonesian));

        mod.RegisterCulture(nameof(Belarusian),
            "be-BY",
            nativeName: "Беларуская",
            grammarData: new(PluralizationStyle.RussianThreeway),
            buttonDrawData: new(sheetFrame: (int)Belarusian));
    }
    internal static void DoSafeLoad()
    {
        IL_LocalizedText.CardinalPluralRule += SupportForNewPluralization;
    }
    internal static bool _canRegister = false;
    internal static bool _registerNative = false;
    /// <summary>
    /// Registers a new localizable culture. Can only be called during a Load hook.
    /// </summary>
    /// <param name="internalName">The internal name of this.</param>
    /// <param name="languageCode">The language code of this culture, e. g. en-US, es-ES, etc.</param>
    /// <param name="fallbackCulture">The <see cref="GameCulture.LegacyId"/> of a fallback culture. Localizations from the fallback culture will be loaded if one for this culture isn't found.</param>
    /// <param name="hasSubtitle">Whether or not a subtitle should be searched for and shown in the language menu.</param>
    /// <param name="hasDescription">Whether or not a hover text (description) should be searched for and shown in the language menu.</param>
    /// <param name="nativeName">The native name of this culture in that culture's language, i. e. 'English' for English, 'Español' for Spanish, etc.</param>
    /// <param name="langNameFormat">A formatter that tells the mod how language names should be displayed in the languages menu.<br/>
    /// Leave <see langword="null"/> to use <see cref="LanguageNameFormat.Default"/>.
    /// </param>
    /// <param name="grammarData">Data related to handling grammar for this culture.</param>
    /// <param name="available">Whether or not this culture should be visible in the language menu. Useful if you want cultures to be 'unlockable' for whatever reason.</param>
    /// <param name="buttonDrawData">Data related to the drawing of this culture's button in the language menu.</param>
    /// <param name="mod">The mod that registers this culture.</param>
    /// <returns>A reference to the newly registered culture.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    public static ref MoreLocalesCulture RegisterCulture
    (
        string internalName,
        string languageCode = null,
        int fallbackCulture = 1,
        bool hasSubtitle = true,
        bool hasDescription = false,
        string nativeName = null,
        LanguageNameFormat? langNameFormat = null,
        GrammarData? grammarData = null,
        Func<bool> available = null,
        LanguageButtonDrawData buttonDrawData = new(),
        Mod mod = null
    )
    {
        if (!_canRegister)
            throw new InvalidOperationException("You cannot register a culture outside of a Load method.");
        if (!_registerNative && (mod is null || mod == MoreLocales.Instance))
            throw new InvalidOperationException("Mods registered by external mods should have a valid mod instance. It cannot be left null or be MoreLocales.");
        foreach (char c in internalName.AsSpan())
        {
            if (char.IsWhiteSpace(c))
                throw new InvalidOperationException($"An internal name for a culture ({internalName}) cannot contain whitespace.");
        }

        GameCulture childCulture;
        if (_legacyCultures.TryGetValue(_registeredCount, out GameCulture vanillaCulture))
        {
            childCulture = vanillaCulture;
            // this culture is already fully registered, nothing else is needed
        }
        else if (!string.IsNullOrEmpty(languageCode))
        {
            childCulture = new GameCulture(languageCode, _registeredCount);
            _NamedCultures.Add((CultureName)_registeredCount, childCulture);
        }
        else
        {
            throw new NullReferenceException($"The parameter {languageCode} cannot be null for cultures that do not copy existing {nameof(GameCulture)}s.");
        }

        Logging.tML.Info($"[MoreLocales] Culture {internalName} was registered by {(mod == MoreLocales.Instance ? "MoreLocales" : mod?.Name ?? "vanilla")}");

        MoreLocalesCulture newCulture = new(childCulture, internalName, fallbackCulture, hasSubtitle, hasDescription, nativeName, langNameFormat, grammarData, available, buttonDrawData, mod);

        if (extraCulturesV2.Length < _registeredCount + 1)
            Array.Resize(ref extraCulturesV2, _registeredCount + 1);

        extraCulturesV2[_registeredCount] = newCulture;
        return ref extraCulturesV2[_registeredCount++];
    }
    /// <summary>
    /// Adds this mod to <see cref="ProtectedMods"/> (read those docs for more information).
    /// </summary>
    /// <param name="mod">The mod to add to the protected list.</param>
    public static void ProtectFilesFromLegacyMarking(Mod mod) => _protectedMods.Add(mod);
    private static void SupportForNewPluralization(ILContext il)
    {
        Mod mod = MoreLocales.Instance;
        try
        {
            var c = new ILCursor(il);

            // i will forever love my previous implementation but unfortunately since others can register their own cultures now, we need to do it in a hacky way

            // find where GameCulture.LegacyId is loaded for use with the switch statement, right before 1 gets subtracted from it
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc2()))
            {
                mod.Logger.Warn("SupportForNewPluralization: Couldn't find GameCulture.LegacyId loading");
                return;
            }
            c.EmitCall(typeof(CultureHelper).GetMethod(nameof(CultureHelper.MapLegacyIDToPluralizationID))); // get the ID of a valid vanilla culture or 10 for custom

            ILLabel[] targets = null;

            if (!c.TryGotoNext(i => i.MatchSwitch(out targets)))
            {
                mod.Logger.Warn("SupportForNewPluralization: Couldn't find switch statement position");
                return;
            }

            ILLabel customPlural = il.DefineLabel();

            // we resize the array to include our new cultures
            var newTargets = new ILLabel[targets.Length + 1]; // entry 10 will be custom
            targets.CopyTo(newTargets, 0); // and make sure the old cultures are in it too
            newTargets[^1] = customPlural;

            // finally, we assign the new switch table to the switch instruction
            c.Next.Operand = newTargets;

            // now we inject our code for custom rules somewhere it won't interfere with other stuff. an easy way to do that is by adding it at the end

            c.Index = il.Instrs.Count; // common mistake: don't subtract one, because the cursor will end up before the last ret, not after it. remember how cursor indices work
            int labelIndex = c.Index;

            // normally you would mark the label here. however, we'll get a nullref exception if we do this.
            // this is because the label's target wants to be c.Next but it's null since we're on the end of the method.

            c.EmitLdloc2(); // legacy id (the original one)
            c.EmitLdloc0(); // mod10
            c.EmitLdloc1(); // mod100
            c.EmitLdarg1(); // count
            c.EmitCall(typeof(CultureHelper).GetMethod(nameof(CultureHelper.CustomPluralization), BindingFlags.Static | BindingFlags.NonPublic));
            c.EmitRet();

            c.Index = labelIndex;
            c.MarkLabel(customPlural); // finally we mark the label

            // the IL edit would normally be done here, but we actually have one more thing left to do
            // the issue with editing switch statements and adding your own cases is that every instruction emitted by monomod has the IL_0000 offset. this is REALLY bad for labels, so switch will die
            // to solve this, we can recalculate all of the offsets so that the labels won't be all messed up. thank you absoluteAquarian aka the MonoSound guy

            ILHelper.UpdateInstructionOffsets(c);
        }
        catch
        {
            MonoModHooks.DumpIL(mod, il);
        }
    }
    private static bool Save(On_Main.orig_SaveSettings orig)
    {
        // So, why do we need this?
        // The game will actually save our custom culture by default, using GameCulture.Name, but it won't recognize it when loading, and revert back to English.
        // First, we can save our custom culture data in our file.
        SaveCustomCultureData();
        // Second, we can revert the culture by ourselves before the game has the chance to save it.
        RevertCustomCulture(false, out var customCulture);
        bool result = orig();
        // Then, bring it back (if settings are saved outside of game exit, this is necessary)
        LanguageManager.Instance?.SetLanguage(customCulture);
        return result;
    }
    private static void AddFallbacks(ILContext il)
    {
        Mod mod = MoreLocales.Instance;
        try
        {
            // first we need to add a local var for our custom GameCulture
            var localGameCulture = new VariableDefinition(il.Import(typeof(GameCulture)));
            il.Body.Variables.Add(localGameCulture);

            var c = new ILCursor(il);

            // this is inside the if statement, so we already know that the active culture isn't english
            if (!c.TryGotoNext(i => i.MatchLdarg0(), i => i.MatchLdarg0(), i => i.MatchCall<LanguageManager>("get_ActiveCulture")))
            {
                mod.Logger.Warn("AddFallbacks: Couldn't find in-between step insertion position");
                return;
            }

            // load this in order to consume it for our delegate
            c.EmitLdarg0();

            // figure out if the current lang has a fallback defined
            c.EmitDelegate<Func<LanguageManager, GameCulture>>(l =>
            {
                int possibleFallback = extraCulturesV2[l.ActiveCulture.LegacyId].FallbackCulture;
                if (possibleFallback != 1)
                    return _legacyCultures[possibleFallback];
                return null;
            });

            // store that value in the variable
            c.EmitStloc(localGameCulture.Index);

            var skipLabel = il.DefineLabel();

            // load the variable to check if it's null
            c.EmitLdloc(localGameCulture.Index);

            // if it's null, skip the call
            c.EmitBrfalse(skipLabel);

            // otherwise, load arguments
            c.EmitLdarg0();
            c.EmitLdloc(localGameCulture.Index);

            // then call the method
            c.EmitCall(typeof(LanguageManager).GetMethod("LoadFilesForCulture", BindingFlags.Instance | BindingFlags.NonPublic));

            // it should skip to after the call
            c.MarkLabel(skipLabel);
        }
        catch
        {
            MonoModHooks.DumpIL(mod, il);
        }
    }
    /// <summary>
    /// Sets the game's language without calling <see cref="LanguageManager.SetLanguage(GameCulture)"/>
    /// </summary>
    /// <param name="culture"></param>
    internal static void SetLanguageSoft(GameCulture culture)
    {
        var lang = LanguageManager.Instance;
        lang.ActiveCulture = culture;
        Thread.CurrentThread.CurrentCulture = culture.CultureInfo;
        Thread.CurrentThread.CurrentUICulture = culture.CultureInfo;
    }
    private const byte FileVersion = 0;
    internal static void LoadCustomCultureData()
    {
        string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

        if (!File.Exists(pathToCustomCultureData))
            return;

        using var reader = new BinaryReader(File.Open(pathToCustomCultureData, FileMode.Open));
        if (reader.BaseStream.Length == 1) // oldest file version stored a single byte as language id
        {
            byte culture = reader.ReadByte();
            if (!CultureHelper.IsValid(culture))
                return;

            loadedCulture = culture;

            // if it's somehow larger than the range of things that was available then, default back to vanilla thing
            if (loadedCulture > (int)Indonesian)
                loadedCulture = LanguageManager.Instance.ActiveCulture.LegacyId;

            // no reason to re-set the language if it's vanill
            if (loadedCulture < (int)BritishEnglish)
                return;
        }
        else
        {
            byte version = reader.ReadByte();

            string langCode = reader.ReadString();
            byte accessibleCulture = reader.ReadByte();

            for (int i = 1; i < extraCulturesV2.Length; i++)
            {
                ref MoreLocalesCulture culture = ref extraCulturesV2[i];
                if (!culture.Vanilla && langCode == culture.Culture.Name)
                {
                    loadedCulture = culture.Culture.LegacyId;
                    break;
                }
            }
        }

        if (loadedCulture != 9999)
        {
            LanguageManager.Instance.SetLanguage(extraCulturesV2[loadedCulture].Culture);
            Main.instance.SetTitle();
        }
    }
    private static void SaveCustomCultureData()
    {
        if (!LanguageManager.Instance.ActiveCulture.IsCustom()) // no reason to save anything if not custom
            return;

        string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

        void WriteFile()
        {
            using var writer = new BinaryWriter(File.Open(pathToCustomCultureData, FileMode.OpenOrCreate));

            writer.Write(FileVersion);

            string langCode = LanguageManager.Instance.ActiveCulture.Name;
            writer.Write(langCode);

            writer.Write((byte)cachedVanillaCulture);
        }

        if (!File.Exists(pathToCustomCultureData))
        {
            WriteFile();
        }
        else
        {
            File.WriteAllText(pathToCustomCultureData, "");
            WriteFile();
        }
    }
    internal static void DoUnload()
    {
        SaveCustomCultureData();
        UnregisterCultures();
        _autoloadedCulturesRegistry = null;
        extraCulturesV2 = null;
    }
    private static void RevertCustomCulture(bool setTitle, out GameCulture customCulture, bool soft = false)
    {
        customCulture = LanguageManager.Instance.ActiveCulture;
        if (!customCulture.IsCustom())
            return;

        if (soft)
            SetLanguageSoft(FromLegacyId(cachedVanillaCulture));
        else
            LanguageManager.Instance.SetLanguage(cachedVanillaCulture);

        if (setTitle)
            Main.instance.SetTitle();
    }
    private static void UnregisterCultures()
    {
        RevertCustomCulture(true, out _, true);

        for (int i = 1; i < extraCulturesV2.Length; i++)
        {
            MoreLocalesCulture culture = extraCulturesV2[i];

            if (culture.Vanilla)
                continue;

            _legacyCultures.Remove(i);
            _NamedCultures.Remove((CultureName)i);
        }

        _legacyCultures.TrimExcess();
        _NamedCultures.TrimExcess();
    }
}
