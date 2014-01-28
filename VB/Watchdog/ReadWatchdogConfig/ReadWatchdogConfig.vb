'------------------------------------------------------------------------------
' ReadWatchdogConfig.vb
'
' Demonstrates how to read the Watchdog configuration.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack

Module ReadWatchdogConfig

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
        Dim aValues() As Double
        Dim errAddr As Integer = -1

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)

            ' Setup and call eReadNames to read the Watchdog configuration from
            ' the LabJack.
            numFrames = 15
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
            ReDim aValues(numFrames)
            LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Watchdog configuration:")
            For i = 0 To numFrames - 1
                Console.WriteLine("    " & aNames(i) & " : " & aValues(i))
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
