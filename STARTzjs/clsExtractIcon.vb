Imports System.Drawing
Imports Microsoft.Win32.SafeHandles
Imports System.Runtime.InteropServices

Public Class clsExtractIcon

#Region " Declaration "

    <DllImport("shell32.dll", CharSet:=CharSet.Auto)> _
    Shared Function ExtractIcon(ByVal HINSTANCE As IntPtr, ByVal szFileName As String, ByVal nIconIndex As Integer) As IntPtr
    End Function

    <DllImport("shell32.dll", CharSet:=CharSet.Auto)> _
    Shared Function ExtractIconEx(ByVal szFileName As String, _
        ByVal nIconIndex As Integer, _
        ByVal phiconLarge() As IntPtr, _
        ByVal phiconSmall() As IntPtr, _
        ByVal nIcons As Integer) As Integer
    End Function

    <DllImport("User32.dll", CharSet:=CharSet.Auto)> _
    Shared Function PrivateExtractIcons(ByVal sFileName As String, _
        ByVal iIndex As Integer, _
        ByVal iWidth As Integer, _
        ByVal iHeight As Integer, _
        ByVal hIcon() As IntPtr, _
        ByVal iIconID() As Integer, _
        ByVal iNumber As Integer, _
        ByVal iFlag As Integer) As Integer
    End Function

    Private sFileName As String
    Private iIndexIconInFile As Integer = 0
    Private clsIconFile As IconFile
    Private clsIconExeDll As IconExeDll
    Private ExeDllIcons() As Icon
    Private VaznaChyba As Boolean = False
    Private Soubor As FileType

    Public Property FileName() As String
        Get
            Return sFileName
        End Get
        Set(value As String)
            sFileName = value
            proCreateClasses()
        End Set
    End Property

    Public ReadOnly Property Chyba() As Boolean
        Get
            Return VaznaChyba
        End Get
    End Property

    Public ReadOnly Property IndexIconInFile() As Integer
        Get
            Return iIndexIconInFile
        End Get
    End Property

    Public ReadOnly Property CountIconsInIcon() As Integer
        Get
            If Soubor = FileType.Exe Then
                Return ExeDllIcons.Length
            ElseIf Soubor = FileType.Icon Then
                Return clsIconFile.Entries.Count
            Else
                Return 1
            End If
        End Get
    End Property

    Public ReadOnly Property CountIconsInFile() As Integer
        Get
            If Soubor = FileType.Exe Then
                Return clsIconExeDll.IconCount()
            Else
                Return 0
            End If
        End Get
    End Property

    Enum FileType As Short
        Icon = 1
        Exe = 2
        Associated = 3
        Dll = 4
    End Enum

#End Region

#Region " Public Procedures "

    Sub New()
    End Sub

    Sub New(ByVal FileName As String)
        sFileName = RemoveQuotationMarks(FileName)
        proCreateClasses()
    End Sub

    Protected Overrides Sub Finalize()
        Try
            Dispose()
        Finally
            MyBase.Finalize()
        End Try
    End Sub

    Public Sub Dispose()
        If IsNothing(clsIconExeDll) = False Then
            clsIconExeDll.Dispose()
            clsIconExeDll.DisposeAll()
        End If
    End Sub

    Public Function GetImageSource() As ImageSource
        Return IconToImageSource(GetIcon())
    End Function

    Public Function GetImageSource(ByVal Velikost As Integer, ByVal Stretch As Boolean) As ImageSource
        Return IconToImageSource(GetIcon(Velikost, Stretch))
    End Function

    Public Function GetImageSource(ByVal index As Integer) As ImageSource
        Return IconToImageSource(GetIcon(index))
    End Function

    Public Function GetCursor() As Cursor
        Return IconToCursor(GetIcon())
    End Function

    Public Function GetCursor(ByVal index As Integer) As Cursor
        Return IconToCursor(GetIcon(index))
    End Function

    Public Function GetCursor(ByVal Velikost As Integer, ByVal Stretch As Boolean) As Cursor
        Return IconToCursor(GetIcon(Velikost, Stretch))
    End Function

    Public Function GetIconAsBitmap() As Bitmap
        Return GetIcon().ToBitmap
    End Function

    Public Function GetIconAsBitmap(ByVal Index As Integer) As Bitmap
        Return GetIcon(Index).ToBitmap
    End Function

    Public Function GetIconAsBitmap(ByVal Velikost As Integer, ByVal Stretch As Boolean) As Bitmap
        Return GetIcon(Velikost, Stretch).ToBitmap
    End Function

    Public Function ChangeIndexIconInFile(ByVal Index As Integer) As Boolean
        If VaznaChyba Then Return False

        If Soubor = FileType.Exe Then
            If Not Index > clsIconExeDll.IconCount() - 1 Then
                If iIndexIconInFile <> Index Then
                    Try
                        ExeDllIcons = IconExeDll.SplitIcon(clsIconExeDll.GetIcon(Index))
                    Catch
                        Return False
                    End Try
                End If
                iIndexIconInFile = Index
                Return True
            End If
        ElseIf Soubor = FileType.Dll Then
            iIndexIconInFile = Index
        End If
        Return False
    End Function

    Public Function GetIcon() As Icon
        If VaznaChyba Then Return Icon.ExtractAssociatedIcon(FileName)

        Select Case Soubor
            Case FileType.Icon
                Dim oneIcon As Icon = Nothing
                Dim MaxSize As Integer
                Dim a As Integer = 0
                For a = 0 To clsIconFile.Entries.Count - 1
                    If MaxSize < clsIconFile.Entries(a).Icon.Width Then MaxSize = clsIconFile.Entries(a).Icon.Width
                Next
                For a = 0 To clsIconFile.Entries.Count - 1
                    If clsIconFile.Entries(a).Icon.Width = MaxSize Then Exit For
                Next
                Dim bmp As Bitmap = clsIconFile.Entries(a).Icon
                Return Icon.FromHandle(DirectCast(bmp, Bitmap).GetHicon)

            Case FileType.Exe
                Dim MaxSize As Integer
                Dim iIndex As Integer = -1
                Dim iUse As Integer
                For Each oneIcon As Icon In ExeDllIcons
                    iIndex += 1
                    If MaxSize <= oneIcon.Size.Width Then MaxSize = oneIcon.Size.Width : iUse = iIndex
                Next
                Return ExeDllIcons.ElementAt(iUse)

            Case FileType.Dll
                Return GetIcon(iIndexIconInFile)

            Case Else
                Return Icon.ExtractAssociatedIcon(FileName)

        End Select
    End Function

    Public Function GetIcon(ByVal Index As Integer) As Icon
        If VaznaChyba Then Return Icon.ExtractAssociatedIcon(FileName)

        Select Case Soubor
            Case FileType.Icon
                If Index > clsIconFile.Entries.Count - 1 Then Index = 0
                Dim bmp As Bitmap = clsIconFile.Entries(Index).Icon
                Return Icon.FromHandle(DirectCast(bmp, Bitmap).GetHicon)

            Case FileType.Exe, FileType.Dll
                If ExeDllIcons Is Nothing Then
                    Dim hIcons As IntPtr() = New IntPtr(0) {IntPtr.Zero}
                    Dim iIconsID() As Integer = New Integer(0) {}
                    PrivateExtractIcons(FileName, Index, 96, 96, hIcons, iIconsID, 1, 0)
                    Try
                        Return Icon.FromHandle(hIcons(0))
                    Catch ex As Exception
                        Return Nothing
                    End Try
                Else
                    If Index > ExeDllIcons.Length - 1 Then Index = 0
                    Return ExeDllIcons(Index)
                End If

            Case Else
                Return Icon.ExtractAssociatedIcon(FileName)

        End Select
    End Function

    Public Function GetIcon(ByVal Velikost As Integer, ByVal Stretch As Boolean) As Icon
        If VaznaChyba Then Return EnlargeIcon(Velikost, Stretch, Icon.ExtractAssociatedIcon(FileName))

        Select Case Soubor
            Case FileType.Icon
                'Format32bppArgb Format24bppRgb Format4bppIndexed Format8bppIndexed
                Dim bmp As Bitmap = clsIconFile.GetIcon(New Size(Velikost, Velikost), Imaging.PixelFormat.Format32bppArgb).Icon
                Return EnlargeIcon(Velikost, Stretch, Icon.FromHandle(DirectCast(bmp, Bitmap).GetHicon))

            Case FileType.Exe
                Dim TrySize As Integer = Velikost
                Do
                    Dim IconVelikost As Icon = Nothing
                    For Each oneIcon In ExeDllIcons
                        If oneIcon.Size.Width = TrySize Then IconVelikost = oneIcon
                    Next
                    If IsNothing(IconVelikost) = False Then Return EnlargeIcon(Velikost, Stretch, IconVelikost)
                    TrySize = TrySize - 16
                Loop Until TrySize = 0
                Return EnlargeIcon(Velikost, Stretch, ExeDllIcons(0))

            Case FileType.Dll
                Dim hIcons As IntPtr() = New IntPtr(0) {IntPtr.Zero}
                Dim iIconsID() As Integer = New Integer(0) {}
                PrivateExtractIcons(FileName, iIndexIconInFile, Velikost, Velikost, hIcons, iIconsID, 1, 0)
                Return Icon.FromHandle(hIcons(0))

            Case Else
                Return EnlargeIcon(Velikost, Stretch, Icon.ExtractAssociatedIcon(FileName))

        End Select
    End Function

#End Region

#Region " Private Procedures "

    Private Function IconToCursor(ByVal Ico As Icon) As Cursor
        Dim handle As New SafeIconHandle(Ico.Handle)
        Return System.Windows.Interop.CursorInteropHelper.Create(handle)
    End Function

    Private Function IconToImageSource(ByVal Ico As Icon) As ImageSource
        Try
            Return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Ico.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
        Catch
            Return Nothing
        End Try
    End Function

    Private Function EnlargeIcon(ByVal Velikost As Integer, ByVal Stretch As Boolean, ByVal IconSize As Icon) As Icon
        If IconSize.Size.Width = Velikost Or Stretch Then
            Return IconSize
        End If
        Dim bm_source As Bitmap = New Bitmap(IconSize.ToBitmap)
        Dim bm_dest As New Bitmap(Velikost, Velikost)
        Dim gr_dest As Graphics = Graphics.FromImage(bm_dest)
        If IconSize.Size.Width > Velikost Then
            gr_dest.DrawImage(bm_source, 0, 0, Velikost, Velikost)
        Else
            gr_dest.DrawImage(bm_source, 8, 8, Velikost - 16, Velikost - 16)
        End If
        Return System.Drawing.Icon.FromHandle(bm_dest.GetHicon)
    End Function

    Private Sub proCheckFileType()
        If sFileName.Length < 4 Then Soubor = FileType.Associated : Exit Sub
        Select Case sFileName.ToLower.Substring(sFileName.Length - 4, 4)
            Case ".ico"
                Soubor = FileType.Icon
            Case ".exe"
                Soubor = FileType.Exe
            Case ".dll"
                Soubor = FileType.Dll
            Case Else
                Soubor = FileType.Associated
        End Select
    End Sub

    Private Sub proCreateClasses()
        proCheckFileType()
        VaznaChyba = False
        Try
            Select Case Soubor
                Case FileType.Icon
                    clsIconFile = New IconFile(sFileName)

                Case FileType.Exe
                    clsIconExeDll = New IconExeDll(sFileName)
                    ExeDllIcons = IconExeDll.SplitIcon(clsIconExeDll.GetIcon(iIndexIconInFile))

            End Select
        Catch ex As Exception
            VaznaChyba = True
            If Not Err.Number = 5 Then
                Dim FormDialog = New wpfDialog(Nothing, ex.Message, "Error: " + Err.Number.ToString, wpfDialog.Ikona.chyba, "Zavřít")
                FormDialog.ShowDialog()
            End If
        End Try
    End Sub

    Private Function RemoveQuotationMarks(ByVal Text As String) As String
        Dim Pos As Integer = Text.IndexOf(Chr(34))
        If Not Pos = -1 Then Text = Text.Substring(Pos + 1, Len(Text) - Pos - 1)
        Pos = Text.IndexOf(Chr(34))
        If Not Pos = -1 Then Text = Text.Substring(0, Pos)
        Pos = Text.IndexOf("//")
        If Pos = -1 Then
            Pos = Text.IndexOf("/")
            If Not Pos = -1 Then Text = Text.Substring(0, Pos - 1)
        End If
        Return Text
    End Function
#End Region

#Region " EXE or DLL file "

    Private Class IconExeDll
        Implements IDisposable

#Region "Win32 interop."

#Region "Consts."

        Private Const LOAD_LIBRARY_AS_DATAFILE As Integer = &H2

        Private Const RT_ICON As Integer = 3
        Private Const RT_GROUP_ICON As Integer = 14

        Private Const MAX_PATH As Integer = 260

        Private Const ERROR_SUCCESS As Integer = 0
        Private Const ERROR_FILE_NOT_FOUND As Integer = 2
        Private Const ERROR_BAD_EXE_FORMAT As Integer = 193
        Private Const ERROR_RESOURCE_TYPE_NOT_FOUND As Integer = 1813

        Private Const sICONDIR As Integer = 6
        ' sizeof(ICONDIR) 
        Private Const sICONDIRENTRY As Integer = 16
        ' sizeof(ICONDIRENTRY)
        Private Const sGRPICONDIRENTRY As Integer = 14
        ' sizeof(GRPICONDIRENTRY)

        'Exe File Constant
        Private Const SHGFI_EXETYPE As Integer = &H2000
        Private Const WIN32_GUI As Integer = 17744   '&H4550 PE, Win32 or Win64
        Private Const WIN16_GUI As Integer = 17742   '&H454E NE, Win16
        Private Const WIN16_DOS As Integer = 23117   '&H5A4D MZ, MS-DOS

        Private Structure SHFILEINFO
            Public hIcon As Integer
            Public iIcon As Integer
            Public dwAttributes As Integer
            <Runtime.InteropServices.MarshalAs(Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst:=260)> _
            Public szDisplayName As String
            <Runtime.InteropServices.MarshalAs(Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst:=80)> _
            Public szTypeName As String
        End Structure
#End Region

#Region "API Functions"

        <Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=True, CharSet:=Runtime.InteropServices.CharSet.Auto)> _
        Private Shared Function LoadLibrary(lpFileName As String) As IntPtr
        End Function

        <Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=True, CharSet:=Runtime.InteropServices.CharSet.Auto)> _
        Private Shared Function LoadLibraryEx(lpFileName As String, hFile As IntPtr, dwFlags As Integer) As IntPtr
        End Function

        Private Declare Auto Function FreeLibrary Lib "kernel32.dll" (hModule As IntPtr) As Boolean

        <Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=True, CharSet:=Runtime.InteropServices.CharSet.Auto)> _
        Private Shared Function GetModuleFileName(hModule As IntPtr, lpFilename As System.Text.StringBuilder, nSize As Integer) As Integer
        End Function

        <Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=True, CharSet:=Runtime.InteropServices.CharSet.Auto)> _
        Private Shared Function EnumResourceNames(hModule As IntPtr, lpszType As Integer, lpEnumFunc As EnumResNameDelegate, lParam As IconResInfo) As Boolean
        End Function

        <Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=True, CharSet:=Runtime.InteropServices.CharSet.Auto)> _
        Private Shared Function FindResource(hModule As IntPtr, lpName As IntPtr, lpType As Integer) As IntPtr
        End Function

        Private Declare Auto Function LoadResource Lib "kernel32.dll" (hModule As IntPtr, hResInfo As IntPtr) As IntPtr

        Private Declare Auto Function LockResource Lib "kernel32.dll" (hResData As IntPtr) As IntPtr

        Private Declare Auto Function SizeofResource Lib "kernel32.dll" (hModule As IntPtr, hResInfo As IntPtr) As Integer

        Private Declare Auto Function SHGetFileInfo Lib "shell32.dll" (ByVal pszPath As String, ByVal dwFileAttributes As Integer, ByRef psfi As SHFILEINFO, ByVal cbFileInfo As Integer, ByVal uFlags As Integer) As Integer

#End Region

#End Region

#Region "Managed Types"

        Private Class IconResInfo
            Public IconNames As New Generic.List(Of ResourceName)()
        End Class

        Private Class ResourceName
            Public Property Id() As IntPtr
                Get
                    Return m_Id
                End Get
                Private Set(value As IntPtr)
                    m_Id = value
                End Set
            End Property
            Private m_Id As IntPtr
            Public Property Name() As String
                Get
                    Return m_Name
                End Get
                Private Set(value As String)
                    m_Name = value
                End Set
            End Property
            Private m_Name As String

            Private _bufPtr As IntPtr = IntPtr.Zero

            Public Sub New(lpName As IntPtr)
                If (CUInt(lpName) >> 16) = 0 Then
                    ' #define IS_INTRESOURCE(_r) ((((ULONG_PTR)(_r)) >> 16) == 0)
                    Me.Id = lpName
                    Me.Name = Nothing
                Else
                    Me.Id = IntPtr.Zero
                    Me.Name = Runtime.InteropServices.Marshal.PtrToStringAuto(lpName)
                End If
            End Sub

            Public Function GetValue() As IntPtr
                If Me.Name Is Nothing Then
                    Return Me.Id
                Else
                    Me._bufPtr = Runtime.InteropServices.Marshal.StringToHGlobalAuto(Me.Name)
                    Return Me._bufPtr
                End If
            End Function

            Public Sub Free()
                If Me._bufPtr <> IntPtr.Zero Then
                    Try
                        Runtime.InteropServices.Marshal.FreeHGlobal(Me._bufPtr)
                    Catch
                    End Try

                    Me._bufPtr = IntPtr.Zero
                End If
            End Sub
        End Class

#End Region

#Region "Private Fields"

        Private _hModule As IntPtr = IntPtr.Zero
        Private _resInfo As IconResInfo = Nothing
        Private _iconCache As Icon() = Nothing

#End Region

#Region "Public Properties"

        Private _filename As String = Nothing

        ' Full path 
        Public ReadOnly Property Filename() As String
            Get
                Return Me._filename
            End Get
        End Property

        Public ReadOnly Property IconCount() As Integer
            Get
                Return Me._resInfo.IconNames.Count
            End Get
        End Property

#End Region

#Region "Contructor/Destructor and relatives"

        ''' <summary>
        ''' Load the specified executable file or DLL, and get ready to extract the icons.
        ''' </summary>
        ''' <param name="filename">The name of a file from which icons will be extracted.</param>
        Public Sub New(filename As String)
            If filename Is Nothing Then
                Throw New ArgumentNullException("filename")
            End If

            Dim SFI As New SHFILEINFO
            Dim dw As Integer = SHGetFileInfo(filename, 0, SFI, Len(SFI), SHGFI_EXETYPE) And &H7FFF
            If dw = WIN16_GUI Or dw = WIN16_DOS Or dw = 0 Then
                Throw New ArgumentException("Specified file '" + filename + "' is 16-bit.")
            End If

            Me._hModule = LoadLibrary(filename)
            If Me._hModule = IntPtr.Zero Then
                Me._hModule = LoadLibraryEx(filename, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE)
                If Me._hModule = IntPtr.Zero Then
                    Select Case Runtime.InteropServices.Marshal.GetLastWin32Error()
                        Case ERROR_FILE_NOT_FOUND
                            Throw New IO.FileNotFoundException("Specified file '" + filename + "' not found.")
                            Exit Sub
                        Case ERROR_BAD_EXE_FORMAT
                            Throw New ArgumentException("Specified file '" + filename + "' is not an executable file or DLL.")
                            Exit Sub
                        Case Else
                            Throw New System.ComponentModel.Win32Exception()
                            Exit Sub
                    End Select
                End If
            End If

            If IntPtr.Size = 8 Then
                'x64
            ElseIf IntPtr.Size = 4 Then 'x86
                Dim buf As New System.Text.StringBuilder(MAX_PATH)
                Dim len As Integer = GetModuleFileName(Me._hModule, buf, buf.Capacity + 1)
                If len <> 0 Then
                    Me._filename = buf.ToString()
                Else
                    Select Case Runtime.InteropServices.Marshal.GetLastWin32Error()
                        Case ERROR_SUCCESS
                            Me._filename = filename
                            Exit Select
                        Case Else
                            Throw New System.ComponentModel.Win32Exception()
                            Exit Sub
                    End Select
                End If
            End If

            Me._resInfo = New IconResInfo()
            Dim success As Boolean = EnumResourceNames(Me._hModule, RT_GROUP_ICON, AddressOf EnumResNameCallBack, Me._resInfo)
            If Not success Then
                Throw New System.ComponentModel.Win32Exception()
            End If

            Me._iconCache = New Icon(Me.IconCount - 1) {}
        End Sub

        Protected Overrides Sub Finalize()
            Try
                Dispose()
            Finally
                MyBase.Finalize()
            End Try
        End Sub

        Public Sub Dispose()
            If Me._hModule <> IntPtr.Zero Then
                Try
                    FreeLibrary(Me._hModule)
                Catch
                End Try

                Me._hModule = IntPtr.Zero
            End If

            If Me._iconCache IsNot Nothing Then
                For Each i As Icon In Me._iconCache
                    If i IsNot Nothing Then
                        Try
                            i.Dispose()
                        Catch
                        End Try
                    End If
                Next

                Me._iconCache = Nothing
            End If
        End Sub

        Public Sub DisposeAll() Implements System.IDisposable.Dispose

        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Extract an icon from the loaded executable file or DLL. 
        ''' </summary>
        ''' <param name="iconIndex">The zero-based index of the icon to be extracted.</param>
        ''' <returns>A System.Drawing.Icon object which may consists of multiple icons.</returns>
        ''' <remarks>Always returns new copy of the Icon. It should be disposed by the user.</remarks>
        Public Function GetIcon(iconIndex As Integer) As Icon
            If Me._hModule = IntPtr.Zero Then
                Throw New ObjectDisposedException("IconExtractor")
            End If

            If iconIndex < 0 OrElse Me.IconCount <= iconIndex Then
                Throw New ArgumentException("iconIndex is out of range. It should be between 0 and " + (Me.IconCount - 1).ToString() + ".")
            End If

            If Me._iconCache(iconIndex) Is Nothing Then
                Me._iconCache(iconIndex) = CreateIcon(iconIndex)
            End If

            Return DirectCast(Me._iconCache(iconIndex).Clone(), Icon)
        End Function

        ''' <summary>
        ''' Split an Icon consists of multiple icons into an array of Icon each consist of single icons.
        ''' </summary>
        ''' <param name="icon">The System.Drawing.Icon to be split.</param>
        ''' <returns>An array of System.Drawing.Icon each consist of single icons.</returns>
        Public Shared Function SplitIcon(icon As Icon) As Icon()
            If icon Is Nothing Then
                Throw New ArgumentNullException("icon")
            End If

            ' Get multiple .ico file image.
            Dim srcBuf As Byte() = Nothing
            Using stream As New IO.MemoryStream()
                icon.Save(stream)
                srcBuf = stream.ToArray()
            End Using

            Dim splitIcons As New Generic.List(Of Icon)()
            If True Then
                Dim count As Integer = BitConverter.ToInt16(srcBuf, 4)
                ' ICONDIR.idCount
                For i As Integer = 0 To count - 1
                    Using destStream As New IO.MemoryStream()
                        Using writer As New IO.BinaryWriter(destStream)
                            ' Copy ICONDIR and ICONDIRENTRY.
                            writer.Write(srcBuf, 0, sICONDIR - 2)
                            writer.Write(CShort(1))
                            ' ICONDIR.idCount == 1;
                            writer.Write(srcBuf, sICONDIR + sICONDIRENTRY * i, sICONDIRENTRY - 4)
                            writer.Write(sICONDIR + sICONDIRENTRY)
                            ' ICONDIRENTRY.dwImageOffset = sizeof(ICONDIR) + sizeof(ICONDIRENTRY)
                            ' Copy picture and mask data.
                            Dim imgSize As Integer = BitConverter.ToInt32(srcBuf, sICONDIR + sICONDIRENTRY * i + 8)
                            ' ICONDIRENTRY.dwBytesInRes
                            Dim imgOffset As Integer = BitConverter.ToInt32(srcBuf, sICONDIR + sICONDIRENTRY * i + 12)
                            ' ICONDIRENTRY.dwImageOffset
                            writer.Write(srcBuf, imgOffset, imgSize)

                            ' Create new icon.
                            destStream.Seek(0, IO.SeekOrigin.Begin)
                            splitIcons.Add(New Icon(destStream))
                        End Using
                    End Using
                Next
            End If

            Return splitIcons.ToArray()
        End Function

        Public Overrides Function ToString() As String
            Dim text As String = [String].Format("IconExtractor (Filename: '{0}', IconCount: {1})", Me.Filename, Me.IconCount)
            Return text
        End Function

#End Region

#Region "Private Methods"

        Private Delegate Function EnumResNameDelegate(hModule As IntPtr, lpszType As Integer, lpszName As IntPtr, lParam As IconResInfo) As Boolean

        Private Function EnumResNameCallBack(hModule As IntPtr, lpszType As Integer, lpszName As IntPtr, lParam As IconResInfo) As Boolean
            ' Callback function for EnumResourceNames().

            If lpszType = RT_GROUP_ICON Then
                lParam.IconNames.Add(New ResourceName(lpszName))
            End If

            Return True
        End Function

        Private Function CreateIcon(iconIndex As Integer) As Icon
            ' Get group icon resource.
            Dim srcBuf As Byte() = GetResourceData(Me._hModule, Me._resInfo.IconNames(iconIndex), RT_GROUP_ICON)

            ' Convert the resouce into an .ico file image.
            Using destStream As New IO.MemoryStream()
                Using writer As New IO.BinaryWriter(destStream)
                    Dim count As Integer = BitConverter.ToUInt16(srcBuf, 4)
                    ' ICONDIR.idCount
                    Dim imgOffset As Integer = sICONDIR + sICONDIRENTRY * count

                    ' Copy ICONDIR.
                    writer.Write(srcBuf, 0, sICONDIR)

                    For i As Integer = 0 To count - 1
                        ' Copy GRPICONDIRENTRY converting into ICONDIRENTRY.
                        writer.BaseStream.Seek(sICONDIR + sICONDIRENTRY * i, IO.SeekOrigin.Begin)
                        writer.Write(srcBuf, sICONDIR + sGRPICONDIRENTRY * i, sICONDIRENTRY - 4)
                        ' Common fields of structures
                        writer.Write(imgOffset)
                        ' ICONDIRENTRY.dwImageOffset
                        ' Get picture and mask data, then copy them.

                        Dim nID As IntPtr = CType(BitConverter.ToUInt16(srcBuf, sICONDIR + sGRPICONDIRENTRY * i + 12), IntPtr)
                        'IntPtr.op_Explicit)

                        ' GRPICONDIRENTRY.nID
                        Dim imgBuf As Byte() = GetResourceData(Me._hModule, nID, RT_ICON)

                        writer.BaseStream.Seek(imgOffset, IO.SeekOrigin.Begin)
                        writer.Write(imgBuf, 0, imgBuf.Length)

                        imgOffset += imgBuf.Length
                    Next

                    destStream.Seek(0, IO.SeekOrigin.Begin)
                    Return New Icon(destStream)
                End Using
            End Using
        End Function

        Private Function GetResourceData(hModule As IntPtr, lpName As IntPtr, lpType As Integer) As Byte()
            ' Get binary image of the specified resource.

            Dim hResInfo As IntPtr = FindResource(hModule, lpName, lpType)
            If hResInfo = IntPtr.Zero Then
                Throw New System.ComponentModel.Win32Exception()
            End If

            Dim hResData As IntPtr = LoadResource(hModule, hResInfo)
            If hResData = IntPtr.Zero Then
                Throw New System.ComponentModel.Win32Exception()
            End If

            Dim hGlobal As IntPtr = LockResource(hResData)
            If hGlobal = IntPtr.Zero Then
                Throw New System.ComponentModel.Win32Exception()
            End If

            Dim resSize As Integer = SizeofResource(hModule, hResInfo)
            If resSize = 0 Then
                Throw New System.ComponentModel.Win32Exception()
            End If

            Dim buf As Byte() = New Byte(resSize - 1) {}
            Runtime.InteropServices.Marshal.Copy(hGlobal, buf, 0, buf.Length)

            Return buf
        End Function

        Private Function GetResourceData(hModule As IntPtr, name As ResourceName, lpType As Integer) As Byte()
            Try
                Dim lpName As IntPtr = name.GetValue()
                Return GetResourceData(hModule, lpName, lpType)
            Finally
                name.Free()
            End Try
        End Function

#End Region

    End Class

#End Region

#Region " ICON file "

#Region " Icon File "

    Private Class IconFile

        'An Icon File has the following structure:
        'IconDir:
        ' - Reserved (Int16/Word)   Must be 0
        ' - Type (Int16/Word)       1 for Icon
        ' - Count (Int16/Word)      The numer of icons in this Icon File
        ' - IconDirEntry:
        '   - See IconImage

        Private _entries As New IconImageCollection

        'New - Loads an IconFile from the specified file
        Public Sub New(ByVal fileName As String)
            Me.New(New IO.FileInfo(fileName))
        End Sub

        'New - Loads an IconFile from the specified file
        Public Sub New(ByVal file As IO.FileInfo)
            Dim stm As IO.FileStream = file.OpenRead
            LoadFromStream(stm)
            Try

            Catch ex As Exception
                Throw
            Finally
                stm.Close()
            End Try
        End Sub

        'New - Loads an IconFile from the specified stream
        Public Sub New(ByVal stream As IO.Stream)
            LoadFromStream(stream)
        End Sub

        'Entries - Returns the collection of IconImages in this IconFile
        Public ReadOnly Property Entries() As IconImageCollection
            Get
                Return _entries
            End Get
        End Property

        'LoadFromStream - Loads the icon information from the specified stream
        Private Sub LoadFromStream(ByVal stream As IO.Stream)
            Dim br As New IO.BinaryReader(stream)
            Dim intCount As Int16

            'Reserved
            If br.ReadInt16 <> 0 Then
                ThrowInvalidFormat(New IO.IOException("The Reserved Int16 was not 0"))
            End If

            'Type
            If br.ReadInt16 <> 1 Then
                ThrowInvalidFormat(New IO.IOException("The Type Int16 was not 1.  This may be a cursor file"))
            End If

            'Count
            intCount = br.ReadInt16

            If intCount = 0 Then
                ThrowInvalidFormat(New IO.IOException("The Count Int16 was 0.  There are no icons in this file"))
            End If

            For i As Integer = 0 To intCount - 1
                Debug.WriteLine(i)
                Entries.Add(New IconImage(stream))
            Next
        End Sub

        'Throws an ArgumentException with the specified inner exception giving more detail about the error.
        Friend Shared Sub ThrowInvalidFormat(ByVal innerException As Exception)
            Throw New ArgumentException(innerException.Message, "stream", innerException)
        End Sub

        'GetIcon - Returns an icon of equal or smaller size and equal or smaller Pixelformat to the specified size and pixelformat.
        Public Function GetIcon(ByVal size As Size, ByVal pixelformat As Imaging.PixelFormat) As IconImage
            'Only 4, 8, 24 and 32bpp are valid
            If pixelformat = Imaging.PixelFormat.Format32bppArgb Or pixelformat = Imaging.PixelFormat.Format24bppRgb Or pixelformat = Imaging.PixelFormat.Format4bppIndexed Or pixelformat = Imaging.PixelFormat.Format8bppIndexed Then
            Else
                Throw New ArgumentException("Pixel Format can only be 32 bit, 8 bit or 4 bit", "pixelformat")
            End If

            'Search for an exact match icon
            For Each icon As IconImage In Entries
                If size.Equals(icon.SizePhysical) And pixelformat = icon.PixelFormat Then
                    Return icon
                End If
            Next

            'So there's no exact icon.  Find the best.
            'The best is the largest smaller icon and next lowest pixelformat
            'An icon of the right size is chosen over the right pixelformat
            Dim colSizes As New SizeCollection
            Dim colFormats As New PixelFormatCollection
            Dim i As Integer
            For Each icon As IconImage In Entries
                'Insert the size into the right place
                'Sizes are assumed to be square, hence sizes are ordered by width
                If colSizes.Count = 0 Then
                    colSizes.Add(icon.SizePhysical)
                    colFormats.Add(icon.PixelFormat)
                Else
                    Dim booAdded As Boolean
                    For i = 0 To colSizes.Count - 1
                        If icon.SizePhysical.Width >= colSizes(i).Width And icon.PixelFormat >= colFormats(i) Then
                            colSizes.Insert(i, icon.SizePhysical)
                            colFormats.Insert(i, icon.PixelFormat)
                            booAdded = True
                            Exit For
                        End If
                    Next

                    If Not booAdded Then
                        colSizes.Add(icon.SizePhysical)
                        colFormats.Add(icon.PixelFormat)
                    End If
                End If
            Next

            'Collections are sorted by size and then pixelformat
            'Go up to the biggest size we can get to
            i = 0

            Do Until i = colSizes.Count - 1 Or size.Width >= colSizes(i).Width
                i += 1
            Loop

            'Go back to get the right pixelformat
            Dim intCurrentWidth As Integer = colSizes(i).Width

            Do Until i = colFormats.Count - 1 Or pixelformat >= colFormats(i)
                i += 1
            Loop

            Return GetIcon(colSizes(i), colFormats(i))
        End Function
    End Class

#End Region

#Region " Icon Image "

    Private Class IconImage

        'For each icon in the IconFile, there's an IconDirEntry and an image
        'IconDirEntry:
        ' - Width (byte)                The width of the icon image
        ' - Height (byte)               The height of the icon image (sometimes this is double what it should be)
        ' - ColourCount (byte)          The number of colours in the icon (0 if 8bpp or higher)
        ' - Reserved (byte)             Must be zero
        ' - Planes (Int16/Word)         The number of planes in the icon image
        ' - BitCount (Int16/Word)       Number of bpp
        ' - BytesInRes (Int32/DWord)    Logical size of the icon image (includes Bitmapheader)
        ' - ImageOffset (Int32/DWord)   Address at which the Bitmapheader starts

        'The Icon Image then starts with a Bitmap header
        ' - bhSize (Int32/DWord)    The number of bytes that the bitmapheader takes up
        ' - Width (Int32/DWord)     The width of the icon image
        ' - Height (Int32/DWord)    The height of the icon image (double because of XOR and AND mask)
        ' - Planes (Int16/Word)     The number of planes in the icon image
        ' - BitCount (Int16/Word)   The number of bpp
        ' - Compression (Int32/DWord)   The compression used, must be zero
        ' - SizeImage (Int32/Word)  The number of bytes in the image
        ' - XPelsPerMeter, YPelsPerMeter, ColoursUsed and ColoursRequired are not used in icons.
        'Each icon image then has a RGB Table, which is an array of RGB values
        'Indexed PixelFormats then have an XOR mask, which is an array of numbers that correspond to indicies of
        'colours in the RGB array
        'This is then followed by an AND mask, where 0 means active and 1 means see through

        Private _width As Integer 'The width of the icon image
        Private _height As Integer 'The Height of the icon image
        Private _colorCount As Byte 'The number of colours in the IconImage
        Private _planes As Int16 'The number of planes in the IconImage
        Private _bitCount As Int16 'The icon's bpp
        Private _bytesInRes As Int32 'Number of Bytes it takes to encode the icon
        Private _imageOffset As Int32 'Place where the iconImage starts
        Private _icon As Bitmap 'The compiled Icon Image
        Private _colours As Color() 'The RGB array
        Private _colourIndicies(,) As Int32 'The XOR Mask

        Private _bhSize As Int32 'The size of the bitmap header
        Private _bhSizeImage As Int32 'The logical size of the image

        'New - Creates a new icon from the specified stream.
        Friend Sub New(ByVal stream As IO.Stream)
            Dim lngStreamPosition As Int64
            Dim br As New IO.BinaryReader(stream)

            'Read the IconDirEntry
            _width = br.ReadByte
            _height = br.ReadByte
            _colorCount = br.ReadByte

            If br.ReadByte <> 0 Then
                IconFile.ThrowInvalidFormat(New IO.IOException("The Reserved Byte in an IconDirEntry was not 0"))
            End If

            _planes = br.ReadInt16
            _bitCount = br.ReadInt16
            _bytesInRes = br.ReadInt32
            _imageOffset = br.ReadInt32

            'Get the Image
            lngStreamPosition = stream.Position
            stream.Position = _imageOffset

            'Read the BitmapInfoHeader
            _bhSize = br.ReadInt32
            _width = br.ReadInt32
            _height = CInt(br.ReadInt32 / 2)
            _planes = br.ReadInt16
            _bitCount = br.ReadInt16

            If br.ReadInt32 <> 0 Then 'Compression should be zero
                IconFile.ThrowInvalidFormat(New IO.IOException("This icon may be compressed."))
            End If

            _bhSizeImage = br.ReadInt32
            br.ReadInt32() 'XPelsPerMeter
            br.ReadInt32() 'YPelsPerMeter
            br.ReadInt32() 'ColoursUsed
            br.ReadInt32() 'ColoursRequired

            'Get Colours and Colour Indicies
            _icon = New Bitmap(_width, _height)

            ReDim _colourIndicies(_width - 1, _height - 1)

            If PixelFormat = Imaging.PixelFormat.Format4bppIndexed Then
                'If the icon is indexed then we have a separate colour table and the bitmap values refer to thier colour's index
                ReDim _colours(CInt(Math.Min((2 ^ _bitCount) - 1, _colorCount - 1)))
            ElseIf PixelFormat = Imaging.PixelFormat.Format8bppIndexed Then
                ReDim _colours(CInt(2 ^ _bitCount - 1))
            Else
                'If the icon is not indexed then each pixel's colour is specified individually rather than being referenced in a colour table
                'Therefore the colour table will be as long as the number of pixels and the indicies will be 0 to xy -1 
                ReDim _colours(CInt(_width) * CInt(_height) - 1)
                Dim i As Integer

                For y As Integer = _height - 1 To 0 Step -1
                    For x As Integer = 0 To _width - 1
                        _colourIndicies(x, y) = i
                        i += 1
                    Next
                Next
            End If

            'Read the Colour table
            For i As Integer = 0 To _colours.Length - 1
                Dim r As Byte
                Dim g As Byte
                Dim b As Byte
                Dim a As Byte

                b = br.ReadByte
                g = br.ReadByte
                r = br.ReadByte

                '24bpp have no reserved channel
                If PixelFormat <> Imaging.PixelFormat.Format24bppRgb Then
                    a = br.ReadByte
                End If

                'Only 32bpp has an alpha channel, 8bpp and 4bpp have 0 there which is totally transparent
                If PixelFormat = Imaging.PixelFormat.Format32bppArgb Then
                    _colours(i) = Color.FromArgb(a, r, g, b)
                Else
                    _colours(i) = Color.FromArgb(r, g, b)
                End If
            Next

            'Read the Index Table.  Rows in the XOR and AND mask are padded to 32bits on each row.
            If PixelFormat = Imaging.PixelFormat.Format4bppIndexed Or PixelFormat = Imaging.PixelFormat.Format8bppIndexed Then
                For y As Integer = _height - 1 To 0 Step -1
                    Dim x As Integer = 0
                    Do Until x >= _width - 1
                        'Convert the next four bytes to binary
                        Dim bytes As Byte() = {br.ReadByte, br.ReadByte, br.ReadByte, br.ReadByte}
                        Dim i As Integer = 0 'The index of the current bit

                        'Store the values
                        Do Until i > 3 Or x > _width - 1
                            If PixelFormat = Imaging.PixelFormat.Format8bppIndexed Then
                                _colourIndicies(x, y) = bytes(i)
                                x += 1
                            ElseIf PixelFormat = Imaging.PixelFormat.Format4bppIndexed Then
                                If bytes(i) < 16 Then
                                    _colourIndicies(x, y) = 0
                                    _colourIndicies(x + 1, y) = bytes(i)
                                Else
                                    Dim strHex As String = bytes(i).ToString("x")

                                    _colourIndicies(x, y) = CInt("&H" & strHex.Substring(0, 1))
                                    _colourIndicies(x + 1, y) = CInt("&H" & strHex.Substring(1, 1))
                                End If

                                x += 2
                            End If

                            i += 1
                        Loop
                    Loop
                Next
            End If

            'Read the And Table
            For y As Integer = _height - 1 To 0 Step -1
                Dim x As Integer = 0
                Do Until x >= _width - 1
                    'Convert to binary
                    Dim bits As New BitArray(New Byte() {br.ReadByte, br.ReadByte, br.ReadByte, br.ReadByte})
                    Dim i As Integer = 0 'The index of the current bit

                    Do Until i >= bits.Length - 1 Or x >= _width - 1
                        For j As Integer = 0 To 7
                            If bits(i) Then
                                _colourIndicies(x + 7 - j, y) = -1
                            End If
                            i += 1
                        Next
                        x += 8
                    Loop
                Loop
            Next

            'Put the colours onto the Bitmap
            For x As Integer = 0 To _width - 1
                For y As Integer = 0 To _height - 1
                    If _colourIndicies(x, y) = -1 Then
                        Icon.SetPixel(x, y, Color.Transparent)
                    Else
                        Icon.SetPixel(x, y, _colours(_colourIndicies(x, y)))
                    End If
                Next
            Next

            'Put the stream back to the next IconDirEntry
            stream.Position = lngStreamPosition
        End Sub

        'Icon - Returns the Icon's Image
        Public ReadOnly Property Icon() As Bitmap
            Get
                Return _icon
            End Get
        End Property

        'PixelFormat - Returns the PixelFormat of the IconImage
        Public ReadOnly Property PixelFormat() As Imaging.PixelFormat
            Get
                Select Case _bitCount
                    Case 4
                        Return Imaging.PixelFormat.Format4bppIndexed

                    Case 8
                        Return Imaging.PixelFormat.Format8bppIndexed

                    Case 24
                        Return Imaging.PixelFormat.Format24bppRgb

                    Case 32
                        Return Imaging.PixelFormat.Format32bppArgb
                    Case Else
                        Return Nothing
                End Select
            End Get
        End Property

        'SizeLogical - Returns the number of bytes that the icon image takes up in the image file
        Public ReadOnly Property SizeLogical() As Int32
            Get
                Return _bytesInRes
            End Get
        End Property

        'SizePhysical - Returns the physical size of the image on screen.
        Public ReadOnly Property SizePhysical() As Size
            Get
                Return New Size(_width, _height)
            End Get
        End Property
    End Class

#End Region

#Region " Image Collection "

    'IconImageCollection - A Collection of IconImages
    Private Class IconImageCollection
        Inherits CollectionBase

        Default Public ReadOnly Property Item(ByVal index As Integer) As IconImage
            Get
                Return CType(List.Item(index), IconImage)
            End Get
        End Property

        Public Sub Add(ByVal icon As IconImage)
            List.Add(icon)
        End Sub

        Public Function Contains(ByVal icon As IconImage) As Boolean
            Return List.Contains(icon)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal icon As IconImage)
            List.Insert(index, icon)
        End Sub

        Public Sub Remove(ByVal icon As IconImage)
            List.Remove(icon)
        End Sub
    End Class

    'SizeCollection - A collection of Sizes
    Private Class SizeCollection
        Inherits CollectionBase

        Default Public ReadOnly Property Item(ByVal index As Integer) As Size
            Get
                Return CType(List.Item(index), Size)
            End Get
        End Property

        Public Sub Add(ByVal size As Size)
            List.Add(size)
        End Sub

        Public Function Contains(ByVal size As Size) As Boolean
            Return List.Contains(size)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal size As Size)
            List.Insert(index, size)
        End Sub

        Public Sub Remove(ByVal size As Size)
            List.Remove(size)
        End Sub
    End Class

    'PixelFormatCollection - A collection of PixelFormats
    Private Class PixelFormatCollection
        Inherits CollectionBase

        Default Public ReadOnly Property Item(ByVal index As Integer) As Imaging.PixelFormat
            Get
                Return CType(List.Item(index), Imaging.PixelFormat)
            End Get
        End Property

        Public Sub Add(ByVal format As Imaging.PixelFormat)
            List.Add(format)
        End Sub

        Public Function Contains(ByVal format As Imaging.PixelFormat) As Boolean
            Return List.Contains(format)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal format As Imaging.PixelFormat)
            List.Insert(index, format)
        End Sub

        Public Sub Remove(ByVal format As Imaging.PixelFormat)
            List.Remove(format)
        End Sub
    End Class

#End Region

#End Region

#Region " Safe Icon Handle "

    Private Class SafeIconHandle
        Inherits SafeHandleZeroOrMinusOneIsInvalid
        <DllImport("user32.dll", SetLastError:=True)> _
        Friend Shared Function DestroyIcon(<[In]()> hIcon As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        Private Sub New()
            MyBase.New(True)
        End Sub

        Public Sub New(hIcon As IntPtr)
            MyBase.New(True)
            Me.SetHandle(hIcon)
        End Sub

        Protected Overrides Function ReleaseHandle() As Boolean
            Return DestroyIcon(Me.handle)
        End Function
    End Class
#End Region

End Class
