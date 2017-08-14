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
                ' Configure the analog inputs' negative channel, range, settling time and
                ' resolution.
                ' Note when streaming, negative channels and ranges can be configured for
                ' individual analog inputs, but the stream has only one settling time and
                ' resolution.
                ReDim aNames(4)
                aNames(0) = "AIN_ALL_NEGATIVE_CH"
                aNames(1) = "AIN0_RANGE"
                aNames(2) = "AIN1_RANGE"
                aNames(3) = "STREAM_SETTLING_US"
                aNames(4) = "STREAM_RESOLUTION_INDEX"
                ReDim aValues(4)
                aValues(0) = LJM.CONSTANTS.GND  ' single-ended
                aValues(1) = 10.0  ' +/-10V
                aValues(2) = 10.0  ' +/-10V
                aValues(3) = 0  ' 0 = default
                aValues(4) = 0  ' 0 = default
                LJM.eWriteNames(handle, 4, aNames, aValues, errAddr)

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
