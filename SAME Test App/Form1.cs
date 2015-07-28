using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using EASEncoder.Models;
using EASEncoder.Models.SAME;
using NAudio.Wave;

namespace SAME_Test_App
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

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var bindingList = new BindingList<SAMERegion>(Regions);
            var source = new BindingSource(bindingList, null);
            datagridRegions.DataSource = source;

            dateStart.ShowUpDown = true;
            dateStart.CustomFormat = "MM/dd/yyyy hh:mm";
            dateStart.Format = DateTimePickerFormat.Custom;
            foreach (var thisState in MessageRegions.States)
            {
                comboState.Items.Add(thisState.Name);
            }

            foreach (var thisCode in MessageTypes.AlertCodes)
            {
                comboCode.Items.Add(thisCode.Name);
            }

            foreach (var thisOriginator in MessageTypes.Originators)
            {
                comboOriginator.Items.Add(thisOriginator.Name);
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
            _start = dateStart.Value.ToUniversalTime();
            _senderId = txtSender.Text;
            _length = txtLength.Text;

            var newMessage = new EASMessage(_selectedOriginator.Id, _selectedAlertCode.Id,
                Regions, _length, _start, _senderId);

            EASEncoder.EASEncoder.CreateNewMessage(newMessage, chkEbsTones.Checked, chkNwsTone.Checked,
                txtAnnouncement.Text);
        }

        private void txtLength_TextChanged(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (player != null)
            {
                player.Stop();
                return;
            }
            _start = dateStart.Value.ToUniversalTime();
            _senderId = txtSender.Text;
            _length = txtLength.Text;

            var newMessage = new EASMessage(_selectedOriginator.Id, _selectedAlertCode.Id,
                Regions, _length, _start, _senderId);


            var messageStream = EASEncoder.EASEncoder.GetMemoryStreamFromNewMessage(newMessage, chkEbsTones.Checked,
                chkNwsTone.Checked, txtAnnouncement.Text);
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
            if (comboState.SelectedIndex >= 0 && comboCounty.SelectedIndex >= 0)
            {
                Regions.Add(new SAMERegion(_selectedState, _selectedCounty));
                var bindingList = new BindingList<SAMERegion>(Regions);
                var source = new BindingSource(bindingList, null);
                datagridRegions.AutoGenerateColumns = true;
                datagridRegions.DataSource = null;
                datagridRegions.DataSource = source;
                datagridRegions.Refresh();
            }
        }
    }
}