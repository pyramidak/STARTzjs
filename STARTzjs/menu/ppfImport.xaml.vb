Imports System.Collections.ObjectModel

Class ppfImport

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()
    Private Lge As Boolean = mySystem.LgeCzech

    Private Sub lbxMenu_Loaded(sender As Object, e As RoutedEventArgs) Handles lbxMenu.Loaded
        Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
    End Sub

    Private Sub lbxMenu_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles lbxMenu.SelectionChanged
        btnAkce.IsEnabled = If(lbxMenu.SelectedItems.Count = 0, False, True)
    End Sub

    Private Sub btnExport_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnExport.Click
        Dim dlg As New Microsoft.Win32.SaveFileDialog()
        dlg.Title = If(Lge, "Vyberte místo pro zálohování STARTzjs datového souboru", "Find place to backup STARTzjs data file")
        dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        dlg.Filter = "eXtensible Markup Language (*START.sxml)|*START.sxml"
        dlg.FileName = mySystem.User & "START.sxml"
        If dlg.ShowDialog = False Then Exit Sub

        If myFile.Exist(dlg.FileName) Then
            Dim wDialog As New wpfDialog(wSetting, If(Lge, "Ve vybrané složce již soubor existuje.", "In selected folder file already exists."), "STARTzjs menu export", wpfDialog.Ikona.varovani, "Zavřít")
            wDialog.ShowDialog()
            Exit Sub
        End If
        Dat.CopyTo(dlg.FileName)
    End Sub

    Private Sub btnAkce_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnAkce.Click
        Dim item As ListBoxItem = CType(lbxMenu.SelectedItems(0), ListBoxItem)
        Select Case item.Tag.ToString
            Case "Backup"
                ImportBackup()
            Case "Desktop"
                ImportDesktop()
            Case "TotalCmd"
                ImportTotalCmd()
            Case "Control"
                ImportControl()
            Case "Shutdown"
                ImportShutdown()
            Case "TaskBar"
                ImportTaskBar()
            Case "Vektiva"
                ImportVektiva()
            Case "UWP"
                ImportStore()
        End Select
    End Sub

    Private Sub AddStartBoxItem(DefItems As Collection(Of clsData.clsDefItem), toTheme As String, imgFolder As String)
        BoxColumnList.AddColumn(toTheme)
        For Each Def In DefItems
            Dim Item As New clsSetting.clsItem
            Item.Name = Def.Name
            Item.Column = toTheme
            Item.Path = Def.Path
            Item.IconPath = imgCesta + imgFolder + Def.Icon
            Item.BoxType = BoxType.Start
            BoxColumnList.AddItem(Item)
        Next
    End Sub

#Region " Backup "

    Private Sub ImportBackup()
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = If(Lge, "Najděte", "Find") & " STARTzjs data file"
        dlg.Filter = "eXtensible Markup Language (*START.sxml)|*START.sxml;*progs.sxml"
        dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        dlg.CheckFileExists = True
        dlg.Multiselect = False
        If dlg.ShowDialog Then Dat.ImportData(dlg.FileName, True)
    End Sub

#End Region

#Region " Desktop "

    Private Sub ImportDesktop()
        Dim PlochaUser As String = My.Computer.FileSystem.SpecialDirectories.Desktop
        Dim arrFiles As New ArrayList
        arrFiles.AddRange(myFolder.Files(PlochaUser, "*.*"))
        If mySystem.Current.Number < 6 Then
            Dim PlochaAllUsers As String = PlochaUser.Substring(0, 1) & ":\Documents and Settings\All Users\" & myFolder.Name(PlochaUser)
            arrFiles.AddRange(myFolder.Files(PlochaAllUsers, "*.lnk", True))
        End If
        Dim sTheme As String = If(Lge, "Plocha", "Desktop")

        Dim DefItems As New Collection(Of clsData.clsDefItem)
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Knihovny", "Libraries"), "START_libraries", "Libraries128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Domací skupina", "Homegroup"), "START_homegroup", "Homegroup128.png"))
        DefItems.Add(New clsData.clsDefItem(mySystem.User, "START_user", "User128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Počítač", "Computer"), "START_computer", "Computer128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Síť", "Network"), "START_network", "Network128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Koš", "Recycle Bin"), "START_bin", "RecycleBin_full128.png"))
        AddStartBoxItem(DefItems, sTheme, "folder/")

        BoxColumnList.AddItems(arrFiles, sTheme, BoxType.File)
    End Sub

#End Region

#Region " TotalCmd "

    Private Sub ImportTotalCmd()
        Dim sRoaming As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Dim sBar As String = sRoaming + "\GHISLER\default.bar"
        If myFile.Exist(sBar) = False Then Exit Sub
        Dim arrFiles As New ArrayList
        Dim Buttons As Integer = myINI.GetSetting(sBar, "Buttonbar", "Buttoncount", 0)
        For btn As Integer = 1 To Buttons
            arrFiles.Add(myINI.GetSetting(sBar, "Buttonbar", "button" & btn, ""))
        Next
        Dim sTheme As String = "TotalCommander"
        BoxColumnList.AddColumn(sTheme)
        BoxColumnList.AddItems(arrFiles, sTheme, BoxType.File)
    End Sub


#End Region

#Region " Shutdown "

    Private Sub ImportShutdown()
        Dim DefItems As New Collection(Of clsData.clsDefItem)
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Zamknout", "Lock"), "START_lock", "lock128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Uspat", "StandBy"), "START_standby", "standby128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Odhlásit", "LogOff"), "START_logoff", "logoff128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Vypnout", "PowerOff"), "START_turnoff", "turnoff128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Restartovat", "Restart"), "START_restart", "restart128.png"))
        AddStartBoxItem(DefItems, If(Lge, "Výchozí", "Default"), "shutdown/")
    End Sub

#End Region

#Region " ControlPanel "

    Private Sub ImportControl()
        Dim sTheme As String = If(Lge, "Ovládací panely", "Control Panels")

        Dim DefItems As New Collection(Of clsData.clsDefItem)
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Ovládací panely", "Controls"), "START_control", "control128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Zvuk", "Sound"), "START_sound", "sound128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Zařízení a tiskárny", "Printers"), "START_printers", "printers128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Možnosti napájení", "Power"), "START_power", "power128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Možnosti rozlišení", "Monitor"), "START_monitor", "monitor128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Správa barev", "Colors"), "START_colors", "colors128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Síťová připojení", "Network Connections"), "START_connections", "NetworkConnections128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Vzdálená plocha", "Remote Connection"), "START_remote", "RemoteConnection128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Možnosti výkonu", "Performance Options"), "START_performance", "PerformanceOptions128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Plánovač úloh", "Task scheduler"), "START_scheduler", "scheduler128.png"))
        DefItems.Add(New clsData.clsDefItem(If(Lge, "Odinstalovat program", "Uninstall program"), "START_uninstall", "uninstall128.png"))
        AddStartBoxItem(DefItems, sTheme, "control/")

        Dim Item As New clsSetting.clsItem
        Item.Column = sTheme
        Item.BoxType = BoxType.File
        Item.Admin = False
        Item.Path = Chr(34) & Environment.GetFolderPath(Environment.SpecialFolder.System) + "\cleanmgr.exe" & Chr(34) & " /d" & mySystem.DiskLetter
        Item.Name = If(Lge, "Vyčištění disku", "Disk Cleanup")
        BoxColumnList.AddItem(Item)

        Item = New clsSetting.clsItem
        Item.Column = sTheme
        Item.BoxType = BoxType.File
        Item.Admin = True
        Item.Path = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\cmd.exe"
        Item.Name = If(Lge, "Příkazová řádka", "Command Prompt")
        BoxColumnList.AddItem(Item)

        Item = New clsSetting.clsItem
        Item.Column = sTheme
        Item.BoxType = BoxType.File
        Item.Admin = True
        Item.Path = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\regedit.exe"
        Item.Name = If(Lge, "Editor registru", "Registry Editor")
        BoxColumnList.AddItem(Item)
    End Sub

#End Region

#Region " Desktop "

    Private Sub ImportTaskBar()
        Dim TaskBar As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"
        Dim sTheme As String = If(Lge, "Hlavní panel", "TaskBar")
        BoxColumnList.AddColumn(sTheme)
        BoxColumnList.AddItems(myFolder.Files(TaskBar, "*.lnk"), sTheme, BoxType.File)
    End Sub

#End Region

#Region " Vektiva "

    Private Sub ImportVektiva()
        Dim wDialog As New wpfDialog(wSetting, FindResource("vektivaapi").ToString, FindResource("vektiva").ToString, wpfDialog.Ikona.tuzka, "OK", "Zavřít", True, False, "", True)
        wDialog.Input = "REMOTE_ID/API_KEY/DEVICE_ID"
        If wDialog.ShowDialog() = False Then Exit Sub
        If wDialog.Input = "" Then Exit Sub

        Dim path As String = "https://vektiva.online/api/" & wDialog.Input

        Dim sTheme As String = "Vektiva"
        BoxColumnList.AddColumn(sTheme)

        Dim Item As New clsSetting.clsItem
        Item.Name = FindResource("vektivaopen").ToString
        Item.Column = sTheme
        Item.Path = path & "/open"
        Item.IconPath = "§WindowOpened"
        Item.BoxType = BoxType.Link
        Item.InputKey = Key.A
        Item.ModKey = ModifierKeys.Shift Or ModifierKeys.Alt
        Item.HideStart = True
        BoxColumnList.AddItem(Item)

        Item = New clsSetting.clsItem
        Item.Name = FindResource("vektivaclose").ToString
        Item.Column = sTheme
        Item.Path = path & "/close"
        Item.IconPath = "§WindowClosed"
        Item.BoxType = BoxType.Link
        Item.InputKey = Key.S
        Item.ModKey = ModifierKeys.Shift Or ModifierKeys.Alt
        Item.HideStart = True
        BoxColumnList.AddItem(Item)

        Item = New clsSetting.clsItem
        Item.Name = FindResource("vektivastop").ToString
        Item.Column = sTheme
        Item.Path = path & "/stop"
        Item.IconPath = "§Gears"
        Item.BoxType = BoxType.Link
        Item.HideStart = True
        BoxColumnList.AddItem(Item)

        Item = New clsSetting.clsItem
        Item.Name = FindResource("vektivafix").ToString
        Item.Column = sTheme
        Item.Path = path & "/fix"
        Item.IconPath = "§GearFix"
        Item.BoxType = BoxType.Link
        Item.HideStart = True
        BoxColumnList.AddItem(Item)
    End Sub

#End Region

#Region " Store "

    Private Sub ImportStore()
        btnAkce.IsEnabled = False
        BoxColumnList.AddColumn("Store")

        bgwSearch.RunWorkerAsync()
    End Sub

    Private WithEvents bgwSearch As New System.ComponentModel.BackgroundWorker
    Private Sub bgwSearch_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles bgwSearch.DoWork
        myApps = New clsApps()
    End Sub

    Private Sub bgwSearch_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles bgwSearch.RunWorkerCompleted
        For Each app In myApps.All()
            Dim item As New clsSetting.clsItem
            item.BoxType = BoxType.UWP
            item.Column = "Store"

            item.Name = app.Name
            item.Path = app.Path

            BoxColumnList.AddItem(item)
        Next
    End Sub

#End Region

End Class
