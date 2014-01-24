'------------------------------------------------------------------------------
' SPI.vb
'
' Demonstrates SPI communication.
'
' You can short MOSI to MISO for testing.
'
' MOSI    FIO2
' MISO    FIO3
' CLK     FIO0
' CS      FIO1
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
        Dim numFrames As Integer
        Dim aNames(0) As String
        Dim aWrites(0) As Integer
        Dim aNumValues(0) As Integer
        Dim aValues() As Double
        Dim errAddr As Integer

        Dim numBytes As Integer
        Dim dataWrite() As Double
        Dim dataRead() As Double
        Dim rand As Random

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)


            ' CS is FIO1
            LJM.eWriteName(handle, "SPI_CS_DIONUM", 1)

            ' CLK is FIO0
            LJM.eWriteName(handle, "SPI_CLK_DIONUM", 0)

            ' MISO is FIO3
            LJM.eWriteName(handle, "SPI_MISO_DIONUM", 3)

            ' MOSI is FIO2
            LJM.eWriteName(handle, "SPI_MOSI_DIONUM", 2)

            ' Modes:
            ' 0 = A: CPHA=0, CPOL=0 
            '     Data clocked on the rising edge
            '     Data changed on the falling edge
            '     Final clock state low
            '     Initial clock state low
            ' 1 = B: CPHA=0, CPOL=1
            '     Data clocked on the falling edge
            '     Data changed on the rising edge
            '     Final clock state low
            '     Initial clock state low
            ' 2 = C: CPHA=1, CPOL=0 
            '     Data clocked on the falling edge
            '     Data changed on the rising edge
            '     Final clock state high
            '     Initial clock state high
            ' 3 = D: CPHA=1, CPOL=1 
            '     Data clocked on the rising edge
            '     Data changed on the falling edge
            '     Final clock state high
            '     Initial clock state high

            ' Selecting Mode: A - CPHA=1, CPOL=1.
            LJM.eWriteName(handle, "SPI_MODE", 0)

            ' Speed Throttle:
            ' Frequency = 1000000000 / (175*(65536-SpeedThrottle) + 1020)
            ' Valid speed throttle values are 1 to 65536 where 0 = 65536.
            ' Note: The above equation and its frequency range were tested for
            ' firmware 1.0009 and may change in the future.

            ' Configuring Max. Speed (~ 1 MHz)
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


            ' Write/Read 4 bytes
            numBytes = 4
            LJM.eWriteName(handle, "SPI_NUM_BYTES", numBytes)


            ' Setup write bytes
            ReDim dataWrite(numBytes - 1)
            rand = New Random()
            For i = 0 To numBytes - 1
                dataWrite(i) = Convert.ToDouble(rand.Next(255))
            Next

            ' Write the bytes
            aNames(0) = "SPI_DATA_WRITE"
            aWrites(0) = LJM.CONSTANTS.WRITE
            aNumValues(0) = numBytes
            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, dataWrite, errAddr)

            ' Display the bytes written
            Console.WriteLine("")
            For i = 0 To numBytes - 1
                Console.Out.WriteLine("dataWrite[" & i & "] = " & dataWrite(i))
            Next

            ' Read the bytes
            ReDim dataRead(numBytes - 1)
            aNames(0) = "SPI_DATA_READ"
            aWrites(0) = LJM.CONSTANTS.READ
            aNumValues(0) = numBytes
            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, dataRead, errAddr)

            ' Display the bytes read
            Console.Out.WriteLine("")
            For i = 0 To numBytes - 1
                Console.Out.WriteLine("dataRead[" & i & "] = " & dataRead(i))
            Next
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
