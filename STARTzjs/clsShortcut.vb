'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'
' Filename:     ShellShortcut.vb
' Author:       Mattias Sjögren (mattias@mvps.org)
'               http://www.msjogren.net/dotnet/
'
' Description:  Defines a .NET friendly class, ShellShortcut, for reading
'               and writing shortcuts.
'               Define the conditional compilation symbol UNICODE to use
'               IShellLinkW internally.
'
' Public types: Class ShellShortcut
'
'
' Dependencies: ShellLinkNative.vb
'
'
' Copyright ©2001-2002, Mattias Sjögren
' 
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text


'
' .NET friendly wrapper for the ShellLink class
'
Class clsShortcut
    Implements IDisposable

    Private Const INFOTIPSIZE As Integer = 1024
    Private Const MAX_PATH As Integer = 260
    Private Const SW_SHOWNORMAL As Integer = 1
    Private Const SW_SHOWMINIMIZED As Integer = 2
    Private Const SW_SHOWMAXIMIZED As Integer = 3
    Private Const SW_SHOWMINNOACTIVE As Integer = 7
    Private m_Link As Shortcut.IShellLinkA
    Private m_sPath As String

    '
    ' linkPath: Path to new or existing shortcut file (.lnk).
    '
    Public Sub New(ByVal linkPath As String)

        Dim pf As Shortcut.IPersistFile
        Dim CLSID_ShellLink As Guid = New Guid("00021401-0000-0000-C000-000000000046")
        Dim tShellLink As Type

        ' Workaround for VB.NET compiler bug with ComImport classes
        '#If [UNICODE] Then
        '      m_Link = CType(New ShellLink(), IShellLinkW)
        '#Else
        '      m_Link = CType(New ShellLink(), IShellLinkA)
        '#End If
        tShellLink = Type.GetTypeFromCLSID(CLSID_ShellLink)
        m_Link = CType(Activator.CreateInstance(tShellLink), Shortcut.IShellLinkA)
        m_sPath = linkPath

        If File.Exists(linkPath) Then
            Try
                pf = CType(m_Link, Shortcut.IPersistFile)
                pf.Load(linkPath, 0)
            Catch ex As Exception
            End Try
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If m_Link Is Nothing Then Exit Sub
        Marshal.ReleaseComObject(m_Link)
        m_Link = Nothing
    End Sub


    '
    ' Gets or sets the argument list of the shortcut.
    '
    Public Property Arguments() As String
        Get
            Dim sb As StringBuilder = New StringBuilder(INFOTIPSIZE)
            m_Link.GetArguments(sb, sb.Capacity)
            Return sb.ToString()
        End Get
        Set(ByVal Value As String)
            m_Link.SetArguments(Value)
        End Set
    End Property

    '
    ' Gets or sets a description of the shortcut.
    '
    Public Property Description() As String
        Get
            Dim sb As StringBuilder = New StringBuilder(INFOTIPSIZE)
            m_Link.GetDescription(sb, sb.Capacity)
            Return sb.ToString()
        End Get
        Set(ByVal Value As String)
            m_Link.SetDescription(Value)
        End Set
    End Property

    '
    ' Gets or sets the working directory (aka start in directory) of the shortcut.
    '
    Public Property WorkingDirectory() As String
        Get
            Dim sb As StringBuilder = New StringBuilder(MAX_PATH)
            m_Link.GetWorkingDirectory(sb, sb.Capacity)
            Return sb.ToString()
        End Get
        Set(ByVal Value As String)
            m_Link.SetWorkingDirectory(Value)
        End Set
    End Property

    '
    ' If Path returns an empty string, the shortcut is associated with
    ' a PIDL instead, which can be retrieved with IShellLink.GetIDList().
    ' This is beyond the scope of this wrapper class.
    '
    ' Gets or sets the target path of the shortcut.
    '
    Public Property Path() As String
        Get
            Dim wfd As New Shortcut.WIN32_FIND_DATAA
            Dim sb As StringBuilder = New StringBuilder(MAX_PATH)
            m_Link.GetPath(sb, sb.Capacity, wfd, Shortcut.SLGP_FLAGS.SLGP_UNCPRIORITY)
            Return sb.ToString()
        End Get

        Set(ByVal Value As String)
            m_Link.SetPath(Value)
        End Set
    End Property

    '
    ' Gets or sets the path of the Icon assigned to the shortcut.
    '
    Public Property IconPath() As String
        Get
            Dim sb As StringBuilder = New StringBuilder(MAX_PATH)
            Dim nIconIdx As Integer
            m_Link.GetIconLocation(sb, sb.Capacity, nIconIdx)
            Return sb.ToString()
        End Get
        Set(ByVal Value As String)
            m_Link.SetIconLocation(Value, IconIndex)
        End Set
    End Property

    '
    ' Gets or sets the index of the Icon assigned to the shortcut.
    ' Set to zero when the IconPath property specifies a .ICO file.
    '
    Public Property IconIndex() As Integer
        Get
            Dim sb As StringBuilder = New StringBuilder(MAX_PATH)
            Dim nIconIdx As Integer
            m_Link.GetIconLocation(sb, sb.Capacity, nIconIdx)
            Return nIconIdx
        End Get
        Set(ByVal Value As Integer)
            m_Link.SetIconLocation(IconPath, Value)
        End Set
    End Property

    '
    ' Retrieves the Icon of the shortcut as it will appear in Explorer.
    ' Use the IconPath and IconIndex properties to change it.
    '
    Public ReadOnly Property Icon() As Icon
        Get
            Dim sb As StringBuilder = New StringBuilder(MAX_PATH)
            Dim nIconIdx As Integer
            Dim hIcon, hInst As IntPtr
            Dim ico, clone As Icon


            m_Link.GetIconLocation(sb, sb.Capacity, nIconIdx)

            hInst = Marshal.GetHINSTANCE(Me.GetType().Module)
            hIcon = Native.ExtractIcon(hInst, sb.ToString(), nIconIdx)
            If hIcon.ToInt32() = 0 Then Return Nothing

            ' Return a cloned Icon, because we have to free the original ourself.
            ico = System.Drawing.Icon.FromHandle(hIcon)
            clone = CType(ico.Clone(), Icon)
            ico.Dispose()
            Native.DestroyIcon(hIcon)
            Return clone

        End Get
    End Property

    '
    ' Gets or sets the System.Diagnostics.ProcessWindowStyle value
    ' that decides the initial show state of the shortcut target. Note that
    ' ProcessWindowStyle.Hidden is not a valid property value.
    '
    Public Property WindowStyle() As ProcessWindowStyle
        Get
            Dim nWS As Integer
            m_Link.GetShowCmd(nWS)

            Select Case nWS
                Case SW_SHOWMINIMIZED, SW_SHOWMINNOACTIVE
                    Return ProcessWindowStyle.Minimized
                Case SW_SHOWMAXIMIZED
                    Return ProcessWindowStyle.Maximized
                Case Else
                    Return ProcessWindowStyle.Normal
            End Select
        End Get

        Set(ByVal Value As ProcessWindowStyle)
            Dim nWS As Integer

            Select Case Value
                Case ProcessWindowStyle.Normal
                    nWS = SW_SHOWNORMAL
                Case ProcessWindowStyle.Minimized
                    nWS = SW_SHOWMINNOACTIVE
                Case ProcessWindowStyle.Maximized
                    nWS = SW_SHOWMAXIMIZED
                Case ProcessWindowStyle.Hidden
                    nWS = 4 'SW_SHOWNOACTIVE
                Case Else ' ProcessWindowStyle.Hidden
                    Throw New ArgumentException("Unsupported ProcessWindowStyle value.")
            End Select

            m_Link.SetShowCmd(nWS)
        End Set
    End Property

    '
    ' Saves the shortcut to disk.
    '
    Public Sub Save()
        Dim pf As Shortcut.IPersistFile = CType(m_Link, Shortcut.IPersistFile)
        pf.Save(m_sPath, True)
    End Sub

    '
    ' Returns a reference to the internal ShellLink object,
    ' which can be used to perform more advanced operations
    ' not supported by this wrapper class, by using the
    ' IShellLink interface directly.
    '
    Public ReadOnly Property ShellLink() As Object
        Get
            Return m_Link
        End Get
    End Property


#Region "Native Win32 API functions"
    Private Class Native

        <DllImport("shell32.dll", CharSet:=CharSet.Auto)>
        Public Shared Function ExtractIcon(ByVal hInst As IntPtr, ByVal lpszExeFileName As String, ByVal nIconIndex As Integer) As IntPtr
        End Function

        <DllImport("user32.dll")>
        Public Shared Function DestroyIcon(ByVal hIcon As IntPtr) As Boolean
        End Function

    End Class
#End Region

End Class

Namespace Shortcut

    ' IShellLink.Resolve fFlags
    <Flags()>
    Public Enum SLR_FLAGS
        SLR_NO_UI = &H1
        SLR_ANY_MATCH = &H2
        SLR_UPDATE = &H4
        SLR_NOUPDATE = &H8
        SLR_NOSEARCH = &H10
        SLR_NOTRACK = &H20
        SLR_NOLINKINFO = &H40
        SLR_INVOKE_MSI = &H80
    End Enum

    ' IShellLink.GetPath fFlags
    <Flags()>
    Public Enum SLGP_FLAGS
        SLGP_SHORTPATH = &H1
        SLGP_UNCPRIORITY = &H2
        SLGP_RAWPATH = &H4
    End Enum

    <StructLayoutAttribute(LayoutKind.Sequential, CharSet:=CharSet.Ansi)> Public Structure WIN32_FIND_DATAA
        Public dwFileAttributes As Integer
        Public ftCreationTime As ComTypes.FILETIME
        Public ftLastAccessTime As ComTypes.FILETIME
        Public ftLastWriteTime As ComTypes.FILETIME
        Public nFileSizeHigh As Integer
        Public nFileSizeLow As Integer
        Public dwReserved0 As Integer
        Public dwReserved1 As Integer
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_PATH)> Public cFileName As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=14)> Public cAlternateFileName As String
        Private Const MAX_PATH As Integer = 260
    End Structure

    <StructLayoutAttribute(LayoutKind.Sequential, CharSet:=CharSet.Unicode)> Public Structure WIN32_FIND_DATAW
        Public dwFileAttributes As Integer
        Public ftCreationTime As ComTypes.FILETIME
        Public ftLastAccessTime As ComTypes.FILETIME
        Public ftLastWriteTime As ComTypes.FILETIME
        Public nFileSizeHigh As Integer
        Public nFileSizeLow As Integer
        Public dwReserved0 As Integer
        Public dwReserved1 As Integer
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_PATH)> Public cFileName As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=14)> Public cAlternateFileName As String
        Private Const MAX_PATH As Integer = 260
    End Structure

    <
      ComImport(),
      InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
      Guid("0000010B-0000-0000-C000-000000000046")
    >
    Public Interface IPersistFile

#Region "Methods inherited from IPersist"

        Sub GetClassID(
          <Out()> ByRef pClassID As Guid)

#End Region

        <PreserveSig()>
        Function IsDirty() As Integer

        Sub Load(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszFileName As String,
          ByVal dwMode As Integer)

        Sub Save(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszFileName As String,
          <MarshalAs(UnmanagedType.Bool)> ByVal fRemember As Boolean)

        Sub SaveCompleted(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszFileName As String)

        Sub GetCurFile(
          ByRef ppszFileName As IntPtr)

    End Interface

    <
      ComImport(),
      InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
      Guid("000214EE-0000-0000-C000-000000000046")
    >
    Public Interface IShellLinkA

        Sub GetPath(
          <Out(), MarshalAs(UnmanagedType.LPStr)> ByVal pszFile As StringBuilder,
          ByVal cchMaxPath As Integer,
          <Out()> ByRef pfd As WIN32_FIND_DATAA,
          ByVal fFlags As SLGP_FLAGS)

        Sub GetIDList(
          ByRef ppidl As IntPtr)

        Sub SetIDList(
          ByVal pidl As IntPtr)

        Sub GetDescription(
          <Out(), MarshalAs(UnmanagedType.LPStr)> ByVal pszName As StringBuilder,
          ByVal cchMaxName As Integer)

        Sub SetDescription(
          <MarshalAs(UnmanagedType.LPStr)> ByVal pszName As String)

        Sub GetWorkingDirectory(
          <Out(), MarshalAs(UnmanagedType.LPStr)> ByVal pszDir As StringBuilder,
          ByVal cchMaxPath As Integer)

        Sub SetWorkingDirectory(
          <MarshalAs(UnmanagedType.LPStr)> ByVal pszDir As String)

        Sub GetArguments(
          <Out(), MarshalAs(UnmanagedType.LPStr)> ByVal pszArgs As StringBuilder,
          ByVal cchMaxPath As Integer)

        Sub SetArguments(
          <MarshalAs(UnmanagedType.LPStr)> ByVal pszArgs As String)

        Sub GetHotkey(
          ByRef pwHotkey As Short)

        Sub SetHotkey(
          ByVal wHotkey As Short)

        Sub GetShowCmd(
          ByRef piShowCmd As Integer)

        Sub SetShowCmd(
          ByVal iShowCmd As Integer)

        Sub GetIconLocation(
          <Out(), MarshalAs(UnmanagedType.LPStr)> ByVal pszIconPath As StringBuilder,
          ByVal cchIconPath As Integer,
          ByRef piIcon As Integer)

        Sub SetIconLocation(
          <MarshalAs(UnmanagedType.LPStr)> ByVal pszIconPath As String,
          ByVal iIcon As Integer)

        Sub SetRelativePath(
          <MarshalAs(UnmanagedType.LPStr)> ByVal pszPathRel As String,
          ByVal dwReserved As Integer)

        Sub Resolve(
          ByVal hwnd As IntPtr,
          ByVal fFlags As SLR_FLAGS)

        Sub SetPath(
          <MarshalAs(UnmanagedType.LPStr)> ByVal pszFile As String)

    End Interface

    <
      ComImport(),
      InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
      Guid("000214F9-0000-0000-C000-000000000046")
    >
    Public Interface IShellLinkW

        Sub GetPath(
          <Out(), MarshalAs(UnmanagedType.LPWStr)> ByVal pszFile As StringBuilder,
          ByVal cchMaxPath As Integer,
          <Out()> ByRef pfd As WIN32_FIND_DATAW,
          ByVal fFlags As SLGP_FLAGS)

        Sub GetIDList(
          ByRef ppidl As IntPtr)

        Sub SetIDList(
          ByVal pidl As IntPtr)

        Sub GetDescription(
          <Out(), MarshalAs(UnmanagedType.LPWStr)> ByVal pszName As StringBuilder,
          ByVal cchMaxName As Integer)

        Sub SetDescription(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszName As String)

        Sub GetWorkingDirectory(
          <Out(), MarshalAs(UnmanagedType.LPWStr)> ByVal pszDir As StringBuilder,
          ByVal cchMaxPath As Integer)

        Sub SetWorkingDirectory(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszDir As String)

        Sub GetArguments(
          <Out(), MarshalAs(UnmanagedType.LPWStr)> ByVal pszArgs As StringBuilder,
          ByVal cchMaxPath As Integer)

        Sub SetArguments(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszArgs As String)

        Sub GetHotkey(
          ByRef pwHotkey As Short)

        Sub SetHotkey(
          ByVal wHotkey As Short)

        Sub GetShowCmd(
          ByRef piShowCmd As Integer)

        Sub SetShowCmd(
          ByVal iShowCmd As Integer)

        Sub GetIconLocation(
          <Out(), MarshalAs(UnmanagedType.LPWStr)> ByVal pszIconPath As StringBuilder,
          ByVal cchIconPath As Integer,
          ByRef piIcon As Integer)

        Sub SetIconLocation(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszIconPath As String,
          ByVal iIcon As Integer)

        Sub SetRelativePath(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszPathRel As String,
          ByVal dwReserved As Integer)

        Sub Resolve(
          ByVal hwnd As IntPtr,
          ByVal fFlags As SLR_FLAGS)

        Sub SetPath(
          <MarshalAs(UnmanagedType.LPWStr)> ByVal pszFile As String)

    End Interface


    ' The following does currently not compile correctly. Use
    ' Type.GetTypeFromCLSID() and Activator.CreateInstance() instead.
    '
    '< _
    '  ComImport(), _
    '  Guid("00021401-0000-0000-C000-000000000046") _
    '> _
    'Public Class ShellLink
    '  'Implements IPersistFile
    '  'Implements IShellLinkA
    '  'Implements IShellLinkW
    'End Class

End Namespace
