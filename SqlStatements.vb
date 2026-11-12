Public Module SqlStatements

    ' +===================================================+
    '                   TABLE CREATION
    ' +===================================================+
    Public Const CreateCategoriesAndPartsTable As String = "
        CREATE TABLE IF NOT EXISTS Categories (
            CategoryID INTEGER PRIMARY KEY,
            CategoryName TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Parts (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            PartNumber TEXT NOT NULL UNIQUE,
            Description TEXT NOT NULL,
            DealerCost REAL NOT NULL,
            MSRP REAL NOT NULL,
            CategoryID INTEGER NOT NULL,
            EffectiveDate TEXT NOT NULL,
            Revision INTEGER DEFAULT 1,
            IsActive INTEGER DEFAULT 1,
            SupersededBy TEXT DEFAULT NULL,
            FOREIGN KEY (CategoryID) 
            REFERENCES Categories (CategoryID) 
            ON DELETE RESTRICT
        );"

    ' +===================================================+
    '                   DATA EXISTENCE CHECKS
    ' +===================================================+

    Public Const CheckCategoriesExist As String = "
        SELECT COUNT(*) 
        FROM Categories;"

    Public Const CheckPartsExist As String = "
        SELECT COUNT(*) 
        FROM Parts;"

    ' +===================================================+
    '                   TABLE INSERTS
    ' +===================================================+

    Public Const InsertCategory As String = "
        INSERT INTO Categories (
            CategoryID, 
            CategoryName
        )
        VALUES (
            @catId, 
            @catName
        );"

    Public Const InsertPart As String = "
        INSERT INTO Parts (
            PartNumber, 
            Description, 
            DealerCost, 
            MSRP, 
            CategoryID, 
            EffectiveDate, 
            Revision, 
            IsActive, 
            SupersededBy
        )
        VALUES (
            @partNum, 
            @desc, 
            @cost, 
            @msrp, 
            @catId, 
            @effDate, 
            @rev, 
            @active, 
            @superBy
        );"

    ' +===================================================+
    '                   DATABASE QUERIES
    ' +===================================================+

    Public Const GetAllPartsWithCategories As String = "
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
        ORDER BY p.CategoryID, p.PartNumber ASC;"

    Public Const GetHighProfitabilityParts As String = "
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
        ORDER BY ProfitMargin DESC;"

    Public Const GetPartsByCategory As String = "
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
        ORDER BY PartNumber ASC;"

    Public Const GetCategoryName As String = "
        Select CategoryName 
            FROM Categories 
            WHERE CategoryID = @catId;"

    Public Const SearchPartsByKeyword As String = "
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
        ORDER BY Description ASC;"

    Public Const GetPartsByNumericPrefix As String = "
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
        ORDER BY PartNumber ASC;"

    Public Const GetActiveReplacement As String = "
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

    Public Const GetSupersessionHistory As String = "
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
    
End Module
