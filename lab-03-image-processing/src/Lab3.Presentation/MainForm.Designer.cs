namespace Lab3.Presentation;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private Button openButton = null!;
    private Label methodLabel = null!;
    private FlowLayoutPanel methodPanel = null!;
    private ComboBox methodComboBox = null!;
    private Button processButton = null!;
    private Label thresholdLabel = null!;
    private FlowLayoutPanel toolbar = null!;
    private TableLayoutPanel imagesPanel = null!;
    private Label sourceTitle = null!;
    private Label resultTitle = null!;
    private PictureBox sourcePictureBox = null!;
    private PictureBox resultPictureBox = null!;
    private Label resultPlaceholder = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripProgressBar loadProgressBar = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation?.Cancel();
            SetImage(sourcePictureBox, null);
            ClearResult();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        openButton = new Button();
        methodLabel = new Label();
        methodPanel = new FlowLayoutPanel();
        methodComboBox = new ComboBox();
        processButton = new Button();
        thresholdLabel = new Label();
        toolbar = new FlowLayoutPanel();
        imagesPanel = new TableLayoutPanel();
        sourceTitle = new Label();
        resultTitle = new Label();
        sourcePictureBox = new PictureBox();
        resultPictureBox = new PictureBox();
        resultPlaceholder = new Label();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        loadProgressBar = new ToolStripProgressBar();
        toolbar.SuspendLayout();
        methodPanel.SuspendLayout();
        imagesPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)sourcePictureBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)resultPictureBox).BeginInit();
        resultPictureBox.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        openButton.AutoSize = true;
        openButton.Name = "openButton";
        openButton.Padding = new Padding(8, 4, 8, 4);
        openButton.Text = "Открыть изображение";
        openButton.Click += OpenButton_Click;

        methodLabel.AutoSize = true;
        methodLabel.Margin = new Padding(3, 10, 3, 3);
        methodLabel.Text = "Метод:";
        methodComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        methodComboBox.Items.AddRange(new object[] { "Метод Оцу", "Итеративный метод" });
        methodComboBox.Margin = new Padding(3, 6, 3, 3);
        methodComboBox.Name = "methodComboBox";
        methodComboBox.Width = 180;
        methodComboBox.SelectedIndex = 0;
        methodPanel.AutoSize = true;
        methodPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        methodPanel.Margin = new Padding(6, 0, 3, 0);
        methodPanel.WrapContents = false;
        methodPanel.Controls.Add(methodLabel);
        methodPanel.Controls.Add(methodComboBox);
        processButton.AutoSize = true;
        processButton.Enabled = false;
        processButton.Name = "processButton";
        processButton.Padding = new Padding(8, 4, 8, 4);
        processButton.Text = "Обработать";
        processButton.Click += ProcessButton_Click;
        thresholdLabel.AutoSize = true;
        thresholdLabel.Margin = new Padding(10, 10, 3, 3);
        thresholdLabel.Text = "Порог: —";

        toolbar.Controls.Add(openButton);
        toolbar.Controls.Add(methodPanel);
        toolbar.Controls.Add(processButton);
        toolbar.Controls.Add(thresholdLabel);
        toolbar.AutoSize = true;
        toolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        toolbar.Dock = DockStyle.Top;
        toolbar.Height = 52;
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(8);
        toolbar.WrapContents = true;

        sourcePictureBox.BackColor = Color.WhiteSmoke;
        sourcePictureBox.BorderStyle = BorderStyle.FixedSingle;
        sourcePictureBox.Dock = DockStyle.Fill;
        sourcePictureBox.Name = "sourcePictureBox";
        sourcePictureBox.SizeMode = PictureBoxSizeMode.Zoom;

        resultPictureBox.BackColor = Color.WhiteSmoke;
        resultPictureBox.BorderStyle = BorderStyle.FixedSingle;
        resultPictureBox.Controls.Add(resultPlaceholder);
        resultPictureBox.Dock = DockStyle.Fill;
        resultPictureBox.Name = "resultPictureBox";
        resultPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

        resultPlaceholder.BackColor = Color.WhiteSmoke;
        resultPlaceholder.Dock = DockStyle.Fill;
        resultPlaceholder.Name = "resultPlaceholder";
        resultPlaceholder.Text = "Результат обработки появится здесь";
        resultPlaceholder.TextAlign = ContentAlignment.MiddleCenter;

        sourceTitle.Dock = DockStyle.Fill;
        sourceTitle.Text = "Исходное изображение";
        sourceTitle.TextAlign = ContentAlignment.MiddleLeft;
        resultTitle.Dock = DockStyle.Fill;
        resultTitle.Text = "Результат";
        resultTitle.TextAlign = ContentAlignment.MiddleLeft;

        imagesPanel.ColumnCount = 2;
        imagesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        imagesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        imagesPanel.Controls.Add(sourceTitle, 0, 0);
        imagesPanel.Controls.Add(resultTitle, 1, 0);
        imagesPanel.Controls.Add(sourcePictureBox, 0, 1);
        imagesPanel.Controls.Add(resultPictureBox, 1, 1);
        imagesPanel.Dock = DockStyle.Fill;
        imagesPanel.Name = "imagesPanel";
        imagesPanel.Padding = new Padding(8);
        imagesPanel.RowCount = 2;
        imagesPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        imagesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        statusLabel.Spring = true;
        statusLabel.Text = "Откройте изображение";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        loadProgressBar.Style = ProgressBarStyle.Marquee;
        loadProgressBar.Visible = false;
        statusStrip.Items.Add(statusLabel);
        statusStrip.Items.Add(loadProgressBar);

        AutoScaleDimensions = new SizeF(7, 15);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 640);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Лабораторная работа 3 — обработка изображений";
        Controls.Add(imagesPanel);
        Controls.Add(toolbar);
        Controls.Add(statusStrip);
        Name = "MainForm";
        toolbar.ResumeLayout(false);
        toolbar.PerformLayout();
        methodPanel.ResumeLayout(false);
        methodPanel.PerformLayout();
        imagesPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)sourcePictureBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)resultPictureBox).EndInit();
        resultPictureBox.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
