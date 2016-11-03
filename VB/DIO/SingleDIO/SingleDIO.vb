﻿'------------------------------------------------------------------------------
' SingleDIO.vb
'
' Demonstrates how to set and read a single digital I/O.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module SingleDIO

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
        Dim name As String
        Dim state As Double
        Dim devType As Integer

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)
            devType = getDeviceType(handle)

            If devType = LJM.CONSTANTS.dtT4 Then
                ' Setting FIO4 on the LabJack T4. FIO0-FIO3 are reserved for
                ' AIN0-AIN3.
                name = "FIO4"

                ' If the FIO/EIO line is an analog input, it needs to first be
                ' changed to a digital I/O by reading from the line or setting
                ' it to digital I/O with the DIO_ANALOG_ENABLE register.
                ' For exanple:
                'LJM.eReadName(handle, name, state)
            Else
                ' Setting FIO0 on the LabJack T7 and other devices.
                name = "FIO0"
            End If
            state = 1  ' Output-low = 0, Output-high = 1
            LJM.eWriteName(handle, name, state)

            Console.WriteLine("")
            Console.WriteLine("Set " & name & " state : " & state)

            ' Setup and call eReadName to read the DIO state.
            If devType = LJM.CONSTANTS.dtT4 Then
                ' Reading from FIO5 on the LabJack T4. FIO0-FIO3 are reserved
                ' for AIN0-AIN3.
                ' Note: Reading a single digital I/O will change the line from
                ' analog to digital input.
                name = "FIO5"
            Else
                ' Reading from FIO1 on the LabJack T7 and other devices.
                name = "FIO1"
            End If
            state = 0
            LJM.eReadName(handle, name, state)

            Console.WriteLine("")
            Console.WriteLine(name & " state : " & state)
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
