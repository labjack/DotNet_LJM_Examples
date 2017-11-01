'------------------------------------------------------------------------------
' WriteWifiConfig.vb
'
' Demonstrates how to configure the WiFi settings on a LabJack.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module WriteWifiConfig

    Sub Main()
        Dim handle As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1
        Dim name As String
        Dim str As String
        Dim value As Double
        Dim intVal As Integer
        Dim ip As Integer = 0
        Dim subnet As Integer = 0
        Dim gateway As Integer = 0
        Dim dhcpEnable As Integer = 0
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

            ' Setup and call eWriteNames to configure WiFi default settings.
            numFrames = 3
            ReDim aNames(numFrames - 1)
            aNames(0) = "WIFI_IP_DEFAULT"
            aNames(1) = "WIFI_SUBNET_DEFAULT"
            aNames(2) = "WIFI_GATEWAY_DEFAULT"
            LJM.IPToNumber("192.168.1.207", ip)
            LJM.IPToNumber("255.255.255.0", subnet)
            LJM.IPToNumber("192.168.1.1", gateway)
            ReDim aValues(numFrames - 1)
            aValues(0) = Convert.ToDouble(BitConverter.ToUInt32(BitConverter.GetBytes(ip), 0))
            aValues(1) = Convert.ToDouble(BitConverter.ToUInt32(BitConverter.GetBytes(subnet), 0))
            aValues(2) = Convert.ToDouble(BitConverter.ToUInt32(BitConverter.GetBytes(gateway), 0))
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Set WiFi configuration:")
            str = ""
            For i = 0 To numFrames - 1
                intVal = BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToUInt32(aValues(i))), 0)
                LJM.NumberToIP(intVal, str)
                Console.WriteLine("    " & aNames(i) & " : " & aValues(i) & _
                                  " - " & str)
            Next

            ' Setup and call eWriteString to configure the default WiFi SSID.
            name = "WIFI_SSID_DEFAULT"
            str = "LJOpen"
            LJM.eWriteNameString(handle, name, str)
            Console.WriteLine("    " & name & " : " & str)

            ' Setup and call eWriteString to configure the default WiFi
            ' password.
            name = "WIFI_PASSWORD_DEFAULT"
            str = "none"
            LJM.eWriteNameString(handle, name, str)
            Console.WriteLine("    " & name & " : " & str)

            ' Setup and call eWriteName to apply the new WiFi configuration.
            name = "WIFI_APPLY_SETTINGS"
            value = 1  ' 1 = apply
            LJM.eWriteName(handle, name, value)
            Console.WriteLine("    " & name & " : " & value)
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
