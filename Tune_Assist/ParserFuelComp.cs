namespace AutoTune
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Data;
  using System.Linq;
  using System.Text;
  using System.Windows.Forms;

  public class ParserFuelComp
  {
    private BuffDV_FuelComp buffFC = new BuffDV_FuelComp();
    //private List<int> tmpRPMlist = BuffDV_FuelComp.FcRPM;
    //private List<double> tmpXlist = BuffDV_FuelComp.fcThrottlePercent;

    private IndexFinder indexer = new IndexFinder();
    private DataTable DT_FC = new DataTable();
    private double accel;
    private double accelChange;
    private bool accelAfterDecel;
    private double actualAFR1;
    private double actualAFR2;
    private double afr1;
    private double afr2;
    private double coolantTemp;
    private double finaltrim1;
    private double finaltrim2;
    private double fuelXtrace;
    private int indexFinderDB;
    private int indexFinderRPM;
    private double intakeAirTemp;
    private double intakeAirTempAVG;
    private double longtrim1;
    private double longtrim2;
    private double accelNext;
    private int shorttrim1;
    private int shorttrim2 = 100;
    private int rpm;
    private double target;
    private int timeNext;
    private int time;
    private bool dualTB;

    private void FindIAT_average(DataGridView tempgrid)
    {
      List<double> iatFull = new List<double>();

      for (int r = 0; r < tempgrid.Rows.Count - 1; ++r)
      {
        double intakeAirTemp = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.IntakeAirTempDex].Value);
        iatFull.Add(intakeAirTemp);
      }

      this.intakeAirTempAVG = (double)iatFull.Average();
    }

    // *********** Fuel Comp Adjustments below.
    public DataTable AdjustFuelComp(BackgroundWorker bw, DataGridView tempgrid)
    {
      using (DataTable dt_FC_hits = new DataTable())
      using (DataTable dt_FC_totals = new DataTable())
      {
        if (IndexFinder.TimeDex == -1
          || IndexFinder.StB1Dex == -1
          || IndexFinder.StB2Dex == -1
          || IndexFinder.AccelDex == -1
          || IndexFinder.AfrB1Dex == -1
          || IndexFinder.AfrB2Dex == -1
          || IndexFinder.TargetDex == -1
          || IndexFinder.FuelCompTraceDex == -1
          || IndexFinder.RpmDex == -1
          || IndexFinder.IntakeAirTempDex == -1
          || tempgrid.Rows.Count < 50)
        {
          StringBuilder sb = new StringBuilder();
          sb.Append("Could not find the following headers: \n");
          if (IndexFinder.TimeDex == -1) { sb.Append("Time\n"); }
          if (IndexFinder.StB1Dex == -1) { sb.Append("A/F CORR-B1 (%)\n"); }
          if (IndexFinder.StB2Dex == -1) { sb.Append("A/F CORR-B2 (%)\n"); }
          if (IndexFinder.AccelDex == -1) { sb.Append("ACCEL PED POS 1\n"); }
          if (IndexFinder.AfrB1Dex == -1) { sb.Append("AFR WB-B1\n"); }
          if (IndexFinder.AfrB2Dex == -1) { sb.Append("AFR WB-B2\n"); }
          if (IndexFinder.TargetDex == -1) { sb.Append("TARGET AFR\n"); }
          if (IndexFinder.IntakeAirTempDex == -1) { sb.Append("INTAKE AIR TMP\n"); }
          if (IndexFinder.FuelCompTraceDex == -1) { sb.Append("Fuel Compensation X Trace\n"); }
          if (IndexFinder.RpmDex == -1) { sb.Append("ENGINE RPM (rpm)\n"); }

          Console.WriteLine(Convert.ToString(sb));
          return this.DT_FC;
        }

        if (Properties.Settings.Default.MAF_IAT && IndexFinder.IntakeAirTempDex != -1)
        {
          this.FindIAT_average(tempgrid);
        }

        foreach (int i in this.buffFC.FcThrottlePercent)
        {
          dt_FC_hits.Columns.Add(Convert.ToString(i), typeof(int));
          dt_FC_totals.Columns.Add(Convert.ToString(i), typeof(double));
          this.DT_FC.Columns.Add(Convert.ToString(i), typeof(double));
        }

        // foreach (int i in this.tmpRPMlist)
        foreach (int i in this.buffFC.FcRPM)
        {
          dt_FC_hits.Rows.Add();
          dt_FC_totals.Rows.Add();
          this.DT_FC.Rows.Add();
        }

        for (int row = 0; row < dt_FC_totals.Rows.Count; ++row)
        {
          for (int col = 0; col < dt_FC_totals.Columns.Count; ++col)
          {
            dt_FC_totals.Rows[row][col] = "100";
            dt_FC_hits.Rows[row][col] = "1";
            this.DT_FC.Rows[row][col] = "100";
          }
        }

        for (int row = AutoTune.Lineforheaders; row < tempgrid.Rows.Count - 10; ++row)
        {
          try
          {
            if (IndexFinder.AccelDex != -1)
            {
              this.accel = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.AccelDex].Value);
              this.accelNext = Convert.ToDouble(tempgrid.Rows[row + 1].Cells[IndexFinder.AccelDex].Value);
              if (IndexFinder.TimeDex != -1)
              {
                this.time = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.TimeDex].Value);
                this.timeNext = Convert.ToInt32(tempgrid.Rows[row + 1].Cells[IndexFinder.TimeDex].Value);
                this.accelChange = Convert.ToDouble((this.accelNext - this.accel) * 1000);
              }
              else
              {
                this.accelChange = 0.0;
              }
            }
            else
            {
              this.accel = 1.4;
              this.accelNext = 1.4;
            }

            if (IndexFinder.CoolantTempDex != -1)
            {
              this.coolantTemp = Convert.ToDouble(tempgrid.Rows[row + 1].Cells[IndexFinder.CoolantTempDex].Value);
            }
            else
            {
              this.coolantTemp = 200.1;
            }

            if (IndexFinder.IntakeAirTempDex != -1)
            {
              this.intakeAirTemp = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.IntakeAirTempDex].Value);
            }
            else
            {
              this.intakeAirTemp = 200.1;
            }

            this.shorttrim1 = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.StB1Dex].Value);
            this.shorttrim2 = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.StB1Dex].Value);
          }
          catch
          {
            Console.WriteLine(" error while setting parameter values for row {0}", row);
            continue;
          }

          try
          {
            this.afr1 = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.AfrB1Dex].Value);
            this.afr2 = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.AfrB2Dex].Value);
            this.fuelXtrace = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.FuelCompTraceDex].Value);
            this.shorttrim1 = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.StB1Dex].Value);
            this.shorttrim2 = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.StB1Dex].Value);
            this.rpm = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.RpmDex].Value);
            this.target = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.TargetDex].Value);
          }
          catch
          {
            Console.WriteLine(" Error in Fuel Comp. Could not find parameters needed.");
            return this.DT_FC;
          }

          // Makes sure the given RPM value lands in the last index.
          int lastRPMx = this.buffFC.FcRPM.Count - 1;
          if (this.rpm > this.buffFC.FcRPM[lastRPMx])
          {
            this.rpm = this.buffFC.FcRPM[lastRPMx];
          }

          if (this.target > 15 || this.rpm < 600)
          {
            continue;
          }

          // Back on accel after decel  ** This will skip down rows to avoid skewing values
          if (this.afr1 == 60 || this.target == 30)
          {
            this.accelAfterDecel = true;
            continue;
          }
          else if (this.accelAfterDecel)
          {
            row += 10;
            this.accelAfterDecel = false;
            continue;
          }

          // Only allows rows where intake air temp are close to the average.
          if (this.intakeAirTemp <= this.intakeAirTempAVG - 8 && this.intakeAirTemp >= this.intakeAirTempAVG + 8
            && this.accelChange > -0.1 && this.accelChange < 0.1)
          {
            continue;
          }

          this.indexFinderDB = this.buffFC.FcThrottlePercent.BinarySearch(this.fuelXtrace);
          if (this.indexFinderDB < 0)
          {
            this.indexFinderDB = ~this.indexFinderDB;
          }

          this.indexFinderRPM = this.buffFC.FcRPM.BinarySearch(this.rpm);
          if (this.indexFinderRPM < 0)
          {
            this.indexFinderRPM = ~this.indexFinderRPM;
          }

          if (IndexFinder.StB1Dex != -1 && IndexFinder.StB2Dex != -1 && this.afr1 < 25 && this.afr2 < 25 && this.target == 14.7)
          {
            this.shorttrim1 = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.StB1Dex].Value);
            this.shorttrim2 = Convert.ToInt32(tempgrid.Rows[row].Cells[IndexFinder.StB2Dex].Value);

            // if long term trimlogged
            if (IndexFinder.LtB1Dex != -1 && IndexFinder.LtB2Dex != -1)
            {
              this.longtrim1 = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.LtB1Dex].Value);
              this.longtrim2 = Convert.ToDouble(tempgrid.Rows[row].Cells[IndexFinder.LtB2Dex].Value);
              this.finaltrim1 = this.shorttrim1 + (Convert.ToInt32(this.longtrim1) - 100);
              this.finaltrim2 = this.shorttrim2 + (Convert.ToInt32(this.longtrim2) - 100);
              this.finaltrim1 = (this.finaltrim1 + this.finaltrim2) / 2;
            }
            else
            {
              this.finaltrim1 = (this.shorttrim1 + this.shorttrim2) / 2;
            }
          }
          else if (IndexFinder.StB1Dex != -1 && IndexFinder.StB2Dex != -1 && this.afr1 < 25 && this.afr2 < 25 && this.target < 14.7)
          {
            this.actualAFR1 = Convert.ToDouble(tempgrid.Rows[row + 2].Cells[IndexFinder.AfrB1Dex].Value);

            if (this.dualTB)
            {
              this.actualAFR2 = Convert.ToDouble(tempgrid.Rows[row + 2].Cells[IndexFinder.AfrB2Dex].Value);
            }
            else
            {
              this.actualAFR2 = 0;
            }

            if (this.actualAFR1 == 0 || this.actualAFR1 > 18 || this.actualAFR2 > 18)
            {
              this.finaltrim1 = 0;
              continue;
            }
            else if (this.actualAFR1 != 0 && this.actualAFR2 != 0)
            {
              this.finaltrim1 = (((this.actualAFR1 + this.actualAFR2) / 2) / this.target) * 100;
            }
            else
            {
              this.finaltrim1 = (this.actualAFR1 / this.target) * 100;
            }
          }

          int hitCount = Convert.ToInt32(dt_FC_hits.Rows[this.indexFinderRPM][this.indexFinderDB]);
          double value = Convert.ToDouble(dt_FC_totals.Rows[this.indexFinderRPM][this.indexFinderDB]);

          if (value == 100
            && hitCount == 1)
          {
            dt_FC_totals.Rows[this.indexFinderRPM][this.indexFinderDB] = this.finaltrim1 + 100;
            dt_FC_hits.Rows[this.indexFinderRPM][this.indexFinderDB] = hitCount + 1;
          }
          else
          {
            dt_FC_totals.Rows[this.indexFinderRPM][this.indexFinderDB] = value + this.finaltrim1;
            dt_FC_hits.Rows[this.indexFinderRPM][this.indexFinderDB] = Convert.ToString(hitCount + 1);
          }
        }

        for (int row = 0; row < dt_FC_totals.Rows.Count; ++row)
        {
          for (int col = 0; col < dt_FC_totals.Columns.Count; ++col)
          {
            double total = 100;
            int hits = 1;
            if (dt_FC_totals.Rows[row][col].ToString() == "0" || dt_FC_totals.Rows[row][col].ToString() == "100" || string.IsNullOrEmpty(dt_FC_totals.Rows[row][col].ToString()))
            {
              total = 100;
            }
            else if (dt_FC_totals.Rows[row][col] != null || !string.IsNullOrEmpty(dt_FC_totals.Rows[row][col].ToString()))
            {
              string totalvalue = Convert.ToString(dt_FC_totals.Rows[row][col]);
              total = Convert.ToDouble(totalvalue);
            }

            if (dt_FC_hits.Rows[row][col].ToString() == "0" || dt_FC_hits.Rows[row][col].ToString() == "1" || string.IsNullOrEmpty(dt_FC_hits.Rows[row][col].ToString()))
            {
              hits = 1;
            }
            else if (dt_FC_hits.Rows[row][col] != null || !string.IsNullOrEmpty(dt_FC_hits.Rows[row][col].ToString()))
            {
              hits = Convert.ToInt32(dt_FC_hits.Rows[row][col]);
            }

            if (total == 0 || hits == 0)
            {
              this.DT_FC.Rows[row][col] = 100;
            }
            else
            {
              this.DT_FC.Rows[row][col] = Convert.ToDouble(Convert.ToDouble(dt_FC_totals.Rows[row][col]) / Convert.ToInt32(dt_FC_hits.Rows[row][col]));
            }
          }
        }

        return this.DT_FC;
      }
    }
  }
}
