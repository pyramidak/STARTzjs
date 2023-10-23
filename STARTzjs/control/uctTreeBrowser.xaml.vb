Imports System.ComponentModel

Public Class uctTreeBrowser

#Region " Properties "

    Private imgCesta As String
    Public Property SelectedPath As String = ""
    Public Property MainWindow As Window
    Public Property SystemFolders As Boolean = True
    Public Property UserFolders As Boolean = True

    Public Property BackColor As Brush
        Get
            Return mTreeView.Background
        End Get
        Set(value As Brush)
            mTreeView.Background = value
        End Set
    End Property

    Enum DiskTypes
        Floppy = 1
        Removable = 2
        Fixed = 3
        Remote = 4
        Cdrom = 5
        Ramdisk = 6
    End Enum

    Class clsFolder
        Public Property Icon As ImageSource
        Public Property Type As DiskTypes
        Public Property Name As String
        Public Property FullName As String
        Public NewOne As Boolean
        Public Loaded As Boolean
    End Class

    Private Function NewItem(Folder As clsFolder) As TreeViewItem
        Dim img As New Image()
        img.Source = Folder.Icon
        img.Height = 20
        img.Margin = New Thickness(0, 0, 5, 0)
        Dim txt As New TextBlock
        txt.Text = Folder.Name
        Dim panel As New StackPanel
        panel.Orientation = Orientation.Horizontal
        panel.Margin = New Thickness(2)
        panel.Children.Add(img)
        panel.Children.Add(txt)
        Dim Item As New TreeViewItem
        Item.Header = panel
        Item.Tag = Folder
        Return Item
    End Function

    Private Sub AddImagesToContextMenu() ' Error in xaml
        Dim img As New Image
        img.Source = CType(Me.FindResource("imgPridat"), ImageSource)
        img.Height = 16
        CType(mTreeView.ContextMenu.Items(0), MenuItem).Icon = img
        img = New Image
        img.Source = CType(Me.FindResource("imgPrejmenovat"), ImageSource)
        img.Height = 16
        CType(mTreeView.ContextMenu.Items(1), MenuItem).Icon = img
        img = New Image
        img.Source = CType(Me.FindResource("imgOdebrat"), ImageSource)
        img.Height = 16
        CType(mTreeView.ContextMenu.Items(2), MenuItem).Icon = img
    End Sub

#End Region

    Private Sub wpfDialogFolder_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        imgCesta = "/" + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName + ";component/Images/folder/"
        AddImagesToContextMenu()
    End Sub

    Public Sub Reload()
        lblLoading.Visibility = Visibility.Visible
        mTreeView.Items.Clear()
        LoadDisks()
        If UserFolders Then LoadUserFolder()
        SelectItemByFullName(SelectedPath)
        mTreeView.Focus()
        lblLoading.Visibility = Visibility.Hidden
    End Sub

    Private Sub mTreeView_SelectedItemChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object)) Handles mTreeView.SelectedItemChanged
        Dim Item As TreeViewItem = TryCast(e.NewValue, TreeViewItem)
        If Item IsNot Nothing Then
            SelectedPath = CType(Item.Tag, clsFolder).FullName
        End If
    End Sub

    Private Sub TreeViewItem_Expanded(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(e.OriginalSource, TreeViewItem)
        If Item IsNot Nothing Then LoadSubFoldersByParent(Item)
    End Sub

    Private Sub LoadSubFoldersByParent(Item As TreeViewItem)
        For Each SubItem As TreeViewItem In Item.Items
            LoadSubFolders(SubItem)
        Next
    End Sub

#Region " Select Path "

    Private Sub SelectItemByFullName(FullName As String)
        If SelectedPath = "" Then Exit Sub
        For Each one As TreeViewItem In mTreeView.Items
            If CheckItem(one) Then Exit Sub
        Next
    End Sub

    Private Function CheckItem(Item As TreeViewItem) As Boolean
        For Each one As TreeViewItem In Item.Items
            If CType(one.Tag, clsFolder).FullName = SelectedPath Then
                one.IsExpanded = True
                one.IsSelected = True
                one.BringIntoView()
                LoadSubFoldersByParent(one)
                Return True
            End If
        Next
        Return False
    End Function

#End Region

#Region " Load UserFolder "

    Private Sub LoadUserFolder()
        Dim Folder As New clsFolder
        Folder.FullName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        Folder.Name = myFolder.Name(Folder.FullName)
        Folder.Type = DiskTypes.Fixed
        Folder.Icon = New BitmapImage(New Uri(imgCesta + "User128.png", UriKind.Relative))
        Dim Item As TreeViewItem = NewItem(Folder)
        mTreeView.Items.Add(Item)
        Item.ExpandSubtree()
        LoadSubFolders(Item)
        For Each SubItem As TreeViewItem In Item.Items
            LoadSubFolders(SubItem)
        Next
    End Sub
#End Region

#Region " Load SubFolders "

    Private Function LoadSubFolders(ByVal Parent As TreeViewItem) As Boolean
        Dim Folder As clsFolder = CType(Parent.Tag, clsFolder)
        If Folder.Loaded Then Return False
        Folder.Loaded = True
        Try
            Dim Dir As New IO.DirectoryInfo(Folder.FullName)
            If Not Dir.GetDirectories.Length = 0 Then
                For Each oneDir In Dir.GetDirectories.OrderBy(Function(x) x.Name)
                    If myFolder.Exist(oneDir.FullName, False, SystemFolders) Then
                        Dim newFolder As New clsFolder
                        newFolder.Name = oneDir.Name
                        newFolder.FullName = oneDir.FullName
                        newFolder.Type = Folder.Type
                        newFolder.Icon = GetFolderIcon(oneDir.FullName)
                        Dim Item As TreeViewItem = NewItem(newFolder)
                        Parent.Items.Add(Item)
                    End If
                Next

            End If
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

#End Region

#Region " Load Disks "

    Public Sub LoadDisks()
        For Each Drive As String In System.IO.Directory.GetLogicalDrives
            Dim DiskInfo As New System.IO.DriveInfo(Drive)
            If DiskInfo.IsReady Then
                Dim Disk As New clsFolder
                Disk.Type = CType(DiskInfo.DriveType, DiskTypes)
                Dim Number As String = myFolder.VolumeSerialNumber(Drive)
                If Number.Length = 8 AndAlso Number.Substring(0, 5) <> "00000" Then
                    Disk.Name = DiskInfo.VolumeLabel
                    Disk.FullName = Drive
                    Select Case Disk.Type '0=unknown,1=norootdir/floppy,2=removable,3=fixed,4=remote,5=cdrom,6=ramdisk
                        Case DiskTypes.Floppy
                            If Disk.Name = "" Then Disk.Name = "Disketa"
                            Disk.Icon = CType(Me.FindResource("disketa"), ImageSource)
                        Case DiskTypes.Removable
                            If Disk.Name = "" Then Disk.Name = "Vyměnitelný disk"
                            Disk.Icon = CType(Me.FindResource("flashdisk"), ImageSource)
                        Case DiskTypes.Fixed
                            If Disk.Name = "" Then Disk.Name = "Místní disk"
                            Disk.Icon = CType(Me.FindResource("harddisk"), ImageSource)
                        Case DiskTypes.Remote
                            If Disk.Name = "" Then Disk.Name = "Vzdálený disk"
                            Disk.Icon = CType(Me.FindResource("server"), ImageSource)
                        Case DiskTypes.Cdrom
                            If Disk.Name = "" Then Disk.Name = "Optický disk"
                            Disk.Icon = CType(Me.FindResource("cdrom"), ImageSource)
                    End Select
                    Disk.Name += " (" + Drive.Substring(0, 1) + ":) "
                    Select Case CDbl(DiskInfo.TotalSize)
                        Case Is > 10 ^ 12
                            Disk.Name += (DiskInfo.TotalSize / 1024 / 1024 / 1024 / 1024).ToString("N2") + " TB"
                        Case Is > 10 ^ 9
                            Disk.Name += (DiskInfo.TotalSize / 1024 / 1024 / 1024).ToString("N1") + " GB"
                        Case Is > 10 ^ 6
                            Disk.Name += (DiskInfo.TotalSize / 1024 / 1024).ToString("N0") + " MB"
                    End Select
                    If Disk.Type = DiskTypes.Fixed Or Disk.Type = DiskTypes.Remote Or Disk.Type = DiskTypes.Removable Or Disk.Type = DiskTypes.Ramdisk Then
                        Dim Item As TreeViewItem = NewItem(Disk)
                        mTreeView.Items.Add(Item)
                        LoadSubFolders(Item)
                    End If
                End If
            End If
        Next
    End Sub

#End Region

#Region " Get Icon Folder "

    Private Function GetFolderIcon(FullName As String) As ImageSource
        Dim sINIpath As String = myFile.Join(FullName, "desktop.ini")
        If myFile.Exist(sINIpath) Then
            Dim Result As String = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconResource", "")
            If Result = "" Then Result = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconFile", "")
            If Not Result = "" Then
                Dim sIcon As String
                Dim iIconIndex As Integer = 0
                If Result.Substring(0, 2) = ".." Then Return New BitmapImage(New Uri(imgCesta + "Folder128.png", UriKind.Relative))
                If Result.Contains(",") Then
                    Dim LastIndex As Integer = Result.LastIndexOf(",")
                    sIcon = Result.Substring(0, LastIndex)
                    iIconIndex = CInt(Result.Substring(LastIndex + 1, Result.Length - LastIndex - 1))
                Else
                    sIcon = Result
                    iIconIndex = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconIndex", 0)
                End If
                If sIcon.StartsWith("%SystemRoot%") Then
                    sIcon = sIcon.Replace("%SystemRoot%", Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                End If
                If sIcon.Length > 2 AndAlso Not sIcon.Substring(1, 1) = ":" Then
                    sIcon = myFile.Join(FullName, sIcon)
                End If
                If myFile.Exist(sIcon) Then
                    Dim IconExtractor As New clsExtractIcon(sIcon)
                    IconExtractor.ChangeIndexIconInFile(iIconIndex)
                    Dim ImgSource = IconExtractor.GetImageSource
                    If ImgSource IsNot Nothing Then Return ImgSource
                End If
            End If
        End If
        Return New BitmapImage(New Uri(imgCesta + "Folder128.png", UriKind.Relative))
    End Function

#End Region


    Private Lge As Boolean = mySystem.LgeCzech

#Region " Přidat "
    Private Sub Pridat_click(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(mTreeView.SelectedItem, TreeViewItem)
        If Item IsNot Nothing Then
            Dim InputBox As New wpfDialog(MainWindow, If(Lge, "Vložte jméno nového adresáře", "Enter name of new dir"), If(Lge, "Nová složka", "New folder"), wpfDialog.Ikona.tuzka, "OK", If(Lge, "Zrušit", "Cancel"), True, False, "", False, 99, True)
            If InputBox.ShowDialog() Then
                Dim Folder As clsFolder = CType(Item.Tag, clsFolder)
                Dim NewFullName As String = myFolder.Join(Folder.FullName, InputBox.Input)
                If myFolder.Exist(NewFullName) Then
                    Item.IsExpanded = True
                Else
                    If myFolder.Create(NewFullName) Then
                        Dim newFolder As New clsFolder
                        newFolder.Name = InputBox.Input
                        newFolder.FullName = NewFullName
                        newFolder.Type = Folder.Type
                        newFolder.Icon = GetFolderIcon(NewFullName)
                        newFolder.NewOne = True
                        Dim addItem As TreeViewItem = NewItem(newFolder)
                        Item.Items.Add(addItem)
                        addItem.IsSelected = True
                        addItem.BringIntoView()
                    End If
                End If
            End If
        End If
    End Sub
#End Region

#Region " Přejmenovat "
    Private Sub Prejmenovat_click(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(mTreeView.SelectedItem, TreeViewItem)
        If Item IsNot Nothing Then
            Dim Folder As clsFolder = CType(Item.Tag, clsFolder)
            Dim InputBox As New wpfDialog(MainWindow, If(Lge, "Vložte jméno nového adresáře", "Enter name of new dir"), If(Lge, "Rename Folder", ""), wpfDialog.Ikona.tuzka, "OK", If(Lge, "Nová složka", "New folder"), True, False, "", False, 99, True)
            InputBox.Input = Folder.Name
            If InputBox.ShowDialog() Then
                Dim NewFullName As String = myFolder.Join(myFolder.Path(Folder.FullName), InputBox.Input)
                If myFolder.Exist(NewFullName) Then
                    Item.IsExpanded = True
                Else
                    If myFolder.Rename(Folder.FullName, NewFullName) Then
                        Folder.Name = InputBox.Input
                        Folder.FullName = NewFullName
                        Dim panel As StackPanel = CType(Item.Header, StackPanel)
                        Dim txt As TextBlock = CType(panel.Children(1), TextBlock)
                        txt.Text = Folder.Name
                        SelectedPath = NewFullName
                    End If
                End If
            End If
        End If
    End Sub
#End Region

#Region " Odebrat "
    Private Sub Odebrat_click(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(mTreeView.SelectedItem, TreeViewItem)
        If Item IsNot Nothing Then
            If myFolder.Delete(SelectedPath, False) Then
                Dim Parent As TreeViewItem = CType(Item.Parent, TreeViewItem)
                If Parent IsNot Nothing Then
                    Parent.IsSelected = True
                    Parent.Items.Remove(Item)
                End If
            End If
        End If
    End Sub

    Private Sub mTreeView_ContextMenuOpening(sender As Object, e As ContextMenuEventArgs) Handles mTreeView.ContextMenuOpening
        mTreeView.ContextMenu.IsEnabled = If(mTreeView.SelectedItem Is Nothing, False, True)
        If mTreeView.SelectedItem IsNot Nothing Then
            Dim OK As Boolean = CType(CType(mTreeView.SelectedItem, TreeViewItem).Tag, clsFolder).NewOne
            CType(mTreeView.ContextMenu.Items(1), MenuItem).IsEnabled = OK
            CType(mTreeView.ContextMenu.Items(2), MenuItem).IsEnabled = OK
        End If
    End Sub
#End Region










End Class
