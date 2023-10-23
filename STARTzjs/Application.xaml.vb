Imports System.Windows.Threading

Class Application

    Public Shared StartUpLocation As String = myFolder.Path(System.Reflection.Assembly.GetExecutingAssembly().Location)
    Public Shared VersionNo As Integer = CInt(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileBuildPart)
    Public Shared Version As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileBuildPart
    Public Shared LegalCopyright As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).LegalCopyright
    Public Shared CompanyName As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).CompanyName
    Public Shared ProductName As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName
    Public Shared ExeName As String = myFile.Name(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).InternalName, False)
    Public Shared ProcessName As String = Diagnostics.Process.GetCurrentProcess.ProcessName
    'dneska ani tak nejde o to byt jako se mit
    Public Const Pass As String = Chr(100) & Chr(110) & Chr(101) & Chr(115) & Chr(107) & Chr(97) & Chr(32) & Chr(97) & Chr(110) & Chr(105) & Chr(32) & Chr(116) & Chr(97) & Chr(107) & Chr(32) & Chr(110) & Chr(101) & Chr(106) & Chr(100) & Chr(101) & Chr(32) & Chr(111) & Chr(32) & Chr(116) & Chr(111) & Chr(32) & Chr(98) & Chr(121) & Chr(116) & Chr(32) & Chr(106) & Chr(97) & Chr(107) & Chr(111) & Chr(32) & Chr(115) & Chr(101) & Chr(32) & Chr(109) & Chr(105) & Chr(116)
    Public Shared selType As Integer = 1
    Public Shared oneProcess As Boolean = True
    Public Shared winStore As Boolean = True

    Public Class myGlobal
        '"dneskanebozitra"
        Public Const NR As String = Chr(13) & Chr(10)
        Public Const imgCesta As String = "/STARTzjs;component/Images/"
        Public Const imgCestaA As String = "pack://application:,,,/Images/"
        Public Const regCesta As String = "Software\Microsoft\Windows\CurrentVersion\"
        Public Shared DateNull As New Date(1, 1, 1)
        Public Shared appCesta As String
        Public Shared TransMultiple As Integer = 50
        Public Shared ItemSize(3) As Size
        Public Shared Dat As clsData
        Public Shared BoxColumnList As clsBoxColumnList
        Public Shared WatchFolder As New clsWatchFolder
        Public Shared myAutostart As New clsAutostart
        Public Shared WithEvents mySystem As New clsSystem
        Public Shared myWinColor As clsWinColors
        Public Shared myApps As clsApps

        Public Enum BoxType
            File = 0
            Folder = 1
            Start = 3
        End Enum

    End Class

#Region " Window "

    Public Shared ReadOnly Property Icon As ImageSource
        Get
            Return myBitmap.UriToImageSource(New Uri("/" + ExeName + ";component/" + ExeName + ".ico", UriKind.Relative))
        End Get
    End Property

    Public Shared Function SettingWindow() As wpfSetting
        For Each wOne As Window In Application.Current.Windows
            If wOne.Name = "wSetting" Then Return CType(wOne, wpfSetting)
        Next
        Return Nothing
    End Function

    Public Shared Function Title() As String
        Return ProductName + " " + Version
    End Function

#End Region

#Region " Dispatcher "
    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.
    Private bError As Boolean

    Private Sub App_DispatcherUnhandledException(ByVal sender As Object, ByVal e As DispatcherUnhandledExceptionEventArgs) Handles MyClass.DispatcherUnhandledException
        'Process unhandled exception
        If bError Then Exit Sub
        bError = True
        e.Handled = True

        Dim Form As New wpfError
        Form.myError = e
        Form.ShowDialog()

        End
    End Sub
#End Region

End Class
