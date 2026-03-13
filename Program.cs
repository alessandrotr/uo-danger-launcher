namespace UoDangerLauncher;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        try
        {
            CleanupUpdateLeftovers();
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(ex.ToString(), "Startup Error");
        }
    }

    static void CleanupUpdateLeftovers()
    {
        try
        {
            string exe = Application.ExecutablePath;
            string old = exe + ".old";
            string update = exe + ".update";
            if (File.Exists(old)) File.Delete(old);
            if (File.Exists(update)) File.Delete(update);
        }
        catch { }
    }    
}