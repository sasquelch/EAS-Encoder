using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using EASEncoder.Models;
using EASEncoder.Models.SAME;
using NAudio.Wave;

namespace EASEncoder_Test_App
{
    public partial class Form1 : Form
    {
        private readonly List<SAMERegion> Regions = new List<SAMERegion>();
        private string _length;
        private SAMEMessageAlertCode _selectedAlertCode;
        private SAMECounty _selectedCounty;
        private SAMEMessageOriginator _selectedOriginator;
        private SAMEState _selectedState;
        private SAMESubdivision _selectedSubdivision;
        private string _senderId;
        private DateTime _start;
        private WaveOutEvent player;
        private int _locationCount;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblOutputDirectory.Text = Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().GetName().CodeBase).Remove(0,6) + "\\Output";

            var bindingList = new BindingList<SAMERegion>(Regions);
            var source = new BindingSource(bindingList, null);
            datagridRegions.DataSource = source;

            dateStart.ShowUpDown = true;
            dateStart.CustomFormat = "MM/dd/yyyy hh:mm tt";
            dateStart.Format = DateTimePickerFormat.Custom;

            comboState.Items.AddRange(MessageRegions.States.OrderBy(x => x.Name).Select(x => x.Name).ToArray());
            comboCode.Items.AddRange(MessageTypes.AlertCodes.OrderBy(x => x.Name).Select(x => x.Name).ToArray());
            comboOriginator.Items.AddRange(MessageTypes.Originators.OrderBy(x => x.Name).Select(x => x.Name).ToArray());

            for (int x = 0; x <= 99; x++)
            {
                if (x <= 60)
                {
                    comboLengthMinutes.Items.Add(x.ToString());
                }
                comboLengthHour.Items.Add(x.ToString());
            }
        }

        private void comboState_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedState = MessageRegions.States.FirstOrDefault(x => x.Name == comboState.Text);
            if (_selectedState != null)
            {
                comboCounty.Items.Clear();
                comboCounty.Text = "";
                _selectedCounty = null;
                foreach (
                    var thisCounty in
                        MessageRegions.Counties.Where(x => x.state.Id == _selectedState.Id).OrderBy(x => x.Name))
                {
                    comboCounty.Items.Add(thisCounty.Name);
                }
            }
        }

        private void comboCounty_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedCounty =
                MessageRegions.Counties.FirstOrDefault(
                    x => x.state.Id == _selectedState.Id && x.Name == comboCounty.Text);
        }

        private void comboCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedAlertCode = MessageTypes.AlertCodes.FirstOrDefault(y => y.Name == comboCode.Text);
        }

        private void comboOriginator_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedOriginator = MessageTypes.Originators.FirstOrDefault(y => y.Name == comboOriginator.Text);
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {

            if (!ValidateInput())
            {
                return;
            }
            _start = dateStart.Value.ToUniversalTime();
            _senderId = txtSender.Text;
            _length = ZeroPad(comboLengthHour.Text, 2) + ZeroPad(comboLengthMinutes.Text, 2);

            var newMessage = new EASMessage(_selectedOriginator.Id, _selectedAlertCode.Id,
                Regions, _length, _start, _senderId);

            

            EASEncoder.EASEncoder.CreateNewMessage(newMessage, chkEbsTones.Checked, chkNwsTone.Checked,
                formatAnnouncement(txtAnnouncement.Text), txtOutputFile.Text, chkOpenFile.Checked);
        }

        internal string ZeroPad(string String, int Length)
        {
            if (string.IsNullOrEmpty(String))
            {
                String = "0";
            }
            if (String.Length > Length)
            {
                return String;
            }

            while (String.Length < Length)
            {
                String = "0" + String;
            }

            return String;
        }

        private void txtLength_TextChanged(object sender, EventArgs e)
        {
        }

        private string formatAnnouncement(string announcement)
        {
            return
                announcement.Replace("*", "")
                    .Replace("\r\n", " ")
                    .ToLower()
                    .Replace(" cdt", "central daylight time")
                    .Replace(" cst", "central standard time")
                    .Replace(" edt", "eastern daylight time")
                    .Replace(" est", "eastern standard time")
                    .Replace(" mdt", "mountain daylight time")
                    .Replace(" mst", "moutain standard time")
                    .Replace(" pdt", "pacific daylight time")
                    .Replace(" pst", "pacific standard time")
                    .Replace(" mph", "miles per hour")
                    .Replace(" winds", "whinds")
                    .Replace("  ", " ")
                    .Replace("...", ", ")
                    .Replace("precautionary/preparedness actions", "")
                    .ToLower();

        }

        private bool ValidateInput()
        {
            if (String.IsNullOrEmpty(txtSender.Text) || txtSender.TextLength != 8)
            {
                MessageBox.Show("You must enter a valid 'Sender' id.  Ensure the id is exactly 8 characters in length.", "Unable to create EAS Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (String.IsNullOrEmpty(comboOriginator.Text))
            {
                MessageBox.Show("You must select an 'Originator' from the drop down menu.", "Unable to create EAS Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (String.IsNullOrEmpty(comboCode.Text))
            {
                MessageBox.Show("You must select a 'Code' (event) from the drop down menu.", "Unable to create EAS Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (String.IsNullOrEmpty(comboLengthHour.Text))
            {
                MessageBox.Show("You must select a 'Length Hour' from the drop down menu.", "Unable to create EAS Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (String.IsNullOrEmpty(comboLengthMinutes.Text))
            {
                MessageBox.Show("You must select a 'Length Minute' from the drop down menu.", "Unable to create EAS Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (Regions.Count < 1)
            {
                MessageBox.Show("You must add at least 1 location (state/county) to the locations list.", "Unable to create EAS Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (String.IsNullOrEmpty(txtOutputFile.Text) || txtOutputFile.Text.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 || txtOutputFile.Text.Contains("\\") || txtOutputFile.Text.Contains("/"))
            {
                MessageBox.Show("You must enter a valid output file name for the EAS audio message.", "Unable to create EAS Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (player != null)
            {
                player.Stop();
                return;
            }

            if (!ValidateInput())
            {
                return;
            }

            _start = dateStart.Value.ToUniversalTime();
            _senderId = txtSender.Text;
            _length = ZeroPad(comboLengthHour.Text, 2) + ZeroPad(comboLengthMinutes.Text, 2);

            var newMessage = new EASMessage(_selectedOriginator.Id, _selectedAlertCode.Id,
                Regions, _length, _start, _senderId);


            var messageStream = EASEncoder.EASEncoder.GetMemoryStreamFromNewMessage(newMessage, chkEbsTones.Checked,
                chkNwsTone.Checked, formatAnnouncement(txtAnnouncement.Text));
            btnGeneratePlay.Text = "Stop Playing";
            WaveStream mainOutputStream = new RawSourceWaveStream(messageStream, new WaveFormat());
            var volumeStream = new WaveChannel32(mainOutputStream);
            volumeStream.PadWithZeroes = false;

            player = new WaveOutEvent();
            player.PlaybackStopped += (o, args) =>
            {
                player.Dispose();
                player = null;
                btnGeneratePlay.Text = "Generate && Play";
            };

            player.Init(volumeStream);

            player.Play();
        }

        private void btnAddRegion_Click(object sender, EventArgs e)
        { 
            if (comboState.SelectedIndex >= 0 && comboCounty.SelectedIndex >= 0 && !Regions.Exists(x => x.County.Id == _selectedCounty.Id && x.State.Id == _selectedState.Id))
            {
                Regions.Add(new SAMERegion(_selectedState, _selectedCounty));
                var bindingList = new BindingList<SAMERegion>(Regions);
                var source = new BindingSource(bindingList, null);
                datagridRegions.DataSource = source;

                comboCounty.SelectedIndex = -1;
                _selectedCounty = null;

                _locationCount += 1;
                locCountLabel.Text = "Event Locations: " + _locationCount;
                locCountLabel.ForeColor = Color.Black;
            }
        }

        private void btnClearRegions_Click(object sender, EventArgs e)
        {
            Regions.Clear();
            var bindingList = new BindingList<SAMERegion>(Regions);
            var source = new BindingSource(bindingList, null);
            datagridRegions.DataSource = source;

            _locationCount = 0;
            locCountLabel.Text = "Event Locations: " + _locationCount;
            locCountLabel.ForeColor = Color.Red;
        }

        private void txtAnnouncement_TextChanged(object sender, EventArgs e)
        {
            //parse vtec
            // (\/)(O)(.)(NEW|CON|EXT|EXA|EXB|UPG|CAN|EXP|COR|ROU)(.)([\w]{4})(.)([A-Z][A-Z])(.)([WAYSFON])(.)([0-9]{4})(.)([0-9]{6})(T)([0-9]{4})(Z)([-])([0-9]{6})([T])([0-9]{4})([Z])(\/)?
        }
    }
}