'------------------------------------------------------------------------------
' DualAINLoop.vb
'
' Demonstrates reading 2 analog inputs (AINs) in a loop.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports System.Threading
Imports LabJack


Module DualAINLoop

    Sub Main()
        Dim handle As Integer
        Dim devType As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1
        Dim intervalHandle As Integer
        Dim skippedIntervals As Integer = 0

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            ' Setup and call eWriteNames to configure AINs.
            If devType = LJM.CONSTANTS.dtT4 Then
                ' LabJack T4 configuration

                ' AIN0 and AIN1:
                '     Range = +/-10 V. Only AIN0-AIN3 support the +/-10 V range.
                '     Resolution index = 0 (default)
                '     Settling = 0 (auto)
                numFrames = 6
                ReDim aNames(numFrames - 1)
                aNames(0) = "AIN0_RANGE"
                aNames(1) = "AIN0_RESOLUTION_INDEX"
                aNames(2) = "AIN0_SETTLING_US"
                aNames(3) = "AIN1_RANGE"
                aNames(4) = "AIN1_RESOLUTION_INDEX"
                aNames(5) = "AIN1_SETTLING_US"
                ReDim aValues(numFrames - 1)
                aValues(0) = 10
                aValues(1) = 0
                aValues(2) = 0
                aValues(3) = 10
                aValues(4) = 0
                aValues(5) = 0
            Else
                ' LabJack T7 and other devices configuration

                ' AIN0 and AIN1:
                '     Negative Channel = 199 (Single-ended)
                '     Range = +/-10 V
                '     Resolution index = 0 (default)
                '     Settling = 0 (auto)
                numFrames = 8
                ReDim aNames(numFrames - 1)
                aNames(0) = "AIN0_NEGATIVE_CH"
                aNames(1) = "AIN0_RANGE"
                aNames(2) = "AIN0_RESOLUTION_INDEX"
                aNames(3) = "AIN0_SETTLING_US"
                aNames(4) = "AIN1_NEGATIVE_CH"
                aNames(5) = "AIN1_RANGE"
                aNames(6) = "AIN1_RESOLUTION_INDEX"
                aNames(7) = "AIN1_SETTLING_US"
                ReDim aValues(numFrames - 1)
                aValues(0) = 199
                aValues(1) = 10
                aValues(2) = 0
                aValues(3) = 0
                aValues(4) = 199
                aValues(5) = 10
                aValues(6) = 0
                aValues(7) = 0
            End If
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Set configuration:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  " & aNames(i) & " : " & aValues(i))
            Next

            ' Setup and call eReadNames to read AINs.
            numFrames = 2
            ReDim aNames(numFrames - 1)
            aNames(0) = "AIN0"
            aNames(1) = "AIN1"
            ReDim aValues(numFrames - 1)
            aValues(0) = 0
            aValues(1) = 0

            Console.WriteLine("")
            Console.WriteLine("Starting read loop.  Press a key to stop.")
            intervalHandle = 1
            LJM.StartInterval(intervalHandle, 1000000)
            While Console.KeyAvailable = False
                LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)
                Console.WriteLine("")
                Console.WriteLine(aNames(0) & " : " & _
                                  aValues(0).ToString("F4") & " V, " & _
                                  aNames(1) & " : " & _
                                  aValues(1).ToString("F4") & " V")
                LJM.WaitForNextInterval(intervalHandle, skippedIntervals)  ' Wait 1 second
                If skippedIntervals > 0 Then
                    Console.WriteLine("SkippedIntervals: " & skippedIntervals)
                End If
            End While
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        ' Close interval and all device handles
        LJM.CleanInterval(intervalHandle)
        LJM.CloseAll()

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine() ' Pause for user
    End Sub

End Module
