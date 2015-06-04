﻿'------------------------------------------------------------------------------
' StreamBasicWithStreamOut.vb
'
' Demonstrates setting up stream-in and stream-out together, then reading
' stream-in values.
'
' Connect a wire from AIN0 to DAC0 to see the effect of stream-out on stream-in
' channel 0.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack
Imports System.Threading

Module StreamBasicWithStreamOut

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
        Const MAX_REQUESTS As Integer = 50  'The number of eStreamRead calls that will be performed.

        Dim handle As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim value As Double
        Dim errAddr As Integer = -1

        Dim scansPerRead As Integer
        Dim numAddressesOut As Integer
        Dim numAddressesIn As Integer
        Dim outNames() As String
        Dim outAddresses() As Integer
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

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)


            ' Setup Stream Out
            numAddressesOut = 1
            ReDim outNames(numAddressesOut - 1)
            outNames(0) = "DAC0"
            ReDim outAddresses(numAddressesOut - 1)
            ReDim aTypes(numAddressesOut - 1) ' Dummy
            LJM.NamesToAddresses(numAddressesOut, outNames, outAddresses, aTypes)

            ' Allocate memory for the stream-out buffer
            LJM.eWriteName(handle, "STREAM_OUT0_TARGET", outAddresses(0))
            LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_SIZE", 512)
            LJM.eWriteName(handle, "STREAM_OUT0_ENABLE", 1)

            ' Write values to the stream-out buffer
            LJM.eWriteName(handle, "STREAM_OUT0_LOOP_SIZE", 6)
            LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_F32", 0.0)  ' 0.0 V
            LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_F32", 1.0)  ' 1.0 V
            LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_F32", 2.0)  ' 2.0 V
            LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_F32", 3.0)  ' 3.0 V
            LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_F32", 4.0)  ' 4.0 V
            LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_F32", 5.0)  ' 5.0 V

            LJM.eWriteName(handle, "STREAM_OUT0_SET_LOOP", 1)

            value = 0.0
            LJM.eReadName(handle, "STREAM_OUT0_BUFFER_STATUS", value)
            Console.WriteLine("\nSTREAM_OUT0_BUFFER_STATUS = " & value)

            ' Stream Configuration
            scanRate = 2000 ' Scans per second
            scansPerRead = 60 ' # scans returned by eStreamRead call
            numAddressesIn = 2
            ReDim aScanListNames(numAddressesIn - 1) ' Scan list names to stream.
            aScanListNames(0) = "AIN0"
            aScanListNames(1) = "AIN1"
            ReDim aTypes(numAddressesIn - 1) ' Dummy
            ReDim aScanList(numAddressesIn + numAddressesOut - 1) ' Scan list addresses to stream. eStreamStart uses Modbus addresses.
            LJM.NamesToAddresses(numAddressesIn, aScanListNames, aScanList, aTypes)

            ' Add the scan list outputs to the end of the scan list.
            ' STREAM_OUT0 = 4800, STREAM_OUT1 = 4801, ...
            aScanList(numAddressesIn) = 4800 ' STREAM_OUT0
            ' If we had more STREAM_OUTs
            ' aScanList(numAddressesIn + 1) = 4801  ' STREAM_OUT1
            ' etc.

            Try
                ' Configure the analog inputs' negative channel, range, settling time and
                ' resolution.
                ' Note when streaming, negative channels and ranges can be configured for
                ' individual analog inputs, but the stream has only one settling time and
                ' resolution.
                ReDim aNames(3)
                aNames(0) = "AIN_ALL_NEGATIVE_CH"
                aNames(1) = "AIN_ALL_RANGE"
                aNames(2) = "STREAM_SETTLING_US"
                aNames(3) = "STREAM_RESOLUTION_INDEX"
                ReDim aValues(3)
                aValues(0) = LJM.CONSTANTS.GND ' single-ended
                aValues(1) = 10.0 ' +/-10V
                aValues(2) = 0 ' 0 = default
                aValues(3) = 0 ' 0 = default
                LJM.eWriteNames(handle, 4, aNames, aValues, errAddr)

                Console.WriteLine("")
                Console.WriteLine("Starting stream. Press a key to stop " & _
                                  "streaming.")
                Thread.Sleep(1000) ' Delay so user's can read message

                ' Configure and start Stream
                LJM.eStreamStart(handle, scansPerRead, aScanList.Length, aScanList, scanRate)

                loopCnt = 0
                totScans = 0
                ReDim aData(scansPerRead * numAddressesIn - 1) ' # of samples per eStreamRead is scansPerRead * numAddressesIn
                skippedTotal = 0
                skippedCur = 0
                deviceScanBacklog = 0
                ljmScanBacklog = 0
                sw = New Stopwatch()

                Console.WriteLine("Starting read loop.")
                sw.Start()
                For loopCnt = 1 To MAX_REQUESTS
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
                    Console.WriteLine("")
                    Console.WriteLine("eStreamRead " & loopCnt)
                    For i As Integer = 0 To scansPerRead - 1
                        For j As Integer = 0 To numAddressesIn - 1
                            Console.Write("  " & aScanListNames(j) & " = " & aData(i * numAddressesIn + j).ToString("F4") & ",")
                        Next
                        Console.WriteLine("")
                    Next
                    Console.WriteLine("  Skipped Scans = " & _
                                      (skippedCur / numAddressesIn) & _
                                      ", Scan Backlogs: Device = " & _
                                      deviceScanBacklog & _
                                      ", LJM = " & ljmScanBacklog)
                Next
                sw.Stop()

                Console.WriteLine("")
                Console.WriteLine("Total scans: " & totScans)
                Console.WriteLine("Skipped scans: " & _
                                  (skippedTotal / numAddressesIn))
                time = sw.ElapsedMilliseconds / 1000.0
                Console.WriteLine("Time taken: " & time & " seconds")
                Console.WriteLine("LJM Scan Rate: " & scanRate & _
                                  " scans/second")
                Console.WriteLine("Timed Scan Rate: " & _
                                  (totScans / time).ToString("F2") & _
                                  " scans/second")
                Console.WriteLine("Sample Rate: " & _
                                  (totScans * numAddressesIn / time).ToString("F2") & _
                                  " samples/second")
            Catch ljme As LJM.LJMException
                showErrorMessage(ljme)
            End Try

            Console.WriteLine("Stop Stream")
            LJM.eStreamStop(handle)
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
