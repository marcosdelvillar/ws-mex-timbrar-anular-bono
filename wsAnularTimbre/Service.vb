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
Imports System.Xml.Serialization


' To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line.
' <System.Web.Script.Services.ScriptService()> _
<WebService(Namespace:="http://tempuri.org/")>
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)>
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Public Class Service
    Inherits System.Web.Services.WebService


    ReadOnly apiKey As String = "y7dr7tzcwjsn7k2icwsypmj6vj8nmaxuufjjcfubrfvfmxqbvmnnvgwb9nstumcmp5ifqwtjry2akhvu"  ''LLave para Produccion
    ReadOnly apiKyPruebas As String = "yywkagn3khxappz4ithfvgmbypuxinab4xxup4z48tdbcqk66tkzm5suvbkeqxmi7bymjgzpspvxc443" ''LLave para Produccion
    'Dim apiKy As String = "d5ecdce5903344c08eeb1c7d8265ae4fbfb22fec42c540979bfe03f191aa7562" ''LLave para Produccion

    <WebMethod()>
    Public Function Version() As String
        Return "Ver. 1.2.0"
    End Function


    <WebMethod()>
    Public Function CancelarCfdi(idBono As String,
                                 idFbo As String,
                                 idUsuario As Integer,
                                 contrasena As String) As String

        Dim hsInfo As New Hashtable
        Dim ds As System.Data.DataSet
        Dim mensaje As String
        Dim sRes As String = ""

        If Not contrasena.Equals("G1v3YarL@ve4946") Then
            Return "Contraseña no verificada"
        End If

        If idUsuario <= 0 Then
            Return "IdUsuario no válido."
        End If


        'IdBono = "977292"
        'IdFbo = "520000033747"

        Dim archivoXmlStatus As String = GetWriteDirectory() & idBono & "_" & idFbo & "_cancelaCFDI.xml"

        Dim oWs As New AnulacionTimbre.CancelacionClient()

        'Traemos datos del timbre
        hsInfo.Add("@IdBono", idBono)
        hsInfo.Add("@CveEmpresario", idFbo)
        ds = EjecutaDatasetSP("sicobonV2.dbo.[spTimbrar_Anulado_UUID]", sRes, hsInfo)

        If ds.Tables(0).Rows.Count = 0 Then
            mensaje = "Registro no encontrado con el status -1- *spTimbrar_Anulado_UUID*"
            EscribirArchivo(mensaje)
            Return mensaje
        End If

        Dim errorMessage As Boolean
        Dim foliosCancelados As Integer
        Dim foliosAnular(0) As AnulacionTimbre.CancelFolio
        Dim transec(0) As AnulacionTimbre.TransactionProperty
        Dim tracking As Guid
        Dim errorReturn As AnulacionTimbre.ErrorMessageCode
        Dim setting As New XmlWriterSettings()
        Dim uuId As String = ds.Tables(0).Rows(0).Item("TFD_UUID").ToString()
        Try
            EscribirArchivo("Bono para anular: " & vbCrLf & "idBono: " & idBono & vbCrLf & "UUID: " & uuId & vbCrLf & "idUsuario: " & idUsuario)
            foliosCancelados = 0
            foliosAnular(0) = New AnulacionTimbre.CancelFolio()
            foliosAnular(0).UUID = uuId
            foliosAnular(0).Reason = "03"
            transec(0) = New AnulacionTimbre.TransactionProperty

            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
            errorReturn = oWs.Cancelar(apiKey, ds.Tables(0).Rows(0).Item("EMI_RFC").ToString(), foliosAnular, transec, errorMessage, tracking)
            oWs.Close()

            hsInfo.Clear()
            hsInfo.Add("@IdBono", idBono)
            hsInfo.Add("@TransacId", tracking.ToString)
            hsInfo.Add("@idUsuario", idUsuario)
            ds = EjecutaDatasetSP("sicobonV2.dbo.[spTimbrar_AnuladoIns]", sRes, hsInfo)

            mensaje = "ENVIADO|" & tracking.ToString
            EscribirArchivo(mensaje)

            Return mensaje
        Catch ex As Exception
            mensaje = ex.Message.ToString
            EscribirArchivo(mensaje)
            Return mensaje
        End Try
    End Function

    <WebMethod()>
    Public Function EstadoTransaccion(idBono As String,
                                      idFbo As String,
                                      transactionId As String) As String

        Dim uuid As New Guid
        Dim client As AnulacionTimbre.CancelacionClient = New AnulacionTimbre.CancelacionClient()
        Dim errorReturn As New AnulacionTimbre.ErrorMessageCode
        Dim errorMessage As New AnulacionTimbre.ErrorMessageCode

        Dim resulcode As String
        Dim uuiStatus As New AnulacionTimbre.StateQueryResponse
        Dim relatUIIS() As AnulacionTimbre.FolioFiscalDetail = Nothing
        Dim totalUUIDs, UUIDRejected, UUIDSent As Integer
        Dim finished, resultado As Boolean
        Dim resultMessage As String = ""
        Dim resultCode As String = "-1" 'valor por default. En ocasiones no devuelve el ResultCode
        Dim xmlRes As String = ""
        Dim statusTimbradoAnulado As String
        Dim donde As String = ""
        Dim sw As New StringWriter()
        Dim s As XmlSerializer

        Try
            'EscribirArchivo("ENTRADA")

            donde = "A1"
            Dim archivoXmlStatusTransact As String = GetWriteDirectory() & idBono & "_" & idFbo & "_EdoTransac.xml"

            uuid = New Guid(transactionId)

            donde = "A2"

            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
            resultado = client.GetTransactionStatusDetail(apiKey, uuid, errorReturn, errorMessage, totalUUIDs, UUIDSent, UUIDRejected, finished, relatUIIS)

            donde = "A3"

            If errorReturn IsNot Nothing Then
                s = New XmlSerializer(errorReturn.GetType())
                s.Serialize(sw, errorReturn)

                EscribirArchivo("errorReturn: " & sw.ToString())

                resultMessage = errorReturn.Message
                resulcode = errorReturn.Code
                'Return "errorReturn: " & sw.ToString()
            End If

            If errorMessage IsNot Nothing Then
                s = New XmlSerializer(errorMessage.GetType())
                s.Serialize(sw, errorMessage)

                EscribirArchivo("errorMessage: no es NOTHING")

                EscribirArchivo("errorMessage: " & sw.ToString())

                resultMessage = errorMessage.Message
                resulcode = errorMessage.Code

                'Return "errorMessage: " & sw.ToString()
            End If


            'If relatUIIS IsNot Nothing Then
            '    s = New XmlSerializer(relatUIIS.GetType())
            '    s.Serialize(sw, relatUIIS)
            '    Return "errorMessage: " & sw.ToString()
            'End If


            donde = "A4"

            donde = "A5"

            If relatUIIS IsNot Nothing AndAlso relatUIIS.Length > 0 Then

                EscribirArchivo("relatUIIS: no es NOTHING")

                s = New XmlSerializer(relatUIIS.GetType())
                s.Serialize(sw, relatUIIS)
                EscribirArchivo(sw.ToString())

                '/ArrayOfFolioFiscalDetail/FolioFiscalDetail/UUIDStatus/State
                Dim xmlAnulacion As String = sw.ToString()
                Dim xmlDoc As New Xml.XmlDocument
                xmlDoc.LoadXml(xmlAnulacion)

                Dim nsManager As New XmlNamespaceManager(xmlDoc.NameTable)
                nsManager.AddNamespace("ns", "urn:reachcore.com:services:api:ws:pacservices:6.0")

                Dim root As XmlNode
                Dim detail As XmlNode

                Try
                    root = xmlDoc.DocumentElement
                    detail = root.SelectNodes("FolioFiscalDetail").Item(0)
                    statusTimbradoAnulado = detail.SelectSingleNode("ns:UUIDStatus/ns:State", nsManager).InnerText
                Catch ex As Exception
                    statusTimbradoAnulado = "ERROR888"

                    Try
                        root = xmlDoc.DocumentElement
                        detail = root.SelectNodes("FolioFiscalDetail").Item(0)
                        statusTimbradoAnulado = detail.SelectSingleNode("ns:UUIDStatus/ns:CancellationStatus", nsManager).InnerText
                    Catch ex2 As Exception
                        statusTimbradoAnulado = "ERROR999"
                    End Try

                End Try

                If relatUIIS(0).ResultCode IsNot Nothing Then
                    resultMessage = relatUIIS(0).ResultMessage
                    resultCode = relatUIIS(0).ResultCode
                End If

                If Not relatUIIS(0).ResultMessage = Nothing Then
                    resultMessage = resultMessage & " " & relatUIIS(0).ResultMessage
                End If

                If resultMessage = Nothing Then
                    resultMessage = relatUIIS(0).UUIDStatus.CancellationStatus
                End If

                Try
                    Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(AnulacionTimbre.FolioFiscalDetail))

                    Dim file As New System.IO.StreamWriter(archivoXmlStatusTransact)
                    writer.Serialize(file, relatUIIS(0))
                    file.Close()

                Catch ex As Exception
                    EscribirArchivo("No fue posible serializar la respuesta: " & vbCrLf & ex.Message.ToString)
                End Try
                xmlRes = LeerArchivo(archivoXmlStatusTransact)

                If statusTimbradoAnulado <> "ERROR888" Then
                    resultMessage = statusTimbradoAnulado
                Else
                    resultMessage = resultMessage & " * " & statusTimbradoAnulado
                End If

            End If

            donde = "A7"

            If resultMessage Is Nothing Then
                resultMessage = "No se puedo leer el estado !?"
            End If

            donde = "A8"

            If resultCode Is Nothing Then resultCode = "-2"
            If resultCode.Trim.Length = 0 Then resultCode = "-2"

            donde = "A9"

            EscribirArchivo("resultCode: " & resultCode & vbCrLf & "resultMessage: " & resultMessage)

            Dim sRes As String = ""
            Dim hsInfo As Hashtable
            hsInfo = New Hashtable
            hsInfo.Add("@IdBono", idBono)
            hsInfo.Add("@Estado", resultMessage)
            hsInfo.Add("@XmlRes", xmlRes)
            hsInfo.Add("@ResultCode", resultCode)

            donde = "A10"

            EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_AnuladoInsStatus", sRes, hsInfo)

            donde = "A11"

            Return resultMessage
        Catch ex As Exception
            Return ex.Message.ToString & " Donde: " & donde
        End Try

    End Function

    <WebMethod()>
    Public Function EstatusAnulacion(ByVal idBono As String,
                                     ByVal idFbo As String) As String

        Dim hsInfo As New Hashtable
        Dim ds As System.Data.DataSet
        Dim resul As Boolean
        Dim mensaje As String
        Dim sRes As String = ""
        Dim donde As String = ""
        Dim rutaArchivoXmlStatus As String

        Try
            donde = "B.1"
            rutaArchivoXmlStatus = GetWriteDirectory() & idBono & "_" & idFbo & "_EdoAnulacion.xml"
            donde = "B.2"

            'Traemos datos del timbre
            hsInfo.Add("@IdBono", idBono)
            hsInfo.Add("@CveEmpresario", idFbo)
            ds = EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_Anulado_UUID", sRes, hsInfo)

            donde = "B.3"

            If ds Is Nothing Then
                mensaje = "Error *spTimbrar_Anulado_UUID*: " & sRes
                EscribirArchivo(mensaje)
                Return mensaje
            End If

            donde = "B.4 Tablas: " & ds.Tables.Count

            If ds.Tables(0).Rows.Count = 0 Then
                mensaje = "Registro no encontrado con el status -1- *spTimbrar_Anulado_UUID*"
                EscribirArchivo(mensaje)
                Return mensaje
            End If
            donde = "B.5"

        Catch ex As Exception
            Return ex.Message.ToString & " DONDE-> " & donde
        End Try

        donde = "B.6"

        Dim uuid As New Guid
        Dim client As New AnulacionTimbre.CancelacionClient()
        Dim errorReturn As New AnulacionTimbre.ErrorMessageCode
        Dim sentRequest As Boolean = False
        Dim resulmessag As String = ""
        Dim resulcode As String = ""
        Dim uuiStatus As New AnulacionTimbre.StateQueryResponse
        Dim relatUIIS(0) As AnulacionTimbre.FolioFiscalDetail
        Dim resultCode As String = ""
        Dim UUIDLocal As String

        Try
            donde = "B.7"
            UUIDLocal = ds.Tables(0).Rows(0).Item("TFD_UUID").ToString
            uuid = New Guid(UUIDLocal)
            relatUIIS(0) = New AnulacionTimbre.FolioFiscalDetail
            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
            resul = client.GetFolioStatusDetail(apiKey, uuid, errorReturn, sentRequest, resulcode, resulmessag, uuiStatus, relatUIIS)

            ' Always close the client.
            client.Close()

            donde = "A.1"

            'Return resulcode & " - " & resulmessag

            Dim mensajeEstado As String
            Dim xmlRes As String = ""
            Dim sw As New StringWriter()
            Dim s As XmlSerializer

            donde = "A.2"

            'Hay error en la solicitud
            If errorReturn IsNot Nothing Then
                donde = "A.2.1"

                s = New XmlSerializer(errorReturn.GetType())
                s.Serialize(sw, errorReturn)

                xmlRes = sw.ToString
                EscribirXml(xmlRes, rutaArchivoXmlStatus)

                If errorReturn.Code IsNot Nothing _
                    AndAlso errorReturn.Message IsNot Nothing _
                    AndAlso errorReturn.Code = "" _
                    AndAlso errorReturn.Message.Trim.Length > 0 Then

                    donde = "A.2.4"
                    hsInfo = New Hashtable From {
                        {"@IdBono", idBono},
                        {"@Estado", "ErrorSolicitud"},
                        {"@XmlRes", xmlRes},
                        {"@ResultCode", "-55"}
                    }

                    donde = "A.2.5"

                    ds = EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_AnuladoInsStatus", sRes, hsInfo)
                    donde = "A.2.6"

                    Return "ErrorSolicitud-55"
                Else
                    Return "ErrorSolicitud-66"
                End If
            End If

            donde = "A.3"

            's = New XmlSerializer(relatUIIS.GetType())
            's.Serialize(sw, relatUIIS)
            'Return sw.ToString()

            donde = "A.4"

            Try
                mensajeEstado = relatUIIS(0).ResultMessage
            Catch ex As Exception
                mensajeEstado = Nothing
            End Try

            donde = "A.5"

            'Mirar si tiene resultado. Si no, el error se tiene que leer de otro lado.
            If relatUIIS IsNot Nothing AndAlso relatUIIS.Length > 0 Then
                donde = "A5.1"
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

                    Dim file As New System.IO.StreamWriter(rutaArchivoXmlStatus)
                    writer.Serialize(file, relatUIIS(0))
                    file.Close()
                Catch ex As Exception
                    EscribirArchivo("No fue posible serializar la respuesta: " & vbCrLf & ex.Message.ToString)
                End Try

                xmlRes = LeerArchivo(rutaArchivoXmlStatus)
            End If

            donde = "A.6"

            If mensajeEstado Is Nothing Then mensajeEstado = resulcode + " " + resulmessag
            If resulcode IsNot Nothing Then resultCode = resulcode

            EscribirArchivo("MensajeEstado: " & mensajeEstado.ToString)

            If resultCode Is Nothing Then resultCode = "-2"
            If resultCode.Trim.Length = 0 Then resultCode = "-2"

            hsInfo = New Hashtable
            hsInfo.Add("@IdBono", idBono)
            hsInfo.Add("@Estado", mensajeEstado)
            hsInfo.Add("@XmlRes", xmlRes)
            hsInfo.Add("@ResultCode", resultCode)

            ds = EjecutaDatasetSP("SicobonV2.dbo.spTimbrar_AnuladoInsStatus", sRes, hsInfo)

            Return mensajeEstado.ToString & " donde: " & donde
        Catch ex As Exception
            Return ex.Message.ToString & " donde: " & donde
        End Try

    End Function



End Class