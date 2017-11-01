'------------------------------------------------------------------------------
' WritePowerConfig.vb
'
' Demonstrates how to configure default power settings.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module WritePowerConfig

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

            ' Setup and call eWriteNames to write configuration values to
            ' the LabJack.
            numFrames = 4
            ReDim aNames(numFrames - 1)
            aNames(0) = "POWER_ETHERNET_DEFAULT"
            aNames(1) = "POWER_WIFI_DEFAULT"
            aNames(2) = "POWER_AIN_DEFAULT"
            aNames(3) = "POWER_LED_DEFAULT"
            ReDim aValues(numFrames - 1)
            aValues(0) = 1  ' Ethernet On
            aValues(1) = 0  ' WiFi Off
            aValues(2) = 1  ' AIN On
            aValues(3) = 1  ' LED On
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Set configuration settings:")

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
