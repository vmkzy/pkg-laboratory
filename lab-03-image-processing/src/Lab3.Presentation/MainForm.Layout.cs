namespace Lab3.Presentation;

public partial class MainForm
{
    private bool _layoutReady;
    private bool _updatingLayout;

    private void InitializeResponsiveLayout()
    {
        _layoutReady = true;
        FontChanged += (_, _) => UpdateLayoutMetrics();
        imagesPanel.SizeChanged += (_, _) => UpdateLayoutMetrics();
        foreach (var combo in new[] { methodComboBox, _border, _line })
            combo.FontChanged += (_, _) => UpdateLayoutMetrics();
        DpiChanged += (_, _) =>
        {
            // Пересчёт после того, как Windows Forms закончит масштабировать дочерние элементы.
            if (IsHandleCreated)
                BeginInvoke((Action)UpdateLayoutMetrics);
        };
        UpdateLayoutMetrics();
    }

    private void UpdateLayoutMetrics()
    {
        if (!_layoutReady || _updatingLayout || IsDisposed || Disposing) return;
        _updatingLayout = true;
        try
        {
            foreach (var combo in new[] { methodComboBox, _border, _line })
                FitComboBox(combo);

            using var graphics = imagesPanel.CreateGraphics();
            var textHeight = TextRenderer.MeasureText(graphics, "Аg", Font, Size.Empty,
                TextFormatFlags.NoPadding).Height;
            imagesPanel.RowStyles[0].Height = textHeight + LogicalToDeviceUnits(10);

            var available = Math.Max(0, imagesPanel.ClientSize.Height - imagesPanel.Padding.Vertical
                - imagesPanel.RowStyles[0].Height);
            // Гистограммы получают достаточно места для заголовка, осей и самого графика.
            // В небольшом окне оставляем как минимум 35% области под изображения.
            var desired = Math.Max(_beforeHistogram.GetPreferredSize(Size.Empty).Height, available * 0.35f);
            imagesPanel.RowStyles[2].Height = Math.Min(desired, available * 0.65f);
        }
        finally { _updatingLayout = false; }
    }

    private static void FitComboBox(ComboBox combo)
    {
        using var graphics = combo.CreateGraphics();
        var textWidth = 0;
        foreach (var item in combo.Items)
            textWidth = Math.Max(textWidth, TextRenderer.MeasureText(graphics,
                item.ToString(), combo.Font, Size.Empty, TextFormatFlags.NoPadding).Width);

        // Помимо текста нужно место под стрелку списка и внутренние отступы.
        var width = textWidth + combo.PreferredHeight + combo.LogicalToDeviceUnits(16);
        combo.Width = width;
        combo.DropDownWidth = width;
    }
}
