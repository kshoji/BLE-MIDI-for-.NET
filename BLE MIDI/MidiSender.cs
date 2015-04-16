using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Output Device
    /// </summary>
    public partial class MidiOutputDevice
    {
        internal const int MAX_TIMESTAMP = 8192;

        /// <summary>
        /// Sends MIDI message to output device.
        /// </summary>
        /// <param name="byte1">the first byte</param>
        private async Task SendMidiMessage(int byte1)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            int timestamp = Environment.TickCount % MAX_TIMESTAMP;
            byte[] writeBuffer = new byte[3];

            writeBuffer[0] = (byte)(0x80 | ((timestamp >> 7) & 0x3f));
            writeBuffer[1] = (byte)(0x80 | (timestamp & 0x7f));
            writeBuffer[2] = (byte)byte1;

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Sends MIDI message to output device.
        /// </summary>
        /// <param name="byte1">the first byte</param>
        /// <param name="byte2">the second byte</param>
        private async Task SendMidiMessage(int byte1, int byte2)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            int timestamp = Environment.TickCount % MAX_TIMESTAMP;
            byte[] writeBuffer = new byte[4];

            writeBuffer[0] = (byte)(0x80 | ((timestamp >> 7) & 0x3f));
            writeBuffer[1] = (byte)(0x80 | (timestamp & 0x7f));
            writeBuffer[2] = (byte)byte1;
            writeBuffer[3] = (byte)byte2;

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Sends MIDI message to output device.
        /// </summary>
        /// <param name="byte1">the first byte</param>
        /// <param name="byte2">the second byte</param>
        /// <param name="byte3">the third byte</param>
        private async Task SendMidiMessage(int byte1, int byte2, int byte3)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            int timestamp = Environment.TickCount % MAX_TIMESTAMP;
            byte[] writeBuffer = new byte[5];

            writeBuffer[0] = (byte)(0x80 | ((timestamp >> 7) & 0x3f));
            writeBuffer[1] = (byte)(0x80 | (timestamp & 0x7f));
            writeBuffer[2] = (byte)byte1;
            writeBuffer[3] = (byte)byte2;
            writeBuffer[4] = (byte)byte3;

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// SysEx
        /// </summary>
        /// <param name="systemExclusive">start with 'F0', and end with 'F7'</param>
        public async void SendMidiSystemExclusive(byte[] systemExclusive)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] timestampAddedSystemExclusive = new byte[systemExclusive.Length + 2];
            Array.Copy(systemExclusive, 0, timestampAddedSystemExclusive, 1, systemExclusive.Length);

            int timestamp = Environment.TickCount % MAX_TIMESTAMP;

            // extend a byte for timestamp LSB, before the last byte('F7')
            timestampAddedSystemExclusive[systemExclusive.Length + 1] = systemExclusive[systemExclusive.Length - 1];
            // set first byte to timestamp LSB
            timestampAddedSystemExclusive[0] = (byte)(0x80 | (timestamp & 0x7f));

            // split into 20 bytes. BLE can't send more than 20 bytes by default MTU.
            byte[] writeBuffer = new byte[20];
            for (int i = 0; i < timestampAddedSystemExclusive.Length; i += 19)
            {
                // Don't send 0xF7 timestamp LSB inside of SysEx(MIDI parser will fail) 0x7f -> 0x7e
                timestampAddedSystemExclusive[systemExclusive.Length] = (byte)(0x80 | (timestamp & 0x7e));

                if (i + 19 <= timestampAddedSystemExclusive.Length)
                {
                    Array.Copy(timestampAddedSystemExclusive, i, writeBuffer, 1, 19);
                }
                else
                {
                    // last message
                    writeBuffer = new byte[timestampAddedSystemExclusive.Length - i + 1];

                    Array.Copy(timestampAddedSystemExclusive, i, writeBuffer, 1, timestampAddedSystemExclusive.Length - i);
                }

                // timestamp MSB
                writeBuffer[0] = (byte)(0x80 | ((timestamp >> 7) & 0x3f));

                await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());

                timestamp = Environment.TickCount % MAX_TIMESTAMP;
            }
        }

        /// <summary>
        /// Note-off
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public async void SendMidiNoteOff(int channel, int note, int velocity)
        {
            await SendMidiMessage(0x80 | (channel & 0xf), note, velocity);
        }

        /// <summary>
        /// Note-on
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public async void SendMidiNoteOn(int channel, int note, int velocity)
        {
            await SendMidiMessage(0x90 | (channel & 0xf), note, velocity);
        }

        /// <summary>
        /// Poly-KeyPress
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public async void SendMidiPolyphonicAftertouch(int channel, int note, int pressure)
        {
            await SendMidiMessage(0xa0 | (channel & 0xf), note, pressure);
        }

        /// <summary>
        /// Control Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">0-127</param>
        /// <param name="value">0-127</param>
        public async void SendMidiControlChange(int channel, int function, int value)
        {
            await SendMidiMessage(0xb0 | (channel & 0xf), function, value);
        }

        /// <summary>
        /// Program Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public async void SendMidiProgramChange(int channel, int program)
        {
            await SendMidiMessage(0xc0 | (channel & 0xf), program, 0);
        }

        /// <summary>
        /// Channel Pressure
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public async void SendMidiChannelAftertouch(int channel, int pressure)
        {
            await SendMidiMessage(0xd0 | (channel & 0xf), pressure, 0);
        }

        /// <summary>
        /// PitchBend Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0(low)-8192(center)-16383(high)</param>
        public async void SendMidiPitchWheel(int channel, int amount)
        {
            await SendMidiMessage(0xe0 | (channel & 0xf), amount & 0x7f, (amount >> 7) & 0x7f);
        }

        /// <summary>
        /// MIDI Time Code(MTC) Quarter Frame
        /// </summary>
        /// <param name="timing">0-127</param>
        public async void SendMidiTimeCodeQuarterFrame(int timing)
        {
            await SendMidiMessage(0xf1, timing & 0x7f);
        }

        /// <summary>
        /// Song Select
        /// </summary>
        /// <param name="song">0-127</param>
        public async void SendMidiSongSelect(int song)
        {
            await SendMidiMessage(0xf3, song & 0x7f);
        }

        /// <summary>
        /// Song Position Pointer
        /// </summary>
        /// <param name="position">0-16383</param>
        public async void SendMidiSongPositionPointer(int position)
        {
            await SendMidiMessage(0xf2, position & 0x7f, (position >> 7) & 0x7f);
        }

        /// <summary>
        /// Tune Request
        /// </summary>
        public async void SendMidiTuneRequest()
        {
            await SendMidiMessage(0xf6);
        }

        /// <summary>
        /// Timing Clock
        /// </summary>
        public async void SendMidiTimingClock()
        {
            await SendMidiMessage(0xf8);
        }

        /// <summary>
        /// Start Playing
        /// </summary>
        public async void SendMidiStart()
        {
            await SendMidiMessage(0xfa);
        }

        /// <summary>
        /// Continue Playing
        /// </summary>
        public async void SendMidiContinue()
        {
            await SendMidiMessage(0xfb);
        }

        /// <summary>
        /// Stop Playing
        /// </summary>
        public async void SendMidiStop()
        {
            await SendMidiMessage(0xfc);
        }

        /// <summary>
        /// Active Sensing
        /// </summary>
        public async void SendMidiActiveSensing()
        {
            await SendMidiMessage(0xfe);
        }

        /// <summary>
        /// Reset Device
        /// </summary>
        public async void SendMidiReset()
        {
            await SendMidiMessage(0xff);
        }

        /// <summary>
        /// RPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7bits or 14bits</param>
        public async void sendRPNMessage(int channel, int function, int value)
        {
            await sendRPNMessageWithTask(channel, (function >> 7) & 0x7f, function & 0x7f, value);
        }

        /// <summary>
        /// RPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        public async void sendRPNMessage(int channel, int functionMSB, int functionLSB, int value)
        {
            await sendRPNMessageWithTask(channel, functionMSB, functionLSB, value);
        }

        /// <summary>
        /// RPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        private async Task sendRPNMessageWithTask(int channel, int functionMSB, int functionLSB, int value)
        {
            // send the function
            await SendMidiMessage(0xb0 | (channel & 0xf), 101, functionMSB & 0x7f);
            await SendMidiMessage(0xb0 | (channel & 0xf), 100, functionLSB & 0x7f);

            // send the value
            if ((value >> 7) > 0)
            {
                await SendMidiMessage(0xb0 | (channel & 0xf), 6, (value >> 7) & 0x7f);
                await SendMidiMessage(0xb0 | (channel & 0xf), 38, value & 0x7f);
            }
            else
            {
                await SendMidiMessage(0xb0 | (channel & 0xf), 6, value & 0x7f);
            }

            // send the NULL function
            await SendMidiMessage(0xb0 | (channel & 0xf), 101, 0x7f);
            await SendMidiMessage(0xb0 | (channel & 0xf), 100, 0x7f);
        }

        /// <summary>
        /// NRPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7bits or 14bits</param>
        public async void sendNRPNMessage(int channel, int function, int value)
        {
            await sendNRPNMessageWithTask(channel, (function >> 7) & 0x7f, function & 0x7f, value);
        }

        /// <summary>
        /// NRPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        public async void sendNRPNMessage(int channel, int functionMSB, int functionLSB, int value)
        {
            await sendNRPNMessageWithTask(channel, functionMSB, functionLSB, value);
        }

        /// <summary>
        /// NRPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        private async Task sendNRPNMessageWithTask(int channel, int functionMSB, int functionLSB, int value)
        {
            // send the function
            await SendMidiMessage(0xb0 | (channel & 0xf), 99, functionMSB & 0x7f);
            await SendMidiMessage(0xb0 | (channel & 0xf), 98, functionLSB & 0x7f);

            // send the value
            if ((value >> 7) > 0)
            {
                await SendMidiMessage(0xb0 | (channel & 0xf), 6, (value >> 7) & 0x7f);
                await SendMidiMessage(0xb0 | (channel & 0xf), 38, value & 0x7f);
            }
            else
            {
                await SendMidiMessage(0xb0 | (channel & 0xf), 6, value & 0x7f);
            }

            // send the NULL function
            await SendMidiMessage(0xb0 | (channel & 0xf), 101, 0x7f);
            await SendMidiMessage(0xb0 | (channel & 0xf), 100, 0x7f);
        }
    }
}
