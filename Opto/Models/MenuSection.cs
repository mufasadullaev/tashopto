using System.Collections.Generic;
using System.Windows.Input;

namespace Opto.Models;

public sealed class MenuSection
{
    public required string Title { get; init; }
    public required IReadOnlyList<MenuAction> Items { get; init; }
}

public sealed class MenuAction
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public bool IsEmphasized { get; init; }
    public ICommand? Command { get; set; }
}
