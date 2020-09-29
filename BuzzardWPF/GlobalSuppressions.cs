// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not required here", Scope = "member", Target = "~M:BuzzardWPF.Properties.Settings.Upgrade")]
[assembly: SuppressMessage("Simplification", "RCS1103:Convert 'if' to assignment.", Justification = "Leave as-is", Scope = "member", Target = "~M:BuzzardWPF.MainWindowViewModel.#ctor")]
[assembly: SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "Acceptable design pattern", Scope = "member", Target = "~M:BuzzardWPF.Converters.ByteFileSizeConverter.ConvertBack(System.Object,System.Type,System.Object,System.Globalization.CultureInfo)~System.Object")]
[assembly: SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "Acceptable design pattern", Scope = "member", Target = "~M:BuzzardWPF.Converters.EmptyRequestNameConverter.ConvertBack(System.Object,System.Type,System.Object,System.Globalization.CultureInfo)~System.Object")]
[assembly: SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "Acceptable design pattern", Scope = "member", Target = "~M:BuzzardWPF.ViewModels.EnableProposalIDConverter.ConvertBack(System.Object,System.Type,System.Object,System.Globalization.CultureInfo)~System.Object")]
[assembly: SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "Leave as-is", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Buzzard code uses m_ for class-level variables", Scope = "module")]
