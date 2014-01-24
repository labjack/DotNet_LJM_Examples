'------------------------------------------------------------------------------
' CRSpeedTest.vb
'
' Performs LabJack operations in a loop and reports the timing statistics for
' the operations.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack

Module CRSpeedTest

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
        Dim aNames() As String
        Dim aWrites() As Integer
        Dim aNumValues() As Integer
        Dim aValues() As Double
        Dim errAddr As Integer = -1

        Dim i As Integer
        Dim wrStr As String

        Const numIterations As Integer = 1000 ' Number of iterations to perform in the loop

        Const numAIN As Integer = 1 ' Number of analog inputs to read
        Const rangeAIN As Double = 10.0
        Const resolutionAIN As Double = 1.0

        ' Digital settings
        Const readDigital As Boolean = False
        Const writeDigital As Boolean = False

        ' Analog output settings
        Const writeDACs As Boolean = False

        ' Time variables
        Dim maxMS As Double = 0
        Dim minMS As Double = 0
        Dim totalMS As Double = 0
        Dim curMS As Double = 0
        Dim sw As Stopwatch
        Dim freq As Long = Stopwatch.Frequency

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)


            If numAIN > 0 Then
                ' Configure analog input settings
                numFrames = Math.Max(0, numAIN * 2)
                ReDim aNames(numFrames - 1)
                ReDim aValues(numFrames - 1)
                For i = 0 To numAIN - 1
                    aNames(i * 2) = "AIN" + i.ToString() + "_RANGE"
                    aValues(i * 2) = rangeAIN
                    aNames(i * 2 + 1) = "AIN" + i.ToString() + _
                                        "_RESOLUTION_INDEX"
                    aValues(i * 2 + 1) = resolutionAIN
                Next
                LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)
            End If

            ' Initialize and configure eNames parameters for loop's eNames call
            numFrames = Math.Max(0, numAIN) + Convert.ToInt32(readDigital) + _
                        Convert.ToInt32(writeDigital) + _
                        Convert.ToInt32(writeDACs) * 2
            ReDim aNames(numFrames - 1)
            ReDim aWrites(numFrames - 1)
            ReDim aNumValues(numFrames - 1)
            ReDim aValues(numFrames - 1) ' In this case numFrames is the size of aValue

            ' Add analog input reads (AIN 0 to numAIN-1)
            For i = 0 To numAIN - 1
                aNames(i) = "AIN" + i.ToString()
                aWrites(i) = LJM.CONSTANTS.READ
                aNumValues(i) = 1
                aValues(i) = 0
            Next

            If readDigital Then
                ' Add digital read
                aNames(i) = "DIO_STATE"
                aWrites(i) = LJM.CONSTANTS.READ
                aNumValues(i) = 1
                aValues(i) = 0
                i += 1
            End If

            If writeDigital Then
                ' Add digital write
                aNames(i) = "DIO_STATE"
                aWrites(i) = LJM.CONSTANTS.WRITE
                aNumValues(i) = 1
                aValues(i) = 0 ' output-low
                i += 1
            End If

            If writeDACs Then
                ' Add analog output writes (DAC0-1)
                For j = 0 To 1
                    aNames(i) = "DAC" + j.ToString()
                    aWrites(i) = LJM.CONSTANTS.WRITE
                    aNumValues(i) = 1
                    aValues(i) = 0.0 ' 0.0 V
                    i += 1
                Next
            End If

                    Console.WriteLine("")
                    Console.WriteLine("Test frames:")

            wrStr = ""
            For i = 0 To numFrames - 1
                If aWrites(i) = LJM.CONSTANTS.READ Then
                    wrStr = "READ"
                Else
                    wrStr = "WRITE"
                End If
                Console.WriteLine("    " & wrStr & " " & aNames(i))
            Next
            Console.WriteLine("")
            Console.WriteLine("Beginning " & numIterations & " iterations...")


            ' eNames operations loop
            For i = 0 To numIterations - 1
                sw = Stopwatch.StartNew()
                LJM.eNames(handle, numFrames, aNames, aWrites, aNumValues, _
                           aValues, errAddr)
                sw.Stop()

                curMS = sw.ElapsedTicks / freq * 1000
                If minMS = 0 Then minMS = curMS
                minMS = Math.Min(curMS, minMS)
                maxMS = Math.Max(curMS, maxMS)
                totalMS += curMS
            Next

            Console.WriteLine("")
            Console.WriteLine(numIterations & " iterations performed:")
            Console.WriteLine("    Time taken: " & totalMS.ToString("F3") & _
                              " ms")
            Console.WriteLine("    Average time per iteration: " & _
                (totalMS / numIterations).ToString("F3") & " ms")
            Console.WriteLine("    Min / Max time for one iteration: " & _
                minMS.ToString("F3") & " ms / " & maxMS.ToString("F3") & _
                " ms")

            Console.WriteLine("")
            Console.WriteLine("Last eNames results: ")
            For i = 0 To numFrames - 1
                If aWrites(i) = LJM.CONSTANTS.READ Then
                    wrStr = "READ"
                Else
                    wrStr = "WRITE"
                End If
                Console.WriteLine("    " & aNames(i) & " " & wrStr & _
                                  " value : " & aValues(i).ToString("0.####"))
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
