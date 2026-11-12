Public Class PartRecord
    Public Property PartNumber As String
    Public Property Description As String
    Public Property DealerCost As Double 
    Public Property MSRP As Double
    Public Property CategoryID As Integer
    Public Property EffectiveDate As DateTime

    ''' Optional Fields
    Public Property Revision As Integer 
    Public Property IsActive As Boolean
    Public Property SupersededBy As String

    Public Sub New(
        partNum As String, 
        desc As String, 
        cost As Double, 
        msrpVal As Double, 
        catId As Integer
    )
        Me.New(partNum, desc, cost, msrpVal, catId, 1, True, Nothing)
    End Sub

    Public Sub New(
        partNum As String, 
        desc As String, 
        cost As Double, 
        msrpVal As Double, 
        catId As Integer,
        rev As Integer,
        active As Boolean,
        super As String
    )
        Me.PartNumber = partNum 
        Me.Description = desc
        Me.DealerCost = cost 
        Me.MSRP = msrpVal
        Me.CategoryID = catId

        Me.EffectiveDate = DateTime.Now

        Me.Revision = rev
        Me.IsActive = active
        Me.SupersededBy = super
    End Sub

    Public ReadOnly Property ProjectedProfitMargin As Double
        Get
            Return Math.Round(MSRP - DealerCost, 2)
        End Get
    End Property

    Public Overrides Function ToString() As String 
        Dim status As String = If(IsActive, "ACTIVE", "HISTORICAL")
        Dim superInfo As String = If(
            String.IsNullOrWhiteSpace(SupersededBy),
            "",
            $" ➔ Replaced by: {SupersededBy}"
        )

        Return $"{PartNumber,-15} | {Description,-55} | Rev {Revision,2} | " &
               $"{status,-10} | Cost: ${DealerCost,7:F2} | MSRP: ${MSRP,7:F2} | " &
               $"Profit: ${ProjectedProfitMargin,7:F2} | " & 
               $"Cat ID: {CategoryID,2}{superInfo}"
    End Function
End Class
