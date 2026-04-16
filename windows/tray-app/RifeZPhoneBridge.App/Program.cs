using System.Threading;
using RifeZPhoneBridge.App;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var singleInstanceMutex = new Mutex(initiallyOwned: true, name: "Global\\RifeZPhoneBridge.App", createdNew: out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("RifeZ Audio Bridge is already running.", "RifeZ Audio Bridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
