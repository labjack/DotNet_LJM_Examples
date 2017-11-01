'------------------------------------------------------------------------------
' ReadEthernetConfig.vb
'
' Demonstrates how to read the ethernet configuration settings from a LabJack.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module ReadEthernetConfig

    Sub Main()
        Dim handle As Integer
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aValues() As Double
        Dim errAddr As Integer = -1
        Dim str As String
        Dim intVal As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)

            ' Setup and call eReadNames to read ethernet configuration.
            numFrames = 8
            ReDim aNames(numFrames - 1)
            aNames(0) = "ETHERNET_IP"
            aNames(1) = "ETHERNET_SUBNET"
            aNames(2) = "ETHERNET_GATEWAY"
            aNames(3) = "ETHERNET_IP_DEFAULT"
            aNames(4) = "ETHERNET_SUBNET_DEFAULT"
            aNames(5) = "ETHERNET_GATEWAY_DEFAULT"
            aNames(6) = "ETHERNET_DHCP_ENABLE"
            aNames(7) = "ETHERNET_DHCP_ENABLE_DEFAULT"
            ReDim aValues(numFrames - 1)
            LJM.eReadNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Ethernet configuration: ")
            str = ""
            For i = 0 To numFrames - 1
                If aNames(i).StartsWith("ETHERNET_DHCP_ENABLE") Then
                    Console.WriteLine("    " & aNames(i) & " : " & aValues(i))
                Else
                    intVal = BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToUInt32(aValues(i))), 0)
                    LJM.NumberToIP(intVal, str)
                    Console.WriteLine("    " & aNames(i) & " : " & _
                                      aValues(i) & " - " & str)
                End If
            Next
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        LJM.CloseAll()

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine()  ' Pause for user
    End Sub

End Module
