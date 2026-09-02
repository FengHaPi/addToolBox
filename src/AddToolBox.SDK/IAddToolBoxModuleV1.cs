namespace AddToolBox.SDK;

/// <summary>One tool view per module. Identity metadata belongs exclusively to module.json.</summary>
public interface IAddToolBoxModuleV1
{
    Type ToolViewType { get; }
}
