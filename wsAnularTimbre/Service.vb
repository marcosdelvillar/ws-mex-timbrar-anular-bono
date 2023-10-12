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


    ReadOnly apiKy As String = "y7dr7tzcwjsn7k2icwsypmj6vj8nmaxuufjjcfubrfvfmxqbvmnnvgwb9nstumcmp5ifqwtjry2akhvu"  ''LLave para Produccion
    ReadOnly apiKyPruebas As String = "yywkagn3khxappz4ithfvgmbypuxinab4xxup4z48tdbcqk66tkzm5suvbkeqxmi7bymjgzpspvxc443" ''LLave para Produccion
    'Dim apiKy As String = "d5ecdce5903344c08eeb1c7d8265ae4fbfb22fec42c540979bfe03f191aa7562" ''LLave para Produccion

    Private ReadOnly RutaBitacora As String = "C:\inetpub\wwwroot\wsTimAnularBono\Bitacora\"

    <WebMethod()>
    Public Function Version() As String
        Return "Ver. 1.1.2"
    End Function


    <WebMethod()>
    Public Function CancelarCfdi(idBono As String,
                                 idFbo As String) As String

        Dim hsInfo As New Hashtable
        Dim ds As System.Data.DataSet
        Dim mensaje As String
        Dim sRes As String = ""

        'IdBono = "977292"
        'IdFbo = "520000033747"

        Dim archivoBitactora As String = RutaBitacora & "" & "" & idBono & "_" & idFbo & ".txt"
        Dim archivoXmlStatus As String = RutaBitacora & "" & "" & idBono & "_" & idFbo & "_cancelaCFDI.xml"

        Dim oWs As New AnulacionTimbre.CancelacionClient()

        'Traemos datos del timbre
        hsInfo.Add("@IdBono", idBono)
        hsInfo.Add("@CveEmpresario", idFbo)
        ds = EjecutaDatasetSP("sicobonV2.dbo.[spTimbrar_Anulado_UUID]", sRes, hsInfo)

        If ds.Tables(0).Rows.Count = 0 Then
            mensaje = "Registro no encontrado con el status -1- *spTimbrar_Anulado_UUID*"
            EscribirArchivo(mensaje, archivoBitactora)
            Return mensaje
        End If

        Dim errorMessage As Boolean
        Dim foliosCancelados As Integer
        Dim foliosAnular(0) As AnulacionTimbre.CancelFolio
        Dim transec(0) As AnulacionTimbre.TransactionProperty
        Dim tracking As Guid
        Dim errorReturn As AnulacionTimbre.ErrorMessageCode
        Dim setting As New XmlWriterSettings()
        Try

            foliosCancelados = 0
            foliosAnular(0) = New AnulacionTimbre.CancelFolio()
            foliosAnular(0).UUID = ds.Tables(0).Rows(0).Item("TFD_UUID").ToString()
            foliosAnular(0).Reason = "03"
            transec(0) = New AnulacionTimbre.TransactionProperty

            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
            errorReturn = oWs.Cancelar(apiKy, ds.Tables(0).Rows(0).Item("EMI_RFC").ToString(), foliosAnular, transec, errorMessage, tracking)
            oWs.Close()

            hsInfo.Clear()
            hsInfo.Add("@IdBono", idBono)
            hsInfo.Add("@TransacId", tracking.ToString)
            ds = EjecutaDatasetSP("sicobonV2.dbo.[spTimbrar_AnuladoIns]", sRes, hsInfo)

            mensaje = "ENVIADO|" & tracking.ToString
            EscribirArchivo(mensaje, archivoBitactora)

            Return mensaje
        Catch ex As Exception
            mensaje = ex.Message.ToString
            EscribirArchivo(mensaje, archivoBitactora)
            Return mensaje
        End Try
    End Function

    <WebMethod()>
    Public Function EstadoTransaccion(idBono As String,
                                      idFbo As String,
                                      transactionId As String) As String

        Dim uuid As New Guid
        Dim client As AnulacionTimbre.CancelacionClient = New AnulacionTimbre.CancelacionClient()
        Dim error1 As New AnulacionTimbre.ErrorMessageCode
        Dim errorMessage As New AnulacionTimbre.ErrorMessageCode

        Dim resulmessag As String = ""
        Dim resulcode As String = ""
        Dim uuiStatus As New AnulacionTimbre.StateQueryResponse
        Dim relatUIIS() As AnulacionTimbre.FolioFiscalDetail
        Dim totalUUIDs, UUIDRejected, UUIDSent As Integer
        Dim finished, resultado As Boolean
        Dim mensajeEstado As String = ""
        Dim xmlRes As String = ""
        Dim resultCode As String = "-1" 'valor por default. En ocasiones no devuelve el ResultCode
        'IdBono = "977289"
        'IdFbo = "520000016327"
        'TransactionId = "b12c5f21-2e30-467d-bf2b-90216632a5be"

        '	b12c5f21-2e30-467d-bf2b-90216632a5be

        Dim archivoBitactora As String = RutaBitacora & "" & "" & idBono & "_" & idFbo & ".txt"
        Dim archivoXmlStatusTransact As String = RutaBitacora & "" & "" & idBono & "_" & idFbo & "_EdoTransac.xml"

        uuid = New Guid(transactionId)

        ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
        resultado = client.GetTransactionStatusDetail(apiKy, uuid, error1, errorMessage, totalUUIDs, UUIDSent, UUIDRejected, finished, relatUIIS)

        If relatUIIS.Length > 0 Then

            If relatUIIS(0).ResultCode IsNot Nothing Then
                mensajeEstado = relatUIIS(0).ResultCode
                resultCode = relatUIIS(0).ResultCode
            End If

            If Not relatUIIS(0).ResultMessage = Nothing Then
                mensajeEstado = mensajeEstado & " " & relatUIIS(0).ResultMessage
            End If

            If mensajeEstado = Nothing Then
                mensajeEstado = relatUIIS(0).UUIDStatus.CancellationStatus
            End If


            Try
                Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(AnulacionTimbre.FolioFiscalDetail))

                Dim file As New System.IO.StreamWriter(archivoXmlStatusTransact)
                writer.Serialize(file, relatUIIS(0))
                file.Close()

            Catch ex As Exception
                EscribirArchivo("No fue posible serializar la respuesta: " & vbCrLf & ex.Message.ToString, archivoBitactora)
            End Try


            xmlRes = LeerArchivo(archivoXmlStatusTransact)
        End If

        If mensajeEstado Is Nothing Then
            mensajeEstado = "No se puedo leer el estado !?"
        End If

        If resultCode Is Nothing Then resultCode = "-2"
        If resultCode.Trim.Length = 0 Then resultCode = "-2"

        Dim sRes As String = ""
        Dim hsInfo As Hashtable
        hsInfo = New Hashtable
        hsInfo.Add("@IdBono", idBono)
        hsInfo.Add("@Estado", mensajeEstado)
        hsInfo.Add("@XmlRes", xmlRes)
        hsInfo.Add("@ResultCode", resultCode)

        EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_AnuladoInsStatus", sRes, hsInfo)

        Return mensajeEstado.ToString
    End Function

    <WebMethod()>
    Public Function EstatusAnulacion(idBono As String,
                                     idFbo As String) As String

        Dim hsInfo As New Hashtable
        Dim ds As System.Data.DataSet
        Dim resul As Boolean
        Dim mensaje As String
        Dim sRes As String = ""
        'IdBono = "977292"
        'IdFbo = "520000033747"

        Dim archivoBitactora As String = RutaBitacora & "" & "" & idBono & "_" & idFbo & ".txt"
        Dim archivoXmlStatus As String = RutaBitacora & "" & "" & idBono & "_" & idFbo & "_EdoAnulacion.xml"

        'Traemos datos del timbre
        hsInfo.Add("@IdBono", idBono)
        hsInfo.Add("@CveEmpresario", idFbo)
        ds = EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_Anulado_UUID", sRes, hsInfo)

        If ds Is Nothing Then
            mensaje = "Error *spTimbrar_Anulado_UUID*: " & sRes
            EscribirArchivo(mensaje, archivoBitactora)
            Return mensaje
        End If

        If ds.Tables(0).Rows.Count = 0 Then
            mensaje = "Registro no encontrado con el status -1- *spTimbrar_Anulado_UUID*"
            EscribirArchivo(mensaje, archivoBitactora)
            Return mensaje
        End If


        Dim uuid As New Guid
        Dim client As New AnulacionTimbre.CancelacionClient()
        Dim errorReturn As New AnulacionTimbre.ErrorMessageCode
        Dim sentRequest As Boolean
        Dim resulmessag As String = ""
        Dim resulcode As String = ""
        Dim uuiStatus As New AnulacionTimbre.StateQueryResponse
        Dim relatUIIS(0) As AnulacionTimbre.FolioFiscalDetail
        Dim resultCode As String = ""
        Try

            'uuid = Guid.Parse(ds.Tables(0).Rows(0).Item("TFD_UUID"))

            Dim UUIDLocal As String = ds.Tables(0).Rows(0).Item("TFD_UUID").ToString
            'UUIDLocal = "6F31C067-AB9F-4513-BF64-99866950E66F"
            uuid = New Guid(UUIDLocal)
            '   uuid = DirectCast(ds.Tables(0).Rows(0).Item("TFD_UUID"), Guid)
            relatUIIS(0) = New AnulacionTimbre.FolioFiscalDetail
            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
            resul = client.GetFolioStatusDetail(apiKy, uuid, errorReturn, sentRequest, resulcode, resulmessag, uuiStatus, relatUIIS)

            ' client.Cancelar(apiKy, ds.Tables(0).Rows(0).Item("EMI_RFC").ToString(), )
            ' Always close the client.
            client.Close()

            Dim mensajeEstado As String
            Dim xmlRes As String = ""

            Try
                mensajeEstado = relatUIIS(0).ResultMessage
            Catch ex As Exception
                mensajeEstado = Nothing
            End Try

            'Mirar si tiene resultado. Si no, el error se tiene que leer de otro lado.
            If relatUIIS.Length > 0 Then
                If mensajeEstado Is Nothing Then
                    mensajeEstado = relatUIIS(0).UUIDStatus.CancellationStatus
                End If

                If mensajeEstado Is Nothing Or mensajeEstado.Trim.Length = 0 Then
                    mensajeEstado = relatUIIS(0).UUIDStatus.IsCancellable
                End If

                If Not relatUIIS(0).ResultCode Is Nothing Then
                    resultCode = relatUIIS(0).ResultCode
                End If

                Try
                    Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(AnulacionTimbre.FolioFiscalDetail))

                    Dim file As New System.IO.StreamWriter(archivoXmlStatus)
                    writer.Serialize(file, relatUIIS(0))
                    file.Close()

                Catch ex As Exception
                    EscribirArchivo("No fue posible serializar la respuesta: " & vbCrLf & ex.Message.ToString, archivoBitactora)
                End Try

                xmlRes = LeerArchivo(archivoXmlStatus)
            End If

            If mensajeEstado Is Nothing Then mensajeEstado = resulcode + " " + resulmessag
            If resulcode IsNot Nothing Then resultCode = resulcode

            EscribirArchivo("MensajeEstado: " & mensajeEstado.ToString, archivoBitactora)

            If resultCode Is Nothing Then resultCode = "-2"
            If resultCode.Trim.Length = 0 Then resultCode = "-2"

            hsInfo = New Hashtable
            hsInfo.Add("@IdBono", idBono)
            hsInfo.Add("@Estado", mensajeEstado)
            hsInfo.Add("@XmlRes", xmlRes)
            hsInfo.Add("@ResultCode", resultCode)

            ds = EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_AnuladoInsStatus", sRes, hsInfo)

            Return mensajeEstado.ToString
        Catch ex As Exception
            resul = ex.Message.ToString
            Return resul
        End Try

    End Function



End Class