using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Class for obtaining BLE MIDI devices
    /// </summary>
    public class BleMidilCentralProvider
    {
        private List<MidiInputDevice> midiInputDevices = new List<MidiInputDevice>();
        private List<MidiOutputDevice> midiOutputDevices = new List<MidiOutputDevice>();
        private readonly OnMidiDeviceAttachedListener onMidiDeviceAttachedListener = new OnMidiDeviceAttachedListener();
        private readonly OnMidiDeviceDetachedListener onMidiDeviceDetachedListener = new OnMidiDeviceDetachedListener();

        /// <summary>
        /// Check for the device connection change
        /// </summary>
        /// <returns></returns>
        public async Task ScanDevices()
        {
            var midiInputDevices = await MidiInputDevice.GetInstances();
            // check for attached
            foreach (var midiInputDevice in midiInputDevices)
            {
                if (!this.midiInputDevices.Contains(midiInputDevice))
                {
                    this.midiInputDevices.Add(midiInputDevice);
                    onMidiDeviceAttachedListener.OnMidiInputDeviceAttached(midiInputDevice);
                }
            }
            // check for detached
            foreach (var midiInputDevice in this.midiInputDevices)
            {
                if (!midiInputDevices.Contains(midiInputDevice))
                {
                    onMidiDeviceDetachedListener.OnMidiInputDeviceDetached(midiInputDevice);
                    this.midiInputDevices.Remove(midiInputDevice);
                }
            }

            var midiOutputDevices = await MidiOutputDevice.GetInstances();
            // check for attached
            foreach (var midiOutputDevice in midiOutputDevices)
            {
                if (!this.midiOutputDevices.Contains(midiOutputDevice))
                {
                    this.midiOutputDevices.Add(midiOutputDevice);
                    onMidiDeviceAttachedListener.OnMidiOutputDeviceAttached(midiOutputDevice);
                }
            }
            // check for detached
            foreach (var midiOutputDevice in this.midiOutputDevices)
            {
                if (!midiOutputDevices.Contains(midiOutputDevice))
                {
                    onMidiDeviceDetachedListener.OnMidiOutputDeviceDetached(midiOutputDevice);
                    this.midiOutputDevices.Remove(midiOutputDevice);
                }
            }
        }

        /// <summary>
        /// Obtains list of MidiInputDevice
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<MidiInputDevice> GetMidiInputDevices()
        {
            return new ReadOnlyCollection<MidiInputDevice>(midiInputDevices);
        }

        /// <summary>
        /// Obtains list of MidiOutputDevice
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<MidiOutputDevice> GetMidiOutputDevices()
        {
            return new ReadOnlyCollection<MidiOutputDevice>(midiOutputDevices);
        }

        /// <summary>
        /// Obtains OnMidiDeviceAttachedListener instance to apply the event handler
        /// </summary>
        /// <returns></returns>
        public OnMidiDeviceAttachedListener GetMidiDeviceAttachedListener()
        {
            return onMidiDeviceAttachedListener;
        }

        /// <summary>
        /// Obtains OnMidiDeviceDetachedListener instance to apply the event handler
        /// </summary>
        /// <returns></returns>
        public OnMidiDeviceDetachedListener GetMidiDeviceDetachedListener()
        {
            return onMidiDeviceDetachedListener;
        }
    }
}
