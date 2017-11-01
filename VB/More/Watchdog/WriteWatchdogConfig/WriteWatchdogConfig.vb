'------------------------------------------------------------------------------
' WriteWatchdogConfig.vb
'
' Demonstrates how to configure the Watchdog on a LabJack.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module WriteWatchdogConfig

    Sub Main()
        Dim handle As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)

            ' Setup and call eWriteNames to configure the Watchdog on a
            ' LabJack. Disable the Watchdog first before any other
            ' configuration.
            numFrames = 16
            ReDim aNames(numFrames - 1)
            aNames(0) = "WATCHDOG_ENABLE_DEFAULT"
            aNames(1) = "WATCHDOG_ADVANCED_DEFAULT"
            aNames(2) = "WATCHDOG_TIMEOUT_S_DEFAULT"
            aNames(3) = "WATCHDOG_STARTUP_DELAY_S_DEFAULT"
            aNames(4) = "WATCHDOG_STRICT_ENABLE_DEFAULT"
            aNames(5) = "WATCHDOG_STRICT_KEY_DEFAULT"
            aNames(6) = "WATCHDOG_RESET_ENABLE_DEFAULT"
            aNames(7) = "WATCHDOG_DIO_ENABLE_DEFAULT"
            aNames(8) = "WATCHDOG_DIO_STATE_DEFAULT"
            aNames(9) = "WATCHDOG_DIO_DIRECTION_DEFAULT"
            aNames(10) = "WATCHDOG_DIO_INHIBIT_DEFAULT"
            aNames(11) = "WATCHDOG_DAC0_ENABLE_DEFAULT"
            aNames(12) = "WATCHDOG_DAC0_DEFAULT"
            aNames(13) = "WATCHDOG_DAC1_ENABLE_DEFAULT"
            aNames(14) = "WATCHDOG_DAC1_DEFAULT"
            aNames(15) = "WATCHDOG_ENABLE_DEFAULT"
            ReDim aValues(numFrames - 1)
            aValues(0) = 0
            aValues(1) = 0
            aValues(2) = 20
            aValues(3) = 0
            aValues(4) = 0
            aValues(5) = 0
            aValues(6) = 1
            aValues(7) = 0
            aValues(8) = 0
            aValues(9) = 0
            aValues(10) = 0
            aValues(11) = 0
            aValues(12) = 0
            aValues(13) = 0
            aValues(14) = 0
            aValues(15) = 0  ' Set WATCHDOG_ENABLE_DEFAULT to 1 to enable
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Set Watchdog configuration:")
            For i = 0 To numFrames - 1
                Console.WriteLine("    " & aNames(i) & " : " & aValues(i))
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
