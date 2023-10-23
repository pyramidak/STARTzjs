Class ppfSave

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private myCloud As New clsCloud

#Region " Loaded "

    Private Sub ppfSave_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
        Select Case Dat.selCloud
            Case Cloud.Documents
                rbtDocuments.IsChecked = True
            Case Cloud.OneDrive
                rbtOneDrive.IsChecked = True
            Case Cloud.DropBox
                rbtDropBox.IsChecked = True
            Case Cloud.GoogleDisk
                rbtGoogleDrive.IsChecked = True
            Case Cloud.Sync
                rbtSync.IsChecked = True
        End Select
        rbtDropBox.IsEnabled = myCloud.DropBoxExist
        rbtOneDrive.IsEnabled = myCloud.OneDriveExist
        rbtGoogleDrive.IsEnabled = myCloud.GoogleDriveExist
        rbtSync.IsEnabled = myCloud.SyncExist
    End Sub

#End Region

#Region " Save "

    Private Sub btnSave_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnSave.Click
        btnSave.IsEnabled = False
        If rbtDocuments.IsChecked Then
            Dat.selCloud = Cloud.Documents
        ElseIf rbtOneDrive.IsChecked Then
            Dat.selCloud = Cloud.OneDrive
        ElseIf rbtDropBox.IsChecked Then
            Dat.selCloud = Cloud.DropBox
        ElseIf rbtGoogleDrive.IsChecked Then
            Dat.selCloud = Cloud.GoogleDisk
        ElseIf rbtSync.IsChecked Then
            Dat.selCloud = Cloud.Sync
        End If
        Dat.Save(False)

        If myFile.Exist(myCloud.NewAppPath(Dat.selCloud, "pyramidak\" & mySystem.User & "START.sxml")) Then
            Dim wDialog As New wpfDialog(Application.SettingWindow, If(mySystem.LgeCzech,
                "V novém umístění již datový soubor existuje." + NR + NR + "Chcete použít nalezený soubor nebo ho nahradit aktuálním?",
                "In new location data file already exists." + NR + NR + "Do you want to load this found file or replace it with actual one?"),
                                         Me.Title, wpfDialog.Ikona.dotaz, If(mySystem.LgeCzech, "Použít", "Load"), If(mySystem.LgeCzech, "Nahradit", "Replace"))
            If wDialog.ShowDialog() Then
                Dat = New clsData
                Me.Cursor = Cursors.Wait
                BoxColumnList = New clsBoxColumnList(Dat.Items)
                BoxColumnList.Load(Dat.Columns)

                wMain.myHotKey = New HotKeyHost(wMain)
                BoxColumnList.seznam.ActivateHotkey()
                Me.Cursor = Cursors.Arrow
            End If
        End If
        Dat.Save(True)
    End Sub

#End Region

#Region " Ostatní "

    Private Sub rbtCloud_Checked(sender As Object, e As RoutedEventArgs) Handles rbtDropBox.Checked, rbtDocuments.Checked, rbtOneDrive.Checked, rbtGoogleDrive.Checked, rbtSync.Checked
        btnSave.IsEnabled = True
        If rbtDocuments.IsChecked And Dat.selCloud = Cloud.Documents Then
            btnSave.IsEnabled = False
        ElseIf rbtOneDrive.IsChecked And Dat.selCloud = Cloud.OneDrive Then
            btnSave.IsEnabled = False
        ElseIf rbtDropBox.IsChecked And Dat.selCloud = Cloud.DropBox Then
            btnSave.IsEnabled = False
        ElseIf rbtGoogleDrive.IsChecked And Dat.selCloud = Cloud.GoogleDisk Then
            btnSave.IsEnabled = False
        ElseIf rbtSync.IsChecked And Dat.selCloud = Cloud.Sync Then
            btnSave.IsEnabled = False
        End If
    End Sub

#End Region


End Class
