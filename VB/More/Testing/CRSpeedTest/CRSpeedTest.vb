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

    Sub Main()
        Dim handle As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aAddresses() As Integer
        Dim aTypes() As Integer
        Dim aWrites() As Integer
        Dim aNumValues() As Integer
        Dim aValues() As Double
        Dim errAddr As Integer = -1

        Dim i As Integer
        Dim wrStr As String
        Dim devType As Integer

        Dim dioInhibit As Integer  ' T4 DIO inhibit setting
        Dim dioAnalogEnable As Integer  ' T4 DIO analog enable setting

        Const numIterations As Integer = 1000  ' Number of iterations to perform in the loop


        Const numAIN As Integer = 1  ' Number of analog inputs to read
        Const rangeAIN As Double = 10.0  ' T7 AIN range
        Const rangeAINHV As Double = 10.0  ' T4 HV channels range
        Const rangeAINLV As Double = 2.5  ' T4 LV channels range
        Const resolutionAIN As Double = 1.0

        ' Digital settings
        Const readDigital As Boolean = False
        Const writeDigital As Boolean = False

        ' Analog output settings
        Const writeDACs As Boolean = False

        ' Use eAddresses (True) or eNames (False) in the operations loop.
        ' eAddresses is faster than eNames.
        Const useAddresses As Boolean = True

        ' Time variables
        Dim maxMS As Double = 0
        Dim minMS As Double = 0
        Dim totalMS As Double = 0
        Dim curMS As Double = 0
        Dim sw As Stopwatch
        Dim freq As Long = Stopwatch.Frequency

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)


            If devType = LJM.CONSTANTS.dtT4 Then
                ' For the T4, configure the channels to analog input or
                ' digital I/O.

                ' Update all digital I/O channels. b1 = Ignored. b0 = Affected.
                dioInhibit = 0  ' b00000000000000000000
                ' Set AIN 0 to numAIN-1 as analog inputs (b1), the rest as
                ' digital I/O (b0).
                dioAnalogEnable = Math.Pow(2, numAIN) - 1
                ReDim aNames(1)
                aNames(0) = "DIO_INHIBIT"
                aNames(1) = "DIO_ANALOG_ENABLE"
                ReDim aValues(1)
                aValues(0) = dioInhibit
                aValues(1) = dioAnalogEnable
                LJM.eWriteNames(handle, 2, aNames, aValues, errAddr)
                If writeDigital = True Then
                    ' Update only digital I/O channels in future digital write
                    ' calls. b1 = Ignored. b0 = Affected.
                    dioInhibit = dioAnalogEnable
                    LJM.eWriteName(handle, "DIO_INHIBIT", dioInhibit)
                End If
            End If

            If numAIN > 0 Then
                ' Configure analog input settings
                numFrames = Math.Max(0, numAIN * 2)
                ReDim aNames(numFrames - 1)
                ReDim aValues(numFrames - 1)
                For i = 0 To numAIN - 1
                    aNames(i * 2) = "AIN" + i.ToString() + "_RANGE"
                    If devType = LJM.CONSTANTS.dtT4 Then
                        ' T4 range
                        If i < 4 Then
                            aValues(i * 2) = rangeAINHV  ' HV line
                        Else
                            aValues(i * 2) = rangeAINLV  ' LV line
                        End If
                    Else
                        ' T7 range
                        aValues(i * 2) = rangeAIN
                    End If
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
            ReDim aValues(numFrames - 1)  ' In this case numFrames is the size of aValue

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
                aValues(i) = 0  ' output-low
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

            ' Make arrays of addresses and data types for eAddresses.
            ReDim aAddresses(numFrames - 1)
            ReDim aTypes(numFrames - 1)
            LJM.NamesToAddresses(numFrames, aNames, aAddresses, aTypes)

            Console.WriteLine("")
            Console.WriteLine("Test frames:")

            wrStr = ""
            For i = 0 To numFrames - 1
                If aWrites(i) = LJM.CONSTANTS.READ Then
                    wrStr = "READ"
                Else
                    wrStr = "WRITE"
                End If
                Console.WriteLine("    " & wrStr & " " & aNames(i) & " (" & _
                                  aAddresses(i) & ")")
            Next
            Console.WriteLine("")
            Console.WriteLine("Beginning " & numIterations & " iterations...")


            ' eNames operations loop
            For i = 0 To numIterations - 1
                sw = Stopwatch.StartNew()
                If useAddresses Then
                    LJM.eAddresses(handle, numFrames, aAddresses, aTypes, _
                                   aWrites, aNumValues, aValues, errAddr)
                Else
                    LJM.eNames(handle, numFrames, aNames, aWrites, aNumValues, _
                               aValues, errAddr)
                End If
                sw.Stop()

                curMS = sw.ElapsedTicks / freq * 1000
                If minMS = 0 Then
                    minMS = curMS
                End If
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
            If useAddresses Then
                Console.WriteLine("Last eAddresses results: ")
            Else
                Console.WriteLine("Last eNames results: ")
            End If
            For i = 0 To numFrames - 1
                If aWrites(i) = LJM.CONSTANTS.READ Then
                    wrStr = "READ"
                Else
                    wrStr = "WRITE"
                End If
                Console.WriteLine("    " & aNames(i) & " (" & aAddresses(i) & _
                                  ") " & wrStr & " value : " & aValues(i))
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
