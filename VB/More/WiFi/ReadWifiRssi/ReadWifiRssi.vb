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

    Function getDeviceType(ByVal handle As Integer)
        Dim devType As Integer
        Dim conType As Integer
        Dim serNum As Integer
        Dim ipAddr As Integer
        Dim port As Integer
        Dim maxBytesPerMB As Integer

        LJM.GetHandleInfo(handle, devType, conType, serNum, ipAddr, port, _
                          maxBytesPerMB)
        Return devType
    End Function

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
