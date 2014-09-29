using kshoji.BleMidi;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Sample_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BleMidilCentralProvider bleMidiCentralProvider = new BleMidilCentralProvider();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void SyncButtonClicked(object sender, RoutedEventArgs e)
        {
            bleMidiCentralProvider.GetMidiDeviceAttachedListener().MidiInputDeviceAttached += MainPage_MidiInputDeviceAttached;
            bleMidiCentralProvider.GetMidiDeviceAttachedListener().MidiOutputDeviceAttached += MainPage_MidiOutputDeviceAttached;
            bleMidiCentralProvider.GetMidiDeviceDetachedListener().MidiOutputDeviceDetached += MainPage_MidiOutputDeviceDetached;

            await bleMidiCentralProvider.ScanDevices();
        }

        void MainPage_MidiInputDeviceAttached(MidiInputDevice midiInputDevice)
        {
            midiInputDevice.GetEventListener().NoteOn += MainPage_NoteOn;
            midiInputDevice.GetEventListener().NoteOff += MainPage_NoteOff;
        }

        void MainPage_NoteOff(MidiInputDevice sender, int channel, int note, int velocity)
        {
            MidiInputListView.Items.Add("Note Off");
        }

        void MainPage_NoteOn(MidiInputDevice sender, int channel, int note, int velocity)
        {
            MidiInputListView.Items.Add("Note On");
        }

        void MainPage_MidiOutputDeviceAttached(MidiOutputDevice midiOutputDevice)
        {
            DevicesComboBox.Items.Add(midiOutputDevice);
        }

        void MainPage_MidiOutputDeviceDetached(MidiOutputDevice midiOutputDevice)
        {
            DevicesComboBox.Items.Remove(midiOutputDevice);
        }
    }
}
