Class ppfInfo

    Private wMain As WpfMain = CType(Application.Current.MainWindow, WpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()

    Private Sub ppfInfo_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        FlowDocViewer.Document = Nothing
    End Sub

End Class
