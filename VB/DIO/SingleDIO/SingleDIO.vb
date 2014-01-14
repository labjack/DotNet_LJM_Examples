'------------------------------------------------------------------------------
' SingleDIO.vb
'
' Demonstrates how to set and read a single digital I/O.
'
' support@labjack.com
' Jan. 13, 2014
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
        Console.WriteLine("Opened a LabJack with Device type: " & _
                          CStr(devType) & ", Connection type: " & _
                          CStr(conType) & ",")
        Console.WriteLine("Serial number: " & CStr(serNum) & _
                          ", IP address: " & ipAddrStr & ", Port: " _
                          & CStr(port) & ",")
        Console.WriteLine("Max bytes per MB: " & CStr(maxBytesPerMB))
    End Sub

    Sub Main()
        Dim handle As Integer
        Dim name As String
        Dim state As Double

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)

            displayHandleInfo(handle)

            ' Setup and call eWriteName to set the DIO state.
            name = "FIO0"
            state = 1 ' Output-low = 0, Output-high = 1
            LJM.eWriteName(handle, name, state)

            Console.WriteLine("")
            Console.WriteLine("Set " & name & " state : " & state)

            ' Setup and call eReadName to read the DIO state.
            name = "FIO1"
            state = 0
            LJM.eReadName(handle, name, state)

            Console.WriteLine("")
            Console.WriteLine(name & " state : " & state)
        Catch ljme As LJM.LJMException
            showErrorMessage(ljme)
        End Try

        Console.WriteLine("")
        Console.WriteLine("Done.")
        Console.WriteLine("Press the enter key to exit.")
        Console.ReadLine() ' Pause for user
    End Sub

End Module
