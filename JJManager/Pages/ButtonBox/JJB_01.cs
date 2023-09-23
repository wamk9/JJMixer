﻿using AudioSwitcher.AudioApi;
using HidSharp;
using JJManager.Class;
using MaterialSkin;
using MaterialSkin.Controls;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JJManager.Pages.ButtonBox
{
    public partial class JJB_01 : MaterialForm
    {
        Joystick _Joystick = null;
        private static Class.Devices _device;
        private static Profiles _profile = null;
        private static AudioManager _audioManager = new AudioManager();
        private static DatabaseConnection _DatabaseConnection = new DatabaseConnection();
        private Thread thrTimers = null;
        private bool _DisconnectDevice = false;
        private bool _IsInputSelected = false;
        private bool _IsCreateProfileOpened = false;

        #region WinForms
        private MaterialSkinManager materialSkinManager = null;
        private AppModulesTimer JoystickReceiver = null;
        private AppModulesNotifyIcon notifyIcon = null;
        #endregion

        public JJB_01(Joystick joystick)
        {
            InitializeComponent();
            components = new System.ComponentModel.Container();

            // MaterialDesign
            materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = _DatabaseConnection.GetTheme();
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);

            if (joystick == null)
                Close();

            _Joystick = joystick;

            _device = new Class.Devices(_Joystick);

            // Fill Forms
            foreach (String Profile in Profiles.GetList(_device.Id))
                CmbBoxSelectProfile.Items.Add(Profile);

            CmbBoxSelectProfile.SelectedIndex = 0;
            _profile = new Profiles(CmbBoxSelectProfile.Items[0].ToString(), _device.Id);


            // Start NotifyIcon
            notifyIcon = new AppModulesNotifyIcon(components, _Joystick.Properties.ProductName, NotifyIcon_Click);

            //Start Timers
            JoystickReceiver = new AppModulesTimer(components, 50, TimerReceiveJoystickMessage_Tick);

            // Events
            FormClosing += new FormClosingEventHandler(JJB_01_FormClosing);
            FormClosed += new FormClosedEventHandler(JJB_01_FormClosed);
            CmbBoxSelectProfile.DropDown += new EventHandler(CmbBoxSelectProfile_DropDown);
            CmbBoxSelectProfile.SelectedIndexChanged += new EventHandler(CmbBoxSelectProfile_SelectedIndexChanged);

            // Fill Forms
            foreach (String Profile in _DatabaseConnection.GetProfiles(_device.Id))
                CmbBoxSelectProfile.Items.Add(Profile);

            CmbBoxSelectProfile.SelectedIndex = 0;

        }

        private void OpenInputModal(int idInput)
        {
            Thread thr = new Thread(() => {
                if (!_IsInputSelected)
                {
                    _IsInputSelected = true;
                    ChangeInputInfo inputForm = new ChangeInputInfo(_profile, idInput);
                    inputForm.ShowDialog();
                    _profile.UpdateInputs();
                    _IsInputSelected = false;
                }

                Thread.CurrentThread.Abort();
            });
            thr.Start();
        }


        #region Events
        private void JJB_01_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_DisconnectDevice)
            {
                e.Cancel = true;
                notifyIcon.Show();
                Visible = false;
            }
            else
            {
                JoystickReceiver.Stop();
            }
        }

        private void JJB_01_FormClosed(object sender, FormClosedEventArgs e)
        {
            Enabled = false;
            GC.Collect();
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            notifyIcon.Hide();
            Visible = true;
            BringToFront();
        }

        private void TimerReceiveJoystickMessage_Tick(object sender, EventArgs e)
        {
            int valueX = _device.GetJoystickAxisPercentage(_Joystick, "X");
            int valueY = _device.GetJoystickAxisPercentage(_Joystick, "Y");

            if (valueX == -1 || valueY == -1)
            {
                _DisconnectDevice = true;
                Close();
            }

            _audioManager.ChangeInputVolume(_profile.GetInputById(1), valueX);
            _audioManager.ChangeInputVolume(_profile.GetInputById(2), valueY);
        }

        private void CmbBoxSelectProfile_DropDown(object sender, EventArgs e)
        {
            int selectedIndex = CmbBoxSelectProfile.SelectedIndex;

            CmbBoxSelectProfile.Items.Clear();

            foreach (String Profile in Profiles.GetList(_device.Id))
                CmbBoxSelectProfile.Items.Add(Profile);

            CmbBoxSelectProfile.SelectedIndex = selectedIndex;
        }

        private void CmbBoxSelectProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CmbBoxSelectProfile.SelectedItem.ToString() != "")
            {
                _profile = new Profiles(CmbBoxSelectProfile.SelectedItem.ToString(), _device.Id);
            }
            else
            {
                CmbBoxSelectProfile.SelectedIndex = 0;
                _profile = new Profiles(CmbBoxSelectProfile.SelectedItem.ToString(), _device.Id);
            }
        }
        #endregion

        #region Buttons
        private void BtnInput01JJB01_Click(object sender, EventArgs e)
        {
            OpenInputModal(1);
        }

        private void BtnInput01JJB01_MouseEnter(object sender, EventArgs e)
        {
            if (!_IsInputSelected)
                BtnInput01JJB01.Image = JJManager.Properties.Resources.JJB_01_input01_hover;
        }

        private void BtnInput01JJB01_MouseLeave(object sender, EventArgs e)
        {
            BtnInput01JJB01.Image = JJManager.Properties.Resources.JJB_01_input01;
        }

        private void BtnInput02JJB01_Click(object sender, EventArgs e)
        {
            OpenInputModal(2);
        }

        private void BtnInput02JJB01_MouseEnter(object sender, EventArgs e)
        {
            if (!_IsInputSelected)
                BtnInput02JJB01.Image = JJManager.Properties.Resources.JJB_01_input02_hover;
        }

        private void BtnInput02JJB01_MouseLeave(object sender, EventArgs e)
        {
            BtnInput02JJB01.Image = JJManager.Properties.Resources.JJB_01_input02;
        }

        private void BtnDisconnectJJB01_Click(object sender, EventArgs e)
        {
            _DisconnectDevice = true;
            Close();
        }

        private void BtnAddProfile_Click(object sender, EventArgs e)
        {
            if (!_IsCreateProfileOpened)
            {
                Thread thr = new Thread(() => {
                    _IsCreateProfileOpened = true;
                    CreateProfile createProfile = new CreateProfile(_device);
                    createProfile.ShowDialog();
                    _IsCreateProfileOpened = false;
                    Thread.CurrentThread.Abort();
                });
                thr.Start();
            }
        }
        #endregion

        private void BtnRemoveProfile_Click(object sender, EventArgs e)
        {
            if (CmbBoxSelectProfile.SelectedIndex == -1)
            {
                MessageBox.Show("Selecione um perfil para exclui-lo.");
                return;
            }

            if (CmbBoxSelectProfile.Items.Count == 1)
            {
                MessageBox.Show("Você possui apenas um perfil e este não pode ser excluído.");
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Você está prestes a excluir o Perfil '" + CmbBoxSelectProfile.SelectedItem.ToString() + "', deseja continuar?", "Exclusão de Perfil", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                _DatabaseConnection.DeleteProfile(CmbBoxSelectProfile.SelectedItem.ToString(), _device.Id);

                CmbBoxSelectProfile.Items.Clear();

                foreach (String Profile in _DatabaseConnection.GetProfiles(_device.Id))
                    CmbBoxSelectProfile.Items.Add(Profile);

                CmbBoxSelectProfile.SelectedIndex = 0;

                MessageBox.Show("Perfil excluído com sucesso!");
            }
        }
    }
}
