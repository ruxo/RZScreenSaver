namespace RZScreenSaver;

public static class AppDeps
{
    public static IAppSettingsRepository Settings { get; } = new AppSettingsRepository();

    public static void Shutdown() {
        Settings.Dispose();
    }
}