namespace AutoTune
{
  using System;
  using System.Threading;
  using System.Windows.Forms;

  public partial class OptionForm : Form
  {
    private bool closedLoop;
    private bool openLoop;
    private bool filterAirTemp;
    private bool filterAccel;
    private bool minimalChanges;

    public OptionForm()
    {
      this.InitializeComponent();
      this.Sensitivity_value_label.Text = this.trackBar1.Value + " %";
    }

    private void Minimal_MAF_checkbox_CheckedChanged(object sender, EventArgs e)
    {
      Properties.Settings.Default.Maf_MINIMAL = this.Minimal_MAF_checkbox.Checked;
      Properties.Settings.Default.Save();
    }

    private void CheckBoxAirTemp_CheckedChanged(object sender, EventArgs e)
    {
      Properties.Settings.Default.MAF_IAT = this.checkBoxAirTemp.Checked;
      Properties.Settings.Default.Save();
    }

    private void CheckBoxAccelChange_CheckedChanged(object sender, EventArgs e)
    {
      Properties.Settings.Default.MAF_ACCEL = this.checkBoxAccelChange.Checked;
      Properties.Settings.Default.Save();
    }

    private void CheckBoxClosedLoop_CheckedChanged(object sender, EventArgs e)
    {
      Properties.Settings.Default.MAF_CL = this.checkBoxClosedLoop.Checked;
      Properties.Settings.Default.Save();
    }

    private void CheckBoxOpenLoop_CheckedChanged(object sender, EventArgs e)
    {
      Properties.Settings.Default.MAF_OL = this.checkBoxOpenLoop.Checked;
      Properties.Settings.Default.Save();
    }

    private void TrackBar1_ValueChanged(object sender, EventArgs e)
    {
      Properties.Settings.Default.MAF_Sensitivity = this.trackBar1.Value;
      Properties.Settings.Default.Save();
      this.Sensitivity_value_label.Text = this.trackBar1.Value + " %";
    }
  }
}
