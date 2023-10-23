Imports System.Text
Imports System.CodeDom.Compiler
Imports System.Reflection

Public Class clsVBEvalProvider

    Private m_oCompilerErrors As CompilerErrorCollection

    Public Property CompilerErrors() As CompilerErrorCollection
        Get
            Return m_oCompilerErrors
        End Get
        Set(ByVal Value As CompilerErrorCollection)
            m_oCompilerErrors = Value
        End Set
    End Property

    Public Sub New()
        MyBase.New()
        m_oCompilerErrors = New CompilerErrorCollection
    End Sub

    Public Function Eval(ByVal vbCode As String) As Object

        Dim oCodeProvider As VBCodeProvider = New VBCodeProvider
        ' Obsolete in 2.0 framework
        ' Dim oICCompiler As ICodeCompiler = oCodeProvider.CreateCompiler

        Dim oCParams As CompilerParameters = New CompilerParameters
        Dim oCResults As CompilerResults
        Dim oAssy As System.Reflection.Assembly
        Dim oExecInstance As Object = Nothing
        Dim oRetObj As Object = Nothing
        Dim oMethodInfo As MethodInfo
        Dim oType As Type


        Try

            ' Setup the Compiler Parameters  
            ' Add any referenced assemblies
            oCParams.ReferencedAssemblies.Add("system.dll")
            oCParams.ReferencedAssemblies.Add("system.xml.dll")
            oCParams.ReferencedAssemblies.Add("system.data.dll")
            oCParams.CompilerOptions = "/t:library"
            oCParams.GenerateInMemory = True

            ' Generate the Code Framework
            Dim sb As StringBuilder = New StringBuilder("")

            sb.Append("Imports System" & vbCrLf)
            sb.Append("Imports System.Xml" & vbCrLf)
            sb.Append("Imports System.Data" & vbCrLf)

            ' Build a little wrapper code, with our passed in code in the middle 
            sb.Append("Namespace dValuate" & vbCrLf)
            sb.Append("Class EvalRunTime " & vbCrLf)
            sb.Append("Public Function EvaluateIt() As Object " & vbCrLf)
            sb.Append(vbCode & vbCrLf)
            sb.Append("End Function " & vbCrLf)
            sb.Append("End Class " & vbCrLf)
            sb.Append("End Namespace" & vbCrLf)
            Debug.WriteLine(sb.ToString())

            Try
                ' Compile and get results 
                ' 2.0 Framework - Method called from Code Provider
                oCResults = oCodeProvider.CompileAssemblyFromSource(oCParams, sb.ToString) 'sb.ToString)
                ' 1.1 Framework - Method called from CodeCompiler Interface
                ' cr = oICCompiler.CompileAssemblyFromSource (cp, sb.ToString)

                ' Check for compile time errors 
                If oCResults.Errors.Count <> 0 Then

                    Me.CompilerErrors = oCResults.Errors
                    Throw New Exception("Compile Errors")

                Else
                    ' No Errors On Compile, so continue to process...

                    oAssy = oCResults.CompiledAssembly
                    oExecInstance = oAssy.CreateInstance("dValuate.EvalRunTime")

                    oType = oExecInstance.GetType
                    oMethodInfo = oType.GetMethod("EvaluateIt")

                    oRetObj = oMethodInfo.Invoke(oExecInstance, Nothing)
                    Return oRetObj

                End If

            Catch ex As Exception
                ' Compile Time Errors Are Caught Here
                ' Some other weird error 
                Throw ex
            End Try

        Catch ex As Exception
            Throw ex
        End Try

        Return oRetObj

    End Function
End Class


