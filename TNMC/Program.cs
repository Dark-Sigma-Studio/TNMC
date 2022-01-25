using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TNMC;



Console.WriteLine("Hello World!");

using (var window = new Game(GameWindowSettings.Default, new NativeWindowSettings()
{
    Size = new Vector2i(800, 600),
    Title = "Hello World",
    Flags = ContextFlags.ForwardCompatible
}))
{
    window.Run();
}