Class ppfWindows

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()
    Private sRegShake As String = "SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
    Private sRegHibernate As String = "SYSTEM\CurrentControlSet\Control\Power"
    Private bChange As Boolean = False

#Region " Loaded "

    Private Sub ppfWindows_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Border1.Background = myColorConverter.ColorToBrush(myWinColor.LightBackground)
        lblHeader.Content = "Windows " + mySystem.Current.Name
        ckbSTARTzjs.IsChecked = Dat.Options.AutoStart
        ckbShake.IsChecked = Not myRegister.GetValue(HKEY.CURRENT_USER, sRegShake, "DisallowShaking", False)
        ckbHibernate.IsChecked = myRegister.GetValue(HKEY.LOCALE_MACHINE, sRegHibernate, "HibernateEnabled", False)
        bChange = True

        If Application.winStore Then
            ckbSTARTzjs.Visibility = Visibility.Collapsed
            btnAutostart.Visibility = Visibility.Visible
        Else
            ckbSTARTzjs.Visibility = Visibility.Visible
            btnAutostart.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub ckbShake_Checked(sender As Object, e As RoutedEventArgs) Handles ckbShake.Checked, ckbShake.Unchecked
        If bChange Then
            myRegister.CreateValue(HKEY.CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "DisallowShaking", If(ckbShake.IsChecked, 0, 1))
        End If
    End Sub

    Private Sub ckbHibernate_Checked(sender As Object, e As RoutedEventArgs) Handles ckbHibernate.Checked, ckbHibernate.Unchecked
        If bChange Then
            If ckbHibernate.IsChecked Then
                myFile.Launch(wSetting, "powercfg /h on", True)
            Else
                myFile.Launch(wSetting, "powercfg /h off", True)
            End If
        End If
    End Sub

#End Region

#Region " Autostart "

    Private Sub btnAutostart_Click(sender As Object, e As RoutedEventArgs) Handles btnAutostart.Click
        myLink.Start(wMain, "ms-settings:startupapps")
    End Sub

    Private Sub ckbSTARTzjs_Checked(sender As Object, e As RoutedEventArgs) Handles ckbSTARTzjs.Checked
        myAutostart.Create("STARTzjs", Chr(34) + appCesta & "\STARTzjs.exe" + Chr(34) + "-win", True)
        Dat.Options.AutoStart = True
    End Sub

    Private Sub ckbSTARTzjs_Unchecked(sender As Object, e As RoutedEventArgs) Handles ckbSTARTzjs.Unchecked
        myAutostart.Create("STARTzjs", appCesta & "\STARTzjs.exe", False)
        Dat.Options.AutoStart = False
    End Sub

#End Region

End Class
