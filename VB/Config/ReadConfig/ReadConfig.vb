'------------------------------------------------------------------------------
' ReadConfig.vb
'
' Demonstrates how to read configuration settings.
'
' support@labjack.com
' Jan. 15, 2014
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

    Sub Main()
        Dim handle As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)

            ' Setup and call eReadNames to read configuration values from the
            ' LabJack.
            numFrames = 10
            ReDim aNames(numFrames)
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
            ReDim aValues(numFrames)
            LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Configuration settings:")
            For i = 0 To numFrames - 1
                Console.WriteLine("    " & aNames(i) & " : " & aValues(i).ToString("0.####"))
            Next
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine() ' Pause for user
    End Sub

End Module
