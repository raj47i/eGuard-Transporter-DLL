Public Class Transporter
    ''
    Const MaxLength As Integer = 2097152
    Const PacketSize As Integer = 4096
    Structure Request
        Private _SenderIP, _SenderName, _Data() As String
        Private _Code As Integer
        Public Sub New(ByVal SourceIP As String, ByVal ReqCode As Integer, ByVal SourceName As String, ByVal MyDat() As String)
            Me._SenderIP = SourceIP
            Me._Code = ReqCode
            Me._SenderName = SourceName
            Me._Data = MyDat
        End Sub
        Public ReadOnly Property SenderIP() As String
            Get
                Return Me._SenderIP
            End Get
        End Property
        Public ReadOnly Property SenderName() As String
            Get
                Return Me._SenderName
            End Get
        End Property
        Public ReadOnly Property Code() As Integer
            Get
                Return _Code
            End Get
        End Property
        Public ReadOnly Property Data() As String()
            Get
                Return _Data
            End Get
        End Property
    End Structure










#Region "############################## Local Variables #################################"
    Private LocalPortNumber As Integer = 6001
    Private Listener As System.Net.Sockets.TcpListener
    Private Started As Boolean = False
    Private WithEvents Frequency As New Timers.Timer(200)
#End Region
#Region "######################## Class Constructor & Destructor ########################"
    'Public Sub New(ByVal Port As Integer)
    '   Listener = New System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse(Me.LocalIP), Port)
    '  LocalPortNumber = Port
    'End Sub
    Protected Overrides Sub Finalize()
        StopReceiving()
        MyBase.Finalize()
    End Sub
#End Region
#Region "############################# Available Methods ################################"
    'Events
    Public Event ReceivedString(ByRef SenderIP As String, ByVal Str As String)
    Public Event ReceivedBytes(ByVal SenderIP As String, ByVal Bytes() As Byte)
    Public Event ReceivedRequest(ByVal Rq As Request)
    'Properties
    Public ReadOnly Property Localhost() As String
        Get
            Return System.Net.Dns.GetHostName()
        End Get
    End Property
    Public ReadOnly Property LocalIP() As String
        Get
            Return System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())(0).ToString
        End Get
    End Property
    Public ReadOnly Property isReceiving() As Boolean
        Get
            Return Me.Started
        End Get
    End Property
    Public ReadOnly Property LocalPort() As Integer
        Get
            Return LocalPortNumber
        End Get
    End Property
    Public Property ReceivingFrequency() As Double
        Get
            Return Frequency.Interval
        End Get
        Set(ByVal MilliSecs As Double)
            Frequency.Enabled = False
            Frequency.Interval = MilliSecs
            Frequency.Enabled = True
        End Set
    End Property
    'Sub Routines
    Public Sub StartReceiving(ByVal Port As Integer)
        If Not Me.isReceiving Then
            LocalPortNumber = Port
            Listener = New System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse(Me.LocalIP), Me.LocalPort)
            Listener.Start()
            Frequency.Enabled = True
            Frequency.Start()
            Started = True
        End If
    End Sub
    Public Sub StopReceiving()
        If Me.isReceiving Then
            Frequency.Enabled = False
            Frequency.Stop()
            Listener.Stop()
            Started = False
        End If
    End Sub
    Public Sub SendString(ByVal Destination As String, ByVal Port As Integer, ByVal DATA As String)
        If String.Compare(Destination, "Localhost", True) = 0 Or String.Compare(Destination, "127.0.0.1", True) = 0 Then Destination = Me.LocalIP
        'Now We Can Try to Send the String DATA'
        Try
            Dim Buffer() As Byte = System.Text.Encoding.Default.GetBytes(DATA.ToCharArray)
            SendBuffer(Destination, Buffer, Port, "000")
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
    Public Sub SendBytes(ByVal Destination As String, ByVal Port As Integer, ByVal Data() As Byte)
        If Port = Nothing Then Port = Me.LocalPort
        If String.Compare(Destination, "Localhost", True) = 0 Or String.Compare(Destination, "127.0.0.1", True) = 0 Then Destination = Me.LocalIP
        'Now We Can Try to Send the Byte Array DATA'
        Try
            SendBuffer(Destination, Data, Port, "001")
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
    Public Sub SendRequest(ByVal Destination As String, ByVal Port As Integer, ByVal RequestCode As Integer, ByVal Data() As String)
        If String.Compare(Destination, "Localhost", True) = 0 Or String.Compare(Destination, "127.0.0.1", True) = 0 Then Destination = Me.LocalIP
        'Encodeing Rquest to String and then Bytes
        Try
            Dim tmps As String = reqEncoder(RequestCode, Data)
            'Now We Can Try to Send the Encoded Request
            Dim MyData() As Byte = System.Text.Encoding.Default.GetBytes(tmps.ToCharArray)
            SendBuffer(Destination, MyData, Port, "010")
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region
#Region "########################## All Internal Operations #############################"
    Private Sub ConnectionIsPending(ByVal o As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles Frequency.Elapsed
        Frequency.Enabled = False
        Try
            If Listener.Pending Then
                'Dim ReceivingThread As New Threading.Thread(AddressOf BufferReceiver)
                Dim ReceivingThread As New Threading.Thread(AddressOf BufferReceiver)
                ReceivingThread.Start()
            End If
        Catch
        End Try
        Frequency.Enabled = True
    End Sub
    Private Sub DecodeHeader(ByRef InDataHeader() As Byte, ByRef Type As String, ByRef Len As Integer)
        Len = 0
        Dim i As Byte
        '
        Dim HeaderStr As String = BytesToBit(InDataHeader(0))
        HeaderStr = HeaderStr & BytesToBit(InDataHeader(1))
        HeaderStr = HeaderStr & BytesToBit(InDataHeader(2))
        Type = HeaderStr.Substring(0, 3)
        '
        Dim Bits() As Char = HeaderStr.Substring(3).ToCharArray
        For i = 0 To 20
            Len += System.Math.Pow(2, i) * Val(Bits(20 - i))
        Next
    End Sub
    Private Function BytesToBit(ByVal Num As Byte) As String
        Dim r As Integer
        Dim tmp As String = ""
        While Num > 0
            Num = System.Math.DivRem(Num, 2, r)
            tmp = r & tmp
        End While
        While tmp.Length < 8
            tmp = "0" & tmp
        End While
        Return tmp
    End Function
    Private Function BitsToByte(ByVal BitSeq As String) As Byte
        Dim i, Num As Byte
        Dim Bits() As Char = BitSeq.ToCharArray
        Num = 0
        For i = 0 To 7
            Num += System.Math.Pow(2, i) * Val(Bits(7 - i))
        Next
        Return Num
    End Function
    Private Sub PrepareHeader(ByVal BufferLength As Integer, ByVal MessageType As String, ByRef Header As Byte())
        Dim r As Integer
        Dim tmp As String = ""
        While BufferLength > 0
            BufferLength = System.Math.DivRem(BufferLength, 2, r)
            tmp = r & tmp
        End While
        While tmp.Length < 21
            tmp = "0" & tmp
        End While
        tmp = MessageType & tmp
        Header(0) = BitsToByte(tmp.Substring(0, 8))
        Header(1) = BitsToByte(tmp.Substring(8, 8))
        Header(2) = BitsToByte(tmp.Substring(16, 8))
    End Sub
    Private Function SendHeader(ByRef CL As System.Net.Sockets.TcpClient, ByVal Length As Integer, ByVal Type As String) As Boolean
        Try
            If Not Length < MaxLength Then Return False
            Dim MyHeader(2) As Byte
            PrepareHeader(Length, Type, MyHeader)
            CL.GetStream.Write(MyHeader, 0, 3)
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function
    Private Function IsIPAddress(ByVal DestnIP As String) As Boolean
        Dim i, p As Byte
        Try
            Dim IPParts() As String = DestnIP.Split(".")
            p = IPParts.GetLength(0)
            If Not p = 4 Then
                Return False
            Else
                For i = 0 To 3
                    If Not (Val(IPParts(i)) < 256 And Val(IPParts(i)) >= 0) Then
                        Return False
                    End If
                Next
            End If
            Return True
        Catch
            Return False
        End Try
    End Function
    Private Sub ReceiveHeader(ByRef Cl As System.Net.Sockets.Socket, ByRef incTypeOfData As String, ByRef incLen As Integer)
        Dim lnBuffer(3) As Byte
        Cl.SetSocketOption(Net.Sockets.SocketOptionLevel.Socket, Net.Sockets.SocketOptionName.ReceiveTimeout, 7000)
        Try
            Cl.Receive(lnBuffer, 3, Net.Sockets.SocketFlags.None)
            DecodeHeader(lnBuffer, incTypeOfData, incLen)
        Catch ex As Exception
            incTypeOfData = ""
            incLen = 0
        End Try
    End Sub
    Private Sub SendBuffer(ByVal Destn As String, ByRef MyBuffer() As Byte, ByVal SelPort As Integer, ByVal BuffDataType As String)
        Dim TempTCPClient As New System.Net.Sockets.TcpClient
        'Now We Can Try to Send the String DATA'
        Try
            If IsIPAddress(Destn) Then
                TempTCPClient.Connect(System.Net.IPAddress.Parse(Destn), SelPort)
            Else
                TempTCPClient.Connect(Destn, SelPort)
            End If
            'Sends Data after sending the type and size of data
            If SendHeader(TempTCPClient, MyBuffer.Length, BuffDataType) Then
                TempTCPClient.GetStream.Write(MyBuffer, 0, MyBuffer.Length)
            Else
                Throw New Exception("Cannot Send Header")
            End If
            TempTCPClient.Close()
        Catch
            TempTCPClient.Close()
            Throw New Exception("Host Not Found")
        End Try
    End Sub
    Private Sub BufferReceiver()
        Dim incLen As Integer = 0
        Dim incType As String = ""
        Dim Client As System.Net.Sockets.Socket
        Try
            Client = Listener.AcceptSocket
            ReceiveHeader(Client, incType, incLen)
            Dim Buffer(incLen) As Byte
            Dim ep As System.Net.IPEndPoint = Client.RemoteEndPoint
            Select Case incType
                Case "000"
                    'String Comes
                    Client.Receive(Buffer, incLen, Net.Sockets.SocketFlags.None)
                    RaiseEvent ReceivedString(ep.Address.ToString, System.Text.Encoding.Default.GetString(Buffer))
                Case "001"
                    'Bytes Comes
                    Client.Receive(Buffer, incLen, Net.Sockets.SocketFlags.None)
                    RaiseEvent ReceivedBytes(ep.Address.ToString, Buffer)
                Case "010"
                    'Request Comes
                    Client.Receive(Buffer, incLen, Net.Sockets.SocketFlags.None)
                    Dim rqCode As Integer
                    Dim hs As String = Nothing
                    Dim strdata() As String = Nothing
                    reqDecoder(System.Text.Encoding.Default.GetString(Buffer), rqCode, hs, strdata)
                    RaiseEvent ReceivedRequest(New Request(ep.Address.ToString, rqCode, hs, strdata))
                Case "011"
                Case "100"
                Case "101"
                Case "110"
                Case "111"
            End Select
            Client.Close()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
    Private Function reqEncoder(ByVal Code As Integer, ByVal Str() As String) As String
        Dim sb As New System.Text.StringBuilder()
        Dim t As String
        For Each t In Str
            If t Is Nothing Then
                t = ""
            End If
            sb.Append(t.Replace(";", "<*€!>"))
            sb.Append(";")
        Next
        sb.Append(Code.ToString())
        sb.Append(";")
        sb.Append(System.Net.Dns.GetHostName())
        Return sb.ToString
    End Function
    Private Sub reqDecoder(ByVal rqStr As String, ByRef Code As Integer, ByRef HostName As String, ByRef Str() As String)
        Dim tmp() As String = rqStr.Split(";")
        HostName = tmp(tmp.Length - 1)
        Code = Val(tmp(tmp.Length - 2))
        ReDim Str(tmp.Length - 3)
        Dim i As Integer
        For i = 0 To Str.Length - 1
            Str(i) = tmp(i).Replace("<*€!>", ";")
        Next
    End Sub
#End Region
End Class
