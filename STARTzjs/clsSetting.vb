Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Diagnostics.Eventing.Reader

Public Class clsSetting

    Sub New()
    End Sub

    Public Lock As Integer = 0
    Public NewVersion As Integer
    Public LastStart As Date = DateNull
    Public LastRestart As Date = DateNull
    Public AutoUpdate As Boolean = True
    Public AutoStart As Boolean = True
    Public ItemLook As Integer
    Public MenuColor As Integer
    Public OwnColor As Color = Color.FromArgb(200, 0, 99, 255)
    Public ItemsSize As Integer = 1
    Public Transparency As Integer = 4
    Public ItemsHidden As Boolean = False

    'Kalkulačka
    Public CalcSmall As Boolean = False
    Public CalcFront As Boolean = False
    Public CalcMem As New ArrayList

    'Budík
    Public AlarmMelody As String
    Public AlarmFolder As String
    Public AlarmEnabled As Boolean = False
    Public AlarmMem As New ObservableCollection(Of clsAlarm)
    Public Email As String
    Public EmailPass As String

    'MQTT
    Public BrokerIP As String
    Public BrokerPort As Integer
    Public BrokerUser As String
    Public BrokerPass As String

#Region " Alarm "

    Public Class clsAlarm
        Implements INotifyPropertyChanged

        Private sTask, sData1, sData3, sMsg, sWhen1, sWhen4, sWatch As String
        Private iWhen2, iData2, iID As Integer
        Private dWhen3 As Double
        Private bOpakovat, bAktivni, bExecute As Boolean
        Private dLast As Date

#Region " Get/Set "
        Public Property ID() As Integer
            Get
                Return iID
            End Get
            Set(ByVal value As Integer)
                iID = value
                OnPropertyChanged("ID")
            End Set
        End Property

        Public Property Last() As Date
            Get
                Return dLast
            End Get
            Set(ByVal value As Date)
                dLast = value
            End Set
        End Property

        Public Property Execute() As Boolean
            Get
                Return bExecute
            End Get
            Set(ByVal value As Boolean)
                bExecute = value
            End Set
        End Property

        Public Property Opakovat() As Boolean
            Get
                Return bOpakovat
            End Get
            Set(ByVal value As Boolean)
                bOpakovat = value
                OnPropertyChanged("Opakovat")
            End Set
        End Property

        Public Property Aktivni() As Boolean
            Get
                Return bAktivni
            End Get
            Set(ByVal value As Boolean)
                bAktivni = value
                dLast = Now
                OnPropertyChanged("Aktivni")
            End Set
        End Property

        Public Property Task() As String
            Get
                Return sTask
            End Get
            Set(ByVal value As String)
                sTask = value
                OnPropertyChanged("Task")
            End Set
        End Property

        Public Property Watch() As String
            Get
                Return sWatch
            End Get
            Set(ByVal value As String)
                sWatch = value
                OnPropertyChanged("Watch")
            End Set
        End Property

        Public Property Data1() As String
            Get
                Return sData1
            End Get
            Set(ByVal value As String)
                sData1 = value
                OnPropertyChanged("sData1")
            End Set
        End Property

        Public Property Data2() As Integer
            Get
                Return iData2
            End Get
            Set(ByVal value As Integer)
                iData2 = value
            End Set
        End Property

        Public Property Data3() As String
            Get
                Return sData3
            End Get
            Set(ByVal value As String)
                sData3 = value
            End Set
        End Property

        Public Property Message() As String
            Get
                Return sMsg
            End Get
            Set(ByVal value As String)
                sMsg = value
                OnPropertyChanged("Message")
            End Set
        End Property

        Public Property When1() As String
            Get
                Return sWhen1
            End Get
            Set(ByVal value As String)
                sWhen1 = value
                OnPropertyChanged("When1")
            End Set
        End Property

        Public Property When2() As Integer
            Get
                Return iWhen2
            End Get
            Set(ByVal value As Integer)
                iWhen2 = value
            End Set
        End Property

        Public Property When3() As Double
            Get
                Return dWhen3
            End Get
            Set(ByVal value As Double)
                dWhen3 = value
            End Set
        End Property
        Public Property When4() As String
            Get
                Return sWhen4
            End Get
            Set(ByVal value As String)
                sWhen4 = value
            End Set
        End Property

#End Region

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub OnPropertyChanged(ByVal name As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End Sub

        Sub New()
        End Sub

    End Class

    Public Sub CorrectTaskNames()
        For Each Alarm In AlarmMem
            If IsNumeric(Alarm.Task) Then Alarm.Task = AlarmTaskNoToText(Alarm.Task)
        Next
    End Sub

    Private Function AlarmTaskNoToText(sTask As String) As String
        Select Case CInt(sTask)
            Case 0
                Return "message"
            Case 10
                Return "shutdown"
            Case 20
                Return "run"
            Case 31
                Return "close0"
            Case 32
                Return "close1"
            Case 33
                Return "close2"
            Case 40
                Return "email"
            Case 50
                Return "alarm"
            Case 60
                Return "copy"
            Case 70
                Return "display"
            Case 80
                Return "mqtt"
            Case Else
                Return "message"
        End Select
    End Function

#End Region

    'Sloupce/témata
    Public Columns As New Collection(Of clsColumn)
    Public Sub AddColumn(Name As String, Hidden As Boolean, Sorted As Boolean)
        Columns.Add(New clsColumn(Name, Hidden, Sorted))
    End Sub

#Region " Column "

    Public Class clsColumn
        Implements INotifyPropertyChanged

        Private sName As String
        Private bHidden, bSorted As Boolean

#Region " Get/Set "

        Public Property Name() As String
            Get
                Return sName
            End Get
            Set(ByVal value As String)
                sName = value
                OnPropertyChanged("Name")
            End Set
        End Property

        Public Property Hidden() As Boolean
            Get
                Return bHidden
            End Get
            Set(ByVal value As Boolean)
                bHidden = value
                OnPropertyChanged("Hidden")
            End Set
        End Property

        Public Property Sorted() As Boolean
            Get
                Return bSorted
            End Get
            Set(ByVal value As Boolean)
                bSorted = value
                OnPropertyChanged("Sorted")
            End Set
        End Property

#End Region

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub OnPropertyChanged(ByVal name As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End Sub

        Sub New()
        End Sub

        Sub New(Name_ As String, Hidden_ As Boolean, Sorted_ As Boolean)
            sName = Name_ : bHidden = Hidden_ : bSorted = Sorted_
        End Sub

    End Class

#End Region

    'Položka/program
    Public Items As New Collection(Of clsItem)
    Public Sub AddItem(Name As String, Column As String, Path As String, IconPath As String, IconIndex As Integer, ModKey As ModifierKeys, InputKey As Key, BoxType As BoxType, Hidden As Boolean, Highlighted As Boolean, Admin As Boolean, HideStart As Boolean)
        Items.Add(New clsItem(Name, Column, Path, IconPath, IconIndex, ModKey, InputKey, BoxType, Hidden, Highlighted, Admin, HideStart))
    End Sub


#Region " Item "

    Public Class clsItem
        Implements INotifyPropertyChanged

        Private sName, sColumn, sPath As String
        Dim sIconPath As String = ""
        Private iIconIndex As Integer
        Private bHidden, bHighlighted, bAdmin, bHidestart As Boolean
        Private eBoxType As BoxType
        Private eInputKey As Key
        Private eModKey As ModifierKeys

#Region " Get/Set "

        Public Property Name() As String
            Get
                Return sName
            End Get
            Set(ByVal value As String)
                sName = value
                OnPropertyChanged("Name")
            End Set
        End Property

        Public Property Column() As String
            Get
                Return sColumn
            End Get
            Set(ByVal value As String)
                sColumn = value
                OnPropertyChanged("Column")
            End Set
        End Property

        Public Property Path() As String
            Get
                Return sPath
            End Get
            Set(ByVal value As String)
                sPath = value
                OnPropertyChanged("Path")
            End Set
        End Property

        Public Property IconPath() As String
            Get
                Return sIconPath
            End Get
            Set(ByVal value As String)
                sIconPath = value
                OnPropertyChanged("IconPath")
            End Set
        End Property

        Public Property IconIndex() As Integer
            Get
                Return iIconIndex
            End Get
            Set(ByVal value As Integer)
                iIconIndex = value
                OnPropertyChanged("IconIndex")
            End Set
        End Property

        Public Property InputKey() As Key
            Get
                Return eInputKey
            End Get
            Set(ByVal value As Key)
                eInputKey = value
                OnPropertyChanged("HotKey")
            End Set
        End Property

        Public Property ModKey() As ModifierKeys
            Get
                Return eModKey
            End Get
            Set(ByVal value As ModifierKeys)
                eModKey = value
                OnPropertyChanged("ModKey")
            End Set
        End Property

        Public Property BoxType() As BoxType
            Get
                Return eBoxType
            End Get
            Set(ByVal value As BoxType)
                eBoxType = value
                OnPropertyChanged("BoxType")
            End Set
        End Property

        Public Property Hidden() As Boolean
            Get
                Return bHidden
            End Get
            Set(ByVal value As Boolean)
                bHidden = value
                OnPropertyChanged("Hidden")
            End Set
        End Property

        Public Property Highlighted() As Boolean
            Get
                Return bHighlighted
            End Get
            Set(ByVal value As Boolean)
                bHighlighted = value
                OnPropertyChanged("Highlighted")
            End Set
        End Property

        Public Property Admin() As Boolean
            Get
                Return bAdmin
            End Get
            Set(ByVal value As Boolean)
                bAdmin = value
                OnPropertyChanged("Admin")
            End Set
        End Property

        Public Property HideStart() As Boolean
            Get
                Return bHidestart
            End Get
            Set(ByVal value As Boolean)
                bHidestart = value
                OnPropertyChanged("HideStart")
            End Set
        End Property

#End Region

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub OnPropertyChanged(ByVal name As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End Sub

        Sub New()
        End Sub

        Sub New(Name_ As String, Column_ As String, Path_ As String, IconPath_ As String, IconIndex_ As Integer, ModKey_ As ModifierKeys, InputKey_ As Key, BoxType_ As BoxType, Hidden_ As Boolean, Highlighted_ As Boolean, Admin_ As Boolean, HideStart_ As Boolean)
            sName = Name_ : sColumn = Column_ : sPath = Path_ : sIconPath = IconPath_ : iIconIndex = IconIndex_ : eModKey = ModKey_ : eInputKey = InputKey_ : eBoxType = BoxType_ : bHidden = Hidden_ : bHighlighted = Highlighted_ : bAdmin = Admin_ : bHidestart = HideStart_
        End Sub

    End Class

#End Region

End Class


