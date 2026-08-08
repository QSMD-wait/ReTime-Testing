using iNKORE.UI.WPF.Modern.Common.IconKeys;
using ReTime_Testing.Models;

namespace ReTime_Testing.ViewModels;

public record ComponentLibraryItem(string Name, string Description, FontIconData? IconGlyph, TextSourceType SourceType, string Category);

public record SourceTypeOption(string DisplayName, TextSourceType SourceType);

public record FormatPresetOption(string Label, string Format);