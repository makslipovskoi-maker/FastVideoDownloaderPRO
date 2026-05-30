using System;
using System.Windows;

namespace FastVideoDownloaderPRO;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application();
        app.Run(new MainWindow());
    }
}
