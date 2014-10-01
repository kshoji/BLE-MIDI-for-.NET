using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Class for using BLE MIDI devices
    /// </summary>
    public sealed partial class BleMidilCentralProvider
    {
        private List<MidiInputDevice> midiInputDevices = new List<MidiInputDevice>();
        private List<MidiOutputDevice> midiOutputDevices = new List<MidiOutputDevice>();

        /// <summary>
        /// Check for the device connection change
        /// </summary>
        /// <returns></returns>
        public async Task ScanDevices()
        {
            // check for attached
            var midiInputDevices = await MidiInputDevice.GetInstances();
            var attachedMidiInputDevices = Array.FindAll<MidiInputDevice>(midiInputDevices.ToArray<MidiInputDevice>(), midiInputDevice => !this.midiInputDevices.Contains(midiInputDevice));
            foreach (var midiInputDevice in attachedMidiInputDevices)
            {
                if (MidiInputDeviceAttached != null)
                {
                    MidiInputDeviceAttached(midiInputDevice);
                }
            }
            this.midiInputDevices.AddRange(attachedMidiInputDevices);

            // check for detached
            Predicate<MidiInputDevice> detachedMidiInputDevicePredicate = midiInputDevice => !midiInputDevices.Contains(midiInputDevice);
            var detachedMidiInputDevices = Array.FindAll<MidiInputDevice>(this.midiInputDevices.ToArray<MidiInputDevice>(), detachedMidiInputDevicePredicate);
            foreach (var midiInputDevice in detachedMidiInputDevices)
            {
                if (MidiInputDeviceDetached != null)
                {
                    MidiInputDeviceDetached(midiInputDevice);
                }
            }
            this.midiInputDevices.RemoveAll(detachedMidiInputDevicePredicate);

            // check for attached
            var midiOutputDevices = await MidiOutputDevice.GetInstances();
            var attachedMidiOutputDevices = Array.FindAll<MidiOutputDevice>(midiOutputDevices.ToArray<MidiOutputDevice>(), midiOutputDevice => !this.midiOutputDevices.Contains(midiOutputDevice));
            foreach (var midiOutputDevice in attachedMidiOutputDevices)
            {
                if (MidiOutputDeviceAttached != null)
                {
                    MidiOutputDeviceAttached(midiOutputDevice);
                }
            }
            this.midiOutputDevices.AddRange(attachedMidiOutputDevices);

            // check for detached
            Predicate<MidiOutputDevice> detachedMidiOutputDevicePredicate = midiOutputDevice => !midiOutputDevices.Contains(midiOutputDevice);
            var detachedMidiOutputDevices = Array.FindAll<MidiOutputDevice>(this.midiOutputDevices.ToArray<MidiOutputDevice>(), detachedMidiOutputDevicePredicate);
            foreach (var midiOutputDevice in detachedMidiOutputDevices)
            {
                if (MidiOutputDeviceDetached != null)
                {
                    MidiOutputDeviceDetached(midiOutputDevice);
                }
            }
            this.midiOutputDevices.RemoveAll(detachedMidiOutputDevicePredicate);
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
    }
}
