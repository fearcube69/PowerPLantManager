using System;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Windows.Forms;

namespace PowerPlantManager
{
    public class Form1 : Form
    {
        private NumericUpDown numMinState = null!;
        private NumericUpDown numMaxState = null!;
        private Button btnPresetPerformance = null!;
        private Button btnPresetBalanced = null!;
        private Button btnPresetEco = null!;
        private Button btnApply = null!;
        private Button btnRefresh = null!;
        
        // Monitoring Controls
        private Label lblClockSpeed = null!;
        private Label lblTemperature = null!;
        private System.Windows.Forms.Timer timerMonitor = null!;
        private ToolTip toolTip = null!;

        private uint maxBaseClockMhz = 0;

        public Form1()
        {
            SetupCustomUi();
            LoadCurrentSettings();
            
            // Get base CPU max frequency for accurate speed calculations
            FetchMaxBaseClock();

            // Initialize 1-second timer for live stats
            timerMonitor = new System.Windows.Forms.Timer { Interval = 1000 };
            timerMonitor.Tick += (s, e) => UpdateLiveStats();
            timerMonitor.Start();

            // Initial immediate read
            UpdateLiveStats();
        }

        private void SetupCustomUi()
        {
            this.Text = "Processor Power Manager";
            this.Size = new Size(400, 390);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true
            };

            // --- Real-Time Monitoring Panel ---
            GroupBox grpMonitor = new GroupBox
            {
                Text = "Live CPU Status",
                Location = new Point(20, 10),
                Size = new Size(345, 80)
            };

            lblClockSpeed = new Label
            {
                Text = "Clock Speed: Measuring...",
                Location = new Point(15, 25),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };

            lblTemperature = new Label
            {
                Text = "CPU Temp: Measuring...",
                Location = new Point(15, 50),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };

            grpMonitor.Controls.Add(lblClockSpeed);
            grpMonitor.Controls.Add(lblTemperature);

            // --- Power State Inputs ---
            Label lblMin = new Label { Text = "Min Processor State (%):", Location = new Point(20, 110), AutoSize = true };
            numMinState = new NumericUpDown { Location = new Point(240, 108), Width = 105, Minimum = 0, Maximum = 100 };
            toolTip.SetToolTip(numMinState, "Minimum CPU percentage when idling.");

            Label lblMax = new Label { Text = "Max Processor State (%):", Location = new Point(20, 150), AutoSize = true };
            numMaxState = new NumericUpDown { Location = new Point(240, 148), Width = 105, Minimum = 0, Maximum = 100 };
            toolTip.SetToolTip(numMaxState, "Maximum CPU state. Setting to 99% disables CPU Turbo Boost.");

            // --- Preset Buttons ---
            Label lblPresets = new Label { Text = "Presets:", Location = new Point(20, 195), AutoSize = true };

            btnPresetPerformance = new Button { Text = "Max (100/100)", Location = new Point(20, 218), Width = 105, Height = 30 };
            btnPresetBalanced = new Button { Text = "Boost Off (5/99)", Location = new Point(130, 218), Width = 115, Height = 30 };
            btnPresetEco = new Button { Text = "Eco (5/50)", Location = new Point(250, 218), Width = 95, Height = 30 };

            toolTip.SetToolTip(btnPresetPerformance, "Max Performance (100%/100%):\nFull CPU speed with active Turbo Boost.");
            toolTip.SetToolTip(btnPresetBalanced, "Boost Off / Balanced (5%/99%):\nCaps Max state to 99% to completely disable CPU Turbo Boost.\nKeeps high base performance while drastically cutting heat and fan noise.");
            toolTip.SetToolTip(btnPresetEco, "Eco Mode (5%/50%):\nLimits CPU frequency to 50% for high thermal efficiency and battery saving.");

            btnPresetPerformance.Click += (s, e) => { numMinState.Value = 100; numMaxState.Value = 100; };
            btnPresetBalanced.Click += (s, e) => { numMinState.Value = 5; numMaxState.Value = 99; };
            btnPresetEco.Click += (s, e) => { numMinState.Value = 5; numMaxState.Value = 50; };

            // --- Action Buttons ---
            btnRefresh = new Button { Text = "Reload Values", Location = new Point(20, 275), Width = 155, Height = 35 };
            btnApply = new Button { Text = "Apply Changes", Location = new Point(190, 275), Width = 155, Height = 35, Font = new Font(this.Font, FontStyle.Bold) };

            btnRefresh.Click += (s, e) => LoadCurrentSettings();
            btnApply.Click += BtnApply_Click;

            // Add controls
            this.Controls.Add(grpMonitor);
            this.Controls.Add(lblMin);
            this.Controls.Add(numMinState);
            this.Controls.Add(lblMax);
            this.Controls.Add(numMaxState);
            this.Controls.Add(lblPresets);
            this.Controls.Add(btnPresetPerformance);
            this.Controls.Add(btnPresetBalanced);
            this.Controls.Add(btnPresetEco);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnApply);
        }

        // --- Live Monitoring Logic ---

        private void FetchMaxBaseClock()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        maxBaseClockMhz = Convert.ToUInt32(obj["MaxClockSpeed"]);
                        break;
                    }
                }
            }
            catch { maxBaseClockMhz = 3000; } // Fallback
        }

        private void UpdateLiveStats()
        {
            // 1. Get Real-time Clock Speed
            double currentGhz = GetCurrentClockSpeedGhz();
            if (currentGhz > 0)
            {
                lblClockSpeed.Text = $"Clock Speed: {currentGhz:F2} GHz";
                lblClockSpeed.ForeColor = currentGhz < (maxBaseClockMhz * 0.99 / 1000.0) ? Color.DarkGreen : Color.DarkRed;
            }
            else
            {
                lblClockSpeed.Text = "Clock Speed: N/A";
            }

            // 2. Get Real-time CPU Temperature
            double tempC = GetCpuTemperatureC();
            if (!double.IsNaN(tempC) && tempC > 0)
            {
                lblTemperature.Text = $"CPU Temp: {tempC:F1} °C";
                lblTemperature.ForeColor = tempC > 75 ? Color.Red : (tempC > 55 ? Color.DarkOrange : Color.DarkGreen);
            }
            else
            {
                lblTemperature.Text = "CPU Temp: N/A (Requires ACPI/Motherboard WMI)";
                lblTemperature.ForeColor = Color.Gray;
            }
        }

        private double GetCurrentClockSpeedGhz()
        {
            try
            {
                // Queries formatted performance counters for live clock percentage
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "root\\CIMV2", 
                    "SELECT PercentProcessorPerformance FROM Win32_PerfFormattedData_Counters_ProcessorInformation WHERE Name='0,_Total' OR Name='_Total'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong perfPercent = Convert.ToUInt64(obj["PercentProcessorPerformance"]);
                        double ghz = (maxBaseClockMhz * (perfPercent / 100.0)) / 1000.0;
                        return ghz;
                    }
                }
            }
            catch { }
            return 0.0;
        }

        private double GetCpuTemperatureC()
        {
            try
            {
                // Queries standard Windows ACPI Thermal Zone
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        double tempTenthsKelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                        double tempCelsius = (tempTenthsKelvin - 2732.0) / 10.0;
                        return tempCelsius;
                    }
                }
            }
            catch { }
            return double.NaN;
        }

        // --- Settings Management & Apply ---

        private void LoadCurrentSettings()
        {
            int minVal = ReadPowerSetting("PROCTHROTTLEMIN");
            int maxVal = ReadPowerSetting("PROCTHROTTLEMAX");

            numMinState.Value = Math.Clamp(minVal, 0, 100);
            numMaxState.Value = Math.Clamp(maxVal, 0, 100);
        }

        private int ReadPowerSetting(string settingAlias)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = $"/query SCHEME_CURRENT SUB_PROCESSOR {settingAlias}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (Process? p = Process.Start(psi))
                {
                    if (p == null) return 100;
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    string key = "Current AC Power Setting Index: 0x";
                    int idx = output.IndexOf(key);
                    if (idx != -1)
                    {
                        string hex = output.Substring(idx + key.Length, 8);
                        return Convert.ToInt32(hex, 16);
                    }
                }
            }
            catch { }
            return 100;
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            int min = (int)numMinState.Value;
            int max = (int)numMaxState.Value;

            if (min > max)
            {
                MessageBox.Show("Minimum processor state cannot be greater than Maximum processor state.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cmdArgs = $"/c powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN {min} " +
                             $"&& powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN {min} " +
                             $"&& powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX {max} " +
                             $"&& powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX {max} " +
                             $"&& powercfg /setactive SCHEME_CURRENT";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArgs,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process? process = Process.Start(psi))
                {
                    process?.WaitForExit();
                }

                // Force immediate live refresh after applying
                UpdateLiveStats();

                MessageBox.Show("Processor power states updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to apply changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}