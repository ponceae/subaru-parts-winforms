Public Class DealerInventoryRecord
    Public Property Manufacturer As String
    Public Property PartNumber As String
    Public Property Description As String
    Public Property StockingGroup As String
    Public Property BinLocation As String
    Public Property OnHand As Integer
    Public Property OnOrder As Integer
    Public Property BackOrder As Integer
    Public Property UnitCost As Double
    Public Property ExtendedCost As Double
    Public Property LtdSales As Integer
    Public Property LastSaleDate As DateTime?

    Public Sub New(
        mf As String,
        partNum As String,
        desc As String,
        sgp As String,
        bin As String,
        oh As Integer,
        oo As Integer,
        bo As Integer,
        cost As Double,
        extCost As Double,
        ltd As Integer,
        lastSale As DateTime?
    )
        Me.Manufacturer = mf
        Me.PartNumber = partNum
        Me.Description = desc
        Me.StockingGroup = sgp
        Me.BinLocation = bin
        Me.OnHand = oh
        Me.OnOrder = oo
        Me.BackOrder = bo
        Me.UnitCost = cost
        Me.ExtendedCost = extCost
        Me.LtdSales = ltd
        Me.LastSaleDate = lastSale
    End Sub

    Public ReadOnly Property ExactExtendedValue As Double
        Get
            Return Math.Round(OnHand * UnitCost, 2)
        End Get
    End Property

    Public Overrides Function ToString() As String
        Dim lastSaleStr As String = If(
            LastSaleDate.HasValue, 
            LastSaleDate.Value.ToString("yyyy-MM-dd"), 
            "Never"
        )
        Dim ooStr As String = If(OnOrder > 0, $" | O/O {OnOrder,3}", "")
        Dim boStr As String = If(BackOrder > 0, $" | B/O: {BackOrder,3}", "")

        Return $"{PartNumber,-15} | {Description,-35} | SGP: {StockingGroup,-4} | " &
               $"Bin: {BinLocation,-6} | O/H {OnHand,3}{ooStr}{boStr} | " & 
               $"Cost: ${UnitCost,7:F2} | Ext: ${ExtendedCost,7:F2} | " & 
               $"LTD: {LtdSales,4} | Last: {lastSaleStr}"
    End Function
End Class
