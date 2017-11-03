'------------------------------------------------------------------------------
' I2CEeprom.vb
'
' Demonstrates I2C communication using a LabJack. The demonstration uses a
' LJTick-DAC connected to FIO0/FIO1 for the T7 or FIO4/FIO5 for the T4, and
' configures the I2C settings. Then a read, write and again a read are
' performed on the LJTick-DAC EEPROM.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module I2CEeprom

    Sub Main()
        Dim handle As Integer
        Dim numBytes As Integer
        Dim aBytes(4) As Byte  ' TX/RX bytes go here. Sending/receiving 5 bytes max.
        Dim errAddr As Integer = -1
        Dim rand As Random
        Dim devType As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            ' Configure the I2C communication.
            If devType = LJM.CONSTANTS.dtT4 Then
                ' Configure FIO4 and FIO5 as digital I/O.
                LJM.eWriteName(handle, "DIO_INHIBIT", &HFFFCF)
                LJM.eWriteName(handle, "DIO_ANALOG_ENABLE", &H0)

                ' For the T4, using FIO4 and FIO5 for SCL and SDA pins. FIO0 to
                ' FIO3 are reserved for analog inputs, and digital lines are
                ' required.
                LJM.eWriteName(handle, "I2C_SDA_DIONUM", 5)  ' SDA pin number = 5 (FIO5)
                LJM.eWriteName(handle, "I2C_SCL_DIONUM", 4)  ' SCL pin number = 4 (FIO4)
            Else
                ' For the T7 and other devices, using FIO0 and FIO1 for the SCL
                ' and SDA pins.
                LJM.eWriteName(handle, "I2C_SDA_DIONUM", 1)  ' SDA pin number = 1 (FIO1)
                LJM.eWriteName(handle, "I2C_SCL_DIONUM", 0)  ' SCL pin number = 0 (FIO0)
            End If

            ' Speed throttle is inversely proportional to clock frequency.
            ' 0 = max.
            LJM.eWriteName(handle, "I2C_SPEED_THROTTLE", 65516)  ' Speed throttle = 65516 (~100 kHz)

            ' Options bits:
            '   bit0: Reset the I2C bus.
            '   bit1: Restart w/o stop
            '   bit2: Disable clock stretching.
            LJM.eWriteName(handle, "I2C_OPTIONS", 0)  ' Options = 0

            LJM.eWriteName(handle, "I2C_SLAVE_ADDRESS", 80)  ' Slave Address of the I2C chip = 80 (0x50)

            ' Initial read of EEPROM bytes 0-3 in the user memory area. We
            ' need a single I2C transmission that writes the chip's memory
            ' pointer and then reads the data.
            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1)  ' Set the number of bytes to transmit
            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4)  ' Set the number of bytes to receive

            ' Set the TX bytes. We are sending 1 byte for the address.
            numBytes = 1
            aBytes(0) = 0  ' Byte 0: Memory pointer = 0
            LJM.eWriteNameByteArray(handle, "I2C_DATA_TX", numBytes, aBytes, errAddr)

            LJM.eWriteName(handle, "I2C_GO", 1)  ' Do the I2C communications.

            ' Read the RX bytes.
            numBytes = 4  ' The number of bytes
            ' aBytes(0) to aBytes(3) will contain the data
            For i = 0 To numBytes - 1
                aBytes(i) = 0
            Next
            LJM.eReadNameByteArray(handle, "I2C_DATA_RX", numBytes, aBytes, errAddr)

            Console.WriteLine("")
            Console.Write("Read User Memory [0-3] = ")
            For i = 0 To numBytes - 1
                Console.Write(aBytes(i) & " ")
            Next
            Console.WriteLine("")

            ' Write EEPROM bytes 0-3 in the user memory area, using the
            ' page write technique.  Note that page writes are limited to
            ' 16 bytes max, and must be aligned with the 16-byte page
            ' intervals.  For instance, if you start writing at address 14,
            ' you can only write two bytes because byte 16 is the start of
            ' a new page.
            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 5)  ' Set the number of bytes to transmit
            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 0)  ' Set the number of bytes to receive

            ' Set the TX bytes.
            numBytes = 5
            aBytes(0) = 0  ' Byte 0: Memory pointer = 0
            ' Create 4 new random numbers to write (aBytes(1-4)).
            rand = New Random()
            For i = 1 To numBytes - 1
                aBytes(i) = Convert.ToByte(rand.Next(255))  ' 0 to 255
            Next
            LJM.eWriteNameByteArray(handle, "I2C_DATA_TX", numBytes, aBytes, errAddr)

            LJM.eWriteName(handle, "I2C_GO", 1)  ' Do the I2C communications.

            Console.Write("Write User Memory [0-3] = ")
            For i = 1 To numBytes - 1
                Console.Write(aBytes(i) & " ")
            Next
            Console.WriteLine("")

            ' Final read of EEPROM bytes 0-3 in the user memory area. We need
            ' a single I2C transmission that writes the address and then reads
            ' the data.
            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1)  ' Set the number of bytes to transmit
            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4)  ' Set the number of bytes to receive

            ' Set the TX bytes. We are sending 1 byte for the address.
            numBytes = 1  ' The number of bytes
            aBytes(0) = 0  ' Byte 0: Memory pointer = 0
            LJM.eWriteNameByteArray(handle, "I2C_DATA_TX", numBytes, aBytes, errAddr)

            LJM.eWriteName(handle, "I2C_GO", 1)  ' Do the I2C communications.

            ' Read the RX bytes.
            numBytes = 4  ' The number of bytes
            ' aBytes(0) to aBytes(3) will contain the data
            For i = 0 To numBytes - 1
                aBytes(i) = 0
            Next
            LJM.eReadNameByteArray(handle, "I2C_DATA_RX", numBytes, aBytes, errAddr)

            Console.Write("Read User Memory [0-3] = ")
            For i = 0 To numBytes - 1
                Console.Write(aBytes(i) & " ")
            Next
            Console.WriteLine("")
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        LJM.CloseAll()  ' Close all handles

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine()  ' Pause for user
    End Sub

End Module
