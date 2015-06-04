'------------------------------------------------------------------------------
' StreamSequentialAIN.vb
'
' Demonstrates how to stream a range of sequential analog inputs using the eStream
' functions. Useful when streaming many analog inputs. AIN channel scan list is
' FIRST_AIN_CHANNEL to FIRST_AIN_CHANNEL + NUMBER_OF_AINS - 1.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack
Imports System.Threading

Module StreamSequentialAIN

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
        Const FIRST_AIN_CHANNEL As Integer = 0 ' 0 = AIN0
        Const NUMBER_OF_AINS As Integer = 16

        Dim handle As Integer
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

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)

            ' Stream Configuration
            scansPerRead = 1000 ' # scans returned by eStreamRead call
            scanRate = 1000 ' Scans per second
            ' Scan list names to stream. AIN(FIRST_AIN_CHANNEL) to AIN(NUMBER_OF_AINS-1).
            numAddresses = NUMBER_OF_AINS
            ReDim aScanListNames(numAddresses - 1) ' Scan list names to stream.
            For i As Integer = 0 To numAddresses - 1
                aScanListNames(i) = "AIN" + (FIRST_AIN_CHANNEL + i).ToString()
            Next
            ReDim aTypes(numAddresses - 1) ' Dummy
            ReDim aScanList(numAddresses - 1) ' Scan list addresses to stream. eStreamStart uses Modbus addresses.
            LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes)

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
                LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, scanRate)

                loopCnt = 0
                totScans = 0
                ReDim aData(scansPerRead * numAddresses - 1) ' # of samples per eStreamRead is scansPerRead * numAddresses
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

                Console.ReadKey(True) ' Doing this to prevent Enter key from closing the program right away.

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

        LJM.CloseAll() ' Close all handles

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine() ' Pause for user
    End Sub

End Module
