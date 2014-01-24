﻿'------------------------------------------------------------------------------
' WriteEthernetConfig.vb
'
' Demonstrates how to set ethernet configuration settings.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack

Module WriteEthernetConfig

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
        Dim errAddr As Integer = -1
        Dim str As String
        Dim intVal As Integer
        Dim ip As Integer = 0
        Dim subnet As Integer = 0
        Dim gateway As Integer = 0
        Dim dhcpEnable As Integer = 0

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)

            ' Setup and call eWriteNames to set the ethernet configuration.
            numFrames = 4
            ReDim aNames(numFrames - 1)
            aNames(0) = "ETHERNET_IP_DEFAULT"
            aNames(1) = "ETHERNET_SUBNET_DEFAULT"
            aNames(2) = "ETHERNET_GATEWAY_DEFAULT"
            aNames(3) = "ETHERNET_DHCP_ENABLE_DEFAULT"
            LJM.IPToNumber("192.168.1.207", ip)
            LJM.IPToNumber("255.255.255.0", subnet)
            LJM.IPToNumber("192.168.1.1", gateway)
            dhcpEnable = 1 ' 1 = Enable, 0 = Disable
            ReDim aValues(numFrames - 1)
            aValues(0) = Convert.ToDouble(BitConverter.ToUInt32(BitConverter.GetBytes(ip), 0))
            aValues(1) = Convert.ToDouble(BitConverter.ToUInt32(BitConverter.GetBytes(subnet), 0))
            aValues(2) = Convert.ToDouble(BitConverter.ToUInt32(BitConverter.GetBytes(gateway), 0))
            aValues(3) = Convert.ToDouble(BitConverter.ToUInt32(BitConverter.GetBytes(dhcpEnable), 0))
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errAddr)

            Console.WriteLine("")
            Console.WriteLine("Set ethernet configuration:")
            str = ""
            For i = 0 To numFrames - 1
                If aNames(i).StartsWith("ETHERNET_DHCP_ENABLE_DEFAULT") Then
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
        Console.ReadLine() ' Pause for user
    End Sub

End Module
