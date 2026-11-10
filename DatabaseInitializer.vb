Imports System.Data
Imports Microsoft.Data.Sqlite
Imports System.IO
Imports System.Collections.Generic

Public Module DatabaseInitializer
    Public ConnectionString As String = "Data Source=subaru_parts.db"

    Public Sub InitAndPopulateData(
        csvFilePath As String, 
        Optional forceRecreate As Boolean = False
    )
        If forceRecreate AndAlso File.Exists("subaru_parts.db") Then
            File.Delete("subaru_parts.db")
        End If

        Using conn As New SqliteConnection(ConnectionString)
            conn.Open()

            Dim createTablesSql As String = "
                CREATE TABLE IF NOT EXISTS Categories (
                    CategoryID INTEGER PRIMARY KEY,
                    CategoryName TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Parts (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PartNumber TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    DealerCost REAL NOT NULL,
                    MSRP REAL NOT NULL,
                    CategoryID INTEGER NOT NULL,
                    EffectiveDate TEXT NOT NULL,
                    Revision INTEGER DEFAULT 1,
                    IsActive INTEGER DEFAULT 1,
                    SupersededBy TEXT DEFAULT NULL
                );"

            Using cmd As New SqliteCommand(createTablesSql, conn)
                cmd.ExecuteNonQuery()
            End Using

            SeedCategories(conn)

            If File.Exists(csvFilePath) Then
                SeedPartsFromCSV(conn, csvFilePath)
            Else
                Console.WriteLine(
                    $"Warning: CSV file '{csvFilePath}' was not found for seeding."
                )
            End If
        End Using
    End Sub

    Private Sub SeedCategories(conn As SqliteConnection)
        Dim checkSql As String = "SELECT COUNT(*) FROM Categories"
        Using checkCmd As New SqliteCommand(checkSql, conn)
            Dim count As Integer = Convert.ToInt32(checkCmd.ExecuteScalar())
            If count > 0 Then Return 
        End Using

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
            Dim insertSql As String = 
            "INSERT INTO Categories (CategoryID, CategoryName) " &
            "VALUES (@catId, @catName)"
            Using cmd As New SqliteCommand(insertSql, conn, transaction)
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

        Console.WriteLine("Successfully seeded 12 categories into database.")
    End Sub

    Private Sub SeedPartsFromCSV(conn As SqliteConnection, csvFilePath As String)
        Dim checkSql As String = "SELECT COUNT(*) FROM parts"
        Using checkCmd As New SqliteCommand(checkSql, conn)
            Dim count As Integer = Convert.ToInt32(checkCmd.ExecuteScalar())
            If count > 0 Then Return
        End Using

        Dim lines As String() = File.ReadAllLines(csvFilePath)
        Dim insertedCount As Integer = 0
        Dim effDateStr As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")

        Using transaction = conn.BeginTransaction()
            Dim insertSql As String = "
                INSERT INTO Parts (
                    PartNumber, Description, DealerCost, MSRP, CategoryID, 
                    EffectiveDate, Revision, IsActive, SupersededBy
                )
                VALUES (
                    @partNum, @desc, @cost, @msrp, @catId, 
                    @effDate, @rev, @active, @superBy
                )"

            Using cmd As New SqliteCommand(insertSql, conn, transaction)
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
                            Dim isActiveVal As Integer = 1
                            Dim revisionVal As Integer = 1
                            Dim superByVal As Object = DBNull.Value

                            ''' IsActive column
                            If cols.Length >= 6 AndAlso _
                                Not String.IsNullOrWhiteSpace(cols(5)) Then

                                Integer.TryParse(cols(5).Trim(), isActiveVal)
                            End If

                            ''' Revision column
                            If cols.Length >= 7 AndAlso _
                                Not String.IsNullOrWhiteSpace(cols(6)) Then

                                Integer.TryParse(cols(6).Trim(), revisionVal)
                            End If

                            ''' SupersededBy column
                            If cols.Length >= 8 AndAlso _
                                Not String.IsNullOrWhiteSpace(cols(7)) Then 

                                superByVal = cols(7).Trim()
                            End If

                            cmd.Parameters("@partNum").Value = cols(0).Trim()
                            cmd.Parameters("@desc").Value = cols(1).Trim()
                            cmd.Parameters("@cost").Value = CDbl(cols(2).Trim())
                            cmd.Parameters("@msrp").Value = CDbl(cols(3).Trim())
                            cmd.Parameters("@catId").Value = CInt(cols(4).Trim())
                            cmd.Parameters("@effDate").Value = effDateStr
                            cmd.Parameters("@active").Value = isActiveVal
                            cmd.Parameters("@rev").Value = revisionVal
                            cmd.Parameters("@superBy").Value = superByVal

                            cmd.ExecuteNonQuery()
                            insertedCount += 1
                        End If
                    End If
                Next
            End Using
            transaction.Commit()
        End Using

        Console.WriteLine(
            $"Successfully seeded {insertedCount} parts into SQLite database"
        )
    End Sub
End Module
