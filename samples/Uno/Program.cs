using System;
using Uno.UI.Hosting;
using UnoSample;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var backend = args switch
        {
            [] => Win32RenderingBackend.Default,
            ["--renderer", "opengl"] => Win32RenderingBackend.OpenGL,
            ["--renderer", "software"] => Win32RenderingBackend.Software,
            ["--renderer", "vulkan"] => Win32RenderingBackend.Vulkan,
            _ => (Win32RenderingBackend?)null,
        };

        if (backend is null)
        {
            Console.Error.WriteLine("Usage: UnoSample [--renderer opengl|software|vulkan]");
            return 1;
        }

        App.RendererName = backend == Win32RenderingBackend.Default ? "OpenGL (Uno's default)" : backend.Value.ToString();

        UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWin32(win32 => win32.RenderingBackend(backend.Value))
            .Build()
            .Run();

        return 0;
    }
}
