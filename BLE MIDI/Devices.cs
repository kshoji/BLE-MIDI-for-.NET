using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.Foundation;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Input Device
    /// </summary>
    public class MidiInputDevice
    {
        private GattCharacteristic midiInputCharacteristic;
        private readonly MidiParser midiParser = new MidiParser();

        /// <summary>
        /// Constructor, don't call directly
        /// </summary>
        private MidiInputDevice(GattCharacteristic characteristic)
        {
            midiInputCharacteristic = characteristic;
            midiInputCharacteristic.ValueChanged += OnCharacteristicValueChanged;
        }

        /// <summary>
        /// Initializes the instance
        /// </summary>
        /// <returns></returns>
        public static async Task<IReadOnlyList<MidiInputDevice>> GetInstances()
        {
            GattDeviceService midiService = await BleMidiDeviceUtils.GetMidiService();
            if (midiService == null)
            {
                throw new ArgumentException("MIDI GattService not found.");
            }

            var result = new List<MidiInputDevice>();
            var midiInputCharacteristics = BleMidiDeviceUtils.GetMidiInputCharacteristics(midiService);
            if (midiInputCharacteristics == null)
            {
                throw new ArgumentException("MIDI GattCharacteristic not found.");
            }

            foreach (var midiInputCharacteristic in midiInputCharacteristics)
            {
                if (midiInputCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    await midiInputCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                }

                result.Add(new MidiInputDevice(midiInputCharacteristic));
            }

            return result;
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
        /// Sets an event listener for the MIDI events
        /// </summary>
        /// <param name="midiInputEventListener"></param>
        public void SetOnMidiInputEventListener(OnMidiInputEventListener midiInputEventListener)
        {
            midiParser.SetMidiInputEventListener(midiInputEventListener);
        }

        ~MidiInputDevice()
        {
            midiParser.SetMidiInputEventListener(null);
        }
    }

    /// <summary>
    /// Represents BLE MIDI Output Device
    /// </summary>
    public class MidiOutputDevice
    {
        private GattCharacteristic midiOutputCharacteristic;

        /// <summary>
        /// Constructor, don't call directly
        /// </summary>
        private MidiOutputDevice(GattCharacteristic characteristic)
        {
            midiOutputCharacteristic = characteristic;
        }

        /// <summary>
        /// Constructs MidiOutputDevice
        /// </summary>
        /// <returns>MidiOutputDevice</returns>
        /// <exception cref="ArgumentException" >if specified gatt doesn't contain BLE MIDI service</exception>
        public static async Task<IReadOnlyList<MidiOutputDevice>> GetInstances()
        {
            GattDeviceService midiService = await BleMidiDeviceUtils.GetMidiService();
            if (midiService == null)
            {
                throw new ArgumentException("MIDI GattService not found.");
            }

            var result = new List<MidiOutputDevice>();
            var midiOutputCharacteristics = BleMidiDeviceUtils.GetMidiOutputCharacteristics(midiService);
            if (midiOutputCharacteristics == null)
            {
                throw new ArgumentException("MIDI GattCharacteristic not found.");
            }

            foreach (var midiInputCharacteristic in midiOutputCharacteristics)
            {
                if (midiInputCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    await midiInputCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                }

                result.Add(new MidiOutputDevice(midiInputCharacteristic));
            }

            return result;
        }

        /// <summary>
        /// 
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
        //        public String getDeviceName() {
        //            return bluetoothGatt.GetDevice().getName() + ".output";
        //        }

        //        public String toString() {
        //            return getDeviceName();
        //        }

        /**
         * Sends MIDI message to output device.
         *
         * @param byte1
         */
        private async void sendMidiMessage(int byte1)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[] { (byte)byte1 };

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Sends MIDI message to output device.
        /// </summary>
        /// <param name="byte1"></param>
        /// <param name="byte2"></param>
        /// <param name="byte3"></param>
        private async void sendMidiMessage(int byte1, int byte2, int byte3)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[3];

            writeBuffer[0] = (byte)byte1;
            writeBuffer[1] = (byte)byte2;
            writeBuffer[2] = (byte)byte3;

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// SysEx
        /// </summary>
        /// <param name="systemExclusive">start with 'F0', and end with 'F7'</param>
        public async void sendMidiSystemExclusive(byte[] systemExclusive)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            // split into 20 bytes. BLE can't send more than 20 bytes.
            byte[] buffer = new byte[20];
            for (int i = 0; i < systemExclusive.Length; i += 20)
            {
                if (i + 20 <= systemExclusive.Length)
                {
                    Array.Copy(systemExclusive, i, buffer, 0, 20);

                    await midiOutputCharacteristic.WriteValueAsync(buffer.AsBuffer());
                }
                else
                {
                    // last message
                    buffer = new byte[systemExclusive.Length - i];
                    Array.Copy(systemExclusive, i, buffer, 0, systemExclusive.Length - i);

                    await midiOutputCharacteristic.WriteValueAsync(buffer.AsBuffer());
                }
            }
        }

        /// <summary>
        /// Note-off
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void sendMidiNoteOff(int channel, int note, int velocity)
        {
            sendMidiMessage(0x80 | (channel & 0xf), note, velocity);
        }

        /// <summary>
        /// Note-on
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void sendMidiNoteOn(int channel, int note, int velocity)
        {
            sendMidiMessage(0x90 | (channel & 0xf), note, velocity);
        }

        /// <summary>
        /// Poly-KeyPress
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public void sendMidiPolyphonicAftertouch(int channel, int note, int pressure)
        {
            sendMidiMessage(0xa0 | (channel & 0xf), note, pressure);
        }

        /// <summary>
        /// Control Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">0-127</param>
        /// <param name="value">0-127</param>
        public void sendMidiControlChange(int channel, int function, int value)
        {
            sendMidiMessage(0xb0 | (channel & 0xf), function, value);
        }

        /// <summary>
        /// Program Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public void sendMidiProgramChange(int channel, int program)
        {
            sendMidiMessage(0xc0 | (channel & 0xf), program, 0);
        }

        /// <summary>
        /// Channel Pressure
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public void sendMidiChannelAftertouch(int channel, int pressure)
        {
            sendMidiMessage(0xd0 | (channel & 0xf), pressure, 0);
        }

        /// <summary>
        /// PitchBend Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0(low)-8192(center)-16383(high)</param>
        public void sendMidiPitchWheel(int channel, int amount)
        {
            sendMidiMessage(0xe0 | (channel & 0xf), amount & 0x7f, (amount >> 7) & 0x7f);
        }

        /// <summary>
        /// MIDI Time Code(MTC) Quarter Frame
        /// </summary>
        /// <param name="timing">0-127</param>
        public async void sendMidiTimeCodeQuarterFrame(int timing)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[2];

            writeBuffer[0] = (byte)0xf1;
            writeBuffer[1] = (byte)(timing & 0x7f);

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Song Select
        /// </summary>
        /// <param name="song">0-127</param>
        public async void sendMidiSongSelect(int song)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[2];

            writeBuffer[0] = (byte)0xf3;
            writeBuffer[1] = (byte)(song & 0x7f);

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Song Position Pointer
        /// </summary>
        /// <param name="position">0-16383</param>
        public void sendMidiSongPositionPointer(int position)
        {
            sendMidiMessage(0xf2, position & 0x7f, (position >> 7) & 0x7f);
        }

        /// <summary>
        /// Tune Request
        /// </summary>
        public void sendMidiTuneRequest()
        {
            sendMidiMessage(0xf6);
        }

        /// <summary>
        /// Timing Clock
        /// </summary>
        public void sendMidiTimingClock()
        {
            sendMidiMessage(0xf8);
        }

        /// <summary>
        /// Start Playing
        /// </summary>
        public void sendMidiStart()
        {
            sendMidiMessage(0xfa);
        }

        /// <summary>
        /// Continue Playing
        /// </summary>
        public void sendMidiContinue()
        {
            sendMidiMessage(0xfb);
        }

        /// <summary>
        /// Stop Playing
        /// </summary>
        public void sendMidiStop()
        {
            sendMidiMessage(0xfc);
        }

        /// <summary>
        /// Active Sensing
        /// </summary>
        public void sendMidiActiveSensing()
        {
            sendMidiMessage(0xfe);
        }

        /// <summary>
        /// Reset Device
        /// </summary>
        public void sendMidiReset()
        {
            sendMidiMessage(0xff);
        }

        /// <summary>
        /// RPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendRPNMessage(int channel, int function, int value)
        {
            sendRPNMessage(channel, (function >> 7) & 0x7f, function & 0x7f, value);
        }

        /// <summary>
        /// RPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendRPNMessage(int channel, int functionMSB, int functionLSB, int value)
        {
            // send the function
            sendMidiControlChange(channel, 101, functionMSB & 0x7f);
            sendMidiControlChange(channel, 100, functionLSB & 0x7f);

            // send the value
            if ((value >> 7) > 0)
            {
                sendMidiControlChange(channel, 6, (value >> 7) & 0x7f);
                sendMidiControlChange(channel, 38, value & 0x7f);
            }
            else
            {
                sendMidiControlChange(channel, 6, value & 0x7f);
            }

            // send the NULL function
            sendMidiControlChange(channel, 101, 0x7f);
            sendMidiControlChange(channel, 100, 0x7f);
        }

        /// <summary>
        /// NRPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendNRPNMessage(int channel, int function, int value)
        {
            sendNRPNMessage(channel, (function >> 7) & 0x7f, function & 0x7f, value);
        }

        /// <summary>
        /// NRPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendNRPNMessage(int channel, int functionMSB, int functionLSB, int value)
        {
            // send the function
            sendMidiControlChange(channel, 99, functionMSB & 0x7f);
            sendMidiControlChange(channel, 98, functionLSB & 0x7f);

            // send the value
            if ((value >> 7) > 0)
            {
                sendMidiControlChange(channel, 6, (value >> 7) & 0x7f);
                sendMidiControlChange(channel, 38, value & 0x7f);
            }
            else
            {
                sendMidiControlChange(channel, 6, value & 0x7f);
            }

            // send the NULL function
            sendMidiControlChange(channel, 101, 0x7f);
            sendMidiControlChange(channel, 100, 0x7f);
        }
    }
}
