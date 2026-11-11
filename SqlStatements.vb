Public Module SqlStatements

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

    Public Const CheckCategoriesExist As String = "
        SELECT COUNT(*) 
        FROM Categories;"

    Public Const CheckPartsExist As String = "
        SELECT COUNT(*) 
        FROM Parts;"

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
    
End Module