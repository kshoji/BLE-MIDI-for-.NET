using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;

namespace kshoji.BleMidi
{
    public class BleMidiDeviceUtils
    {
        // TODO move to Resource file
        static readonly Guid serviceUuid = BleUuidUtils.FromString("0001");
        static readonly Guid inputCharacteristicUuid = BleUuidUtils.FromString("0002");
        static readonly Guid outputCharacteristicUuid = BleUuidUtils.FromString("0003");

        // usage:
        // DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromShortId(0x0001));
        /// <summary>
        /// Obtains GattDeviceService instance for MIDI
        /// </summary>
        /// <returns>GattDeviceService</returns>
        public static async Task<GattDeviceService> GetMidiService()
        {
            // FIXME only first one will be processed.
            var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromShortId(0x0001));
            if (devices.Count > 0)
            {
                foreach (var device in devices)
                {
                    GattDeviceService service = await GattDeviceService.FromIdAsync(device.Id);
                    if (BleUuidUtils.Matches(service.Uuid, serviceUuid))
                    {
                        return service;
                    }
                }
            }

            // not found.
            return null;
        }

        /// <summary>
        /// Obtains BluetoothGattCharacteristic for MIDI Input
        /// </summary>
        /// <param name="gattDeviceService"></param>
        /// <returns>null if no characteristic found</returns>
        public static IReadOnlyList<GattCharacteristic> GetMidiInputCharacteristics(GattDeviceService gattDeviceService)
        {
            List<GattCharacteristic> result = new List<GattCharacteristic>();

            IReadOnlyList<GattCharacteristic> characteristics = gattDeviceService.GetCharacteristics(GattDeviceService.ConvertShortIdToUuid(0x0002));
            foreach (var characteristic in characteristics)
            {
                if (BleUuidUtils.Matches(characteristic.Uuid, inputCharacteristicUuid))
                {
                    result.Add(characteristic);
                }
            }

            return result;
        }

        /// <summary>
        /// Obtains BluetoothGattCharacteristic for MIDI Output
        /// </summary>
        /// <param name="gattDeviceService"></param>
        /// <returns>null if no characteristic found</returns>
        public static IReadOnlyList<GattCharacteristic> GetMidiOutputCharacteristics(GattDeviceService gattDeviceService)
        {
            List<GattCharacteristic> result = new List<GattCharacteristic>();

            IReadOnlyList<GattCharacteristic> characteristics = gattDeviceService.GetCharacteristics(GattDeviceService.ConvertShortIdToUuid(0x0003));
            foreach (var characteristic in characteristics)
            {
                if (BleUuidUtils.Matches(characteristic.Uuid, outputCharacteristicUuid))
                {
                    result.Add(characteristic);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Utility class for BLE UUID
    /// TODO consider to use GattDescriptor.ConvertShortIdToUuid
    /// </summary>
    public class BleUuidUtils
    {
        /// <summary>
        /// Parses a Guid string with the format defined by toString().
        /// </summary>
        /// <param name="uuidString">the Guid string to parse</param>
        /// <returns>Guid instance</returns>
        public static Guid FromString(string uuidString)
        {
            try
            {
                return Guid.Parse(uuidString);
            }
            catch (FormatException)
            {
                // may be a short style
                return Guid.Parse("0000" + uuidString + "-0000-0000-0000-000000000000");
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
        private static ushort GetShortUuid(Guid src)
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
        private static bool IsShortGuid(Guid src)
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
