Imports System.Net.Configuration
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks
Imports MQTTnet
Imports MQTTnet.Client
Public Class clsMQTT

    Private myOptions As Options.MqttClientOptionsBuilder
    Private myClient As MqttClient
    Private myFactory As New MqttFactory
    Private LastError As String

    Public IsConnectedWanted As Boolean
    Public Event Err(Message As String, Task As TaskErr)
    Public Event Connected()
    Public Event Disconnected()
    Public Event Recieved(Topic As String, Payload As String)

    Enum TaskErr
        Connect
        Disconnect
        Reconnect
        Publish
        Subscribe
        Unsubscribe
    End Enum

    Public ReadOnly Property IsConnected As Boolean
        Get
            Return myClient IsNot Nothing AndAlso myClient.IsConnected
        End Get
    End Property

    Sub New()
    End Sub
    Sub New(Server As String, Port As Integer, User As String, Password As String)
        Create(Server, Port, User, Password)
        Connect.GetAwaiter()
    End Sub

    Public Sub Create(Server As String, Port As Integer, User As String, Password As String)
        myClient = CType(myFactory.CreateMqttClient(), MqttClient)
        myClient.UseApplicationMessageReceivedHandler(AddressOf MessageRecieved)
        myClient.UseDisconnectedHandler(AddressOf ConnectionClosed)
        myClient.UseConnectedHandler(AddressOf ConnectionOpened)
        myOptions = New Options.MqttClientOptionsBuilder
        myOptions.WithClientId(Environment.MachineName).WithTcpServer(Server, Port)
        If Not User = "" Then myOptions.WithCredentials(User, Password)
    End Sub
    Public Async Function Connect() As Task
        Try
            Await myClient.ConnectAsync(myOptions.Build).ConfigureAwait(False)
            IsConnectedWanted = True
        Catch ex As Exception
            If LastError <> ex.Message Then
                LastError = ex.Message
                RaiseEvent Err(ex.Message, TaskErr.Connect)
            End If
        End Try
    End Function

    Public Async Function Disconnect() As Task
        Try
            IsConnectedWanted = False
            Await myClient.DisconnectAsync().ConfigureAwait(False)
        Catch ex As Exception
            If LastError <> ex.Message Then
                LastError = ex.Message
                RaiseEvent Err(ex.Message, TaskErr.Disconnect)
            End If
        End Try
    End Function

    Public Async Function Reconnect() As Task
        Try
            Await myClient.ReconnectAsync().ConfigureAwait(False)
        Catch ex As Exception
            If LastError <> ex.Message Then
                LastError = ex.Message
                RaiseEvent Err(ex.Message, TaskErr.Reconnect)
            End If
        End Try
    End Function

    Public Async Function Publish(Topic As String, Payload As String) As Task
        Try
            Await myClient.PublishAsync(Topic, Payload).ConfigureAwait(False)
        Catch ex As Exception
            If LastError <> ex.Message Then
                LastError = ex.Message
                RaiseEvent Err(ex.Message, TaskErr.Publish)
            End If
        End Try
    End Function

    Public Async Function Subscribe(Topic As String) As Task
        Try
            Await myClient.SubscribeAsync(Topic).ConfigureAwait(False)
        Catch ex As Exception
            If LastError <> ex.Message Then
                LastError = ex.Message
                RaiseEvent Err(ex.Message, TaskErr.Subscribe)
            End If
        End Try
    End Function

    Public Async Function Unsubscribe(Topics As String()) As Task
        Try
            Await myClient.UnsubscribeAsync(Topics).ConfigureAwait(False)
        Catch ex As Exception
            If LastError <> ex.Message Then
                LastError = ex.Message
                RaiseEvent Err(ex.Message, TaskErr.Unsubscribe)
            End If
        End Try
    End Function

    Private Sub MessageRecieved(e As MqttApplicationMessageReceivedEventArgs)
        RaiseEvent Recieved(e.ApplicationMessage.Topic, If(e.ApplicationMessage.Payload Is Nothing, "", Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload)))
    End Sub

    Private Sub ConnectionClosed(e As Disconnecting.MqttClientDisconnectedEventArgs)
        If IsConnectedWanted Then
            Reconnect.GetAwaiter()
        Else
            RaiseEvent Disconnected()
        End If
    End Sub

    Private Sub ConnectionOpened(e As Connecting.MqttClientConnectedEventArgs)
        RaiseEvent Connected()
    End Sub

End Class
