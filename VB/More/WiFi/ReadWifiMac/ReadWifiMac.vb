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

    Sub Main()
        Dim handle As Integer
        Dim aBytes(7) As Byte
        Dim errAddr As Integer = -1
        Dim macNumber As Int64
        Dim macString As String = ""
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

            ' Call eReadAddressByteArray to read the WiFi MAC (address 60024)
            ' from the LabJack. We are reading a byte array which is the big
            ' endian binary representation of the 64-bit MAC.
            LJM.eReadAddressByteArray(handle, 60024, 8, aBytes, errAddr)

            ' Convert big endian byte array to a 64-bit unsigned integer value
            If BitConverter.IsLittleEndian Then
                Array.Reverse(aBytes)
            End If

            macNumber = BitConverter.ToInt64(aBytes, 0)

            ' Convert the MAC value/number to its string representation
            macString = ""
            LJM.NumberToMAC(macNumber, macString)

            Console.WriteLine("")
            Console.WriteLine("WiFi MAC : " & macNumber & " - " & _
                              macString)
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
