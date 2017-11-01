'------------------------------------------------------------------------------
' ReadWifiRssi.vb
'
' Demonstrates how to read the WiFi RSSI.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module ReadWifiRssi

    Sub Main()
        Dim handle As Integer
        Dim name As String
        Dim value As Double
        Dim devType As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)

            devType = getDeviceType(handle)
            If devType = LJM.CONSTANTS.dtT4 Then
                Console.WriteLine("")
                Console.WriteLine("The LabJack T4 does not support WiFi.")
                GoTo Done
            End If

            ' Setup and call eReadName to read the WiFi RSSI.
            name = "WIFI_RSSI"
            value = 0
            LJM.eReadName(handle, name, value)

            Console.WriteLine("")
            Console.WriteLine(name & " : " & value)
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
