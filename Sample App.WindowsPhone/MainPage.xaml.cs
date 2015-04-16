using kshoji.BleMidi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Sample_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BleMidilCentralProvider bleMidiCentralProvider = new BleMidilCentralProvider();
        private TaskScheduler uiContext = TaskScheduler.FromCurrentSynchronizationContext();

        public MainPage()
        {
            this.InitializeComponent();

            bleMidiCentralProvider.MidiInputDeviceAttached += MainPage_MidiInputDeviceAttached;
            bleMidiCentralProvider.MidiInputDeviceDetached += MainPage_MidiInputDeviceDetached;
            bleMidiCentralProvider.MidiOutputDeviceAttached += MainPage_MidiOutputDeviceAttached;
            bleMidiCentralProvider.MidiOutputDeviceDetached += MainPage_MidiOutputDeviceDetached;

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

    
        private bool isScanning = false;

        private void SyncButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isScanning)
                {
                    bleMidiCentralProvider.StartScanDevices();
                    isScanning = true;
                    ((Button)sender).Content = "Stop Scan";
                }
                else
                {
                    bleMidiCentralProvider.StopScanDevices();
                    isScanning = false;
                    ((Button)sender).Content = "Scan Device";
                }
            }
            catch (Exception)
            {
                // TODO process semaphore timeout exception
            }
        }

        void MainPage_MidiInputDeviceAttached(MidiInputDevice midiInputDevice)
        {
            midiInputDevice.ActiveSensing += midiInputDevice_ActiveSensing;
            midiInputDevice.ChannelAftertouch += midiInputDevice_ChannelAftertouch;
            midiInputDevice.Continue += midiInputDevice_Continue;
            midiInputDevice.ControlChange += midiInputDevice_ControlChange;
            midiInputDevice.NoteOff += MainPage_NoteOff;
            midiInputDevice.NoteOn += MainPage_NoteOn;
            midiInputDevice.NRPNMessage += midiInputDevice_NRPNMessage;
            midiInputDevice.PitchWheel += midiInputDevice_PitchWheel;
            midiInputDevice.PolyphonicAftertouch += midiInputDevice_PolyphonicAftertouch;
            midiInputDevice.ProgramChange += midiInputDevice_ProgramChange;
            midiInputDevice.Reset += midiInputDevice_Reset;
            midiInputDevice.RPNMessage += midiInputDevice_RPNMessage;
            midiInputDevice.SongPositionPointer += midiInputDevice_SongPositionPointer;
            midiInputDevice.SongSelect += midiInputDevice_SongSelect;
            midiInputDevice.Start += midiInputDevice_Start;
            midiInputDevice.Stop += midiInputDevice_Stop;
            midiInputDevice.SystemExclusive += midiInputDevice_SystemExclusive;
            midiInputDevice.TimeCodeQuarterFrame += midiInputDevice_TimeCodeQuarterFrame;
            midiInputDevice.TimingClock += midiInputDevice_TimingClock;
            midiInputDevice.TuneRequest += midiInputDevice_TuneRequest;
        }

        void MainPage_MidiInputDeviceDetached(MidiInputDevice midiInputDevice)
        {
            midiInputDevice.ActiveSensing -= midiInputDevice_ActiveSensing;
            midiInputDevice.ChannelAftertouch -= midiInputDevice_ChannelAftertouch;
            midiInputDevice.Continue -= midiInputDevice_Continue;
            midiInputDevice.ControlChange -= midiInputDevice_ControlChange;
            midiInputDevice.NoteOff -= MainPage_NoteOff;
            midiInputDevice.NoteOn -= MainPage_NoteOn;
            midiInputDevice.NRPNMessage -= midiInputDevice_NRPNMessage;
            midiInputDevice.PitchWheel -= midiInputDevice_PitchWheel;
            midiInputDevice.PolyphonicAftertouch -= midiInputDevice_PolyphonicAftertouch;
            midiInputDevice.ProgramChange -= midiInputDevice_ProgramChange;
            midiInputDevice.Reset -= midiInputDevice_Reset;
            midiInputDevice.RPNMessage -= midiInputDevice_RPNMessage;
            midiInputDevice.SongPositionPointer -= midiInputDevice_SongPositionPointer;
            midiInputDevice.SongSelect -= midiInputDevice_SongSelect;
            midiInputDevice.Start -= midiInputDevice_Start;
            midiInputDevice.Stop -= midiInputDevice_Stop;
            midiInputDevice.SystemExclusive -= midiInputDevice_SystemExclusive;
            midiInputDevice.TimeCodeQuarterFrame -= midiInputDevice_TimeCodeQuarterFrame;
            midiInputDevice.TimingClock -= midiInputDevice_TimingClock;
            midiInputDevice.TuneRequest -= midiInputDevice_TuneRequest;
        }

        void midiInputDevice_ActiveSensing(MidiInputDevice sender)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Active Sensing from {0}", sender.GetDeviceInformation().Name));
            }, uiContext);
        }

        void midiInputDevice_ChannelAftertouch(MidiInputDevice sender, int channel, int pressure)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Channel Aftertouch from {0}, channel:{1}, pressure:{2}", sender.GetDeviceInformation().Name, channel, pressure));
            }, uiContext);
        }

        void midiInputDevice_Continue(MidiInputDevice sender)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Continue from {0}", sender.GetDeviceInformation().Name));
            }, uiContext);
        }

        void midiInputDevice_ControlChange(MidiInputDevice sender, int channel, int function, int value)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Control Change from {0}, channel:{1}, function:{2}, value:{3}", sender.GetDeviceInformation().Name, channel, function, value));
            }, uiContext);
        }

        void midiInputDevice_NRPNMessage(MidiInputDevice sender, int channel, int function, int value)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("NRPN Message from {0}, channel:{1}, function:{2}, value:{3}", sender.GetDeviceInformation().Name, channel, function, value));
            }, uiContext);
        }

        void MainPage_NoteOff(MidiInputDevice sender, int channel, int note, int velocity)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Note Off from {0}, channel:{1}, note:{2}, velocity:{3}", sender.GetDeviceInformation().Name, channel, note, velocity));

                MidiInputListView.SelectedIndex = MidiInputListView.Items.Count - 1;
                MidiInputListView.UpdateLayout();
                MidiInputListView.ScrollIntoView(MidiInputListView.SelectedItem);
            }, uiContext);
        }

        void MainPage_NoteOn(MidiInputDevice sender, int channel, int note, int velocity)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Note On from {0}, channel:{1}, note:{2}, velocity:{3}", sender.GetDeviceInformation().Name, channel, note, velocity));

                MidiInputListView.SelectedIndex = MidiInputListView.Items.Count - 1;
                MidiInputListView.UpdateLayout();
                MidiInputListView.ScrollIntoView(MidiInputListView.SelectedItem);
            }, uiContext);
        }

        void midiInputDevice_PitchWheel(MidiInputDevice sender, int channel, int amount)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Pitch Wheel from {0}, channel:{1}, amount:{2}", sender.GetDeviceInformation().Name, channel, amount));
            }, uiContext);
        }

        void midiInputDevice_PolyphonicAftertouch(MidiInputDevice sender, int channel, int note, int pressure)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Polyphonic Aftertouch from {0}, channel:{1}, note:{2}, pressure:{3}", sender.GetDeviceInformation().Name, channel, note, pressure));
            }, uiContext);
        }

        void midiInputDevice_ProgramChange(MidiInputDevice sender, int channel, int program)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Program Change from {0}, channel:{1}, program:{2}", sender.GetDeviceInformation().Name, channel, program));
            }, uiContext);
        }

        void midiInputDevice_Reset(MidiInputDevice sender)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Reset from {0}", sender.GetDeviceInformation().Name));
            }, uiContext);
        }

        void midiInputDevice_RPNMessage(MidiInputDevice sender, int channel, int function, int value)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("RPN Message from {0}, channel:{1}, function:{2}, value:{3}", sender.GetDeviceInformation().Name, channel, function, value));
            }, uiContext);
        }

        void midiInputDevice_SongPositionPointer(MidiInputDevice sender, int position)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Song Position Pointer from {0}, position:{1}", sender.GetDeviceInformation().Name, position));
            }, uiContext);
        }

        void midiInputDevice_SongSelect(MidiInputDevice sender, int song)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Song Select from {0}, song:{1}", sender.GetDeviceInformation().Name, song));
            }, uiContext);
        }

        void midiInputDevice_Start(MidiInputDevice sender)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Start from {0}", sender.GetDeviceInformation().Name));
            }, uiContext);
        }

        void midiInputDevice_Stop(MidiInputDevice sender)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Stop from {0}", sender.GetDeviceInformation().Name));
            }, uiContext);
        }

        void midiInputDevice_SystemExclusive(MidiInputDevice sender, byte[] systemExclusive)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("System Exclusive from {0}, data:{1}", sender.GetDeviceInformation().Name, BitConverter.ToString(systemExclusive).Replace("-", " ")));
            }, uiContext);
        }

        void midiInputDevice_TimeCodeQuarterFrame(MidiInputDevice sender, int timing)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Time Code Quarter Frame from {0}, timing:{1}", sender.GetDeviceInformation().Name, timing));
            }, uiContext);
        }

        void midiInputDevice_TimingClock(MidiInputDevice sender)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Timing Clock from {0}", sender.GetDeviceInformation().Name));
            }, uiContext);
        }

        void midiInputDevice_TuneRequest(MidiInputDevice sender)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                MidiInputListView.Items.Add(String.Format("Tune Request from {0}", sender.GetDeviceInformation().Name));
            }, uiContext);
        }

        void MainPage_MidiOutputDeviceAttached(MidiOutputDevice midiOutputDevice)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                DevicesComboBox.Items.Add(midiOutputDevice);
            }, uiContext);
        }

        void MainPage_MidiOutputDeviceDetached(MidiOutputDevice midiOutputDevice)
        {
            Task.Run(() => { }).ContinueWith((task) =>
            {
                DevicesComboBox.Items.Remove(midiOutputDevice);
            }, uiContext);
        }
    }
}
