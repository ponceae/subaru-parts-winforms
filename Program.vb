Imports System
Imports System.IO
Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        Dim csvFilePath As String = "parts.csv"

        If Not File.Exists(csvFilePath) Then
            MessageBox.Show(
                $"Data file '{csvFilePath}' was not found.",
                "Missing Data File",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            )
            Return 
        End If

        Try 
            DatabaseInitializer.InitAndPopulateData(csvFilePath, forceRecreate:=True)
        Catch e as Exception
            MessageBox.Show(
                $"Error: Failed to initialize database:" &
                $"{Environment.NewLine}{e.Message}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            )
            Return
        End Try

        Application.Run(New SubaruPartsForm())
    End Sub
End Module
