Public Class wpfSetting

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Public Property IndexPage As Integer = 1
    Public Property Message As String

    Private Sub wpfSetting_Closed(sender As Object, e As System.EventArgs) Handles Me.Closed
        Dat.HasChanges = True
    End Sub

    Private Sub wpfSetting_Initialized(sender As Object, e As System.EventArgs) Handles Me.Initialized
        Me.Left = wMain.Left : Me.Top = wMain.Top
        Me.Icon = Application.Icon
    End Sub

    Private Sub wpfSetting_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles Me.KeyUp
        If e.Key = Key.Escape Then Me.Close()
    End Sub

    Private Sub wpfSetting_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        imgWindows.Source = mySystem.Current.Image
        lbxMenu.SelectedIndex = IndexPage
        lbxMenu.Focus()
        If Not Message = "" Then
            Dim wDialog As New wpfDialog(Application.SettingWindow, Message, "Nová verze STARTzjs", Nothing, If(mySystem.LgeCzech, "Zavřít", "Close"))
            wDialog.ShowDialog()
        End If
    End Sub

    Private Sub lbxMenu_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles lbxMenu.SelectionChanged
        Dim item As StackPanel = CType(lbxMenu.SelectedItem, StackPanel)

        If IsNothing(item) = False Then
            Select Case item.Tag.ToString
                Case "About"
                    FramePage.Navigate(New ppfAbout)

                Case "Info"
                    FramePage.Navigate(New ppfInfo)

                Case "Import"
                    FramePage.Navigate(New ppfImport)

                Case "Budik"
                    FramePage.Navigate(New ppfBudik)

                Case "Logs"
                    FramePage.Navigate(New ppfMenu)

                Case "Save"
                    FramePage.Navigate(New ppfSave)

                Case "Windows"
                    FramePage.Navigate(New ppfWindows)

                Case "mqtt"
                    FramePage.Navigate(New ppfMQTT)


            End Select
        End If
    End Sub

End Class
