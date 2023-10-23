Public Class wpfKey

    Public Property Vysledek As clsKeyType
    Private WithEvents myKeyType As New clsKeyType
    Private Timer As System.Windows.Threading.DispatcherTimer
    Private wMain As Window = CType(Me.Owner, wpfMain)

    Private Sub KeyTypeCatched(ByVal KeyType As clsKeyType) Handles myKeyType.Catched
        If KeyType.Canceled = False Then
            For Each box In BoxColumnList.seznam
                If box.HKey.Modifiers = KeyType.Modifiers And box.HKey.Key = KeyType.Keys Then
                    txtKey.Text = KeyType.Text + " již existuje." + NR + "Zkuste jinou kombinaci."
                    Exit Sub
                End If
            Next
        End If
        txtKey.Margin = New Thickness(0)
        txtKey.Text = KeyType.Text
        Timer.IsEnabled = True
        Vysledek = myKeyType
    End Sub

    Private Sub MainWindow_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles Me.KeyUp
        myKeyType.Test(e)
    End Sub

    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Call (New clsGlass).GlassEffect(Me)
        txtKey.Text = "Stiskněte kombinaci kláves" + NR + "(Backspace zruší Hotkey)"
        Timer = New System.Windows.Threading.DispatcherTimer
        Timer.Interval = TimeSpan.FromSeconds(1)
        AddHandler Timer.Tick, AddressOf Timer_Tick
    End Sub

    Private Sub Timer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.Close()
    End Sub

End Class
