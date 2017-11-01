'------------------------------------------------------------------------------
' ReadWifiConfig.vb
'
' Demonstrates how to read the WiFi configuration.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module ReadWifiConfig

    Sub Main()
        Dim handle As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1
        Dim name As String
        Dim str As String
        Dim intVal As Integer
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

            ' Setup and call eReadNames to read WiFi configuration.
            numFrames = 9
            ReDim aNames(numFrames - 1)
            aNames(0) = "WIFI_IP"
            aNames(1) = "WIFI_SUBNET"
            aNames(2) = "WIFI_GATEWAY"
            aNames(3) = "WIFI_DHCP_ENABLE"
            aNames(4) = "WIFI_IP_DEFAULT"
            aNames(5) = "WIFI_SUBNET_DEFAULT"
            aNames(6) = "WIFI_GATEWAY_DEFAULT"
            aNames(7) = "WIFI_DHCP_ENABLE_DEFAULT"
            aNames(8) = "WIFI_STATUS"
            ReDim aValues(numFrames - 1)
            LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("eWiFi configuration:")
            str = ""
            For i = 0 To numFrames - 1
                If aNames(i) = "WIFI_STATUS" Or aNames(i).StartsWith("WIFI_DHCP_ENABLE") Then
                    Console.WriteLine("    " & aNames(i) & " : " & aValues(i))
                Else
                    intVal = BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToUInt32(aValues(i))), 0)
                    LJM.NumberToIP(intVal, str)
                    Console.WriteLine("    " & aNames(i) & " : " & _
                                      aValues(i) & " - " & str)
                End If
            Next

            ' Setup and call eReadNameString to read the WiFi SSID string.
            name = "WIFI_SSID"
            str = ""
            LJM.eReadNameString(handle, name, str)

            Console.WriteLine("    " & name & " : " & str)
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

Done:
        LJM.CloseAll()

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine()  ' Pause for user
    End Sub

End Module
