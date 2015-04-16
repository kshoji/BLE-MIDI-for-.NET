using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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

        private CancellationTokenSource scanDeviceCanceller;

        /// <summary>
        /// Starts Scanning BLE MIDI devices
        /// </summary>
        public void StartScanDevices()
        {
            if (scanDeviceCanceller != null)
            {
                // already started
                return;
            }

            scanDeviceCanceller = new CancellationTokenSource();

            // Create task to execute.
            Action action = async () => {
                try
                {
                    while (true)
                    {
                        scanDeviceCanceller.Token.ThrowIfCancellationRequested();

                        await ScanDevices();
                        await Task.Delay(1000);
                    }
                }
                catch (OperationCanceledException)
                {
                    scanDeviceCanceller.Dispose();
                    scanDeviceCanceller = null;
                }
            };

            Task.Factory.StartNew(action, scanDeviceCanceller.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Stops Scanning BLE MIDI devices
        /// </summary>
        public void StopScanDevices()
        {
            if (scanDeviceCanceller == null)
            {
                // not started
                return;
            }

            if (scanDeviceCanceller.Token.CanBeCanceled)
            {
                scanDeviceCanceller.Cancel();
            }
        }

        /// <summary>
        /// Check for the device connection change
        /// </summary>
        /// <returns>awaitable Task</returns>
        private async Task ScanDevices()
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
        /// <returns>The list of MidiInputDevice</returns>
        public IReadOnlyList<MidiInputDevice> GetMidiInputDevices()
        {
            return new ReadOnlyCollection<MidiInputDevice>(midiInputDevices);
        }

        /// <summary>
        /// Obtains list of MidiOutputDevice
        /// </summary>
        /// <returns>The list of MidiOutputDevice</returns>
        public IReadOnlyList<MidiOutputDevice> GetMidiOutputDevices()
        {
            return new ReadOnlyCollection<MidiOutputDevice>(midiOutputDevices);
        }
    }
}
