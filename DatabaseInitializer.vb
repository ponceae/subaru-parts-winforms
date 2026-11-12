Imports System.Data
Imports Microsoft.Data.Sqlite
Imports System.IO
Imports System.Collections.Generic

Public Module DatabaseInitializer
    Public Const CONN_STR As String = 
        "Data Source=subaru_parts.db;Foreign Keys=True;"

    ''' <summary>
    ''' Create the `Parts` and `Categories` database tables if the connection data
    ''' source does not exist, overwriting if otherwise.
    ''' 
    ''' Delegates the database seeding to helper functions.
    ''' </summary>
    Public Sub InitAndPopulateData(
        fp As String, Optional forceRecreate As Boolean = False
    )
        If forceRecreate AndAlso File.Exists("subaru_parts.db") Then
            File.Delete("subaru_parts.db")
        End If

        Using conn As New SqliteConnection(CONN_STR)
            conn.Open()

            Using cmd As New SqliteCommand(
                SqlStatements.CreateCategoriesAndPartsTable, conn
            )
                cmd.ExecuteNonQuery()
            End Using

            SeedCategories(conn)

            If File.Exists(fp) Then
                SeedPartsFromCSV(conn, fp)
            Else
                Console.WriteLine($"ERROR: File '{fp}' not found for seeding.")
            End If
        End Using
    End Sub

    ''' <summary>
    ''' Check if a table already contains rows.
    ''' </summary>
    Private Function HasData(sql As String, conn As SqliteConnection) As Boolean
        Using cmd As New SqliteCommand(sql, conn)
            Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            Return count > 0
        End Using
    End Function

    ''' <summary>
    ''' Check if a CSV column exists and contains text.
    ''' </summary>
    Private Function HasValue(cols As String(), index As Integer) As Boolean
        Return cols.Length > index AndAlso Not String.IsNullOrWhiteSpace(cols(index))
    End Function

    ''' <summary>
    ''' Populate the `Category` table with data from the given database.
    ''' </summary>
    Private Sub SeedCategories(conn As SqliteConnection)
        If HasData(SqlStatements.CheckCategoriesExist, conn) Then Return

        Dim categories As New Dictionary(Of Integer, String) From {
            {0, "Engine"},
            {1, "Transmission"},
            {2, "Suspension, Axle and Brake"},
            {3, "Steering System and Cable"},
            {4, "Engine Mount, Fuel, Exhaust and Engine Cooling"},
            {5, "Body Parts"},
            {6, "Door, Seat, Windshield, and Instrument Panel"},
            {7, "Heater and Air Condition"},
            {8, "Electrical Parts"},
            {9, "Sealing, Accessories and Trim"},
            {10, "Subaru of America Genuine Accessories"},
            {11, "Paint & Chemicals"}
        }

        Using transaction = conn.BeginTransaction()
            Using cmd As New SqliteCommand(
                SqlStatements.InsertCategory, conn, transaction
            )
                cmd.Parameters.Add("@catId", SqliteType.Integer)
                cmd.Parameters.Add("@catName", SqliteType.Text)

                For Each kvp In categories
                    cmd.Parameters("@catId").Value = kvp.Key
                    cmd.Parameters("@catName").Value = kvp.Value
                    cmd.ExecuteNonQuery()
                Next 
            End Using 
            transaction.Commit()
        End Using

        Console.WriteLine(
            $"Successfully seeded {categories.Count} categories into database."
        )
    End Sub

    ''' <summary>
    ''' Populate the `Parts` table with data from the given database. 
    '''
    ''' Fails gracefully if duplicate part numbers entries are discovered.
    ''' </summary>
    Private Sub SeedPartsFromCSV(conn As SqliteConnection, csvFilePath As String)
        If HasData(SqlStatements.CheckPartsExist, conn) Then Return

        Dim lines As String() = File.ReadAllLines(csvFilePath)
        Dim insertedCount As Integer = 0
        Dim effDateStr As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        
        Dim seenPartNums As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        dim duplicatePartNums As New List(Of String)()

        Using transaction = conn.BeginTransaction()
            Using cmd As New SqliteCommand(SqlStatements.InsertPart, conn, transaction)
                cmd.Parameters.Add("@partNum", SqliteType.Text)
                cmd.Parameters.Add("@desc", SqliteType.Text)
                cmd.Parameters.Add("@cost", SqliteType.Real)
                cmd.Parameters.Add("@msrp", SqliteType.Real)
                cmd.Parameters.Add("@catId", SqliteType.Integer)
                cmd.Parameters.Add("@effDate", SqliteType.Text)
                cmd.Parameters.Add("@rev", SqliteType.Integer)
                cmd.Parameters.Add("@active", SqliteType.Integer)
                cmd.Parameters.Add("@superBy", SqliteType.Text)

                For i As Integer = 1 to lines.Length - 1
                    Dim line As String = lines(i).Trim()
                    If line.Length > 0 Then
                        Dim cols As String() = line.Split(","c)

                        If cols.Length >= 5 Then
                            Dim partNum As String = cols(0).Trim()
                            Dim dealerCost As double = 0.0
                            Dim msrpCost As Double = 0.0
                            Dim catId As Integer = 0
                            Dim isActiveVal As Integer = 1
                            Dim revisionVal As Integer = 1
                            Dim superByVal As Object = DBNull.Value

                            Double.TryParse(cols(2).Trim(), dealerCost)
                            Double.TryParse(cols(3).Trim(), msrpCost)
                            Integer.TryParse(cols(4).Trim(), catId)

                            ''' IsActive column
                            If HasValue(cols, 5) Then 
                                Integer.TryParse(cols(5).Trim(), isActiveVal)
                            End If

                            ''' Revision column
                            If HasValue(cols, 6) Then
                                Integer.TryParse(cols(6).Trim(), revisionVal)
                            End If

                            ''' SupersededBy column
                            If HasValue(cols, 7) Then
                                superByVal = cols(7).Trim()
                            End If

                            If seenPartNums.Contains(partNum) Then
                                duplicatePartNums.Add(partNum)
                            Else
                                seenPartNums.Add(partNum)

                                cmd.Parameters("@partNum").Value = partNum
                                cmd.Parameters("@desc").Value = cols(1).Trim()
                                cmd.Parameters("@cost").Value = dealerCost
                                cmd.Parameters("@msrp").Value = msrpCost
                                cmd.Parameters("@catId").Value = catId
                                cmd.Parameters("@effDate").Value = effDateStr
                                cmd.Parameters("@active").Value = isActiveVal
                                cmd.Parameters("@rev").Value = revisionVal
                                cmd.Parameters("@superBy").Value = superByVal

                                cmd.ExecuteNonQuery()
                                insertedCount += 1
                            End If
                        End If
                    End If
                Next
            End Using
            transaction.Commit()
        End Using

        If duplicatePartNums.Count > 0 Then
            Console.WriteLine($"{duplicatePartNums.Count} Duplicate parts found")
            For Each duplicate In duplicatePartNums 
                Console.WriteLine($" - {duplicate}")
            Next
        End If
        Console.WriteLine(
            $"Successfully seeded {insertedCount} parts into SQLite database"
        )
    End Sub
End Module
