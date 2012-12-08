Public Class Form1
    Private MyPort As Integer = 9000

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        MsgBox("Status : " & vbCrLf & "  Local IP : " & Transporter1.LocalIP _
        & vbCrLf & "  Localhost : " & Transporter1.Localhost & vbCrLf & "  LocalPort : " & Transporter1.LocalPort _
        & vbCrLf & "  isReceiving : " & Transporter1.isReceiving, MsgBoxStyle.Information)
    End Sub

    Private Sub Transporter1_ReceivedString(ByRef SenderIP As String, ByVal Str As String) Handles Transporter1.ReceivedString
        MsgBox("From : " & SenderIP & vbCrLf & "String : " & Str, MsgBoxStyle.Information, "String Received")
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Transporter1.StopReceiving()
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Try
            Transporter1.SendString(TextBox2.Text, Val(TextBox3.Text), TextBox1.Text)
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub MyPrt_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyPrt.TextChanged
        MyPort = Val(MyPrt.Text)
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Transporter1.StartReceiving(MyPort)
    End Sub
End Class
