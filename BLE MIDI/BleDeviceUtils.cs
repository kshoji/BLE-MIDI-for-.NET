using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.Storage.Streams;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Utility class for BLE MIDI Device detection
    /// </summary>
    public class BleMidiDeviceUtils
    {
        private static readonly List<Guid> serviceUuids = new List<Guid>();
        private static readonly List<Guid> inputCharacteristicUuids = new List<Guid>();
        private static readonly List<Guid> outputCharacteristicUuids = new List<Guid>();

        static BleMidiDeviceUtils()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize Guid lists
        /// </summary>
        private static async void Initialize()
        {
            var xml = await Package.Current.InstalledLocation.GetFileAsync(@"BLE MIDI\Contents\uuids.xml");
            string text = await FileIO.ReadTextAsync(xml, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            string currentElementName = null;
            using (XmlReader reader = XmlReader.Create(new StringReader(text)))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name.Equals("string-array"))
                            {
                                currentElementName = reader.GetAttribute("name");
                            }
                            break;

                        case XmlNodeType.Text:
                            switch (currentElementName)
                            {
                                case "uuidListForService":
                                    serviceUuids.Add(BleUuidUtils.FromString(reader.Value));
                                    break;
                                case "uuidListForInputCharacteristic":
                                    inputCharacteristicUuids.Add(BleUuidUtils.FromString(reader.Value));
                                    break;
                                case "uuidListForOutputCharacteristic":
                                    outputCharacteristicUuids.Add(BleUuidUtils.FromString(reader.Value));
                                    break;
                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Obtains list of GattDeviceService for MIDI
        /// </summary>
        /// <returns>List of GattDeviceService</returns>
        public static async Task<IReadOnlyDictionary<GattDeviceService, DeviceInformation>> GetMidiServices()
        {
            Dictionary<GattDeviceService, DeviceInformation> result = new Dictionary<GattDeviceService, DeviceInformation>();

            foreach (var serviceUuid in serviceUuids)
            {
                string deviceSelector;
                if (BleUuidUtils.IsShortGuid(serviceUuid))
                {
                    deviceSelector = GattDeviceService.GetDeviceSelectorFromShortId(BleUuidUtils.GetShortUuid(serviceUuid));
                }
                else
                {
                    deviceSelector = GattDeviceService.GetDeviceSelectorFromUuid(serviceUuid);
                }

                var devices = await DeviceInformation.FindAllAsync(deviceSelector);
                if (devices.Count > 0)
                {
                    foreach (var device in devices)
                    {
                        GattDeviceService service = await GattDeviceService.FromIdAsync(device.Id);
                        if (service != null)
                        {
                            result.Add(service, device);
                        }
                    }
                }
            }

            return new ReadOnlyDictionary<GattDeviceService, DeviceInformation>(result);
        }

        /// <summary>
        /// Obtains list of BluetoothGattCharacteristic for MIDI Input
        /// </summary>
        /// <param name="gattDeviceService"></param>
        /// <returns>list of the GattCharacteristic, empty list if no characteristic found</returns>
        public static IReadOnlyList<GattCharacteristic> GetMidiInputCharacteristics(GattDeviceService gattDeviceService)
        {
            List<GattCharacteristic> result = new List<GattCharacteristic>();

            foreach (var inputCharacteristic in inputCharacteristicUuids)
            {
                Guid convertedCharacteristic = inputCharacteristic;
                if (BleUuidUtils.IsShortGuid(convertedCharacteristic))
                {
                    convertedCharacteristic = GattDeviceService.ConvertShortIdToUuid(BleUuidUtils.GetShortUuid(inputCharacteristic));
                }

                IReadOnlyList<GattCharacteristic> characteristics = gattDeviceService.GetCharacteristics(convertedCharacteristic);
                foreach (var characteristic in characteristics)
                {
                    result.Add(characteristic);
                }
            }

            return new ReadOnlyCollection<GattCharacteristic>(result);
        }

        /// <summary>
        /// Obtains list of BluetoothGattCharacteristic for MIDI Output
        /// </summary>
        /// <param name="gattDeviceService"></param>
        /// <returns>list of the GattCharacteristic, empty list if no characteristic found</returns>
        public static IReadOnlyList<GattCharacteristic> GetMidiOutputCharacteristics(GattDeviceService gattDeviceService)
        {
            List<GattCharacteristic> result = new List<GattCharacteristic>();

            foreach (var outputCharacteristic in outputCharacteristicUuids)
            {
                Guid convertedCharacteristic = outputCharacteristic;
                if (BleUuidUtils.IsShortGuid(convertedCharacteristic))
                {
                    convertedCharacteristic = GattDeviceService.ConvertShortIdToUuid(BleUuidUtils.GetShortUuid(outputCharacteristic));
                }

                IReadOnlyList<GattCharacteristic> characteristics = gattDeviceService.GetCharacteristics(convertedCharacteristic);
                foreach (var characteristic in characteristics)
                {
                    result.Add(characteristic);
                }
            }

            return new ReadOnlyCollection<GattCharacteristic>(result);
        }
    }

    /// <summary>
    /// Utility class for BLE UUID
    /// </summary>
    public class BleUuidUtils
    {
        /// <summary>
        /// Parses a Guid string with the format defined by toString().
        /// </summary>
        /// <param name="uuidString">the Guid string to parse</param>
        /// <returns>Guid instance</returns>
        /// <exception cref="FormatException">if argument is invalid format</exception>
        public static Guid FromString(string uuidString)
        {
            try
            {
                return Guid.Parse(uuidString);
            }
            catch (FormatException)
            {
                // may be a short style
                return GattDescriptor.ConvertShortIdToUuid(ushort.Parse(uuidString));
            }
        }

        /// <summary>
        /// Check if matches UUID
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static bool Matches(Guid src, Guid dst)
        {
            if (src == null && dst == null)
            {
                return true;
            }
            if (src == null || dst == null)
            {
                return false;
            }

            if (IsShortGuid(src) || IsShortGuid(dst))
            {
                return GetShortUuid(src) == GetShortUuid(dst);
            }
            else
            {
                return src.Equals(dst);
            }
        }

        /// <summary>
        /// Obtains short style part of uuid from specified Guid
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static ushort GetShortUuid(Guid src)
        {
            ushort result = 0;
            byte[] uuid = src.ToByteArray();
            for (int i = 0; i < 2; i++)
            {
                result |= (ushort)(uuid[i] << (1 - i));
            }
            return result;
        }

        /// <summary>
        /// Checks if the specified Guid is short style
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static bool IsShortGuid(Guid src)
        {
            byte[] uuid = src.ToByteArray();
            int otherbits = 0;
            for (int i = 0; i < uuid.Length; i++)
            {
                if (i >= 2)
                {
                    otherbits += uuid[i];
                }
            }
            if (otherbits != 0)
            {
                return false;
            }
            return true;
        }
    }
}
