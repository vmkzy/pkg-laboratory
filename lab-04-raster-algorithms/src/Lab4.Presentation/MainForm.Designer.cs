using Lab4.RasterData;

namespace Lab4.Presentation;

partial class MainForm
{
    private ComboBox methodComboBox = null!;
    private NumericUpDown x0Input = null!;
    private NumericUpDown y0Input = null!;
    private NumericUpDown x1Input = null!;
    private NumericUpDown y1Input = null!;
    private NumericUpDown centerXInput = null!;
    private NumericUpDown centerYInput = null!;
    private NumericUpDown radiusInput = null!;
    private NumericUpDown radiusXInput = null!;
    private NumericUpDown radiusYInput = null!;
    private NumericUpDown cellSizeInput = null!;
    private FlowLayoutPanel radiusGroup = null!;
    private FlowLayoutPanel radiusXGroup = null!;
    private FlowLayoutPanel radiusYGroup = null!;
    private Button fitButton = null!;
    private CheckBox showGridCheckBox = null!;
    private CheckBox showAxesCheckBox = null!;
    private Label pixelInfoLabel = null!;
    private Button drawButton = null!;
    private Button clearButton = null!;
    private Button benchmarkButton = null!;
    private FlowLayoutPanel lineInputs = null!;
    private FlowLayoutPanel circleInputs = null!;
    private Label hintLabel = null!;
    private Label statusLabel = null!;
    private GridPainter gridPainter = null!;
    private TableLayoutPanel rootLayout = null!;

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10);
        Text = "Лабораторная работа 4 — растровые алгоритмы";
        ClientSize = new Size(1120, 780);
        MinimumSize = new Size(960, 680);
        StartPosition = FormStartPosition.CenterScreen;

        methodComboBox = new ComboBox
        {
            Name = "methodComboBox",
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 280,
            Margin = new Padding(4, 4, 20, 4)
        };
        drawButton = new Button
        {
            Name = "drawButton", Text = "Построить", AutoSize = true,
            Padding = new Padding(12, 4, 12, 4), Margin = new Padding(0, 0, 8, 4)
        };
        clearButton = new Button
        {
            Name = "clearButton", Text = "Очистить", AutoSize = true,
            Padding = new Padding(12, 4, 12, 4), Margin = new Padding(0, 0, 0, 4)
        };
        benchmarkButton = new Button
        {
            Name = "benchmarkButton", Text = "Сравнить скорость", AutoSize = true,
            Padding = new Padding(8, 4, 8, 4), Margin = new Padding(12, 0, 0, 4)
        };
        var toolbar = CreateRow();
        toolbar.Controls.Add(new Label
        {
            Text = "Алгоритм:", AutoSize = true,
            Padding = new Padding(0, 8, 0, 0), Margin = new Padding(0, 0, 4, 0)
        });
        toolbar.Controls.Add(methodComboBox);
        toolbar.Controls.Add(drawButton);
        toolbar.Controls.Add(clearButton);
        toolbar.Controls.Add(benchmarkButton);

        x0Input = CreateNumber("x0Input", -25, -40, 40);
        y0Input = CreateNumber("y0Input", -15, -40, 40);
        x1Input = CreateNumber("x1Input", 25, -40, 40);
        y1Input = CreateNumber("y1Input", 18, -40, 40);
        lineInputs = CreateRow();
        lineInputs.Name = "lineInputs";
        AddInput(lineInputs, "X1:", x0Input);
        AddInput(lineInputs, "Y1:", y0Input);
        AddInput(lineInputs, "X2:", x1Input);
        AddInput(lineInputs, "Y2:", y1Input);

        centerXInput = CreateNumber("centerXInput", 0, -40, 40);
        centerYInput = CreateNumber("centerYInput", 0, -40, 40);
        radiusInput = CreateNumber("radiusInput", 15, 0, 40);
        circleInputs = CreateRow();
        circleInputs.Name = "circleInputs";
        AddInput(circleInputs, "Центр X:", centerXInput);
        AddInput(circleInputs, "Центр Y:", centerYInput);
        radiusGroup = AddInput(circleInputs, "Радиус:", radiusInput);
        radiusXInput = CreateNumber("radiusXInput", 25, 0, 40);
        radiusYInput = CreateNumber("radiusYInput", 12, 0, 40);
        radiusXGroup = AddInput(circleInputs, "Полуось A:", radiusXInput);
        radiusYGroup = AddInput(circleInputs, "Полуось B:", radiusYInput);
        radiusXGroup.Visible = false;
        radiusYGroup.Visible = false;
        circleInputs.Visible = false;

        cellSizeInput = CreateNumber("cellSizeInput", 8, GridViewport.MinimumCellSize, GridViewport.MaximumCellSize);
        var viewRow = CreateRow();
        AddInput(viewRow, "Масштаб, пкс/клетку:", cellSizeInput);
        fitButton = new Button
        {
            Name = "fitButton", Text = "Весь растр", AutoSize = true,
            Padding = new Padding(6, 2, 6, 2), Margin = new Padding(0, 0, 16, 0)
        };
        showGridCheckBox = new CheckBox
        {
            Name = "showGridCheckBox", Text = "Сетка", Checked = true, AutoSize = true,
            Margin = new Padding(0, 6, 12, 0)
        };
        showAxesCheckBox = new CheckBox
        {
            Name = "showAxesCheckBox", Text = "Оси", Checked = true, AutoSize = true,
            Margin = new Padding(0, 6, 16, 0)
        };
        pixelInfoLabel = new Label
        {
            Name = "pixelInfoLabel", Text = "Наведите курсор на клетку", AutoSize = true,
            Padding = new Padding(0, 7, 0, 0)
        };
        viewRow.Controls.Add(fitButton);
        viewRow.Controls.Add(showGridCheckBox);
        viewRow.Controls.Add(showAxesCheckBox);
        viewRow.Controls.Add(pixelInfoLabel);

        hintLabel = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill,
            Text = "",
            Padding = new Padding(0, 5, 0, 2), ForeColor = Color.DimGray
        };
        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, RowCount = 5, Margin = Padding.Empty
        };
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++)
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.Controls.Add(toolbar, 0, 0);
        settingsLayout.Controls.Add(lineInputs, 0, 1);
        settingsLayout.Controls.Add(circleInputs, 0, 2);
        settingsLayout.Controls.Add(hintLabel, 0, 3);
        settingsLayout.Controls.Add(viewRow, 0, 4);

        gridPainter = new GridPainter
        {
            Name = "gridPainter", Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 10), BackColor = Color.White
        };
        statusLabel = new Label
        {
            Name = "statusLabel", AutoSize = true, Dock = DockStyle.Fill,
            Padding = new Padding(8), BorderStyle = BorderStyle.FixedSingle
        };
        rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 3
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.Controls.Add(settingsLayout, 0, 0);
        rootLayout.Controls.Add(gridPainter, 0, 1);
        rootLayout.Controls.Add(statusLabel, 0, 2);
        Controls.Add(rootLayout);
        AcceptButton = drawButton;
        ResumeLayout(true);
    }

    private static FlowLayoutPanel CreateRow() => new()
    {
        AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Fill, WrapContents = true, Margin = Padding.Empty
    };

    private static NumericUpDown CreateNumber(string name, int value, int min, int max) => new()
    {
        Name = name, Minimum = min, Maximum = max, Value = value,
        Width = 82, Margin = new Padding(4), TextAlign = HorizontalAlignment.Right
    };

    private static FlowLayoutPanel AddInput(FlowLayoutPanel row, string label, NumericUpDown input)
    {
        var group = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false, Margin = new Padding(0, 0, 16, 0)
        };
        group.Controls.Add(new Label
        {
            Text = label, AutoSize = true, Padding = new Padding(0, 7, 0, 0), Margin = Padding.Empty
        });
        group.Controls.Add(input);
        row.Controls.Add(group);
        return group;
    }
}
