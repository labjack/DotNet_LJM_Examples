'------------------------------------------------------------------------------
' EasyFunctions.vb
'
' Demonstrates easy functions usage. For eStream usage look at the stream
' example.
'
' support@labjack.com
'------------------------------------------------------------------------------
Option Explicit On

Imports LabJack


Module EasyFunctions

    Sub Main()
        Dim handle As Integer
        Dim name As String
        Dim address As Integer
        Dim type As Integer
        Dim value As Double
        Dim numFrames As Integer
        Dim aNames() As String
        Dim aAddresses() As Integer
        Dim aTypes() As Integer
        Dim aWrites() As Integer
        Dim aNumValues() As Integer
        Dim aValues() As Double
        Dim errorAddress As Integer = -1

        Try
            ' Open first found LabJack
            LJM.OpenS("ANY", "ANY", "ANY", handle)  ' Any device, Any connection, Any identifier
            'LJM.OpenS("T7", "ANY", "ANY", handle)  ' T7 device, Any connection, Any identifier
            'LJM.OpenS("T4", "ANY", "ANY", handle)  ' T4 device, Any connection, Any identifier
            'LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", handle)  ' Any device, Any connection, Any identifier

            displayHandleInfo(handle)

            ' Setup and call eWriteName to write a value.
            name = "DAC0"
            value = 2.5  ' 2.5 V
            LJM.eWriteName(handle, name, value)

            Console.WriteLine("")
            Console.WriteLine("eWriteName: ")
            Console.WriteLine("  Name - " & name & ", value : " & value)


            ' Setup and call eReadName to read a value.
            name = "SERIAL_NUMBER"
            LJM.eReadName(handle, name, value)

            Console.WriteLine("")
            Console.WriteLine("eReadName result: ")
            Console.WriteLine("  Name - " & name & ", value : " & value)


            ' Setup and call eWriteNames to write values.
            numFrames = 2
            ReDim aNames(numFrames - 1)
            aNames(0) = "DAC0"
            aNames(1) = "TEST_UINT16"
            ReDim aValues(numFrames - 1)
            aValues(0) = 2.5  ' 2.5 V
            aValues(1) = 12345  ' 12345
            LJM.eWriteNames(handle, numFrames, aNames, aValues, errorAddress)

            Console.WriteLine("")
            Console.WriteLine("eWriteNames:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  Name - " & aNames(i) & ", value : " & _
                                  CStr(aValues(i)))
            Next


            ' Setup and call eReadNames to read values.
            numFrames = 3
            ReDim aNames(numFrames - 1)
            aNames(0) = "SERIAL_NUMBER"
            aNames(1) = "PRODUCT_ID"
            aNames(2) = "FIRMWARE_VERSION"
            ReDim aValues(numFrames - 1)
            LJM.eReadNames(handle, numFrames, aNames, aValues, errorAddress)

            Console.WriteLine("")
            Console.WriteLine("eReadNames results:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  Name - " & aNames(i) & ", value : " & _
                                  CStr(aValues(i)))
            Next

            ' Setup and call eNames to write/read values to/from the LabJack.
            numFrames = 3
            ReDim aNames(numFrames - 1)
            aNames(0) = "DAC0"
            aNames(1) = "TEST_UINT16"
            aNames(2) = "TEST_UINT16"
            ReDim aWrites(numFrames - 1)
            aWrites(0) = LJM.CONSTANTS.WRITE
            aWrites(1) = LJM.CONSTANTS.WRITE
            aWrites(2) = LJM.CONSTANTS.READ
            ReDim aNumValues(numFrames - 1)
            aNumValues(0) = 1
            aNumValues(1) = 1
            aNumValues(2) = 1
            ReDim aValues(numFrames - 1)
            aValues(0) = 2.5  ' write 2.5 V
            aValues(1) = 12345  ' write 12345
            aValues(2) = 0  ' read
            LJM.eNames(handle, numFrames, aNames, aWrites, aNumValues, _
                       aValues, errorAddress)

            Console.WriteLine("")
            Console.WriteLine("eNames results:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  Names - " & aNames(i) + ", write -  " & _
                                  CStr(aWrites(i)) & ", values: " + _
                                  CStr(aValues(i)))
            Next

            ' Setup and call eWriteAddress to write a value.
            address = 1000  ' DAC0
            type = LJM.CONSTANTS.FLOAT32
            value = 2.5  ' 2.5 V
            LJM.eWriteAddress(handle, address, type, value)

            Console.WriteLine("")
            Console.WriteLine("eWriteAddress:")
            Console.WriteLine("  Address - " & address & ", data type - " & _
                              type & ", value : " & value)


            ' Setup and call eReadAddress to read a value.
            address = 60028  ' Serial number
            type = LJM.CONSTANTS.UINT32
            value = 0
            LJM.eReadAddress(handle, address, type, value)

            Console.WriteLine("")
            Console.WriteLine("eReadAddress result:")
            Console.WriteLine("  Address - " & address & ", data type - " & _
                              type & ", value : " & value)


            ' Setup and call eWriteAddresses to write values.
            numFrames = 2
            ReDim aAddresses(numFrames - 1)
            aAddresses(0) = 1000  ' DAC0
            aAddresses(1) = 55110  ' TEST_UINT16
            ReDim aTypes(numFrames - 1)
            aTypes(0) = LJM.CONSTANTS.FLOAT32
            aTypes(1) = LJM.CONSTANTS.UINT16
            ReDim aValues(numFrames - 1)
            aValues(0) = 2.5  ' 2.5 V
            aValues(1) = 12345  ' 12345
            LJM.eWriteAddresses(handle, numFrames, aAddresses, aTypes, _
                                aValues, errorAddress)

            Console.WriteLine("")
            Console.WriteLine("eWriteAddresses:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  Address - " & aAddresses(i) & _
                                  ", data type - " & aTypes(i) & _
                                  ", value : " & aValues(i))
            Next

            ' Setup and call eReadAddresses to read values.
            numFrames = 3
            ReDim aAddresses(numFrames - 1)
            aAddresses(0) = 60028  ' serial number
            aAddresses(1) = 60000  ' product ID
            aAddresses(2) = 60004  ' firmware version
            ReDim aTypes(numFrames - 1)
            aTypes(0) = LJM.CONSTANTS.UINT32
            aTypes(1) = LJM.CONSTANTS.FLOAT32
            aTypes(2) = LJM.CONSTANTS.FLOAT32
            ReDim aValues(numFrames - 1)
            aValues(0) = 0
            aValues(1) = 0
            aValues(2) = 0
            LJM.eReadAddresses(handle, numFrames, aAddresses, aTypes, _
                               aValues, errorAddress)

            Console.WriteLine("")
            Console.WriteLine("eReadAddresses results:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  Address - " & aAddresses(i) & _
                                  ", data type - " & aTypes(i) & _
                                  ", value : " & aValues(i))
            Next

            ' Setup and call eAddresses to write/read values.
            numFrames = 3
            ReDim aAddresses(numFrames - 1)
            aAddresses(0) = 1000  ' DAC0
            aAddresses(1) = 55110  ' TEST_UINT16 
            aAddresses(2) = 55110  ' TEST_UINT16
            ReDim aTypes(numFrames - 1)
            aTypes(0) = LJM.CONSTANTS.FLOAT32
            aTypes(1) = LJM.CONSTANTS.UINT16
            aTypes(2) = LJM.CONSTANTS.UINT16
            ReDim aWrites(numFrames - 1)
            aWrites(0) = LJM.CONSTANTS.WRITE
            aWrites(1) = LJM.CONSTANTS.WRITE
            aWrites(2) = LJM.CONSTANTS.READ
            ReDim aNumValues(numFrames - 1)
            aNumValues(0) = 1
            aNumValues(1) = 1
            aNumValues(2) = 1
            ReDim aValues(numFrames - 1)
            aValues(0) = 2.5  ' write 2.5 V
            aValues(1) = 12345  ' write 12345
            aValues(2) = 0  ' read
            LJM.eAddresses(handle, numFrames, aAddresses, aTypes, aWrites, _
                           aNumValues, aValues, errorAddress)

            Console.WriteLine("")
            Console.WriteLine("eAddresses results:")
            For i = 0 To numFrames - 1
                Console.WriteLine("  Address - " & aAddresses(i) & _
                                  ", data type - " & aTypes(i) & _
                                  ", write -  " & aWrites(i) & _
                                  ", values: " & aValues(i))
            Next
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
