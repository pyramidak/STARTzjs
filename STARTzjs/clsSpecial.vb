Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices

#Region " KeyType "

Public Class clsKeyType
    Private sMod, sKey As String
    Private kMod As ModifierKeys
    Public Modifiers As ModifierKeys
    Private kKey As Key
    Public Keys As Key
    Public Text As String
    Public Canceled As Boolean
    Public Event Catched(ByVal KeyType As clsKeyType)

    Public Sub Test(e As System.Windows.Input.KeyEventArgs)
        If Canceled Then Exit Sub
        kMod = Nothing : kKey = Nothing : sMod = "" : sKey = ""

        If e.Key = Key.Back Then
            Text = If(Application.myGlobal.mySystem.LgeCzech, "(zrušit)", "(remove)")
            Canceled = True
            RaiseEvent Catched(Me)
        ElseIf e.Key = Key.System Then
            kKey = e.SystemKey : sKey = e.SystemKey.ToString
        Else
            kKey = e.Key : sKey = e.Key.ToString
        End If
        If sKey.ToLower.Contains("ctrl") Or sKey.ToLower.Contains("alt") Or sKey.ToLower.Contains("shift") Or sKey.ToLower.Contains("win") Then Exit Sub

        If Keyboard.IsKeyDown(Key.LeftAlt) Or Keyboard.IsKeyDown(Key.RightAlt) Then kMod = CType(kMod + ModifierKeys.Alt, ModifierKeys) : sMod += "ALT + "
        If Keyboard.IsKeyDown(Key.LeftCtrl) Or Keyboard.IsKeyDown(Key.RightCtrl) Then kMod = CType(kMod + ModifierKeys.Control, ModifierKeys) : sMod += "CTRL + "
        If Keyboard.IsKeyDown(Key.LeftShift) Or Keyboard.IsKeyDown(Key.RightShift) Then kMod = CType(kMod + ModifierKeys.Shift, ModifierKeys) : sMod += "SHIFT + "
        If Keyboard.IsKeyDown(Key.LWin) Or Keyboard.IsKeyDown(Key.RWin) Then kMod = CType(kMod + ModifierKeys.Windows, ModifierKeys) : sMod += "WIN + "

        If Keyboard.IsKeyDown(Key.LeftCtrl) Or Keyboard.IsKeyDown(Key.RightCtrl) _
            Or Keyboard.IsKeyDown(Key.LeftAlt) Or Keyboard.IsKeyDown(Key.RightAlt) _
            Or Keyboard.IsKeyDown(Key.LeftShift) Or Keyboard.IsKeyDown(Key.RightShift) _
            Or Keyboard.IsKeyDown(Key.LWin) Or Keyboard.IsKeyDown(Key.RWin) Then

            Text = sMod + sKey
            Modifiers = kMod
            Keys = kKey
            RaiseEvent Catched(Me)
        End If
    End Sub

End Class

#End Region

#Region " Rycycle Bin "

Public Class clsRecycleBin

    Sub New()
    End Sub

    Public Function IsEmpty() As Boolean
        Dim info As SHRECYCLEBININFO
        info.cbSize = Runtime.InteropServices.Marshal.SizeOf(info)
        Dim res As UInteger = SHQueryRecycleBin(vbNullString, info)
        Return If(info.i64Size = 0, True, False)
    End Function

    Public Sub Empty(AskToConfirm As Boolean)
        Try
            Dim res As UInteger = SHEmptyRecycleBin(IntPtr.Zero, Nothing, If(AskToConfirm, Nothing, RecycleFlags.SHRB_NOCONFIRMATION))
        Catch ex As Exception
        End Try
    End Sub

    Private Enum RecycleFlags As UInteger
        SHRB_NOCONFIRMATION = &H1
        SHRB_NOPROGRESSUI = &H2
        SHRB_NOSOUND = &H4
    End Enum

    <Runtime.InteropServices.DllImport("Shell32.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Shared Function SHEmptyRecycleBin(ByVal hwnd As IntPtr, ByVal pszRootPath As String, ByVal dwFlags As RecycleFlags) As UInteger
    End Function

    Private Structure SHRECYCLEBININFO
        Dim cbSize As Integer
        Dim i64Size As Long
        Dim i64NumItems As Long
    End Structure

    <Runtime.InteropServices.DllImport("Shell32.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Shared Function SHQueryRecycleBin(ByVal pszRootPath As String, ByRef pSHQueryRBInfo As SHRECYCLEBININFO) As UInteger
    End Function

End Class

#End Region

#Region " Folders Watch "

Public Class clsWatchFolder
    Inherits Collection(Of IO.FileSystemWatcher)

    Private enabled As Boolean
    Public Property IsEnabled As Boolean
        Get
            Return enabled
        End Get
        Set(ByVal value As Boolean)
            enabled = value
            Me.ToList.ForEach(Sub(a) a.EnableRaisingEvents = enabled)
        End Set
    End Property

    Overloads Sub Add(path As String, enable As Boolean)
        If path Is Nothing OrElse path = "" OrElse myFolder.Exist(path) = False Then Exit Sub
        Dim Watcher As IO.FileSystemWatcher = Me.FirstOrDefault(Function(a) a.Path = path)
        If Watcher Is Nothing Then
            Watcher = New IO.FileSystemWatcher(path)
            AddHandler Watcher.Created, AddressOf FolderChanged
            AddHandler Watcher.Deleted, AddressOf FolderChanged
            AddHandler Watcher.Changed, AddressOf FolderChanged
            AddHandler Watcher.Renamed, AddressOf FolderChanged
            Watcher.EnableRaisingEvents = enable
            Me.Add(Watcher)
        End If
    End Sub

    Overloads Sub Remove(path As String)
        Dim Watcher As IO.FileSystemWatcher = Me.FirstOrDefault(Function(a) a.Path = path)
        If Watcher IsNot Nothing Then
            RemoveHandler Watcher.Created, AddressOf FolderChanged
            RemoveHandler Watcher.Deleted, AddressOf FolderChanged
            RemoveHandler Watcher.Changed, AddressOf FolderChanged
            RemoveHandler Watcher.Renamed, AddressOf FolderChanged
            Watcher.Dispose()
            Me.Remove(Watcher)
        End If
    End Sub

    Sub New()
    End Sub

    Sub New(AlarmTable As ObservableCollection(Of clsSetting.clsAlarm))
        AlarmTable.Where(Function(x) x.Watch = "dir").ToList.ForEach(Sub(y) Me.Add(y.When4, False))
    End Sub

    Private Sub FolderChanged(sender As Object, e As IO.FileSystemEventArgs)
        Dim Watcher As IO.FileSystemWatcher = CType(sender, IO.FileSystemWatcher)
        For Each Alarm In Dat.AlarmMem
            If Alarm.Watch = "dir" AndAlso Alarm.When4 = Watcher.Path Then
                If Alarm.When2 = 0 Or Alarm.When2 = e.ChangeType Or Alarm.When2 + 5 = e.ChangeType Then
                    Alarm.Message = e.Name + " was " + e.ChangeType.ToString.ToLower + "."
                    Alarm.Execute = True
                End If
            End If
        Next
    End Sub

End Class

#End Region

#Region " Autostart "

Public Class clsAutostart

    Private arrAutostart As New ArrayList
    Private AutostartLoaded As Date
    Private regCesta As String = "Software\Microsoft\Windows\CurrentVersion\"

    Public Function LoadList(Optional ByVal Force As Boolean = False) As ArrayList
        If AutostartLoaded.AddMinutes(5) > Now And Force = False Then Return arrAutostart
        AutostartLoaded = Now
        arrAutostart.Clear()
        For Each oneFile As String In myFolder.Files(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "*.lnk", True)
            arrAutostart.Add(myFile.RemoveQuotationMarks(New clsShortcut(oneFile).Path.ToLower))
        Next
        For iRoot = 2 To 4 Step 2
            Dim RegFiles() As String = myRegister.QueryNames(CType(iRoot, HKEY), regCesta + "Run")
            For Each oneFile As String In RegFiles
                Dim sFoundFile As String = myRegister.GetValue(CType(iRoot, HKEY), regCesta + "Run", oneFile, "")
                If Not sFoundFile = "" Then arrAutostart.Add(myFile.RemoveQuotationMarks(sFoundFile.ToLower))
            Next
        Next
        Return arrAutostart
    End Function

    Public Sub Create(ByVal sName As String, ByVal sPath As String, ByVal bZapnout As Boolean)
        If bZapnout = False Then
            For Each oneFile As String In myFolder.Files(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "*.lnk", True)
                If LCase(New clsShortcut(oneFile).Path) = LCase(sPath) Then
                    myFile.Delete(oneFile, True)
                End If
            Next
            myRegister.DeleteValue(HKEY.LOCALE_MACHINE, regCesta + "Run", myRegister.FindValue(HKEY.LOCALE_MACHINE, regCesta + "Run", sPath))
            myRegister.DeleteValue(HKEY.CURRENT_USER, regCesta + "Run", myRegister.FindValue(HKEY.CURRENT_USER, regCesta + "Run", sPath))
        Else
            myRegister.CreateValue(HKEY.CURRENT_USER, regCesta + "Run", sName, sPath)
        End If
        LoadList(True)
    End Sub
End Class

#End Region

#Region " Windows Colors "

#Region " Windows Theme Color "

Public Class clsWinColors

    Public Background As Color
    Public Selection As Color
    Public LightBackground As Color

    Sub New()
        UpdateColors()
    End Sub

    Public Function UpdateColors() As Color
        Dim mColor As Color
        Select Case Dat.Options.MenuColor
            Case 0
                mColor = GetThemeColor()
            Case 1
                mColor = GetDesktopColor()
            Case 2
                mColor = Dat.Options.OwnColor
        End Select
        If mColor = Color.FromArgb(0, 0, 0, 0) Then mColor = Dat.Options.OwnColor
        Selection = myColorConverter.LighterColor(mColor, 1 / 2)
        LightBackground = myColorConverter.LighterColor(mColor, 3 / 4)
        mColor.A = CByte(TransMultiple * Dat.Options.Transparency)
        Background = mColor
        Return mColor
    End Function

    Private Function GetDesktopColor() As Color
        Dim corner As Point = myWindow.PPItoPixel(New Point(2, CInt(SystemParameters.PrimaryScreenHeight / 2)), False)
        Dim BMP As New System.Drawing.Bitmap(1, 1)
        Dim GFX As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(BMP)
        Try
            GFX.CopyFromScreen(New System.Drawing.Point(CInt(corner.X), CInt(corner.Y)), New System.Drawing.Point(0, 0), BMP.Size)
            Dim dColor As System.Drawing.Color = BMP.GetPixel(0, 0)
            Return myColorConverter.ColorDrawingToMedia(dColor)
        Catch
            Return Color.FromArgb(0, 0, 0, 0)
        End Try
    End Function

    Private Function GetThemeColor() As Color
        Dim corner As Point
        If mySystem.GetTaskbarLocation() = clsSystem.TaskbarLocation.Bottom Or mySystem.GetTaskbarLocation() = clsSystem.TaskbarLocation.Left Then
            corner = New Point(2, SystemParameters.PrimaryScreenHeight - 2)
        Else
            corner = New Point(SystemParameters.PrimaryScreenWidth - 2, 2)
        End If
        corner = myWindow.PPItoPixel(corner, False)
        Dim BMP As New System.Drawing.Bitmap(1, 1)
        Dim GFX As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(BMP)
        Try
            GFX.CopyFromScreen(New System.Drawing.Point(CInt(corner.X), CInt(corner.Y)), New System.Drawing.Point(0, 0), BMP.Size)
            Dim dColor As System.Drawing.Color = BMP.GetPixel(0, 0)
            Return myColorConverter.ColorDrawingToMedia(dColor)
        Catch
            Return Color.FromArgb(0, 0, 0, 0)
        End Try
    End Function

End Class

#End Region

#Region " Transparency and Glass Effect "

Class clsGlass

    Private Enum DwmBlurBehindFlags As UInteger
        ''' Indicates a value for fEnable has been specified.
        DWM_BB_ENABLE = &H1
        ''' Indicates a value for hRgnBlur has been specified.
        DWM_BB_BLURREGION = &H2
        ''' Indicates a value for fTransitionOnMaximized has been specified.
        DWM_BB_TRANSITIONONMAXIMIZED = &H4
    End Enum

    <DllImport("uxtheme", CharSet:=CharSet.Unicode)>
    Private Shared Function SetWindowTheme(ByVal hWnd As IntPtr, ByVal textSubAppName As String, ByVal textSubIdList As String) As Integer
    End Function

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmIsCompositionEnabled(<MarshalAs(UnmanagedType.Bool)> ByRef pfEnabled As Boolean) As Integer
    End Function

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmExtendFrameIntoClientArea(ByVal hwnd As IntPtr, ByRef pMarInset As ExtendedMargins) As Integer
    End Function

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmEnableBlurBehindWindow(ByVal hWnd As IntPtr, ByRef pBlurBehind As DWM_BLURBEHIND) As Integer
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure ExtendedMargins
        Public cxLeftWidth As Integer
        Public cxRightWidth As Integer
        Public cyTopHeight As Integer
        Public cyBottomHeight As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure DWM_BLURBEHIND
        Public dwFlags As DwmBlurBehindFlags
        Public fEnable As Boolean
        Public hRgnBlur As IntPtr
        Public fTransitionOnMaximized As Boolean
    End Structure

    Private mTransparentColor As System.Windows.Media.Color = CType(System.Windows.Media.ColorConverter.ConvertFromString("#01FFFFFF"), System.Windows.Media.Color)

    Private Function IsGlassEnabled(ByVal wnd As Window) As Boolean
        If Environment.OSVersion.Version.Major < 6 Then Return False

        Dim isGlassSupported As Boolean = False
        DwmIsCompositionEnabled(isGlassSupported)

        If Application.myGlobal.mySystem.Current.Number > 7 Or isGlassSupported = False Then
            Return TransparencyEffect(wnd)
        End If

        Return isGlassSupported
    End Function

    Public Function TransparencyEffect(ByVal wnd As Window) As Boolean
        wnd.Background = New SolidColorBrush(mTransparentColor)
        Dim mColor As System.Windows.Media.Color
        Select Case wnd.Name
            Case "wMain"

            Case "wKey"
                mColor = CType(System.Windows.Media.ColorConverter.ConvertFromString("#320063FF"), System.Windows.Media.Color)
                Dim wKey As wpfKey = CType(wnd, wpfKey)
                wKey.panGrid.Background = New SolidColorBrush(mColor)
            Case "wCalc"
                Return True

            Case "wTree"
                Return True
        End Select
        Return False
    End Function

    Public Function GlassEffect(ByVal wnd As Window) As Boolean
        If IsGlassEnabled(wnd) = False Then Return False

        Dim Margins As New ExtendedMargins
        Margins.cxLeftWidth = -1
        Margins.cxRightWidth = -1
        Margins.cyTopHeight = -1
        Margins.cyBottomHeight = -1

        Dim blurBehindParameters As New DWM_BLURBEHIND()
        blurBehindParameters.dwFlags = DwmBlurBehindFlags.DWM_BB_ENABLE
        blurBehindParameters.fEnable = True
        blurBehindParameters.hRgnBlur = IntPtr.Zero

        wnd.Background = New SolidColorBrush(mTransparentColor)
        Dim mainWindowPtr As IntPtr = New System.Windows.Interop.WindowInteropHelper(wnd).Handle
        Dim mainWindowSrc As System.Windows.Interop.HwndSource = System.Windows.Interop.HwndSource.FromHwnd(mainWindowPtr)
        Dim memColor As System.Windows.Media.Color = mainWindowSrc.CompositionTarget.BackgroundColor
        mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0)

        Try
            Dim h As Integer
            Select Case wnd.WindowStyle
                Case WindowStyle.SingleBorderWindow, WindowStyle.ThreeDBorderWindow, WindowStyle.ToolWindow
                    wnd.ResizeMode = ResizeMode.CanResize
                    h = DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, Margins)

                Case WindowStyle.None
                    wnd.ResizeMode = ResizeMode.NoResize
                    h = DwmEnableBlurBehindWindow(mainWindowSrc.Handle, blurBehindParameters)

            End Select
            If h < 0 Then
                mainWindowSrc.CompositionTarget.BackgroundColor = memColor
                Return False
            End If
        Catch generatedExceptionName As DllNotFoundException
            mainWindowSrc.CompositionTarget.BackgroundColor = memColor
            Return False
        End Try
        Return True
    End Function

End Class

#End Region

#End Region

'potřeba upravit s novým PointToScreen
#Region " Monitor "

Public Class clsMonitor

    Private Const MONITOR_DEFAULTTOPRIMERTY As Int32 = &H1
    Private Const MONITOR_DEFAULTTONEAREST As Int32 = &H2

    <DllImport("user32.dll")>
    Private Shared Function MonitorFromWindow(handle As IntPtr, flags As Int32) As IntPtr
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    <Runtime.Versioning.ResourceExposure(Runtime.Versioning.ResourceScope.None)>
    Private Shared Function GetMonitorInfo(hmonitor As HandleRef, <[In], Out> info As MonitorInfoEx) As Boolean
    End Function

    <DllImport("user32.dll", ExactSpelling:=True)>
    <Runtime.Versioning.ResourceExposure(Runtime.Versioning.ResourceScope.None)>
    Private Shared Function EnumDisplayMonitors(hdc As HandleRef, rcClip As IntPtr, lpfnEnum As MonitorEnumProc, dwData As IntPtr) As Boolean
    End Function

    Private Delegate Function MonitorEnumProc(monitor As IntPtr, hdc As IntPtr, lprcMonitor As IntPtr, lParam As IntPtr) As Boolean

    <StructLayout(LayoutKind.Sequential)>
    Private Structure Rect
        Public left As Integer
        Public top As Integer
        Public right As Integer
        Public bottom As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto, Pack:=4)>
    Private Class MonitorInfoEx
        Friend cbSize As Integer = Marshal.SizeOf(GetType(MonitorInfoEx))
        Friend rcMonitor As New Rect()
        Friend rcWork As New Rect()
        Friend dwFlags As Integer = 0
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=32)>
        Friend szDevice As Char() = New Char(31) {}
    End Class

    Private Const MonitorinfofPrimary As Integer = &H1
    Public Shared NullHandleRef As New HandleRef(Nothing, IntPtr.Zero)

    Private m_Bounds As System.Windows.Rect
    Private m_WorkingArea As System.Windows.Rect
    Private m_Name As String
    Private m_IsPrimary As Boolean

#Region " Get/Set "

    Public Property Bounds() As System.Windows.Rect
        Get
            Return m_Bounds
        End Get
        Private Set
            m_Bounds = Value
        End Set
    End Property

    Public Property WorkingArea() As System.Windows.Rect
        Get
            Return m_WorkingArea
        End Get
        Private Set
            m_WorkingArea = Value
        End Set
    End Property

    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Private Set
            m_Name = Value
        End Set
    End Property

    Public Property IsPrimary() As Boolean
        Get
            Return m_IsPrimary
        End Get
        Private Set
            m_IsPrimary = Value
        End Set
    End Property

#End Region

    Private Sub New(monitor__1 As IntPtr, hdc As IntPtr)
        Dim info = New MonitorInfoEx()
        GetMonitorInfo(New HandleRef(Nothing, monitor__1), info)
        Bounds = New System.Windows.Rect(info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.right - info.rcMonitor.left, info.rcMonitor.bottom - info.rcMonitor.top)
        WorkingArea = New System.Windows.Rect(info.rcWork.left, info.rcWork.top, info.rcWork.right - info.rcWork.left, info.rcWork.bottom - info.rcWork.top)
        IsPrimary = ((info.dwFlags And MonitorinfofPrimary) <> 0)
        Name = New String(info.szDevice).TrimEnd(ChrW(0))
    End Sub

    Public Shared ReadOnly Property AllMonitors() As IEnumerable(Of clsMonitor)
        Get
            Dim closure = New MonitorEnumCallback()
            Dim proc = New MonitorEnumProc(AddressOf closure.Callback)
            EnumDisplayMonitors(NullHandleRef, IntPtr.Zero, proc, IntPtr.Zero)
            Return closure.Monitors.Cast(Of clsMonitor)()
        End Get
    End Property

    Private Class MonitorEnumCallback
        Public Property Monitors() As ArrayList
            Get
                Return m_Monitors
            End Get
            Private Set
                m_Monitors = Value
            End Set
        End Property
        Private m_Monitors As ArrayList

        Public Sub New()
            Monitors = New ArrayList()
        End Sub

        Public Function Callback(monitor As IntPtr, hdc As IntPtr, lprcMonitor As IntPtr, lparam As IntPtr) As Boolean
            Monitors.Add(New clsMonitor(monitor, hdc))
            Return True
        End Function
    End Class
End Class

#End Region

#Region " Alarm Sheet "

Public Class clsSheet
    Inherits ObjectModel.Collection(Of clsRow)

    Public colAlarm As New ObjectModel.Collection(Of clsSetting.clsAlarm)
    Public csvCount As Integer

#Region " Row "

    Public Class clsRow
        Private sDevice As String
        Private sVelicina As String
        Private dHodnota As Double
        Private dDatum As Date

#Region " Get/Set "

        Public Property Device() As String
            Get
                Return sDevice
            End Get
            Set(ByVal value As String)
                sDevice = value
            End Set
        End Property

        Public Property Variable() As String
            Get
                Return sVelicina
            End Get
            Set(ByVal value As String)
                sVelicina = value
            End Set
        End Property

        Public Property Value() As Double
            Get
                Return dHodnota
            End Get
            Set(ByVal value As Double)
                dHodnota = value
            End Set
        End Property

        Public Property DateTime() As Date
            Get
                Return dDatum
            End Get
            Set(ByVal value As Date)
                dDatum = value
            End Set
        End Property

#End Region

        Sub New(zarizeni As String, velicina As String, hodnota As Double, datum As Date)
            sDevice = zarizeni : sVelicina = velicina : dHodnota = hodnota : dDatum = datum
        End Sub

        Sub New(zarizeni As String, velicina As String, hodnota As String, datum As String)
            sDevice = zarizeni : sVelicina = velicina : dHodnota = myString.GetDouble(hodnota) : dDatum = myString.GetDate(datum)
        End Sub

    End Class

    Public Overloads Sub Add(zarizeni As String, velicina As String, hodnota As String, datum As String, Optional omez As DateTime = Nothing)
        Dim Value As Double = myString.GetDouble(hodnota)
        Dim DatTime As Date = myString.GetDate(datum)
        If Not zarizeni = "" And Not velicina = "" And Not Value = -1 And Not DatTime = New Date Then
            If Not omez = Nothing AndAlso DatTime < omez Then 'pouze novější datum
            Else
                Me.Add(New clsRow(zarizeni, velicina, Value, DatTime))
            End If
        End If
    End Sub

#End Region

#Region " Load CSV file "

    Public Function LoadSheet(stream As IO.MemoryStream, Optional porovnej As Integer = 0, Optional omez As DateTime = Nothing) As Boolean
        Dim table As Data.DataTable = New clsCSV().GetDataTable(stream)
        stream.Close()
        If porovnej = table.Rows.Count Then Return False
        csvCount = table.Rows.Count

        'Ověření správnosti formátu sloupečků dat z csv
        Dim FirstRow As Integer = -1
        If table.Columns.Count >= 3 Then
            FirstRow = CheckTableValues(table, 0)
            If FirstRow = -1 Then
                FirstRow = CheckTableValues(table, 1)
            End If
        End If
        If FirstRow = -1 Then Return False

        Me.Clear()
        For a As Integer = FirstRow To table.Rows.Count - 1
            Dim b As Integer = table.Rows.Count - 1 - a + FirstRow 'pozpátku od nejnovějších záznamů
            Me.Add(table.Rows(b).Item(0).ToString, table.Rows(b).Item(1).ToString, table.Rows(b).Item(2).ToString, table.Rows(b).Item(3).ToString, omez)
        Next
        table.Dispose()

        Return If(Me.Count = 0, False, True)
    End Function

    Private Function CheckTableValues(table As Data.DataTable, radek As Integer) As Integer
        If Not myString.GetDouble(table.Rows(radek).Item(2).ToString) = -1 Then
            If Not myString.GetDate(table.Rows(radek).Item(3).ToString) = New Date Then
                Return radek
            End If
        End If
        Return -1
    End Function

#End Region

End Class

#End Region