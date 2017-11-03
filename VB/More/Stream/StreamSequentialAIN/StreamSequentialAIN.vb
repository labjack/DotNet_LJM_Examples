'------------------------------------------------------------------------------
' StreamSequentialAIN.vb
'
' Demonstrates how to stream a range of sequential analog inputs using the
' eStream functions. Useful when streaming many analog inputs. AIN channel scan
' list is FIRST_AIN_CHANNEL to FIRST_AIN_CHANNEL + NUMBER_OF_AINS - 1.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports System.Threading
Imports LabJack


Module StreamSequentialAIN

    Sub Main()
        Const FIRST_AIN_CHANNEL As Integer = 0 ' 0 = AIN0
        Const NUMBER_OF_AINS As Integer = 8

        Dim handle As Integer = 0
        Dim devType As Integer = 0

        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1

        Dim scansPerRead As Integer
        Dim numAddresses As Integer
        Dim aScanListNames() As String
        Dim aTypes() As Integer
        Dim aScanList() As Integer
        Dim scanRate As Double

        Dim loopCnt As UInt64
        Dim totScans As UInt64
        Dim aData() As Double
        Dim skippedTotal As UInt64
        Dim skippedCur As Integer
        Dim deviceScanBacklog As Integer
        Dim ljmScanBacklog As Integer
        Dim sw As Stopwatch
        Dim time As Double

        Dim dioInhibit As Double
        Dim dioAnalogEnable As Double
        Const rangeAINHV As Double = 10.0  ' HV channels range (AIN0-AIN3)
        Const rangeAINLV As Double = 2.4  ' LV channels range (AIN4+)

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            Try
                ' When streaming, negative channels and ranges can be
                ' configured for individual analog inputs, but the stream has only
                ' one settling time and resolution.
                If devType = LJM.CONSTANTS.dtT4 Then
                    ' T4 configuration

                    ' Configure the channels to analog input or digital I/O
                    ' Update all digital I/O channels. b1 = Ignored. b0 = Affected.
                    dioInhibit = &H0 ' b00000000000000000000
                    ' Set AIN0-AIN3 and AIN FIRST_AIN_CHANNEL to
                    ' FIRST_AIN_CHANNEL+NUMBER_OF_AINS-1 as analog inputs (b1), the
                    ' rest as digital I/O (b0).
                    dioAnalogEnable = (((Math.Pow(2, NUMBER_OF_AINS) - 1) << FIRST_AIN_CHANNEL) Or &HF)
                    ReDim aNames(1)
                    aNames(0) = "DIO_INHIBIT"
                    aNames(1) = "DIO_ANALOG_ENABLE"
                    ReDim aValues(1)
                    aValues(0) = dioInhibit
                    aValues(1) = dioAnalogEnable
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, errAddr)

                    ' Configure the analog input ranges.
                    ReDim aNames(NUMBER_OF_AINS - 1)
                    ReDim aValues(NUMBER_OF_AINS - 1)
                    For i = 0 To NUMBER_OF_AINS - 1
                        aNames(i) = "AIN" & (FIRST_AIN_CHANNEL + i) & "_RANGE"
                        aValues(i) = IIf((FIRST_AIN_CHANNEL + i) < 4, rangeAINHV, rangeAINLV)
                    Next
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, errAddr)

                    ' Configure the stream settling time and stream resolution
                    ' index.
                    ReDim aNames(1)
                    aNames(0) = "STREAM_SETTLING_US"
                    aNames(1) = "STREAM_RESOLUTION_INDEX"
                    ReDim aValues(2)
                    aValues(0) = 10.0  ' 0 (default)
                    aValues(1) = 0  ' 0 (default)
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, errAddr)
                Else
                    ' T7 and other devices configuration

                    ' Ensure triggered stream is disabled.
                    LJM.eWriteName(handle, "STREAM_TRIGGER_INDEX", 0)

                    ' Enabling internally-clocked stream.
                    LJM.eWriteName(handle, "STREAM_CLOCK_SOURCE", 0)

                    ' Configure the analog input negative channels, ranges, stream
                    ' settling time and stream resolution index.
                    ReDim aNames(3)
                    aNames(0) = "AIN_ALL_NEGATIVE_CH"
                    aNames(1) = "AIN_ALL_RANGE"
                    aNames(2) = "STREAM_SETTLING_US"
                    aNames(3) = "STREAM_RESOLUTION_INDEX"
                    ReDim aValues(3)
                    aValues(0) = LJM.CONSTANTS.GND  ' single-ended
                    aValues(1) = 10.0  ' +/-10V,
                    aValues(2) = 0  ' 0 (default)
                    aValues(3) = 0  ' 0 (default)
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, errAddr)
                End If

                ' Stream Configuration
                scansPerRead = 1000  ' # scans returned by eStreamRead call
                scanRate = 1000  ' Scans per second
                ' Scan list names to stream. AIN(FIRST_AIN_CHANNEL) to
                ' AIN(NUMBER_OF_AINS-1).
                numAddresses = NUMBER_OF_AINS
                ReDim aScanListNames(numAddresses - 1)  ' Scan list names to stream.
                For i As Integer = 0 To numAddresses - 1
                    aScanListNames(i) = "AIN" + (FIRST_AIN_CHANNEL + i).ToString()
                Next
                ReDim aTypes(numAddresses - 1)  ' Dummy
                ReDim aScanList(numAddresses - 1)  ' Scan list addresses to stream.
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes)


                Console.WriteLine("")
                Console.WriteLine("Starting stream. Press a key to stop " & _
                                  "streaming.")
                Thread.Sleep(1000)  ' Delay so user's can read message

                ' Configure and start stream
                LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, scanRate)

                loopCnt = 0
                totScans = 0
                ReDim aData(scansPerRead * numAddresses - 1)  ' # of samples per eStreamRead is scansPerRead * numAddresses
                skippedTotal = 0
                skippedCur = 0
                deviceScanBacklog = 0
                ljmScanBacklog = 0
                sw = New Stopwatch()

                Console.WriteLine("Starting read loop.")
                sw.Start()
                While Console.KeyAvailable = False
                    LJM.eStreamRead(handle, aData, deviceScanBacklog, ljmScanBacklog)
                    totScans += scansPerRead

                    ' Count the skipped samples which are indicated by -9999
                    ' values.Missed samples occur after a device's stream
                    ' buffer overflows and are reported after auto-recover
                    ' mode ends.
                    skippedCur = 0
                    For Each d As Double In aData
                        If d = -9999.0 Then skippedCur += 1
                    Next
                    skippedTotal += skippedCur
                    loopCnt += 1
                    Console.WriteLine("")
                    Console.WriteLine("eStreamRead " & loopCnt)
                    Console.WriteLine("  1st scan out of " & scansPerRead & ":")
                    For j = 0 To numAddresses - 1
                        Console.WriteLine("    " + aScanListNames(j) & " = " & _
                                      aData(j).ToString("F4"))
                    Next
                    Console.WriteLine("  numSkippedScans: " & _
                                      (skippedCur / numAddresses) & _
                                      ", deviceScanBacklog: " & _
                                      deviceScanBacklog & _
                                      ", ljmScanBacklog: " & ljmScanBacklog)
                End While

                sw.Stop()

                ' Doing this to prevent Enter key from closing the program right away.
                Console.ReadKey(True)

                Console.WriteLine("")
                Console.WriteLine("Total scans: " & totScans)
                Console.WriteLine("Skipped scans: " & _
                                  (skippedTotal / numAddresses))
                time = sw.ElapsedMilliseconds / 1000.0
                Console.WriteLine("Time taken: " & time & " seconds")
                Console.WriteLine("LJM Scan Rate: " & scanRate & _
                                  " scans/second")
                Console.WriteLine("Timed Scan Rate: " & _
                                  (totScans / time).ToString("F2") & _
                                  " scans/second")
                Console.WriteLine("Sample Rate: " & _
                                  (totScans * numAddresses / time).ToString("F2") & _
                                  " samples/second")
            Catch ljme As LJM.LJMException
                showErrorMessage(ljme)
            End Try

            Console.WriteLine("Stop Stream")
            LJM.eStreamStop(handle)
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
