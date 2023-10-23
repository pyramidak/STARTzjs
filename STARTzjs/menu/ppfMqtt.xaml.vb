Imports System.Threading.Tasks

Class ppfMqtt

    Private WithEvents mqtt As clsMQTT
    Private Sub ppfWindows_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
        txtBroker.Text = Dat.Options.BrokerIP
        txtPort.Text = Dat.Options.BrokerPort.ToString
        If txtPort.Text = "" Then txtPort.Text = "1883"
        txtUser.Text = Dat.Options.BrokerUser
        txtPass.Password = myString.Decrypt(Dat.Options.BrokerPass, Application.Pass)
    End Sub

    Private Sub txtBroker_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBroker.TextChanged
        btnTest.IsEnabled = If(txtBroker.Text = "", False, True)
    End Sub

    Private Sub btnTest_Click(sender As Object, e As RoutedEventArgs) Handles btnTest.Click
        btnTest.IsEnabled = False
        If IsNumeric(txtPort.Text) = False Then txtPort.Text = "1883"
        mqtt = New clsMQTT(txtBroker.Text, CInt(txtPort.Text), txtUser.Text, txtPass.Password)
    End Sub

    Private Sub Err(Message As String, Task As clsMQTT.TaskErr) Handles mqtt.Err
        Dispatcher.Invoke(Sub() ShowMessage(Message, wpfDialog.Ikona.chyba))
    End Sub

    Private Sub Connected() Handles mqtt.Connected
        mqtt.Disconnect.Wait(1000)
        Dispatcher.Invoke(Sub() SaveSetting())
        Dispatcher.Invoke(Sub() ShowMessage(If(mySystem.LgeCzech, "Spojení bylo navázáno. Nastavení uloženo.", "The connection has been established. The setting was saved."), wpfDialog.Ikona.ok))
    End Sub

    Private Sub ShowMessage(Message As String, Ikona As wpfDialog.Ikona)
        Dim wDialog As New wpfDialog(Application.SettingWindow, Message, "MQTT Client", Ikona, "OK")
        wDialog.ShowDialog()
        btnTest.IsEnabled = True
    End Sub

    Private Sub SaveSetting()
        Dat.Options.BrokerIP = txtBroker.Text
        Dat.Options.BrokerPort = CInt(txtPort.Text)
        Dat.Options.BrokerUser = txtUser.Text
        Dat.Options.BrokerPass = myString.Encrypt(txtPass.Password, Application.Pass)
    End Sub

End Class
