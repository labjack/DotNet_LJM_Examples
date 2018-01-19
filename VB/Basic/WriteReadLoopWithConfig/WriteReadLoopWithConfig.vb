'------------------------------------------------------------------------------
' Performs an initial call to eWriteNames to write configuration values, and
' then calls eWriteNames and eReadNames repeatedly in a loop.
'
' For documentation on register names to use, see the T-series Datasheet or the
' Modbus Map:
'
' https://labjack.com/support/datasheets/t-series
' https://labjack.com/support/software/examples/modbus/modbus-map
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module WriteReadLoopWithConfig

    Sub Main()
        Dim handle As Integer
        Dim devType As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1
        Dim intervalHandle = 1
        Dim skippedIntervals = 0
        Dim it = 0
        Dim dacVolt As Integer
        Dim fioState As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            ' Setup and call eWriteNames to configure AIN0 (all devices) and
            ' digital I/O (T4 only)
            numFrames = 4
            ReDim aNames(numFrames - 1)
            ReDim aValues(numFrames - 1)
            If devType = LJM.CONSTANTS.dtT4 Then
                ' LabJack T4 configuration

                ' Set FIO5 (DIO5) and FIO6 (DIO6) lines to digital I/O.
                '     DIO_INHIBIT = 0xF9F, b111110011111.
                '                   Update only DIO5 and DIO6.
                '     DIO_ANALOG_ENABLE = 0x000, b000000000000.
                '                         Set DIO5 and DIO6 to digital I/O (b0).
                aNames(0) = "DIO_INHIBIT"
                aNames(1) = "DIO_ANALOG_ENABLE"
                aValues(0) = &HF9F
                aValues(1) = &H0

                ' AIN0:
                '     The T4 only has single-ended analog inputs.
                '     The range of AIN0-AIN3 is +/-10 V.
                '     The range of AIN4-AIN11 is 0-2.5 V.
                '     Resolution index = 0 (default)
                '     Settling = 0(auto)
                aNames(2) = "AIN0_RESOLUTION_INDEX"
                aNames(3) = "AIN0_SETTLING_US"
                aValues(2) = 0
                aValues(3) = 0
            Else
                ' LabJack T7 and other devices configuration

                ' AIN0:
                '     Negative Channel = 199 (Single-ended)
                '     Range = +/-10 V
                '     Resolution index = 0 (default)
                '     Settling = 0 (auto)
                aNames(0) = "AIN0_NEGATIVE_CH"
                aNames(1) = "AIN0_RANGE"
                aNames(2) = "AIN0_RESOLUTION_INDEX"
                aNames(3) = "AIN0_SETTLING_US"
                aValues(0) = 199
                aValues(1) = 10
                aValues(2) = 0
                aValues(3) = 0
            End If
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Set configuration:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  " & aNames(i) & " : " & aValues(i))
            Next

            numFrames = 2
            ReDim aNames(numFrames - 1)
            ReDim aValues(numFrames - 1)
            aValues(0) = 0
            aValues(1) = 0

            Console.WriteLine("")
            Console.WriteLine("Starting read loop.  Press a key to stop.")
            LJM.StartInterval(intervalHandle, 1000000)
            While Console.KeyAvailable = False
                ' Setup and call eWriteNames to write to DAC0, and FIO5 (T4)
                ' or FIO1 (T7 and other devices).
                ' DAC0 will cycle ~0.0 to ~5.0 volts in 1.0 volt increments.
                ' FIO5/FIO1 will toggle output high (1) and low (0) states.
                If devType = LJM.CONSTANTS.dtT4 Then
                    aNames(0) = "DAC0"
                    aNames(1) = "FIO5"
                Else
                    aNames(0) = "DAC0"
                    aNames(1) = "FIO1"
                End If
                dacVolt = it Mod 6  ' 0-5
                fioState = it Mod 2  ' 0 or 1
                aValues(0) = dacVolt
                aValues(1) = fioState
                LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

                Console.WriteLine("")
                Console.Write("eWriteNames :")
                For i = 0 To numFrames - 1
                    Console.Write(" " & aNames(i) & " = " & _
                        aValues(i).ToString("F4"))
                Next
                Console.WriteLine("")

                ' Setup and call eReadNames to read AIN0, and FIO6 (T4) or
                ' FIO2 (T7 and other devices).
                If devType = LJM.CONSTANTS.dtT4 Then
                    aNames(0) = "AIN0"
                    aNames(1) = "FIO6"
                Else
                    aNames(0) = "AIN0"
                    aNames(1) = "FIO2"
                End If
                aValues(0) = 0
                aValues(1) = 0
                LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)
                Console.Write("eReadNames  :")
                For i = 0 To numFrames - 1
                    Console.Write(" " & aNames(i) & " = " & _
                                  aValues(i).ToString("F4"))
                Next
                Console.WriteLine("")

                it = it + 1

                ' Wait for next 1 second interval
                LJM.WaitForNextInterval(intervalHandle, skippedIntervals)
                If skippedIntervals > 0 Then
                    Console.WriteLine("SkippedIntervals: " & skippedIntervals)
                End If
            End While
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        ' Close interval and device handles
        LJM.CleanInterval(intervalHandle)
        LJM.CloseAll()

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine()  ' Pause for user
    End Sub

End Module
