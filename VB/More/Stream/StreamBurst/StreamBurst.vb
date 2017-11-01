'------------------------------------------------------------------------------
' StreamBurst.vb
'
' Demonstrates how to use the StreamBurst function for streaming.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack
Imports System.Diagnostics


Module StreamBurst

    Sub Main()
        Dim handle As Integer
        Dim devType As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1

        Dim numScans As Integer
        Dim numAddresses As Integer
        Dim aScanListNames() As String
        Dim aTypes() As Integer
        Dim aScanList() As Integer
        Dim scanRate As Double

        Dim aData() As Double
        Dim skippedTotal As UInt32
        Dim sw As Stopwatch
        Dim time As Double

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            ' Stream Configuration
            numScans = 20000 ' Number of scans to perform
            numAddresses = 2
            ReDim aScanListNames(numAddresses - 1)  ' Scan list names to stream.
            aScanListNames(0) = "AIN0"
            aScanListNames(1) = "AIN1"
            ReDim aTypes(numAddresses - 1)  ' Dummy
            ReDim aScanList(numAddresses - 1)  ' Scan list addresses to stream. StreamBurst uses Modbus addresses.
            LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes)
            scanRate = 10000  ' Scans per second
            ReDim aData(numScans * numAddresses - 1)

            Try
                ' When streaming, negative channels and ranges can be configured for
                ' individual analog inputs, but the stream has only one settling time and
                ' resolution.
                If devType = LJM.CONSTANTS.dtT4 Then
                    ' LabJack T4 configuration

                    ' AIN0 and AIN1 ranges are +/-10 V, stream settling is
                    ' 0 (default) and stream resolution index is 0 (default).
                    numFrames = 4
                    ReDim aNames(numFrames - 1)
                    aNames(0) = "AIN0_RANGE"
                    aNames(1) = "AIN1_RANGE"
                    aNames(2) = "STREAM_SETTLING_US"
                    aNames(3) = "STREAM_RESOLUTION_INDEX"
                    ReDim aValues(numFrames - 1)
                    aValues(0) = 10.0
                    aValues(1) = 10.0
                    aValues(2) = 0
                    aValues(3) = 0
                Else
                    ' T7 and other devices configuration

                    ' Ensure triggered stream is disabled.
                    LJM.eWriteName(handle, "STREAM_TRIGGER_INDEX", 0)

                    ' Enabling internally-clocked stream.
                    LJM.eWriteName(handle, "STREAM_CLOCK_SOURCE", 0)

                    ' All negative channels are single-ended, AIN0 and AIN1
                    ' ranges are +/-10 V, stream settling is 0 (default) and
                    ' stream resolution index is 0 (default).
                    numFrames = 5
                    ReDim aNames(numFrames - 1)
                    aNames(0) = "AIN_ALL_NEGATIVE_CH"
                    aNames(1) = "AIN0_RANGE"
                    aNames(2) = "AIN1_RANGE"
                    aNames(3) = "STREAM_SETTLING_US"
                    aNames(4) = "STREAM_RESOLUTION_INDEX"
                    ReDim aValues(numFrames - 1)
                    aValues(0) = LJM.CONSTANTS.GND
                    aValues(1) = 10.0
                    aValues(2) = 10.0
                    aValues(3) = 0
                    aValues(4) = 0
                End If
                LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

                Console.WriteLine("")
                Console.WriteLine("Scan list:")
                For i = 0 To numAddresses - 1
                    Console.WriteLine("  " & aScanListNames(i))
                Next
                Console.WriteLine("Scan rate = " & scanRate & " Hz")
                Console.WriteLine("Sample rate = " & (scanRate * numAddresses) & " Hz")
                Console.WriteLine("Total number of scans = " & numScans)
                Console.WriteLine("Total number of samples = " & (numScans * numAddresses))
                Console.WriteLine("Seconds of samples = " & (numScans / scanRate) & " seconds")

                Console.WriteLine("")
                Console.Write("Streaming with StreamBurst...")

                sw = New Stopwatch()
                sw.Start()

                ' Stream data using StreamBurst
                LJM.StreamBurst(handle, numAddresses, aScanList, scanRate, numScans, aData)

                sw.Stop()

                Console.WriteLine(" Done")

                Console.WriteLine("")
                Console.WriteLine("Skipped scans = " & _
                                  (skippedTotal / numAddresses))
                time = sw.ElapsedMilliseconds / 1000.0
                Console.WriteLine("Time taken = " & time & " seconds")

                Console.WriteLine("")
                Console.WriteLine("Last scan:")
                For i = 0 To numAddresses - 1
                    Console.WriteLine("  " & aScanListNames(i) & " = " & _
                                      aData((numScans - 1) * numAddresses + i))
                Next

            Catch ljme As LJM.LJMException
                showErrorMessage(ljme)
            End Try

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
