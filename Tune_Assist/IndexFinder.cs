namespace AutoTune
{
  using System.Windows.Forms;

  public class IndexFinder
  {
    public static int TimeDex { get; set; }

    public static int StB1Dex { get; set; }

    public static int StB2Dex { get; set; }

    public static int AccelDex { get; set; }

    public static int LtB1Dex { get; set; }

    public static int LtB2Dex { get; set; }

    public static int AfrB1Dex { get; set; }

    public static int AfrB2Dex { get; set; }

    public static int MafB1Dex { get; set; }

    public static int MafB2Dex { get; set; }

    public static int TargetDex { get; set; }

    public static int IntakeAirTempDex { get; set; }

    public static int CoolantTempDex { get; set; }

    public static int FuelCompTraceDex { get; set; }

    public static int RpmDex { get; set; }

    public static bool DualTB { get; set; }

    public void FindHeader_Indexes(string headerLine)
    {
      string[] headerArray = headerLine.Split(',');
      TimeDex = -1;
      StB1Dex = -1;
      StB2Dex = -1;
      AccelDex = -1;
      LtB1Dex = -1;
      LtB2Dex = -1;
      AfrB1Dex = -1;
      AfrB2Dex = -1;
      MafB1Dex = -1;
      MafB2Dex = -1;
      TargetDex = -1;
      IntakeAirTempDex = -1;
      CoolantTempDex = -1;
      FuelCompTraceDex = -1;
      RpmDex = -1;

      for (int i = 0; i < headerArray.Length; ++i)
      {
        string header = headerArray[i];

        if (TimeDex == -1 && header.Contains("Time"))
        {
          TimeDex = i;
          continue;
        }

        if (StB1Dex == -1 && (header.Contains("Fuel Trim Short Term Bank #1")
          || header.Contains("A/F CORR-B1")))
        {
          StB1Dex = i;
          continue;
        }

        if (StB2Dex == -1 && (header.Contains("Fuel Trim Short Term Bank #2")
          || header.Contains("A/F CORR-B2")))
        {
          StB2Dex = i;
          continue;
        }

        if (AccelDex == -1 && (header.Contains("Accelerator Pedal Sensor #1")
          || header.Contains("ACCEL PED POS 1")
          || header.Contains("THROTTLE SENSOR 1 - B1")))
        {
          AccelDex = i;
          continue;
        }

        if (LtB1Dex == -1 && (header.Contains("Fuel Trim Long Term Bank #1")
          || header.Contains("LT Fuel Trim B1")))
        {
          LtB1Dex = i;
          continue;
        }

        if (LtB2Dex == -1 && (header.Contains("Fuel Trim Long Term Bank #2")
          || header.Contains("LT Fuel Trim B2")))
        {
          LtB2Dex = i;
          continue;
        }

        if (AfrB1Dex == -1 && (header.Contains("AFR Bank1 (afr)")
          || header.Contains("LC-1 (1) AFR")
          || header.Contains("AFR WB-B1")))
        {
          AfrB1Dex = i;
          continue;
        }

        if (AfrB2Dex == -1 && (header.Contains("AFR Bank2 (afr)")
          || header.Contains("LC-1 (2) AFR")
          || header.Contains("AFR WB-B2")))
        {
          AfrB2Dex = i;
          continue;
        }

        if (MafB1Dex == -1 && (header.Contains("Mass Airflow Sensor Bank #1")
          || header.Contains("MAS A/F -B1")))
        {
          MafB1Dex = i;
          continue;
        }

        if (MafB2Dex == -1 && (header.Contains("Mass Airflow Sensor Bank #2")
          || header.Contains("MAS A/F -B2")))
        {
          DualTB = true;
          MafB2Dex = i;
          continue;
        }

        if (TargetDex == -1 && (header.Contains("AFR Target")
          || header.Contains("TARGET AFR")))
        {
          TargetDex = i;
          continue;
        }

        if (IntakeAirTempDex == -1 && (header.Contains("Intake Air Temperature")
          || header.Contains("INTAKE AIR TMP")))
        {
          IntakeAirTempDex = i;
          continue;
        }

        if (CoolantTempDex == -1 && (header.Contains("Coolant Temperature")
          || header.Contains("COOLANT TEMP")
          || header.Contains("ENG OIL TEMP")))
        {
          CoolantTempDex = i;
          continue;
        }

        if (FuelCompTraceDex == -1 && header.Contains("Fuel Compensation X Trace"))
        {
          FuelCompTraceDex = i;
          continue;
        }

        if (RpmDex == -1 && (header.Contains("Engine Speed")
          || header.Contains("ENGINE RPM ")
          || header.Contains("ENGINE RPM (rpm)")))
        {
          RpmDex = i;
          continue;
        }
      }
    }
  }
}
