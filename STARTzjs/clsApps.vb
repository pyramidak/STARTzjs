Imports Microsoft.WindowsAPICodePack.Shell

Public Class clsApps
    Public Class clsApp
        Public Name As String
        Public Path As String
        Public Image As ImageSource
        Public Icon As System.Drawing.Icon
    End Class

    Private appFolder As IKnownFolder
    Private winStore As Boolean
    Private LastCount As Integer
    Private listApp As List(Of clsApp)
    Private listPath As List(Of String)
    Public Count As Integer

    Sub New(Optional paths As List(Of String) = Nothing, Optional store As Boolean = True)
        Dim FODLERID_AppsFolder As New Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}")
        Dim appsFolder As ShellObject = CType(KnownFolderHelper.FromKnownFolderId(FODLERID_AppsFolder), ShellObject)
        appFolder = CType(appsFolder, IKnownFolder)
        winStore = store

        If paths IsNot Nothing Then
            listPath = paths
            All(winStore, paths)
        End If
    End Sub

    Public Function Exist(Path As String) As Boolean
        If LastCount <> appFolder.Count Then All(winStore, listPath)
        For Each app In listApp
            If app.Path = Path Then Return True
        Next
        Return False
    End Function

    Public Function Find(Path As String) As clsApp
        If LastCount <> appFolder.Count Then All(winStore, listPath)
        For Each app In listApp
            If app.Path = Path Then
                Return app
            End If
        Next
        Return Nothing
    End Function

    Public Function All(Optional winStore As Boolean = True, Optional paths As List(Of String) = Nothing) As List(Of clsApp)
        If LastCount <> appFolder.Count Then
            listApp = New List(Of clsApp)
            For Each app In appFolder
                Dim OKstore As Boolean = False
                Dim OKpath As Boolean = False
                If winStore = False Then OKstore = True
                If winStore AndAlso app.ParsingName.ToLower.EndsWith("!app") And Not app.ParsingName.StartsWith("Microsoft") Then OKstore = True
                'If app.ParsingName Like "*!Whats*" Then MsgBox("found")
                Select Case app.ParsingName 'Výjimky přidat
                    Case "Microsoft.ScreenSketch_8wekyb3d8bbwe!App"
                        OKstore = True
                    Case "Microsoft.WindowsStore_8wekyb3d8bbwe!App"
                        OKstore = True
                    Case "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App"
                        OKstore = True
                    Case "Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App"
                        OKstore = True
                    Case "ArduinoLLC.ArduinoIDE_mdqgnx93n4wtt!ArduinoIDE"
                        OKstore = True
                    Case "5319275A.WhatsAppDesktop_cv1g1gvanyjgm!WhatsAppDesktop"
                        OKstore = True
                End Select
                If paths Is Nothing Then OKpath = True
                If paths IsNot Nothing AndAlso paths.Any(Function(x) x = app.ParsingName) Then OKpath = True

                If OKstore And OKpath Then listApp.Add(newApp(app))
            Next
            Count = listApp.Count
            LastCount = appFolder.Count
            Return listApp
        Else
            Return listApp
        End If
    End Function

    Private Function newApp(app As ShellObject) As clsApp
        Dim cApp As New clsApp
        cApp.Name = app.Name
        cApp.Path = app.ParsingName
        cApp.Image = app.Thumbnail.MediumBitmapSource
        cApp.Icon = app.Thumbnail.MediumIcon
        Return cApp
    End Function

End Class