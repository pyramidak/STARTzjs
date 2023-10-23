Imports System.Runtime.InteropServices

#Region " HotKeyEvent "



Public Class HotKeyEventArgs

    Inherits EventArgs
    Public Property HotKey() As HotKey
        Get
            Return m_HotKey
        End Get
        Private Set(value As HotKey)
            m_HotKey = value
        End Set
    End Property
    Private m_HotKey As HotKey

    Public Sub New(hotKey__1 As HotKey)
        HotKey = hotKey__1
    End Sub
End Class

#End Region

#Region " Serializable "

<Serializable()>
Public Class HotKeyAlreadyRegisteredException
    Inherits Exception
    Public Property HotKey() As HotKey
        Get
            Return m_HotKey
        End Get
        Private Set(value As HotKey)
            m_HotKey = value
        End Set
    End Property
    Private m_HotKey As HotKey
    Public Sub New(message As String, hotKey__1 As HotKey)
        MyBase.New(message)
        HotKey = hotKey__1
    End Sub
    Public Sub New(message As String, hotKey__1 As HotKey, inner As Exception)
        MyBase.New(message, inner)
        HotKey = hotKey__1
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
End Class

''' <summary>
''' Represents an hotKey
''' </summary>
<Serializable()>
Public Class HotKey
    Implements ComponentModel.INotifyPropertyChanged
    Implements Runtime.Serialization.ISerializable
    Implements IEquatable(Of HotKey)

    ''' <summary>
    ''' Creates an HotKey object. This instance has to be registered in an HotKeyHost.
    ''' </summary>
    Public Sub New()
    End Sub

    ''' <summary>
    ''' Creates an HotKey object. This instance has to be registered in an HotKeyHost.
    ''' </summary>
    ''' <param name="key">The key</param>
    ''' <param name="modifiers">The modifier. Multiple modifiers can be combined with or.</param>
    Public Sub New(key As Key, modifiers As ModifierKeys)
        Me.New(key, modifiers, True)
    End Sub

    ''' <summary>
    ''' Creates an HotKey object. This instance has to be registered in an HotKeyHost.
    ''' </summary>
    ''' The key
    ''' The modifier. Multiple modifiers can be combined with or.
    ''' Specifies whether the HotKey will be enabled when registered to an HotKeyHost
    Public Sub New(key__1 As Key, modifiers__2 As ModifierKeys, enabled__3 As Boolean)
        Key = key__1
        Modifiers = modifiers__2
        Enabled = enabled__3
    End Sub


    Private m_key As Key
    ''' <summary>
    ''' The Key. Must not be null when registering to an HotKeyHost.
    ''' </summary>
    Public Property Key() As Key
        Get
            Return m_key
        End Get
        Set(value As Key)
            If m_key <> value Then
                m_key = value
                OnPropertyChanged("Key")
            End If
        End Set
    End Property

    Private m_modifiers As ModifierKeys
    ''' <summary>
    ''' The modifier. Multiple modifiers can be combined with or.
    ''' </summary>
    Public Property Modifiers() As ModifierKeys
        Get
            Return m_modifiers
        End Get
        Set(value As ModifierKeys)
            If m_modifiers <> value Then
                m_modifiers = value
                OnPropertyChanged("Modifiers")
            End If
        End Set
    End Property

    Private m_enabled As Boolean
    Public Property Enabled() As Boolean
        Get
            Return m_enabled
        End Get
        Set(value As Boolean)
            If value <> m_enabled Then
                m_enabled = value
                OnPropertyChanged("Enabled")
            End If
        End Set
    End Property

    Public Event PropertyChanged As ComponentModel.PropertyChangedEventHandler Implements ComponentModel.INotifyPropertyChanged.PropertyChanged

    Protected Overridable Sub OnPropertyChanged(propertyName As String)
        RaiseEvent PropertyChanged(Me, New ComponentModel.PropertyChangedEventArgs(propertyName))
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim hotKey As HotKey = TryCast(obj, HotKey)
        If hotKey IsNot Nothing Then
            Return Equals(hotKey)
        Else
            Return False
        End If
    End Function

    Public Overloads Function Equals(other As HotKey) As Boolean Implements System.IEquatable(Of HotKey).Equals
        Return (Key = other.Key AndAlso Modifiers = other.Modifiers)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return CInt(Modifiers) + 10 * CInt(Key)
    End Function

    Public Overrides Function ToString() As String
        Return String.Format("{0} + {1} ({2}Enabled)", Key, Modifiers, If(Enabled, "", "Not "))
    End Function

    ''' <summary>
    ''' Will be raised if the hotkey is pressed (works only if registed in HotKeyHost)
    ''' </summary>
    Public Event HotKeyPressed As EventHandler(Of HotKeyEventArgs)

    Protected Overridable Sub OnHotKeyPress()
        RaiseEvent HotKeyPressed(Me, New HotKeyEventArgs(Me))
    End Sub

    Friend Sub RaiseOnHotKeyPressed()
        OnHotKeyPress()
    End Sub

    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        Key = DirectCast(info.GetValue("Key", GetType(Key)), Key)
        Modifiers = DirectCast(info.GetValue("Modifiers", GetType(ModifierKeys)), ModifierKeys)
        Enabled = info.GetBoolean("Enabled")
    End Sub

    Public Overridable Sub GetObjectData(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
        info.AddValue("Key", Key, GetType(Key))
        info.AddValue("Modifiers", Modifiers, GetType(ModifierKeys))
        info.AddValue("Enabled", Enabled)
    End Sub

End Class

#End Region

#Region " HotKeyHost "

''' <summary>
''' The HotKeyHost needed for working with hotKeys.
''' </summary>
Public NotInheritable Class HotKeyHost
    Implements IDisposable

    ''' <summary>
    ''' Creates a new HotKeyHost
    ''' </summary>
    ''' The handle of the window. Must not be null.
    Public Sub New(wnd As Window)
        Dim wMainPtr As IntPtr = New System.Windows.Interop.WindowInteropHelper(wnd).Handle
        Dim hwndSource As System.Windows.Interop.HwndSource = System.Windows.Interop.HwndSource.FromHwnd(wMainPtr)

        If hwndSource Is Nothing Then
            Throw New ArgumentNullException("hwndSource")
        End If

        Me.hook = New Interop.HwndSourceHook(AddressOf WndProc)
        Me.hwndSource = hwndSource
        hwndSource.AddHook(hook)
    End Sub

#Region "HotKey Interop"

    Private Const WM_HotKey As Integer = 786

    Private Declare Ansi Function RegisterHotKey Lib "user32" (hwnd As IntPtr, id As Integer, modifiers As Integer, key As Integer) As Integer

    Private Declare Ansi Function UnregisterHotKey Lib "user32" (hwnd As IntPtr, id As Integer) As Integer

#End Region

#Region "Interop-Encapsulation"

    Private hook As Interop.HwndSourceHook
    Private hwndSource As Interop.HwndSource

    Private Sub RegisterHotKey(id As Integer, hotKey As HotKey)
        If CInt(hwndSource.Handle) <> 0 Then
            RegisterHotKey(hwndSource.Handle, id, CInt(hotKey.Modifiers), KeyInterop.VirtualKeyFromKey(hotKey.Key))
            Dim [error] As Integer = Marshal.GetLastWin32Error()
            If [error] <> 0 Then
                Dim e As Exception = New ComponentModel.Win32Exception([error])

                If [error] = 1409 Then
                    'Throw New HotKeyAlreadyRegisteredException(e.Message, hotKey, e)
                Else
                    Throw e
                End If
            End If
        Else
            Throw New InvalidOperationException("Handle is invalid")
        End If
    End Sub

    Private Sub UnregisterHotKey(id As Integer)
        If CInt(hwndSource.Handle) <> 0 Then
            UnregisterHotKey(hwndSource.Handle, id)
            Dim [error] As Integer = Marshal.GetLastWin32Error()
            If [error] <> 0 Then
                'Throw New ComponentModel.Win32Exception([error])
            End If
        End If
    End Sub

#End Region

    ''' <summary>
    ''' Will be raised if any registered hotKey is pressed
    ''' </summary>
    Public Event HotKeyPressed As EventHandler(Of HotKeyEventArgs)

    Private Function WndProc(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        If msg = WM_HotKey Then
            If m_hotKeys.ContainsKey(CInt(wParam)) Then
                Dim h As HotKey = m_hotKeys(CInt(wParam))
                h.RaiseOnHotKeyPressed()
                RaiseEvent HotKeyPressed(Me, New HotKeyEventArgs(h))
            End If
        End If

        Return New IntPtr(0)
    End Function


    Private Sub hotKey_PropertyChanged(sender As Object, e As ComponentModel.PropertyChangedEventArgs)
        Dim kvPair = m_hotKeys.FirstOrDefault(Function(h) h.Value Is sender)

        If kvPair.Value IsNot Nothing Then
            If e.PropertyName = "Enabled" Then
                If kvPair.Value.Enabled Then
                    RegisterHotKey(kvPair.Key, kvPair.Value)
                Else
                    UnregisterHotKey(kvPair.Key)
                End If
            ElseIf e.PropertyName = "Key" OrElse e.PropertyName = "Modifiers" Then
                If kvPair.Value.Enabled Then
                    UnregisterHotKey(kvPair.Key)
                    RegisterHotKey(kvPair.Key, kvPair.Value)
                End If
            End If
        End If
    End Sub


    Private m_hotKeys As New Dictionary(Of Integer, HotKey)()


    Public Class SerialCounter
        Public Sub New(start As Integer)
            Current = start
        End Sub

        Public Property Current() As Integer
            Get
                Return m_Current
            End Get
            Private Set(value As Integer)
                m_Current = value
            End Set
        End Property
        Private m_Current As Integer

        Public Function [Next]() As Integer
            Return System.Threading.Interlocked.Increment(Current)
        End Function
    End Class

    ''' <summary>
    ''' All registered hotKeys
    ''' </summary>
    Public ReadOnly Property HotKeys() As IEnumerable(Of HotKey)
        Get
            Return m_hotKeys.Values
        End Get
    End Property


    Private Shared ReadOnly idGen As New SerialCounter(1)
    'Annotation: Can be replaced with "Random"-class
    ''' <summary>
    ''' Adds an hotKey.
    ''' </summary>
    ''' <param name="hotKey">The hotKey which will be added. Must not be null and can be registed only once.</param>
    Public Sub AddHotKey(hotKey As HotKey)
        If hotKey Is Nothing Then
            Throw New ArgumentNullException("value")
        End If
        If hotKey.Key = 0 Then
            Throw New ArgumentNullException("value.Key")
        End If
        If m_hotKeys.ContainsValue(hotKey) Then
            Throw New HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey)
        End If

        Dim id As Integer = idGen.[Next]()
        If hotKey.Enabled Then
            RegisterHotKey(id, hotKey)
        End If
        AddHandler hotKey.PropertyChanged, AddressOf hotKey_PropertyChanged
        m_hotKeys(id) = hotKey
    End Sub

    ''' <summary>
    ''' Removes an hotKey
    ''' </summary>
    ''' <param name="hotKey">The hotKey to be removed</param>
    ''' <returns>True if success, otherwise false</returns>
    Public Function RemoveHotKey(hotKey As HotKey) As Boolean
        Dim kvPair = m_hotKeys.FirstOrDefault(Function(h) h.Value Is hotKey)

        If kvPair.Value IsNot Nothing Then
            RemoveHandler kvPair.Value.PropertyChanged, AddressOf hotKey_PropertyChanged
            If kvPair.Value.Enabled Then
                UnregisterHotKey(kvPair.Key)
            End If
            Return m_hotKeys.Remove(kvPair.Key)
        End If
        Return False
    End Function

#Region "Destructor"

    Private disposed As Boolean

    Private Sub Dispose(disposing As Boolean)
        If disposed Then
            Return
        End If

        If disposing Then
            hwndSource.RemoveHook(hook)
        End If

        For i As Integer = m_hotKeys.Count - 1 To 0 Step -1
            RemoveHotKey(m_hotKeys.Values.ElementAt(i))
        Next

        disposed = True
    End Sub

    Public Sub Dispose() Implements System.IDisposable.Dispose
        Me.Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overrides Sub Finalize()
        Try
            Me.Dispose(False)
        Finally
            MyBase.Finalize()
        End Try
    End Sub

#End Region

End Class

#End Region

