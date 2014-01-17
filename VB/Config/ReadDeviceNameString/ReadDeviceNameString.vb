﻿'------------------------------------------------------------------------------
' ReadDeviceNameString.vb
'
' Demonstrates how to read the device name string.
'
' support@labjack.com
' Jan. 15, 2014
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack

Module ReadDeviceNameString

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
        Dim str As String = ""

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)

            ' Call eReadNameString to read the name string.
            LJM.eReadNameString(handle, "DEVICE_NAME_DEFAULT", str)

            Console.WriteLine("")
            Console.WriteLine("Device name : " & str)
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine() ' Pause for user
    End Sub

End Module
