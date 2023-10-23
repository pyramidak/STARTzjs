Imports System.Windows.Threading
Imports System.Net.Mail

Public Class wpfError

    Private Const PasswordMail As String = ""
    Public Property myError As DispatcherUnhandledExceptionEventArgs

    Private Function GetMessage() As String
        Return txtError.Text + NR + NR + "Výjimka nastala" + NR + txtWhen.Text + NR + NR + "Email uživatele" + NR + txtEmail.Text
    End Function

    Private Sub btn_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnCopy.Click
        Try
            Clipboard.SetText(GetMessage)
            Dim wDialog = New wpfDialog(Me, "Hlášení o chybě zkopírováno do schránky Windows." + NR + _
                            "Hlášení do vámi vytvořeného emailu vložíte CTRL + V." + NR + _
                            "Email zašlete na:     zdenek@jantac.net", "Zaslání žádosti", Nothing, "Rozumím")
            wDialog.ShowDialog()
        Catch
            Dim FormDialog = New wpfDialog(Me, "Nepodařilo se přenést zprávu do schránky Windowsu." + NR + "Zkopírujte zprávu ručně označením textu CTRL + A a zkopírování CTRL + C.", Me.Title, wpfDialog.Ikona.varovani, "Zavřít")
            FormDialog.ShowDialog()
        End Try
    End Sub

    Private Sub Window_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Me.Title = Application.Current.MainWindow.Title
        If mySystem IsNot Nothing Then
            txtError.Text += Application.ProductName & " " & Application.Version + NR
            txtError.Text += "Framework " & mySystem.sFramework + NR
            txtError.Text += "Windows " & mySystem.Current.Name + "  x" + If(mySystem.Is64bit, "64", "86") + NR
            txtError.Text += If(mySystem.Current.Number = 5, " (pro tento systém nebyl program testován)" + NR, "") + NR
        End If
        txtError.Text += myError.Exception.Message + NR + NR
            txtError.Text += myError.Exception.StackTrace
        If IsNothing(myError.Exception.InnerException) = False Then
            txtError.Text += NR + NR + "InnerException"
            txtError.Text += NR + myError.Exception.InnerException.Message
            txtError.Text += NR + NR + myError.Exception.InnerException.StackTrace
        End If
        Try
            Clipboard.SetText(txtError.Text)
            txtEmail.Text = myRegister.GetValue(HKEY.CURRENT_USER, "Software\pyramidak", "UserEmail", "")
        Catch
        End Try
    End Sub

#Region " Send "

    Private WithEvents ThreadWorker As New System.ComponentModel.BackgroundWorker
    Private myMail As New MailMessage
    Private Chyba As Exception

    Private Sub ThreadWorker_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles ThreadWorker.DoWork
        Dim smtp As New SmtpClient
        smtp.Host = "smtp.gmail.com"
        smtp.Port = 587 '465
        smtp.EnableSsl = True
        smtp.DeliveryMethod = SmtpDeliveryMethod.Network
        smtp.Timeout = 7000
        smtp.Credentials = New System.Net.NetworkCredential("pyramidak@gmail.com", PasswordMail)
        Chyba = Nothing
        Try
            smtp.Send(myMail)
        Catch Ex As Exception
            Chyba = Ex 'časový limit operace vypršel má číslo chyby 5
        End Try
    End Sub

    Private Sub ThreadWorker_RunWorkerCompleted(sender As Object, e As ComponentModel.RunWorkerCompletedEventArgs) Handles ThreadWorker.RunWorkerCompleted
        Dim wDialog As wpfDialog = Nothing
        If Chyba Is Nothing Then
            wDialog = New wpfDialog(Me, "Hlášení o chybě úspěšně odesláno.", Me.Title, wpfDialog.Ikona.ok, "Zavřít")
        Else
            btnSend.IsEnabled = True
            wDialog = New wpfDialog(Me, "Nepovedlo se odeslat hlášení do mailboxu. " + Chyba.Message + " Zkuste to prosím znovu.", Me.Title, wpfDialog.Ikona.varovani, "Zavřít")
        End If
        wDialog.ShowDialog()
    End Sub

    Private Sub btnSend_Click(sender As Object, e As RoutedEventArgs) Handles btnSend.Click
        btnSend.IsEnabled = False
        If txtEmail.Text.Contains("@") Then myRegister.CreateValue(HKEY.CURRENT_USER, "Software\pyramidak", "UserEmail", txtEmail.Text)
        myMail.From = New MailAddress("pyramidak@gmail.com", "pyramidak software")
        myMail.To.Add("pyramidak@gmail.com")
        myMail.Subject = "Error " & Application.ProductName & " " & Application.Version
        myMail.Body = GetMessage()
        myMail.IsBodyHtml = False
        ThreadWorker.RunWorkerAsync()
    End Sub

#End Region

End Class
