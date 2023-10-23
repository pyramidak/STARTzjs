Imports System.ComponentModel
Imports System.Collections.ObjectModel

#Region " Data "

Public Class clsData

    Private Lge As Boolean = mySystem.LgeCzech
    Private wMain As wpfMain = DirectCast(Application.Current.MainWindow, wpfMain)
    Private WithEvents timSave As Threading.DispatcherTimer
    Public xmlPath As String
    Public Options As New clsSetting
    Public HasChanges As Boolean
    Public selCloud As Cloud

#Region " Výchozí "

    Public Class clsDefItem
        Public Name As String
        Public Path As String
        Public Icon As String
        Public Key As Key
        Public ModKey As ModifierKeys
        Public Compulsory As Boolean
        Public Type As BoxType

        Sub New(sName As String, sPath As String, sIcon As String, Optional bCompulsory As Boolean = False, Optional xType As BoxType = BoxType.Start, Optional iMod As ModifierKeys = 0, Optional iKey As Key = 0)
            Name = sName : Path = sPath : Icon = sIcon : Key = iKey : ModKey = iMod : Compulsory = bCompulsory : Type = xType
        End Sub
    End Class

    Private Sub DefaultItems()

        Dim DefMain As New Collection(Of clsDefItem)
        DefMain.Add(New clsDefItem(If(Lge, "Nastavení STARTzjs", "STARTzjs setting"), "START_setting", imgCesta + "setting128.png", True, BoxType.Start, ModifierKeys.Alt, Key.S))
        DefMain.Add(New clsDefItem(If(Lge, "Budík", "Alarmclock"), "START_alarm", imgCesta + "alarm128.png", True))
        DefMain.Add(New clsDefItem(If(Lge, "Kalkulačka", "Calculator"), "START_calc", imgCesta + "calc128.png", True, BoxType.Start, ModifierKeys.Alt, Key.C))
        DefMain.Add(New clsDefItem(If(Lge, "Ovládací panely", "Controls"), "START_control", imgCesta + "control/control128.png"))
        DefMain.Add(New clsDefItem(If(Lge, "Restartovat", "Restart"), "START_restart", imgCesta + "shutdown/restart128.png"))
        DefMain.Add(New clsDefItem(If(Lge, "Zamknout", "Lock"), "START_lock", imgCesta + "shutdown/lock128.png"))
        DefMain.Add(New clsDefItem(If(Lge, "Uspat", "StandBy"), "START_standby", imgCesta + "shutdown/standby128.png"))
        AddDefaultItems(DefMain, If(Lge, "Výchozí", "Default"), "START_setting", "")

        Dim DefControl As New Collection(Of clsDefItem)
        DefControl.Add(New clsDefItem(If(Lge, "Zvuk", "Sound"), "START_sound", "sound128.png"))
        DefControl.Add(New clsDefItem(If(Lge, "Zařízení a tiskárny", "Printers"), "START_printers", "printers128.png"))
        DefControl.Add(New clsDefItem(If(Lge, "Možnosti napájení", "Power"), "START_power", "power128.png"))
        DefControl.Add(New clsDefItem(If(Lge, "Možnosti rozlišení", "Monitor"), "START_monitor", "monitor128.png"))
        DefControl.Add(New clsDefItem(If(Lge, "Správa barev", "Colors"), "START_colors", "colors128.png"))
        DefControl.Add(New clsDefItem(If(Lge, "Síťová připojení", "Network Connections"), "START_connections", "NetworkConnections128.png"))
        DefControl.Add(New clsDefItem(If(Lge, "Plánovač úloh", "Task scheduler"), "START_scheduler", "scheduler128.png"))
        DefControl.Add(New clsDefItem(If(Lge, "Odinstalovat program", "Uninstall program"), "START_uninstall", "uninstall128.png"))
        AddDefaultItems(DefControl, If(Lge, "Ovládací panely", "Controls"), "START_monitor", "control/")

        Dim DefFolder As New Collection(Of clsDefItem)
        DefFolder.Add(New clsDefItem(If(Lge, "Knihovny", "Libraries"), "START_libraries", "Libraries128.png"))
        DefFolder.Add(New clsDefItem(If(Lge, "Domací skupina", "Homegroup"), "START_homegroup", "Homegroup128.png"))
        DefFolder.Add(New clsDefItem(mySystem.User, "START_user", "User128.png"))
        DefFolder.Add(New clsDefItem(If(Lge, "Počítač", "Computer"), "START_computer", "Computer128.png"))
        DefFolder.Add(New clsDefItem(If(Lge, "Síť", "Network"), "START_network", "Network128.png"))
        DefFolder.Add(New clsDefItem(If(Lge, "Koš", "Recycle Bin"), "START_bin", "RecycleBin_full128.png"))
        AddDefaultItems(DefFolder, If(Lge, "Složky", "Folders"), "START_computer", "folder/")

    End Sub

    Private Sub AddDefaultItems(DefItems As Collection(Of clsDefItem), toTheme As String, checkPath As String, imgFolder As String)
        For Each Item In Options.Items
            If Item.Path = checkPath Then toTheme = Item.Column : Exit For
        Next

        Dim ColumnCreated As Boolean
        Dim Column = Options.Columns.FirstOrDefault(Function(x) x.Name = toTheme)
        If Column Is Nothing Then
            ColumnCreated = True
            Options.AddColumn(toTheme, False, False)
        End If

        For Each Def In DefItems
            If ColumnCreated Then Def.Compulsory = True
            Dim Item = Items.FirstOrDefault(Function(x) x.Path = Def.Path)
            If Item Is Nothing And Def.Compulsory Then
                Options.AddItem(Def.Name, toTheme, Def.Path, If(imgFolder = "", Def.Icon, imgCesta + imgFolder + Def.Icon), 0, Def.ModKey, Def.Key, Def.Type, False, False, False, False)
            End If
        Next
    End Sub

#End Region

    Public ReadOnly Property AlarmNewID() As Integer
        Get
            Dim pocet As Integer
            For Each Alarm In Options.AlarmMem
                If Alarm.ID > pocet Then pocet = Alarm.ID
            Next
            pocet += 1
            Return pocet
        End Get
    End Property

    Public Function AlarmMem() As ObservableCollection(Of clsSetting.clsAlarm)
        Options.AlarmMem.Where(Function(a) a.ID = 0).ToList.ForEach(Sub(b) b.ID = Options.AlarmMem.IndexOf(b) + 1)
        Return Options.AlarmMem
    End Function

    Private Sub timSave_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timSave.Tick
        If HasChanges Then Save(True)
    End Sub

    Public Function CalcMem() As ArrayList
        Return Options.CalcMem
    End Function

    Public Function Columns() As Collection(Of clsSetting.clsColumn)
        Return Options.Columns
    End Function

    Public Function Items() As Collection(Of clsSetting.clsItem)
        Return Options.Items
    End Function

    Public Sub New()
        selCloud = myRegister.GetCloudMyApp
        Dim myCloud As New clsCloud
        xmlPath = myCloud.NewAppPath(selCloud, "pyramidak\" & mySystem.User & "START.sxml")

        If myFile.Exist(xmlPath) Then
            Options = CType(New clsSerialization(Options).ReadXml(xmlPath), clsSetting)
            Dim paths = Options.Items.Where(Function(x) x.BoxType = BoxType.UWP).Select(Function(y) y.Path).ToList
            If Not paths.Count = 0 Then myApps = New clsApps(paths)
        End If
        DefaultItems()

        timSave = New Threading.DispatcherTimer
        timSave.Interval = TimeSpan.FromMinutes(2)
        timSave.IsEnabled = True
    End Sub

    Public Sub ImportData(Path As String, Load As Boolean)

        If Path Like "*START.sxml" Then
            Dim xl As New clsSetting
            xl = CType(New clsSerialization(xl).ReadXml(Path), clsSetting)
            For Each Sloupec As clsSetting.clsColumn In xl.Columns
                Dim Column = Options.Columns.FirstOrDefault(Function(x) x.Name = Sloupec.Name)
                If Column Is Nothing Then
                    If Load Then
                        BoxColumnList.AddColumn(Sloupec.Name)
                    Else
                        Options.AddColumn(Sloupec.Name, Sloupec.Hidden, Sloupec.Sorted)
                    End If
                End If
                For Each Polozka In xl.Items
                    Dim Item = Options.Items.FirstOrDefault(Function(x) x.Name = Polozka.Name)
                    If Item Is Nothing Then
                        If Load Then
                            BoxColumnList.AddItem(Polozka)
                        Else
                            Options.Items.Add(Polozka)
                        End If
                    End If
                Next
            Next
        Else
            Dim ds As New setProgs
            ds.ReadXml(Path)
            For Each oneTheme As setProgs.tableThemeRow In ds.tableTheme
                Dim Column = Options.Columns.FirstOrDefault(Function(x) x.Name = oneTheme.theme)
                If Column Is Nothing Then
                    If Load Then
                        BoxColumnList.AddColumn(oneTheme.theme)
                    Else
                        Options.AddColumn(oneTheme.theme, oneTheme.hidden, oneTheme.sorted)
                    End If
                End If
                For Each oneProg As setProgs.tableProgsRow In oneTheme.GettableProgsRows
                    Dim Item = Options.Items.FirstOrDefault(Function(x) x.Name = oneProg.name)
                    If Item Is Nothing Then
                        Item = New clsSetting.clsItem
                        Item.Name = oneProg.name : Item.Column = oneProg.theme : Item.Path = oneProg.path
                        Item.IconPath = If(oneProg.IsiconNull, "", oneProg.icon)
                        Item.IconIndex = If(oneProg.IsiconindexNull, 0, oneProg.iconindex)
                        Item.ModKey = If(oneProg.IsiModNull, ModifierKeys.None, CType(oneProg.iMod, ModifierKeys))
                        Item.InputKey = If(oneProg.IsiKeyNull, Key.None, CType(oneProg.iKey, Key))
                        Item.BoxType = If(oneProg.IstypeNull, If(oneProg.IsfolderNull, BoxType.File, If(oneProg.folder, BoxType.Folder, BoxType.File)), CType(oneProg.type, BoxType))
                        Item.Hidden = If(oneProg.IshiddenNull, False, oneProg.hidden)
                        Item.Highlighted = If(oneProg.IshighlightNull, False, oneProg.highlight)
                        Item.Admin = If(oneProg.IsadminNull, False, oneProg.admin)
                        If Load Then
                            BoxColumnList.AddItem(Item)
                        Else
                            Options.Items.Add(Item)
                        End If
                    End If
                Next
            Next
        End If

    End Sub

    Public Sub Reload()
        timSave.IsEnabled = False

        If myFile.Exist(xmlPath) Then
            Options = CType(New clsSerialization(Options).ReadXml(xmlPath), clsSetting)
        End If
        DefaultItems()

        timSave.IsEnabled = True
    End Sub

    Public Sub CopyTo(Cesta As String)
        myFile.Copy(xmlPath, Cesta)
    End Sub

    Public Sub Save(AutoSave As Boolean)
        timSave.IsEnabled = False

        myRegister.WriteCloudMyApp(selCloud)
        myFolder.Exist(myFolder.Path(xmlPath), True)
        Call New clsSerialization(Options, wMain).WriteXml(xmlPath)
        If AutoSave = False Then myFile.Copy(xmlPath, xmlPath & ".bak")

        HasChanges = False
        timSave.IsEnabled = AutoSave
    End Sub

    Public Sub SaveOff()
        timSave.IsEnabled = False
    End Sub

    Public Sub SaveOn()
        timSave.IsEnabled = True
    End Sub

End Class

#End Region

#Region " Item "

Public Enum BoxType
    File = 0
    Folder = 1
    Link = 2
    Start = 3
    UWP = 4
    Unknown = 5
End Enum

Public Class clsBoxItem
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Protected Sub OnPropertyChanged(ByVal name As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

    Private bExist, bAutostart As Boolean
    Private sUninstall As String
    Public imgS As ImageSource
    Private cur As Cursor
    Private IconExtractor As clsExtractIcon
    Private iHeight, iWidth As Double
    Private wMain As wpfMain = DirectCast(Application.Current.MainWindow, wpfMain)
    Private HK As HotKey
    Private SetItem As clsSetting.clsItem
    Private colorBackground As Color
    Private iItemLook As Integer

#Region " Get/Set "

    Public Property ItemLook() As Integer
        Get
            Return iItemLook
        End Get
        Set(ByVal value As Integer)
            iItemLook = value
            OnPropertyChanged("ItemLook")
        End Set
    End Property

    Public Property BackColor() As Color
        Get
            Return colorBackground
        End Get
        Set(ByVal value As Color)
            colorBackground = value
            OnPropertyChanged("BackColor")
        End Set
    End Property

    Public ReadOnly Property Item As clsSetting.clsItem
        Get
            Return SetItem
        End Get
    End Property

    Public Property ItemHeight() As Double
        Get
            Return iHeight
        End Get
        Set(ByVal value As Double)
            iHeight = value
            OnPropertyChanged("ItemHeight")
        End Set
    End Property

    Public Property ItemWidth() As Double
        Get
            Return iWidth
        End Get
        Set(ByVal value As Double)
            iWidth = value * 0.83
            OnPropertyChanged("ItemWidth")
        End Set
    End Property

    Public Property HKey() As HotKey
        Get
            Return HK
        End Get
        Set(ByVal value As HotKey)
            HK = value
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Highlighted() As Boolean
        Get
            Return SetItem.Highlighted
        End Get
        Set(ByVal value As Boolean)
            SetItem.Highlighted = value
            OnPropertyChanged("Highlighted")
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Hidden() As Boolean
        Get
            Return SetItem.Hidden
        End Get
        Set(ByVal value As Boolean)
            SetItem.Hidden = value
            OnPropertyChanged("Hidden")
            Dat.HasChanges = True
        End Set
    End Property

    Public Property HideStart() As Boolean
        Get
            Return SetItem.HideStart
        End Get
        Set(ByVal value As Boolean)
            SetItem.HideStart = value
            OnPropertyChanged("HideStart")
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Admin() As Boolean
        Get
            Return SetItem.Admin
        End Get
        Set(ByVal value As Boolean)
            SetItem.Admin = value
            ExistCheck()
        End Set
    End Property

    Public Property Autostart() As Boolean
        Get
            Return bAutostart
        End Get
        Set(ByVal value As Boolean)
            bAutostart = value
        End Set
    End Property

    Public Property Uninstall() As String
        Get
            Return sUninstall
        End Get
        Set(ByVal value As String)
            sUninstall = value
        End Set
    End Property

    Public Property Icon() As String
        Get
            Return SetItem.IconPath
        End Get
        Set(ByVal value As String)
            SetItem.IconPath = value
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Cursor() As Cursor
        Get
            Return cur
        End Get
        Set(ByVal value As Cursor)
            cur = value
        End Set
    End Property

    Public Property IconIndex() As Integer
        Get
            Return SetItem.IconIndex
        End Get
        Set(ByVal value As Integer)
            SetItem.IconIndex = value
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Type() As BoxType
        Get
            Return Item.BoxType
        End Get
        Set(ByVal value As BoxType)
            Item.BoxType = value
        End Set
    End Property

    Public Property Exist() As Boolean
        Get
            Return bExist
        End Get
        Set(ByVal value As Boolean)
            bExist = value
            OnPropertyChanged("Exist")
        End Set
    End Property

    Public Property ImgSource() As ImageSource
        Get
            Return imgS
        End Get
        Set(ByVal value As ImageSource)
            imgS = value
            OnPropertyChanged("ImgSource")
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Name() As String
        Get
            Return SetItem.Name
        End Get
        Set(ByVal value As String)
            SetItem.Name = value
            OnPropertyChanged("Name")
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Theme() As String
        Get
            Return SetItem.Column
        End Get
        Set(ByVal value As String)
            SetItem.Column = value
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Path() As String
        Get
            Return SetItem.Path
        End Get
        Set(ByVal value As String)
            SetItem.Path = value
            Dat.HasChanges = True
        End Set
    End Property
#End Region

    Sub New(Item As clsSetting.clsItem)
        colorBackground = myWinColor.Selection
        iItemLook = Dat.Options.ItemLook
        Me.ItemHeight = ItemSize(Dat.Options.ItemsSize).Height
        Me.ItemWidth = ItemSize(Dat.Options.ItemsSize).Width
        SetItem = Item

        Try
            If Icon.StartsWith("§") Then
                Me.ImgSource = CType(wMain.FindResource(Icon.Substring(1, Icon.Length - 1)), ImageSource)
                cur = myBitmap.ToCursor(CType(wMain.FindResource(Icon.Substring(1, Icon.Length - 1)), DrawingImage), New Size(ItemSize(Dat.Options.ItemsSize).Width, ItemSize(Dat.Options.ItemsSize).Height))
            ElseIf Icon.StartsWith("/" & Application.ExeName & ";") Then
                Me.ImgSource = New BitmapImage(New Uri(Icon, UriKind.Relative))
                cur = myBitmap.ToCursor(New Uri(Icon, UriKind.Relative))
            ElseIf Icon.ToLower.EndsWith(".png") AndAlso myfile.Exist(Icon) Then
                Me.ImgSource = New BitmapImage(New Uri(Icon, UriKind.Absolute))
            End If
        Catch
        End Try

        If Type = BoxType.File Then bAutostart = isAutostart()

        EmptyHotkey()
        If Not SetItem.ModKey = ModifierKeys.None And Not SetItem.InputKey = Key.None Then 'And bExist
            AddHotkey(SetItem.ModKey, SetItem.InputKey)
        End If

        'Dim vlakno As New Threading.Thread(AddressOf ExistCheck)
        'vlakno.Start()
        ExistCheck()
    End Sub

#Region " HotKey "

    Public Sub ActivateHotkey()
        If Not HK.Modifiers = ModifierKeys.None And Not HK.Key = Key.None Then
            If wMain.myHotKey IsNot Nothing Then wMain.myHotKey.AddHotKey(HK)
        End If
    End Sub

    Private Sub EmptyHotkey()
        HK = New wpfMain.NewHotKey(Name, Key.None, ModifierKeys.None, False)
    End Sub

    Public Sub AddHotkey(Modkey As ModifierKeys, InputKey As Key)
        HK = New wpfMain.NewHotKey(Name, InputKey, Modkey, True)
        ActivateHotkey()
        SetItem.ModKey = Modkey
        SetItem.InputKey = InputKey
    End Sub

    Public Sub RemoveHotkey()
        If HK.Enabled = False Then Exit Sub
        Try 'Potlačení chyby kvůli Windows XP
            wMain.myHotKey.RemoveHotKey(HK)
        Catch
        End Try
        EmptyHotkey()
        SetItem.ModKey = ModifierKeys.None
        SetItem.InputKey = Key.None
    End Sub

#End Region

#Region " Exist "

    Public Sub ExistCheck()
        Select Case Type
            Case BoxType.Start
                Me.Exist = True
                Exit Sub
            Case BoxType.File
                Me.Exist = myFile.Exist(Path)
            Case BoxType.Folder
                Me.Exist = myFolder.Exist(Path)
            Case BoxType.Link
                If HideStart Then
                    Me.Exist = True
                Else
                    Dim Link As String = myLink.AbsoluteUri(Path)
                    If Link = "" Then
                        Me.Exist = False
                    Else
                        Me.Exist = True
                        Me.Path = Link
                    End If
                End If
            Case BoxType.UWP
                Me.Exist = myApps.Exist(Path)

        End Select

        If bExist Then
            If Icon = "" Then
                ExtractIcon(Path, Type)
            Else
                If Not Icon.StartsWith("§") And Not Icon.StartsWith("/" & Application.ExeName & ";") Then
                    If myFile.Exist(Icon) Then
                        If Icon.ToLower.EndsWith(".png") Then
                            ImgSource = New BitmapImage(New Uri(Icon, UriKind.Absolute))
                        Else
                            ExtractIcon(Icon, BoxType.File)
                        End If
                    Else
                        Icon = ""
                        IconIndex = 0
                    End If
                End If
            End If
        Else
            If Type = BoxType.Link Then
                Me.Exist = True
                ImgSource = CType(wMain.FindResource("WebRed"), ImageSource)
                cur = myBitmap.ToCursor(CType(wMain.FindResource("WebRed"), DrawingImage), New Size(ItemSize(Dat.Options.ItemsSize).Width, ItemSize(Dat.Options.ItemsSize).Height))
            Else
                ImgSource = New BitmapImage(New Uri(imgCesta & "folder/missing128.png", UriKind.Relative))
                cur = myBitmap.ToCursor(New Uri(imgCesta & "folder/missing128.png", UriKind.Relative))
            End If
        End If
    End Sub

#End Region

#Region " Icon "

    Private Sub ExtractIcon(Cesta As String, xType As BoxType)
        Select Case xType
            Case BoxType.File
                IconExtractor = New clsExtractIcon(Cesta)
                IconExtractor.ChangeIndexIconInFile(IconIndex)
                imgS = If(Admin, myBitmap.Merge(IconExtractor.GetImageSource, CType(wMain.FindResource("UAC"), ImageSource), 3, 4), IconExtractor.GetImageSource)
                cur = IconExtractor.GetCursor(48, False)
                IconExtractor.Dispose()

            Case BoxType.Folder
                GetIconByINI(Cesta)

            Case BoxType.Link
                Dim Ico As System.Drawing.Icon = myLink.WebIcon(Cesta)
                If Ico IsNot Nothing Then
                    imgS = myBitmap.IconToImageSource(Ico)
                    cur = myBitmap.ToCursor(Ico)
                End If

            Case BoxType.UWP
                Dim app = myApps.Find(Path)
                imgS = app.Image
                cur = myBitmap.ToCursor(app.Icon)

        End Select

        If bAutostart Then
            Me.ImgSource = myBitmap.Merge(imgS, mySystem.Current.Image, 3, 3)
        Else
            Me.ImgSource = imgS
        End If
    End Sub

    Private Sub GetIconByINI(Cesta As String)
        Dim sINIpath As String = myFile.Join(Cesta, "desktop.ini")
        If myFile.Exist(sINIpath) Then
            Dim Result As String = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconResource", "")
            If Result = "" Then Result = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconFile", "")
            If Not Result = "" Then
                IconIndex = 0
                If Result.Substring(0, 2) = ".." Then Exit Sub
                If Result.Contains(",") Then
                    Dim LastIndex As Integer = Result.LastIndexOf(",")
                    Icon = Result.Substring(0, LastIndex)
                    IconIndex = CInt(Result.Substring(LastIndex + 1, Result.Length - LastIndex - 1))
                Else
                    Icon = Result
                    Result = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconIndex", "")
                    If IsNumeric(Result) Then IconIndex = CInt(Result)
                End If
                If Icon.StartsWith("%SystemRoot%") Then
                    Icon = Icon.Replace("%SystemRoot%", Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                End If
                If Icon.Length > 2 AndAlso Not Icon.Substring(1, 1) = ":" Then
                    Icon = myFile.Join(Cesta, Icon)
                End If
                If myFile.Exist(Icon) Then
                    ExtractIcon(Icon, BoxType.File)
                    Exit Sub
                Else
                    Icon = "" : IconIndex = 0
                End If
            End If
        End If
        Me.ImgSource = New BitmapImage(New Uri(imgCesta & "folder/folder128.png", UriKind.Relative))
        cur = myBitmap.ToCursor(New Uri(imgCesta & "folder/folder128.png", UriKind.Relative))
    End Sub

#End Region

#Region " Autostart "

    Public Function isAutostart() As Boolean
        Dim arrAutostart As ArrayList = myAutostart.LoadList
        For Each oneFile As String In arrAutostart
            If oneFile.StartsWith(myFile.RemoveQuotationMarks(SetItem.Path.ToLower)) Then Return True
        Next
        Return False
    End Function

    Public Function isAutostart(ByVal filename As String) As Boolean
        Dim arrAutostart As ArrayList = myAutostart.LoadList
        For Each oneFile As String In arrAutostart
            If oneFile.StartsWith(myFile.RemoveQuotationMarks(filename.ToLower)) Then Return True
        Next
        Return False
    End Function

#End Region

End Class

#End Region

#Region " Item List "

Public Class clsBoxList
    Inherits ObservableCollection(Of clsBoxItem)

    Sub New(Items As Collection(Of clsSetting.clsItem))
        Me.Clear()
        Items.ToList.ForEach(Sub(x) Me.Add(New clsBoxItem(x)))
    End Sub

    Sub AddItem(Item As clsSetting.clsItem)
        If Not Item.Name = "" And Not Item.Path = "" Then
            If Exist(Item.Name, Item.Path) = False Then
                Dat.Items.Add(Item)
                Me.Add(New clsBoxItem(Item))
            End If
        End If
    End Sub

    Sub AddItems(type As BoxType, fileNames() As String, theme As String, Optional oHidestart As Boolean = False)
        For Each fileName As String In fileNames
            Dim Item As New clsSetting.clsItem
            Item.Column = theme
            Item.Path = fileName
            Item.HideStart = oHidestart

            If type = BoxType.Unknown Then
                If myFile.Exist(fileName) Then
                    Item.BoxType = BoxType.File
                Else
                    If myFolder.Exist(Item.Path) Then
                        Item.BoxType = BoxType.Folder
                    Else
                        Item.Path = myLink.AbsoluteUri(Item.Path)
                        If Not Item.Path = "" Then Item.BoxType = BoxType.Link
                    End If
                End If
            Else
                Item.BoxType = type
            End If

            Select Case Item.BoxType
                Case BoxType.File
                    Item.Name = myFile.Name(fileName, False)
                    If fileName.ToLower.EndsWith("lnk") Then ExtractLink(Item)
                    If fileName.ToLower.EndsWith("url") Then ExtractWeb(Item)
                Case BoxType.Folder
                    Item.Name = myFolder.Name(fileName)
                Case BoxType.Link
                    Item.Name = myLink.WebName(fileName)
            End Select

            AddItem(Item)
        Next
    End Sub

    Overloads Sub Insert(Index As Integer, BoxItem As clsBoxItem)
        If Index = Me.Items.Count Then
            Dat.Items.Add(BoxItem.Item)
        Else
            Dim DatIndex As Integer = Dat.Items.IndexOf(Me.Items(Index).Item)
            Dat.Items.Insert(DatIndex, BoxItem.Item)
        End If
        MyBase.Insert(Index, BoxItem)
    End Sub

    Overloads Sub RemoveAt(Index As Integer)
        Remove(Me.Items(Index))
    End Sub

    Overloads Sub Remove(BoxItem As clsBoxItem)
        Dim DatIndex As Integer = Dat.Items.IndexOf(BoxItem.Item)
        MyBase.Remove(BoxItem)
        Dat.Items.RemoveAt(DatIndex)
        Dat.HasChanges = True
    End Sub

    Public Function Exist(ByVal Name As String, ByVal Path As String) As Boolean
        For Each box In Me.Items
            If Name.ToLower = box.Name.ToLower Then Return True
            If Path.ToLower = box.Path.ToLower Then Return True
        Next
        Return False
    End Function

    Public Function Find(ByVal Name As String, ByVal Path As String) As clsBoxItem
        For Each box In Me.Items
            If Name.ToLower = box.Name.ToLower Then Return box
            If Path.ToLower = box.Path.ToLower Then Return box
        Next
        Return Nothing
    End Function

    Private Function ExtractLink(Item As clsSetting.clsItem) As clsSetting.clsItem
        Dim SS As New clsShortcut(Item.Path)
        If myFile.Exist(SS.Path) Then
            Item.BoxType = BoxType.File
            If SS.Arguments = "" Then
                Item.Path = SS.Path
            Else
                Item.Path = Chr(34) & SS.Path & Chr(34) & " " & SS.Arguments
            End If
        Else
            If myFolder.Exist(SS.Path) Then
                Item.BoxType = BoxType.Folder
                Item.Path = myFolder.RemoveLastSlash(SS.Path)
            Else
                If myLink.Exist(SS.Path) Then Item.BoxType = BoxType.Link
            End If
        End If
        Return Item
    End Function

    Private Function ExtractWeb(Item As clsSetting.clsItem) As clsSetting.clsItem
        Dim SouborINI As String = Item.Path
        Item.Path = myINI.GetSetting(SouborINI, "InternetShortcut", "URL", "")
        If Item.Path.ToLower.StartsWith("http") Then Item.BoxType = BoxType.Link
        Item.IconPath = myINI.GetSetting(SouborINI, "InternetShortcut", "IconFile", "")
        If Item.IconPath.ToLower.StartsWith("http") Then Item.IconPath = ""
        Return Item
    End Function

    Public Sub ActivateHotkey()
        For Each box As clsBoxItem In MyBase.Items
            box.ActivateHotkey()
        Next
    End Sub

End Class

#End Region

#Region " Column "

Public Class clsBoxColumn
    Private elem As UIElement
    Private cvs As New CollectionViewSource
    Private lbx As New ListBox
    Private SetColumn As clsSetting.clsColumn

#Region " Get/Set "

    Public ReadOnly Property Column As clsSetting.clsColumn
        Get
            Return SetColumn
        End Get
    End Property

    Public Property Sorted() As Boolean
        Get
            Return SetColumn.Sorted
        End Get
        Set(value As Boolean)
            SetColumn.Sorted = value
            Refresh()
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Hidden() As Boolean
        Get
            Return SetColumn.Hidden
        End Get
        Set(value As Boolean)
            SetColumn.Hidden = value
            Dat.HasChanges = True
        End Set
    End Property

    Public Property Theme() As String
        Get
            Return SetColumn.Name
        End Get
        Set(value As String)
            Dim Seznam As clsBoxList = CType(cvs.Source, clsBoxList)
            For Each box In Seznam
                If box.Theme = SetColumn.Name Then box.Theme = value
            Next
            SetColumn.Name = value
            lbx.Tag = value
            Dat.HasChanges = True
        End Set
    End Property

    Public Property ListBox() As ListBox
        Get
            Return lbx
        End Get
        Set(value As ListBox)
            lbx = value
        End Set
    End Property

    Public Property Element() As UIElement
        Get
            Return TryCast(lbx, UIElement)
        End Get
        Set(value As UIElement)
            elem = CType(value, UIElement)
        End Set
    End Property

    Public Property CollectionViewSource() As CollectionViewSource
        Get
            Return cvs
        End Get
        Set(value As CollectionViewSource)
            cvs = value
        End Set
    End Property
#End Region

    Sub New(Column As clsSetting.clsColumn, ByVal seznam As clsBoxList)
        SetColumn = Column

        cvs.Source = seznam
        Show(Dat.Options.ItemsHidden)

        lbx.Height = 300 : lbx.Width = ItemSize(Dat.Options.ItemsSize).Width : lbx.VerticalAlignment = VerticalAlignment.Top : lbx.HorizontalAlignment = HorizontalAlignment.Left : lbx.AllowDrop = True
        lbx.Style = DirectCast(Application.Current.FindResource("GlassListBox"), Style)
        lbx.ItemContainerStyle = DirectCast(Application.Current.FindResource("GlassListBoxItem"), Style)
        lbx.Background = myColorConverter.ColorToBrush(BoxColumnList.BackColor)
        lbx.Tag = Me.Theme
        lbx.ItemsSource = cvs.View

        AddHandler lbx.MouseDoubleClick, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_MouseDoubleClick
        AddHandler lbx.PreviewMouseLeftButtonUp, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_PreviewMouseLeftButtonUp
        AddHandler lbx.PreviewMouseRightButtonUp, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_PreviewMouseRightButtonUp
        AddHandler lbx.DragEnter, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_DragEnter
        AddHandler lbx.Drop, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_Drop
        AddHandler lbx.GiveFeedback, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_GiveFeedback
        AddHandler lbx.PreviewMouseLeftButtonDown, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_PreviewMouseLeftButtonDown
        AddHandler lbx.PreviewMouseMove, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_PreviewMouseMove
        AddHandler lbx.PreviewTouchDown, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_PreviewTouchDown
        AddHandler lbx.PreviewTouchMove, AddressOf DirectCast(Application.Current.MainWindow, wpfMain).lbxMain_PreviewTouchMove
        Dat.HasChanges = True
    End Sub

    Public Sub Refresh()
        If SetColumn.Sorted And cvs.View.SortDescriptions.Count = 0 Then '
            cvs.View.SortDescriptions.Add(New SortDescription("Name", ListSortDirection.Ascending))
        Else
            If cvs.View.SortDescriptions.Count > 0 Then cvs.View.SortDescriptions.RemoveAt(0)
        End If
        cvs.View.Refresh()
    End Sub

    Public Sub Show(ByVal Hidden As Boolean)
        If Hidden Then
            cvs.View.Filter = New Predicate(Of Object)(AddressOf FilterThemeHide)
        Else
            cvs.View.Filter = New Predicate(Of Object)(AddressOf FilterTheme)
        End If
        Refresh()
    End Sub

    Private Function FilterTheme(ByVal item As Object) As Boolean
        Dim bi As clsBoxItem = CType(item, clsBoxItem)
        Return If(bi.Theme = SetColumn.Name, True, False)
    End Function

    Private Function FilterThemeHide(ByVal item As Object) As Boolean
        Dim bi As clsBoxItem = CType(item, clsBoxItem)
        Return If(bi.Theme = SetColumn.Name And bi.Hidden = False, True, False)
    End Function

End Class

#End Region

#Region " Column List "

Public Class clsBoxColumnList
    Inherits Collection(Of clsBoxColumn)

    Private wMain As wpfMain = DirectCast(Application.Current.MainWindow, wpfMain)
    Public seznam As clsBoxList
    Public Sloupcu As Integer
    Private Radku As Integer

#Region " Aktualizace vzhledu"

    Public BackColor As Color = myWinColor.Background

    Public Sub UpdateLook()
        Dim mColor As Color = myWinColor.UpdateColors()

        If Not mColor = BackColor Then
            BackColor = mColor
            For Each column In Me.Items
                column.ListBox.Background = New SolidColorBrush(BackColor)
            Next
            For Each box In seznam
                box.BackColor = myWinColor.Selection
            Next

            'wMain.cMenu.Background = New SolidColorBrush(myWinColor.Selection)
        End If

        If seznam(0).ItemLook <> Dat.Options.ItemLook Then
            For Each box In seznam
                box.ItemLook = Dat.Options.ItemLook
            Next
        End If
    End Sub

#End Region

#Region " Bin/Koš "

    Private Bin As New clsRecycleBin
    Private WithEvents timBin As Threading.DispatcherTimer
    Public BinIsEmpty As Boolean

    Public Sub BinEmpty()
        Bin.Empty(True)
        BinStateChanged(True)
    End Sub

    Private Sub timBin_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timBin.Tick
        If IsNothing(seznam) Then Exit Sub
        Dim binItemBox As clsBoxItem = seznam.Find("", "START_bin") 'nutná kontrola, aby update byl jen v případě, že je koš používán
        If binItemBox IsNot Nothing Then
            Dim bBinEmpty As Boolean = Bin.IsEmpty()
            If Not bBinEmpty = BinIsEmpty Then
                BinStateChanged(BinIsEmpty)
            End If
        End If
    End Sub

    Private Sub BinStateChanged(Empty As Boolean)
        BinIsEmpty = Empty
        If IsNothing(seznam) Then Exit Sub
        Dim binItemBox As clsBoxItem = seznam.Find("", "START_bin")
        If binItemBox IsNot Nothing Then
            Dim binUri As New Uri(imgCestaA + If(Empty, "folder/RecycleBin_empty128.png", "folder/RecycleBin_full128.png"))
            binItemBox.ImgSource = New BitmapImage(binUri)
            binItemBox.Cursor = myBitmap.ToCursor(binUri)
        End If
    End Sub

#End Region

#Region " Get/Set "

    Public Property BoxList() As clsBoxList
        Get
            Return seznam
        End Get
        Set(ByVal value As clsBoxList)
            seznam = value
        End Set
    End Property

#End Region

#Region " Load Resize "

    Public Sub Load(Columns As Collection(Of clsSetting.clsColumn)) 'Show
        Columns.ToList.ForEach(Sub(x) Me.Add(New clsBoxColumn(x, seznam)))
        ShowColumns()
        CheckAllUnins()
    End Sub

    Public Sub Refresh()
        For Each column In MyBase.Items
            column.Refresh()
        Next
    End Sub

    Public Sub Show(ByVal Hidden As Boolean)
        If HiddenAll() Then
            Dat.Options.ItemsHidden = False
            Hidden = False
        End If
        For Each column In MyBase.Items
            column.Show(Hidden)
        Next
        ShowColumns()
    End Sub

    Private Sub ShowColumns()
        Sloupcu = 0
        wMain.panStack.Children.Clear()
        For Each column In MyBase.Items
            If Dat.Options.ItemsHidden Then
                If column.Hidden = False Then
                    wMain.panStack.Children.Add(column.Element)
                    Sloupcu += 1
                End If
            Else
                wMain.panStack.Children.Add(column.Element)
                Sloupcu += 1
            End If
        Next
        Resize()
    End Sub

    Public Sub Resize()
        If wMain.Left < 0 Then wMain.Left = If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Left, 60, 1)
        If wMain.Top < 0 Then wMain.Top = If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Top, 40, 1)
        Radku = CountRows()

        Dim Obrazovka As Point = New Point(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight)
        wMain.Width = Sloupcu * ItemSize(Dat.Options.ItemsSize).Width
        If wMain.Width > Obrazovka.X Then
            wMain.Left = If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Left, 60, 1)
        Else
            If wMain.Left + wMain.ActualWidth > Obrazovka.X Then
                wMain.Left = Obrazovka.X - wMain.ActualWidth - If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Right, 60, 0)
            End If
        End If

        If (Radku * (ItemSize(Dat.Options.ItemsSize).Height + 4)) + 30 > Obrazovka.Y Then
            Radku = CInt((Obrazovka.Y - 30) / (ItemSize(Dat.Options.ItemsSize).Height + 4))
            wMain.Top = If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Top, 40, 1)
            wMain.Height = (Radku * (ItemSize(Dat.Options.ItemsSize).Height + 4)) + 34
        Else
            wMain.Height = (Radku * (ItemSize(Dat.Options.ItemsSize).Height + 4)) + 34
            If wMain.Top + wMain.ActualHeight > Obrazovka.Y Then
                wMain.Top = Obrazovka.Y - wMain.ActualHeight - If(mySystem.GetTaskbarLocation = clsSystem.TaskbarLocation.Bottom, 40, 0)
            End If
        End If

        For Each column In MyBase.Items
            column.ListBox.Width = ItemSize(Dat.Options.ItemsSize).Width
            column.ListBox.Height = wMain.Height
        Next
    End Sub

    Public Sub ResizeItems()
        For Each box In seznam
            box.ItemHeight = ItemSize(Dat.Options.ItemsSize).Height
            box.ItemWidth = ItemSize(Dat.Options.ItemsSize).Width
        Next
        Resize()
    End Sub

    Private Function CountRows() As Integer
        Dim Radek As Integer = 0
        For Each column In MyBase.Items
            If Dat.Options.ItemsHidden = True And column.Hidden = True Then
            Else
                If Radek < column.ListBox.Items.Count Then Radek = column.ListBox.Items.Count
            End If
        Next
        Return Radek
    End Function

    Public Function RowsCountChanged() As Boolean
        Return If(CountRows() <> Radku, True, False)
    End Function

#End Region

#Region " New Add Insert Remove "

    Sub New(Items As Collection(Of clsSetting.clsItem)) 'Prepare
        Me.Clear()
        seznam = New clsBoxList(Items)

        BinStateChanged(Bin.IsEmpty)
        timBin = New Threading.DispatcherTimer
        timBin.Interval = TimeSpan.FromMinutes(5)
        timBin.IsEnabled = True
    End Sub

    Public Sub AddColumn(Optional Name As String = "")
        Dim Column As New clsSetting.clsColumn
        If Name = "" Then
            Dim num As Integer
            Do
                num += 1
                Name = "Sloupec " + num.ToString
            Loop While Exist(Name)
        Else
            If Exist(Name) Then Exit Sub
        End If
        Column.Name = Name
        Dat.Columns.Add(Column)
        Me.Add(New clsBoxColumn(Column, seznam))
        ShowColumns()
    End Sub

    Public Sub InsertBoxItem(dragBoxItem As clsBoxItem, dropBoxItem As clsBoxItem)
        If seznam.IndexOf(dragBoxItem) = seznam.IndexOf(dropBoxItem) Then Exit Sub

        Dim sDragTheme As String = dragBoxItem.Theme
        Dim sDropTheme As String = dropBoxItem.Theme
        dragBoxItem.Theme = sDropTheme

        seznam.Remove(dragBoxItem)
        seznam.Insert(seznam.IndexOf(dropBoxItem), dragBoxItem)
        If Not CountRows() = Radku Then Resize()
    End Sub

    Public Sub InsertBoxItem(dragBoxItem As clsBoxItem, dropBoxColumn As clsBoxColumn)
        Dim sDragTheme As String = dragBoxItem.Theme
        dragBoxItem.Theme = dropBoxColumn.Theme

        seznam.Remove(dragBoxItem)
        Dat.Items.Add(dragBoxItem.Item)
        seznam.Add(dragBoxItem)
        If Not CountRows() = Radku Then Resize()
    End Sub

    Public Sub InsertBoxColumn(dragBoxColumn As clsBoxColumn, dropBoxColumn As clsBoxColumn)
        Dim iDragIndex As Integer = Me.IndexOf(dragBoxColumn)
        Dim iDropIndex As Integer = Me.IndexOf(dropBoxColumn)
        Me.Remove(dragBoxColumn)
        Me.Insert(iDropIndex, dragBoxColumn)
    End Sub

    Public Sub AddItem(fileName As String, theme As String, Optional type As BoxType = BoxType.Unknown, Optional oHidestart As Boolean = False)
        Mouse.SetCursor(Cursors.Wait)
        Dim fileNames(0) As String
        fileNames(0) = fileName
        seznam.AddItems(type, fileNames, theme, oHidestart)

        If Not CountRows() = Radku Then Resize()
        If Not type = BoxType.Link Then
            myAutostart.LoadList(True)
            CheckAllUnins()
        End If
        Mouse.SetCursor(Cursors.Arrow)
    End Sub

    Public Sub AddItems(ByVal fileNames() As String, ByVal theme As String, Optional type As BoxType = BoxType.Unknown)
        Mouse.SetCursor(Cursors.Wait)
        seznam.AddItems(type, fileNames, theme)

        If Not CountRows() = Radku Then Resize()
        If Not type = BoxType.Link Then
            myAutostart.LoadList(True)
            CheckAllUnins()
        End If
        Mouse.SetCursor(Cursors.Arrow)
    End Sub

    Public Sub AddItems(ByVal arrFiles As ArrayList, ByVal theme As String, Optional type As BoxType = BoxType.Unknown)
        Mouse.SetCursor(Cursors.Wait)
        Dim fileNames(arrFiles.Count - 1) As String
        arrFiles.CopyTo(fileNames, 0)
        seznam.AddItems(type, fileNames, theme)
        If Not CountRows() = Radku Then Resize()
        myAutostart.LoadList(True)
        CheckAllUnins()
        Mouse.SetCursor(Cursors.Arrow)
    End Sub

    Public Sub AddItem(Item As clsSetting.clsItem)
        seznam.AddItem(Item)
        If Not CountRows() = Radku Then Resize()
    End Sub

    Public Function Exist(Theme As String) As Boolean
        Return Me.Items.Any(Function(x) x.Theme.ToLower = Theme.ToLower)
    End Function

    Private Function GetBoxColumn(ByVal Theme As String) As clsBoxColumn
        Return Me.Items.FirstOrDefault(Function(x) x.Theme = Theme)
    End Function

    Overloads Sub Insert(Index As Integer, BoxColumn As clsBoxColumn)
        If Index = Me.Items.Count Then
            Dat.Columns.Add(BoxColumn.Column)
        Else
            Dim DatIndex As Integer = Dat.Columns.IndexOf(Me.Items(Index).Column)
            Dat.Columns.Insert(DatIndex, BoxColumn.Column)
        End If
        MyBase.Insert(Index, BoxColumn)
    End Sub

    Overloads Sub RemoveAt(Index As Integer)
        Remove(Me.Items(Index))
    End Sub

    Overloads Sub Remove(BoxColumn As clsBoxColumn)
        Dim DatIndex As Integer = Dat.Columns.IndexOf(BoxColumn.Column)
        MyBase.Remove(BoxColumn)
        Dat.Columns.RemoveAt(DatIndex)
        Dat.HasChanges = True
    End Sub

#End Region

#Region " Uninstall "

    Public Sub CheckAllUnins()
        Dim mThread As New System.Threading.ThreadStart(AddressOf CheckUninsFolder)
        Dim oThread As New System.Threading.Thread(mThread)
        oThread.Start()
    End Sub

    Private Sub CheckUninsFolder()
        For Each oneBoxItem In seznam
            If oneBoxItem.Type = BoxType.File Or oneBoxItem.Path = "START_setting" Then
                Dim Filepath As String
                If oneBoxItem.Path = "START_setting" Then
                    Filepath = appCesta & "\STARTzjs.exe"
                Else
                    Filepath = myFile.RemoveQuotationMarks(oneBoxItem.Path)
                End If
                If Filepath Like "*.exe" Then
                    oneBoxItem.Uninstall = ""
                    For Each oneFile As String In myFolder.Files(myFolder.Path(Filepath), "*.exe")
                        If oneFile.ToLower.Contains("unins") Or oneFile.ToLower.StartsWith("inst") Then
                            oneBoxItem.Uninstall = oneFile
                        End If
                    Next
                End If
            End If
        Next
        CheckUninsRegister("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall") 'x64
        CheckUninsRegister("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall") 'x32
    End Sub

    Private Sub CheckUninsRegister(RegPath As String)
        For iRoot = 2 To 4 Step 2
            Dim RegKeys() As String = myRegister.QueryKeys(CType(iRoot, HKEY), RegPath)
            For Each oneKey As String In RegKeys
                Dim insLocation As String = myFile.RemoveQuotationMarks(myRegister.GetValue(CType(iRoot, HKEY), RegPath & "\" & oneKey, "InstallLocation", ""))
                If Not insLocation = "" Then
                    For Each oneBoxItem In seznam
                        If oneBoxItem.Type = BoxType.File Or oneBoxItem.Path = "START_setting" Then
                            Dim Filepath As String
                            If oneBoxItem.Path = "START_setting" Then
                                Filepath = appCesta & "\STARTzjs.exe"
                            Else
                                Filepath = oneBoxItem.Path
                            End If
                            If Filepath.ToLower.Contains(insLocation.ToLower) Then
                                Dim UninstallString As String = myRegister.GetValue(CType(iRoot, HKEY), RegPath & "\" & oneKey, "UninstallString", "")
                                If oneBoxItem.Uninstall = "" And Not UninstallString = "" Then oneBoxItem.Uninstall = UninstallString
                            End If
                        End If
                    Next
                End If
            Next
        Next
    End Sub

#End Region

#Region " Hidden and Blinking "

    Public Function HiddenAll() As Boolean
        For Each column In Me.Items
            If column.Hidden = False Then Return False
        Next
        Return True
    End Function

    Public Function IsAlarmBlinkOn() As Boolean
        Dim box As clsBoxItem = BoxColumnList.BoxList.Find("", "START_alarm")
        Return box.Highlighted
    End Function
    Public Sub AlarmBlinking(OnOff As Boolean)
        Dim box As clsBoxItem = BoxColumnList.BoxList.Find("", "START_alarm")
        If IsNothing(box) = False Then
            If OnOff Then OnOff = Not box.Highlighted
            box.Highlighted = OnOff
        End If
    End Sub

#End Region

End Class

#End Region

