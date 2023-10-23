Imports System.Windows.Threading
Imports System.Data
Imports System.Threading.Tasks

Class ppfBudik

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()
    Private WithEvents timAlarm As DispatcherTimer
    Private Lge As Boolean = mySystem.LgeCzech

#Region " Loaded "

#Region " load combos "

    Private Sub fillCombo()
        cboPo.Items.Clear()
        cboPo.Items.Add(wMain.FindResource("t5").ToString)
        cboPo.Items.Add(wMain.FindResource("t10").ToString)
        cboPo.Items.Add(wMain.FindResource("t15").ToString)
        cboPo.Items.Add(wMain.FindResource("t30").ToString)
        cboPo.Items.Add(wMain.FindResource("t1").ToString)
        cboPo.Items.Add(wMain.FindResource("t2").ToString)
        cboPo.Items.Add(wMain.FindResource("t4").ToString)
        cboPo.Items.Add(wMain.FindResource("t6").ToString)
        cboPo.Items.Add(wMain.FindResource("t12").ToString)
        cboPo.Items.Add(wMain.FindResource("t24").ToString)
        cboPo.SelectedIndex = 1
        cbxShutdown.Items.Clear()
        cbxShutdown.Items.Add(wMain.FindResource("tLock").ToString)
        cbxShutdown.Items.Add(wMain.FindResource("StandBy").ToString)
        cbxShutdown.Items.Add(wMain.FindResource("LogOff").ToString)
        cbxShutdown.Items.Add(wMain.FindResource("PowerOff").ToString)
        cbxShutdown.Items.Add(wMain.FindResource("Restart").ToString)
        cbxShutdown.SelectedIndex = 1
        cbxSymbolV.Items.Clear()
        cbxSymbolV.Items.Add(">")
        cbxSymbolV.Items.Add("<")
        cbxSymbolV.Items.Add("=")
        cbxSymbolV.SelectedIndex = 2
        cbxMQTT.Items.Clear()
        cbxMQTT.Items.Add(">")
        cbxMQTT.Items.Add("<")
        cbxMQTT.Items.Add("=")
        cbxMQTT.SelectedIndex = 2
        cbxChange.Items.Clear()
        cbxChange.Items.Add(If(Lge, "všechny", "all"))
        cbxChange.Items.Add(If(Lge, "vytvoření", "create"))
        cbxChange.Items.Add(If(Lge, "smazání", "delete"))
        cbxChange.Items.Add(If(Lge, "přejmenování", "rename"))
        cbxChange.Items.Add(If(Lge, "změna", "change"))
        cbxChange.SelectedIndex = 0
        cbxDisplay.Items.Clear()
        cbxDisplay.Items.Add(wMain.FindResource("DisplayInternal").ToString)
        cbxDisplay.Items.Add(wMain.FindResource("DisplayClone").ToString)
        cbxDisplay.Items.Add(wMain.FindResource("DisplayExtend").ToString)
        cbxDisplay.Items.Add(wMain.FindResource("DisplayExternal").ToString)
        cbxDisplay.SelectedIndex = 0
    End Sub
#End Region

    Private Sub ppfBudik_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Me.Title = wMain.FindResource("budik").ToString
        Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
        fillCombo()

        lblBudik.Text = "" : lblZbyva.Text = "" : lblCas.Text = ""
        timAlarm = New DispatcherTimer
        timAlarm.Interval = TimeSpan.FromSeconds(1)

        'Nastavení budíku
        txtZvuk.Text = Dat.Options.AlarmMelody
        lblFolder.Text = Dat.Options.AlarmFolder
        txtFrom.Text = Dat.Options.Email
        passBox.Password = myString.Decrypt(Dat.Options.EmailPass, Application.Pass)
        tabWhat.SelectedItem = tbpMessage
        optShut2.IsChecked = True

        'Načtení časů budíku
        cmdOdebrat.IsEnabled = False
        lvwAlarm.ItemsSource = Dat.AlarmMem
        timAlarm.IsEnabled = True
        If Dat.AlarmMem.Count = 0 Then cmdAkt.IsEnabled = False
    End Sub

#End Region

#Region " Timer "

    Private Sub timAlarm_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timAlarm.Tick
        lblCas.Text = T2S(TimeOfDay.Hour) & ":" & T2S(TimeOfDay.Minute)
        If Dat.AlarmMem.Count = 0 Then cmdAkt.IsEnabled = False
        If wMain.timAlarm.IsEnabled Then
            cmdAkt.Content = If(Lge, "Zastavit", "Stop")
        Else
            cmdAkt.Content = If(Lge, "Spustit", "Start")
            lblZbyva.Text = "" : lblBudik.Text = ""
            Exit Sub
        End If

        'Zobrazení odpočítávání
        Dim Alarm = TryCast(lvwAlarm.SelectedItem, clsSetting.clsAlarm)
        If Alarm Is Nothing Then Alarm = Dat.AlarmMem(0)
        Select Case Alarm.Watch
            Case "time"
                lblBudik.Text = Alarm.When1
                Dim Will As Date = Date.Parse(Alarm.When1)
                Dim Minut As Integer = CInt(DateDiff(DateInterval.Minute, Date.Now, Will)) + 1
                If Minut < 1 Then
                    Minut = CInt(DateDiff(DateInterval.Minute, Date.Now, Will.AddDays(1))) + 1
                End If
                If Minut = 1 Then
                    lblZbyva.Text = "0:" & T2S(CStr(If(Will.Subtract(Date.Now).Seconds < 0, 0, Will.Subtract(Date.Now).Seconds)))
                Else
                    lblZbyva.Text = TimeSerial(0, Minut, 0).ToShortTimeString
                End If
            Case Else
                lblZbyva.Text = ""
                lblBudik.Text = ""
        End Select
    End Sub

#End Region

#Region " Time "

    Private Function T2S(ByVal Cas As String) As String
        T2S = Cas
        If Len(Cas) < 2 Then T2S = "0" & T2S
    End Function

    Private Function T2S(ByVal Cas As Integer) As String
        Dim sCas As String = CStr(Cas)
        T2S = sCas
        If Len(sCas) < 2 Then T2S = "0" & T2S
    End Function

    Private Sub txtHodin_KeyUp1(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles txtHodin.KeyUp
        If e.Key = Key.Enter Then txtMinut.Focus()
    End Sub

    Private Sub txtHodin_MouseDoubleClick1(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles txtHodin.MouseDoubleClick
        txtHodin.Text = T2S(Hour(Now))
    End Sub

    Private Sub txtHodin_MouseWheel1(ByVal sender As Object, ByVal e As System.Windows.Input.MouseWheelEventArgs) Handles txtHodin.MouseWheel
        txtHodin.Text = T2S(CInt(Val(txtHodin.Text)) + CInt(IIf(e.Delta < 0, +1, -1)))
        If Val(txtHodin.Text) = -1 Then txtHodin.Text = "23"
        If Val(txtHodin.Text) = 24 Then txtHodin.Text = "00"
    End Sub

    Private Sub txtMinut_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles txtMinut.KeyUp
        If e.Key = Key.Enter Then txtHodin.Focus()
    End Sub

    Private Sub txtMinut_MouseDoubleClick1(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles txtMinut.MouseDoubleClick
        txtMinut.Text = T2S(Minute(Now))
    End Sub

    Private Sub txtMinut_MouseWheel1(ByVal sender As Object, ByVal e As System.Windows.Input.MouseWheelEventArgs) Handles txtMinut.MouseWheel
        txtMinut.Text = T2S(CInt(Val(txtMinut.Text)) + CInt(IIf(e.Delta < 0, +1, -1)))
        If Val(txtMinut.Text) = -1 Then txtMinut.Text = "59"
        If Val(txtMinut.Text) = 60 Then txtMinut.Text = "00"
    End Sub
    Private Sub cboPo_SelectionChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles cboPo.SelectionChanged
        Dim ho, mi As Short
        Select Case cboPo.SelectedIndex
            Case 0
                mi = 5
            Case 1
                mi = 10
            Case 2
                mi = 15
            Case 3
                mi = 30
            Case 4
                ho = 1
            Case 5
                ho = 2
            Case 6
                ho = 4
            Case 7
                ho = 6
            Case 8
                ho = 12
            Case 9
                ho = 23 : mi = 55
        End Select
        txtHodin.Text = T2S(Hour(TimeSerial(Hour(Now) + ho, Minute(Now) + mi, 0)))
        txtMinut.Text = T2S(Minute(TimeSerial(Hour(Now) + ho, Minute(Now) + mi, 0)))
    End Sub

#End Region

#Region " Toggle MQTT "

    Private Sub ckbReconnect_Checked(sender As Object, e As RoutedEventArgs) Handles ckbReconnect.Checked, ckbReconnect.Unchecked
        Dim switch = Not CBool(ckbReconnect.IsChecked)
        txtTopicP.IsEnabled = switch
        txtPayloadP.IsEnabled = switch
    End Sub

#End Region

#Region " Budíky "

    Private Sub cmdAktivace_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdAkt.Click
        wMain.AlarmToggle()
    End Sub

    Private Sub cmdOdebrat_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdOdebrat.Click
        If Dat.AlarmMem.Count = 0 Then Exit Sub
        Dim Alarmy = lvwAlarm.SelectedItems.Cast(Of clsSetting.clsAlarm)
        If Alarmy Is Nothing Then cmdOdebrat.IsEnabled = False : Exit Sub
        'Folder
        For Each slozka In Alarmy.Where(Function(a) a.Watch = "dir").Select(Function(b) b.When4).Distinct
            If Alarmy.Where(Function(a) a.Watch = "dir" And a.When4 = slozka).Count = Dat.AlarmMem.Where(Function(a) a.Watch = "dir" And a.When4 = slozka).Count Then
                WatchFolder.Remove(slozka)
            End If
        Next
        'MQTT list
        Dim Topics As String() = Alarmy.Where(Function(a) a.Watch = "mqtt").Select(Function(b) b.When1).Distinct.ToArray
        'Remove from database
        Alarmy.ToList.ForEach(Sub(b) Dat.AlarmMem.Remove(b))
        'MQTT remove
        If Not Topics.Count = 0 Then wMain.mqttSubscribeRemove(Topics)
        'No alarms
        If Dat.AlarmMem.Count = 0 Then
            wMain.AlarmOnOff(False)
            cmdAkt.IsEnabled = False
        End If
        cmdOdebrat.IsEnabled = False
    End Sub

    Private Sub cmdPridat_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdPridat.Click
        'Kontrola stupních dat
        Select Case CType(tabWhen.SelectedItem, TabItem).Name 'IF this/KDYŽ něco - kontrola stupních dat
            Case tbpTime.Name
                Dim sMsg As String = If(Lge, "Nesmyslný čas. Opravte to.", "These settings make no sense. Please correct them.")
                If IsNumeric(txtHodin.Text) = False Then
                    Dim wDialog As New wpfDialog(wSetting, sMsg, Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtHodin.Focus()
                    Exit Sub
                ElseIf CDbl(txtHodin.Text) > 23 Or CDbl(txtHodin.Text) < 0 Then
                    Dim wDialog As New wpfDialog(wSetting, sMsg, Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtHodin.Focus()
                    Exit Sub
                End If
                If IsNumeric(txtMinut.Text) = False Then
                    Dim wDialog As New wpfDialog(wSetting, sMsg, Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtMinut.Focus()
                    Exit Sub
                ElseIf CDbl(txtMinut.Text) > 59 Or CDbl(txtMinut.Text) < 0 Then
                    Dim wDialog As New wpfDialog(wSetting, sMsg, Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtMinut.Focus()
                    Exit Sub
                End If

            Case tbpWindow.Name
                If rbnW0.IsChecked Then
                    If txtWnd.Text = "" Then
                        Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vložte titul okna aplikace, na který se bude čekat.", "Enter window title of application to wait for."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                        wDialog.ShowDialog()
                        btnWnd.Focus()
                        Exit Sub
                    End If
                End If
                If rbnW1.IsChecked Then
                    If clsSystem.isProcess(txtWnd.Text) Then
                        Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vložte titul okna spuštěné aplikace.", "Enter window title of running application."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                        wDialog.ShowDialog()
                        btnWnd.Focus()
                        Exit Sub
                    End If
                End If
                If rbnW2.IsChecked Then
                    If clsSystem.isProcess(ProcessIDwhen) = False Then
                        Dim wDialog As New wpfDialog(wSetting, If(Lge, "Okno aplikace s ID " + ProcessIDwhen.ToString + " již neexistuje. Zachyťte nové okno.", "Select a window of running application."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                        wDialog.ShowDialog()
                        btnWnd.Focus()
                        Exit Sub
                    End If
                End If

            Case tbpDir.Name
                If myFolder.Exist(lblFolder.Text) = False Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Zadejte platnou složku/adresář, ve kterém bude kontrolován seznam souborů.", "Enter valid folder/dir which will be checking list of files in."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    cmdFolder.Focus()
                    Exit Sub
                End If

            Case tbpFile.Name
                If txtWatchFile.Text = "" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vyberte soubor.", "Select a file."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    cmdWatchFile.Focus()
                    Exit Sub
                End If

            Case tbpMQTT.Name
                If Dat.Options.BrokerIP = "" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Nejdříve nastavte MQTT klienta.", "First you must set MQTT Client."), Me.Title, wpfDialog.Ikona.varovani, "MQTT Client")
                    wDialog.ShowDialog()
                    wSetting.FramePage.Navigate(New ppfMqtt)
                    Exit Sub
                End If
                If txtTopicS.Text = "" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vložte téma k naslouchání.", "Enter topic to subscribe"), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtTopicS.Focus()
                    Exit Sub
                End If
                If IsNumeric(txtPayloadS.Text) = False Then cbxMQTT.SelectedIndex = 2

        End Select

        Select Case CType(tabWhat.SelectedItem, TabItem).Name 'THAN that/Tak udělej - kontrola stupních dat
            Case tbpRun.Name
                If txtRun.Text = "" And txtRun.Tag Is Nothing Then
                    Dim wDialog As New wpfDialog(wSetting, FindResource("tenterappname").ToString, Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    cmdRun.Focus()
                    Exit Sub
                End If

            Case tbpClose.Name
                If lblShut.Text = "" Or lblShut.Text = "0" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vyberte okno programu, který se má zavřít.", "Select program window to close."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    cmdShut.Focus()
                    Exit Sub
                End If

            Case tbpAlarm.Name
                If lvwAlarm.SelectedItems.Count = 0 Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vyberte budík, u kterého chcete měnit stav.", "Select the alarm you want to change the status for."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    lvwAlarm.Focus()
                    Exit Sub
                End If

            Case tbpEmail.Name
                If Not txtTo.Text.Contains("@") Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Chybí email příjemce.", "Recepient email missing."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtTo.Focus()
                    Exit Sub
                End If
                If EmailInputCheck() = False Then Exit Sub

            Case tbpCopy.Name
                If txtFileFrom.Text = "" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vyberte soubor, který se bude kopírovat.", "Select the file you want to copy."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtFileFrom.Focus()
                    Exit Sub
                End If
                If txtFileTo.Text = "" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vyberte kam se bude soubor kopírovat.", "Select the destination for file copy."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtFileTo.Focus()
                    Exit Sub
                End If

            Case tbpMQTTp.Name
                If Dat.Options.BrokerIP = "" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Nejdříve nastavte MQTT klienta.", "First you must set MQTT Client."), Me.Title, wpfDialog.Ikona.varovani, "MQTT Client")
                    wDialog.ShowDialog()
                    wSetting.FramePage.Navigate(New ppfMqtt)
                    Exit Sub
                End If
                If txtTopicP.Text = "" And ckbReconnect.IsChecked = False Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vložte téma k odeslání.", "Enter topic to publish."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtTopicP.Focus()
                    Exit Sub
                End If

            Case tbpKeys.Name
                If txtKeys.Text = "" Then
                    Dim wDialog As New wpfDialog(wSetting, If(Lge, "Vložte stisk kláves.", "Enter keystrokes."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                    wDialog.ShowDialog()
                    txtKeys.Focus()
                    Exit Sub
                End If

        End Select

        'Tvorba budíku
        Dim newAlarm As New clsSetting.clsAlarm
        newAlarm.Aktivni = True
        Select Case CType(tabWhen.SelectedItem, TabItem).Name
            Case tbpTime.Name
                newAlarm.Watch = "time"
                newAlarm.When2 = If(ckbInterval.IsChecked, 1, 0)
                If ckbInterval.IsChecked Then
                    newAlarm.When4 = T2S(txtHodin.Text) & ":" & T2S(txtMinut.Text)
                    newAlarm.When1 = wMain.NewTimeForInterval(newAlarm.When4)
                Else
                    newAlarm.When1 = T2S(txtHodin.Text) & ":" & T2S(txtMinut.Text)
                End If

            Case tbpDir.Name
                newAlarm.Watch = "dir"
                newAlarm.When1 = cbxChange.SelectedItem.ToString
                newAlarm.When2 = cbxChange.SelectedIndex
                newAlarm.When4 = lblFolder.Text
                WatchFolder.Add(newAlarm.When4, wMain.timAlarm.IsEnabled)

            Case tbpWindow.Name
                newAlarm.Watch = "window"
                newAlarm.When1 = txtWnd.Tag.ToString
                newAlarm.When2 = ProcessIDwhen
                newAlarm.When3 = If(rbnW0.IsChecked, 1, If(rbnW1.IsChecked, 2, 3))

            Case tbpFile.Name
                newAlarm.Watch = "file"
                newAlarm.When1 = txtValue.Text
                newAlarm.When4 = txtWatchFile.Text
                newAlarm.When2 = cbxSymbolV.SelectedIndex

            Case tbpMQTT.Name
                newAlarm.Watch = "mqtt"
                newAlarm.When1 = txtTopicS.Text
                newAlarm.When4 = txtPayloadS.Text
                newAlarm.When2 = cbxMQTT.SelectedIndex
                wMain.mqttSubscribeAdd(txtTopicS.Text)

        End Select

        Select Case CType(tabWhat.SelectedItem, TabItem).Name
            Case tbpMessage.Name
                newAlarm.Task = "message"
                newAlarm.Data1 = txtZprava.Text
                newAlarm.Data3 = txtZvuk.Text

            Case tbpShutdown.Name
                newAlarm.Task = "shutdown"
                newAlarm.Data1 = cbxShutdown.SelectedItem.ToString
                newAlarm.Data2 = cbxShutdown.SelectedIndex

            Case tbpRun.Name
                newAlarm.Task = "run"
                If txtRun.Tag IsNot Nothing AndAlso Not txtRun.Tag.ToString = "" Then
                    newAlarm.Data2 = 1
                    newAlarm.Data1 = txtRun.Tag.ToString
                Else
                    newAlarm.Data2 = 2
                    newAlarm.Data1 = txtRun.Text
                End If


            Case tbpClose.Name
                newAlarm.Data1 = lblShut.Text
                newAlarm.Data2 = ProcessIDwhat
                If optShut0.IsChecked = True Then newAlarm.Task = "close0" : newAlarm.Data1 = lblShut.Tag.ToString
                If optShut1.IsChecked = True Then newAlarm.Task = "close1"
                If optShut2.IsChecked = True Then newAlarm.Task = "close2"

            Case tbpEmail.Name
                newAlarm.Task = "email"
                newAlarm.Data1 = txtTo.Text
                newAlarm.Data3 = txtZprava.Text
                Dat.Options.Email = txtFrom.Text
                Dat.Options.EmailPass = myString.Encrypt(passBox.Password, Application.Pass)

            Case tbpAlarm.Name
                newAlarm.Task = "alarm"
                newAlarm.Data1 = CType(lvwAlarm.SelectedItems(0), clsSetting.clsAlarm).ID & ". " & If(optAktivate.IsChecked, FindResource("tactivate").ToString, FindResource("tdeactivate").ToString)
                newAlarm.Data2 = CType(lvwAlarm.SelectedItems(0), clsSetting.clsAlarm).ID
                newAlarm.Data3 = optAktivate.IsChecked.ToString

            Case tbpCopy.Name
                newAlarm.Task = "copy"
                newAlarm.Data1 = txtFileFrom.Text
                newAlarm.Data3 = txtFileTo.Text
                newAlarm.Data2 = CInt(ckbOverwrite.IsChecked)

            Case tbpDisplay.Name
                newAlarm.Task = "display"
                newAlarm.Data1 = cbxDisplay.SelectedItem.ToString
                newAlarm.Data2 = cbxDisplay.SelectedIndex

            Case tbpMQTTp.Name
                newAlarm.Task = "mqtt"
                newAlarm.Data2 = If(ckbReconnect.IsChecked, 2, 0)
                If ckbReconnect.IsChecked Then
                    newAlarm.Data1 = "reconnect"
                Else
                    newAlarm.Data1 = txtTopicP.Text
                    newAlarm.Data3 = txtPayloadP.Text
                End If


            Case tbpKeys.Name
                newAlarm.Task = "keys"
                newAlarm.Data1 = txtKeys.Text

            Case tbpMute.Name
                newAlarm.Task = "mute"

        End Select
        newAlarm.ID = Dat.AlarmNewID
        Dat.AlarmMem.Add(newAlarm)
        cmdAkt.IsEnabled = True
    End Sub

    Private Sub lbxSeznam_SelectionChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles lvwAlarm.SelectionChanged
        If IsNothing(lvwAlarm.SelectedItem) Or wMain.bgwFile.IsBusy Then
            cmdOdebrat.IsEnabled = False
            Exit Sub
        Else
            cmdOdebrat.IsEnabled = True
        End If

        If Not Dat.AlarmMem.Count = 0 Then
            Dim Alarm = TryCast(lvwAlarm.SelectedItem, clsSetting.clsAlarm)
            Select Case Alarm.Watch
                Case "dir"
                    tabWhen.SelectedItem = tbpDir
                    lblFolder.Text = Alarm.When4
                    cbxChange.SelectedIndex = Alarm.When2

                Case "time"
                    tabWhen.SelectedItem = tbpTime
                    txtHodin.Text = Strings.Left(Alarm.When1, 2)
                    txtMinut.Text = Strings.Right(Alarm.When1, 2)
                    ckbInterval.IsChecked = If(Alarm.When2 = 1, True, False)

                Case "window"
                    tabWhen.SelectedItem = tbpWindow
                    txtWnd.Text = Alarm.When1
                    ProcessIDwhen = Alarm.When2
                    Select Case Alarm.When3
                        Case 1
                            rbnW0.IsChecked = True
                        Case 2
                            rbnW1.IsChecked = True
                        Case 3
                            txtWnd.Text = Alarm.When2.ToString
                            rbnW2.IsChecked = True
                    End Select

                Case "file"
                    tabWhen.SelectedItem = tbpFile
                    txtValue.Text = Alarm.When1
                    cbxSymbolV.SelectedIndex = Alarm.When2
                    txtWatchFile.Text = Alarm.When4

                Case "mqtt"
                    tabWhen.SelectedItem = tbpMQTT
                    txtTopicS.Text = Alarm.When1
                    txtPayloadS.Text = Alarm.When4
                    cbxMQTT.SelectedIndex = Alarm.When2

            End Select

            Select Case Alarm.Task
                Case "message"
                    txtZprava.Text = Alarm.Data1
                    txtZvuk.Text = Alarm.Data3
                Case "shutdown"
                    tabWhat.SelectedItem = tbpShutdown
                    cbxShutdown.SelectedIndex = Alarm.Data2
                Case "run"
                    tabWhat.SelectedItem = tbpRun
                    txtRun.Text = Alarm.Data1
                Case "close0"
                    tabWhat.SelectedItem = tbpClose
                    optShut0.IsChecked = True
                    lblShut.Text = Alarm.Data2.ToString
                    ProcessIDwhat = Alarm.Data2
                Case "close1"
                    tabWhat.SelectedItem = tbpClose
                    optShut1.IsChecked = True
                    lblShut.Text = Alarm.Data1
                    ProcessIDwhat = Alarm.Data2
                Case "close2"
                    tabWhat.SelectedItem = tbpClose
                    optShut2.IsChecked = True
                    lblShut.Text = Alarm.Data1
                    ProcessIDwhat = Alarm.Data2
                Case "email"
                    tabWhat.SelectedItem = tbpEmail
                    txtTo.Text = Alarm.Data1
                    txtZprava.Text = Alarm.Data3
                Case "alarm"
                    tabWhat.SelectedItem = tbpAlarm
                    If CBool(Alarm.Data3) Then
                        optAktivate.IsChecked = True
                    Else
                        optDeaktivate.IsChecked = True
                    End If
                Case "copy"
                    tabWhat.SelectedItem = tbpCopy
                    txtFileFrom.Text = Alarm.Data1
                    txtFileTo.Text = Alarm.Data3
                    ckbOverwrite.IsChecked = CBool(Alarm.Data2)
                Case "display"
                    tabWhat.SelectedItem = tbpDisplay
                    cbxDisplay.SelectedIndex = Alarm.Data2
                Case "mqtt"
                    tabWhat.SelectedItem = tbpMQTTp
                    txtTopicP.Text = Alarm.Data1
                    txtPayloadP.Text = Alarm.Data3
                    Dim switch = If(Alarm.Data2 = 2, True, False)
                    ckbReconnect.IsChecked = switch
                    txtTopicP.IsEnabled = Not switch
                    txtPayloadP.IsEnabled = Not switch
                Case "keys"
                    tabWhat.SelectedItem = tbpKeys
                    txtKeys.Text = Alarm.Data1
                Case "mute"
                    tabWhat.SelectedItem = tbpMute

                Case Else
                    tabWhat.SelectedItem = tbpMessage
                    txtZprava.Text = Alarm.Data1
                    txtZvuk.Text = Alarm.Data3
            End Select
        End If
    End Sub

#End Region

#Region " Zpráva "

    Private Sub cmdZvuk_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdZvuk.Click
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = If(Lge, "Vyberte zvukovou zprávu", "Choose the wave message")
        dlg.Filter = If(Lge, "Zvukové soubory", "Wave files") & " (*.MP3;*.WAV)|*.mp3;*.wav"
        dlg.CheckFileExists = True
        If dlg.ShowDialog = True Then
            txtZvuk.Text = dlg.FileName
        Else
            txtZvuk.Text = ""
        End If
        Dat.Options.AlarmMelody = txtZvuk.Text
    End Sub

#End Region

#Region " Složka "
    Private Sub cmdFolder_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdFolder.Click
        Dim wDialogFolder As New wpfDialogFolder
        wDialogFolder.Owner = wSetting
        wDialogFolder.SystemFolders = False
        wDialogFolder.UserFolders = True
        If wDialogFolder.ShowDialog() Then
            lblFolder.Text = wDialogFolder.SelectFolder
        Else
            lblFolder.Text = ""
        End If
        Dat.Options.AlarmFolder = lblFolder.Text
    End Sub

#End Region

#Region " File "

    Private Sub txtValue_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtValue.TextChanged
        If IsNumeric(txtValue.Text) Then
            cbxSymbolV.IsEnabled = True
        Else
            cbxSymbolV.SelectedIndex = 2
            cbxSymbolV.IsEnabled = False
        End If
    End Sub
    Private Sub optValue_Checked(sender As Object, e As RoutedEventArgs) Handles optValue.Checked, optValue.Unchecked
        Dim stav As Boolean = CBool(optValue.IsChecked)
        cbxSymbolV.IsEnabled = stav
        txtValue.IsEnabled = stav
        If IsNumeric(txtValue.Text) = False Then cbxSymbolV.SelectedIndex = 2
    End Sub
    Private Sub cmdWatchFile_Click(sender As Object, e As RoutedEventArgs) Handles cmdWatchFile.Click
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = If(Lge, "Vyberte soubor", "Choose a file")
        dlg.Filter = If(Lge, "Soubory", "Files") & " (*.*)|*.*"
        dlg.CheckFileExists = True
        If dlg.ShowDialog = True Then
            txtWatchFile.Text = dlg.FileName
        Else
            txtWatchFile.Text = ""
        End If
    End Sub

#End Region

#Region " Process Options "

    Private Function getProcessID() As Integer
        Dim formProcess As New wpfProcess
        formProcess.Owner = wSetting
        formProcess.ShowDialog()
        Return formProcess.ProcessID
    End Function

    Private ProcessIDwhen As Integer
    Private Sub btnWnd_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnWnd.Click
        ProcessIDwhen = getProcessID()
        If ProcessIDwhen = 0 Then Exit Sub
        ShowProcessWndType()
    End Sub

    Private Sub ShowProcessWndType()
        If clsSystem.isProcess(ProcessIDwhen) Then
            Dim thisProcess As Process = Process.GetProcessById(ProcessIDwhen, ".")
            If rbnW0.IsChecked Then txtWnd.Text = thisProcess.MainWindowTitle
            If rbnW1.IsChecked Then txtWnd.Text = thisProcess.MainWindowTitle
            If rbnW2.IsChecked Then txtWnd.Text = thisProcess.Id.ToString
            txtWnd.Tag = thisProcess.MainWindowTitle
        End If
    End Sub

    Private Sub rbnW0_Checked(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles rbnW0.Checked, rbnW1.Checked, rbnW2.Checked
        ShowProcessWndType()
    End Sub

    Private ProcessIDwhat As Integer
    Private Sub cmdShut_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdShut.Click
        ProcessIDwhat = getProcessID()
        If ProcessIDwhat = 0 Then Exit Sub
        ShowProcessShutType()
    End Sub

    Private Sub ShowProcessShutType()
        If clsSystem.isProcess(ProcessIDwhat) Then
            Dim thisProcess As Process = Process.GetProcessById(ProcessIDwhat, ".")
            If optShut0.IsChecked Then lblShut.Text = thisProcess.Id.ToString
            If optShut1.IsChecked Then lblShut.Text = thisProcess.ProcessName
            If optShut2.IsChecked Then lblShut.Text = thisProcess.MainWindowTitle
            lblShut.Tag = thisProcess.ProcessName
        End If
    End Sub

    Private Sub optShut0_Checked(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles optShut0.Checked, optShut1.Checked, optShut2.Checked
        ShowProcessShutType()
    End Sub
#End Region

#Region " Spustit program "
    Private Sub txtRun_TextChanged(sender As Object, e As RoutedEventArgs) Handles txtRun.TextChanged
        Dim Items = BoxColumnList.seznam.Where(Function(a) a.Name.ToLower.StartsWith(txtRun.Text.ToLower))
        If Items.Count = 1 Then
            txtRun.Text = Items(0).Name
            imgApp.Source = Items(0).ImgSource
            txtRun.Tag = Nothing
        Else
            If txtRun.Tag Is Nothing Then imgApp.Source = Nothing
        End If
    End Sub

    Private Sub cmdRun_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdRun.Click
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = If(Lge, "Vyberte proram", "Choose the program")
        dlg.Filter = If(Lge, "Aplikace", "Applications") & " (*.exe)|*.exe|" _
            & If(Lge, "Všechny soubory", "All files") & " (*.*)|*.*"
        dlg.CheckFileExists = True
        If dlg.ShowDialog Then
            txtRun.Tag = dlg.FileName
            txtRun.Text = myFile.Name(dlg.FileName)
            Dim IconExtractor As New clsExtractIcon(dlg.FileName)
            imgApp.Source = IconExtractor.GetImageSource
            IconExtractor.Dispose()
        Else
            txtRun.Text = ""
            imgApp.Source = Nothing
            txtRun.Tag = Nothing
        End If
    End Sub

#End Region

#Region " Vypnout PC "

    Private Sub cbxShutdown_SelectionChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles cbxShutdown.SelectionChanged
        Select Case cbxShutdown.SelectedIndex
            Case 0
                picPower.Source = New BitmapImage(New Uri(imgCesta + "shutdown/lock128.png", UriKind.Relative))
            Case 1
                picPower.Source = New BitmapImage(New Uri(imgCesta + "shutdown/standby128.png", UriKind.Relative))
            Case 2
                picPower.Source = New BitmapImage(New Uri(imgCesta + "shutdown/logoff128.png", UriKind.Relative))
            Case 3
                picPower.Source = New BitmapImage(New Uri(imgCesta + "shutdown/turnoff128.png", UriKind.Relative))
            Case 4
                picPower.Source = New BitmapImage(New Uri(imgCesta + "shutdown/restart128.png", UriKind.Relative))
        End Select
    End Sub

#End Region

#Region " Display output "

    Private Sub cbxDisplay_SelectionChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles cbxDisplay.SelectionChanged
        Select Case cbxDisplay.SelectedIndex
            Case 0
                picDisplay.Source = myBitmap.UriToImageSource(New Uri(imgCesta + "display/DisplayInternal166.ico", UriKind.Relative))
            Case 1
                picDisplay.Source = myBitmap.UriToImageSource(New Uri(imgCesta + "display/DisplayClone166.ico", UriKind.Relative))
            Case 2
                picDisplay.Source = myBitmap.UriToImageSource(New Uri(imgCesta + "display/DisplayExtend166.ico", UriKind.Relative))
            Case 3
                picDisplay.Source = myBitmap.UriToImageSource(New Uri(imgCesta + "display/DisplayExternal166.ico", UriKind.Relative))
        End Select
    End Sub

#End Region

#Region " Email "

    Private WithEvents Email As New clsEmail

    Private Function EmailInputCheck() As Boolean
        If Not txtFrom.Text.Contains("@") Then
            Dim wDialog As New wpfDialog(wSetting, If(Lge, "Chybí email uživatele.", "User email missing."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
            wDialog.ShowDialog()
            txtFrom.Focus()
            Return False
        End If
        If passBox.Password = "" Then
            Dim wDialog As New wpfDialog(wSetting, If(Lge, "Chybí heslo uživatele emailu.", "User email password missing."), Me.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
            wDialog.ShowDialog()
            passBox.Focus()
            Return False
        End If
        Return True
    End Function

    Private Sub cmdTest_Click(sender As Object, e As RoutedEventArgs) Handles cmdTest.Click
        If EmailInputCheck() Then
            cmdTest.IsEnabled = False
            Email.ChangeUser(txtFrom.Text, passBox.Password)
            Email.Send(Email.CreateMail(txtFrom.Text, "STARTzjs Test Email", ""))
        End If
    End Sub

    Private Sub SentEvent(Chyba As Exception) Handles Email.EmailSent
        If Chyba Is Nothing Then
            Dim wDialog As New wpfDialog(wSetting, If(Lge, "Test proběhl úspěšně. Zkontrolujte si emailovou schránku.", "Test was successfull. Check your mailbox."), Application.Title, wpfDialog.Ikona.ok, "OK")
            wDialog.ShowDialog()
            Dat.Options.Email = txtFrom.Text
            Dat.Options.EmailPass = myString.Encrypt(passBox.Password, Application.Pass)
        Else

            If txtFrom.Text.Contains("@gmail.com") Then
                Dim msg As String = If(Lge, "Pokud chcete odesílat emaily z této aplikace, musíte:", "To send emails from this app, you need to:") & NR
                msg &= "1.) " & If(Lge, "Povolit přístup méně zabezpečených aplikací.", "Allow less secure application access.") & NR
                msg &= "2.) " & If(Lge, "Do gmail schránky obdržíte bezpečností upozornění, kde je třeba potvrdit, že jste to byli vy, kdo chtěl poslat email.",
                                        "You will receive warning email in your gmail box, where you need to confirm that you have tried to send the email.")

                Dim wDialog As New wpfDialog(wSetting, msg, Application.Title, wpfDialog.Ikona.varovani, If(Lge, "Otevřít web", "Open web"), If(Lge, "Zavřít", "Close"))
                If wDialog.ShowDialog() = True Then myLink.Start(wSetting, "http://www.google.com/settings/security/lesssecureapps")
            Else
                Dim wDialog As New wpfDialog(wSetting, Chyba.Message, Application.Title, wpfDialog.Ikona.varovani, If(Lge, "Zavřít", "Close"))
                wDialog.ShowDialog()
            End If
            Dat.Options.EmailPass = ""
        End If
        cmdTest.IsEnabled = True
    End Sub

#End Region

#Region " File Copy "
    Private Sub cmdFileFrom_Click(sender As Object, e As RoutedEventArgs) Handles cmdFileFrom.Click
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = If(Lge, "Vyberte nějaký soubor", "Choose a file")
        dlg.Filter = If(Lge, "Soubory", "Files") & " (*.*)|*.*"
        dlg.CheckFileExists = True
        If dlg.ShowDialog = True Then
            If dlg.FileName = txtFileTo.Text Then
                Dim wDialog As New wpfDialog(wSetting, If(Lge, "Cílový soubor nemůže mít stejné umístění jako vstupní soubor.", "Same file name must not have same destination."), wMain.Title, wpfDialog.Ikona.chyba, If(Lge, "Zavřít", "Close"))
                wDialog.ShowDialog()
                Exit Sub
            End If
            txtFileFrom.Text = dlg.FileName
        Else
            txtFileFrom.Text = ""
        End If
    End Sub

    Private Sub cmdFileTo_Click(sender As Object, e As RoutedEventArgs) Handles cmdFileTo.Click
        Dim dlg As New Microsoft.Win32.SaveFileDialog()
        dlg.Title = If(Lge, "Vyberte umístění souboru", "Choose a file destination")
        dlg.Filter = If(Lge, "Soubory", "Files") & " (*.*)|*.*"
        If Not txtFileFrom.Text = "" Then dlg.FileName = myFile.Name(txtFileFrom.Text)
        If dlg.ShowDialog = True Then
            If dlg.FileName = txtFileFrom.Text Then
                Dim wDialog As New wpfDialog(wSetting, If(Lge, "Cílový soubor nemůže mít stejné umístění jako vstupní soubor.", "Same file name must not have same destination."), wMain.Title, wpfDialog.Ikona.chyba, If(Lge, "Zavřít", "Close"))
                wDialog.ShowDialog()
                Exit Sub
            End If
            txtFileTo.Text = dlg.FileName
        Else
            txtFileTo.Text = ""
        End If
    End Sub




#End Region

End Class
