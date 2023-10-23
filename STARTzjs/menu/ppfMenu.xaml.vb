Class ppfMenu

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()
    Private Spusteno As Boolean
    Private timPickup As New Threading.DispatcherTimer
    Private uriImg As New Uri("/STARTzjs;component/Images/RGBpalette.png", UriKind.Relative)

#Region " Loaded "

    Private Sub ppfInfo_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
        timPickup.Interval = TimeSpan.FromMilliseconds(400)
        AddHandler timPickup.Tick, AddressOf timPickup_Tick
        Spusteno = True
        SliderSize.Value = Dat.Options.ItemsSize
        SliderTrans.Value = Dat.Options.Transparency
        cbxBarva.Text = Dat.Options.OwnColor.ToString
        If mySystem.Framework >= 4 And mySystem.Current.Number > 7 Then
            cbxBarva.Foreground = myColorConverter.ColorToBrush(Dat.Options.OwnColor)
        Else
            cbxBarva.Background = myColorConverter.ColorToBrush(Dat.Options.OwnColor)
        End If
        Select Case Dat.Options.MenuColor
            Case 0
                rbnTheme.IsChecked = True
            Case 1
                rbnDesktop.IsChecked = True
            Case 2
                rbnOwn.IsChecked = True
        End Select
        Select Case Dat.Options.ItemLook
            Case 0
                rbnNoBorder.IsChecked = True
            Case 1
                rbnEllipse.IsChecked = True
        End Select
    End Sub

#End Region

#Region " Color Picker "

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
        myFile.Launch(wSetting, Chr(34) + "control" + Chr(34) + " " + "desk.cpl")
    End Sub

    Private Sub timPickup_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim BMP As New System.Drawing.Bitmap(1, 1)
        Dim GFX As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(BMP)
        Dim Pointer As Point = myWindow.PixelToPPI(myWindow.GetMousePosition(), False)
        GFX.CopyFromScreen(New System.Drawing.Point(CInt(Pointer.X), CInt(Pointer.Y)), New System.Drawing.Point(0, 0), BMP.Size)
        Dim dColor As System.Drawing.Color = BMP.GetPixel(0, 0)
        Dim mColor As Color = myColorConverter.ColorDrawingToMedia(dColor)
        cbxBarva.Text = mColor.ToString
        mColor.A = CByte(200)
        If mySystem.Framework >= 4 And mySystem.Current.Number > 7 Then
            cbxBarva.Foreground = myColorConverter.ColorToBrush(mColor)
        Else
            cbxBarva.Background = myColorConverter.ColorToBrush(mColor)
        End If
        Dat.Options.OwnColor = mColor
        BoxColumnList.UpdateLook()
    End Sub

#End Region

#Region " Combo "

    Private Sub cbxColor_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles cbxBarva.SelectionChanged
        Dim cbx As ComboBox = CType(sender, ComboBox)
        cbx.SelectedIndex = -1
    End Sub

    Private Sub cbxColor_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs)
        Dim cbx As ComboBox = CType(sender, ComboBox)
        If Not cbx.Text.StartsWith("#") And Not cbx.Text = "" Then
            If IsNothing(myColorConverter.NameToColor(cbx.Text)) = False Then
                Dim mColor As Color = myColorConverter.NameToColor(cbx.Text)
                If Not mColor.ToString = "#00000000" Then
                    cbx.Background = myColorConverter.ColorToBrush(mColor)
                    cbx.Text = mColor.ToString
                    Dat.Options.OwnColor = mColor
                    BoxColumnList.UpdateLook()
                End If
            End If
        End If
    End Sub

    Private Sub cbxColor_DropDownClosed(sender As System.Object, e As System.EventArgs) Handles cbxBarva.DropDownClosed
        Dim cbx As ComboBox = CType(sender, ComboBox)
        timPickup.Stop()
        cbx.Background = myColorConverter.ColorToBrush(Dat.Options.OwnColor)
        cbx.Text = Dat.Options.OwnColor.ToString
    End Sub

    Private Sub cbxColor_DropDownOpened(sender As System.Object, e As System.EventArgs) Handles cbxBarva.DropDownOpened
        Dim cbx As ComboBox = CType(sender, ComboBox)
        Dim Item As ComboBoxItem = CType(cbx.Items(0), ComboBoxItem)
        For Each Control As Object In DirectCast(Item.Content, Grid).Children
            If TypeOf Control Is Image Then
                Dim imgFound As Image = CType(Control, Image)
                imgFound.Source = New BitmapImage(uriImg)
            End If
        Next
        timPickup.Start()
    End Sub

#End Region

    Private Sub SliderSize_ValueChanged(ByVal sender As System.Object, ByVal e As System.Windows.RoutedPropertyChangedEventArgs(Of System.Double)) Handles SliderSize.ValueChanged
        If Spusteno Then
            Dat.Options.ItemsSize = CInt(SliderSize.Value)
            BoxColumnList.ResizeItems()
        End If
    End Sub

    Private Sub SliderTrans_ValueChanged(ByVal sender As System.Object, ByVal e As System.Windows.RoutedPropertyChangedEventArgs(Of System.Double)) Handles SliderTrans.ValueChanged
        If Spusteno Then ChangeColor()
    End Sub

    Private Sub ChangeColor()
        If Spusteno Then
            Dat.Options.Transparency = CInt(SliderTrans.Value)
            BoxColumnList.UpdateLook()
            Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
        End If
    End Sub

    Private Sub rbnColor_Checked(sender As Object, e As RoutedEventArgs) Handles rbnOwn.Checked, rbnDesktop.Checked, rbnTheme.Checked
        cbxBarva.IsEnabled = CBool(rbnOwn.IsChecked)
        If rbnTheme.IsChecked = True Then Dat.Options.MenuColor = 0
        If rbnDesktop.IsChecked = True Then Dat.Options.MenuColor = 1
        If rbnOwn.IsChecked = True Then Dat.Options.MenuColor = 2
        ChangeColor()
    End Sub

    Private Sub rbnLook_Checked(sender As Object, e As RoutedEventArgs) Handles rbnNoBorder.Checked, rbnEllipse.Checked
        If rbnNoBorder.IsChecked = True Then Dat.Options.ItemLook = 0
        If rbnEllipse.IsChecked = True Then Dat.Options.ItemLook = 1
        ChangeColor()
    End Sub
End Class