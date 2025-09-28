using System.Reflection;

namespace SmartSleep.App.Utilities;

public static class AppInfo
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    public static string Name => "Smart Sleep";

    public static string Version => _assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static string NameWithVersion => $"{Name} {Version}";

    public static string ProductName => _assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? Name;

    public static string Company => _assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "SmartSleep Project";

    public static string Description => _assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";

    public static string Copyright => _assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
}