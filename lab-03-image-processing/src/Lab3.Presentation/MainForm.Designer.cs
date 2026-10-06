namespace Lab3.Presentation;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private Button openButton = null!;
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
            SetImage(sourcePictureBox, null);
            SetImage(resultPictureBox, null);
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        openButton = new Button();
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

        toolbar.Controls.Add(openButton);
        toolbar.Dock = DockStyle.Top;
        toolbar.Height = 52;
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(8);
        toolbar.WrapContents = false;

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
