Imports System.Collections.ObjectModel

Public Class wpfProcess

    Private Lge As Boolean = mySystem.LgeCzech
    Private myKolekce As New clsKolekce
    Property Kolekce() As clsKolekce
        Get
            Return myKolekce
        End Get
        Set(ByVal value As clsKolekce)
            myKolekce = value
        End Set
    End Property

    Private iID As Integer
    Property ProcessID() As Integer
        Get
            Return iID
        End Get
        Set(ByVal value As Integer)
            iID = value
        End Set
    End Property

    Private Sub Window_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Me.Title = If(Lge, "Vyberte spuštěný proces", "Select running process")
        chdID.Header = If(Lge, "ID procesu", "Process ID")
        chdName.Header = If(Lge, "Jméno procesu", "Process Name")
        chdPriority.Header = If(Lge, "Priorita", "Priority")
        chdTitle.Header = If(Lge, "Titulek hlavního okna", "Main Window Title")
    End Sub

    Private Sub lvwProcess_SelectionChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles lvwProcess.SelectionChanged
        iID = CType(lvwProcess.SelectedItem, clsProcess).ID
        Me.Close()
    End Sub

    Class clsKolekce
        Inherits Collection(Of clsProcess)

        Sub New()
            Dim allProcesses(), thisProcess As Process
            allProcesses = System.Diagnostics.Process.GetProcesses
            For Each thisProcess In allProcesses
                If Not thisProcess.MainWindowTitle = "" Then
                    Me.Add(New clsProcess(thisProcess))
                End If
            Next
        End Sub
    End Class

    Class clsProcess
        Private iID As Integer
        Private sName As String
        Private sPriority As String
        Private sTitle As String

        Sub New(ByVal Proces As Process)
            iID = 0 : sName = "" : sPriority = "" : sTitle = ""
            Try
                iID = Proces.Id
                sName = Proces.ProcessName
                sPriority = Proces.PriorityClass.ToString
                sTitle = Proces.MainWindowTitle
            Catch
            End Try
        End Sub

        Public Property ID() As Integer
            Get
                Return iID
            End Get
            Set(ByVal value As Integer)
                iID = value
            End Set
        End Property

        Public Property Name() As String
            Get
                Return sName
            End Get
            Set(ByVal value As String)
                sName = value
            End Set
        End Property

        Public Property Priority() As String
            Get
                Return sPriority
            End Get
            Set(ByVal value As String)
                sPriority = value
            End Set
        End Property

        Public Property Title() As String
            Get
                Return sTitle
            End Get
            Set(ByVal value As String)
                sTitle = value
            End Set
        End Property
    End Class

End Class
