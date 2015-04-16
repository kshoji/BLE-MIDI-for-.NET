using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Input Device
    /// </summary>
    public partial class MidiInputDevice
    {
        private readonly GattCharacteristic midiInputCharacteristic;
        private readonly DeviceInformation deviceInformation;

        /// <summary>
        /// Private constructor
        /// </summary>
        private MidiInputDevice(GattCharacteristic characteristic, DeviceInformation deviceInformation)
        {
            midiInputCharacteristic = characteristic;
            this.deviceInformation = deviceInformation;
            midiInputCharacteristic.ValueChanged += OnCharacteristicValueChanged;
        }

        /// <summary>
        /// Obtains the list of MidiInputDevice
        /// </summary>
        /// <returns>List of MidiInputDevice, empty list if doesn't found</returns>
        public static async Task<IReadOnlyList<MidiInputDevice>> GetInstances()
        {
            var result = new List<MidiInputDevice>();

            IReadOnlyDictionary<GattDeviceService, DeviceInformation> midiServices = await BleMidiDeviceUtils.GetMidiServices();
            if (midiServices == null || midiServices.Count < 1)
            {
                return new ReadOnlyCollection<MidiInputDevice>(result);
            }

            foreach (var midiService in midiServices)
            {
                var midiInputCharacteristics = BleMidiDeviceUtils.GetMidiInputCharacteristics(midiService.Key);
                if (midiInputCharacteristics == null)
                {
                    continue;
                }

                foreach (var characteristic in midiInputCharacteristics)
                {
                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                    {
                        while (true)
                        {
                            try {
                                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
                                break;
                            }
                        }
                    }
                    result.Add(new MidiInputDevice(characteristic, midiService.Value));
                }
            }

            return new ReadOnlyCollection<MidiInputDevice>(result);
        }

        /// <summary>
        /// Event handler for characteristic value changed
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="args">The event arguments</param>
        protected virtual void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // FIXME event receiving order is incorrect.
            // more details of this issue, see these sites:
            // http://stackoverflow.com/questions/23848634/event-handler-handling-events-out-of-order
            // https://social.msdn.microsoft.com/Forums/windowshardware/en-US/1d2ec821-2467-4fc2-8fec-2b26f5cac6e6/gattcharacteristicvaluechanged-event-ordering-with-bluetooth-le-81?forum=wdk
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);
            ParseMidiEvent(data);
        }

        /// <summary>
        /// Obtains the device information
        /// </summary>
        /// <returns>The device information</returns>
        public DeviceInformation GetDeviceInformation()
        {
            return deviceInformation;
        }
    }

    /// <summary>
    /// Represents BLE MIDI Output Device
    /// </summary>
    public partial class MidiOutputDevice
    {
        private readonly GattCharacteristic midiOutputCharacteristic;
        private readonly DeviceInformation deviceInformation;

        /// <summary>
        /// Private constructor
        /// </summary>
        private MidiOutputDevice(GattCharacteristic characteristic, DeviceInformation deviceInformation)
        {
            midiOutputCharacteristic = characteristic;
            this.deviceInformation = deviceInformation;
        }

        /// <summary>
        /// Obtains the list of MidiOutputDevice
        /// </summary>
        /// <returns>List of MidiOutputDevice, empty list if doesn't found</returns>
        public static async Task<IReadOnlyList<MidiOutputDevice>> GetInstances()
        {
            var result = new List<MidiOutputDevice>();

            IReadOnlyDictionary<GattDeviceService, DeviceInformation> midiServices = await BleMidiDeviceUtils.GetMidiServices();
            if (midiServices == null || midiServices.Count < 1)
            {
                return new ReadOnlyCollection<MidiOutputDevice>(result);
            }

            foreach (var midiService in midiServices)
            {
                var midiOutputCharacteristics = BleMidiDeviceUtils.GetMidiOutputCharacteristics(midiService.Key);
                if (midiOutputCharacteristics == null)
                {
                    continue;
                }

                foreach (var characteristic in midiOutputCharacteristics)
                {
                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                    {
                        await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    }

                    result.Add(new MidiOutputDevice(characteristic, midiService.Value));
                }
            }

            return new ReadOnlyCollection<MidiOutputDevice>(result);
        }

        /// <summary>
        /// Obtains the device information
        /// </summary>
        /// <returns>The device information</returns>
        public DeviceInformation GetDeviceInformation() {
            return deviceInformation;
        }
    }
}
