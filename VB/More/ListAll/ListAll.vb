'------------------------------------------------------------------------------
' ListAll.vb
'
' Demonstrates usage of the ListAll functions (LJM_ListAll) which scans for
' LabJack devices and returns information about the found devices. This
' will only find LabJack devices supported by the LJM library.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module ListAll

    Sub Main()
        Dim DEVICE_NAMES As New Dictionary(Of Integer, String)
        Dim CONN_NAMES As New Dictionary(Of Integer, String)
        Const MAX_SIZE As Integer = LJM.CONSTANTS.LIST_ALL_SIZE

        Dim numFound As Integer = 0
        Dim aDeviceTypes(MAX_SIZE) As Integer
        Dim aConnectionTypes(MAX_SIZE) As Integer
        Dim aSerialNumbers(MAX_SIZE) As Integer
        Dim aIPAddresses(MAX_SIZE) As Integer
        Dim dev As String = ""
        Dim con As String = ""
        Dim ip As String = ""
        Dim nl As String = Environment.NewLine

        DEVICE_NAMES.Add(LJM.CONSTANTS.dtT7, "T7")
        DEVICE_NAMES.Add(LJM.CONSTANTS.dtT4, "T4")
        DEVICE_NAMES.Add(LJM.CONSTANTS.dtDIGIT, "Digit")

        CONN_NAMES.Add(LJM.CONSTANTS.ctUSB, "USB")
        CONN_NAMES.Add(LJM.CONSTANTS.ctTCP, "TCP")
        CONN_NAMES.Add(LJM.CONSTANTS.ctETHERNET, "Ethernet")
        CONN_NAMES.Add(LJM.CONSTANTS.ctWIFI, "WiFi")

        Try
            ' Find and display LabJack devices with listAllS.
            LJM.ListAllS("ANY", "ANY", numFound, aDeviceTypes, aConnectionTypes, aSerialNumbers, aIPAddresses)
            Console.WriteLine("ListAllS found " & numFound & " LabJacks:" & nl)

            ' Find and display LabJack devices with listAll.
            'LJM.ListAll(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, numFound, aDeviceTypes, aConnectionTypes, aSerialNumbers, aIPAddresses)
            'Console.WriteLine("ListAll found " & numFound & " LabJacks:" & nl)

            Console.WriteLine(String.Format("{0, -18}{1, -18}{2, -18}{3, -18}", "Device Type", "Connection Type", "Serial Number", "IP Address"))
            For i As Integer = 0 To numFound - 1
                If DEVICE_NAMES.TryGetValue(aDeviceTypes(i), dev) = False Then
                    dev = aDeviceTypes(i).ToString()
                End If
                If CONN_NAMES.TryGetValue(aConnectionTypes(i), con) = False Then
                    con = aConnectionTypes(i).ToString()
                End If
                LJM.NumberToIP(aIPAddresses(i), ip)
                Console.WriteLine(String.Format("{0, -18}{1, -18}{2, -18}{3, -18}", dev, con, aSerialNumbers(i), ip))
            Next
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        LJM.CloseAll()  ' Close all handles

        Console.WriteLine(nl & "Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine()  ' Pause for user
    End Sub

End Module
