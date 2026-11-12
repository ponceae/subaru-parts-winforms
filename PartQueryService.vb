Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Microsoft.Data.Sqlite

Public Module PartQueryService

    ''' <summary>
    ''' Open and return the connection to the database file.
    ''' </summary>
    Private Function GetOpenConnection() As SqliteConnection
        Dim conn as New SqliteConnection(DatabaseInitializer.ConnectionString)
        conn.Open()
        Return conn
    End Function

    ''' <summary>
    ''' Map the current row of the reader to a `PartRecord` object.
    ''' </summary>
    Private Function MapReaderToPartRecord(reader As SqliteDataReader) As PartRecord 
        Dim superByVal As String = If(
            reader("SupersededBy") Is DBNull.Value,
            Nothing,
            reader("SupersededBy").ToString()
        )
        
        Return New PartRecord(
            partNum:=reader.GetString(reader.GetOrdinal("PartNumber")),
            desc:=reader.GetString(reader.GetOrdinal("Description")),
            cost:=reader.GetDouble(reader.GetOrdinal("DealerCost")),
            msrpVal:=reader.GetDouble(reader.GetOrdinal("MSRP")),
            catId:=reader.GetInt32(reader.GetOrdinal("CategoryID")),
            rev:=reader.GetInt32(reader.GetOrdinal("Revision")),
            active:=Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("IsActive"))),
            super:=superByVal
        )
    End Function

    ''' <summary>
    ''' Use the calling function to extract a sql statement and query the database,
    ''' and binds the optional parameter, and return the mapped results.
    ''' </summary>
    Private Function ExecuteQuery(Of T)(
        Optional paramName As String = Nothing,
        Optional paramValue As T = Nothing,
        <CallerMemberName> Optional callerName As String = ""
    ) As List(Of PartRecord)
        Dim queryOutput As New List(Of PartRecord)()
        Dim sqlField As FieldInfo = GetType(SqlStatements).GetField(
            callerName, 
            BindingFlags.Public Or BindingFlags.Static
        )
        Dim sqlStr As String = CStr(sqlField.GetValue(Nothing))

        Try
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sqlStr, conn)
                    If Not String.IsNullOrEmpty(paramName) Then
                        cmd.Parameters.AddWithValue(
                            paramName, 
                            If(CObj(paramValue), DBNull.Value)
                        ) 
                    End If
                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        while reader.Read()
                            queryOutput.Add(MapReaderToPartRecord(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine(
                $"[Query Error - {callerName}]: {e.Message}"
            )   
        End Try
        Return queryOutput   
    End Function

    ''' <summary>
    ''' Query all parts with their human readable category names.
    ''' </summary>
    Public Function GetAllPartsWithCategories() As List(Of PartRecord)
        Return ExecuteQuery(Of Object)()
    End Function

    ''' <summary>
    ''' Query parts with a profit margin >= minProfit (default=50.00), in desc order.
    ''' </summary>
    Public Function GetHighProfitabilityParts(
        Optional minProfit As Double = 50.00
    ) As List(Of PartRecord)
        Return ExecuteQuery("@minProfit", minProfit)
    End Function

    ''' <summary>
    ''' Query and filter active parts by CategoryID (0-11).
    ''' </summary>
    Public Function GetPartsByCategory(catId As Integer) As List(Of PartRecord)
        Return ExecuteQuery("@catId", catId)
    End Function

    ''' <summary>
    ''' Look up the human readable category name by its CategoryID
    ''' </summary>
    Public Function GetCategoryName(catId As Integer) As String
        Dim categoryName As String = $"Category #{catId}"

        Try 
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(SqlStatements.GetCategoryName, conn)
                    cmd.Parameters.AddWithValue("@catId", catId)
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso result IsNot DBNull.Value Then
                        categoryName = result.ToString()
                    End If
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine($"[Query Error - GetCategoryName]: {e.Message}")
        End Try

        Return categoryName
    End Function

    ''' <summary>
    ''' Case insensitive keyword search on description
    ''' </summary>
    Public Function SearchPartsByKeyword(keyword As String) As List(Of PartRecord)
        Return ExecuteQuery("@searchTerm", $"%{If(keyword, "").Trim()}%")
    End Function

    ''' <summary>
    ''' Query all active parts matching a specific 5 digit numeric prefix. Ignores
    ''' letter prefixed parts.
    ''' </summary>
    Public Function GetPartsByNumericPrefix(prefix As String) As List(Of PartRecord)
        Return ExecuteQuery("@prefix", If(prefix, "").Trim())
    End Function

    ''' <summary>
    ''' Recursively find the current active replacement part for the given part number.
    ''' </summary>
    Public Function GetActiveReplacement(partNum As String) As PartRecord 
        Return ExecuteQuery("@partNum", If(partNum, "").Trim()).FirstOrDefault()
    End Function

    ''' <summary>
    ''' Retrieve the complete supersession chain regardless of which part in the chain
    ''' is entered.
    ''' </summary>
    Public Function GetSupersessionHistory(partNum As String) As List(Of PartRecord)
        Return ExecuteQuery("@partNum", If(partNum, "").Trim())
    End Function
End Module
