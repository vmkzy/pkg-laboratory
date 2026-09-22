namespace Lab2.Presentation;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку с графическими файлами",
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        folderPathTextBox.Text = dialog.SelectedPath;
        startButton.Enabled = true;
        statusLabel.Text = "Папка выбрана. Можно начать анализ.";
    }

    private void StartButton_Click(object? sender, EventArgs e)
    {
        statusLabel.Text = "Каркас готов. Сканирование подключим после реализации парсеров.";
    }

    private void ResultsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (resultsGrid.CurrentRow?.DataBoundItem is null)
        {
            detailsValueLabel.Text = "Выберите файл в таблице";
        }
    }
}
