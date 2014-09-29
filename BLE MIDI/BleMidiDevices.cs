using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Input Device
    /// </summary>
    public partial class MidiInputDevice
    {
        private GattCharacteristic midiInputCharacteristic;
        private readonly MidiParser midiParser;

        /// <summary>
        /// Private constructor
        /// </summary>
        private MidiInputDevice(GattCharacteristic characteristic)
        {
            midiParser = new MidiParser(this);
            midiInputCharacteristic = characteristic;
            midiInputCharacteristic.ValueChanged += OnCharacteristicValueChanged;
        }

        /// <summary>
        /// Obtains the list of MidiInputDevice
        /// </summary>
        /// <returns>List of MidiInputDevice, empty list if doesn't found</returns>
        public static async Task<IReadOnlyList<MidiInputDevice>> GetInstances()
        {
            var result = new List<MidiInputDevice>();

            IReadOnlyList<GattDeviceService> midiServices = await BleMidiDeviceUtils.GetMidiServices();
            if (midiServices == null || midiServices.Count < 1)
            {
                return new ReadOnlyCollection<MidiInputDevice>(result);
            }

            foreach (var midiService in midiServices)
            {
                var midiInputCharacteristics = BleMidiDeviceUtils.GetMidiInputCharacteristics(midiService);
                if (midiInputCharacteristics == null)
                {
                    continue;
                }

                foreach (var characteristic in midiInputCharacteristics)
                {
                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                    {
                        await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    }
                    System.Diagnostics.Debug.WriteLine("service id:" + (await BleMidiDeviceUtils.GetBleDeviceName(midiService)));
                    result.Add(new MidiInputDevice(characteristic));
                }
            }

            return new ReadOnlyCollection<MidiInputDevice>(result);
        }

        /// <summary>
        /// Event handler for characteristic value changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);
            midiParser.parse(data);
        }

        /// <summary>
        /// Obtains an event litener for the MIDI events
        /// </summary>
        /// <param name="midiInputEventListener"></param>
        public OnMidiInputEventListener GetEventListener()
        {
            return midiParser.GetMidiInputEventListener();
        }
    }

    /// <summary>
    /// Represents BLE MIDI Output Device
    /// </summary>
    public partial class MidiOutputDevice
    {
        private GattCharacteristic midiOutputCharacteristic;

        /// <summary>
        /// Private constructor
        /// </summary>
        private MidiOutputDevice(GattCharacteristic characteristic)
        {
            midiOutputCharacteristic = characteristic;
        }

        /// <summary>
        /// Obtains the list of MidiOutputDevice
        /// </summary>
        /// <returns>List of MidiOutputDevice, empty list if doesn't found</returns>
        public static async Task<IReadOnlyList<MidiOutputDevice>> GetInstances()
        {
            var result = new List<MidiOutputDevice>();

            IReadOnlyList<GattDeviceService> midiServices = await BleMidiDeviceUtils.GetMidiServices();
            if (midiServices == null || midiServices.Count < 1)
            {
                return new ReadOnlyCollection<MidiOutputDevice>(result);
            }

            foreach (var midiService in midiServices)
            {
                var midiOutputCharacteristics = BleMidiDeviceUtils.GetMidiOutputCharacteristics(midiService);
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

                    result.Add(new MidiOutputDevice(characteristic));
                }
            }

            return new ReadOnlyCollection<MidiOutputDevice>(result);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MidiOutputDevice()
        {
            // Do nothing
        }

        /**
         * Obtains the device name
         *
         * @return device name + ".output"
         */
        public String GetDeviceName() {
            return midiOutputCharacteristic.UserDescription + ".output";
        }

        //        public String toString() {
        //            return getDeviceName();
        //        }


    }
}
