BLE MIDI for .NET
=================

MIDI over Bluetooth LE library for Windows 8.1

This library is created as `.NET Portable Library`, so it can be used for creating

- Windows (8.1) Store Apps
- Windows Phone (8.1) Store Apps
    - There're no Windows Phone 8.1 devices released in my country Japan, so I can't test with it.

Status
------

Work in progress.
I'll receive the first BLE MIDI device at this month.

Usage of the library
--------------------

At first, create `BleMidilCentralProvider` instance
```c#
using kshoji.BleMidi;

BleMidilCentralProvider bleMidiCentralProvider = new BleMidilCentralProvider();
```

And then, setup the event handlers.

```c#
bleMidiCentralProvider.MidiInputDeviceAttached += MainPage_MidiInputDeviceAttached;

// MIDI device connection event handlers
void MainPage_MidiInputDeviceAttached(MidiInputDevice midiInputDevice)
{
    midiInputDevice.NoteOn += MainPage_NoteOn;
    midiInputDevice.NoteOff += MainPage_NoteOff;
}

// MIDI event handlers, called from another thread.
void MainPage_NoteOn(MidiInputDevice sender, int channel, int note, int velocity)
{
	System.Diagnostics.Debug.WriteLine("NoteOn note:" + note);
}
void MainPage_NoteOff(MidiInputDevice sender, int channel, int note, int velocity)
{
	System.Diagnostics.Debug.WriteLine("NoteOff note:" + note);
}
```

Now, call `ScanDevices` method to scan the BLE MIDI devices.

```c#
await bleMidiCentralProvider.ScanDevices();
```

For more details, see the [wiki](https://github.com/kshoji/BLE-MIDI-for-.NET/wiki).
