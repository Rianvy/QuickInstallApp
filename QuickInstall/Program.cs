using QuickInstall;

class QuickInstallApp
{
    private ProgramManager programManager;
    private UIManager uiManager;

    public QuickInstallApp()
    {
        programManager = new ProgramManager("programs.json", "Downloads");
        uiManager = new UIManager(programManager);
    }

    public void Run()
    {
        uiManager.Start();
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("Starting QuickInstall...");
        var app = new QuickInstallApp();
        app.Run();
    }
}


