using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Sipos.Resume.Theme.Operandor.Layout;

/// <summary>A heading whose level the parent decides, from <c>h2</c> to <c>h6</c>.</summary>
/// <remarks>
/// Decision: the level is a parameter, not part of the component that renders the item.
/// Why: a project is a third-level heading in the projects section and a fifth-level one under a position's client
/// projects; a screen reader's heading list must show that nesting.
/// </remarks>
public sealed class LevelHeading : ComponentBase
{
    /// <summary>The level, from 2 to 6.</summary>
    [Parameter, EditorRequired] public int Level { get; set; }

    /// <summary>The heading's class.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The heading's content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.OpenElement(0, $"h{Math.Clamp(Level, 2, 6)}");
        builder.AddAttribute(1, "class", Class);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}
