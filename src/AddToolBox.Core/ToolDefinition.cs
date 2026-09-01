namespace AddToolBox.Core;

public sealed record ToolDefinition(
    string Id,
    string DisplayName,
    string IconKey);

public static class BuiltInTools
{
    public static ToolDefinition Calculator { get; } = new(
        "builtin.calculator",
        "计算器",
        "calculator");

    public static ToolDefinition Image { get; } = new(
        "builtin.image",
        "图片",
        "image");

    public static ToolDefinition File { get; } = new(
        "builtin.file",
        "文件",
        "file");

    public static ToolDefinition Text { get; } = new(
        "builtin.text",
        "文本",
        "text");

    public static ToolDefinition Color { get; } = new(
        "builtin.color",
        "取色器",
        "color");

    public static IReadOnlyList<ToolDefinition> All { get; } = Array.AsReadOnly(
        new[]
        {
            Calculator,
            Image,
            File,
            Text,
            Color
        });
}
