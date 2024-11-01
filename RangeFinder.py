import asyncio
import logging
import struct
from datetime import datetime
from pynput.keyboard import Controller
from bleak import BleakClient, BleakScanner
from bleak.backends.characteristic import BleakGATTCharacteristic
from measurement_logger import append_measurement

charUUID = '00004301-0000-0041-5253-534f46540000'

logger = logging.getLogger(__name__)
keyboard = Controller()

def notification_handler(characteristic: BleakGATTCharacteristic, data: bytearray):
    logger.info("%s: %r", characteristic.uuid, data.hex())
    # Check if the data starts with the measurement prefix
    if data.startswith(b'\xc0\x55\x10\x06'):
        logger.info("%r", data[7:11].hex())
        # bytes 7-10 contain the length (as little endian 32-bit float)
        length = struct.unpack('<f', data[7:11])[0] * 1000
        #Timestamp
        timestamp = datetime.now().strftime("%H:%M:%S")
        # Create a tuple with the timestamp and length
        measurement = (timestamp, round(length, 1), 'mm')
        append_measurement("bosch_measurements.csv",measurement)
        logger.info("Measurement: %r", measurement)
        
        keyboard.type(f"{measurement}\n")

async def main():
    logger.info("starting scan...")
    device = await BleakScanner.find_device_by_address('00:13:43:48:01:18', timeout=5, cb=dict(use_bdaddr=True))
    if device is None:
        logger.error("could not find device with address '%s'", '00:13:43:48:01:18')
        exit(0)

    logger.info("connecting to device...")

    async with BleakClient(device) as client:
        logger.info("Connected")
        await asyncio.sleep(0.5)
        # Listen to any measurement notifications when the green "measure" button is pressed on the laser meter
        await client.start_notify(charUUID, notification_handler)
        await asyncio.sleep(0.5)
        # Send some magic byte sequence to ensure that indications contain measurement data
        await client.write_gatt_char(charUUID, bytearray([0xc0, 0x55, 0x02, 0x01, 0x00, 0x1a]), True)

        while client.is_connected:
            await asyncio.sleep(1)

if __name__ == "__main__":
    log_level = logging.DEBUG
    logging.basicConfig(
        level=log_level,
        format="%(asctime)-15s %(name)-8s %(levelname)s: %(message)s",
    )

    loop = asyncio.get_event_loop()
    try:
        asyncio.ensure_future(main())
        loop.run_forever()
    except KeyboardInterrupt:
        pass
    finally:
        print("Closing Loop")
        loop.close()
