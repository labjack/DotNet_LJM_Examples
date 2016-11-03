'------------------------------------------------------------------------------
' SingleAINWithConfig.vb
'
' Demonstrates configuring and reading a single analog input (AIN).
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module SingleAINWithConfig

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
        Dim name As String
        Dim value As Double

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)

            ' Setup and call eWriteNames to configure the AIN.
            ' AIN0:
            '     Negative Channel = 199 (Single-ended)
            '     Range = +/-10 V
            '         T4 note: Only AIN0-AIN3 can support +/-10 V range.
            '     Resolution index = 0 (default)
            '         T7 note: 0 (default) is index 8, or 9 for Pro.
            '     Settling = 0 (auto)
            numFrames = 4
            ReDim aNames(numFrames - 1)
            aNames(0) = "AIN0_NEGATIVE_CH"
            aNames(1) = "AIN0_RANGE"
            aNames(2) = "AIN0_RESOLUTION_INDEX"
            aNames(3) = "AIN0_SETTLING_US"
            ReDim aValues(numFrames - 1)
            aValues(0) = 199
            aValues(1) = 10
            aValues(2) = 0
            aValues(3) = 0
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Set configuration:")
            For i = 0 To numFrames - 1
                Console.WriteLine("    " & aNames(i) & " : " & aValues(i))
            Next

            ' Setup and call eReadName to read an AIN.
            name = "AIN0"
            value = 0
            LJM.eReadName(handle, name, value)

            Console.WriteLine("")
            Console.WriteLine(name & " reading : " & value.ToString("F4") & _
                              " V")
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
