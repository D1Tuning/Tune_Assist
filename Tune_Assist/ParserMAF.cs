namespace AutoTune
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Data;
  using System.Linq;
  using System.Text;
  using System.Windows.Forms;

  public class ParserMAF
  {
    private static List<double> mafVolts = new List<double>
    {
      0.00, 0.08, 0.16, 0.24, 0.32, 0.40, 0.48, 0.56, 0.64, 0.72, 0.80, 0.88, 0.96, 1.04, 1.12, 1.20, 1.28,
      1.36, 1.44, 1.52, 1.60, 1.68, 1.76, 1.84, 1.92, 2.00, 2.08, 2.16, 2.24, 2.32, 2.40, 2.48, 2.56, 2.64,
      2.72, 2.80, 2.88, 2.96, 3.04, 3.12, 3.20, 3.28, 3.36, 3.44, 3.52, 3.60, 3.68, 3.76, 3.84, 3.92, 4.00,
      4.08, 4.16, 4.24, 4.32, 4.40, 4.48, 4.56, 4.64, 4.72, 4.80, 4.88, 4.96, 5.04
    };

    private List<double> maf1ClosedLoop = new List<double>();
    private List<double> maf2ClosedLoop = new List<double>();
    private List<double> maf1OpenLoop = new List<double>();
    private List<double> maf2OpenLoop = new List<double>();
    private DataTable clDT1 = new DataTable();
    private DataTable clDT2 = new DataTable();
    private DataTable olDT1 = new DataTable();
    private DataTable olDT2 = new DataTable();
    private List<int> hitsCL1 = new List<int>(64);
    private List<int> hitsCL2 = new List<int>(64);
    private List<int> hitsOL1 = new List<int>(64);
    private List<int> hitsOL2 = new List<int>(64);
    private IndexFinder indexer = new IndexFinder();
    private double accel;
    private double accelChange;
    private double actualAFR1;
    private double actualAFR2;
    private double afr1;
    private double afr2;
    private double coolantTemp;
    private double finaltrim1;
    private double finaltrim2;
    private double olTrim1;
    private double olTrim2;
    private int indexFinder1;
    private int indexFinder2;
    private double intakeAirTemp;
    private double intakeAirTempAVG;
    private double longtrim1;
    private double longtrim2;
    private double maf1v;
    private double maf2v = 0;
    private double accelNext;
    private double timeNext;
    private int shorttrim1;
    private int shorttrim2 = 100;
    private double target;
    private double time;
    private int totalLines = 0;
    private string clStatus = string.Empty;
    private string olStatus = string.Empty;
    private bool accelAfterDecel;
    private static int TotalHits;

    public List<double> MafVolts
    {
      get
      {
        return mafVolts;
      }
    }

    public DataTable AdjustMAF(BackgroundWorker bw, DataGridView tempgrid)
    {
      using (DataTable dt = new DataTable())
      {
        // Init the adjustment lists and add voltage columns
        foreach (double d in mafVolts)
        {
          this.maf1ClosedLoop.Add(100.00);
          this.maf2ClosedLoop.Add(100.00);
          this.maf1OpenLoop.Add(100.00);
          this.maf2OpenLoop.Add(100.00);
          this.hitsCL1.Add(0);
          this.hitsCL2.Add(0);
          this.hitsOL1.Add(0);
          this.hitsOL2.Add(0);
          this.clDT1.Columns.Add(Convert.ToString(d));
          this.clDT2.Columns.Add(Convert.ToString(d));
          this.olDT1.Columns.Add(Convert.ToString(d));
          this.olDT2.Columns.Add(Convert.ToString(d));
        }

        if (tempgrid.Rows.Count >= 100)
        {
          this.totalLines = tempgrid.Rows.Count - AutoTune.Lineforheaders;
        }
        else
        {
          MessageBox.Show("The selected CSV log is not long enough.\nPlease log more data and retry.");
          return dt;
        }


        if (Properties.Settings.Default.MAF_IAT && IndexFinder.IntakeAirTempDex != -1)
        {
          this.FindIAT_average(tempgrid);
        }

        this.BuildDT();

        if (IndexFinder.TargetDex != -1 && IndexFinder.MafB1Dex != -1 && IndexFinder.AfrB1Dex != -1 && IndexFinder.AfrB2Dex != -1)
        {
          for (int r = AutoTune.Lineforheaders + 1; r < tempgrid.Rows.Count - 1; ++r)
          {
            try
            {
              this.target = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.TargetDex].Value);
              this.maf1v = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.MafB1Dex].Value);
              this.afr1 = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.AfrB1Dex].Value);
              this.afr2 = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.AfrB2Dex].Value);
            }
            catch
            {
              Console.WriteLine("Error while setting parameter values for row {0}", r);
              continue;
            }

            if (IndexFinder.StB1Dex != -1 && IndexFinder.StB2Dex != -1)
            {
              try
              {
                if (IndexFinder.AccelDex != -1)
                {
                  this.accel = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.AccelDex].Value);
                  this.accelNext = Convert.ToDouble(tempgrid.Rows[r + 1].Cells[IndexFinder.AccelDex].Value);
                  if (IndexFinder.TimeDex != -1)
                  {
                    this.time = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.TimeDex].Value);
                    this.timeNext = Convert.ToDouble(tempgrid.Rows[r + 1].Cells[IndexFinder.TimeDex].Value);
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
                  this.coolantTemp = Convert.ToDouble(tempgrid.Rows[r + 1].Cells[IndexFinder.CoolantTempDex].Value);
                }
                else
                {
                  this.coolantTemp = 200.1;
                }

                if (IndexFinder.IntakeAirTempDex != -1)
                {
                  this.intakeAirTemp = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.IntakeAirTempDex].Value);
                }
                else
                {
                  this.intakeAirTemp = 200.1;
                }

                this.shorttrim1 = Convert.ToInt32(tempgrid.Rows[r].Cells[IndexFinder.StB1Dex].Value);
                this.shorttrim2 = Convert.ToInt32(tempgrid.Rows[r].Cells[IndexFinder.StB1Dex].Value);

                if (IndexFinder.DualTB)
                {
                  this.maf2v = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.MafB2Dex].Value);
                }
              }
              catch
              {
                Console.WriteLine(" error while setting parameter values for row {0}", r);
                continue;
              }

              // Back on accel after decel  ** This will skip down rows to avoid skewing values
              if (this.afr1 == 60 || this.target == 30)
              {
                this.accelAfterDecel = true;
                continue;
              }
              else if (this.afr1 < 20 && this.target < 15 && this.accelAfterDecel)
              {
                r += 9;
                this.accelAfterDecel = false;
                continue;
              }

              int minCoolantTemp = 174;
              if (AutoTune.LogType == "ecutek")
              {
                minCoolantTemp = 79;
              }
              else if (AutoTune.LogType == "uprev")
              {
                minCoolantTemp = 174;
              }

              // Closed loop
              if (this.target == 14.7 && this.coolantTemp > minCoolantTemp && Properties.Settings.Default.MAF_CL)
              {
                // Dual throttle bodies and have logged long term trim
                if (IndexFinder.LtB1Dex != -1 && IndexFinder.LtB2Dex != -1 && IndexFinder.DualTB)
                {
                  this.longtrim1 = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.LtB1Dex].Value);
                  this.longtrim2 = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.LtB2Dex].Value);
                  if (this.longtrim1 == 100)
                  {
                    this.finaltrim1 = this.shorttrim1;
                  }
                  else
                  {
                    this.finaltrim1 = (double)(this.shorttrim1 * .333) + (this.longtrim1 * .666);
                  }

                  if (this.longtrim2 == 100)
                  {
                    this.finaltrim2 = this.shorttrim2;
                  }
                  else
                  {
                    this.finaltrim2 = (double)(this.shorttrim2 * .333) + (this.longtrim2 * .666);
                  }
                }

                // Dual throttle bodies and have NOT logged long term trim
                else if ((IndexFinder.LtB1Dex == -1 || IndexFinder.LtB2Dex == -1) && IndexFinder.DualTB)
                {
                  this.finaltrim1 = this.shorttrim1;
                  this.finaltrim2 = this.shorttrim2;
                }

                // Single throttle body and have logged long term trim
                else if (!IndexFinder.DualTB && (IndexFinder.LtB1Dex != -1 || IndexFinder.LtB2Dex != -1))
                {
                  this.longtrim1 = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.LtB1Dex].Value);
                  this.longtrim2 = Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.LtB2Dex].Value);

                  if (this.longtrim1 == 100)
                  {
                    this.finaltrim1 = this.shorttrim1;
                  }
                  else
                  {
                    this.finaltrim1 = (double)(this.shorttrim1 * .333) + (this.longtrim1 * .666);

                  }

                  if (this.longtrim2 == 100)
                  {
                    this.finaltrim2 = (double)(this.shorttrim2 * .333) + (this.longtrim2 * .666);
                  }
                  else
                  {
                    this.finaltrim2 = this.shorttrim2;
                  }

                  this.finaltrim1 = (double)(this.finaltrim1 + this.finaltrim2) / 2;
                }
                else
                {
                  this.finaltrim1 = (this.shorttrim1 + this.shorttrim2) / 2;
                  this.finaltrim2 = 100;
                }

                this.indexFinder1 = mafVolts.BinarySearch(this.maf1v);
                if (this.indexFinder1 < 0)
                {
                  this.indexFinder1 = ~this.indexFinder1;
                  if (this.indexFinder1 <= 0 || this.indexFinder1 > mafVolts.Count)
                  {
                    continue;
                  }
                }

                if (IndexFinder.DualTB)
                {
                  this.indexFinder2 = mafVolts.BinarySearch(this.maf2v);
                  if (this.indexFinder2 < 0)
                  {
                    this.indexFinder2 = ~this.indexFinder2;
                    if (this.indexFinder2 <= 0 || this.indexFinder2 > mafVolts.Count)
                    {
                      continue;
                    }
                  }
                }

                // CLOSED LOOP
                // IAT filter: OFF -- ACCEL filter: ON
                if ((!Properties.Settings.Default.MAF_IAT || IndexFinder.IntakeAirTempDex == -1)
                      && Properties.Settings.Default.MAF_ACCEL && this.accelChange > -100 && this.accelChange < 100
                      && this.indexFinder1 >= 0 && this.indexFinder1 < mafVolts.Count)
                {
                  this.ClosedLoop_Start();
                }

                // IAT filter: ON -- ACCEL filter: OFF
                else if (Properties.Settings.Default.MAF_IAT && IndexFinder.IntakeAirTempDex != -1
                        && (this.intakeAirTemp >= this.intakeAirTempAVG - 10 && this.intakeAirTemp <= this.intakeAirTempAVG + 10)
                        && (!Properties.Settings.Default.MAF_ACCEL || IndexFinder.AccelDex == -1))
                {
                  this.ClosedLoop_Start();
                }

                // filter out intake air temp changes  &&  filter out quick accel pedal position changes
                else if (Properties.Settings.Default.MAF_IAT && IndexFinder.IntakeAirTempDex != -1
                        && (this.intakeAirTemp >= this.intakeAirTempAVG - 10 && this.intakeAirTemp <= this.intakeAirTempAVG + 10)
                        && Properties.Settings.Default.MAF_ACCEL && this.accelChange > -60 && this.accelChange < 60)
                {
                  this.ClosedLoop_Start();
                }
                else if ((Properties.Settings.Default.MAF_IAT || IndexFinder.IntakeAirTempDex != -1)
                          && (!Properties.Settings.Default.MAF_ACCEL || IndexFinder.AccelDex == -1))
                {
                  this.ClosedLoop_Start();
                }
              }

              // Open loop
              else if (this.target < 14.7 && this.shorttrim1 == 100 && this.afr1 < 20 && this.coolantTemp > minCoolantTemp && Properties.Settings.Default.MAF_OL)
              {
                this.olTrim1 = this.afr1 / this.target;
                this.olTrim2 = this.afr2 / this.target;

                this.indexFinder1 = mafVolts.BinarySearch(this.maf1v);
                if (this.indexFinder1 < 0)
                {
                  this.indexFinder1 = ~this.indexFinder1;
                  if (this.indexFinder1 <= 0 || this.indexFinder1 > mafVolts.Count)
                  {
                    continue;
                  }
                }

                if (IndexFinder.DualTB)
                {
                  this.indexFinder2 = mafVolts.BinarySearch(this.maf2v);
                  if (this.indexFinder2 < 0)
                  {
                    this.indexFinder2 = ~this.indexFinder2;
                    if (this.indexFinder2 <= 0 || this.indexFinder2 > mafVolts.Count)
                    {
                      continue;
                    }
                  }
                }

                this.actualAFR1 = 0;
                this.actualAFR2 = 0;

                try
                {
                  this.actualAFR1 = Convert.ToDouble(tempgrid.Rows[r + 2].Cells[IndexFinder.AfrB1Dex].Value);
                  if (IndexFinder.DualTB)
                  {
                    this.actualAFR2 = Convert.ToDouble(tempgrid.Rows[r + 2].Cells[IndexFinder.AfrB2Dex].Value);
                  }
                }
                catch
                {
                  Console.WriteLine(" error while actualAFR values for row {0}", r);
                  continue;
                }

                // Open Loop Starter
                // IAT filter: OFF -- ACCEL filter: ON
                if ((!Properties.Settings.Default.MAF_IAT || IndexFinder.IntakeAirTempDex == -1)
                      && Properties.Settings.Default.MAF_ACCEL && this.accelChange > -60 && this.accelChange < 60
                      && this.indexFinder1 >= 0 && this.indexFinder1 < mafVolts.Count)
                {
                  this.OpenLoop_Start();
                }

                // IAT filter: ON -- ACCEL filter: OFF
                else if (Properties.Settings.Default.MAF_IAT && IndexFinder.IntakeAirTempDex != -1
                        && (this.intakeAirTemp >= this.intakeAirTempAVG - 10 && this.intakeAirTemp <= this.intakeAirTempAVG + 10)
                        && (!Properties.Settings.Default.MAF_ACCEL || IndexFinder.AccelDex == -1))
                {
                  this.OpenLoop_Start();
                }

                // filter out intake air temp changes  &&  filter out quick accel pedal position changes
                else if (Properties.Settings.Default.MAF_IAT && IndexFinder.IntakeAirTempDex != -1
                        && (this.intakeAirTemp >= this.intakeAirTempAVG - 10 && this.intakeAirTemp <= this.intakeAirTempAVG + 10)
                        && Properties.Settings.Default.MAF_ACCEL && this.accelChange > -60 && this.accelChange < 60)
                {
                  this.OpenLoop_Start();
                }
                else if ((Properties.Settings.Default.MAF_IAT || IndexFinder.IntakeAirTempDex != -1)
                          && (!Properties.Settings.Default.MAF_ACCEL || IndexFinder.AccelDex == -1))
                {
                  this.OpenLoop_Start();
                }
              }
            }
            else
            {
              StringBuilder sb = new StringBuilder();
              sb.Append("Could not find the following headers: \n");
              if (IndexFinder.TimeDex == -1) { sb.Append("Time\n"); }
              if (IndexFinder.StB1Dex == -1) { sb.Append("A/F CORR-B1 (%)\n"); }
              if (IndexFinder.StB2Dex == -1) { sb.Append("A/F CORR-B2 (%)\n"); }
              if (IndexFinder.AccelDex == -1) { sb.Append("ACCEL PED POS 1\n"); }
              if (IndexFinder.LtB1Dex == -1) { sb.Append("LT Fuel Trim B1 (%)\n"); }
              if (IndexFinder.LtB2Dex == -1) { sb.Append("LT Fuel Trim B2 (%)\n"); }
              if (IndexFinder.AfrB1Dex == -1) { sb.Append("AFR WB-B1\n"); }
              if (IndexFinder.AfrB2Dex == -1) { sb.Append("AFR WB-B2\n"); }
              if (IndexFinder.MafB1Dex == -1) { sb.Append("MAS A/F -B1 (V)\n"); }
              if (IndexFinder.MafB2Dex == -1) { sb.Append("MAS A/F -B2 (V)\n"); }
              if (IndexFinder.TargetDex == -1) { sb.Append("TARGET AFR\n"); }
              if (IndexFinder.IntakeAirTempDex == -1) { sb.Append("INTAKE AIR TMP\n"); }
              if (IndexFinder.CoolantTempDex == -1) { sb.Append("COOLANT TEMP\n"); }

              Console.WriteLine(Convert.ToString(sb));
              MessageBox.Show(
                "Error", "We could not find minimal parameters needed \n to calculate the MAF scaling adjustments.\n"
                + Convert.ToString(sb) + "\n Please add these parameters to the uprev logger and try again.");
            }
          }

          // END of looping rows
          // NOW start reading valuses from DT
          this.ClosedLoop_Finish();

          this.OpenLoop_Finish();

          // Build DataTable for returning values
          dt.Columns.Add("Voltage", typeof(double));
          dt.Columns.Add("ClosedLoop_B1", typeof(double));
          dt.Columns.Add("ClosedLoop_B2", typeof(double));
          dt.Columns.Add("Hits_B1", typeof(int));
          dt.Columns.Add("Hits_B2", typeof(int));
          for (int i = 0; i < mafVolts.Count; ++i)
          {
            DataRow dr = dt.NewRow();
            double CL1 = this.maf1ClosedLoop[i];
            double CL2 = this.maf2ClosedLoop[i];
            double OL1 = this.maf1OpenLoop[i];
            double OL2 = this.maf2OpenLoop[i];
            double final1 = 0;
            double final2 = 0;
            double diff = 0;
            double diff1 = 0;
            double diff2 = 0;

            // Minimal changes to the maf
            if (Properties.Settings.Default.Maf_MINIMAL)
            {
              if (CL1 == 100 && OL1 != 100)
              {
                final1 = OL1;
              }
              else if (CL1 != 100 && OL1 != 100)
              {
                final1 = (CL1 * .333) + (OL1 * .666);
              }
              else
              {
                final1 = CL1;
              }

              if (CL2 == 100 && OL2 != 100)
              {
                final2 = OL2;
              }
              else if (CL2 != 100 && OL2 != 100)
              {
                final2 = (CL2 * .333) + (OL2 * .666);
              }
              else
              {
                final2 = CL2;
              }

              // Finding difference between finals
              diff = final1 - final2;

              if (diff < 0)
              {
                diff = -diff;
              }

              if (final1 < 100 && final2 < 100)
              {
                if (final1 < final2)
                {
                  final1 = final2 = 100;
                  final1 -= diff;
                }
                else if (final1 > final2)
                {
                  final1 = final2 = 100;
                  final2 -= diff;
                }
              }
              else if (final1 > 100 && final2 > 100)
              {
                if (final1 < final2)
                {
                  final1 = final2 = 100;
                  final2 += diff;
                }
                else if (final1 > final2)
                {
                  final1 = final2 = 100;
                  final1 += diff;
                }
              }
              else
              {
                diff = final1 - final2;
                diff1 = final1 - 100;
                diff2 = final2 - 100;
                if (diff1 < 0)
                {
                  diff1 = -diff;
                }

                if (diff2 < 0)
                {
                  diff2 = -diff;
                }

                diff1 /= 4;
                diff2 /= 4;
                if (final1 < 100 && (final1 + diff1) < 100)
                {
                  final1 += diff1;
                }
                else if (final1 > 100 && (final1 - diff1) > 100)
                {
                  final1 -= diff1;
                }
                else
                {
                  final1 = 100;
                }

                if (final2 < 100 && (final2 + diff2) < 100)
                {
                  final2 += diff2;
                }
                else if (final2 > 100 && (final2 - diff2) > 100)
                {
                  final2 -= diff2;
                }
                else
                {
                  final2 = 100;
                }
              }
            }

            // If not using "Minimal Changes"
            else
            {
              if (CL1 == 100 && OL1 != 100)
              {
                final1 = (double)OL1;
              }
              else if (CL1 != 100 && OL1 == 100)
              {
                final1 = (double)CL1;
              }
              else if (CL1 != 100 && OL1 != 100)
              {
                final1 = (double)(CL1 * .445) + (OL1 * .555);
              }
              else
              {
                final1 = 100;
              }

              if (CL2 == 100 && OL2 != 100)
              {
                final2 = (double)OL2;
              }
              else if (CL2 != 100 && OL2 == 100)
              {
                final2 = (double)CL2;
              }
              else if (CL2 != 100 && OL2 != 100)
              {
                final2 = (double)(CL2 * .445) + (OL2 * .555);
              }
              else
              {
                final2 = 100;
              }
            }

            dr[0] = (double)mafVolts[i];
            dr[1] = final1;
            dr[2] = final2;
            dr[3] = (int)this.hitsCL1[i];
            dr[4] = (int)this.hitsCL2[i];
            dt.Rows.Add(dr);
          }
        }
        else
        {
          StringBuilder sb = new StringBuilder();
          sb.Append("Could not find the following headers: \n");
          if (IndexFinder.TimeDex == -1) { sb.Append("Time\n"); }
          if (IndexFinder.StB1Dex == -1) { sb.Append("A/F CORR-B1 (%)\n"); }
          if (IndexFinder.StB2Dex == -1) { sb.Append("A/F CORR-B2 (%)\n"); }
          if (IndexFinder.AccelDex == -1) { sb.Append("ACCEL PED POS 1\n"); }
          if (IndexFinder.LtB1Dex == -1) { sb.Append("LT Fuel Trim B1 (%)\n"); }
          if (IndexFinder.LtB2Dex == -1) { sb.Append("LT Fuel Trim B2 (%)\n"); }
          if (IndexFinder.AfrB1Dex == -1) { sb.Append("AFR WB-B1\n"); }
          if (IndexFinder.AfrB2Dex == -1) { sb.Append("AFR WB-B2\n"); }
          if (IndexFinder.MafB1Dex == -1) { sb.Append("MAS A/F -B1 (V)\n"); }
          if (IndexFinder.MafB2Dex == -1) { sb.Append("MAS A/F -B2 (V)\n"); }
          if (IndexFinder.TargetDex == -1) { sb.Append("TARGET AFR\n"); }
          if (IndexFinder.IntakeAirTempDex == -1) { sb.Append("INTAKE AIR TMP\n"); }
          if (IndexFinder.CoolantTempDex == -1) { sb.Append("COOLANT TEMP\n"); }
          Console.WriteLine(Convert.ToString(sb));
          MessageBox.Show("Error", "We could not find minimal parameters needed \n to calculate Closed Loop MAF scaling adjustments.\n" + sb.ToString());
        }

        return dt;
      }
    }

    private void ClosedLoop_Start()
    {
      // DUAL MAF
      if (IndexFinder.DualTB)
      {
        // MAF1
        for (int i = 0; ;)
        {
          if (this.finaltrim1 < 75 || this.finaltrim1 > 125)
          {
            break;
          }

          // Find empty spot to insert value in DataTable
          if (i == this.clDT1.Rows.Count - 1 || this.clDT1.Rows.Count == 0)
          {
            DataRow dr = this.clDT1.NewRow();
            int c = 0;
            foreach (double d in mafVolts)
            {
              dr[c] = 1.1;
              ++c;
            }

            this.clDT1.Rows.Add(dr);
          }

          double cell1 = Convert.ToDouble(this.clDT1.Rows[i][this.indexFinder1]);

          if (cell1 == 1.1)
          {
            this.clDT1.Rows[i][this.indexFinder1] = this.finaltrim1;
            break;
          }
          else
          {
            ++i;
          }
        }

        // MAF 2
        for (int i = 0; ;)
        {
          // Find empty spot to insert value in DataTable 2
          if (i == this.clDT2.Rows.Count - 1 || this.clDT2.Rows.Count == 0)
          {
            DataRow dr = this.clDT2.NewRow();
            int c = 0;
            foreach (double d in mafVolts)
            {
              dr[c] = 1.1;
              ++c;
            }

            this.clDT2.Rows.Add(dr);
          }

          double cell2 = Convert.ToDouble(this.clDT2.Rows[i][this.indexFinder2]);
          if (this.finaltrim1 < 75 || this.finaltrim1 > 125)
          {
            break;
          }

          if (cell2 == 1.1)
          {
            this.clDT2.Rows[i][this.indexFinder2] = this.finaltrim2;
            break;
          }
          else
          {
            ++i;
          }
        }
      }

      // Single MAF
      else
      {
        for (int i = 0; ;)
        {
          // find empty spot to insert value in DataTable
          double cell1 = Convert.ToDouble(this.clDT1.Rows[i][this.indexFinder1]);

          if (i == this.clDT1.Rows.Count - 1 || this.clDT1.Rows.Count == 0)
          {
            DataRow dr = this.clDT1.NewRow();
            int c = 0;
            foreach (double d in mafVolts)
            {
              dr[c] = 1.1;
              ++c;
            }

            this.clDT1.Rows.Add(dr);
          }

          if (this.finaltrim1 < 75 || this.finaltrim1 > 125)
          {
            break;
          }

          if (cell1 == 1.1)
          {
            this.clDT1.Rows[i][this.indexFinder1] = this.finaltrim1;
            break;
          }
          else
          {
            ++i;
          }
        }
      }
    }

    private void ClosedLoop_Finish()
    {
      if (IndexFinder.DualTB)
      {
        // MAF1
        for (int c = 0; c < this.clDT1.Columns.Count - 1; ++c)
        {
          // Read values from DataTable
          List<double> tmpList = new List<double>();
          for (int line = 0; line < this.clDT1.Rows.Count - 1; ++line)
          {
            double cell = Convert.ToDouble(this.clDT1.Rows[line][c]);
            if (cell != 1.1)
            {
              tmpList.Add(Convert.ToDouble(this.clDT1.Rows[line][c]));
            }
            else
            {
              break;
            }
          }

          // Shows how many hits were for each voltage
          if (this.hitsCL1[c] == 0)
          {
            this.hitsCL1[c] = tmpList.Count;
          }
          else
          {
            this.hitsCL1[c] += tmpList.Count;
          }

          if (tmpList.Count > 2)
          {
            this.maf1ClosedLoop[c] = (double)tmpList.Average();
          }
          else
          {
            this.maf1ClosedLoop[c] = 100;
          }
        }

        // MAF2
        for (int c = 0; c < this.clDT2.Columns.Count - 1; ++c)
        {
          // Read values from DataTable 2
          List<double> tmpList = new List<double>();
          for (int line = 0; line < this.clDT2.Rows.Count - 1; ++line)
          {
            double cell2 = Convert.ToDouble(this.clDT2.Rows[line][c]);
            if (cell2 != 1.1)
            {
              tmpList.Add(Convert.ToDouble(this.clDT2.Rows[line][c]));
            }
            else
            {
              break;
            }
          }

          // Shows how many hits were for each voltage
          if (this.hitsCL2[c] == 0)
          {
            this.hitsCL2[c] = tmpList.Count;
          }
          else
          {
            this.hitsCL2[c] += tmpList.Count;
          }

          if (tmpList.Count > 2)
          {
            this.maf2ClosedLoop[c] = (double)tmpList.Average();
          }
          else
          {
            this.maf2ClosedLoop[c] = 100;
          }
        }
      }

      // Single MAF
      else
      {
        // read values from DataTable
        for (int c = 0; c < this.clDT1.Columns.Count - 1; ++c)
        {
          List<double> tmpList = new List<double>();
          for (int line = 0; line < this.clDT1.Rows.Count - 1; ++line)
          {
            double cell = Convert.ToDouble(this.clDT1.Rows[line][c]);
            if (cell != 1.1)
            {
              tmpList.Add(Convert.ToDouble(this.clDT1.Rows[line][c]));
            }
            else
            {
              break;
            }
          }

          // Shows how many hits were for each voltage
          if (this.hitsCL1[c] == 0)
          {
            this.hitsCL1[c] = tmpList.Count;
          }
          else
          {
            this.hitsCL1[c] += tmpList.Count;
          }

          if (tmpList.Count > 2)
          {
            this.maf1ClosedLoop[c] = (double)tmpList.Average();
          }
          else
          {
            this.maf1ClosedLoop[c] = 100;
          }
        }
      }
    }

    private void OpenLoop_Start()
    {
      // this.dualTB = this.dualTB && IndexFinder.MafB2Dex != -1 ? true : false;

      // MAF 1 - write values to datatable
      for (int i = 0; ;)
      {
        double cell1 = Convert.ToDouble(this.olDT1.Rows[i][this.indexFinder1]);

        if (this.actualAFR1 != 0)
        {
          this.finaltrim1 = (this.actualAFR1 / this.target) * 100;
        }

        // Add extra row if close to the end
        if (i == this.olDT1.Rows.Count - 1)//|| this.olDT1.Rows.Count == 0)
        {
          DataRow dr = this.olDT1.NewRow();
          int c = 0;
          foreach (double d in mafVolts)
          {
            dr[c] = 1.1;
            ++c;
          }

          this.olDT1.Rows.Add(dr);
        }

        if (cell1 == 1.1 && this.actualAFR1 != 0 && this.indexFinder1 >= 0 && this.indexFinder1 < mafVolts.Count)
        {
          this.olDT1.Rows[i][this.indexFinder1] = this.finaltrim1;
          break;
        }
        else
        {
          ++i;
        }
      }

      if (!IndexFinder.DualTB)
      {
        return;
      }

      // MAF 2 - write values to datatable
      if (this.actualAFR2 != 0)
      {
        this.finaltrim2 = (this.actualAFR2 / this.target) * 100;
      }

      for (int i = 0; ;)
      {
        double cell2 = Convert.ToDouble(this.olDT2.Rows[i][this.indexFinder2]);

        // Add extra row if close to the end
        if (i == this.olDT2.Rows.Count - 1)// || this.olDT2.Rows.Count == 0)
        {
          DataRow dr = this.olDT2.NewRow();
          int c = 0;
          foreach (double d in mafVolts)
          {
            dr[c] = 1.1;
            ++c;
          }

          this.olDT2.Rows.Add(dr);
        }

        if (cell2 == 1.1 && this.actualAFR2 != 0 && this.indexFinder1 >= 0 && this.indexFinder1 < mafVolts.Count)
        {
          this.olDT2.Rows[i][this.indexFinder2] = this.finaltrim2;
          break;
        }
        else
        {
          ++i;
        }
      }
    }

    private void OpenLoop_Finish()
    {
      // MAF1 - Read values from datatable
      for (int c = 0; c < this.olDT1.Columns.Count - 1; ++c)
      {
        // Read values from DataTable
        List<double> tmpList = new List<double>();
        for (int line = 0; line < this.olDT1.Rows.Count - 1; ++line)
        {
          double cell = Convert.ToDouble(this.olDT1.Rows[line][c]);
          if (cell != 1.1)
          {
            tmpList.Add(Convert.ToDouble(this.olDT1.Rows[line][c]));
          }
          else
          {
            break;
          }
        }

        // Shows how many hits were for each voltage
        if (this.hitsCL1[c] == 0)
        {
          this.hitsCL1[c] = tmpList.Count;
        }
        else
        {
          this.hitsCL1[c] += tmpList.Count;
        }

        if (tmpList.Count > 2)
        {
          this.maf1OpenLoop[c] = (double)tmpList.Average();
        }
        else
        {
          this.maf1OpenLoop[c] = 100;
        }
      }

      if (!IndexFinder.DualTB)
      {
        return;
      }

      // MAF2 - Read vales from datatable
      for (int c = 0; c < this.olDT2.Columns.Count - 1; ++c)
      {
        // Read values from DataTable 2
        List<double> tmpList = new List<double>();
        for (int line = 0; line < this.olDT2.Rows.Count - 1; ++line)
        {
          double cell2 = Convert.ToDouble(this.olDT2.Rows[line][c]);
          if (cell2 != 1.1)
          {
            tmpList.Add(Convert.ToDouble(this.olDT2.Rows[line][c]));
          }
          else
          {
            break;
          }
        }

        // Shows how many hits were for each voltage
        if (this.hitsCL2[c] == 0)
        {
          this.hitsCL2[c] = tmpList.Count;
        }
        else
        {
          this.hitsCL2[c] += tmpList.Count;
        }

        if (tmpList.Count > 2)
        {
          this.maf2OpenLoop[c] = (double)tmpList.Average();
        }
        else
        {
          this.maf2OpenLoop[c] = 100;
        }
      }
    }

    private void SetConfig()
    {
      if (Properties.Settings.Default.MAF_CL
      && !Properties.Settings.Default.MAF_IAT
      && !Properties.Settings.Default.MAF_ACCEL
      && IndexFinder.TargetDex != -1 && IndexFinder.MafB1Dex != -1
      && IndexFinder.AfrB1Dex != -1 && IndexFinder.AfrB2Dex != -1
      && IndexFinder.CoolantTempDex != -1)
      {
        this.clStatus = "CL_Basic";
      }
      else if (Properties.Settings.Default.MAF_CL
      && Properties.Settings.Default.MAF_IAT
      && !Properties.Settings.Default.MAF_ACCEL
      && IndexFinder.TargetDex != -1 && IndexFinder.MafB1Dex != -1
      && IndexFinder.AfrB1Dex != -1 && IndexFinder.AfrB2Dex != -1
      && IndexFinder.CoolantTempDex != -1
      && IndexFinder.IntakeAirTempDex != -1)
      {
        this.clStatus = "CL_IAT";
      }
      else if (Properties.Settings.Default.MAF_CL
      && !Properties.Settings.Default.MAF_IAT
      && Properties.Settings.Default.MAF_ACCEL
      && IndexFinder.TargetDex != -1 && IndexFinder.MafB1Dex != -1
      && IndexFinder.AfrB1Dex != -1 && IndexFinder.AfrB2Dex != -1
      && IndexFinder.CoolantTempDex != -1
      && IndexFinder.AccelDex != -1)
      {
        this.clStatus = "CL_ACCEL";
      }
      else if (Properties.Settings.Default.MAF_CL
      && Properties.Settings.Default.MAF_IAT
      && Properties.Settings.Default.MAF_ACCEL
      && IndexFinder.TargetDex != -1 && IndexFinder.MafB1Dex != -1
      && IndexFinder.AfrB1Dex != -1 && IndexFinder.AfrB2Dex != -1
      && IndexFinder.CoolantTempDex != -1
      && IndexFinder.IntakeAirTempDex != -1
      && IndexFinder.AccelDex != -1)
      {
        this.clStatus = "CL_Full";
      }
      else
      {
        this.clStatus = "Error";
      }

      if (Properties.Settings.Default.MAF_OL
      && IndexFinder.TargetDex != -1 && IndexFinder.MafB1Dex != -1
      && IndexFinder.AfrB1Dex != -1 && IndexFinder.AfrB2Dex != -1
      && IndexFinder.CoolantTempDex != -1)
      {
        this.olStatus = "OL_Full";
      }
      else
      {
        this.olStatus = "Error";
      }
    }

    private void BuildDT()
    {
      // Build first line for the adjustment CL DataTable
      if (this.clDT1.Rows.Count == 0)
      {
        DataRow dr = this.clDT1.NewRow();
        int c = 0;
        foreach (double d in mafVolts)
        {
          dr[c] = 1.1;
          ++c;
        }

        this.clDT1.Rows.Add(dr);
      }

      if (IndexFinder.DualTB)
      {
        // Build first line for the adjustment CL DataTable
        if (this.clDT2.Rows.Count == 0)
        {
          DataRow dr = this.clDT2.NewRow();
          int c = 0;
          foreach (double d in mafVolts)
          {
            dr[c] = 1.1;
            ++c;
          }

          this.clDT2.Rows.Add(dr);
        }
      }

      // Build first line for the adjustment OL DataTable
      if (this.olDT1.Rows.Count == 0)
      {
        DataRow dr = this.olDT1.NewRow();
        int c = 0;
        foreach (double d in mafVolts)
        {
          dr[c] = 1.1;
          ++c;
        }

        this.olDT1.Rows.Add(dr);
      }

      if (IndexFinder.DualTB)
      {
        // Build first line for the adjustment OL DataTable
        if (this.olDT2.Rows.Count == 0)
        {
          DataRow dr = this.olDT2.NewRow();
          int c = 0;
          foreach (double d in mafVolts)
          {
            dr[c] = 1.1;
            ++c;
          }

          this.olDT2.Rows.Add(dr);
        }
      }
    }

    private void FindIAT_average(DataGridView tempgrid)
    {
      List<double> iatFull = new List<double>();

      for (int r = AutoTune.Lineforheaders + 2; r < tempgrid.Rows.Count - 1; ++r)
      {
        try
        {
          iatFull.Add(Convert.ToDouble(tempgrid.Rows[r].Cells[IndexFinder.IntakeAirTempDex].Value));
        }
        catch { }
      }

      this.intakeAirTempAVG = (double)iatFull.Average();
    }
  }
}
