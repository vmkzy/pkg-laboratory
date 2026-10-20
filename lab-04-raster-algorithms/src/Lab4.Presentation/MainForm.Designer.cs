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
    private Button drawButton = null!;
    private Button clearButton = null!;
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
        var toolbar = CreateRow();
        toolbar.Controls.Add(new Label
        {
            Text = "Алгоритм:", AutoSize = true,
            Padding = new Padding(0, 8, 0, 0), Margin = new Padding(0, 0, 4, 0)
        });
        toolbar.Controls.Add(methodComboBox);
        toolbar.Controls.Add(drawButton);
        toolbar.Controls.Add(clearButton);

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
        AddInput(circleInputs, "Радиус:", radiusInput);
        circleInputs.Visible = false;

        hintLabel = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill,
            Text = "Растр 81 × 81, координаты от −40 до 40. Одна клетка — один пиксель; координата задаёт центр клетки. Y направлена вверх.",
            Padding = new Padding(0, 5, 0, 2), ForeColor = Color.DimGray
        };
        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, RowCount = 4, Margin = Padding.Empty
        };
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++)
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.Controls.Add(toolbar, 0, 0);
        settingsLayout.Controls.Add(lineInputs, 0, 1);
        settingsLayout.Controls.Add(circleInputs, 0, 2);
        settingsLayout.Controls.Add(hintLabel, 0, 3);

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

    private static void AddInput(FlowLayoutPanel row, string label, NumericUpDown input)
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
    }
}
