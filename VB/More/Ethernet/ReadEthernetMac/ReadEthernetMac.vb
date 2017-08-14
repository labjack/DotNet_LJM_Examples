'------------------------------------------------------------------------------
' ReadEthernetMac.vb
'
' Demonstrates how to read the ethernet MAC from a LabJack.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module ReadEthernetMac

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
        Dim aBytes(7) As Byte
        Dim errAddr As Integer = -1

        Dim macBytes(7) As Byte
        Dim macNumber As Int64
        Dim macString As String = ""

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)

            ' Call eReadAddressByteArray to read the ethernet MAC. We are
            ' reading a byte array which is the big endian binary
            ' representation of the 64-bit MAC.
            LJM.eReadAddressByteArray(handle, 60020, 8, aBytes, errAddr)

            ' Convert big endian byte array to a 64-bit unsigned integer value
            If BitConverter.IsLittleEndian Then
                Array.Reverse(aBytes)
            End If

            macNumber = BitConverter.ToInt64(aBytes, 0)

            ' Convert the MAC value/number to its string representation
            macString = ""
            LJM.NumberToMAC(macNumber, macString)

            Console.WriteLine("")
            Console.WriteLine("Ethernet MAC : " & macNumber & " - " & _
                              macString)
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
