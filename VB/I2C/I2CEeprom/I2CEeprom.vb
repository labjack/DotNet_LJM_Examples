'------------------------------------------------------------------------------
' I2CEeprom.vb
'
' Demonstrates I2C communication using the LJM driver. The demonstration uses a
' LJTick-DAC connected to FIO0/FIO1, configures I2C settings, and reads, writes
' and reads bytes from/to the EEPROM.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack

Module I2CEeprom

    Sub showErrorMessage(ByVal e As LJM.LJMException)
        Console.WriteLine("LJMException: " & e.ToString)
        Console.WriteLine(e.StackTrace)
    End Sub

    Sub displayHandleInfo(ByVal handle As Integer)
        Dim devType As Integer
        Dim conType As Integer
        Dim serNum As Integer
        Dim ipAddr As Integer
        Dim port As Integer
        Dim maxBytesPerMB As Integer
        Dim ipAddrStr As String = ""

        LJM.GetHandleInfo(handle, devType, conType, serNum, ipAddr, port, _
                          maxBytesPerMB)
        LJM.NumberToIP(ipAddr, ipAddrStr)
        Console.WriteLine("Opened a LabJack with Device type: " & devType & _
                          ", Connection type: " & conType & ",")
        Console.WriteLine("Serial number: " & serNum & ", IP address: " & _
                          ipAddrStr & ", Port: " & port & ",")
        Console.WriteLine("Max bytes per MB: " & maxBytesPerMB)
    End Sub

    Sub Main()
        Dim handle As Integer
        Dim aNames(1) As String
        Dim aWrites(1) As Integer
        Dim aNumValues(1) As Integer
        Dim aValues() As Double
        Dim errAddr As Integer
        Dim rand As Random

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)


            ' Configure the I2C communication.
            LJM.eWriteName(handle, "I2C_SDA_DIONUM", 1) ' SDA pin number = 1 (FIO1)

            LJM.eWriteName(handle, "I2C_SCL_DIONUM", 0) ' SCA pin number = 0 (FIO0)

            ' Speed throttle is inversely proportional to clock frequency.
            ' 0 = max.
            LJM.eWriteName(handle, "I2C_SPEED_THROTTLE", 0) ' Speed throttle = 0

            ' Options bits:
            '   bit0: Reset the I2C bus.
            '   bit1: Restart w/o stop
            '   bit2: Disable clock stretching.
            LJM.eWriteName(handle, "I2C_OPTIONS", 0) ' Options = 0

            LJM.eWriteName(handle, "I2C_SLAVE_ADDRESS", 80) ' Slave Address of the I2C chip = 80 (0x50)


            ' Initial read of EEPROM bytes 0-3 in the user memory area. We
            ' need a single I2C transmission that writes the chip's memory
            ' pointer and then reads the data.
            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1) ' Set the number of bytes to transmit
            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4) ' Set the number of bytes to receive

            ReDim aValues(5) ' TX/RX bytes will go here

            ' Set the TX bytes. We are sending 1 byte for the address.
            aNames(0) = "I2C_WRITE_DATA"
            aWrites(0) = LJM.CONSTANTS.WRITE ' Indicates we are writing the values.
            aNumValues(0) = 1 ' The number of bytes
            aValues(0) = 0 ' Byte 0: Memory pointer = 0
            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, errAddr)

            LJM.eWriteName(handle, "I2C_GO", 1) ' Do the I2C communications.

            ' Read the RX bytes.
            aNames(0) = "I2C_READ_DATA"
            aWrites(0) = LJM.CONSTANTS.READ ' Indicates we are reading the values.
            aNumValues(0) = 4 ' The number of bytes
            ' aValues(0) to aValues(3) will contain the data
            For i = 0 To 3
                aValues(i) = 0
            Next
            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, errAddr)

            Console.WriteLine("")
            Console.Write("Read User Memory [0-3] = ")
            For i = 0 To 3
                Console.Write(aValues(i) & " ")
            Next
            Console.WriteLine("")

            ' Write EEPROM bytes 0-3 in the user memory area, using the
            ' page write technique.  Note that page writes are limited to
            ' 16 bytes max, and must be aligned with the 16-byte page
            ' intervals.  For instance, if you start writing at address 14,
            ' you can only write two bytes because byte 16 is the start of
            ' a new page.
            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 5) ' Set the number of bytes to transmit
            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 0) ' Set the number of bytes to receive

            ' Set the TX bytes.
            aNames(0) = "I2C_WRITE_DATA"
            aWrites(0) = LJM.CONSTANTS.WRITE ' Indicates we are writing the values.
            aNumValues(0) = 5 ' The number of bytes
            aValues(0) = 0 ' Byte 0: Memory pointer = 0
            ' Create 4 new random numbers to write (aValues(1-4)).
            rand = New Random()
            For i = 1 To 4
                aValues(i) = Convert.ToDouble(rand.Next(255)) ' 0 to 255
            Next
            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, errAddr)

            LJM.eWriteName(handle, "I2C_GO", 1) ' Do the I2C communications.

            Console.Write("Write User Memory [0-3] = ")
            For i = 1 To 4
                Console.Write(aValues(i) & " ")
            Next
            Console.WriteLine("")


            ' Final read of EEPROM bytes 0-3 in the user memory area. We need
            ' a single I2C transmission that writes the address and then reads
            ' the data.
            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1) ' Set the number of bytes to transmit
            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4) ' Set the number of bytes to receive

            ' Set the TX bytes. We are sending 1 byte for the address.
            aNames(0) = "I2C_WRITE_DATA"
            aWrites(0) = LJM.CONSTANTS.WRITE ' Indicates we are writing the values.
            aNumValues(0) = 1 ' The number of bytes
            aValues(0) = 0 ' Byte 0: Memory pointer = 0
            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, errAddr)

            LJM.eWriteName(handle, "I2C_GO", 1) ' Do the I2C communications.

            ' Read the RX bytes.
            aNames(0) = "I2C_READ_DATA"
            aWrites(0) = LJM.CONSTANTS.READ ' Indicates we are reading the values.
            aNumValues(0) = 4 ' The number of bytes
            ' aValues(0) to aValues(3) will contain the data
            For i = 0 To 3
                aValues(i) = 0
            Next
            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, errAddr)

            Console.Write("Read User Memory [0-3] = ")
            For i = 0 To 3
                Console.Write(aValues(i) & " ")
            Next
            Console.WriteLine("")
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        LJM.CloseAll() ' Close all handles

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine() ' Pause for user
    End Sub

End Module
