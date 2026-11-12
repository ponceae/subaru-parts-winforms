Imports System
Imports System.Drawing
Imports System.Windows.Forms

Public Class SubaruPartsForm
    Inherits Form

    Private viewport As New Panel With {
        .Dock = DockStyle.Fill, .BackColor = ColorTranslator.FromHtml("#FFFFFF")
    }
    private navyTile As Color = ColorTranslator.FromHtml("#003366")
    private rallyBlue As Color = ColorTranslator.FromHtml("#0052A5")
    private charcoalTile As Color = ColorTranslator.FromHtml("#222A35")
    private subaruGold As Color = ColorTranslator.FromHtml("#F5C518")

    Public Sub New()
        Text = "Subaru Parts Tool"
        Size = New Size(1050, 620)
        StartPosition = FormStartPosition.CenterScreen
        Controls.Add(viewport)
        ShowHomeMenu()
    End Sub

    Private Sub ShowHomeMenu()
        viewport.Controls.Clear()

        Dim header As New Label With {
            .Text = "Subaru Parts Information",
            .Dock = DockStyle.Top, .Height = 58, .Padding = New Padding(22, 16, 0, 0),
            .Font = New Font("Segoe UI", 14, FontStyle.Bold), .ForeColor = navyTile
        }

        Dim goldRule As New Panel With {
            .Dock = DockStyle.Top, .Height = 3, .BackColor = subaruGold
        }

        Dim grid As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill, 
            .Padding = New Padding(15, 14, 15, 15), 
            .AutoScroll = True
        }
        grid.Controls.AddRange({
            Tile(
                "1. All Parts", 
                navyTile, 
                ColorTranslator.FromHtml("#FFFFFF"),
                Sub() OpenGridView(
                    "1. All Parts", 
                    Nothing, 
                    Function(q) PartQueryService.GetAllPartsWithCategories()
                )
            ),
            Tile(
                "2. Parts by Category", 
                navyTile, 
                ColorTranslator.FromHtml("#FFFFFF"),
                Sub() OpenGridView(
                    "2. Parts by Category", 
                    "Category ID (0-11):", 
                    Function(q) PartQueryService.GetPartsByCategory(
                        If(Integer.TryParse(q, Nothing), cInt(q), 0)
                    )
                )
            ),
            Tile(
                "3. Keyword Search", 
                rallyBlue, 
                ColorTranslator.FromHtml("#FFFFFF"),
                Sub() OpenGridView(
                    "3. Keyword Search", 
                    "Description Keyword:", 
                    Function(q) PartQueryService.SearchPartsByKeyword(q)
                )
            ),
            Tile(
                "4. 5-Digit Prefix Search", 
                rallyBlue, 
                ColorTranslator.FromHtml("#FFFFFF"),
                Sub() OpenGridView(
                    "4. 5-Digit Prefix Search", 
                    "Prefix:", 
                    Function(q) PartQueryService.GetPartsByNumericPrefix(q)
                )
            ),
            Tile(
                "5. Active Replacement", 
                rallyBlue, 
                ColorTranslator.FromHtml("#FFFFFF"),
                Sub() OpenCardView("5. Active Replacement")
            ),
            Tile(
                "6. High Profit Parts", 
                charcoalTile, 
                ColorTranslator.FromHtml("#FFFFFF"),
                Sub() OpenGridView(
                    "6. High Profit Parts",
                    "Min Profit ($):",
                    Function(q) PartQueryService.GetHighProfitabilityParts(
                        If(Double.TryParse(q, Nothing), CDbl(q), 50)
                    )
                )
            ),
            Tile(
                "7. Supersession Chain", 
                charcoalTile, 
                ColorTranslator.FromHtml("#FFFFFF"),
                Sub() OpenGridView(
                    "7. Supersession Chain",
                    "Part Number:",
                    Function(q) PartQueryService.GetSupersessionHistory(q)
                )
            )
        })

        viewport.Controls.AddRange({grid, goldRule, header})
    End Sub

    Private Function Tile(
        title As String, bg As Color, fg As Color, onClick As Action
    ) As Button
        Dim btn As New Button With {
            .Text = title, .Size = New Size(165, 115), 
            .Margin = New Padding(7),
            .BackColor = bg, 
            .ForeColor = fg, 
            .FlatStyle = FlatStyle.Flat,
            .TextAlign = 
                ContentAlignment.TopLeft, .Padding = New Padding(12, 12, 10, 10),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand
        }
        btn.FlatAppearance.BorderSize = 2
        btn.FlatAppearance.BorderColor = bg
        AddHandler btn.MouseEnter, Sub() btn.FlatAppearance.BorderColor = subaruGold
        AddHandler btn.MouseLeave, Sub() btn.FlatAppearance.BorderColor = bg
        AddHandler btn.Click, Sub() onClick()
        Return btn
    End Function

    Private Sub OpenGridView(
        title As String, prompt As String, queryFn As Func(Of String, Object)
    )
        Dim dgv As New DataGridView With {
            .Dock = DockStyle.Fill, 
            .ReadOnly = True, 
            .BackgroundColor = ColorTranslator.FromHtml("#FFFFFF"),
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BorderStyle = BorderStyle.None
        }
        BuildQueryScreen(title, prompt, Sub(q) dgv.DataSource = queryFn(q), dgv)
        If prompt Is Nothing Then
            dgv.DataSource = queryFn("")
        End If
    End Sub

    Private Sub OpenCardView(title As String)
        Dim lbl As New Label With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 12),
            .Padding = New Padding(24),
            .BackColor = ColorTranslator.FromHtml("#FFFFFF")
        }
        BuildQueryScreen(
            title, "Part Number:", Sub(q) 
                Dim p = PartQueryService.GetActiveReplacement(q) 
                lbl.Text = If(
                    p Is Nothing, 
                    "No active replacement found.", 
                    $"{p.PartNumber} - {p.Description}{vbCrLf}" &
                    $"Cost: {p.DealerCost:C2} | MSRP: {p.MSRP:C2}"
                ) 
                End Sub, 
                lbl
            )
    End Sub

    Private Sub BuildQueryScreen(
        title As String, prompt As String, onRun As Action(Of String), body As Control
    )
        viewport.Controls.Clear()

        Dim topBar As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .Height = 50,
            .Padding = New Padding(10, 9, 10, 5),
            .BackColor = navyTile
        }
        Dim backBtn As New Button With {
            .Text = "← Menu",
            .AutoSize = True,
            .BackColor = ColorTranslator.FromHtml("#FFFFFF"),
            .ForeColor = navyTile,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler backBtn.Click, Sub() ShowHomeMenu()

        Dim titleLbl As New Label With {
            .Text = title, 
            .AutoSize = True,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#FFFFFF"),
            .Margin = New Padding(10, 5, 20, 0)
        }
        topBar.Controls.AddRange({backBtn, titleLbl})

        If prompt IsNot Nothing Then
            Dim input As New TextBox With {
                .Width = 185, 
                .BackColor = ColorTranslator.FromHtml("#FFFFFF")
            }
            Dim runBtn As New Button With {
                .Text = "Run",
                .AutoSize = True,
                .BackColor = subaruGold,
                .ForeColor = charcoalTile,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            }
            AddHandler runBtn.Click, Sub() onRun(input.Text)
            AddHandler input.KeyDown, Sub(s, e) 
                If e.KeyCode = Keys.Enter Then onRun(input.Text)
            End Sub
            topBar.Controls.AddRange({
                New Label With {
                    .Text = prompt,
                    .AutoSize = True,
                    .ForeColor = ColorTranslator.FromHtml("#FFFFFF"),
                    .Margin = New Padding(0, 6, 4, 0)
                },
                input,
                runBtn
            })
        End if

        viewport.Controls.AddRange({body, topBar})
    End Sub
End Class
