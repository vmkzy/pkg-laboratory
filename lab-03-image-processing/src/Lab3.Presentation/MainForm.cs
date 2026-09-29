namespace Lab3.Presentation;

public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "Лабораторная работа 3 — обработка изображений";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 480);
        Size = new Size(960, 640);

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 72,
            Padding = new Padding(20),
            Font = new Font(Font.FontFamily, 15, FontStyle.Bold),
            Text = "Лабораторная работа 3. Обработка изображений",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var message = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 11),
            Text = "Проект создан. Загрузка изображений и алгоритмы будут добавлены на следующих этапах.",
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(message);
        Controls.Add(title);
    }
}
