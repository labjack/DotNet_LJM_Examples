'------------------------------------------------------------------------------
' TriggeredStreamStart.vb
'
' Demonstrates how to stream with a LabJack T7 using triggered start on
' DIO0/FIO0.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports System.Threading
Imports LabJack


Module TriggeredStreamStart

    Sub variableStreamSleep(ByVal scansPerRead As Integer, ByVal scanRate As Integer, ByVal ljmScanBacklog As Integer)
        Const DECREASE_TOTAL As Double = 0.9
        Dim sleepFactor As Double
        Dim sleepMS As Integer
        Dim portionScansReady As Double

        portionScansReady = Convert.ToDouble(ljmScanBacklog) / Convert.ToDouble(scansPerRead)
        If portionScansReady > DECREASE_TOTAL Then
            sleepFactor = 0
        Else
            sleepFactor = (1 - portionScansReady) * DECREASE_TOTAL
        End If

        sleepMS = sleepFactor * 1000.0 * scansPerRead / Convert.ToDouble(scanRate)
        If sleepMS < 1 Then
            Return
        End If
        Thread.Sleep(sleepMS)
    End Sub

    Sub Main()
        Const SCAN_RATE As Integer = 1000  ' Scans per second

        Dim handle As Integer
        Dim devType As Integer

        Dim scansPerRead As Integer
        Dim numAddresses As Integer
        Dim aScanListNames() As String
        Dim aTypes() As Integer
        Dim aScanList() As Integer

        Dim loopCnt As UInt64
        Dim totScans As UInt64
        Dim aData() As Double
        Dim skippedTotal As UInt64
        Dim skippedCur As Integer
        Dim deviceScanBacklog As Integer
        Dim ljmScanBacklog As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            If devType = LJM.CONSTANTS.dtT4 Then
                Console.WriteLine("")
                Console.WriteLine("The LabJack T4 does not support triggered start streaming.")
                GoTo Done
            End If

            ' Stream Configuration
            scansPerRead = SCAN_RATE / 2  ' # scans returned by eStreamRead call
            numAddresses = 4
            ReDim aScanListNames(numAddresses - 1)  ' Scan list names to stream.
            aScanListNames(0) = "AIN0"
            aScanListNames(1) = "FIO_STATE"
            aScanListNames(2) = "SYSTEM_TIMER_20HZ"
            aScanListNames(3) = "STREAM_DATA_CAPTURE_16"

            ReDim aTypes(numAddresses - 1)  ' Dummy
            ReDim aScanList(numAddresses - 1)  ' Scan list addresses to stream. eStreamStart uses Modbus addresses.
            LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes)

            ' Configure LJM for unpredictable stream timing
            LJM.WriteLibraryConfigS(LJM.CONSTANTS.STREAM_SCANS_RETURN, LJM.CONSTANTS.STREAM_SCANS_RETURN_ALL_OR_NONE)
            LJM.WriteLibraryConfigS(LJM.CONSTANTS.STREAM_RECEIVE_TIMEOUT_MS, 0)

            ' 2000 sets the first stream trigger
            LJM.eWriteName(handle, "STREAM_TRIGGER_INDEX", 2000)

            ' Enabling internally-clocked stream.
            LJM.eWriteName(handle, "STREAM_CLOCK_SOURCE", 0)

            ' Clear any previous DIO0_EF settings
            LJM.eWriteName(handle, "DIO0_EF_ENABLE", 0)

            ' 5 enables a rising or falling edge to trigger stream
            LJM.eWriteName(handle, "DIO0_EF_INDEX", 5)

            ' Enable DIO0_EF
            LJM.eWriteName(handle, "DIO0_EF_ENABLE", 1)

            Try
                ' Configure and start Stream
                LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, SCAN_RATE)
                Console.WriteLine("You can trigger stream now via a rising or falling edge on DIO0/FIO0.")
                Console.WriteLine("Press a key to stop stream mode.")

                loopCnt = 0
                totScans = 0
                ReDim aData(scansPerRead * numAddresses - 1)  ' # of samples per eStreamRead is scansPerRead * numAddresses
                skippedTotal = 0
                skippedCur = 0
                deviceScanBacklog = 0
                ljmScanBacklog = 0

                While Console.KeyAvailable = False
                    Try
                        variableStreamSleep(scansPerRead, SCAN_RATE, ljmScanBacklog)
                        ' Thread.Sleep(450)
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
                        Console.Write("  First scan out of " & scansPerRead & ": ")
                        For j = 0 To numAddresses - 1
                            Console.Write(aScanListNames(j) & " = " & _
                                          aData(j).ToString("F4") & ", ")
                        Next
                        Console.WriteLine("")
                        Console.WriteLine("  numSkippedScans: " & _
                                          (skippedCur / numAddresses) & _
                                          ", deviceScanBacklog: " & _
                                          deviceScanBacklog & _
                                          ", ljmScanBacklog: " & ljmScanBacklog)
                    Catch ljme0 As LJM.LJMException
                        ' If the error is not NO_SCANS_RETURNS, throw an
                        ' exception to stop the loop
                        If ljme0.LJMError <> LJM.LJMERROR.NO_SCANS_RETURNED Then
                            Throw ljme0
                        End If
                    End Try
                End While
            Catch ljme1 As LJM.LJMException
                showErrorMessage(ljme1)
            Finally
                Try
                    Console.WriteLine("Stop stream mode")
                    LJM.eStreamStop(handle)
                Catch
                    ' Ignore stream stop exception
                End Try
            End Try
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

Done:
        LJM.CloseAll()  ' Close all handles

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine()  ' Pause for user
    End Sub

End Module
