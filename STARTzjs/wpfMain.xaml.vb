Imports System.Collections.ObjectModel
Imports System.Windows.Threading
Imports Microsoft.Win32
Imports MQTTnet.Protocol

Class wpfMain

#Region " Window "

    Private Lge As Boolean = mySystem.LgeCzech
    Public myHotKey As HotKeyHost

    Private Sub ShowWindowInCenterOfMousePoint()
        Dim Obrazovka As Point = New Point(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight)
        Dim Mys As Point = myWindow.PixelToPPI(myWindow.GetMousePosition, True)

        Me.Left = Mys.X - CInt(Me.ActualWidth / 2)
        If Me.Left < 0 Or Me.ActualWidth > Obrazovka.X Then
            Me.Left = If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Left, 60, 1)
        Else
            If Me.Left + Me.ActualWidth > Obrazovka.X Then
                Me.Left = Obrazovka.X - Me.ActualWidth
            End If
        End If

        Me.Top = Mys.Y - CInt(Me.Height / 2)
        If Me.Top < 0 Or Me.ActualHeight > Obrazovka.Y Then
            Me.Top = If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Top, 40, 1)
        Else
            If Me.Top + Me.ActualHeight > Obrazovka.Y Then
                Me.Top = Obrazovka.Y - Me.ActualHeight - 20
            End If
        End If
    End Sub

    Private Sub wpfMain_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        If BoxColumnList IsNot Nothing Then BoxColumnList.Resize()
    End Sub

    Private Sub wpfMain_StateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.StateChanged
        If Me.WindowState = WindowState.Normal Then
            ShowWindowInCenterOfMousePoint()
        End If
    End Sub

    Private Sub wpfMain_Closed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Closed
        Dat.Save(False)
        HotKeyDispose()
    End Sub

    Private Sub HotKeyDispose()
        For Each box In BoxColumnList.seznam
            If box.HKey.Enabled Then
                myHotKey.RemoveHotKey(box.HKey)
            End If
        Next
        myHotKey.Dispose()
    End Sub

    Private Sub wpfMain_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        If IsNothing(bgwSearch) = False AndAlso bgwSearch.IsBusy Then e.Cancel = True
    End Sub

    Private Sub wpfMain_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        If mySystem.isAppRunning(Application.ProcessName, mySystem.User) Then Application.OneProcess = False
        AddHandler SystemEvents.PowerModeChanged, AddressOf SystemEvents_PowerModeChanged
        appCesta = myFolder.Path(System.Reflection.Assembly.GetExecutingAssembly().Location)
        mySystem.LoadLanguageDictionary(mySystem.LgeCzech, "resource")

        Dim MenuItemMargin As Thickness
        If mySystem.Framework < 4 Then
            MenuItemMargin = New Thickness(-30, 0, 0, 0)
        Else
            If mySystem.Current.Number < 8 Then
                MenuItemMargin = New Thickness(-30, 0, 0, 0)
            Else
                MenuItemMargin = New Thickness(-38, 0, 0, 0)
            End If
        End If
        Me.Resources("MenuItemMargin") = MenuItemMargin

        Me.ContextMenu = Nothing
        Me.Title = "STARTzjs " & Application.Version
        Me.Left = 0 - Me.ActualWidth
        Me.Visibility = Visibility.Visible
        ItemSize(0) = New Size(175, 48)
        ItemSize(1) = New Size(190, 64)
        ItemSize(2) = New Size(215, 80)
        ItemSize(3) = New Size(230, 96)
    End Sub

    Private Sub wMain_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Dim myGlass As New clsGlass
        myGlass.TransparencyEffect(Me)
        Dat = New clsData
        myWinColor = New clsWinColors 'zjišťuje barvu windows téma a nastaví výchozí
        BoxColumnList = New clsBoxColumnList(Dat.Items)
        BoxColumnList.Load(Dat.Columns)
        myHotKey = New HotKeyHost(Me)
        If Application.OneProcess Then BoxColumnList.seznam.ActivateHotkey()
        ZJShareMem.Open("ZJS")
        timZJS = New DispatcherTimer
        timZJS.Interval = TimeSpan.FromSeconds(5)
        timZJS.IsEnabled = True
        timAlarm = New DispatcherTimer
        timAlarm.Interval = TimeSpan.FromSeconds(1)
        timFile = New DispatcherTimer
        timFile.Interval = TimeSpan.FromSeconds(15)
        timResume = New DispatcherTimer
        timResume.Interval = TimeSpan.FromSeconds(5)

        Dim Arg As String = ""
        Dim Args() As String = Environment.GetCommandLineArgs
        If UBound(Args) > 0 Then Arg = Args(1)
        If Arg = "win" Or Arg = "-win" Or Arg = "/win" Then
            Me.WindowState = WindowState.Minimized
        Else
            ShowWindowInCenterOfMousePoint()
        End If

        If Not Dat.AlarmMem.Count = 0 And Application.OneProcess Then
            Dat.Options.CorrectTaskNames()
            WatchFolder = New clsWatchFolder(Dat.AlarmMem)
            If Dat.Options.AlarmEnabled Then AlarmOnOff(True)
        End If

        If Dat.Options.AutoUpdate And Application.winStore = False Then 'Kontrola autospouštění při startu
            myAutostart.Create("STARTzjs", Chr(34) + appCesta & "\STARTzjs.exe" + Chr(34) + "-win", True)
        Else
            myAutostart.Create("STARTzjs", appCesta & "\STARTzjs.exe", False)
        End If

        Dat.HasChanges = False
    End Sub

    Private Sub wpfMain_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseMove
        If e.LeftButton = MouseButtonState.Pressed AndAlso Mouse.GetPosition(Me).Y > 30 Then '<> 
            myWindow.ReleaseCapture()
            myWindow.Drag(Me)
        End If
    End Sub

#End Region

#Region " ListBox "

#Region " Drag and Drop "

    Enum DDI
        BoxItem
        BoxColumn
    End Enum
    Private ts1, id1 As Integer
    Private dragItem As DDI
    Private dragPoint As Point
    Private ColumnIcon As New Uri("STARTzjs;component/Images/folder/dragfolder128.ico", UriKind.Relative)
    Private DragFalseRepeat, LeftTrueRightFalse, WindowDragActive, TouchDragActive As Boolean

    Private Sub PreviewDown(ByVal sender As Object, ByVal dragNewPoint As Point)
        If Dat.Options.Lock <> 0 Then Exit Sub
        LeftTrueRightFalse = True : TouchDragActive = True
        GetBoxItem(sender, dragNewPoint)
        dragPoint = dragNewPoint
    End Sub

    Public Sub lbxMain_PreviewMouseLeftButtonDown(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs)
        PreviewDown(sender, e.GetPosition(Me))
    End Sub

    Public Sub lbxMain_PreviewTouchDown(sender As Object, e As TouchEventArgs)
        If Math.Abs(ts1 - e.Timestamp) < 20 And e.TouchDevice.Id > id1 Then
            WindowDragActive = True
            Exit Sub
        Else
            WindowDragActive = False
            myWindow.ReleaseCapture()
        End If

        id1 = e.TouchDevice.Id
        ts1 = e.Timestamp

        PreviewDown(sender, e.GetTouchPoint(Me).Position)
    End Sub

    Private Sub PreviewMove(ByVal sender As Object, ByVal dragNewPoint As Point, ByVal Pressed As Boolean)
        If IsNothing(nowListBox) Or LeftTrueRightFalse = False Then Exit Sub
        Dim diff As Vector = dragPoint - dragNewPoint

        If Pressed Then
            If Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance Or Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance Then
                TouchDragActive = False
                DragFalseRepeat = False
                If IsNothing(nowListBoxItem) Then
                    If IsNothing(nowListBox) Then
                    Else
                        If dragPoint.Y < 32 Then
                            dragItem = DDI.BoxColumn
                            Dim dragData As New DataObject(dragItem.ToString, GetBoxColumn(nowListBox))
                            DragDrop.DoDragDrop(TryCast(sender, DependencyObject), dragData, DragDropEffects.Move)
                        End If
                    End If
                Else
                    dragItem = DDI.BoxItem
                    Dim dragData As New DataObject(dragItem.ToString, nowBoxItem)
                    DragDrop.DoDragDrop(TryCast(sender, DependencyObject), dragData, DragDropEffects.Move)
                End If
            End If
        End If
    End Sub

    Public Sub lbxMain_PreviewTouchMove(sender As Object, e As TouchEventArgs)
        If WindowDragActive Then
            myWindow.Drag(Me)
        Else
            PreviewMove(sender, e.GetTouchPoint(Me).Position, TouchDragActive)
        End If
    End Sub

    Public Sub lbxMain_PreviewMouseMove(sender As System.Object, e As System.Windows.Input.MouseEventArgs)
        PreviewMove(sender, e.GetPosition(Me), If(e.LeftButton = MouseButtonState.Pressed, True, False))
    End Sub

    Public Sub lbxMain_GiveFeedback(sender As System.Object, e As System.Windows.GiveFeedbackEventArgs)
        e.UseDefaultCursors = False

        If e.Effects = DragDropEffects.Move Then

            Select Case dragItem
                Case DDI.BoxItem
                    If nowBoxItem IsNot Nothing AndAlso nowBoxItem.Cursor IsNot Nothing Then
                        Mouse.SetCursor(nowBoxItem.Cursor)
                        e.Handled = True
                    End If

                Case DDI.BoxColumn
                    Mouse.SetCursor(myBitmap.ToCursor(ColumnIcon))
                    e.Handled = True

                Case Else

            End Select

        End If
    End Sub

    Public Sub lbxMain_DragEnter(sender As System.Object, e As System.Windows.DragEventArgs)
        If e.Data.GetDataPresent(DDI.BoxItem.ToString) Then
            e.Effects = DragDropEffects.Move
        ElseIf e.Data.GetDataPresent("FileDrop") Then
            e.Effects = DragDropEffects.Link
        Else
            e.Effects = DragDropEffects.None
        End If
    End Sub

    Public Sub lbxMain_Drop(sender As System.Object, e As System.Windows.DragEventArgs)
        If Dat.Options.Lock <> 0 Then Exit Sub
        'e.OriginalSource by ukázalo rovnou Button, pod kterým je ListBoxItem
        GetBoxItem(sender, e.GetPosition(Me))

        If e.Data.GetDataPresent("FileDrop") Then
            Dim fileNames() As String = CType(e.Data.GetData(DataFormats.FileDrop), String())
            BoxColumnList.AddItems(fileNames, GetBoxColumn(nowListBox).Theme)
        ElseIf e.Data.GetDataPresent(DataFormats.Text) Then
            BoxColumnList.AddItem(e.Data.GetData(DataFormats.Text).ToString, GetBoxColumn(nowListBox).Theme)
        End If

        If DragFalseRepeat Then Exit Sub

        If e.Data.GetDataPresent(DDI.BoxItem.ToString) Then
            DragFalseRepeat = True
            If IsNothing(nowBoxItem) Then
                If IsNothing(nowListBox) Then
                Else
                    BoxColumnList.InsertBoxItem(CType(e.Data.GetData(DDI.BoxItem.ToString), clsBoxItem), GetBoxColumn(nowListBox))
                End If
            Else
                BoxColumnList.InsertBoxItem(CType(e.Data.GetData(DDI.BoxItem.ToString), clsBoxItem), nowBoxItem)
            End If
        ElseIf e.Data.GetDataPresent(DDI.BoxColumn.ToString) Then
            DragFalseRepeat = True
            If IsNothing(nowListBox) Then
            Else
                Dim dragBoxColumn As clsBoxColumn = CType(e.Data.GetData(DDI.BoxColumn.ToString), clsBoxColumn)
                Dim dropBoxColumn As clsBoxColumn = GetBoxColumn(nowListBox)
                ChangePostionTo(dragBoxColumn.ListBox, nowListBox)
                BoxColumnList.InsertBoxColumn(dragBoxColumn, dropBoxColumn)
            End If
        End If
    End Sub

    Private Sub ChangePostionTo(dragElement As UIElement, dropElement As UIElement)
        Dim iDropIndex As Integer = panStack.Children.IndexOf(dropElement)
        Dim iDragIndex As Integer = panStack.Children.IndexOf(dragElement)
        panStack.Children.Remove(dragElement)
        panStack.Children.Insert(iDropIndex, dragElement)
    End Sub

#End Region

    Private nowListBox As ListBox
    Private nowListBoxItem As ListBoxItem
    Private nowBoxItem As clsBoxItem

    Public Sub lbxMain_MouseDoubleClick(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs)
        Me.WindowState = WindowState.Minimized
    End Sub

    Private Sub GetBoxItem(sender As System.Object, Optional dpiPosition As Point = Nothing)
        nowListBoxItem = Nothing : nowBoxItem = Nothing : nowBoxItem = Nothing
        Dim pixPosition As Point
        Dim lbx As ListBox = TryCast(sender, ListBox)
        If IsNothing(lbx) Then Exit Sub
        nowListBox = lbx
        If dpiPosition.X = 0 And dpiPosition.Y = 0 Then
            pixPosition = myWindow.GetMousePosition
        Else
            pixPosition = myWindow.PPItoPixel(New Point(dpiPosition.X + Me.Left, dpiPosition.Y + Me.Top), True)
        End If
        Dim elem As UIElement = TryCast(lbx.InputHitTest(lbx.PointFromScreen(pixPosition)), UIElement)
        If IsNothing(elem) Then Exit Sub
        While elem IsNot lbx
            If TypeOf elem Is ListBoxItem Then
                nowListBoxItem = DirectCast(elem, ListBoxItem)
                nowBoxItem = DirectCast(nowListBoxItem.Content, clsBoxItem)
                Exit Sub
            End If
            Try
                elem = DirectCast(VisualTreeHelper.GetParent(elem), UIElement)
            Catch
                Exit Sub
            End Try
        End While
    End Sub

    Private Function GetBoxColumn(ByVal lbx As ListBox) As clsBoxColumn
        For Each column In BoxColumnList
            If column.ListBox Is lbx Then
                Return column
            End If
        Next
        Return Nothing
    End Function

    Public Sub lbxMain_PreviewMouseLeftButtonUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
        GetBoxItem(sender, e.GetPosition(Me))
        If nowBoxItem IsNot Nothing Then
            Select Case nowBoxItem.Type
                Case BoxType.Start
                    StartItem(nowBoxItem.Path)
                Case BoxType.Link
                    Me.WindowState = WindowState.Minimized
                    If nowBoxItem.HideStart Then
                        myLink.StartHidden(nowBoxItem.Path)
                    Else
                        myLink.Start(Me, nowBoxItem.Path)
                    End If
                Case BoxType.File, BoxType.Folder
                    If If(nowBoxItem.Type = BoxType.Folder, myFolder.Exist(nowBoxItem.Path), myFile.Exist(nowBoxItem.Path)) Then
                        Me.WindowState = WindowState.Minimized
                        myFile.Launch(Me, nowBoxItem.Path, nowBoxItem.Admin)
                    Else
                        nowBoxItem.ExistCheck()
                    End If
                Case BoxType.UWP
                    Me.WindowState = WindowState.Minimized
                    myFile.Launch(Me, "shell:appsFolder\" & nowBoxItem.Path, nowBoxItem.Admin)
            End Select
        End If
    End Sub

    Public Sub lbxMain_PreviewMouseRightButtonUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
        LeftTrueRightFalse = False
        GetBoxItem(sender, e.GetPosition(Me))
        If nowBoxItem Is Nothing Then
            If nowListBox Is Nothing Then
                Exit Sub
            Else
                cMenu.Placement = Primitives.PlacementMode.RelativePoint
                cMenu.PlacementTarget = nowListBox
                cMenu.PlacementRectangle = New Rect(5, 35, 0, 0)
            End If
        Else
            cMenu.Placement = Primitives.PlacementMode.RelativePoint
            cMenu.PlacementTarget = nowListBoxItem
            If myWindow.GetMousePosition.Y + 50 > System.Windows.SystemParameters.WorkArea.Height Then
                cMenu.PlacementRectangle = New Rect(0, 10, 0, 0)
            Else
                cMenu.PlacementRectangle = New Rect(0, nowListBoxItem.ActualHeight - 10, 0, 0)
            End If
        End If
        cMenu.IsOpen = False
        PrepareContextMenu()
        cMenu.IsOpen = True
    End Sub


#End Region

#Region " ContextMenu "

#Region " Items "

    Private Sub smiHotkey_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiHotkey.Click
        Dim Form As New wpfKey
        Form.Owner = Me
        Form.Width = Me.Width / 2
        Form.Height = Me.Height / 3
        Form.ShowDialog()
        nowBoxItem.RemoveHotkey()
        If Form.Vysledek.Canceled = False Then
            nowBoxItem.RemoveHotkey()
            nowBoxItem.AddHotkey(Form.Vysledek.Modifiers, Form.Vysledek.Keys)
            Dim err As Integer = Runtime.InteropServices.Marshal.GetLastWin32Error()
            If err <> 0 Then
                Dim ex As Exception = New ComponentModel.Win32Exception(err)
                If err = 1409 Then
                    Dim wDialog As New wpfDialog(Me, ex.Message, Me.Title, wpfDialog.Ikona.chyba, "Zavřít")
                    wDialog.ShowDialog()
                    nowBoxItem.RemoveHotkey()
                End If
            End If
        End If
    End Sub

    Private Sub smiAddFile_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiAddFile.Click
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = If(Lge, "Vyberte programy", "Choose programs")
        dlg.Filter = If(Lge, "Programy", "Programs") & " (*.exe)|*.exe|" _
            & If(Lge, "Všechny soubory", "All files") & " (*.*)|*.*"
        dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        dlg.Multiselect = True
        If dlg.ShowDialog = False Then Exit Sub
        BoxColumnList.AddItems(dlg.FileNames, GetBoxColumn(nowListBox).Theme, BoxType.File)
    End Sub

    Private Sub smiAddFolder_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiAddFolder.Click
        Dim wDialogFolder As New wpfDialogFolder
        wDialogFolder.Owner = Me
        wDialogFolder.SystemFolders = True
        wDialogFolder.UserFolders = True
        If wDialogFolder.ShowDialog() Then
            BoxColumnList.AddItem(wDialogFolder.SelectFolder, GetBoxColumn(nowListBox).Theme, BoxType.Folder)
        End If
    End Sub

    Private Sub smiAddLink_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiAddLink.Click
        Dim wDialog As New wpfDialog(Me, FindResource("vloztelink").ToString, Me.Title, wpfDialog.Ikona.tuzka, "OK", "Storno", True, True, FindResource("apilink").ToString, True)
        If wDialog.ShowDialog() = False Then Exit Sub
        If wDialog.Input = "" Then Exit Sub
        BoxColumnList.AddItem(wDialog.Input, GetBoxColumn(nowListBox).Theme, BoxType.Link, wDialog.Zatrzeno)
    End Sub

    Private Sub txtItem_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs) Handles txtItem.TextChanged
        If nowBoxItem IsNot Nothing And bKontrolaTextu Then
            If txtItem.Text = "" Then Exit Sub
            txtItem.Background = Brushes.WhiteSmoke
            If myFile.isNameSafe(txtItem.Text) = False Then
                txtItem.Background = Brushes.LightPink
                Exit Sub
            End If
            If BoxColumnList.seznam.Exist(txtItem.Text, "") Then
                txtItem.Background = Brushes.LightPink
            Else
                nowBoxItem.Name = txtItem.Text
            End If
        End If
    End Sub

    Private Sub smiFolder_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiFolder.Click
        myFile.Launch(Me, myFile.Path(nowBoxItem.Path))
    End Sub

    Private Sub smiRemove_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiRemove.Click
        BoxColumnList.seznam.Remove(nowBoxItem)
        If BoxColumnList.RowsCountChanged Then BoxColumnList.Resize()
    End Sub

    Private Sub txtArguments_LostFocus(sender As Object, e As RoutedEventArgs) Handles txtArguments.LostFocus
        CheckArguments()
    End Sub

    Private Sub txtArguments_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs) Handles txtArguments.TextChanged
    End Sub

    Private Sub txtArguments_KeyUp(sender As Object, e As KeyEventArgs) Handles txtArguments.KeyUp
        If e.Key = Key.Enter Then CheckArguments()
    End Sub

    Private Sub CheckArguments()
        If nowBoxItem IsNot Nothing Then
            If bKontrolaTextu = False And nowBoxItem.HideStart Then Exit Sub 'první kontrola API při přípravě neproběhne
            If txtArguments.Text = "" Then Exit Sub
            txtArguments.Background = Brushes.WhiteSmoke

            Dim OK As Boolean = True
            Select Case nowBoxItem.Type
                Case BoxType.File
                    If myFile.isPathSafe(myFile.RemoveQuotationMarks(txtArguments.Text)) = False Then
                        OK = False
                    Else
                        If myFile.Exist(txtArguments.Text) = False Then OK = False
                    End If

                Case BoxType.Folder
                    If myFile.isPathSafe(myFile.RemoveQuotationMarks(txtArguments.Text)) = False Then
                        OK = False
                    Else
                        If myFolder.Exist(txtArguments.Text) = False Then OK = False
                    End If

                Case BoxType.Link
                    Dim Link As String = myLink.AbsoluteUri(txtArguments.Text)
                    If Link = "" Then
                        OK = False
                    Else
                        txtArguments.Text = Link
                    End If

                Case BoxType.Start
                    Exit Sub

            End Select

            If OK Then
                If bKontrolaTextu Then
                    nowBoxItem.Path = txtArguments.Text
                    nowBoxItem.ExistCheck()
                End If
            Else
                txtArguments.Background = Brushes.LightPink
                Exit Sub
            End If
        End If
    End Sub

    Private Sub ckbHidestart_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbHideStart.Checked
        nowBoxItem.HideStart = True
    End Sub

    Private Sub ckbHidestart_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbHideStart.Unchecked
        nowBoxItem.HideStart = False
    End Sub

    Private Sub ckbHidden_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbHidden.Checked
        nowBoxItem.Hidden = True
    End Sub

    Private Sub ckbHidden_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbHidden.Unchecked
        nowBoxItem.Hidden = False
    End Sub

    Private Sub ckbHighlighted_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbHighlighted.Checked
        nowBoxItem.Highlighted = True
    End Sub

    Private Sub ckbHighlighted_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbHighlighted.Unchecked
        nowBoxItem.Highlighted = False
    End Sub

    Private Sub ckbAdmin_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbAdmin.Checked
        nowBoxItem.Admin = True
    End Sub

    Private Sub ckbAdmin_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbAdmin.Unchecked
        nowBoxItem.Admin = False
    End Sub

    Private Sub smiEmpty_Clicked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiEmpty.Click
        BoxColumnList.BinEmpty()
    End Sub
#End Region

#Region " Columns "

    Private Sub txtColumn_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs) Handles txtColumn.TextChanged
        If nowListBox IsNot Nothing And bKontrolaTextu Then
            If txtColumn.Text = "" Then Exit Sub
            txtColumn.Background = Brushes.WhiteSmoke
            If myFile.isNameSafe(txtColumn.Text) = False Then
                txtColumn.Background = Brushes.LightPink
                Exit Sub
            End If
            If BoxColumnList.Exist(txtColumn.Text) Then
                txtColumn.Background = Brushes.LightPink
            Else
                GetBoxColumn(nowListBox).Theme = txtColumn.Text
            End If
        End If
    End Sub

    Private Sub smiAddColumn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiAddColumn.Click
        BoxColumnList.AddColumn()
    End Sub

    Private Sub smiRunAll_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiRunAll.Click
        For Each item In GetBoxColumn(nowListBox).CollectionViewSource.View
            Dim box As clsBoxItem = CType(item, clsBoxItem)
            myFile.Launch(Me, box.Path)
        Next
    End Sub

    Private Sub ckbSortItems_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbSortItems.Checked
        GetBoxColumn(nowListBox).Sorted = True
    End Sub

    Private Sub ckbSortItems_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbSortItems.Unchecked
        GetBoxColumn(nowListBox).Sorted = False
    End Sub

    Private Sub ckbColumnHidden_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbColumnHidden.Unchecked
        GetBoxColumn(nowListBox).Hidden = False
        UpdateColumnHidden()
    End Sub

    Private Sub ckbColumnHidden_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbColumnHidden.Checked
        GetBoxColumn(nowListBox).Hidden = True
        UpdateColumnHidden()
    End Sub

    Private Sub smiColumnRemove_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles smiColumnRemove.Click
        Dim nowBoxColumn As clsBoxColumn = GetBoxColumn(nowListBox)

        Dim colBox As New Collection(Of clsBoxItem)
        For Each box In BoxColumnList.seznam
            If box.Theme = nowBoxColumn.Theme Then
                If box.Path = "START_setting" Then Exit Sub
                colBox.Add(box)
            End If
        Next
        For Each box In colBox
            BoxColumnList.seznam.Remove(box)
        Next

        BoxColumnList.Remove(nowBoxColumn)
        BoxColumnList.Show(Dat.Options.ItemsHidden)
    End Sub

#End Region

#Region " Common "

    Private Sub smiRemoveNotExist_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiRemoveNotExist.Click
        CheckAllExist()
        Dim colBox As New Collection(Of clsBoxItem)
        For Each box In BoxColumnList.seznam
            If box.Exist = False Then colBox.Add(box)
        Next
        For Each box In colBox
            BoxColumnList.seznam.Remove(box)
        Next
        SwitchON(True)
    End Sub

    Private Sub smiHideAll_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiHideAll.Click
        Dat.Options.ItemsHidden = Not Dat.Options.ItemsHidden
        BoxColumnList.Show(Dat.Options.ItemsHidden)
    End Sub

    Private Sub UpdateColumnHidden()
        If BoxColumnList.HiddenAll Then
            lblHideAll.Foreground = Brushes.LightSlateGray
            smiHideAll.IsEnabled = False
        Else
            lblHideAll.Foreground = Brushes.Black
            smiHideAll.IsEnabled = True
        End If
    End Sub

    Private Sub CheckAllExist()
        SwitchON(False)
        For Each box In BoxColumnList.seznam
            If Not box.Type = BoxType.Start Then box.ExistCheck()
        Next
    End Sub

    Private Sub SwitchON(ByVal Off As Boolean)
        smiRemoveNotExist.IsEnabled = Off
        smiSearch.IsEnabled = Off
        If Off Then
            lblSearch.Foreground = Brushes.Black
            lblRemoveNotExist.Foreground = Brushes.Black
        Else
            lblSearch.Foreground = Brushes.LightSlateGray
            lblRemoveNotExist.Foreground = Brushes.LightSlateGray
        End If
    End Sub

    Private Sub txtLock_GotFocus(sender As Object, e As System.Windows.RoutedEventArgs) Handles txtLock.GotFocus
        txtLock.Text = If(Lge, "4 číslice", "4 digits")
    End Sub

    Private Sub txtLock_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles txtLock.KeyUp
        If IsNumeric(txtLock.Text) = False Then
            txtLock.Text = ""
        End If
    End Sub

    Private Sub txtLock_LostFocus(sender As Object, e As System.Windows.RoutedEventArgs) Handles txtLock.LostFocus
        txtLock.Text = If(Dat.Options.Lock = 0, If(Lge, "Zamknout", "Lock"), If(Lge, "Odemknout", "Unlock"))
    End Sub

    Private Sub txtLock_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs) Handles txtLock.TextChanged
        If IsNumeric(txtLock.Text) Then
            If Dat.Options.Lock = 0 Then
                If txtLock.Text.Trim.Length = 4 Then
                    Dat.Options.Lock = CInt(txtLock.Text)
                    PrepareContextMenu()
                End If
            Else
                If CInt(txtLock.Text) = Dat.Options.Lock Then
                    Dat.Options.Lock = 0
                    PrepareContextMenu()
                End If
            End If
        End If
    End Sub

#End Region


#Region " Autorun "

    Private Sub ckbAutorun_Checked(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles ckbAutorun.Checked
        If nowBoxItem.Path = "START_setting" Then
            myRegister.WriteAutoStartMyApp()
        Else
            nowBoxItem.Autostart = True
            myAutostart.Create(nowBoxItem.Name, nowBoxItem.Path, True)
        End If
    End Sub

    Private Sub ckbAutorun_Unchecked(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles ckbAutorun.Unchecked
        If nowBoxItem.Path = "START_setting" Then
            myRegister.DeleteAutoStartMyApp()
        Else
            nowBoxItem.Autostart = False
            myAutostart.Create(nowBoxItem.Name, nowBoxItem.Path, False)
        End If
    End Sub

#End Region

#Region " Uninstall "

    Private Sub smiUninstall_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles smiUninstall.Click
        Dim bAdmin As Boolean = If(nowBoxItem.Uninstall.Contains("Program Files"), True, False)
        myFile.Launch(Me, nowBoxItem.Uninstall, bAdmin)
    End Sub

#End Region

#Region " IconMenuItem "

    Private MenuItems As New Collection(Of MenuItem)

    Private Function CreateMenuItem(ByVal imgIcon As ImageSource, ByVal Text As String, ByVal Index As Integer, Optional ByVal Checked As Boolean = False) As MenuItem
        Dim smi As New MenuItem
        Dim img As New Image
        Dim can As New Canvas
        Dim lbl As New TextBlock
        Dim pan As New StackPanel
        pan.Orientation = Orientation.Horizontal
        lbl.Text = Text
        lbl.Style = CType(Me.FindResource("MenuTextBlock"), Style)
        img.Source = imgIcon
        img.Style = CType(Me.FindResource("MenuImage"), Style)
        If Checked Then
            Dim img2 As New Image
            img2.Style = CType(Me.FindResource("MenuImage"), Style)
            img2.Source = CType(Me.FindResource("Zatrzeno"), ImageSource)
            can.Children.Add(img)
            can.Children.Add(img2)
            can.Width = 32
            pan.Children.Add(can)
        Else
            pan.Children.Add(img)
        End If
        pan.Children.Add(lbl)
        smi.Header = pan
        smi.Tag = Index
        AddHandler smi.Click, AddressOf smiIconSource_Click
        MenuItems.Add(smi)
        Return smi
    End Function

    Private Sub MenuItemsClear()
        For Each MenuItem In MenuItems
            RemoveHandler MenuItem.Click, AddressOf smiIconSource_Click
            smiIconChange.Items.Remove(MenuItem)
        Next
        MenuItems.Clear()
    End Sub

    Private Sub smiIconSource_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim smi As MenuItem = CType(sender, MenuItem)
        nowBoxItem.IconIndex = CInt(smi.Tag)
        nowBoxItem.ExistCheck()
    End Sub

    Private Sub smiChangeSource_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles smiChangeSource.Click
        Dim Udalost As String = If(Lge, "Přidat program", "Add program")
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = If(Lge, "Vyberte soubor s ikonou", "Find your icon")
        dlg.Filter = If(Lge, "Ikony, aplikace a knihovny", "Icons, applications and libraries") & " (*.ico;*.exe;*.dll)|*.ico;*.exe;*.dll|" _
                & If(Lge, "Všechny soubory", "All files") & " (*.*)|*.*"
        If Not nowBoxItem.Type = BoxType.Link Then
            dlg.InitialDirectory = myFolder.Path(If(nowBoxItem.Icon = "", nowBoxItem.Path, nowBoxItem.Icon))
        End If
        dlg.CheckFileExists = True
        dlg.CheckPathExists = True
        If dlg.ShowDialog = False Then Exit Sub
        If dlg.FileName = nowBoxItem.Path Then
            nowBoxItem.Icon = ""
        Else
            nowBoxItem.Icon = dlg.FileName
        End If
        nowBoxItem.ExistCheck()
    End Sub

    Private Sub smiSaveIcon_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles smiSaveIcon.Click
        Dim dlg As New Microsoft.Win32.SaveFileDialog()
        dlg.Filter = "Icons (*.ico)|*.ico"
        dlg.FileName = nowBoxItem.Name
        dlg.Title = If(Lge, "Vyberte umístění pro uložení ikony", "Select location to save icon")
        dlg.CheckPathExists = True
        If dlg.ShowDialog = True Then
            Dim sNewFile As String = dlg.FileName.Substring(0, dlg.FileName.Length - 4)

            Dim myExtract As New clsExtractIcon(If(nowBoxItem.Icon = "", nowBoxItem.Path, nowBoxItem.Icon))
            myExtract.ChangeIndexIconInFile(nowBoxItem.IconIndex)
            myFile.Delete(sNewFile & "_48.ico", False)
            Dim fs As New IO.FileStream(sNewFile & "_48.ico", IO.FileMode.CreateNew)
            myExtract.GetIcon(48, False).Save(fs)
            fs.Close() : fs.Dispose()
            myFile.Delete(sNewFile & "_Max.ico", False)
            fs = New IO.FileStream(sNewFile & "_Max.ico", IO.FileMode.CreateNew)
            myExtract.GetIcon.Save(fs)
            fs.Close() : fs.Dispose()
            myFile.Launch(Me, myFile.Path(dlg.FileName))
        End If
    End Sub

#End Region

#Region " OpenCloseMenu "

    Private bKontrolaTextu As Boolean

    Private Sub cMenu_ContextMenuClosing(ByVal sender As System.Object, ByVal e As System.Windows.Controls.ContextMenuEventArgs) Handles cMenu.ContextMenuClosing
        If Dat.Options.ItemsHidden Then BoxColumnList.Show(True)
    End Sub

    Private Sub PrepareContextMenu()
        bKontrolaTextu = False
        smiItem.IsEnabled = If(Dat.Options.Lock = 0, True, False)
        smiAddFile.IsEnabled = If(Dat.Options.Lock = 0, True, False)
        smiAddFolder.IsEnabled = If(Dat.Options.Lock = 0, True, False)
        smiAddLink.IsEnabled = If(Dat.Options.Lock = 0, True, False)
        RemoveHandler ckbAutorun.Checked, AddressOf ckbAutorun_Checked
        RemoveHandler ckbAutorun.Unchecked, AddressOf ckbAutorun_Unchecked
        'Programy
        If Not nowListBox.Items.Count = 0 Then
            If nowBoxItem Is Nothing Then
                smiItem.Visibility = Visibility.Collapsed
            Else
                smiItem.Visibility = Visibility.Visible
                smiEmpty.Visibility = If(nowBoxItem.Path = "START_bin", Visibility.Visible, Visibility.Collapsed)
                smiEmpty.IsEnabled = If(BoxColumnList.BinIsEmpty, False, True)
                lblEmpty.Foreground = If(BoxColumnList.BinIsEmpty, Brushes.LightSlateGray, Brushes.Black)
                smiRemove.Visibility = Visibility.Visible
                txtItem.Text = nowBoxItem.Name
                txtArguments.Text = nowBoxItem.Path
                CheckArguments()
                smiFolder.Visibility = Visibility.Collapsed
                smiArguments.Visibility = Visibility.Collapsed
                smiIconChange.Visibility = Visibility.Collapsed
                smiUninstall.Visibility = If(nowBoxItem.Uninstall = "", Visibility.Collapsed, Visibility.Visible)
                smiAutorun.Visibility = Visibility.Collapsed
                smiAdmin.Visibility = Visibility.Collapsed
                smiHideStart.Visibility = Visibility.Collapsed
                Select Case nowBoxItem.Type
                    Case BoxType.File
                        smiAdmin.Visibility = Visibility.Visible
                        smiAutorun.Visibility = Visibility.Visible
                        If myFolder.Exist(myFolder.Path(nowBoxItem.Path)) Then smiFolder.Visibility = Visibility.Visible
                    Case BoxType.Link
                        smiHideStart.Visibility = Visibility.Visible
                        ckbHideStart.IsChecked = nowBoxItem.HideStart
                End Select
                imgItem.Source = nowBoxItem.ImgSource
                imgIconChange.Source = nowBoxItem.ImgSource
                imgAutorun.Source = mySystem.Current.Image
                ckbHidden.IsChecked = nowBoxItem.Hidden
                ckbHighlighted.IsChecked = nowBoxItem.Highlighted
                ckbAdmin.IsChecked = nowBoxItem.Admin
                Select Case nowBoxItem.Path
                    Case "START_lock", "START_standby", "START_logoff", "START_restart", "START_turnoff"
                        smiHotkey.Visibility = Visibility.Collapsed
                    Case Else
                        smiHotkey.Visibility = Visibility.Visible
                        If nowBoxItem.HKey.Enabled = False Then
                            lblHotkey.Text = "HotKey"
                        Else
                            lblHotkey.Text = nowBoxItem.HKey.Modifiers.ToString + " + " + nowBoxItem.HKey.Key.ToString
                        End If
                End Select
                If nowBoxItem.Type = BoxType.Start Then
                    Select Case nowBoxItem.Path
                        Case "START_setting"
                            smiRemove.Visibility = Visibility.Collapsed
                            smiAutorun.Visibility = Visibility.Visible
                            ckbAutorun.IsChecked = nowBoxItem.isAutostart(appCesta & "\STARTzjs.exe")
                        Case "START_alarm", "START_calc"
                            smiRemove.Visibility = Visibility.Collapsed
                    End Select
                Else
                    nowBoxItem.ExistCheck()
                    smiArguments.Visibility = Visibility.Visible
                    smiIconChange.Visibility = Visibility.Visible
                    If nowBoxItem.Exist Then
                        ckbAutorun.IsChecked = nowBoxItem.isAutostart
                        Dim sSimplePath As String = myFile.RemoveQuotationMarks(If(nowBoxItem.Icon = "", nowBoxItem.Path, nowBoxItem.Icon))
                        If IsNothing(smiIconChange.Tag) Then
                            MenuItemsClear()
                            Dim myExtract As New clsExtractIcon(sSimplePath)
                            If myExtract.Chyba = False Then
                                Dim iMax As Integer = If(myExtract.CountIconsInFile > 9, 9, myExtract.CountIconsInFile)
                                For i As Integer = 0 To iMax
                                    If myExtract.ChangeIndexIconInFile(i) Then
                                        Dim smi As MenuItem = CreateMenuItem(myExtract.GetImageSource(32, False), "Icon " + CStr(i + 1), i, If(nowBoxItem.IconIndex = i, True, False))
                                        If nowBoxItem.IconIndex = i Then smi.IsChecked = True
                                        smiIconChange.Items.Add(smi)
                                    End If
                                Next
                            End If
                            myExtract.Dispose()
                        End If
                    End If
                End If
            End If
        Else
            smiItem.Visibility = Visibility.Collapsed
        End If
        'Sloupec
        Dim nowBoxColumn As clsBoxColumn = GetBoxColumn(nowListBox)
        smiColumn.IsEnabled = If(Dat.Options.Lock = 0, True, False)
        smiAddColumn.IsEnabled = If(Dat.Options.Lock = 0, True, False)
        txtColumn.Text = nowBoxColumn.Theme
        smiColumnRemove.Visibility = If(BoxColumnList.Sloupcu = 1, Visibility.Collapsed, Visibility.Visible)
        smiRunAll.Visibility = If(BoxColumnList.Count > 1, Visibility.Visible, Visibility.Collapsed)
        ckbSortItems.IsChecked = nowBoxColumn.Sorted
        ckbColumnHidden.IsChecked = nowBoxColumn.Hidden
        'Společné
        txtLock.Text = If(Dat.Options.Lock = 0, If(Lge, "Zamknout", "Lock"), If(Lge, "Odemknout", "Unlock"))
        imgLock.Source = CType(FindResource(If(Dat.Options.Lock = 0, "Unlock", "Lock")), ImageSource)
        smiHideAll.Visibility = If(Dat.Options.Lock = 0, Visibility.Visible, Visibility.Collapsed)
        lblHideAll.Text = If(Dat.Options.ItemsHidden, If(Lge, "Zobrazit skryté", "Show hidden"), If(Lge, "Schovat skryté", "Hide marked hidden"))
        UpdateColumnHidden()
        smiSearch.Visibility = Visibility.Collapsed
        smiRemoveNotExist.Visibility = Visibility.Collapsed
        For Each box In BoxColumnList.seznam
            If box.Exist = False And Not box.Type = BoxType.UWP Then
                smiSearch.Visibility = Visibility.Visible
                If Dat.Options.Lock = 0 Then smiRemoveNotExist.Visibility = Visibility.Visible
                Exit For
            End If
        Next
        AddHandler ckbAutorun.Checked, AddressOf ckbAutorun_Checked
        AddHandler ckbAutorun.Unchecked, AddressOf ckbAutorun_Unchecked
        bKontrolaTextu = True
    End Sub

#End Region

#Region " Search "

    Private Sub smiSearch_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles smiSearch.Click
        If smiSearch.IsEnabled Then
            CheckAllExist()
            Dim OK As Boolean = False
            For Each box In BoxColumnList.seznam
                If box.Exist = False Then OK = True : Exit For
            Next
            If OK Then
                bgwSearch = New System.ComponentModel.BackgroundWorker
                AddHandler bgwSearch.DoWork, AddressOf bgwSearch_DoWork
                AddHandler bgwSearch.RunWorkerCompleted, AddressOf bgwSearch_RunWorkerCompleted
                bgwSearch.RunWorkerAsync()
            Else
                SwitchON(True)
            End If
        End If
    End Sub

    Private notFound As Integer
    Private bgwSearch As System.ComponentModel.BackgroundWorker

    Private Sub bgwSearch_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        SearchThisFolder(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
        SearchThisFolder(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
        If mySystem.Is64bit Then SearchThisFolder(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
        'SearchThisFolder(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3))
    End Sub

    Private Sub SearchThisFolder(ByVal sFolder As String)
        For Each box In BoxColumnList.seznam
            If box.Exist = False And box.Type = BoxType.File Then
                For Each oneName As String In myFolder.Files(sFolder, "*.exe", True)
                    If myFile.Name(box.Path) = myFile.Name(oneName) Then
                        box.Path = oneName
                        Exit For
                    End If
                Next
            End If
        Next
    End Sub

    Private Sub bgwSearch_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs)
        bgwSearch.Dispose()
        For Each box In BoxColumnList.seznam
            If box.Exist = False Then box.ExistCheck()
        Next
        SwitchON(True)
    End Sub
#End Region

#End Region

#Region " Hotkey "

    <Serializable()>
    Public Class NewHotKey
        Inherits HotKey

        Private wMain As wpfMain = DirectCast(Application.Current.MainWindow, wpfMain)
        Private m_name As String

#Region " Compulsory of Serializable "

        Public Property Name() As String
            Get
                Return m_name
            End Get
            Set(ByVal value As String)
                If value <> m_name Then
                    m_name = value
                    OnPropertyChanged(m_name)
                End If
            End Set
        End Property

        Protected Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
            Name = info.GetString("Name")
        End Sub

        Public Overrides Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.GetObjectData(info, context)

            info.AddValue("Name", Name)
        End Sub

#End Region

        Public Sub New(ByVal Nazev As String, ByVal key As Key, ByVal modifiers As ModifierKeys, ByVal enabled As Boolean)
            MyBase.New(key, modifiers, enabled)
            Name = Nazev
        End Sub

        Protected Overrides Sub OnHotKeyPress()
            For Each box In BoxColumnList.seznam
                If box.HKey Is Me Then
                    Select Case box.Type
                        Case BoxType.Start
                            If box.Path = "START_setting" Then
                                wMain.WindowState = WindowState.Normal
                                wMain.Activate()
                                wMain.ShowWindowInCenterOfMousePoint()
                            Else
                                StartItem(box.Path)
                            End If
                        Case BoxType.Link
                            If box.HideStart Then
                                myLink.StartHidden(box.Path)
                            Else
                                myLink.Start(Application.Current.MainWindow, box.Path)
                            End If
                        Case BoxType.File, BoxType.Folder
                            myFile.Launch(Application.Current.MainWindow, box.Path, box.Admin)
                    End Select
                End If
            Next
            MyBase.OnHotKeyPress()
        End Sub

    End Class
#End Region

#Region " Timers "

#Region " Start "

    Private ZJShareMem As New clsSharedMemory
    Private WithEvents timZJS As DispatcherTimer

    Private Sub timZJS_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timZJS.Tick
        Try
            If ZJShareMem.DataExists Then
                If ZJShareMem.Peek() = "ZJS:" & Application.ExeName & ":EXIT" Then Me.Close()
            End If
        Catch
            timZJS.Stop()
        End Try

        If Not Dat.Options.MenuColor = 2 Then BoxColumnList.UpdateLook()

        Dim iNewVersion As Integer = 580
        If Application.VersionNo = iNewVersion And Not Dat.Options.NewVersion = iNewVersion And Application.winStore = False Then
            Dat.Options.NewVersion = iNewVersion
            Dim wDialog = New wpfDialog(Me, If(Lge, "Vítejte v nové verzi STARTzjs ", "Welcome to the new version of STARTzjs ") + NR + NR +
                If(Lge, "Získání aplikace bylo přesunuto do Windows Storu.", "Getting the app has been moved to the Windows Store.") + NR +
                If(Lge, "Nové verze budou dostupné pouze přes Store.", "New versions will only be available through the Store.") + NR +
                If(Lge, "To eliminuje problémy kvůli nepodepsanému kódu.", "This eliminates problems due to unsigned code.") + NR + NR +
                If(Lge, "Kdykoliv můžete začít používat aplikaci ze Storu,", "You can start using the app from the Store at any time,") + NR +
                If(Lge, "Aplikaci pak hledejte ve START nabídce Windows.", "Then look for the application in the Windows START menu.") + NR +
                If(Lge, "Starou verzi odinstalujte, aby se nespouštěli obě.", "Uninstall the old version so that it doesn't run both."), Me.Title, wpfDialog.Ikona.ok, If(Lge, "Store", "Store"))
            If wDialog.ShowDialog() = True Then
                myLink.Start(Me, "ms-windows-store://pdp/?productid=9NX78DH1FM1N")
            End If
        End If
    End Sub

#End Region

#Region " Budík "

    Public WithEvents timAlarm As DispatcherTimer
    Private CountTick As Integer

    Private Function T2S(ByVal Cas As String) As String
        T2S = Cas
        If Len(Cas) < 2 Then T2S = "0" & T2S
    End Function

    Private Function T2S(ByVal Cas As Integer) As String
        Dim sCas As String = CStr(Cas)
        T2S = sCas
        If Len(sCas) < 2 Then T2S = "0" & T2S
    End Function

    Public Function NewTimeForInterval(Interval As String) As String
        Dim Hours As Double = CDbl(Interval.Substring(0, 2))
        Dim Minutes As Double = CDbl(Interval.Substring(3, 2))
        Dim newTime = DateAdd(DateInterval.Hour, Hours, Now)
        newTime = DateAdd(DateInterval.Minute, Minutes, newTime)
        Return T2S(newTime.Hour) & ":" & T2S(newTime.Minute)
    End Function

    Public Sub AlarmToggle()
        AlarmOnOff(Not timAlarm.IsEnabled)
    End Sub

    Public Sub AlarmOnOff(switch As Boolean, Optional AutoOnNoChange As Boolean = False)
        CountTick = 0
        If AutoOnNoChange = False Then Dat.Options.AlarmEnabled = switch
        BoxColumnList.AlarmBlinking(switch)
        timAlarm.IsEnabled = switch
        WatchFolder.IsEnabled = switch
        timFile.IsEnabled = switch
        mqttActivation(switch)
    End Sub

    Private Sub timAlarm_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timAlarm.Tick
        'Zviditelnění aktivního budíku
        CountTick += 1
        If (CountTick = 4 And BoxColumnList.IsAlarmBlinkOn = False) Or (CountTick = 1 And BoxColumnList.IsAlarmBlinkOn) Then 'Alarm Blinking
            CountTick = 0
            BoxColumnList.AlarmBlinking(True)
        End If

        'Kontrola stavu
        Dim CasKontroly As String = T2S(TimeOfDay.Hour) & ":" & T2S(TimeOfDay.Minute) & ":" & T2S(TimeOfDay.Second)
        For Each Alarm In Dat.AlarmMem
            Dim Match As Boolean = False
            Select Case Alarm.Watch
                Case "time"
                    If CasKontroly = Alarm.When1 & ":00" Then Match = True
                    If Alarm.Opakovat And Alarm.When2 = 1 Then
                        Alarm.When1 = wMain.NewTimeForInterval(Alarm.When4)
                    End If

                Case "interval"


                Case "dir"
                    If Alarm.Execute Then Match = True

                Case "window"
                    Select Case Alarm.When3
                        Case 1
                            If IsNothing(clsSystem.GetProcess(0, Alarm.When1)) = False Then Match = True

                        Case 2
                            If IsNothing(clsSystem.GetProcess(0, Alarm.When1)) = True Then Match = True

                        Case 3
                            If IsNothing(clsSystem.GetProcess(Alarm.When2)) = True Then Match = True

                    End Select
                Case "file"
                    If Alarm.Execute Then Match = True

                Case "mqtt"
                    If Alarm.Execute Then Match = True

            End Select

            If Match And Alarm.Aktivni Then
                Alarm.Execute = False
                If Not Alarm.Opakovat Then Alarm.Aktivni = False
                Dim msg As String = NR & If(Alarm.Message = "", "", If(Alarm.When1 = "dir", "Složka: ", "") & Alarm.Message & If(Alarm.Watch = "mqtt" Or Alarm.Watch = "file", " : " & Alarm.When1, ""))

                Select Case Alarm.Task
                    Case "message"
                        If Not Alarm.Data3 = "" Then
                            Try
                                Dim MP As New MediaPlayer
                                MP.Open(New Uri(Alarm.Data3, UriKind.Absolute))
                                MP.Play()
                            Catch
                            End Try
                        End If
                        If Not Alarm.Data1 = "" Then
                            MessageBox.Show(If(Lge, "Právě je ", "Time was ") & TimeOfDay.ToShortTimeString & NR & If(Lge, "Zpráva:  ", "Message:  ") & Alarm.Data1 & msg, Me.Title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly)
                        End If

                    Case "shutdown"
                        Dat.Save(False)
                        AlarmOnOff(False, True)
                        Select Case Alarm.Data2
                            Case 0
                                mySystem.Lock()
                            Case 1
                                mySystem.StandBy()
                            Case 2
                                mySystem.LogOff()
                            Case 3
                                mySystem.PowerOff()
                            Case 4
                                mySystem.Restart()
                        End Select

                    Case "run"
                        If Alarm.Data1 = "START_alarm" Then
                            AlarmOnOff(True)
                        Else
                            If myFile.Exist(Alarm.Data1) Then
                                myFile.Launch(Me, Alarm.Data1)
                            Else
                                Dim Found As Boolean = False
                                For Each box In BoxColumnList.seznam
                                    If box.Name.ToLower = Alarm.Data1.ToLower Then
                                        Found = True
                                        Select Case box.Type
                                            Case BoxType.Start
                                                StartItem(box.Path)
                                            Case BoxType.Link
                                                If box.HideStart Then
                                                    myLink.StartHidden(box.Path)
                                                Else
                                                    myLink.Start(Application.Current.MainWindow, box.Path)
                                                End If
                                            Case BoxType.File, BoxType.Folder
                                                myFile.Launch(Application.Current.MainWindow, box.Path)
                                        End Select
                                        Exit For
                                    End If
                                Next
                                If Found = False Then myFile.Launch(Me, Alarm.Data1)
                            End If
                        End If

                    Case "close0"
                        Dim thisProcess As Process = clsSystem.GetProcess(Alarm.Data2)
                        If IsNothing(thisProcess) = False Then thisProcess.CloseMainWindow()
                    Case "close1"
                        Dim theseProcesses() As Process = clsSystem.GetProcess(Alarm.Data1)
                        If IsNothing(theseProcesses) = False Then
                            For Each oneProcess In theseProcesses
                                oneProcess.CloseMainWindow()
                            Next
                        End If
                    Case "close2"
                        Dim thisProcess As Process = clsSystem.GetProcess(0, Alarm.Data1)
                        If IsNothing(thisProcess) = False Then thisProcess.CloseMainWindow()

                    Case "email"
                        If Dat.Options.Email.Contains("@") Then
                            Dim Email As New clsEmail(Dat.Options.Email, myString.Decrypt(Dat.Options.EmailPass, "myWinColor.LightBackground"))
                            Email.Send(Email.CreateMail(Alarm.Data1, Alarm.Data3, Alarm.Message))
                        End If

                    Case "alarm"
                        Dim doAlarm = Dat.AlarmMem.FirstOrDefault(Function(x) x.ID = Alarm.Data2)
                        If doAlarm IsNot Nothing Then doAlarm.Aktivni = CBool(Alarm.Data3)

                    Case "copy"
                        myFile.Copy(Alarm.Data1, Alarm.Data3, CBool(Alarm.Data2))

                    Case "display"
                        Select Case Alarm.Data2
                            Case 0
                                mySystem.SetDisplayMode(clsSystem.DisplayMode.Internal)
                            Case 1
                                mySystem.SetDisplayMode(clsSystem.DisplayMode.Duplicate)
                            Case 2
                                mySystem.SetDisplayMode(clsSystem.DisplayMode.Extend)
                            Case 3
                                mySystem.SetDisplayMode(clsSystem.DisplayMode.External)

                        End Select

                    Case "mqtt"
                        If Alarm.Data2 = 2 Then
                            mqttReconnect()
                        Else
                            mqtt.Publish(Alarm.Data1, Alarm.Data3).GetAwaiter()
                        End If

                    Case "keys"
                        My.Computer.Keyboard.SendKeys(If(Alarm.When4 = "", Alarm.Message, Alarm.Data1), True)

                    Case "mute"
                        mySystem.MuteSound(Me)

                End Select
            End If
        Next
        If Dat.AlarmMem.Where(Function(x) x.Aktivni).Count = 0 Then AlarmOnOff(False)
    End Sub
#End Region

#Region " File "

    Private WithEvents timFile As DispatcherTimer
    Public WithEvents bgwFile As New ComponentModel.BackgroundWorker

    Private Sub timFile_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timFile.Tick
        If Not bgwFile.IsBusy Then bgwFile.RunWorkerAsync()
    End Sub

    Private Sub bgwFileThreadWorking(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles bgwFile.DoWork
        Dim Files = Dat.AlarmMem.Where(Function(a) a.Watch = "file" And a.Aktivni).Select(Function(b) b.When4).Distinct
        For Each File In Files
            Dim Alarm = Dat.AlarmMem.FirstOrDefault(Function(a) a.Watch = "file" And a.Aktivni And a.When4 = File)
            If Alarm IsNot Nothing Then
                Dim stream As IO.MemoryStream
                stream = myFile.ToStream(Alarm.When4)

                If stream IsNot Nothing Then
                    Dim Value As String = myString.FromStream(stream, 25) '25 je max.délka, udržuj i v txtValue.MaxLength
                    For Each AlarmC In Dat.AlarmMem.Where(Function(a) a.Watch = "file" And a.When4 = Alarm.When4 And a.Aktivni)
                        Dim Match As Boolean
                        Select Case AlarmC.When2 '0> 1< 2=
                            Case 0
                                Match = If(IsNumeric(Value) And IsNumeric(AlarmC.When1) AndAlso CDbl(Value) > CDbl(AlarmC.When1), True, False)
                            Case 1
                                Match = If(IsNumeric(Value) And IsNumeric(AlarmC.When1) AndAlso CDbl(Value) < CDbl(AlarmC.When1), True, False)
                            Case 2
                                Match = If(Value = AlarmC.When1, True, False)
                        End Select
                        If Match Then
                            AlarmC.Message = Value
                            AlarmC.Execute = True
                        End If
                    Next
                End If
            End If
        Next
    End Sub

#End Region

#Region " MQTT "

    Private WithEvents mqtt As clsMQTT

    Private Sub Received(Topic As String, Payload As String) Handles mqtt.Recieved
        For Each Alarm In Dat.AlarmMem.Where(Function(a) a.Watch = "mqtt" And a.Aktivni And a.When1 = Topic)
            Dim Match As Boolean
            Select Case Alarm.When2 '0> 1< 2=
                Case 0
                    Match = If(IsNumeric(Payload) And IsNumeric(Alarm.When4) AndAlso CDbl(Payload) > CDbl(Alarm.When4), True, False)
                Case 1
                    Match = If(IsNumeric(Payload) And IsNumeric(Alarm.When4) AndAlso CDbl(Payload) < CDbl(Alarm.When4), True, False)
                Case 2
                    Match = If(Payload = Alarm.When4, True, False)
            End Select
            If Alarm.When4 = "" Then Match = True
            If Match Then
                Alarm.Message = Payload
                Alarm.Execute = True
            End If
        Next
    End Sub

    Private Sub Err(Message As String, Task As clsMQTT.TaskErr) Handles mqtt.Err
        Dispatcher.Invoke(Sub() ShowMessage(Message, Task))
    End Sub

    Private Sub ShowMessage(Message As String, Task As clsMQTT.TaskErr)
        AlarmOnOff(False, True)
        If timResume.IsEnabled = False Then timResume.IsEnabled = True
        Exit Sub

        If Task = clsMQTT.TaskErr.Disconnect Then
            mqtt = New clsMQTT
        Else
            If timResume.IsEnabled = False Then
                mqttActivation(False) 'kvůli vícerochybám raději přerušit spojení
                Dim Wnd As Window = If(Application.SettingWindow Is Nothing, Me, CType(Application.SettingWindow, Window))
                Dim wDialog As New wpfDialog(Wnd, Message, "MQTT Client", wpfDialog.Ikona.chyba, "OK")
                wDialog.ShowDialog()
            End If
        End If
    End Sub

    Private Sub mqttActivation(Activate As Boolean)
        If Activate Then
            If Dat.AlarmMem.Where(Function(x) x.Watch = "mqtt").Count > 0 Then
                mqtt = New clsMQTT
                mqtt.Create(Dat.Options.BrokerIP, Dat.Options.BrokerPort, Dat.Options.BrokerUser, myString.Decrypt(Dat.Options.BrokerPass, Application.Pass))
                mqtt.Connect.Wait(1000)
                If mqtt.IsConnected Then
                    For Each Topic In Dat.AlarmMem.Where(Function(x) x.Watch = "mqtt").Select(Function(y) y.When1).Distinct
                        mqtt.Subscribe(Topic).Wait(1000)
                    Next
                    AlarmBlinkingOnOff(Activate)
                Else
                    mqtt = New clsMQTT
                    AlarmBlinkingOnOff(False)
                    If Application.oneProcess And Today > Dat.Options.LastRestart.Date Then
                        HotKeyDispose()
                        Dat.Options.LastRestart = Today
                        Dat.Save(False)
                        Process.Start(Application.ResourceAssembly.Location)
                        Application.Current.Shutdown()
                    End If
                End If
            End If
        Else
            If mqtt IsNot Nothing AndAlso mqtt.IsConnected Then mqtt.Disconnect.Wait()
            mqtt = New clsMQTT
            AlarmBlinkingOnOff(Activate)
        End If
    End Sub

    Private Sub AlarmBlinkingOnOff(Activate As Boolean)
        If Dat.AlarmMem.All(Function(x) x.Watch = "mqtt") Then
            timAlarm.IsEnabled = Activate
            BoxColumnList.AlarmBlinking(Activate)
        End If
    End Sub

    Public Sub mqttSubscribeAdd(Topic As String)
        If timAlarm.IsEnabled AndAlso mqtt.IsConnected Then
            Dim Alarm = Dat.AlarmMem.FirstOrDefault(Function(a) a.Watch = "mqtt" And a.When1 = Topic)
            If Alarm Is Nothing Then mqtt.Subscribe(Topic).Wait(1000)
        End If
    End Sub

    Public Sub mqttSubscribeRemove(Topics As String())
        If timAlarm.IsEnabled AndAlso mqtt.IsConnected Then
            mqtt.Unsubscribe(Topics).Wait(1000)
        End If
    End Sub

    Public Sub mqttReconnect()
        mqtt.Reconnect.Wait(1000)
        If timAlarm.IsEnabled AndAlso mqtt.IsConnected Then
            Dim Topics As String() = Dat.AlarmMem.Where(Function(a) a.Watch = "mqtt").Select(Function(b) b.When1).Distinct.ToArray
            If Topics.Length > 0 Then mqtt.Unsubscribe(Topics).Wait(1000)
            For Each Topic In Topics
                mqttSubscribeAdd(Topic)
            Next
        End If
    End Sub

#End Region

#Region " Power mode Suspend and Resume "
    Private WithEvents timResume As DispatcherTimer
    Private Sub timResume_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timResume.Tick
        timResume.IsEnabled = False
        AlarmOnOff(True)
        Dat.SaveOn()
    End Sub
    Private Sub SystemEvents_PowerModeChanged(ByVal sernder As Object, ByVal e As Microsoft.Win32.PowerModeChangedEventArgs)
        Select Case e.Mode
            Case PowerModes.Suspend
                Dat.SaveOff()
                AlarmOnOff(False, True)
            Case PowerModes.Resume
                timResume.IsEnabled = True 'wait 5 seconds
        End Select
    End Sub

#End Region

#End Region

#Region " StartItems "

    Private Shared Sub OpenSetting(ByVal DefIndex As Integer, Optional ByVal Message As String = "")
        If IsNothing(Application.SettingWindow) = False Then Exit Sub

        Dim wnd As New wpfSetting
        wnd.Owner = Application.Current.MainWindow
        wnd.IndexPage = DefIndex
        wnd.Message = Message
        wnd.ShowDialog()
    End Sub

    Private Shared Sub StartItem(ByVal StartName As String)
        Dim wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
        Dim sMessage As String = If(mySystem.LgeCzech, "Tato funkce není ve vašem systému podporována.", "This feature is not supported on your system.")

        Select Case StartName
            Case "START_setting"
                OpenSetting(3)
            Case "START_alarm"
                OpenSetting(6)
            Case "START_calc"
                Dim wndCalc As Window = CalcWindow()
                If wndCalc Is Nothing Then
                    Dim wnd As New wpfCalc
                    wnd.Show()
                Else
                    wndCalc.WindowState = WindowState.Normal
                    wndCalc.Activate()
                End If
                wMain.WindowState = WindowState.Minimized

            Case "START_lock"
                mySystem.Lock()
            Case "START_logoff"
                mySystem.LogOff()
            Case "START_standby"
                mySystem.StandBy()
            Case "START_turnoff"
                mySystem.PowerOff()
            Case "START_restart"
                mySystem.Restart()

            Case "START_control"
                myFile.Launch(wMain, "control", False, sMessage)
            Case "START_printers"
                myFile.Launch(wMain, Chr(34) + "control" + Chr(34) + " " + "printers", False, sMessage)
            Case "START_sounds"
                myFile.Launch(wMain, "control", False, sMessage)
            Case "START_colors"
                myFile.Launch(wMain, "colorcpl", False, sMessage)
            Case "START_power"
                myFile.Launch(wMain, Chr(34) + "control" + Chr(34) + " " + "powercfg.cpl", False, sMessage)
            Case "START_monitor"
                myFile.Launch(wMain, Chr(34) + "control" + Chr(34) + " " + "desk.cpl", False, sMessage)
            Case "START_scheduler"
                myFile.Launch(wMain, "taskschd.msc", False, sMessage)
            Case "START_uninstall"
                myFile.Launch(wMain, Chr(34) + "control" + Chr(34) + " " + "appwiz.cpl", False, sMessage)
            Case "START_connections"
                myFile.Launch(wMain, Chr(34) + "control" + Chr(34) + " " + "ncpa.cpl", False, sMessage)
            Case "START_remote"
                myFile.Launch(wMain, "mstsc.exe", False, sMessage)
            Case "START_performance"
                myFile.Launch(wMain, "SystemPropertiesPerformance.exe", False, sMessage)
            Case "START_sound"
                myFile.Launch(wMain, Chr(34) + "control" + Chr(34) + " " + "mmsys.cpl", False, sMessage)

            Case "START_libraries"
                myFile.Launch(wMain, "shell:Libraries", False, sMessage)
            Case "START_homegroup"
                myFile.Launch(wMain, "shell:HomeGroupFolder", False, sMessage)
            Case "START_user"
                myFile.Launch(wMain, "shell:UsersFilesFolder", False, sMessage)
            Case "START_computer"
                myFile.Launch(wMain, "shell:MyComputerFolder", False, sMessage)
            Case "START_network"
                myFile.Launch(wMain, "shell:NetworkPlacesFolder", False, sMessage)
            Case "START_bin"
                myFile.Launch(wMain, "shell:RecycleBinFolder", False, sMessage)
        End Select
    End Sub

    Private Shared Function CalcWindow() As Window
        For Each wOne As Window In Application.Current.Windows
            If wOne.Name = "wCalc" Then Return wOne
        Next
        Return Nothing
    End Function

#End Region

End Class
