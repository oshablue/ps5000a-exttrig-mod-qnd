/**************************************************************************
 * 
 * Filename: PS5000ASigGen.cs
 *
 * Description:
 *   This is a program that demonstrates how to call the PicoScope 5000 
 *   Series (ps5000a) driver API functions to control the AWG/Signal Generator. 
 *
 * Examples:
 *    Outputs signal from signal generator
 *    Loads in file and creates signal using the Arbitrary Waveform Generator
 *    
 * Copyright (C) 2016 - 2017 Pico Technology Ltd. See LICENSE file for terms.  
 *
 **************************************************************************/

using System;
using System.Windows.Forms;

using PS5000AImports;
using PicoStatus;


namespace PS5000ASigGen
{
    public partial class AWG_SIGGEN : Form
    {

        short handle = 0;
        UInt32 status;

        short minArbitraryWaveformValue;
        short maxArbitraryWaveformValue;
        uint minArbitraryWaveformSize;
        uint maxArbitraryWaveformSize;

        // Initialise view 
        public AWG_SIGGEN()
        {
            InitializeComponent();
            fileNameTextBox.Text = "Please select signal type";
            fileNameTextBox.ReadOnly = true;
            sigToAWG.Checked = false;
            sweepCheckBox.Checked = false;
            signalTypeComboBox.SelectedIndex = 0;
            sweepTypeComboBox.SelectedIndex = 0;
        }

        //opens device
        private void Start_button_Click(object sender, EventArgs e)
        {
            //opens device 
            status = Imports.OpenUnit(out handle, null, Imports.DeviceResolution.PS5000A_DR_8BIT);

            // If handle is valid but device is USB powered, then the device power status will need to be changed
            if (status == StatusCodes.PICO_POWER_SUPPLY_NOT_CONNECTED || status == StatusCodes.PICO_USB3_0_DEVICE_NON_USB3_0_PORT)
            {
                status = Imports.ChangePowerSource(handle, status);
            }
            else if (status != StatusCodes.PICO_OK)
            {
                MessageBox.Show("Cannot open device error code: " + status.ToString(), "Error Opening Device", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
            else
            {
                // Do nothing - device has DC power connected 
            }

            // Find max AWG Buffer Size

            status = Imports.SigGenArbitraryMinMaxValues(handle, out minArbitraryWaveformValue, out maxArbitraryWaveformValue, out minArbitraryWaveformSize, out maxArbitraryWaveformSize);

            if (status == StatusCodes.PICO_OK)
            {
                if (maxArbitraryWaveformSize > 0)
                {
                    sigToAWG.Enabled = true;
                    fileNameTextBox.Enabled = true;
                }
            }

            controls.Visible = true;
        }

        //changes from signal generator to abitary waveform generator
        private void SIGtoAWG_CheckedChanged(object sender, EventArgs e)
        {
            awgLabel.Visible = sigToAWG.Checked;
            awgFileInfoLabel.Visible = sigToAWG.Checked;
            signalTypeComboBox.Visible = !sigToAWG.Checked;

            if (sigToAWG.Checked)
            {
                fileNameTextBox.Clear();
                fileNameTextBox.ReadOnly = false;
            }
            else
            {
                fileNameTextBox.Text = "Please select signal type";
                fileNameTextBox.ReadOnly = true;
            }
        }

        //enables sweep controls
        private void Sweep_CheckedChanged(object sender, EventArgs e)
        {
            SweepController.Visible = sweepCheckBox.Checked;
        }

        //If dc or white noise sweep is not enable so hides button
        private void signal_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (signalTypeComboBox.SelectedIndex == 8 || signalTypeComboBox.SelectedIndex == 9)
            {
                sweepCheckBox.Checked = false;
                sweepCheckBox.Enabled = false;
            }
            else
            {
                sweepCheckBox.Enabled = true;
            }

        }


        private void Update_button_Click(object sender, EventArgs e)
        {
            Imports.SweepType sweeptype = Imports.SweepType.PS5000A_UP;
            Imports.ExtraOperations operations = Imports.ExtraOperations.PS5000A_ES_OFF;
            uint shots = 0;
            uint sweeps = 0;
            Imports.SigGenTrigType triggertype = Imports.SigGenTrigType.PS5000A_SIGGEN_RISING;
            Imports.SigGenTrigSource triggersource = Imports.SigGenTrigSource.PS5000A_SIGGEN_NONE;
            short extinthreshold = 0;
            double stopFreq;
            double startFreq;
            double increment;
            double dwellTime;
            int offset;
            uint pkToPk;


            // OBL ADDITIONS for Ext Trig for AWG or FGEN
            float voltageLevel = 2.0F;
            float maxExternalVoltage = 5.0F; // Datasheet for PS5000A/B suggests external trigger input is +/- 5VDC-coupled input
            //Imports.SigGenTrigSource trigSrc = Imports.SigGenTrigSource.PS5000A_SIGGEN_EXT_IN; // also available is SCOPE for a scope channel

            // C header file shows the max ext trig value at 32767 (+/-) but it is not otherwise made available (yet) here
            // so:
            int thresholdLevel = (int)((voltageLevel / maxExternalVoltage) * 32767.0);


            try
            {
                startFreq = Convert.ToDouble(startFrequencyTextBox.Text);
                pkToPk = Convert.ToUInt32(peatkToPeakVoltageTextBox.Text) * 1000;
                offset = Convert.ToInt32(offsetVoltageTextBox.Text) * 1000;
            }
            catch
            {
                MessageBox.Show("Error with start frequency, offset and/or pktopk", "INVALID VALUES", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (sweepCheckBox.Checked)
            {
                try
                {
                    stopFreq = Convert.ToDouble(stopFreqTextBox.Text);
                    increment = Convert.ToDouble(frequencyIncrementTextBox.Text);
                    dwellTime = Convert.ToDouble(timeIncrementTextBox.Text);
                    sweeptype = (Imports.SweepType)(sweepTypeComboBox.SelectedIndex);
                }
                catch
                {
                    MessageBox.Show("Sweep values are incorrect", "INCORRECT VALUES", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }
            else
            {
                stopFreq = startFreq;
                increment = 0;
                dwellTime = 0;
                sweeptype = Imports.SweepType.PS5000A_UP;
            }

            if (sigToAWG.Checked)
            {
                Imports.IndexMode indexMode = Imports.IndexMode.PS5000A_SINGLE;
                int waveformsize = 0;
                string line;
                System.IO.StreamReader file;

                short[] waveform = new short[maxArbitraryWaveformSize];

                try
                {
                    file = new System.IO.StreamReader(fileNameTextBox.Text);
                }
                catch
                {
                    MessageBox.Show("Cannot open file", "Error file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                while ((line = file.ReadLine()) != null && waveformsize < maxArbitraryWaveformSize)
                {
                    try
                    {
                        waveform[waveformsize] = Convert.ToInt16(line);
                    }
                    catch
                    {
                        if (Convert.ToInt32(line) > Int16.MaxValue)
                        {
                            waveform[waveformsize] = Int16.MaxValue;
                        }
                        else
                        {
                            waveform[waveformsize] = Int16.MinValue;
                        }

                    }
                    waveformsize++;
                }

                file.Close();


                Array.Resize(ref waveform, waveformsize);

                // As frequency depends on the number or points need to use delta phase.
                // Use the SigGenFrequencyToPhase method to calculate this.

                uint startDeltaPhase;
                uint stopDeltaPhase;
                uint deltaPhaseIncrement;
                uint dwellCount;

                
                status = Imports.SigGenFrequencyToPhase(handle, startFreq, indexMode, (uint) waveformsize, out startDeltaPhase);
                status = Imports.SigGenFrequencyToPhase(handle, stopFreq, indexMode, (uint) waveformsize, out stopDeltaPhase);
                status = Imports.SigGenFrequencyToPhase(handle, increment, indexMode, (uint) waveformsize, out deltaPhaseIncrement);
                status = Imports.SigGenFrequencyToPhase(handle, (double) (1.0 / dwellTime), indexMode, (uint) waveformsize, out dwellCount);
                
                status = Imports.SetSigGenArbitrary(handle, offset, pkToPk, startDeltaPhase, stopDeltaPhase, deltaPhaseIncrement, dwellCount, waveform, waveformsize, sweeptype,
                                                        operations, indexMode, shots, sweeps, triggertype, triggersource, extinthreshold);

                if (status != StatusCodes.PICO_OK)
                {
                    MessageBox.Show("Error SetSigGenArbitrary error code :" + status.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else
            {
                Imports.WaveType wavetype = Imports.WaveType.PS5000A_SINE;

                if (signalTypeComboBox.SelectedIndex < (int) Imports.WaveType.PS5000A_MAX_WAVE_TYPES)
                {
                    if ((wavetype = (Imports.WaveType)(signalTypeComboBox.SelectedIndex)) == Imports.WaveType.PS5000A_DC_VOLTAGE)
                    {
                        pkToPk = 0;
                    }

                }
                else
                {
                    operations = (Imports.ExtraOperations)(signalTypeComboBox.SelectedIndex - 8);
                }

                // OBL NOTES AND MODS:
                // V2 is "High precision" per A API programmer's guide (PG)
                // no V2 is standard def
                // extinthreshold is calculated at the top of this function
                // redefine some items here for our FGEN test output triggered on ext trig
                // TEST: To test for non-sweep, one cycle upon trigger, use shots = 1; sweeps = 0; OK
                // TEST: shots = 0; sweeps = 1; in theory would sweep 1 cycle but this does not work (yet)
                //   it just sweeps forever at start freq (annoyingly)
                // There are limitations here for our application of a short chirp sweep
                // May need better to use the freq to phase approac for example
                // Or yes even the AWG
                // Anyway this work around might do it:
                shots = Convert.ToUInt32(shotsTextBox.Text);
                //shots = 70; // 0; // 10000; // YES this appears to be the total number of cycles allowed within a sweep, even if this doesn't take us to the stop freq, so it needs to be calculated
                sweeps = 0; // 1; // 1; // 1; // 1 sweep per trigger // Problematic -- seems to sweep and then go forever at stop freq (!)
                // Maybe mysteriously this is a scope that does not have the ability to set the number of sweeps?
                // Maybe the freq to phase style of setup is what is required here.
                triggertype = Imports.SigGenTrigType.PS5000A_SIGGEN_RISING; //.PS5000A_SIGGEN_GATE_HIGH; //.PS5000A_SIGGEN_RISING;
                triggersource = Imports.SigGenTrigSource.PS5000A_SIGGEN_EXT_IN; // Also available is SCOPE_IN for example on PS that doesn't have a dedicated ext trig input
                extinthreshold = (short)thresholdLevel; // defined above near top of function

                //extinthreshold = 100;
                status = Imports.SetSigGenBuiltInV2(handle, offset, pkToPk, wavetype, startFreq, stopFreq, increment, dwellTime, sweeptype,
                                                        operations, shots, sweeps, triggertype, triggersource, extinthreshold);
                Console.WriteLine(status);

                if (status != StatusCodes.PICO_OK)
                {
                    MessageBox.Show("Error SetSigGenBuiltInV2 error code :" + status.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
        }

        /// <summary>
        /// When the form is closed, disconnect device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AWG_SIGGEN_FormClosing(object sender, FormClosingEventArgs e)
        {
            Imports.CloseUnit(handle);
        }

        private void startFrequencyLabel_Click(object sender, EventArgs e)
        {

        }

        private void SweepController_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
