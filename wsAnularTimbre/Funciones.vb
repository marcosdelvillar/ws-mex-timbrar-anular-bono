Imports System.Data.SqlClient
Imports System.IO
Imports System.ServiceModel

Module Funciones

    Public Function EjecutaDatasetSP(ByVal StoredProcedure As String, ByRef sError As String, Optional ByVal Parametros As Hashtable = Nothing) As DataSet

        Dim cmdCommand As System.Data.SqlClient.SqlCommand
        Dim con As New SqlClient.SqlConnection
        Try
            Dim strConnectionString As String = ConfigurationManager.ConnectionStrings("SWForever_ConnectionString").ConnectionString
            con.ConnectionString = strConnectionString
            If con.State <> ConnectionState.Open Then
                con.Open()
            End If

            cmdCommand = New SqlCommand
            cmdCommand.CommandText = StoredProcedure
            cmdCommand.CommandType = CommandType.StoredProcedure
            cmdCommand.Connection = con

            If Not Parametros Is Nothing Then
                Dim Col As String
                For Each KeyHash As Object In Parametros.Keys
                    Col = KeyHash.ToString
                    cmdCommand.Parameters.Add(New SqlClient.SqlParameter(Col, Parametros(Col)))
                Next
            End If
            Dim daEjecutaDs As SqlDataAdapter = New SqlDataAdapter
            Dim dsEjecutaDs As DataSet = New DataSet
            daEjecutaDs.SelectCommand = cmdCommand
            daEjecutaDs.Fill(dsEjecutaDs)
            con.Close()
            sError = "TODOOK"
            Return dsEjecutaDs
        Catch ex As Exception
            sError = ex.Message
            con.Close()
            Return Nothing
        End Try

    End Function

    Public Sub WriteFile(ByVal sCONTENIDO As String, ByVal sDESTINO As String)

        Dim strArchivo As String = sDESTINO

        Try
            If FileIO.FileSystem.FileExists(strArchivo) Then FileIO.FileSystem.DeleteFile(strArchivo)

            FileOpen(2, strArchivo, OpenMode.Append, OpenAccess.Write, OpenShare.LockWrite)
            PrintLine(2, sCONTENIDO)
            FileClose(2)

        Catch ex As Exception

        End Try

    End Sub


    Public Function GetWriteDirectory() As String
        Return ConfigurationManager.AppSettings("DirSalida").ToString & "" & Now.Year & "\" & Format(Now.Month, "00") & "\"
    End Function

    Public Function EscribirArchivo(ByVal sCONTENIDO As String) As String

        Dim Directorio As String = ConfigurationManager.AppSettings("DirSalida").ToString & "" & Now.Year & "\" & Format(Now.Month, "00") & "\"
        Dim strArchivo As String = Directorio & $"bita{Now:dd}.txt"
        If Not IO.Directory.Exists(Directorio) Then IO.Directory.CreateDirectory(Directorio)

        Try
            FileOpen(2, strArchivo, OpenMode.Append, OpenAccess.Write, OpenShare.LockWrite)
            PrintLine(2, sCONTENIDO)
            FileClose(2)
            Return "TODOOK"
        Catch ex As Exception
            Return "Error al escribir archivo: " & ex.Message.ToString
        End Try

    End Function

    Public Function LeerArchivo(ByVal FileName As String) As String
        Dim oRead As System.IO.StreamReader
        Dim LineIn As New StringBuilder

        Try
            'SE REVISA QUE EXISTA EL ARCHIVO 
            If Not File.Exists(FileName) Then
                Return "Error, El archivo no existe en la ruta: " & FileName
            End If

            'SE REALIZA LA LECTURA DEL ARCHIVO
            oRead = File.OpenText(FileName)

            'SE RECORRE TODO EL STREAM DEL ARCHIVO
            While Not oRead.EndOfStream
                'SE ALMACENA EL CONTENIDO DE LA LINEA EN LA VARIABLE
                LineIn.Append(oRead.ReadLine().Replace(vbCrLf, ""))
            End While

            'SE CIERRA EL STREAM DEL ARCHIVO
            oRead.Close()

            Dim resultado As String = LineIn.ToString

            resultado = resultado.Replace("á", "a")
            resultado = resultado.Replace("é", "e")
            resultado = resultado.Replace("í", "i")
            resultado = resultado.Replace("ó", "o")
            resultado = resultado.Replace("ú", "u")

            Return resultado
        Catch ex As Exception
            Return ""
        End Try
    End Function

End Module
