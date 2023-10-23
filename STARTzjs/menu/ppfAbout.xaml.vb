Class ppfAbout

    Private PicturePath As String
    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()
    Private Lge As Boolean = mySystem.LgeCzech

#Region " Loaded "

    Private Sub ppfAbout_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
        lblApp.Text = Application.CompanyName & "  " & Application.ProductName & "  " & If(Lge, "verze", "version") & " " & Application.Version
        lblCop.Text = "copyright ©2002-" & mySystem.BuildYear.ToString & "  " & Application.LegalCopyright
        txtLicense.Text = myString.FromBytes(myFile.ReadEmbeddedResource(If(Lge, "CZ", "EN") & "-License.txt"))
    End Sub

#End Region

#Region " Hyperlinks "

    Private Sub lbl_MouseLeftButtonUp(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles lblMail.MouseLeftButtonUp, lblWeb.MouseLeftButtonUp
        Dim lbl As TextBlock = CType(sender, TextBlock)
        myLink.Start(wSetting, lbl.Text)
    End Sub

    Private Sub lbl_MouseEnter(sender As System.Object, e As System.Windows.Input.MouseEventArgs) Handles lblMail.MouseEnter, lblWeb.MouseEnter
        Me.Cursor = Cursors.Hand
    End Sub

    Private Sub lbl_MouseLeave(sender As System.Object, e As System.Windows.Input.MouseEventArgs) Handles lblMail.MouseLeave, lblWeb.MouseLeave
        Me.Cursor = Cursors.Arrow
    End Sub

#End Region





End Class
