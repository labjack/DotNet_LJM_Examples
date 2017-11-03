'------------------------------------------------------------------------------
' SPI.vb
'
' Demonstrates SPI communication.
'
' You can short MOSI to MISO for testing.
'
' T7:
'     MOSI    FIO2
'     MISO    FIO3
'     CLK     FIO0
'     CS      FIO1
'
' T4:
'     MOSI    FIO6
'     MISO    FIO7
'     CLK     FIO4
'     CS      FIO5
'
' If you short MISO to MOSI, then you will read back the same bytes that you
' write.  If you short MISO to GND, then you will read back zeros.  If you
' short MISO to VS or leave it unconnected, you will read back 255s.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module SPI

    Sub Main()
        Dim handle As Integer
        Dim numFrames As Integer
        Dim aNames(0) As String
        Dim aValues() As Double
        Dim errAddr As Integer
        Dim numBytes As Integer
        Dim aBytes(3) As Byte
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

            If devType = LJM.CONSTANTS.dtT4 Then
                ' Configure FIO4 to FIO7 as digital I/O.
                LJM.eWriteName(handle, "DIO_INHIBIT", &HFFF0F)
                LJM.eWriteName(handle, "DIO_ANALOG_ENABLE", &H0)

                ' Setting CS, CLK, MISO, and MOSI lines for the T4. FIO0 to
                ' FIO3 are reserved for analog inputs, and SPI requires
                ' digital lines.
                LJM.eWriteName(handle, "SPI_CS_DIONUM", 5)  ' CS is FIO5
                LJM.eWriteName(handle, "SPI_CLK_DIONUM", 4)  ' CLK is FIO4
                LJM.eWriteName(handle, "SPI_MISO_DIONUM", 7)  ' MISO is FIO7
                LJM.eWriteName(handle, "SPI_MOSI_DIONUM", 6)  ' MOSI is FIO6
            Else
                ' Setting CS, CLK, MISO, and MOSI lines for the T7 and other
                ' devices.
                LJM.eWriteName(handle, "SPI_CS_DIONUM", 1)  ' CS is FIO1
                LJM.eWriteName(handle, "SPI_CLK_DIONUM", 0)  ' CLK is FIO0
                LJM.eWriteName(handle, "SPI_MISO_DIONUM", 3)  ' MISO is FIO3
                LJM.eWriteName(handle, "SPI_MOSI_DIONUM", 2)  ' MOSI is FIO2
            End If

            ' Selecting Mode CPHA=1 (bit 0), CPOL=1 (bit 1)
            LJM.eWriteName(handle, "SPI_MODE", 3)

            ' Speed Throttle:
            ' Valid speed throttle values are 1 to 65536 where 0 = 65536.
            ' Configuring Max. Speed (~800 kHz) = 0
            LJM.eWriteName(handle, "SPI_SPEED_THROTTLE", 0)

            ' Options
            ' bit 0:
            '     0 = Active low clock select enabled
            '     1 = Active low clock select disabled.
            ' bit 1:
            '     0 = DIO directions are automatically changed
            '     1 = DIO directions are not automatically changed.
            ' bits 2-3: Reserved
            ' bits 4-7: Number of bits in the last byte. 0 = 8.
            ' bits 8-15: Reserved

            ' Enabling active low clock select pin
            LJM.eWriteName(handle, "SPI_OPTIONS", 0)

            ' Read back and display the SPI settings
            numFrames = 7
            ReDim aNames(numFrames - 1)
            aNames(0) = "SPI_CS_DIONUM"
            aNames(1) = "SPI_CLK_DIONUM"
            aNames(2) = "SPI_MISO_DIONUM"
            aNames(3) = "SPI_MOSI_DIONUM"
            aNames(4) = "SPI_MODE"
            aNames(5) = "SPI_SPEED_THROTTLE"
            aNames(6) = "SPI_OPTIONS"
            ReDim aValues(numFrames - 1)
            LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("SPI Configuration:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  " & aNames(i) & " = " & aValues(i))
            Next

            ' Write(TX)/Read(RX) 4 bytes
            numBytes = 4
            LJM.eWriteName(handle, "SPI_NUM_BYTES", numBytes)

            ' Write the bytes
            rand = New Random()
            For i = 0 To numBytes - 1
                aBytes(i) = Convert.ToByte(rand.Next(255))
            Next
            LJM.eWriteNameByteArray(handle, "SPI_DATA_TX", numBytes, aBytes, errAddr)
            LJM.eWriteName(handle, "SPI_GO", 1)  ' Do the SPI communications

            ' Display the bytes written
            Console.WriteLine("")
            For i = 0 To numBytes - 1
                Console.Out.WriteLine("dataWrite[" & i & "] = " & aBytes(i))
            Next

            ' Read the bytes
            ' Initialize byte array values to zero
            For i = 0 To numBytes - 1
                aBytes(i) = Convert.ToByte(rand.Next(255))
            Next
            LJM.eReadNameByteArray(handle, "SPI_DATA_RX", numBytes, aBytes, errAddr)
            LJM.eWriteName(handle, "SPI_GO", 1)  ' Do the SPI communications

            ' Display the bytes read
            Console.Out.WriteLine("")
            For i = 0 To numBytes - 1
                Console.Out.WriteLine("dataRead[" & i & "] = " & aBytes(i))
            Next
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
