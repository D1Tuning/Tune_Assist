namespace AutoTune
{
  using System;
  using System.Windows.Forms;

  internal static class Program
  {
    [STAThread]
    private static void Main(string[] arg)
    {
      try
      {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (arg.Length != 0)
        {
          AutoTune.FileName = arg[0];
          Application.Run(new AutoTune(AutoTune.FileName));
        }
        else
        {
          Application.Run(new AutoTune());
        }
      }
      catch
      {
        Console.Out.WriteLine("Error starting program!");
      }
    }
  }
}