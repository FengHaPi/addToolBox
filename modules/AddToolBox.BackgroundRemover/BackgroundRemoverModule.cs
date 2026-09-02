using AddToolBox.SDK;

namespace AddToolBox.BackgroundRemover;

public sealed class BackgroundRemoverModule : IAddToolBoxModuleV1
{
    public Type ToolViewType => typeof(BackgroundRemoverView);
}
