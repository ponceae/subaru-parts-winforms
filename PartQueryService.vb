Imports System.Collections.Generic
Imports Microsoft.Data.Sqlite

Public Module PartQueryService

    Private Function GetOpenConnection() As SqliteConnection
        Dim conn as New SqliteConnection(DatabaseInitializer.ConnectionString)
        conn.Open()
        Return conn
    End Function

    Private Function MapReaderToPartRecord(reader As SqliteDataReader) As PartRecord 
        Dim superByVal As String = If(reader("SupersededBy") Is DBNull.Value,
                                      Nothing,
                                      reader("SupersededBy").ToString())
        
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
    ''' Query all parts with their human readable category names.
    ''' </summary>
    Public Function GetAllPartsWithCategories() As List(Of PartRecord)
        Dim results As New List(Of PartRecord)()

        Dim sql As String = "
            SELECT
                p.PartNumber,
                p.Description,
                p.DealerCost,
                p.MSRP,
                p.CategoryID,
                p.Revision,
                p.IsActive,
                p.SupersededBy,
                c.CategoryName
            FROM Parts p
            INNER JOIN Categories c On p.CategoryID = c.CategoryID
            ORDER BY p.CategoryID, p.PartNumber ASC"
        
        Try
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            results.Add(MapReaderToPartRecord(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine(
                $"[Query Error - GetAllPartsWIthCategories]: {e.Message}"
            )
        End Try

        Return results
    End Function

    ''' <summary>
    ''' Query parts with a profit margin >= minProfit (default=50.00), in desc order.
    ''' </summary>
    Public Function GetHighProfitabilityParts(
        Optional minProfit As Double = 50.00
    ) As List(Of PartRecord)
        Dim results As New List(Of PartRecord)()

        Dim sql As String = "
            SELECT
                PartNumber,
                Description,
                DealerCost,
                MSRP,
                CategoryID,
                Revision,
                IsActive,
                SupersededBy,
                (MSRP - DealerCost) AS profitMargin
            FROM Parts
            WHERE (MSRP - DealerCost) >= @minProfit
            ORDER BY ProfitMargin DESC"

        Try
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@minProfit", minProfit)

                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            results.Add(MapReaderToPartRecord(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine(
                $"[Query Error - GetHighProfitabilityParts]: {e.Message}"
            )
        End Try

        Return results
    End Function

    ''' <summary>
    ''' Query and filter active parts by CategoryID (0-11).
    ''' </summary>
    Public Function GetPartsByCategory(catId As Integer) As List(Of PartRecord)
        Dim results As New List(Of PartRecord)()

        Dim sql As String = "
        SELECT
            PartNumber,
            Description,
            DealerCost,
            MSRP,
            CategoryID,
            Revision,
            IsActive,
            SupersededBy
        FROM Parts
        WHERE CategoryID = @catId
          AND IsActive = 1
        ORDER BY PartNumber ASC"

        Try
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@catId", catId)

                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            results.Add(MapReaderToPartRecord(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine(
                $"[Query Error - GetPartsByCategory]: {e.Message}"
            )
        End Try

        Return results
    End Function

    ''' <summary>
    ''' Look up the human readable category name by its CategoryID
    ''' </summary>
    Public Function GetCategoryName(catId As Integer) As String
        Dim categoryName As String = $"Category #{catId}"
        Dim sql As String = "
            Select CategoryName 
            FROM Categories 
            WHERE CategoryID = @catId"
        
        Try 
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
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
        Dim results As New List(Of PartRecord)()

        Dim sql As String = "
            SELECT
                PartNumber,
                Description,
                DealerCost,
                MSRP,
                CategoryID,
                Revision,
                IsActive,
                SupersededBy
            FROM Parts
            WHERE Description LIKE @searchTerm
            ORDER BY Description ASC"

        Try
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@searchTerm", $"%{keyword.Trim()}%")

                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            results.Add(MapReaderToPartRecord(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine(
                $"[Query Error - SearchPartsByKeyword]: {e.Message}"
            )
        End Try

        Return results
    End Function

    ''' <summary>
    ''' Query all active parts matching a specific 5 digit numeric prefix. Ignores
    ''' letter prefixed parts.
    ''' </summary>
    Public Function GetPartsByNumericPrefix(prefix As String) As List(Of PartRecord)
        Dim results As New List(Of PartRecord)()
        Dim cleanPrefix As String = prefix.Trim()

        Dim sql As String = "
        SELECT
            PartNumber,
            Description,
            DealerCost,
            MSRP,
            CategoryID,
            Revision,
            IsActive,
            SupersededBy
        FROM Parts
        WHERE PartNumber GLOB '[0-9]*'
          AND SUBSTR(PartNumber, 1, 5) = @prefix
          AND IsActive = 1
        ORDER BY PartNumber ASC"
    
        Try 
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@prefix", cleanPrefix)

                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            results.Add(MapReaderToPartRecord(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine(
                $"[Query Error - GetPartsByNumericPrefix]: {e.Message}"
            )
        End Try

        Return results
    End Function

    ''' <summary>
    ''' Recursively find the current active replacement part for the given part number.
    ''' </summary>
    Public Function GetActiveReplacement(partNum As String) As PartRecord 
        Dim activePart As PartRecord = Nothing

        Dim sql As String = "
            WITH RECURSIVE SupersessionChain AS (
                SELECT * FROM Parts WHERE UPPER(PartNumber) = UPPER(@partNum)
                UNION ALL
                SELECT p.* FROM Parts p
                JOIN SupersessionChain sc 
                  ON UPPER(p.PartNumber) = UPPER(sc.SupersededBy)
            )
            SELECT 
                PartNumber, 
                Description, 
                DealerCost, 
                MSRP, 
                CategoryID, 
                Revision, 
                IsActive, 
                SupersededBy
            FROM SupersessionChain
            WHERE IsActive = 1 LIMIT 1;"

        Try
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@partNum", partNum.Trim())

                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        If reader.Read() Then 
                            activePart = MapReaderToPartRecord(reader)
                        End If
                    End Using 
                End Using 
            End Using
        Catch e As Exception 
            Console.WriteLine(
                $"[Query Error - GetActiveReplacement]: {e.Message}"
            )
        End Try

        Return activePart
    End Function

    ''' <summary>
    ''' Retrieve the complete supersession chain regardless of which part in the chain
    ''' is entered.
    ''' </summary>
    Public Function GetSupersessionHistory(partNum As String) As List(Of PartRecord)
        Dim history as New List(Of PartRecord)()

        Dim sql As String = "
            WITH RECURSIVE
                ActiveEndpoint AS (
                    SELECT
                        PartNumber,
                        SupersededBy,
                        IsActive
                    FROM Parts
                    WHERE
                        UPPER(PartNumber) = UPPER(@partNum)
                    UNION ALL
                    SELECT
                        p.PartNumber,
                        p.SupersededBy,
                        p.IsActive
                    FROM Parts p
                    JOIN 
                        ActiveEndpoint ae ON p.PartNumber = ae.SupersededBy
                ),
                FullChain AS (
                    SELECT *
                    FROM Parts
                    WHERE
                        PartNumber = (
                            SELECT PartNumber
                            FROM ActiveEndpoint
                            WHERE IsActive = 1
                            LIMIT 1
                        )
                    UNION ALL
                    SELECT p.*
                    FROM Parts p
                    JOIN FullChain fc ON p.SupersededBy = fc.PartNumber
                )
                SELECT * FROM FullChain ORDER BY Revision ASC;"
        
        
        Try 
            Using conn As SqliteConnection = GetOpenConnection()
                Using cmd As New SqliteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@partNum", partNum.Trim())

                    Using reader As SqliteDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            history.Add(MapReaderToPartRecord(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch e As Exception
            Console.WriteLine(
                $"[Query Error - GetSupersessionHistory]: {e.Message}"
            )
        End Try

        Return history
    End Function
End Module
