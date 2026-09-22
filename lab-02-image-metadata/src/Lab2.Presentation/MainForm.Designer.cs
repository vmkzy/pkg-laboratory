namespace Lab2.Presentation;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel folderLayout = null!;
    private Label folderLabel = null!;
    private TextBox folderPathTextBox = null!;
    private Button browseButton = null!;
    private Button startButton = null!;
    private Button cancelButton = null!;
    private CheckBox subdirectoriesCheckBox = null!;
    private TableLayoutPanel progressLayout = null!;
    private ProgressBar scanProgressBar = null!;
    private Label statusLabel = null!;
    private Label countersLabel = null!;
    private SplitContainer contentSplit = null!;
    private DataGridView resultsGrid = null!;
    private TableLayoutPanel previewLayout = null!;
    private Label previewTitleLabel = null!;
    private PictureBox previewPictureBox = null!;
    private Label detailsTitleLabel = null!;
    private Label detailsValueLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        folderLayout = new TableLayoutPanel();
        folderLabel = new Label();
        folderPathTextBox = new TextBox();
        browseButton = new Button();
        startButton = new Button();
        cancelButton = new Button();
        subdirectoriesCheckBox = new CheckBox();
        progressLayout = new TableLayoutPanel();
        scanProgressBar = new ProgressBar();
        statusLabel = new Label();
        countersLabel = new Label();
        contentSplit = new SplitContainer();
        resultsGrid = new DataGridView();
        previewLayout = new TableLayoutPanel();
        previewTitleLabel = new Label();
        previewPictureBox = new PictureBox();
        detailsTitleLabel = new Label();
        detailsValueLabel = new Label();
        rootLayout.SuspendLayout();
        folderLayout.SuspendLayout();
        progressLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)contentSplit).BeginInit();
        contentSplit.Panel1.SuspendLayout();
        contentSplit.Panel2.SuspendLayout();
        contentSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)resultsGrid).BeginInit();
        previewLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        SuspendLayout();

        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(folderLayout, 0, 0);
        rootLayout.Controls.Add(progressLayout, 0, 1);
        rootLayout.Controls.Add(contentSplit, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Padding = new Padding(12);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        folderLayout.ColumnCount = 4;
        folderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        folderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115F));
        folderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115F));
        folderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115F));
        folderLayout.Controls.Add(folderLabel, 0, 0);
        folderLayout.Controls.Add(folderPathTextBox, 0, 1);
        folderLayout.Controls.Add(browseButton, 1, 1);
        folderLayout.Controls.Add(startButton, 2, 1);
        folderLayout.Controls.Add(cancelButton, 3, 1);
        folderLayout.Controls.Add(subdirectoriesCheckBox, 0, 2);
        folderLayout.Dock = DockStyle.Fill;
        folderLayout.Margin = new Padding(0);
        folderLayout.RowCount = 3;
        folderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
        folderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        folderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));

        folderLabel.AutoSize = true;
        folderLabel.Dock = DockStyle.Fill;
        folderLabel.Text = "Папка с изображениями";
        folderLabel.TextAlign = ContentAlignment.MiddleLeft;

        folderPathTextBox.Dock = DockStyle.Fill;
        folderPathTextBox.Margin = new Padding(0, 3, 8, 4);
        folderPathTextBox.ReadOnly = true;
        folderPathTextBox.TabIndex = 0;

        browseButton.Dock = DockStyle.Fill;
        browseButton.Margin = new Padding(0, 2, 8, 4);
        browseButton.TabIndex = 1;
        browseButton.Text = "Выбрать...";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += BrowseButton_Click;

        startButton.Dock = DockStyle.Fill;
        startButton.Enabled = false;
        startButton.Margin = new Padding(0, 2, 8, 4);
        startButton.TabIndex = 2;
        startButton.Text = "Начать";
        startButton.UseVisualStyleBackColor = true;
        startButton.Click += StartButton_Click;

        cancelButton.Dock = DockStyle.Fill;
        cancelButton.Enabled = false;
        cancelButton.Margin = new Padding(0, 2, 0, 4);
        cancelButton.TabIndex = 3;
        cancelButton.Text = "Отмена";
        cancelButton.UseVisualStyleBackColor = true;

        subdirectoriesCheckBox.AutoSize = true;
        subdirectoriesCheckBox.Checked = true;
        subdirectoriesCheckBox.CheckState = CheckState.Checked;
        folderLayout.SetColumnSpan(subdirectoriesCheckBox, 4);
        subdirectoriesCheckBox.Dock = DockStyle.Fill;
        subdirectoriesCheckBox.TabIndex = 4;
        subdirectoriesCheckBox.Text = "Искать во вложенных папках";
        subdirectoriesCheckBox.UseVisualStyleBackColor = true;

        progressLayout.ColumnCount = 2;
        progressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        progressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        progressLayout.Controls.Add(scanProgressBar, 0, 0);
        progressLayout.Controls.Add(countersLabel, 1, 0);
        progressLayout.Controls.Add(statusLabel, 0, 1);
        progressLayout.Dock = DockStyle.Fill;
        progressLayout.Margin = new Padding(0);
        progressLayout.RowCount = 2;
        progressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        progressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        scanProgressBar.Dock = DockStyle.Fill;
        scanProgressBar.Margin = new Padding(0, 5, 12, 5);

        countersLabel.AutoSize = true;
        countersLabel.Dock = DockStyle.Fill;
        countersLabel.Text = "Обработано: 0   Ошибок: 0";
        countersLabel.TextAlign = ContentAlignment.MiddleRight;

        statusLabel.AutoSize = true;
        progressLayout.SetColumnSpan(statusLabel, 2);
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.ForeColor = Color.DimGray;
        statusLabel.Text = "Выберите папку для начала работы";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        contentSplit.Dock = DockStyle.Fill;
        contentSplit.FixedPanel = FixedPanel.Panel2;
        contentSplit.Margin = new Padding(0);
        contentSplit.Panel1.Controls.Add(resultsGrid);
        contentSplit.Panel2.Controls.Add(previewLayout);
        contentSplit.SplitterDistance = 740;
        contentSplit.TabIndex = 2;

        resultsGrid.AllowUserToAddRows = false;
        resultsGrid.AllowUserToDeleteRows = false;
        resultsGrid.AllowUserToResizeRows = false;
        resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        resultsGrid.BackgroundColor = SystemColors.Window;
        resultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        resultsGrid.Columns.Add("FileName", "Имя файла");
        resultsGrid.Columns.Add("Format", "Формат");
        resultsGrid.Columns.Add("Dimensions", "Размер");
        resultsGrid.Columns.Add("Dpi", "DPI");
        resultsGrid.Columns.Add("ColorDepth", "Глубина");
        resultsGrid.Columns.Add("Compression", "Сжатие");
        resultsGrid.Columns.Add("Status", "Статус");
        resultsGrid.Dock = DockStyle.Fill;
        resultsGrid.MultiSelect = false;
        resultsGrid.ReadOnly = true;
        resultsGrid.RowHeadersVisible = false;
        resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        resultsGrid.SelectionChanged += ResultsGrid_SelectionChanged;

        previewLayout.ColumnCount = 1;
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        previewLayout.Controls.Add(previewTitleLabel, 0, 0);
        previewLayout.Controls.Add(previewPictureBox, 0, 1);
        previewLayout.Controls.Add(detailsTitleLabel, 0, 2);
        previewLayout.Controls.Add(detailsValueLabel, 0, 3);
        previewLayout.Dock = DockStyle.Fill;
        previewLayout.Margin = new Padding(12, 0, 0, 0);
        previewLayout.Padding = new Padding(12, 0, 0, 0);
        previewLayout.RowCount = 4;
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        previewTitleLabel.AutoSize = true;
        previewTitleLabel.Dock = DockStyle.Fill;
        previewTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        previewTitleLabel.Text = "Предпросмотр";
        previewTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        previewPictureBox.BackColor = Color.WhiteSmoke;
        previewPictureBox.BorderStyle = BorderStyle.FixedSingle;
        previewPictureBox.Dock = DockStyle.Fill;
        previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

        detailsTitleLabel.AutoSize = true;
        detailsTitleLabel.Dock = DockStyle.Fill;
        detailsTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        detailsTitleLabel.Text = "Сведения о файле";
        detailsTitleLabel.TextAlign = ContentAlignment.BottomLeft;

        detailsValueLabel.AutoSize = true;
        detailsValueLabel.Dock = DockStyle.Fill;
        detailsValueLabel.ForeColor = Color.DimGray;
        detailsValueLabel.Text = "Выберите файл в таблице";

        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1080, 690);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 10F);
        MinimumSize = new Size(920, 580);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Лабораторная работа №2 — анализ графических файлов";
        rootLayout.ResumeLayout(false);
        folderLayout.ResumeLayout(false);
        folderLayout.PerformLayout();
        progressLayout.ResumeLayout(false);
        progressLayout.PerformLayout();
        contentSplit.Panel1.ResumeLayout(false);
        contentSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)contentSplit).EndInit();
        contentSplit.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)resultsGrid).EndInit();
        previewLayout.ResumeLayout(false);
        previewLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        ResumeLayout(false);
    }
}
