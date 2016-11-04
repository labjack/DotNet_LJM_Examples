'------------------------------------------------------------------------------
' ReadConfig.vb
'
' Demonstrates how to read configuration settings.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack

Module ReadConfig

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
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer
        Dim devType As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            ' Setup and call eReadNames to read configuration values from the
            ' LabJack.
            If devType = LJM.CONSTANTS.dtT4 Then
                ' LabJack T4 configuration to read
                numFrames = 8
                ReDim aNames(numFrames - 1)
                aNames(0) = "PRODUCT_ID"
                aNames(1) = "HARDWARE_VERSION"
                aNames(2) = "FIRMWARE_VERSION"
                aNames(3) = "BOOTLOADER_VERSION"
                aNames(4) = "SERIAL_NUMBER"
                aNames(5) = "POWER_ETHERNET_DEFAULT"
                aNames(6) = "POWER_AIN_DEFAULT"
                aNames(7) = "POWER_LED_DEFAULT"
            Else
                ' LabJack T7 and other devices configuration to read
                numFrames = 10
                ReDim aNames(numFrames - 1)
                aNames(0) = "PRODUCT_ID"
                aNames(1) = "HARDWARE_VERSION"
                aNames(2) = "FIRMWARE_VERSION"
                aNames(3) = "BOOTLOADER_VERSION"
                aNames(4) = "WIFI_VERSION"
                aNames(5) = "SERIAL_NUMBER"
                aNames(6) = "POWER_ETHERNET_DEFAULT"
                aNames(7) = "POWER_WIFI_DEFAULT"
                aNames(8) = "POWER_AIN_DEFAULT"
                aNames(9) = "POWER_LED_DEFAULT"
            End If
            ReDim aValues(numFrames - 1)
            LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Configuration settings:")
            For i = 0 To numFrames - 1
                Console.WriteLine("    " & aNames(i) & " : " & aValues(i).ToString("0.####"))
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
