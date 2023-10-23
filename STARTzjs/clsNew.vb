Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Management
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Net.Mail
Imports System.ComponentModel
Imports System.Security.Cryptography.X509Certificates
Imports Microsoft.Win32

#Region " System "

Public Class clsSystem
    Inherits Collection(Of clsWindows)

    Public BuildYear As Integer = 2023
    Public Current As clsWindows
    Public User As String
    Public DomainName As String
    Public Is64bit As Boolean
    Public AdminRole As Boolean
    Public LgeCzech As Boolean
    Public Path As New clsPath
    Public ComputerName As String

#Region " Sub New "

    Sub New()
        sFramework()
        Is64bit = Environment.Is64BitOperatingSystem
        LgeCzech = GetLocalLanguage()

        Me.Add(New clsWindows("XP", 5))
        Me.Add(New clsWindows("Vista", 6))
        Me.Add(New clsWindows("7", 7))
        Me.Add(New clsWindows("8", 8))
        Me.Add(New clsWindows("10", 10))
        Me.Add(New clsWindows("11", 11))

        Dim sSystem As String = My.Computer.Info.OSFullName.ToLower
        For Each one In Me
            If sSystem.Contains(one.Name) Then
                Current = one
                Exit For
            End If
        Next
        If Current Is Nothing Then Current = New clsWindows("11", 11)

        ComputerName = Environment.MachineName
        User = System.Environment.UserName
        DomainName = System.Environment.UserDomainName
        My.User.InitializeWithWindowsUser()
        AdminRole = My.User.IsInRole(Microsoft.VisualBasic.ApplicationServices.BuiltInRole.Administrator)
    End Sub

    Public Class clsWindows
        Public Name As String
        Public Number As Integer
        Public Image As ImageSource

        Sub New(ByVal sName As String, ByVal iNumber As Integer)
            Name = sName : Number = iNumber
            If System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName = "STARTzjs" Then
                Image = CType(Application.Current.FindResource("Win" + sName.ToLower), ImageSource)
            End If
        End Sub
    End Class

#End Region

#Region " Cesty "

    Public Class clsPath
        Private Company As String = "pyramidak"
        Public Roaming As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\" & Company
        Public Documents As String = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) & "\" & Company
    End Class
#End Region

#Region " Framework "

    Public Function Framework() As Integer
        Return System.Environment.Version.Major
    End Function

    Public Function sFramework() As String
        Dim sFW As String = System.Environment.Version.Major.ToString + "." + System.Environment.Version.Minor.ToString
        If sFW = "2.0" Then sFW += " (3.5)"
        Return sFW
    End Function

#End Region

#Region " Local Language "

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto)>
    Public Shared Function GetLocaleInfo(ByVal Locale As Integer, ByVal LCType As Integer, ByVal lpLCData As String, ByVal cchData As Integer) As Integer
    End Function

    Private Function GetLocalLanguage() As Boolean
        Dim LOCALE_USER_DEFAULT As Integer = &H400
        Dim LOCALE_SENGLANGUAGE As Integer = &H1001
        Dim Buffer As String, Ret As Integer
        Buffer = New String(Chr(0), 256)
        Ret = GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SENGLANGUAGE, Buffer, Len(Buffer))
        Dim SysTrue As Boolean = If(Buffer.Substring(0, Ret - 1) = "Czech" Or Buffer.Substring(0, Ret - 1) = "Slovak", True, False)
        Dim RegTruePath As String = "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + Application.ProductName
        Dim RegTrue As String = myRegister.GetValue(HKEY.LOCALE_MACHINE, RegTruePath, "DefaultLanguage", "")
        If RegTrue = "" Then RegTrue = myRegister.GetValue(HKEY.CURRENT_USER, RegTruePath, "DefaultLanguage", "")
        Return If(RegTrue = "", SysTrue, If(RegTrue = "Czech" Or RegTrue = "Slovak", True, False))
    End Function

    Public Sub LoadLanguageDictionary(Czech As Boolean, Slozka As String)
        Dim LgeDict As New ResourceDictionary()
        LgeDict.Source = New Uri("/" + Application.ExeName + ";component/" & Slozka & "/" + If(Czech, "CZ", "EN") + "-String.xaml", UriKind.Relative)
        For Each Resource As ResourceDictionary In Application.Current.Resources.MergedDictionaries
            If Resource.Source.ToString.EndsWith("-String.xaml") Then 'odebere aktuální jazyk
                Application.Current.Resources.MergedDictionaries.Remove(Resource)
                Exit For
            End If
        Next
        Application.Current.Resources.MergedDictionaries.Add(LgeDict) 'přidá žádaný jazyk
    End Sub

#End Region

#Region " Shutdown "

    <DllImport("user32.dll")>
    Private Shared Function ExitWindowsEx(ByVal uFlags As Integer, ByVal dwReason As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function LockWorkStation() As Boolean
    End Function

    <DllImport("powrprof.dll", SetLastError:=True)>
    Public Shared Function SetSuspendState(<[In](), MarshalAs(UnmanagedType.I1)> ByVal Hibernate As Boolean, <[In](), MarshalAs(UnmanagedType.I1)> ByVal ForceCritical As Boolean, <[In](), MarshalAs(UnmanagedType.I1)> ByVal DisableWakeEvent As Boolean) As <MarshalAs(UnmanagedType.I1)> Boolean
    End Function

    <DllImport("advapi32.dll")>
    Private Shared Function InitiateSystemShutdownEx(<MarshalAs(UnmanagedType.LPStr)> ByVal lpMachinename As String, <MarshalAs(UnmanagedType.LPStr)> ByVal lpMessage As String, ByVal dwTimeout As Int32, ByVal bForceAppsClosed As Boolean, ByVal bRebootAfterShutdown As Boolean, ByVal dwReason As Int32) As Boolean
    End Function

    Public Sub StandBy()
        SetSuspendState(False, False, False)
    End Sub

    Public Sub Lock()
        LockWorkStation()
    End Sub

    Public Sub PowerOff()
        System.Diagnostics.Process.Start("Shutdown", "-s -t 0")
    End Sub

    Public Sub Restart()
        System.Diagnostics.Process.Start("Shutdown", "-r -t 0")
    End Sub

    Public Sub LogOff()
        'ExitWindowsEx(0, 0) Log Off
        ExitWindowsEx(4, 0) 'forced Log Off
    End Sub
#End Region

#Region " Process "

    Public Function isAppRunning(sProcessName As String, Optional sUser As String = "") As Boolean
        Return GetProcessOwner(sProcessName, sUser)
        If UBound(Diagnostics.Process.GetProcessesByName(sProcessName)) > 0 Then
            If sUser = "" Then
                Return True
            Else
                Return GetProcessOwner(sProcessName, sUser)
            End If
        Else
            Return False
        End If
    End Function

    Private Function GetProcessOwner(ProcessName As String, UserName As String) As Boolean
        Try
            Dim CountInstance As Integer
            Dim selectQuery As SelectQuery = New SelectQuery("Select * from Win32_Process Where Name = '" + ProcessName + ".exe' ")
            Dim searcher As ManagementObjectSearcher = New ManagementObjectSearcher(selectQuery)
            Dim y As System.Management.ManagementObjectCollection
            y = searcher.Get
            For Each proc As ManagementObject In y
                Dim sOwner(1) As String
                proc.InvokeMethod("GetOwner", CType(sOwner, Object()))
                If proc("Name").ToString = ProcessName & ".exe" Then
                    If sOwner(0) = UserName Then
                        CountInstance = CountInstance + 1
                        If CountInstance > 1 Then Return True
                    End If
                End If
            Next
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Shared Function isProcess(ByVal ProcessID As Integer) As Boolean
        If ProcessID = 0 Then Return False
        Try
            Process.GetProcessById(ProcessID, ".")
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Shared Function isProcess(ByVal ProcessName As String) As Boolean
        If ProcessName = "" Then Return False
        Try
            If Process.GetProcessesByName(ProcessName, ".").Length > 0 Then Return True
        Catch
        End Try
        Return False
    End Function

    Public Shared Function GetProcess(ByVal ProcessID As Integer) As Process
        If ProcessID = 0 Then Return Nothing
        Try
            Return Process.GetProcessById(ProcessID, ".")
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function GetProcess(ByVal ProcessName As String) As Process()
        If ProcessName = "" Then Return Nothing
        Try
            Return Process.GetProcessesByName(ProcessName, ".")
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function GetProcess(ByVal ProcessID As Integer, ByVal WindowTitle As String) As Process
        If WindowTitle = "" Then Return Nothing
        Dim allProcesses(), thisProcess As Process
        allProcesses = System.Diagnostics.Process.GetProcesses
        For Each thisProcess In allProcesses
            If thisProcess.MainWindowTitle = WindowTitle Then
                If ProcessID = 0 Then
                    Return thisProcess
                Else
                    If thisProcess.Id = ProcessID Then Return thisProcess
                End If
            End If
        Next
        Return Nothing
    End Function

#End Region

#Region " Windows Product ID "

    Public Function GetProductID() As String
        Return My.Computer.Registry.GetValue("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\", "ProductId", "N/A").ToString
    End Function

    Public Function GetDigitalProductID() As String
        Return GetDigitalProductIDfromRegister("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\", "DigitalProductId")
    End Function

    Private Function GetDigitalProductIDfromRegister(ByVal KeyPath As String, ByVal ValueName As String) As String
        Dim oHexBuf As Object = My.Computer.Registry.GetValue(KeyPath, ValueName, Nothing)
        If oHexBuf Is Nothing Then Return "Nothing"

        Dim HexBuf() As Byte = CType(oHexBuf, Byte())
        Dim tmp As String = ""

        For l As Integer = LBound(HexBuf) To UBound(HexBuf)
            tmp = tmp & " " & Hex(HexBuf(l))
        Next

        Dim StartOffset As Integer = 52
        Dim EndOffset As Integer = 67
        Dim Digits(24) As String

        Digits(0) = "B" : Digits(1) = "C" : Digits(2) = "D" : Digits(3) = "F"
        Digits(4) = "G" : Digits(5) = "H" : Digits(6) = "J" : Digits(7) = "K"
        Digits(8) = "M" : Digits(9) = "P" : Digits(10) = "Q" : Digits(11) = "R"
        Digits(12) = "T" : Digits(13) = "V" : Digits(14) = "W" : Digits(15) = "X"
        Digits(16) = "Y" : Digits(17) = "2" : Digits(18) = "3" : Digits(19) = "4"
        Digits(20) = "6" : Digits(21) = "7" : Digits(22) = "8" : Digits(23) = "9"

        Dim dLen As Integer = 29
        Dim sLen As Integer = 15
        Dim HexDigitalPID(15) As Integer
        Dim Des(30) As String

        Dim tmp2 As String = ""

        For i = StartOffset To EndOffset
            HexDigitalPID(i - StartOffset) = HexBuf(i)
            tmp2 = tmp2 & " " & Hex(HexDigitalPID(i - StartOffset))
        Next

        Dim KEYSTRING As String = ""

        For i As Integer = dLen - 1 To 0 Step -1
            If ((i + 1) Mod 6) = 0 Then
                Des(i) = "-"
                KEYSTRING = KEYSTRING & "-"
            Else
                Dim HN As Integer = 0
                For N As Integer = (sLen - 1) To 0 Step -1
                    Dim Value As Integer = (CInt((HN * 2 ^ 8)) Or HexDigitalPID(N))
                    HexDigitalPID(N) = Value \ 24
                    HN = (Value Mod 24)

                Next

                Des(i) = Digits(HN)
                KEYSTRING = KEYSTRING & Digits(HN)
            End If
        Next

        Return StrReverse(KEYSTRING)
    End Function

#End Region

#Region " Physical Harddisks "

    Public DiskLetter As String = Environment.SystemDirectory.Substring(0, 1)
    Private Harddisky As New Collection(Of clsHarddisk)

    Public Property HardDisks() As Collection(Of clsHarddisk)
        Get
            If Harddisky.Count = 0 Then LoadHarddisks()
            Return Harddisky
        End Get
        Set(ByVal value As Collection(Of clsHarddisk))
            Harddisky = value
        End Set
    End Property

    Class clsHarddisk
        Public Property DeviceID As String
        Public Property Model As String
        Public Property SerialNumber As String
        Public Property Letter As String
        Public Property Type As DiskTypes
    End Class

    Public Sub LoadHarddisks()
        If Harddisky.Count = 0 Then
            Try
                For Each drive As ManagementObject In New ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive").[Get]()
                    Dim disk As New clsHarddisk
                    For Each partition As ManagementObject In drive.GetRelated("Win32_DiskPartition")
                        For Each logical As ManagementObject In partition.GetRelated("Win32_LogicalDisk")
                            disk.Letter = logical("Name").ToString
                            If disk.Letter.Length > 1 Then disk.Letter = disk.Letter.Substring(0, 1)
                            'Exit For
                        Next
                    Next
                    disk.DeviceID = drive("DeviceId").ToString
                    disk.Type = If(drive("Model").ToString.ToLower.Contains("usb"), DiskTypes.Flashdisk_8, DiskTypes.Harddisk_7)
                    disk.Model = disk.Letter & ":   " & drive("Model").ToString
                    disk.SerialNumber = drive("SerialNumber").ToString.Trim.Replace("-", "").Replace(".", "")
                    If disk.SerialNumber.Length >= 8 AndAlso disk.SerialNumber.Substring(0, 5) <> "00000" Then
                        If Harddisky.FirstOrDefault(Function(x) x.Model = disk.Model) Is Nothing Then Harddisky.Add(disk)
                    End If
                Next
            Catch ex As Exception
            End Try
        End If
    End Sub

#End Region

#Region " Taskbar Location "

    Public Enum TaskbarLocation
        Top
        Bottom
        Left
        Right
    End Enum

    Public Function GetTaskbarLocation() As TaskbarLocation
        Dim bounds As New Rect(New Size(System.Windows.SystemParameters.PrimaryScreenWidth, System.Windows.SystemParameters.PrimaryScreenHeight))
        Dim working As Rect = System.Windows.SystemParameters.WorkArea
        If working.Height < bounds.Height And working.Y > 0 Then
            Return TaskbarLocation.Top
        ElseIf working.Height < bounds.Height And working.Y = 0 Then
            Return TaskbarLocation.Bottom
        ElseIf working.Width < bounds.Width And working.X > 0 Then
            Return TaskbarLocation.Left
        ElseIf working.Width < bounds.Width And working.X = 0 Then
            Return TaskbarLocation.Right
        Else
            Return Nothing
        End If
    End Function

#End Region

#Region " Change Screen "
    Enum DisplayMode
        Internal
        External
        Extend
        Duplicate
    End Enum
    Public Sub SetDisplayMode(ByVal Mode As DisplayMode)
        Dim proc = New Process()
        proc.StartInfo.FileName = "DisplaySwitch.exe"

        Select Case Mode
            Case DisplayMode.External
                proc.StartInfo.Arguments = "/external"
            Case DisplayMode.Internal
                proc.StartInfo.Arguments = "/internal"
            Case DisplayMode.Extend
                proc.StartInfo.Arguments = "/extend"
            Case DisplayMode.Duplicate
                proc.StartInfo.Arguments = "/clone"
        End Select

        proc.Start()
    End Sub

#End Region

#Region " Mute sound "

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr
    End Function
    Private Const WM_APPCOMMAND As Integer = &H319
    Private Const APPCOMMAND_VOLUME_MUTE As Integer = &H80000
    Private Const APPCOMMAND_VOLUME_DOWN As Integer = &H90000
    Private Const APPCOMMAND_VOLUME_UP As Integer = &HA0000

    Public Sub MuteSound(Wnd As Window)
        Dim wMainPtr As IntPtr = New System.Windows.Interop.WindowInteropHelper(Wnd).Handle
        SendMessage(New Interop.WindowInteropHelper(Wnd).Handle, WM_APPCOMMAND, IntPtr.Zero, New IntPtr(APPCOMMAND_VOLUME_MUTE))
    End Sub

#End Region

End Class

#End Region

#Region " Shared Memory "

Public Class clsSharedMemory
    'APIs
    Private Declare Function CreateFileMapping Lib "kernel32" Alias "CreateFileMappingA" (ByVal hFile As Integer, ByVal lpFileMappigAttributes As Integer, ByVal flProtect As Integer, ByVal dwMaximumSizeHigh As Integer, ByVal dwMaximumSizeLow As Integer, ByVal lpName As String) As Integer
    Private Declare Function MapViewOfFile Lib "kernel32" Alias "MapViewOfFile" (ByVal hFileMappingObject As Integer, ByVal dwDesiredAccess As Integer, ByVal dwFileOffsetHigh As Integer, ByVal dwFileOffsetLow As Integer, ByVal dwNumberOfBytesToMap As Integer) As IntPtr
    Private Declare Function UnmapViewOfFile Lib "kernel32" Alias "UnmapViewOfFile" (ByVal lpBaseAddress As IntPtr) As Integer
    Private Declare Function CloseHandle Lib "kernel32" Alias "CloseHandle" (ByVal hObject As Integer) As Integer

    'Constants
    Private Const FILE_MAP_ALL_ACCESS As Integer = &HF001F
    Private Const PAGE_READWRITE As Integer = &H4
    Private Const INVALID_HANDLE_VALUE As Integer = -1

    'Variables
    Private FileHandle As Integer
    Private SharePoint As IntPtr

#Region " Open and Close (Memory) Procedures "

    Public Function Open(ByVal MemoryName As String) As Boolean
        'Get a handle to an area of memory and name it the name passed in MemoryName.
        'Any application that maps an area of memory with that name gets the same 
        'address, so data can be shared.
        'Note: the INVALID_HANDLE_VALUE, which tells windows not to use a file but
        'just memory.
        FileHandle = CreateFileMapping(INVALID_HANDLE_VALUE, 0, PAGE_READWRITE, 0, 128, MemoryName)

        'Get a pointer to the area of memory we mapped.
        If Not FileHandle = 0 Then
            SharePoint = MapViewOfFile(FileHandle, FILE_MAP_ALL_ACCESS, 0, 0, 0)
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub Close()
        'Close the memory handle.
        UnmapViewOfFile(SharePoint)
        CloseHandle(FileHandle)
    End Sub

    Protected Overrides Sub Finalize()
        'Close the memory handle
        Call Close()

        'Finalize Base Class
        MyBase.Finalize()
    End Sub

#End Region

    Public Function Peek() As String
        'Copy the data length to a variable.
        Dim myDataLength As Integer = Marshal.ReadInt32(SharePoint)

        'Create an array to hold the data in memory.
        Dim myBuffer(myDataLength - 1) As Byte

        'Copy the data in memory to the array. 'SharePoint.ToInt64
        Try
            Marshal.Copy(New IntPtr(SharePoint.ToInt32 + 4), myBuffer, 0, myDataLength)
        Catch ex As Exception
            Marshal.Copy(New IntPtr(SharePoint.ToInt64 + 4), myBuffer, 0, myDataLength)
        End Try

        'Return Output (Unicode)
        Return System.Text.Encoding.UTF8.GetString(myBuffer)
    End Function

    Public Sub Put(ByVal Data As String)
        'Create an array with one element for each character. (Unicode)
        Dim myBuffer As Byte() = System.Text.Encoding.UTF8.GetBytes(Data)

        'Copy the length of the string into the first four bytes of the memory location
        Marshal.WriteInt32(SharePoint, Data.Length)

        'Copy the string data to memory right after the length.
        Marshal.Copy(myBuffer, 0, New IntPtr(SharePoint.ToInt32 + 4), myBuffer.Length)
    End Sub

    Public Sub ResetMemory()
        'Reset Data Lenght (Set Data Length to 0 - Empty)
        Marshal.WriteInt32(SharePoint, 0I)
    End Sub

    Public ReadOnly Property DataExists() As Boolean
        Get
            'Copy the data length to a variable.
            Dim myDataLength As Integer
            myDataLength = Marshal.ReadInt32(SharePoint)
            If Not myDataLength = 0 Then Return True
            Return False
        End Get
    End Property

End Class

#End Region

#Region " CSV "

Class clsCSV

    Public Function GetDataTable(filename As String) As Data.DataTable
        Dim csvData As Data.DataTable = New Data.DataTable()

        Try
            Using csvReader As FileIO.TextFieldParser = New FileIO.TextFieldParser(filename)
                csvData = GetDataTable(csvReader)
            End Using
        Catch ex As Exception
        End Try

        Return csvData
    End Function

    Public Function GetDataTable(stream As IO.MemoryStream) As Data.DataTable
        Dim csvData As Data.DataTable = New Data.DataTable()

        stream.Seek(0, IO.SeekOrigin.Begin)
        Try
            Using csvReader As FileIO.TextFieldParser = New FileIO.TextFieldParser(stream)
                csvData = GetDataTable(csvReader)
            End Using
        Catch ex As Exception
        End Try

        Return csvData
    End Function

    Private Function GetDataTable(csvReader As FileIO.TextFieldParser) As Data.DataTable
        Dim csvData As Data.DataTable = New Data.DataTable()

        csvReader.SetDelimiters(New String() {","})
        csvReader.HasFieldsEnclosedInQuotes = True

        Dim colFields As String() = csvReader.ReadFields()
        For Each column As String In colFields
            Dim datecolumn As Data.DataColumn = New Data.DataColumn(column)
            datecolumn.AllowDBNull = True
            csvData.Columns.Add(datecolumn)
        Next

        csvData.Rows.Add(colFields) 'první řádek načíst jako data
        While Not csvReader.EndOfData
            Dim fieldData As String() = csvReader.ReadFields()
            csvData.Rows.Add(fieldData)
        End While

        Return csvData
    End Function

End Class

#End Region

#Region " Encryption "

Class clsEncryption

    Public Password As String
    Private key() As Byte = Nothing
    Private iv() As Byte = Nothing
    Private aesCSP As New Cryptography.AesCryptoServiceProvider()

    Sub New(Heslo As String, Optional Okno As Window = Nothing, Optional NoPassEndApp As Boolean = False)
        If Heslo = "" Then
            Dim wDialog = New wpfDialog(Okno, "Zadejte přístupové heslo:", Okno.Title, wpfDialog.Ikona.heslo, "OK", "Zavřít", True)
            wDialog.ShowDialog()
            If wDialog.DialogResult = False And NoPassEndApp = True Then End

        Else
            Password = Heslo
        End If

        ' Find a valid key size for this provider.
        Dim key_size_bits As Integer = 0
        For i As Integer = 1024 To 1 Step -1
            If (aesCSP.ValidKeySize(i)) Then
                key_size_bits = i
                Exit For
            End If
        Next i
        Debug.Assert(key_size_bits > 0)

        ' Get the block size for this provider.
        Dim block_size_bits As Integer = aesCSP.BlockSize

        ' Generate the key and initialization vector.
        Dim salt() As Byte = {&H0, &H0, &H1, &H2, &H3, &H4, &H5, &H6, &HF1, &HF0, &HEE, &H21, &H22, &H45}
        Dim derive_bytes As New Cryptography.Rfc2898DeriveBytes(Password, salt, 1000)

        key = derive_bytes.GetBytes(CInt(key_size_bits / 8))
        iv = derive_bytes.GetBytes(CInt(block_size_bits / 8))
    End Sub

    Protected Overrides Sub Finalize()
        aesCSP.Clear()
        MyBase.Finalize()
    End Sub

    Public Function EncryptString(ByVal in_string As String) As String
        Return EncryptString_Aes(True, in_string)
    End Function

    Public Function DecryptString(ByVal in_string As String) As String
        Return EncryptString_Aes(False, in_string)
    End Function

    Public Sub EncryptFile(ByVal in_file As String, ByVal out_file As String)
        CryptFile(True, in_file, out_file)
    End Sub

    Public Sub DecryptFile(ByVal in_file As String, ByVal out_file As String)
        CryptFile(False, in_file, out_file)
    End Sub

    Public Sub EncryptStream(ByVal in_stream As IO.MemoryStream, ByVal out_file As String)
        If myFile.Delete(out_file, False, False) Then
            in_stream.Position = 0
            Using out_stream As New IO.FileStream(out_file, IO.FileMode.Create, IO.FileAccess.Write)
                If Password = "" Then
                    in_stream.CopyTo(out_stream)
                Else
                    CryptStream(True, in_stream, out_stream)
                End If
            End Using
        End If
        in_stream.Dispose()
    End Sub

#Region " Public Stream Decryption "

    Public Function DecryptFile(ByVal in_file As String) As IO.MemoryStream
        If myFile.Exist(in_file) Then
            If Password = "" Then
                Return myFile.ToStream(in_file)
            Else
                Return DecryptStream(New IO.FileStream(in_file, IO.FileMode.Open))
            End If
        Else
            Return Nothing
        End If
    End Function

    'Pouze pro dekódování kvůli UTF8Encoding
    Public Function DecryptStream(ByVal in_stream As IO.Stream) As IO.MemoryStream
        ' Make the decryptor.
        Dim crypto_transform As Cryptography.ICryptoTransform
        crypto_transform = aesCSP.CreateDecryptor(key, iv)

        Dim memStream As New IO.MemoryStream
        Try
            Using csDecrypt As New Cryptography.CryptoStream(in_stream, crypto_transform, Cryptography.CryptoStreamMode.Read)
                Using srDecrypt As New IO.StreamReader(csDecrypt)
                    Dim encoding As New System.Text.UTF8Encoding
                    Dim Bytes() As Byte = encoding.GetBytes(srDecrypt.ReadToEnd)
                    memStream = New IO.MemoryStream(Bytes)
                End Using
            End Using
        Catch ex As Exception
        End Try

        crypto_transform.Dispose()

        Return memStream
    End Function

#End Region

#Region " Private File Encryption "

    Private Sub CryptFile(ByVal bEncrypt As Boolean, ByVal in_file As String, ByVal out_file As String)
        ' Create input and output file streams.
        If myFile.Delete(out_file, False, False) Then
            Using in_stream As New IO.FileStream(in_file, IO.FileMode.Open, IO.FileAccess.Read)
                Using out_stream As New IO.FileStream(out_file, IO.FileMode.Create, IO.FileAccess.Write)
                    If Password = "" Then
                        in_stream.CopyTo(out_stream)
                    Else
                        CryptStream(bEncrypt, in_stream, out_stream)
                    End If
                End Using
            End Using
        End If
    End Sub

    'Deokódování nebo enkódování do souboru:
    Private Sub CryptStream(ByVal bEncrypt As Boolean, ByVal in_stream As IO.Stream, ByVal out_stream As IO.Stream)
        ' Make the encryptor or decryptor.
        Dim crypto_transform As Cryptography.ICryptoTransform = Nothing
        If bEncrypt Then
            crypto_transform = aesCSP.CreateEncryptor(key, iv)
        Else
            crypto_transform = aesCSP.CreateDecryptor(key, iv)
        End If

        Try
            Using crypto_stream As New Cryptography.CryptoStream(out_stream, crypto_transform, Cryptography.CryptoStreamMode.Write)
                ' Encrypt or decrypt the file.
                Const block_size As Integer = 1024
                Dim buffer(block_size) As Byte
                Dim bytes_read As Integer
                Do
                    ' Read some bytes.
                    bytes_read = in_stream.Read(buffer, 0, block_size)
                    If (bytes_read = 0) Then Exit Do

                    ' Write the bytes into the CryptoStream.
                    crypto_stream.Write(buffer, 0, bytes_read)
                Loop
            End Using
        Catch
        End Try

        crypto_transform.Dispose()
    End Sub
#End Region

#Region " Private String Encryption "

    Private Function EncryptString_Aes(ByVal bEncrypt As Boolean, ByVal sText As String) As String
        ' Make the encryptor or decryptor.
        Dim crypto_transform As Cryptography.ICryptoTransform
        If bEncrypt Then
            crypto_transform = aesCSP.CreateEncryptor(key, iv)
            ' Create the streams used for encryption.
            Dim msEncrypt As New IO.MemoryStream()
            Using csEncrypt As New Cryptography.CryptoStream(msEncrypt, crypto_transform, Cryptography.CryptoStreamMode.Write)
                Using swEncrypt As New IO.StreamWriter(csEncrypt)
                    'Write all data to the stream.
                    swEncrypt.Write(sText)
                End Using
            End Using
            Return Convert.ToBase64String(msEncrypt.ToArray())
        Else
            crypto_transform = aesCSP.CreateDecryptor(key, iv)
            ' Create the streams used for decryption.
            Try
                Using msDecrypt As New IO.MemoryStream(Convert.FromBase64String(sText))
                    Using csDecrypt As New Cryptography.CryptoStream(msDecrypt, crypto_transform, Cryptography.CryptoStreamMode.Read)
                        Using srDecrypt As New IO.StreamReader(csDecrypt)
                            ' Read the decrypted bytes from the decrypting stream
                            ' and place them in a string.
                            Return srDecrypt.ReadToEnd()
                        End Using
                    End Using
                End Using
            Catch ex As Exception
            End Try
        End If
        crypto_transform.Dispose()
        Return ""
    End Function
#End Region

End Class


#End Region

#Region " Serialization "

Class clsSerialization

    Private myObjekt As Object
    Private Wnd As Window
    Sub New(Objekt As Object, Optional Okno As Window = Nothing)
        myObjekt = Objekt : Wnd = Okno
    End Sub

    Public Sub WriteXml(fileName As String)
        Dim mySerializer As New Xml.Serialization.XmlSerializer(myObjekt.GetType)
        If myFile.Delete(fileName, False, False) Then
            If myFolder.Exist(myFile.Path(fileName), True) Then
                Using fileStream As New IO.FileStream(fileName, IO.FileMode.Create, IO.FileAccess.Write)
                    mySerializer.Serialize(fileStream, myObjekt)
                End Using
            End If
        End If
    End Sub

    Public Function WriteXml() As IO.MemoryStream
        Dim mySerializer As New Xml.Serialization.XmlSerializer(myObjekt.GetType)
        Dim memStream As New IO.MemoryStream
        mySerializer.Serialize(memStream, myObjekt)
        Return memStream
    End Function

    Public Function ReadXml(fileName As String) As Object
        Dim mySerializer As New Xml.Serialization.XmlSerializer(myObjekt.GetType)
        Using reader As New IO.StreamReader(fileName)
            Try
                Return mySerializer.Deserialize(reader)
            Catch Err As Exception
                If myFile.Exist(fileName & ".bak") Then
                    Using reader2 As New IO.StreamReader(fileName & ".bak")
                        Try
                            Return mySerializer.Deserialize(reader2)
                        Catch Err2 As Exception
                            Call (New wpfDialog(Wnd, fileName & NR & "Obnovení ze zálohy ne nepovedlo." + NR + "Obnovte ručně zálohu z cloudu.", Application.Title, wpfDialog.Ikona.ok)).ShowDialog()
                        End Try
                    End Using
                Else
                    Call (New wpfDialog(Wnd, fileName & NR & "Nepodporovaný formát souboru.", Application.Title, wpfDialog.Ikona.ok)).ShowDialog()
                End If
            End Try
        End Using
        Return myObjekt
    End Function

    Public Function ReadXml(fileStream As IO.Stream) As Object
        If fileStream Is Nothing Then Return myObjekt
        Dim mySerializer As New Xml.Serialization.XmlSerializer(myObjekt.GetType)
        Using reader As New IO.StreamReader(fileStream)
            Try
                Return mySerializer.Deserialize(reader)
            Catch
                Call (New wpfDialog(Wnd, "Nepodporovaný formát souboru.", Application.Title, wpfDialog.Ikona.ok)).ShowDialog()
            End Try
        End Using
        fileStream.Dispose()
        Return myObjekt
    End Function
End Class

#End Region

#Region " Cloud "

Public Enum Cloud
    Documents = 0
    OneDrive = 1
    DropBox = 2
    GoogleDisk = 3
    Sync = 4
End Enum

Public Class clsCloud
    Public DropBoxExist As Boolean
    Public DropBoxFolder As String = ""
    Public GoogleDriveExist As Boolean
    Public GoogleDriveFolder As String = ""
    Public OneDriveExist As Boolean
    Public OneDriveFolder As String = ""
    Public SyncExist As Boolean
    Public SyncFolder As String = ""

    Sub New()
        CheckClouds()
    End Sub

    Public Sub CheckClouds()
        Dim sFolder As String
        Dim dbPath As String
        'DropBox
        dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dropbox\host.db")
        If myFile.Exist(dbPath) = False Then dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dropbox\host.db")
        If myFile.Exist(dbPath) Then
            Try
                Dim lines As String() = System.IO.File.ReadAllLines(dbPath)
                Dim dbBase64Text As Byte() = Convert.FromBase64String(lines(1))
                Dim dFolder As String = System.Text.ASCIIEncoding.ASCII.GetString(dbBase64Text)
                DropBoxExist = myFolder.Exist(dFolder)
                If DropBoxExist Then DropBoxFolder = dFolder
            Catch ex As Exception
                DropBoxExist = False
            End Try
        End If

        'GoogleDrive
        sFolder = myRegister.GetValue(HKEY.CURRENT_USER, "SOFTWARE\Google\DriveFS\Share", "BasePath", "")
        If Not sFolder = "" Then
            dbPath = myFolder.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Disk Google")
            If myFolder.Exist(dbPath) = False Then dbPath = myFolder.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Můj Disk")
            GoogleDriveExist = myFolder.Exist(dbPath)
            If GoogleDriveExist Then GoogleDriveFolder = dbPath
        Else
            dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\Google\Drive\user_default\sync_config.db"
            If myFile.Exist(dbPath) = False Then dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\Google\Drive\sync_config.db"
            Try
                Dim gFolder As String = GetFolderFromFile(dbPath)
                GoogleDriveExist = myFolder.Exist(gFolder)
                If GoogleDriveExist Then GoogleDriveFolder = gFolder
            Catch ex As Exception
                GoogleDriveExist = False
            End Try
        End If

        'OneDrive
        sFolder = myRegister.GetValue(HKEY.CURRENT_USER, "Software\Microsoft\OneDrive", "UserFolder", "")
        If sFolder = "" Then myRegister.GetValue(HKEY.CURRENT_USER, "Software\Microsoft\Windows\CurrentVersion\SkyDrive", "UserFolder", "")
        If sFolder = "" Then myRegister.GetValue(HKEY.CURRENT_USER, "Software\Microsoft\SkyDrive", "UserFolder", "")
        If sFolder = "" Then myRegister.GetValue(HKEY.CURRENT_USER, "Software\Microsoft\Windows\CurrentVersion\OneDrive", "UserFolder", "")
        OneDriveExist = myFolder.Exist(sFolder)
        If OneDriveExist Then OneDriveFolder = sFolder

        'Sync
        Try
            sFolder = GetFolderFromFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\Sync.Config\1.1\cfg.db")
            SyncExist = myFolder.Exist(sFolder)
            If SyncExist Then SyncFolder = sFolder
        Catch ex As Exception
            SyncExist = False
        End Try

    End Sub

    Private Function GetFolderFromFile(dbPath As String) As String
        GetFolderFromFile = ""
        If myFile.Exist(dbPath) Then
            If myFile.Copy(dbPath, dbPath + ".bak") Then
                dbPath = dbPath + ".bak"
                Dim SR As New IO.StreamReader(dbPath)
                Dim sText As String = SR.ReadToEnd
                SR.Dispose()

                Dim disk As String = dbPath.Substring(0, 3)
                Dim iStart As Integer = sText.IndexOf(disk, 1)
                If iStart = -1 Then iStart = sText.IndexOf(disk.Replace("\", "/"), 1)
                If iStart = -1 Then Return ""
                Dim iEnd As Integer = sText.IndexOf("Sync", iStart)
                If iEnd = -1 Then
                    iEnd = sText.IndexOf("Disk Google", iStart)
                    If iEnd = -1 Then
                        iEnd = sText.IndexOf("Google Drive", iStart)
                        If iEnd = -1 Then
                            Return ""
                        Else
                            iEnd += 12
                        End If
                    Else
                        iEnd += 11
                    End If
                Else
                    iEnd += 4
                End If

                GetFolderFromFile = sText.Substring(iStart, iEnd - iStart)
                If GetFolderFromFile.Contains("/") Then GetFolderFromFile = GetFolderFromFile.Replace("/", "\")
            End If
        End If
    End Function

    Public Function NewAppPath(newCloud As Cloud, Optional filename As String = "") As String
        If filename = "" Then filename = "pyramidak\" & Application.ExeName & If(Application.selType = 1, ".xml", ".db")

        Select Case newCloud
            Case Cloud.OneDrive
                If OneDriveExist = False Then newCloud = Cloud.Documents
            Case Cloud.DropBox
                If DropBoxExist = False Then newCloud = Cloud.Documents
            Case Cloud.GoogleDisk
                If GoogleDriveExist = False Then newCloud = Cloud.Documents
            Case Cloud.Sync
                If SyncExist = False Then newCloud = Cloud.Documents
        End Select

        Select Case newCloud
            Case Cloud.OneDrive
                Return myFile.Join(OneDriveFolder, filename)
            Case Cloud.DropBox
                Return myFile.Join(DropBoxFolder, filename)
            Case Cloud.GoogleDisk
                Return myFile.Join(GoogleDriveFolder, filename)
            Case Cloud.Sync
                Return myFile.Join(SyncFolder, filename)
            Case Else
                Return myFile.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename)
        End Select
    End Function

    Public Function GoogleTokenExist() As Boolean
        Return myFile.Exist(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & Application.CompanyName & "\" & Application.ExeName & "\Google.Apis.Auth.OAuth2.Responses.TokenResponse-user")
    End Function

End Class

#End Region

#Region " Converters "

#Region " Size "

Class clsPlusConverter
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Return CDbl(value) + Double.Parse(CStr(parameter), New System.Globalization.CultureInfo("en-US"))
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class

Class clsMultipleConverter
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Return CDbl(value) * Double.Parse(CStr(parameter), New System.Globalization.CultureInfo("en-US"))
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class
#End Region

#Region " Days "
Class clsDaysConverter
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        If IsNumeric(value) = False Then
            Return Nothing
        Else
            Dim dny As Integer = CInt(value)
            Dim wDatum As Date = DateSerial(1, 1, dny)
            Return String.Format("{2} dnů, {1} měsíců, {0} roků", wDatum.Year - 2001, wDatum.Month - 1, wDatum.Day - 1)
        End If
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class
#End Region

#Region " Boolean "
Public Class clsBooleanConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        If IsDBNull(value) Then value = False
        If value Is Nothing Then value = False
        Return value
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class

Public Class clsNumberToBooleanConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing Then Return False
        Return CDbl(value) > 0
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class

Public Class clsDateToBooleanConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing Then Return False
        Return Today > CDate(value)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class

Public Class clsNothingToBooleanConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Return Not IsNothing(value)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class
#End Region

#Region " Visibility "

Public Class clsStringToVisibilityConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing Then value = ""
        Return If(value.ToString = "", Visibility.Hidden, Visibility.Visible)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException("Cannot convert back")
    End Function
End Class

#End Region

Class StyleConverter
    Implements IMultiValueConverter

    Public Function Convert(ByVal values As Object(), ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IMultiValueConverter.Convert
        Dim Hodnota As Integer
        If IsNumeric(values(0)) Then Hodnota = CInt(values(0))
        Return TryCast(values(Hodnota + 1), Style)
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetTypes As Type(), ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function

End Class

#End Region

#Region " Email "

Public Class clsEmail

    Private WithEvents bgwEmail As New System.ComponentModel.BackgroundWorker
    Private Smtp As New SmtpClient
    Private sUserEmail As String
    Private SentException As Exception
    Public Event EmailSent(Chyba As Exception)

    Sub New()

    End Sub
    Sub New(UserEmail As String, UserPassword As String, Optional port As Integer = 587)
        sUserEmail = UserEmail
        CreateSmtp(UserPassword, port)
    End Sub

    Public Sub ChangeUser(UserEmail As String, UserPassword As String, Optional port As Integer = 587)
        sUserEmail = UserEmail
        CreateSmtp(UserPassword, port)
    End Sub

    Private Sub CreateSmtp(password As String, port As Integer)
        Smtp.Host = "smtp." & Split(sUserEmail, "@")(1)
        Smtp.Port = port '587 nebo 465
        Smtp.EnableSsl = True
        Smtp.DeliveryMethod = SmtpDeliveryMethod.Network
        Smtp.Timeout = 7000
        Smtp.Credentials = New Net.NetworkCredential(sUserEmail, password)
    End Sub

    Public Function CreateMail(email As String, header As String, message As String) As MailMessage
        Dim mail As New MailMessage
        mail.From = New MailAddress(sUserEmail)
        mail.To.Add(email)
        mail.IsBodyHtml = False
        mail.Body = header
        mail.Subject = message
        Return mail
    End Function

    Public Sub Send(Mail As MailMessage)
        bgwEmail.RunWorkerAsync(Mail)
    End Sub
    Private Sub bgwEmail_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles bgwEmail.DoWork
        Dim Mail = CType(e.Argument, MailMessage)
        Try
            Smtp.Send(Mail)
            SentException = Nothing
        Catch Ex As Exception 'časový limit operace vypršel má číslo chyby 5
            SentException = Ex
        End Try
    End Sub

    Private Sub bgwEmail_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bgwEmail.RunWorkerCompleted
        RaiseEvent EmailSent(SentException)
    End Sub

End Class

#End Region


