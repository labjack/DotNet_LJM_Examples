'------------------------------------------------------------------------------
' ReadWifiMac.vb
'
' Demonstrates how to read the WiFi MAC.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack

Module ReadWifiMac

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
        Dim numFrames As Integer = 1
        Dim aAddresses(0) As Integer
        Dim aTypes(0) As Integer
        Dim aWrites(0) As Integer
        Dim aNumValues(0) As Integer
        Dim aValues(7) As Double
        Dim errAddr As Integer = -1

        Dim macBytes(7) As Byte
        Dim macNumber As Int64
        Dim macString As String = ""

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)

            ' Call eAddresses to read the WiFi MAC from the LabJack. Note that
            ' we are reading a byte array which is the big endian binary
            ' representation of the 64-bit MAC.
            aAddresses(0) = 60024
            aTypes(0) = LJM.CONSTANTS.BYTE
            aWrites(0) = LJM.CONSTANTS.READ
            aNumValues(0) = 8
            LJM.eAddresses(handle, numFrames, aAddresses, aTypes, aWrites, _
                           aNumValues, aValues, errAddr)

            ' Convert returned values to bytes
            For i = 0 To 7
                macBytes(i) = Convert.ToByte(aValues(i))
            Next

            ' Convert big endian byte array to a 64-bit unsigned integer value
            If BitConverter.IsLittleEndian Then
                Array.Reverse(macBytes)
            End If

            macNumber = BitConverter.ToInt64(macBytes, 0)

            ' Convert the MAC value/number to its string representation
            macString = ""
            LJM.NumberToMAC(macNumber, macString)

            Console.WriteLine("")
            Console.WriteLine("WiFi MAC : " & macNumber & " - " & _
                              macString)
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
