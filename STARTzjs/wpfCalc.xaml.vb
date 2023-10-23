Imports System.Windows.Threading

Public Class wpfCalc

#Region " Declarations "

    Private Example, Code As String
    Const PI As Double = 3.141592653589
    Private MeWidth As Integer = 305
    Private MeHeight As Integer = 295
    Private MeSmallHeight As Integer = 115
    Private WithEvents timErr As DispatcherTimer
    Private Lge As Boolean = mySystem.LgeCzech

#End Region

#Region " Procedures "

    Private Sub proSetPressNum(ButtonText As String)
        If ButtonText.Contains("?") Then
            Dim Otaz As Integer = ButtonText.IndexOf("?")
            If Not txtVstup.Text = "" And Not txtVstup.Text = "0" Then
                'vložit příklad do přidávané funkce
                If Not txtVstup.SelectedText = "" Then
                    'vložit označenou část příkladu do přidávané funkce
                    Dim Delka As Integer = txtVstup.SelectedText.Length
                    Dim Predpona As String = ""
                    If Not txtVstup.SelectionStart = 0 Then
                        Predpona = txtVstup.Text.Substring(0, txtVstup.SelectionStart)
                    End If
                    Dim Jadro As String = ""
                    Jadro = ButtonText.Substring(0, Otaz) & "(" & txtVstup.SelectedText & ")" & ButtonText.Substring(Otaz + 1, Len(ButtonText) - Otaz - 1)
                    Dim Pripona As String = ""
                    If Not txtVstup.SelectionStart + txtVstup.SelectionLength = txtVstup.Text.Length Then
                        Pripona = txtVstup.Text.Substring(txtVstup.SelectionStart + txtVstup.SelectionLength, txtVstup.Text.Length - txtVstup.SelectionStart - txtVstup.SelectionLength)
                    End If
                    txtVstup.Text = Predpona & Jadro & Pripona
                    txtVstup.SelectionStart = Otaz + Len(Predpona) + 1
                    txtVstup.SelectionLength = Delka
                Else
                    'vložit příklad do přidávané funkce
                    Dim Delka As Integer = txtVstup.Text.Length
                    txtVstup.Text = ButtonText.Substring(0, Otaz) & "(" & txtVstup.Text & ")" & ButtonText.Substring(Otaz + 1, Len(ButtonText) - Otaz - 1)
                    txtVstup.SelectionStart = Otaz + 1
                    txtVstup.SelectionLength = Delka
                End If
            Else
                'vložit funkci
                txtVstup.Text = ButtonText
                txtVstup.SelectionStart = Otaz
                txtVstup.SelectionLength = 1
            End If
        Else
            'vložit znak
            txtVstup.SelectedText = ButtonText
            txtVstup.CaretIndex += txtVstup.SelectedText.Length
            txtVstup.SelectionLength = 0
        End If
        txtVstup.Focus()
    End Sub

    Public Function nCount(Number As String) As Double
        nCount = 0
        If IsNumeric(Number) = False Then
            SyntaxErr()
            Exit Function
        End If
        If Val(Number) > 170 Then
            SyntaxErr()
            Exit Function
        End If
        nCount = 1
        For c As Integer = 1 To CInt(Number)
            nCount = nCount * c
        Next c
    End Function

    Private Sub SyntaxErr()
        timErr.Interval = TimeSpan.FromMilliseconds(10)
        timErr.IsEnabled = True
    End Sub

    Private Sub timErr_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timErr.Tick
        txtVstup.Text = "Syntax Error"
        If timErr.Interval = TimeSpan.FromSeconds(1) Then
            timErr.IsEnabled = False
            txtVstup.Text = Example
            txtVstup.Focus()
            txtVstup.SelectionStart = 0
            txtVstup.SelectionLength = Len(txtVstup.Text)
        End If
        timErr.Interval = TimeSpan.FromSeconds(1)
    End Sub

    Private Function CompileAndRunCode(ByVal Example As String) As Double
        Dim VBCodeToExecute As String = "Dim Eval As Double" & Environment.NewLine
        VBCodeToExecute &= "Eval = " & Example & Environment.NewLine
        VBCodeToExecute &= "Return Eval" & Environment.NewLine

        Try
            ' Instance our CodeDom wrapper
            Dim vbEP As New clsVBEvalProvider
            ' Compile and run
            Dim objResult As Object = vbEP.Eval(VBCodeToExecute)
            If vbEP.CompilerErrors.Count <> 0 Then
                Dim wDialog As New wpfDialog(Me, vbEP.CompilerErrors.Item(0).ErrorText, Me.Title, wpfDialog.Ikona.chyba, "Close")
                wDialog.ShowDialog()
                Return 0
            Else
                Return CDbl(objResult)
            End If

        Catch ex As Exception
            SyntaxErr()
            Return 0
        End Try
    End Function
#End Region

#Region " frmCalc_events "

    Private Sub wpfCalc_StateChanged(sender As Object, e As EventArgs) Handles Me.StateChanged
        If Me.WindowState = WindowState.Maximized And Dat.Options.CalcSmall = False Then
            Me.WindowState = WindowState.Normal
            Dat.Options.CalcSmall = Not Dat.Options.CalcSmall
            changeSize()
            Me.WindowState = WindowState.Maximized
        End If
    End Sub

    Private Sub wpfCalc_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Input.MouseWheelEventArgs) Handles Me.MouseWheel
        Dat.Options.calcsmall = Not Dat.Options.calcsmall
        changeSize()
    End Sub

    Private Sub wpfCalc_SizeChanged(ByVal sender As Object, ByVal e As System.Windows.SizeChangedEventArgs) Handles Me.SizeChanged
        cboExamples.Width = Me.ActualWidth - 36
        txtVstup.Width = Me.ActualWidth - 56
    End Sub

    Private Sub wCalc_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Me.Title = If(Lge, "Kalkulačka", "Calculator")
        Me.Width = MeWidth
        Me.Height = MeHeight
        changeSize()
        Dim Mys As Point = myWindow.PixelToPPI(myWindow.GetMousePosition, True)
        Me.Top = Mys.Y - CInt(Me.ActualHeight / 2)
        Me.Left = Mys.X - CInt(Me.ActualWidth / 2)
        If Me.Left < 1 Then Me.Left = 50
        If Me.Top < 1 Then Me.Top = 50
        Me.Topmost = Dat.Options.CalcFront
        cmdTop.Content = If(Dat.Options.CalcFront, cmdTop.Content.ToString.ToUpper, cmdTop.Content.ToString.ToLower)
        cboExamples.ItemsSource = Dat.CalcMem

        Call (New clsGlass).GlassEffect(Me)
        timErr = New DispatcherTimer
        timErr.Interval = TimeSpan.FromSeconds(1)
        Me.Activate()
        txtVstup.Focus()
        cmdDel.ToolTip = If(Lge, "Zpět", "Backspace")
        btnClearMemory.ToolTip = If(Lge, "Vyprázdnit pamět výpočtů.", "Clear example memory.")
        cmdClear.ToolTip = If(Lge, "Smazat aktuální výpočet.", "Clear actual example.")
        cmdTop.ToolTip = If(Lge, "Okno navrchu.", "Window on top.")
        btnSize.ToolTip = If(Lge, "Změnit velikost okna.", "Change window size.")
    End Sub

    Private Sub wCalc_KeyDown(ByVal sender As System.Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles MyBase.KeyDown
        If e.Key = Key.Escape Then
            txtVstup.Text = Example
            txtVstup.SelectionStart = Len(txtVstup.Text)
            txtVstup.SelectionLength = Len(txtVstup.Text)
        End If
    End Sub

#End Region

#Region " ChangeSize "

    Private Sub btnSize_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnSize.Click
        Dat.Options.calcsmall = Not Dat.Options.calcsmall
        changeSize()
    End Sub

    Private Sub changeSize()
        btnSize.Content = CStr(IIf(Dat.Options.calcsmall, btnSize.Content.ToString.ToLower, btnSize.Content.ToString.ToUpper))
        If Dat.Options.calcsmall Then
            Me.MinWidth = MeWidth
            Me.MinHeight = MeSmallHeight
            Me.MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth
            Me.MaxHeight = MeSmallHeight
            Me.Height = MeSmallHeight
        Else
            Me.MinWidth = MeWidth
            Me.MinHeight = MeHeight
            Me.MaxWidth = MeWidth
            Me.MaxHeight = MeHeight
            Me.Width = MeWidth
            Me.Height = MeHeight
        End If
    End Sub

#End Region

#Region " Memory "

    Private Sub proAddCalculation(ByVal Calculation As String)
        cboExamples.ItemsSource = Nothing
        If Dat.CalcMem.Count > 20 Then Dat.CalcMem.RemoveAt(20)
        Dat.CalcMem.Insert(0, Calculation)
        cboExamples.ItemsSource = Dat.CalcMem
    End Sub

    Private Sub btnClearMemory_Click_1(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnClearMemory.Click
        cboExamples.ItemsSource = Nothing
        Dat.CalcMem.Clear()
        cboExamples.ItemsSource = Dat.CalcMem
    End Sub

    Private Sub cboExamples_SelectionChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles cboExamples.SelectionChanged
        If IsNothing(cboExamples.SelectedItem) = False Then
            Dim Example = cboExamples.SelectedItem.ToString()
            If Not Example.LastIndexOf("=") = -1 Then
                txtVstup.SelectedText = "(" & Example.Substring(0, Example.LastIndexOf("=") - 1) & ")"
                txtVstup.Focus()
            End If
        End If
    End Sub

#End Region

#Region " Buttons "

    Private Sub cmdDel_Click(sender As Object, e As RoutedEventArgs) Handles cmdDel.Click
        txtVstup.RaiseEvent(New KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Back) With {.RoutedEvent = UIElement.KeyDownEvent})
    End Sub

    Private Sub Solve_Click_1(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Solve.Click
        Dim Check As String
        Dim Zavorka As Boolean
        Dim a, b As Integer
        Dim Result As Double
        If txtVstup.Text = "" Then Exit Sub
        b = InStr(1, txtVstup.Text, "=")
        If Not b = 0 Then
            txtVstup.Text = Trim(Strings.Right(txtVstup.Text, Len(txtVstup.Text) - b))
            txtVstup.SelectionStart = Len(txtVstup.Text)
            txtVstup.SelectionLength = Len(txtVstup.Text)
            Exit Sub
        End If
        Example = txtVstup.Text
        cboExamples.IsEnabled = True
        Check = Trim(LCase(txtVstup.Text))
        Check = Check.Replace("pi", PI.ToString)
        Check = Check.Replace(",", ".")

        Do 'Výpočet faktorů! před kompilací
            Zavorka = False
            a = InStr(2, Check, "!")
            If Not a = 0 Then
                For b = 1 To a - 1
                    If Mid(Check, a - b, 1) = ")" And b = 1 Then Zavorka = True
                    If Mid(Check, a - b, 1) = "(" And Zavorka = True Then Exit For
                    If IsNumeric(Mid(Check, a - b, 1)) = False And Zavorka = False Then Exit For
                Next b
                If Zavorka = True Then
                    If a - b = 0 Then 'Oprava erroru když výraz končí ale nezačínká závorkou
                        a += 1
                        Check = "(" & Check
                    End If
                    b = b + 1
                End If
                Dim strin As String = Mid(Check, a - b + 1, b - 1)
                If Zavorka = True Then
                    If CompileAndRunCode(strin) = 0 Then Exit Sub
                    strin = Str(CompileAndRunCode(strin))
                End If
                If nCount(strin) = 0 Then Exit Sub
                Check = Strings.Left(Check, a - b) & nCount(strin) & Strings.Right(Check, Len(Check) - a)
            End If
        Loop Until a = 0

        Result = CompileAndRunCode(Check)
        txtVstup.Text = Example & " = " & Result
        txtVstup.SelectionStart = Len(Example)
        txtVstup.SelectionLength = Len(Str(Result)) + 3

        If Not Result = 0 Then
            proAddCalculation(Example & " = " & ((Int(Result * 100)) / 100).ToString)
        End If
    End Sub

    Private Sub cmdTop_Click_1(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdTop.Click
        Me.Topmost = Not Me.Topmost
        Dat.Options.CalcFront = Me.Topmost
        cmdTop.Content = CStr(IIf(Me.Topmost, cmdTop.Content.ToString.ToUpper, cmdTop.Content.ToString.ToLower))
    End Sub

    Private Sub cmdClear_Click_1(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles cmdClear.Click
        txtVstup.Text = ""
        txtVstup.Focus()
    End Sub

#End Region

#Region " Buttons_Calc "

    Private Sub NumCos_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumCos.Click
        proSetPressNum("Math.Cos(?/180*PI)")
    End Sub

    Private Sub NumSin_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumSin.Click
        proSetPressNum("Math.Sin(?/180*PI)")
    End Sub

    Private Sub NumTan_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumTan.Click
        proSetPressNum("Math.Tan(?/180*PI)")
    End Sub

    Private Sub NumPi_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumPi.Click
        proSetPressNum("PI")
    End Sub

    Private Sub NumX2_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumX2.Click
        proSetPressNum("?^2")
    End Sub

    Private Sub Num2X_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num2X.Click
        proSetPressNum("?^(1/2)")
    End Sub

    Private Sub Num1X_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num1X.Click
        proSetPressNum("Math.Log?/Math.Log(10)")
    End Sub

    Private Sub NumX_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumX.Click
        proSetPressNum("?!")
    End Sub

    Private Sub NumL_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumL.Click
        proSetPressNum("(")
    End Sub

    Private Sub NumR_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumR.Click
        proSetPressNum(")")
    End Sub

    Private Sub Num7_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num7.Click
        proSetPressNum(Num7.Content.ToString)
    End Sub

    Private Sub Num8_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num8.Click
        proSetPressNum(Num8.Content.ToString)
    End Sub

    Private Sub Num9_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num9.Click
        proSetPressNum(Num9.Content.ToString)
    End Sub

    Private Sub Num4_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num4.Click
        proSetPressNum(Num4.Content.ToString)
    End Sub

    Private Sub Num5_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num5.Click
        proSetPressNum(Num5.Content.ToString)
    End Sub

    Private Sub Num6_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num6.Click
        proSetPressNum(Num6.Content.ToString)
    End Sub

    Private Sub Num1_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num1.Click
        proSetPressNum(Num1.Content.ToString)
    End Sub

    Private Sub Num2_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num2.Click
        proSetPressNum(Num2.Content.ToString)
    End Sub

    Private Sub Num3_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num3.Click
        proSetPressNum(Num3.Content.ToString)
    End Sub

    Private Sub Num0_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Num0.Click
        proSetPressNum(Num0.Content.ToString)
    End Sub

    Private Sub NumDot_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumDot.Click
        proSetPressNum(NumDot.Content.ToString)
    End Sub

    Private Sub NumDivide_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumDivide.Click
        proSetPressNum("/")
    End Sub

    Private Sub NumMulti_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumMulti.Click
        proSetPressNum("*")
    End Sub

    Private Sub NumMinus_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumMinus.Click
        proSetPressNum("-")
    End Sub

    Private Sub NumPlus_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles NumPlus.Click
        proSetPressNum("+")
    End Sub
#End Region

End Class
