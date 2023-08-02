Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System
Imports System.Diagnostics
Imports System.Security
Imports System.Collections.Generic
Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.IO
Imports System.Xml.Xsl
Imports System.Runtime.InteropServices
Imports System.Xml
Imports System.Xml.Schema
Imports System.Data.SqlClient
Imports System.Data
Imports System.Guid
Imports System.Net






' To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line.
' <System.Web.Script.Services.ScriptService()> _
<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class Service
     Inherits System.Web.Services.WebService
    Dim PathXML As String
    Dim nameXML As String
    Dim nameXMLP As String
    Dim sRES As String = ""
    Dim apiKy As String = "y7dr7tzcwjsn7k2icwsypmj6vj8nmaxuufjjcfubrfvfmxqbvmnnvgwb9nstumcmp5ifqwtjry2akhvu"  ''LLave para Produccion
    Dim apiKyPruebas As String = "yywkagn3khxappz4ithfvgmbypuxinab4xxup4z48tdbcqk66tkzm5suvbkeqxmi7bymjgzpspvxc443" ''LLave para Produccion
    'Dim apiKy As String = "d5ecdce5903344c08eeb1c7d8265ae4fbfb22fec42c540979bfe03f191aa7562" ''LLave para Produccion
    Private m_xmlDOM As System.Xml.XmlDocument
    Private Nodo As System.Xml.XmlNode

    Private RutaBitacora As String = "C:\inetpub\wwwroot\wsTimAnularBono\Bitacora\"

    <WebMethod()> _
    Public Function HelloWorld() As String
        Return "Ver. 1.1"
    End Function

    <WebMethod()> _
    Public Function EstadoTransaccion(ByVal IdBono As String, _
                                      ByVal IdFbo As String, _
                                      ByVal TransactionId As String) As String

        Dim uuid As New Guid
        Dim client As AnulacionTimbre.CancelacionClient = New AnulacionTimbre.CancelacionClient()
        Dim Error1 As New AnulacionTimbre.ErrorMessageCode
        Dim ErrorMessage As New AnulacionTimbre.ErrorMessageCode

        Dim resulmessag As String = ""
        Dim resulcode As String = ""
        Dim uuiStatus As New AnulacionTimbre.StateQueryResponse
        Dim RelatUIIS() As AnulacionTimbre.FolioFiscalDetail
        Dim TotalUUIDs, UUIDRejected, UUIDSent As Integer
        Dim Finished, resultado As Boolean
        Dim MensajeEstado As String = ""
        Dim XmlRes As String = ""
        Dim ResultCode As String = "-1" 'valor por default. En ocasiones no devuelve el ResultCode
        'IdBono = "977289"
        'IdFbo = "520000016327"
        'TransactionId = "b12c5f21-2e30-467d-bf2b-90216632a5be"

        '	b12c5f21-2e30-467d-bf2b-90216632a5be

        Dim ArchivoBitactora As String = RutaBitacora & "" & "" & IdBono & "_" & IdFbo & ".txt"
        Dim ArchivoXmlStatusTransact As String = RutaBitacora & "" & "" & IdBono & "_" & IdFbo & "_EdoTransac.xml"

        uuid = New Guid(TransactionId)

        ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
        resultado = client.GetTransactionStatusDetail(apiKy, uuid, Error1, ErrorMessage, TotalUUIDs, UUIDSent, UUIDRejected, Finished, RelatUIIS)

        If RelatUIIS.Length > 0 Then

            If Not RelatUIIS(0).ResultCode Is Nothing Then
                MensajeEstado = RelatUIIS(0).ResultCode
                ResultCode = RelatUIIS(0).ResultCode
            End If

            If Not RelatUIIS(0).ResultMessage = Nothing Then
                MensajeEstado = MensajeEstado & " " & RelatUIIS(0).ResultMessage
            End If

            If MensajeEstado = Nothing Then
                MensajeEstado = RelatUIIS(0).UUIDStatus.CancellationStatus
            End If


            Try
                Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(AnulacionTimbre.FolioFiscalDetail))

                Dim file As New System.IO.StreamWriter(ArchivoXmlStatusTransact)
                writer.Serialize(file, RelatUIIS(0))
                file.Close()

            Catch ex As Exception
                EscribirArchivo("No fue posible serializar la respuesta: " & vbCrLf & ex.Message.ToString, ArchivoBitactora)
            End Try


            XmlRes = LeerArchivo(ArchivoXmlStatusTransact)
        End If

        If MensajeEstado Is Nothing Then
            MensajeEstado = "No se puedo leer el estado !?"
        End If

        If ResultCode Is Nothing Then ResultCode = "-2"
        If ResultCode.Trim.Length = 0 Then ResultCode = "-2"

        Dim hsInfo As Hashtable
        hsInfo = New Hashtable
        hsInfo.Add("@IdBono", IdBono)
        hsInfo.Add("@Estado", MensajeEstado)
        hsInfo.Add("@XmlRes", XmlRes)
        hsInfo.Add("@ResultCode", ResultCode)


        EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_AnuladoInsStatus", sRES, hsInfo)


        Return MensajeEstado.ToString

    End Function

    <WebMethod()> _
    Public Function EstatusAnulacion(ByVal IdBono As String, ByVal IdFbo As String) As String

        Dim hsInfo As New Hashtable
        Dim ds As System.Data.DataSet
        Dim resul As Boolean
        Dim mensaje As String


        'IdBono = "977292"
        'IdFbo = "520000033747"



        Dim ArchivoBitactora As String = RutaBitacora & "" & "" & IdBono & "_" & IdFbo & ".txt"
        Dim ArchivoXmlStatus As String = RutaBitacora & "" & "" & IdBono & "_" & IdFbo & "_EdoAnulacion.xml"

        'Traemos datos del timbre
        hsInfo.Add("@IdBono", IdBono)
        hsInfo.Add("@CveEmpresario", IdFbo)
        ds = EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_Anulado_UUID", sRES, hsInfo)

        If ds Is Nothing Then
            mensaje = "Error *spTimbrar_Anulado_UUID*: " & sRES
            EscribirArchivo(mensaje, ArchivoBitactora)
            Return mensaje
        End If

        If ds.Tables(0).Rows.Count = 0 Then
            mensaje = "Registro no encontrado con el status -1- *spTimbrar_Anulado_UUID*"
            EscribirArchivo(mensaje, ArchivoBitactora)
            Return mensaje
        End If


        Dim uuid As New Guid
        Dim client As AnulacionTimbre.CancelacionClient = New AnulacionTimbre.CancelacionClient()
        Dim ErrorReturn As New AnulacionTimbre.ErrorMessageCode
        Dim SentRequest As Boolean
        Dim resulmessag As String = ""
        Dim resulcode As String = ""
        Dim uuiStatus As New AnulacionTimbre.StateQueryResponse
        Dim RelatUIIS(0) As AnulacionTimbre.FolioFiscalDetail
        Dim ResultCode As String = ""
        Try

            '  uuid = Guid.Parse(ds.Tables(0).Rows(0).Item("TFD_UUID"))

            Dim UUIDLocal As String = ds.Tables(0).Rows(0).Item("TFD_UUID").ToString
            'UUIDLocal = "6F31C067-AB9F-4513-BF64-99866950E66F"
            uuid = New Guid(UUIDLocal)
            '   uuid = DirectCast(ds.Tables(0).Rows(0).Item("TFD_UUID"), Guid)
            RelatUIIS(0) = New AnulacionTimbre.FolioFiscalDetail
            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
            resul = client.GetFolioStatusDetail(apiKy, uuid, ErrorReturn, SentRequest, resulcode, resulmessag, uuiStatus, RelatUIIS)

            ' client.Cancelar(apiKy, ds.Tables(0).Rows(0).Item("EMI_RFC").ToString(), )
            ' Always close the client.
            client.Close()

            Dim MensajeEstado As String
            Dim XmlRes As String = ""

            Try
                MensajeEstado = RelatUIIS(0).ResultMessage
            Catch ex As Exception
                MensajeEstado = Nothing
            End Try

            'Mirar si tiene resultado. Si no, el error se tiene que leer de otro lado.
            If RelatUIIS.Length > 0 Then
                If MensajeEstado Is Nothing Then
                    MensajeEstado = RelatUIIS(0).UUIDStatus.CancellationStatus
                End If

                If MensajeEstado Is Nothing Or MensajeEstado.Trim.Length = 0 Then
                    MensajeEstado = RelatUIIS(0).UUIDStatus.IsCancellable
                End If

                If Not RelatUIIS(0).ResultCode Is Nothing Then
                    ResultCode = RelatUIIS(0).ResultCode
                End If

                Try
                    Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(AnulacionTimbre.FolioFiscalDetail))

                    Dim file As New System.IO.StreamWriter(ArchivoXmlStatus)
                    writer.Serialize(file, RelatUIIS(0))
                    file.Close()

                Catch ex As Exception
                    EscribirArchivo("No fue posible serializar la respuesta: " & vbCrLf & ex.Message.ToString, ArchivoBitactora)
                End Try


                XmlRes = LeerArchivo(ArchivoXmlStatus)
            End If


            If MensajeEstado Is Nothing Then
                MensajeEstado = resulcode + " " + resulmessag
            End If

            If Not resulcode Is Nothing Then
                ResultCode = resulcode
            End If

            EscribirArchivo("MensajeEstado: " & MensajeEstado.ToString, ArchivoBitactora)

            If ResultCode Is Nothing Then ResultCode = "-2"
            If ResultCode.Trim.Length = 0 Then ResultCode = "-2"

            hsInfo = New Hashtable
            hsInfo.Add("@IdBono", IdBono)
            hsInfo.Add("@Estado", MensajeEstado)
            hsInfo.Add("@XmlRes", XmlRes)
            hsInfo.Add("@ResultCode", ResultCode)

            ds = EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_AnuladoInsStatus", sRES, hsInfo)

            Return MensajeEstado.ToString
        Catch ex As Exception
            resul = ex.Message.ToString
            Return resul
        End Try

    End Function

#Region "Cancelacion de Timbrado de CFD"
    <WebMethod()> _
    Public Function CancelarCFDI(ByVal IdBono As String, ByVal IdFbo As String) As String
        Dim sREST As String
        Dim hsInfo As New Hashtable
        Dim ErrTimbre As String = "0"
        Dim ds As System.Data.DataSet
        Dim mensaje As String

        'IdBono = "977292"
        'IdFbo = "520000033747"

        Dim ArchivoBitactora As String = RutaBitacora & "" & "" & IdBono & "_" & IdFbo & ".txt"
        Dim ArchivoXmlStatus As String = RutaBitacora & "" & "" & IdBono & "_" & IdFbo & "_cancelaCFDI.xml"

        Dim Ows As AnulacionTimbre.CancelacionClient = New AnulacionTimbre.CancelacionClient()

        'Traemos datos del timbre
        hsInfo.Add("@IdBono", IdBono)
        hsInfo.Add("@CveEmpresario", IdFbo)
        ds = EjecutaDatasetSP("sicobonV2.dbo.[spTimbrar_Anulado_UUID]", sRES, hsInfo)

        If ds.Tables(0).Rows.Count = 0 Then
            mensaje = "Registro no encontrado con el status -1- *spTimbrar_Anulado_UUID*"
            EscribirArchivo(mensaje, ArchivoBitactora)
            Return mensaje
        End If


        Dim ErrorMessage As Boolean

        Dim FoliosCancelados As Integer
        'Dim AcuseSAT As String
        Dim FoliosAnular(0) As AnulacionTimbre.CancelFolio
        Dim transec(0) As AnulacionTimbre.TransactionProperty
        Dim tracking As Guid
        Dim ErrorReturn As AnulacionTimbre.ErrorMessageCode
        Dim setting As XmlWriterSettings = New XmlWriterSettings()
        Try

            FoliosCancelados = 0

            FoliosAnular(0) = New AnulacionTimbre.CancelFolio()

            FoliosAnular(0).UUID = ds.Tables(0).Rows(0).Item("TFD_UUID").ToString()
            FoliosAnular(0).Reason = "03"
            transec(0) = New AnulacionTimbre.TransactionProperty

            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
            ErrorReturn = Ows.Cancelar(apiKy, ds.Tables(0).Rows(0).Item("EMI_RFC").ToString(), FoliosAnular, transec, ErrorMessage, tracking)
            Ows.Close()

            hsInfo.Clear()
            hsInfo.Add("@IdBono", IdBono)
            hsInfo.Add("@TransacId", tracking.ToString)
            ds = EjecutaDatasetSP("sicobonV2.dbo.[spTimbrar_AnuladoIns]", sRES, hsInfo)

            mensaje = "ENVIADO|" & tracking.ToString
            EscribirArchivo(mensaje, ArchivoBitactora)

            Return mensaje

        Catch ex As Exception
            mensaje = ex.Message.ToString
            EscribirArchivo(mensaje, ArchivoBitactora)
            Return mensaje
        End Try
    End Function
#End Region
    Public Shared Function EjecutaDatasetSP(ByVal StoredProcedure As String, ByRef sError As String, Optional ByVal Parametros As Hashtable = Nothing) As DataSet

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

    Private Sub writeFile(ByVal sCONTENIDO As String, ByVal sDESTINO As String)

        Dim strArchivo As String = sDESTINO

        Try
            If FileIO.FileSystem.FileExists(strArchivo) Then FileIO.FileSystem.DeleteFile(strArchivo)

            FileOpen(2, strArchivo, OpenMode.Append, OpenAccess.Write, OpenShare.LockWrite)
            PrintLine(2, sCONTENIDO)
            FileClose(2)

        Catch ex As Exception

        End Try

    End Sub

    Private Shared Function EscribirArchivo(ByVal sCONTENIDO As String, ByVal sDESTINO As String) As String

        Dim strArchivo As String = sDESTINO

        Try
            'If FileIO.FileSystem.FileExists(strArchivo) Then FileIO.FileSystem.DeleteFile(strArchivo)

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
End Class