using ParseLab.Core;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;

namespace ParseLab.UI;

/// <summary>
/// 界面统一使用的字体、颜色与尺寸。
/// 所有控件高度都按字体的实际行高动态计算（Font.Height 随显示缩放比例变化），
/// 因此在 100%、125%、150% 等不同缩放比例的显示器上文字都不会被裁切。
/// </summary>
internal static class Ui
{
    public static readonly Font UiFont = new("微软雅黑", 9f);
    public static readonly Font UiFontSmall = new("微软雅黑", 8.5f);
    public static readonly Font TitleFont = new("微软雅黑", 9.5f, FontStyle.Bold);
    public static readonly Font MonoFont = new("Consolas", 10.5f);
    public static readonly Font MonoFontSmall = new("Consolas", 9.5f);

    public static readonly Color HeaderBack = Color.FromArgb(238, 243, 250);
    public static readonly Color BorderColor = Color.FromArgb(200, 210, 220);
    public static readonly Color AccentColor = Color.FromArgb(30, 90, 160);
    public static readonly Color WarnColor = Color.FromArgb(192, 0, 0);
    public static readonly Color OkColor = Color.FromArgb(0, 120, 60);
    public static readonly Color SplitBack = Color.FromArgb(235, 238, 242);
    public static readonly Color PanelBack = Color.FromArgb(248, 250, 252);

    // ---------------- 按字体行高计算的尺寸 ----------------

    public static int ButtonHeight => UiFont.Height + 14;
    public static int InfoHeight => UiFontSmall.Height + 10;
    public static int SummaryHeight => TitleFont.Height + 10;
    public static int GridRowHeight(Font font) => font.Height + 8;

    /// <summary>分组框内容区上边距（要避开标题所占的高度）。</summary>
    public static Padding GroupPadding(Font titleFont)
        => new(8, titleFont.Height + 2, 8, 8);

    /// <summary>统一设置文本框为等宽字体，便于查看横排的表格文本。</summary>
    public static TextBox MakeCodeBox(bool readOnly = false)
    {
        return new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = MonoFont,
            ReadOnly = readOnly,
            BackColor = readOnly ? Color.FromArgb(250, 250, 250) : Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    /// <summary>创建一个统一风格的按钮（高度按字体行高计算）。</summary>
    public static Button MakeButton(string text)
    {
        return new Button
        {
            Text = text,
            Font = UiFont,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = ButtonHeight,
            MinimumSize = new Size(0, ButtonHeight),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0, 2, 6, 2),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true
        };
    }

    /// <summary>创建一个会自动换行、高度自适应的工具栏。</summary>
    public static FlowLayoutPanel MakeToolbar()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(6, 5, 6, 5),
            Margin = new Padding(0),
            BackColor = HeaderBack
        };
    }

    /// <summary>创建一个常规标签（高度与按钮一致，便于在同一行内对齐）。</summary>
    public static Label MakeLabel(string text, Font? font = null, Color? fore = null)
    {
        return new Label
        {
            Text = text,
            Font = font ?? UiFont,
            ForeColor = fore ?? Color.Black,
            AutoSize = true,
            Height = ButtonHeight,
            Margin = new Padding(0, 2, 4, 2),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    /// <summary>
    /// 设置分割容器的初始分割位置。SplitContainer 在构造时尺寸为 0，
    /// 此时直接赋值会抛异常，因此必须等控件真正拿到尺寸之后再设置，并且只设置一次。
    /// </summary>
    public static void SetupSplit(SplitContainer split, Orientation orientation,
        double ratio, int min1, int min2)
    {
        split.Orientation = orientation;
        bool done = false;

        void Apply()
        {
            if (done) return;
            int total = orientation == Orientation.Vertical ? split.Width : split.Height;
            if (total < min1 + min2 + split.SplitterWidth + 8) return;

            try
            {
                int max = total - min2 - split.SplitterWidth;
                split.SplitterDistance = Math.Clamp((int)(total * ratio), min1, Math.Max(min1, max));
                split.Panel1MinSize = min1;
                split.Panel2MinSize = min2;
                done = true;
            }
            catch (InvalidOperationException)
            {
                // 窗口被压缩得过小时忽略，保持默认布局即可
            }
        }

        split.HandleCreated += (_, _) => Apply();
        split.SizeChanged += (_, _) => Apply();
    }

    /// <summary>
    /// 统一配置只读表格：表头高度自适应、行高按字体计算、单元格留出内边距，
    /// 从而避免文字被上下裁切。
    /// </summary>
    public static void StyleGrid(DataGridView grid, Font cellFont, Font headerFont)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
        grid.GridColor = Color.FromArgb(228, 232, 236);
        grid.Font = cellFont;
        grid.ScrollBars = ScrollBars.Both;

        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.Font = headerFont;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(3, 3, 3, 3);
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        grid.RowTemplate.Height = GridRowHeight(cellFont);
        grid.DefaultCellStyle.Padding = new Padding(3, 2, 3, 2);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 228, 250);
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
    }

    /// <summary>在指定位置写一个文本文件（UTF-8 带 BOM），供导出与生成运行结果使用。</summary>
    public static void WriteText(string path, string text)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, text, new UTF8Encoding(true));
    }
}

// =====================================================================
//                            表格 / 文本 显示控件
// =====================================================================

/// <summary>
/// 通用分析表显示控件：把一张二维表（列标题 + 行标题 + 单元格）显示成表格。
/// LL(1) 分析表、LR(0)/SLR(1) 分析表、分析过程表都复用它。
/// </summary>
public sealed class GridPanel : UserControl
{
    private readonly Label _summary = new();
    private readonly DataGridView _grid = new();
    private const string EmptyText = "（尚未生成）";

    /// <summary>当前表格内容（含行标题列），供导出使用。</summary>
    public List<string> Columns { get; } = new();
    public List<string[]> Rows { get; } = new();

    public GridPanel()
    {
        BackColor = Color.White;

        _summary.Dock = DockStyle.Top;
        _summary.Height = Ui.SummaryHeight;
        _summary.TextAlign = ContentAlignment.MiddleLeft;
        _summary.Font = Ui.UiFontSmall;
        _summary.ForeColor = Ui.AccentColor;
        _summary.BackColor = Ui.HeaderBack;
        _summary.Padding = new Padding(6, 0, 0, 0);
        _summary.Text = EmptyText;
        _summary.AutoEllipsis = true;

        _grid.Dock = DockStyle.Fill;
        Ui.StyleGrid(_grid, Ui.UiFont, Ui.UiFontSmall);
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        _grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.CellFormatting += Grid_CellFormatting;

        Controls.Add(_grid);
        Controls.Add(_summary);
    }

    /// <summary>按单元格内容的含义着色：移进、归约、接受用不同底色，便于读表。</summary>
    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
        string v = e.Value?.ToString() ?? "";
        if (v.Length == 0) return;

        if (v == "acc") e.CellStyle.BackColor = Color.FromArgb(214, 244, 219);
        else if (v.StartsWith("s") && v.Length > 1 && char.IsDigit(v[1]))
            e.CellStyle.BackColor = Color.FromArgb(228, 240, 255);
        else if (v.StartsWith("r") && v.Length > 1 && char.IsDigit(v[1]))
            e.CellStyle.BackColor = Color.FromArgb(255, 243, 219);
        else e.CellStyle.BackColor = Color.FromArgb(250, 250, 242);
    }

    /// <summary>显示一张分析表（第一列是行标题）。</summary>
    public void ShowTable(ParseTable table, string rowHeader, string? summary = null)
    {
        var columns = new List<string> { rowHeader };
        columns.AddRange(table.Columns);

        var rows = new List<string[]>();
        for (int i = 0; i < table.Rows.Count; i++)
        {
            var line = new string[table.Columns.Count + 1];
            line[0] = table.Rows[i];
            Array.Copy(table.Cells[i], 0, line, 1, table.Columns.Count);
            rows.Add(line);
        }

        string text = summary ?? $"{table.Title}　共 {table.Rows.Count} 行 × {table.Columns.Count} 列";
        ShowRows(columns, rows, text);
    }

    /// <summary>显示任意二维文本表。</summary>
    public void ShowRows(List<string> columns, List<string[]> rows, string? summary)
    {
        Columns.Clear(); Columns.AddRange(columns);
        Rows.Clear(); Rows.AddRange(rows);

        _grid.Rows.Clear();
        _grid.Columns.Clear();

        foreach (string c in columns)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = c,
                MinimumWidth = 58,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

        foreach (var r in rows) _grid.Rows.Add(r);

        // 冻结第一列（行标题），横向滚动时始终可见
        if (_grid.Columns.Count > 0) _grid.Columns[0].Frozen = true;

        _summary.Text = summary ?? $"共 {rows.Count} 行 × {Math.Max(0, columns.Count - 1)} 列";
    }

    public void Clear(string? text = null)
    {
        Columns.Clear(); Rows.Clear();
        _grid.Rows.Clear();
        _grid.Columns.Clear();
        _summary.Text = text ?? EmptyText;
    }

    /// <summary>把当前表格导出成对齐的文本行（中文按两个字符宽度计算，列对齐美观）。</summary>
    public List<string> ToTextLines()
    {
        var lines = new List<string>();
        if (Columns.Count == 0) return lines;

        int n = Columns.Count;
        var width = new int[n];
        for (int c = 0; c < n; c++) width[c] = DisplayWidth(Columns[c]);
        foreach (var r in Rows)
            for (int c = 0; c < n && c < r.Length; c++)
                width[c] = Math.Max(width[c], DisplayWidth(r[c]));

        string RowLine(IReadOnlyList<string> cells)
        {
            var sb = new StringBuilder();
            for (int c = 0; c < n; c++)
            {
                string v = c < cells.Count ? cells[c] : "";
                sb.Append(v);
                if (c < n - 1) sb.Append(new string(' ', Math.Max(1, width[c] - DisplayWidth(v) + 2)));
            }
            return sb.ToString().TrimEnd();
        }

        lines.Add(RowLine(Columns));
        lines.Add(new string('-', width.Sum() + 2 * (n - 1)));
        foreach (var r in Rows) lines.Add(RowLine(r));
        return lines;
    }

    /// <summary>字符串的显示宽度：中日韩等全角字符按 2 计，其余按 1 计。</summary>
    public static int DisplayWidth(string s)
    {
        int w = 0;
        foreach (char ch in s ?? "") w += ch >= 0x1100 && IsWide(ch) ? 2 : 1;
        return w;
    }

    private static bool IsWide(char c)
        => (c >= 0x1100 && c <= 0x115F) || (c >= 0x2E80 && c <= 0xA4CF) ||
           (c >= 0xAC00 && c <= 0xD7A3) || (c >= 0xF900 && c <= 0xFAFF) ||
           (c >= 0xFE30 && c <= 0xFE6F) || (c >= 0xFF00 && c <= 0xFF60) ||
           (c >= 0xFFE0 && c <= 0xFFE6);
}

/// <summary>
/// LR(0) 项目集规范族（识别活前缀的 DFA）的状态转换图绘制控件。
/// 采用圆周布局：状态 0 用外部指入箭头表示初态，含接受项目的状态用双圈表示，
/// 两个状态之间若有多个符号可达则合并成一条边、标号用逗号连接。
/// </summary>
public sealed class LrGraphView : Control
{
    private LrResult? _model;
    private PointF[] _pos = Array.Empty<PointF>();
    private float _radius = 20f;

    public LrGraphView()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
        Font = new Font("微软雅黑", 8.5f);
    }

    public LrResult? Model
    {
        get => _model;
        set { _model = value; Invalidate(); }
    }

    /// <summary>把当前图形绘制到指定画布上（界面显示与导出图片共用，效果完全一致）。</summary>
    public void RenderTo(Graphics g, Size size)
    {
        var old = Size;
        try
        {
            Size = size;
            using var args = new PaintEventArgs(g, new Rectangle(Point.Empty, size));
            OnPaint(args);
        }
        finally { Size = old; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);

        if (_model is null || _model.States.Count == 0)
        {
            using var f = new Font("微软雅黑", 10f);
            g.DrawString("（尚未生成，请先执行语法分析）", f, Brushes.Gray, 20, 20);
            return;
        }

        var r = _model;
        int n = r.States.Count;
        ComputeLayout(n);

        // 把两个状态之间的多个符号合并成一条边，避免同一对状态画出多条重叠的线
        var edges = new Dictionary<(int From, int To), List<string>>();
        foreach (var kv in r.Transitions)
        {
            var key = (kv.Key.Item1, kv.Value);
            if (!edges.TryGetValue(key, out var list)) edges[key] = list = new List<string>();
            list.Add(kv.Key.Item2);
        }

        // 先画边，再画结点，结点可以盖住边端
        var pairs = new HashSet<(int, int)>(edges.Keys);
        using var pen = new Pen(Color.FromArgb(70, 70, 70), 1.3f);
        foreach (var kv in edges)
        {
            string label = string.Join(",", kv.Value);
            if (kv.Key.From == kv.Key.To) DrawSelfLoop(g, _pos[kv.Key.From], label, pen);
            else DrawEdge(g, _pos[kv.Key.From], _pos[kv.Key.To], label,
                pairs.Contains((kv.Key.To, kv.Key.From)), pen);
        }

        for (int i = 0; i < n; i++) DrawNode(g, _pos[i], r, i);
    }

    private void ComputeLayout(int n)
    {
        _pos = new PointF[n];
        if (n == 1) { _pos[0] = new PointF(Width / 2f, Height / 2f); return; }

        float cx = Width / 2f, cy = Height / 2f;
        _radius = Math.Clamp(Math.Min(Width, Height) / 26f, 15f, 24f);
        float ring = Math.Min(Width, Height) / 2f - _radius - 44f;
        if (ring < _radius + 16f) ring = _radius + 16f;

        for (int i = 0; i < n; i++)
        {
            double a = -Math.PI / 2 + 2 * Math.PI * i / n;   // 从正上方开始顺时针排布
            _pos[i] = new PointF(cx + (float)(ring * Math.Cos(a)), cy + (float)(ring * Math.Sin(a)));
        }
    }

    private void DrawNode(Graphics g, PointF p, LrResult r, int id)
    {
        float rad = _radius;
        var rect = new RectangleF(p.X - rad, p.Y - rad, rad * 2, rad * 2);

        // 含接受项目（acc）的状态用双圈，其余单圈
        bool accept = r.Lr0Action.TryGetValue((id, Symbols.End), out string? a) && a == "acc";

        using var fill = new SolidBrush(accept ? Color.FromArgb(232, 244, 255) : Color.White);
        using var border = new Pen(Ui.AccentColor, 1.7f);
        g.FillEllipse(fill, rect);
        g.DrawEllipse(border, rect);
        if (accept)
        {
            float r2 = rad - 3.5f;
            g.DrawEllipse(border, new RectangleF(p.X - r2, p.Y - r2, r2 * 2, r2 * 2));
        }

        if (id == 0)   // 初态：外部指入箭头
        {
            var from = new PointF(p.X - rad - 30, p.Y - rad - 16);
            var to = new PointF(p.X - rad * 0.72f, p.Y - rad * 0.72f);
            using var pen = new Pen(Color.FromArgb(200, 60, 60), 1.7f);
            g.DrawLine(pen, from, to);
            DrawArrow(g, to, from, Brushes.Firebrick);
        }

        using var nameFont = new Font("Consolas", Math.Max(8f, rad * 0.72f), FontStyle.Bold);
        var size = g.MeasureString(id.ToString(), nameFont);
        g.DrawString(id.ToString(), nameFont, Brushes.Black,
            p.X - size.Width / 2f, p.Y - size.Height / 2f);
    }

    private void DrawEdge(Graphics g, PointF a, PointF b, string label, bool curved, Pen pen)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return;
        float ux = dx / len, uy = dy / len, nx = -uy, ny = ux;

        var p1 = new PointF(a.X + ux * _radius, a.Y + uy * _radius);
        var p2 = new PointF(b.X - ux * _radius, b.Y - uy * _radius);

        PointF labelPos;
        if (curved)
        {
            float off = Math.Min(34f, len * 0.26f);
            var ctrl = new PointF((a.X + b.X) / 2f + nx * off, (a.Y + b.Y) / 2f + ny * off);
            using var path = new GraphicsPath();
            path.AddBezier(p1, ctrl, ctrl, p2);
            g.DrawPath(pen, path);

            const float t = 0.86f;
            var near = new PointF(
                (float)(Math.Pow(1 - t, 2) * p1.X + 2 * (1 - t) * t * ctrl.X + t * t * p2.X),
                (float)(Math.Pow(1 - t, 2) * p1.Y + 2 * (1 - t) * t * ctrl.Y + t * t * p2.Y));
            DrawArrow(g, p2, near, Brushes.DimGray);
            labelPos = new PointF((a.X + b.X) / 2f + nx * (off * 1.15f), (a.Y + b.Y) / 2f + ny * (off * 1.15f));
        }
        else
        {
            g.DrawLine(pen, p1, p2);
            DrawArrow(g, p2, p1, Brushes.DimGray);
            labelPos = new PointF((p1.X + p2.X) / 2f + nx * 13f, (p1.Y + p2.Y) / 2f + ny * 13f);
        }

        DrawLabel(g, label, labelPos);
    }

    private void DrawSelfLoop(Graphics g, PointF c, string label, Pen pen)
    {
        float rad = _radius;
        float h = rad * 2.1f;
        var p1 = new PointF(c.X - rad * 0.55f, c.Y - rad * 0.83f);
        var p2 = new PointF(c.X + rad * 0.55f, c.Y - rad * 0.83f);
        var c1 = new PointF(c.X - rad * 1.5f, c.Y - rad - h);
        var c2 = new PointF(c.X + rad * 1.5f, c.Y - rad - h);

        using var path = new GraphicsPath();
        path.AddBezier(p1, c1, c2, p2);
        g.DrawPath(pen, path);
        DrawArrow(g, p2, c2, Brushes.DimGray);

        DrawLabel(g, label, new PointF(c.X, c.Y - rad - h * 0.8f));
    }

    /// <summary>画带白色底衬的标号，避免与线条重叠看不清。</summary>
    private void DrawLabel(Graphics g, string label, PointF center)
    {
        if (string.IsNullOrEmpty(label)) return;
        var size = g.MeasureString(label, Font);
        var rect = new RectangleF(center.X - size.Width / 2f - 2, center.Y - size.Height / 2f,
            size.Width + 4, size.Height);
        using var back = new SolidBrush(Color.FromArgb(240, 255, 255, 255));
        g.FillRectangle(back, rect);
        g.DrawString(label, Font, Brushes.Black, rect.X + 2, rect.Y);
    }

    private static void DrawArrow(Graphics g, PointF tip, PointF from, Brush brush)
    {
        float dx = tip.X - from.X, dy = tip.Y - from.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return;
        float ux = dx / len, uy = dy / len, nx = -uy, ny = ux;

        const float size = 9f, half = 4f;
        g.FillPolygon(brush, new[]
        {
            new PointF(tip.X, tip.Y),
            new PointF(tip.X - ux * size + nx * half, tip.Y - uy * size + ny * half),
            new PointF(tip.X - ux * size - nx * half, tip.Y - uy * size - ny * half)
        });
    }
}

/// <summary>把一段文本显示在一个标签页里的只读文本框。</summary>
public static class TextPanel
{
    public static TabPage MakeTab(string title, Control content, string? tip = null)
    {
        var page = new TabPage(title) { BackColor = Color.White, Padding = new Padding(3) };
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        if (tip is not null)
        {
            page.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = Ui.InfoHeight,
                Font = Ui.UiFontSmall,
                ForeColor = Color.DimGray,
                BackColor = Ui.PanelBack,
                Padding = new Padding(6, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = tip
            });
        }
        return page;
    }

    public static TabPage MakeGridTab(string title, GridPanel grid, string? tip = null)
        => MakeTab(title, grid, tip);
}

// =====================================================================
//                              程序入口
// =====================================================================

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.ToString(), "程序异常",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        Application.Run(new MainForm());
    }
}

// =====================================================================
//                               主窗口
// =====================================================================

/// <summary>
/// 主窗口。《编译原理》实验二“语法分析”的集成演示程序：
///   ① LL(1) 语法分析     —— 探索实验（一）：自动生成 LL(1) 分析表并模拟分析过程
///   ② LR(0) / SLR(1)     —— 探索实验（二）：自动生成项目集规范族与两张分析表
/// 文法以“四元组”形式输入：只需按行写出产生式，程序自动推导出 Vn、Vt、P、S。
/// </summary>
public sealed class MainForm : Form
{
    // ---------------- 输入区 ----------------
    private readonly TextBox _grammarBox = Ui.MakeCodeBox();
    private readonly TextBox _quadBox = Ui.MakeCodeBox(readOnly: true);
    private readonly TextBox _inputBox = new();
    private readonly ComboBox _grammarPreset = new();
    private readonly ComboBox _inputPreset = new();
    private readonly ToolStripStatusLabel _status = new();

    // ---------------- LL(1) 结果 ----------------
    private readonly GridPanel _llTable = new();
    private readonly GridPanel _llProcess = new();
    private readonly TextBox _llResult = Ui.MakeCodeBox(readOnly: true);
    private readonly TextBox _llSets = Ui.MakeCodeBox(readOnly: true);
    private readonly TextBox _llTransform = Ui.MakeCodeBox(readOnly: true);

    // ---------------- LR 结果 ----------------
    private readonly GridPanel _lrStates = new();
    private readonly GridPanel _lr0Table = new();
    private readonly GridPanel _slrTable = new();
    private readonly GridPanel _lrProcess = new();
    private readonly TextBox _lrResult = Ui.MakeCodeBox(readOnly: true);
    private readonly LrGraphView _lrGraph = new();
    private readonly ComboBox _lrMode = new();

    private Grammar? _grammar;
    private Ll1Result? _ll;
    private LrResult? _lr;

    /// <summary>内置示例：一整套“文法 + 待识别字符串”，点击即可演示。</summary>
    public static readonly (string Name, string Grammar, string Input)[] Samples =
    {
        ("① 指导书示例文法 G[E]（含左递归，LL(1) 需先改造）",
            "E→E+T|T\nT→T*F|F\nF→(E)|i", "i+i"),
        ("② 教材 G′[E]（已消除左递归，标准 LL(1) 文法）",
            "E→TE′\nE′→+TE′|ε\nT→FT′\nT′→*FT′|ε\nF→(E)|i", "i+i*i"),
        ("③ 教材 LR 文法 S→CC，C→cC|d",
            "S→CC\nC→cC|d", "ccdd"),
        ("④ 表达式文法（对照 LR(0) 与 SLR(1) 的冲突情况）",
            "E→E+T|T\nT→T*F|F\nF→(E)|i", "i+i*i"),
        ("⑤ 悬空 else 文法（展示移进-归约冲突）",
            "S→if E then S else S|if E then S|a\nE→b", "if b then if b then a else a")
    };

    public MainForm()
    {
        Text = "ParseLab 语法分析实验系统 —— 《编译原理》实验二";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 620);
        Font = Ui.UiFont;
        BackColor = Color.White;

        var work = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);
        Size = new Size(
            Math.Min(1360, Math.Max(900, (int)(work.Width * 0.94))),
            Math.Min(900, Math.Max(620, (int)(work.Height * 0.94))));

        BuildUi();
        LoadSample(0);
    }

    // ==================================================================
    //                              界面搭建
    // ==================================================================

    private void BuildUi()
    {
        var menu = BuildMenu();
        MainMenuStrip = menu;
        Controls.Add(BuildSplit());          // Fill
        Controls.Add(BuildStatusStrip());    // Bottom
        Controls.Add(menu);                  // Top
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Font = Ui.UiFont, BackColor = Ui.PanelBack };

        var file = new ToolStripMenuItem("文件(&F)");
        file.DropDownItems.Add(new ToolStripMenuItem("导出全部结果…", null, (_, _) => ExportAll()));
        file.DropDownItems.Add(new ToolStripMenuItem("导出结果到程序目录的 output 文件夹", null,
            (_, _) => ExportToOutputFolder()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("退出", null, (_, _) => Close()));

        var run = new ToolStripMenuItem("分析(&A)");
        run.DropDownItems.Add(new ToolStripMenuItem("开始语法分析（F5）", null, (_, _) => Analyze()));
        run.DropDownItems.Add(new ToolStripSeparator());
        run.DropDownItems.Add(new ToolStripMenuItem("清空结果", null, (_, _) => ClearResults()));

        var help = new ToolStripMenuItem("帮助(&H)");
        help.DropDownItems.Add(new ToolStripMenuItem("关于本程序", null, (_, _) => ShowAbout()));
        help.DropDownItems.Add(new ToolStripMenuItem("文法书写说明", null, (_, _) => ShowGrammarHelp()));

        menu.Items.Add(file);
        menu.Items.Add(run);
        menu.Items.Add(help);
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.F5) Analyze(); };
        return menu;
    }

    private StatusStrip BuildStatusStrip()
    {
        var strip = new StatusStrip { Font = Ui.UiFontSmall, BackColor = Ui.PanelBack };
        _status.Text = "请选择或输入文法，然后点击“开始语法分析”。";
        _status.Spring = true;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        strip.Items.Add(_status);
        return strip;
    }

    /// <summary>
    /// 整体布局：左侧编辑文法，右侧显示分析结果（LL(1) 与 LR 两个标签页）。
    /// </summary>
    private Control BuildSplit()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 6,
            BackColor = Ui.SplitBack
        };
        split.Panel1.Controls.Add(BuildGrammarSide());
        split.Panel2.Controls.Add(BuildResultSide());
        Ui.SetupSplit(split, Orientation.Vertical, 0.36,
            Ui.ButtonHeight * 8, Ui.ButtonHeight * 9);
        return split;
    }

    // ------------------------- 左侧：文法输入 -------------------------

    private Control BuildGrammarSide()
    {
        // 工具栏
        var toolbar = Ui.MakeToolbar();

        var lblPreset = Ui.MakeLabel("内置示例：");
        _grammarPreset.Font = Ui.UiFont;
        _grammarPreset.DropDownStyle = ComboBoxStyle.DropDownList;
        _grammarPreset.Width = Ui.UiFont.Height * 26;
        _grammarPreset.Margin = new Padding(0, 2, 8, 2);
        foreach (var s in Samples) _grammarPreset.Items.Add(s.Name);
        _grammarPreset.SelectedIndex = 0;
        _grammarPreset.SelectedIndexChanged += (_, _) =>
        {
            if (_grammarPreset.SelectedIndex >= 0) LoadSample(_grammarPreset.SelectedIndex);
        };

        var btnClear = Ui.MakeButton("清空");
        btnClear.Click += (_, _) =>
        {
            _grammarBox.Clear();
            _quadBox.Clear();
            ClearResults();
            _status.Text = "已清空。";
        };

        var btnHelp = Ui.MakeButton("文法书写说明");
        btnHelp.Click += (_, _) => ShowGrammarHelp();

        toolbar.Controls.AddRange(new Control[] { lblPreset, _grammarPreset, btnClear, btnHelp });

        // 文法输入框
        _grammarBox.Dock = DockStyle.Fill;
        _grammarBox.TextChanged += (_, _) => RefreshQuadruple();
        var grammarGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "文法（四元组输入：每行一条产生式，右部可用 | 分隔）",
            Font = Ui.TitleFont,
            Padding = Ui.GroupPadding(Ui.TitleFont),
            BackColor = Color.White
        };
        grammarGroup.Controls.Add(_grammarBox);

        // 四元组展示
        _quadBox.Dock = DockStyle.Fill;
        var quadGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "文法四元组 G = (Vn, Vt, P, S)",
            Font = Ui.TitleFont,
            Padding = Ui.GroupPadding(Ui.TitleFont),
            BackColor = Color.White
        };
        quadGroup.Controls.Add(_quadBox);

        var inner = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 6,
            Orientation = Orientation.Horizontal,
            BackColor = Ui.SplitBack
        };
        inner.Panel1.Controls.Add(grammarGroup);
        inner.Panel2.Controls.Add(quadGroup);
        Ui.SetupSplit(inner, Orientation.Horizontal, 0.62,
            Ui.ButtonHeight * 5, Ui.ButtonHeight * 3);

        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        host.Controls.Add(inner);
        host.Controls.Add(toolbar);
        return host;
    }

    // ------------------------- 右侧：分析结果 -------------------------

    private Control BuildResultSide()
    {
        // 待识别字符串输入行
        var toolbar = Ui.MakeToolbar();

        toolbar.Controls.Add(Ui.MakeLabel("待识别字符串：", Ui.TitleFont));
        _inputBox.Font = Ui.MonoFont;
        _inputBox.Width = Ui.UiFont.Height * 16;
        _inputBox.Margin = new Padding(0, 2, 8, 2);
        _inputBox.Text = "i+i";

        var btnAnalyze = Ui.MakeButton("开始语法分析（F5）");
        btnAnalyze.Font = new Font(Ui.UiFont, FontStyle.Bold);
        btnAnalyze.Click += (_, _) => Analyze();

        var lblInputPreset = Ui.MakeLabel("常用输入：");
        _inputPreset.Font = Ui.UiFont;
        _inputPreset.DropDownStyle = ComboBoxStyle.DropDownList;
        _inputPreset.Width = Ui.UiFont.Height * 12;
        _inputPreset.Margin = new Padding(0, 2, 8, 2);
        foreach (string s in new[] { "i+i", "i+i*i", "(i+i)*i", "i*", "i+i*", "ccdd", "cdcd", "dcd", "dd" })
            _inputPreset.Items.Add(s);
        _inputPreset.SelectedIndex = 0;
        _inputPreset.SelectedIndexChanged += (_, _) =>
        {
            if (_inputPreset.SelectedIndex >= 0) _inputBox.Text = _inputPreset.Text;
        };

        var btnExport = Ui.MakeButton("导出全部结果…");
        btnExport.Click += (_, _) => ExportAll();

        toolbar.Controls.AddRange(new Control[]
        {
            _inputBox, btnAnalyze, lblInputPreset, _inputPreset, btnExport
        });

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = Ui.UiFont, Padding = new Point(16, 5) };
        tabs.Controls.Add(BuildLlTab());
        tabs.Controls.Add(BuildLrTab());

        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        host.Controls.Add(tabs);
        host.Controls.Add(toolbar);
        return host;
    }

    private TabPage BuildLlTab()
    {
        var inner = new TabControl { Dock = DockStyle.Fill, Font = Ui.UiFont, Padding = new Point(14, 5) };
        // 按指导书要求的顺序：分析表 → 分析过程表 → 分析结果
        inner.Controls.Add(TextPanel.MakeGridTab("① 分析表", _llTable));
        inner.Controls.Add(TextPanel.MakeGridTab("② 分析过程表", _llProcess));
        inner.Controls.Add(TextPanel.MakeTab("③ 分析结果", _llResult));
        inner.Controls.Add(TextPanel.MakeTab("④ FIRST / FOLLOW / SELECT", _llSets));
        inner.Controls.Add(TextPanel.MakeTab("⑤ 文法改造", _llTransform));

        var page = new TabPage("LL(1) 语法分析") { BackColor = Color.White, Padding = new Padding(3) };
        page.Controls.Add(inner);
        return page;
    }

    private TabPage BuildLrTab()
    {
        // LR 分析表用哪一张（LR(0) 还是 SLR(1)）由下拉框切换
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(4, 4, 4, 4),
            Margin = new Padding(0),
            BackColor = Ui.HeaderBack
        };
        header.Controls.Add(Ui.MakeLabel("分析过程所依据的分析表：", Ui.TitleFont));
        _lrMode.Font = Ui.UiFont;
        _lrMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _lrMode.Width = Ui.UiFont.Height * 12;
        _lrMode.Margin = new Padding(0, 2, 8, 2);
        _lrMode.Items.AddRange(new object[] { "SLR(1) 分析表", "LR(0) 分析表" });
        _lrMode.SelectedIndex = 0;
        _lrMode.SelectedIndexChanged += (_, _) => RefreshLrProcess();

        header.Controls.Add(_lrMode);

        var inner = new TabControl { Dock = DockStyle.Fill, Font = Ui.UiFont, Padding = new Point(14, 5) };
        inner.Controls.Add(TextPanel.MakeGridTab("① LR(0) 分析表", _lr0Table));
        inner.Controls.Add(TextPanel.MakeGridTab("② SLR(1) 分析表", _slrTable));
        inner.Controls.Add(TextPanel.MakeGridTab("③ 分析过程表", _lrProcess));
        inner.Controls.Add(TextPanel.MakeTab("④ 分析结果", _lrResult));
        inner.Controls.Add(TextPanel.MakeGridTab("⑤ 项目集规范族", _lrStates));

        // 项目集规范族的状态转换图（识别活前缀的 DFA）
        var graphHeader = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(2, 2, 2, 4),
            Margin = new Padding(0),
            BackColor = Color.White
        };
        var btnSavePng = Ui.MakeButton("导出状态图…");
        btnSavePng.Click += (_, _) => ExportGraph();
        graphHeader.Controls.Add(btnSavePng);
        _lrGraph.Dock = DockStyle.Fill;

        var graphHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        graphHost.Controls.Add(_lrGraph);
        graphHost.Controls.Add(graphHeader);
        inner.Controls.Add(TextPanel.MakeTab("⑥ 状态转换图", graphHost));

        var page = new TabPage("LR(0) / SLR(1) 语法分析") { BackColor = Color.White, Padding = new Padding(3) };
        page.Controls.Add(inner);
        page.Controls.Add(header);
        return page;
    }

    // ==================================================================
    //                              功能实现
    // ==================================================================

    private void LoadSample(int index)
    {
        int i = Math.Clamp(index, 0, Samples.Length - 1);
        var s = Samples[i];
        _grammarBox.Text = s.Grammar.Replace("\r\n", "\n").Replace("\n", "\r\n");
        _grammarBox.SelectionStart = 0;
        _inputBox.Text = s.Input;
        RefreshQuadruple();
        ClearResults();
        _status.Text = $"已载入示例：{s.Name}";
    }

    /// <summary>把当前文法解析成四元组并显示；文法有错时只提示、不弹窗（边输入边更新）。</summary>
    private void RefreshQuadruple()
    {
        string text = _grammarBox.Text;
        if (text.Trim().Length == 0) { _quadBox.Clear(); return; }

        var g = Grammar.Parse(text, out var errors);
        var sb = new StringBuilder();
        sb.AppendLine(g.Quadruple());
        if (errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("提示：");
            foreach (string e in errors) sb.AppendLine("  · " + e);
        }
        _quadBox.Text = sb.ToString();
    }

    private void ClearResults()
    {
        _grammar = null; _ll = null; _lr = null;
        _llTable.Clear(); _llProcess.Clear(); _llSets.Clear(); _llTransform.Clear();
        _llResult.Clear();
        _lrStates.Clear(); _lr0Table.Clear(); _slrTable.Clear(); _lrProcess.Clear();
        _lrResult.Clear();
        _lrGraph.Model = null;
    }

    /// <summary>执行语法分析：对同一条文法同时生成 LL(1) 与 LR(0)/SLR(1) 的全部分析结果。</summary>
    private void Analyze()
    {
        string text = _grammarBox.Text;
        if (text.Trim().Length == 0)
        {
            MessageBox.Show("请先输入文法（每行一条产生式，如 E→E+T|T）。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var g = Grammar.Parse(text, out var errors);
        if (g.NonTerminals.Count == 0)
        {
            MessageBox.Show("文法解析失败：\r\n" + string.Join("\r\n", errors), "文法错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _grammar = g;
        RefreshQuadruple();

        string input = _inputBox.Text.Trim();
        if (input.Length == 0)
        {
            MessageBox.Show("请输入待识别的字符串（如 i+i）。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            _ll = Ll1Analyzer.Build(g, input);
            FillLl(_ll);

            _lr = LrAnalyzer.Build(g);
            FillLr(_lr);
            RefreshLrProcess();
        }
        catch (Exception ex)
        {
            MessageBox.Show("分析过程中出现异常：" + ex.Message, "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        sw.Stop();

        _status.Text =
            $"分析完成：Vn {g.NonTerminals.Count} 个，Vt {g.Terminals.Count} 个，产生式 {g.Productions.Count} 条；" +
            $"LL(1) 冲突 {_ll.Conflicts.Count} 处，LR(0) 冲突 {_lr.Lr0Conflicts.Count} 处，SLR(1) 冲突 {_lr.SlrConflicts.Count} 处；" +
            $"耗时 {sw.Elapsed.TotalMilliseconds:0.0} ms。";
    }

    // ------------------------------ LL(1) ------------------------------

    private void FillLl(Ll1Result r)
    {
        // ① 分析表
        _llTable.ShowTable(r.Table, "非终结符",
            $"LL(1) 语法分析表　共 {r.Table.Rows.Count} 行 × {r.Table.Columns.Count} 列" +
            (r.IsLl1 ? "　文法无冲突，是 LL(1) 文法" : $"　存在 {r.Conflicts.Count} 处冲突"));

        // ② 分析过程表（指导书要求的 5 列）
        var cols = new List<string> { "步骤", "分析栈", "当前输入a", "剩余输入串", "所用产生式" };
        var rows = new List<string[]>();
        foreach (var s in r.Process)
            rows.Add(new[] { s.Index.ToString(), s.Stack, s.CurrentInput, s.RemainingInput, s.Production });
        _llProcess.ShowRows(cols, rows,
            $"LL(1) 分析过程　共 {r.Process.Count} 步　输入串：{r.Input}");

        // ③ 分析结果
        var sb = new StringBuilder();
        sb.AppendLine("========== 一、文法 ==========");
        sb.AppendLine(r.Transformed.Quadruple());
        sb.AppendLine();
        sb.AppendLine("========== 二、LL(1) 文法判定 ==========");
        if (r.IsLl1)
            sb.AppendLine("分析表 M 中没有任何多重入口，该文法是 LL(1) 文法。");
        else
        {
            sb.AppendLine($"分析表 M 中存在 {r.Conflicts.Count} 处冲突，该文法不是 LL(1) 文法：");
            foreach (string c in r.Conflicts) sb.AppendLine("  · " + c);
        }
        sb.AppendLine();
        sb.AppendLine("========== 三、分析结果 ==========");
        sb.AppendLine("待识别字符串：" + r.Input);
        sb.AppendLine($"分析步数：{r.Process.Count} 步");
        sb.AppendLine("分析结论：" + r.Result);
        _llResult.Text = sb.ToString();

        // ④ FIRST / FOLLOW / SELECT
        _llSets.Text = BuildSetsText(r);

        // ⑤ 文法改造过程
        var tf = new StringBuilder();
        if (r.TransformSteps.Count == 0)
            tf.AppendLine("输入的文法已经满足 LL(1) 分析的要求，无需改造。");
        else
        {
            tf.AppendLine("为使文法适应 LL(1) 分析，程序自动完成了以下改造：");
            tf.AppendLine();
            for (int i = 0; i < r.TransformSteps.Count; i++)
                tf.AppendLine($"（{i + 1}）{r.TransformSteps[i]}");
            tf.AppendLine();
            tf.AppendLine("改造后的文法：");
            tf.AppendLine(r.Transformed.Quadruple());
        }
        _llTransform.Text = tf.ToString();
    }

    private static string BuildSetsText(Ll1Result r)
    {
        var g = r.Transformed;
        var sets = r.Sets;
        var sb = new StringBuilder();

        sb.AppendLine("========== FIRST 集 ==========");
        foreach (string nt in g.NonTerminals)
            sb.AppendLine($"FIRST({nt}) = {{ {string.Join(", ", sets.First[nt])} }}");

        sb.AppendLine();
        sb.AppendLine("========== FOLLOW 集 ==========");
        foreach (string nt in g.NonTerminals)
            sb.AppendLine($"FOLLOW({nt}) = {{ {string.Join(", ", sets.Follow[nt])} }}");

        sb.AppendLine();
        sb.AppendLine("========== SELECT 集 ==========");
        foreach (var p in g.Productions)
            sb.AppendLine($"SELECT({p.Text}) = {{ {string.Join(", ", sets.Select[p.Index])} }}");

        return sb.ToString();
    }

    // --------------------------- LR(0) / SLR(1) ---------------------------

    private void FillLr(LrResult r)
    {
        // ① LR(0) 分析表
        _lr0Table.ShowTable(r.Lr0Table, "状态",
            $"LR(0) 语法分析表　共 {r.States.Count} 个状态" +
            (r.Lr0Conflicts.Count == 0 ? "　无冲突" : $"　存在 {r.Lr0Conflicts.Count} 处冲突"));

        // ② SLR(1) 分析表
        _slrTable.ShowTable(r.SlrTable, "状态",
            $"SLR(1) 语法分析表　共 {r.States.Count} 个状态" +
            (r.SlrConflicts.Count == 0 ? "　无冲突，是 SLR(1) 文法" : $"　存在 {r.SlrConflicts.Count} 处冲突"));

        // ③ 分析过程表在 RefreshLrProcess 中填充（可切换所用分析表）

        // ④ 分析结果
        var sb = new StringBuilder();
        sb.AppendLine("========== 一、拓广文法 ==========");
        sb.AppendLine($"在原文法前加入产生式 {r.AugmentedStart}→{r.Input.Start}，得到拓广文法：");
        sb.AppendLine(r.Augmented.Quadruple());
        sb.AppendLine();
        sb.AppendLine("========== 二、LR(0) 项目集规范族 ==========");
        sb.AppendLine($"共 {r.States.Count} 个项目集（状态 0 为初态）。");
        sb.AppendLine();
        sb.AppendLine("========== 三、冲突与文法判别 ==========");
        if (r.Lr0Conflicts.Count == 0)
            sb.AppendLine("LR(0) 分析表无冲突，该文法是 LR(0) 文法。");
        else
        {
            sb.AppendLine($"LR(0) 分析表存在 {r.Lr0Conflicts.Count} 处冲突，该文法不是 LR(0) 文法：");
            foreach (string c in r.Lr0Conflicts) sb.AppendLine("  · " + c);
        }
        sb.AppendLine();
        if (r.SlrConflicts.Count == 0)
            sb.AppendLine("SLR(1) 分析表无冲突，该文法是 SLR(1) 文法。");
        else
        {
            sb.AppendLine($"SLR(1) 分析表存在 {r.SlrConflicts.Count} 处冲突，该文法不是 SLR(1) 文法：");
            foreach (string c in r.SlrConflicts) sb.AppendLine("  · " + c);
        }
        sb.AppendLine();
        sb.AppendLine("========== 四、分析结果 ==========");
        sb.AppendLine("待识别字符串：" + r.InputString);
        sb.AppendLine("所用分析表：" + (r.UseSlr ? "SLR(1) 分析表" : "LR(0) 分析表"));
        sb.AppendLine("分析结论：" + (r.Result.Length == 0 ? "（尚未分析）" : r.Result));
        _lrResult.Text = sb.ToString();

        // ⑤ 项目集规范族
        var cols = new List<string> { "状态", "LR(0) 项目集", "转移（读入符号 → 后继状态）" };
        var rows = new List<string[]>();
        foreach (var st in r.States)
        {
            var trans = new List<string>();
            foreach (string sym in r.Augmented.Terminals.Concat(r.Augmented.NonTerminals))
                if (r.Transitions.TryGetValue((st.Id, sym), out int to))
                    trans.Add($"{sym} → {to}");

            rows.Add(new[] { st.Id.ToString(), st.Render(r.Augmented), string.Join("　", trans) });
        }
        _lrStates.ShowRows(cols, rows, $"LR(0) 项目集规范族　共 {r.States.Count} 个状态");

        // ⑥ 状态转换图
        _lrGraph.Model = r;
    }

    /// <summary>导出项目集规范族的状态转换图。</summary>
    private void ExportGraph()
    {
        if (_lr is null)
        {
            MessageBox.Show("请先执行一次语法分析。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "导出状态转换图",
            Filter = "PNG 图片 (*.png)|*.png",
            FileName = "LR0_项目集规范族状态转换图.png"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            var size = new Size(Math.Max(760, _lrGraph.Width), Math.Max(560, _lrGraph.Height));
            using var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                _lrGraph.RenderTo(g, size);
            }
            bmp.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
            _status.Text = "状态转换图已导出到：" + dlg.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>按当前选择的分析表（LR(0) 或 SLR(1)）重新模拟分析过程。</summary>
    private void RefreshLrProcess()
    {
        if (_lr is null) return;
        bool slr = _lrMode.SelectedIndex == 0;

        LrAnalyzer.Analyze(_lr, slr, _inputBox.Text.Trim());

        var cols = new List<string> { "步骤", "状态栈", "符号栈", "当前输入", "剩余输入串", "动作" };
        var rows = new List<string[]>();
        foreach (var s in _lr.Process)
            rows.Add(new[] { s.Index.ToString(), s.StateStack, s.SymbolStack, s.CurrentInput, s.RemainingInput, s.Action });
        _lrProcess.ShowRows(cols, rows,
            $"{(slr ? "SLR(1)" : "LR(0)")} 分析过程　共 {_lr.Process.Count} 步　输入串：{_lr.InputString}");

        // 分析结论随所选分析表变化，刷新“分析结果”页
        FillLrResultOnly(_lr);
    }

    private void FillLrResultOnly(LrResult r)
    {
        string text = _lrResult.Text;
        int idx = text.IndexOf("========== 四、分析结果 ==========", StringComparison.Ordinal);
        if (idx < 0) return;

        var sb = new StringBuilder(text[..idx]);
        sb.AppendLine("========== 四、分析结果 ==========");
        sb.AppendLine("待识别字符串：" + r.InputString);
        sb.AppendLine("所用分析表：" + (r.UseSlr ? "SLR(1) 分析表" : "LR(0) 分析表"));
        sb.AppendLine("分析步数：" + r.Process.Count + " 步");
        sb.AppendLine("分析结论：" + (r.Result.Length == 0 ? "（尚未分析）" : r.Result));
        _lrResult.Text = sb.ToString();
    }

    // ------------------------------ 导出 ------------------------------

    /// <summary>把所有结果按“分析表 → 分析过程表 → 分析结果”的顺序汇成一个文本。</summary>
    private string BuildExportText()
    {
        if (_grammar is null || _ll is null || _lr is null)
            return "";

        var sb = new StringBuilder();
        sb.AppendLine("《编译原理》实验二  语法分析结果");
        sb.AppendLine("生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("################ 文法 ################");
        sb.AppendLine(_grammar.Quadruple());
        sb.AppendLine();
        sb.AppendLine("待识别字符串：" + _inputBox.Text.Trim());

        sb.AppendLine();
        sb.AppendLine("################ 一、分析表 ################");
        sb.AppendLine();
        sb.AppendLine("-------------------- 1. LL(1) 分析表 --------------------");
        foreach (string line in _llTable.ToTextLines()) sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine("-------------------- 2. LR(0) 分析表 --------------------");
        foreach (string line in _lr0Table.ToTextLines()) sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine("-------------------- 3. SLR(1) 分析表 --------------------");
        foreach (string line in _slrTable.ToTextLines()) sb.AppendLine(line);

        sb.AppendLine();
        sb.AppendLine("################ 二、分析过程表 ################");
        sb.AppendLine();
        sb.AppendLine("-------------------- 1. LL(1) 分析过程 --------------------");
        foreach (string line in _llProcess.ToTextLines()) sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine("-------------------- 2. LR 分析过程 --------------------");
        foreach (string line in _lrProcess.ToTextLines()) sb.AppendLine(line);

        sb.AppendLine();
        sb.AppendLine("################ 三、分析结果 ################");
        sb.AppendLine();
        sb.AppendLine("-------------------- 1. LL(1) --------------------");
        sb.AppendLine(_llResult.Text);
        sb.AppendLine("-------------------- 2. LR(0) / SLR(1) --------------------");
        sb.AppendLine(_lrResult.Text);
        sb.AppendLine();
        sb.Append("==================== 原始分析表（制表符分隔，便于复制到 Excel） ====================");
        sb.AppendLine();
        AppendRaw(sb, "LL(1) 分析表", _llTable);
        AppendRaw(sb, "LR(0) 分析表", _lr0Table);
        AppendRaw(sb, "SLR(1) 分析表", _slrTable);
        AppendRaw(sb, "LL(1) 分析过程", _llProcess);
        AppendRaw(sb, "LR 分析过程", _lrProcess);
        return sb.ToString();
    }

    private static void AppendRaw(StringBuilder sb, string title, GridPanel grid)
    {
        sb.AppendLine();
        sb.AppendLine("【" + title + "】");
        sb.AppendLine(string.Join("\t", grid.Columns));
        foreach (var r in grid.Rows) sb.AppendLine(string.Join("\t", r));
    }

    private void ExportAll()
    {
        if (_grammar is null) { MessageBox.Show("请先执行一次语法分析。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        using var dlg = new SaveFileDialog
        {
            Title = "导出语法分析结果",
            Filter = "文本文件 (*.txt)|*.txt",
            FileName = "语法分析结果.txt"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            Ui.WriteText(dlg.FileName, BuildExportText());
            _status.Text = "结果已导出到：" + dlg.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportToOutputFolder()
    {
        if (_grammar is null) { MessageBox.Show("请先执行一次语法分析。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        try
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "output");
            string file = Path.Combine(dir, "语法分析结果.txt");
            Ui.WriteText(file, BuildExportText());

            Ui.WriteText(Path.Combine(dir, "LL1_分析表.txt"), string.Join(Environment.NewLine, _llTable.ToTextLines()));
            Ui.WriteText(Path.Combine(dir, "LL1_分析过程表.txt"), string.Join(Environment.NewLine, _llProcess.ToTextLines()));
            Ui.WriteText(Path.Combine(dir, "LL1_分析结果.txt"), _llResult.Text);
            Ui.WriteText(Path.Combine(dir, "LR0_分析表.txt"), string.Join(Environment.NewLine, _lr0Table.ToTextLines()));
            Ui.WriteText(Path.Combine(dir, "SLR1_分析表.txt"), string.Join(Environment.NewLine, _slrTable.ToTextLines()));
            Ui.WriteText(Path.Combine(dir, "LR_分析过程表.txt"), string.Join(Environment.NewLine, _lrProcess.ToTextLines()));
            Ui.WriteText(Path.Combine(dir, "LR_分析结果.txt"), _lrResult.Text);
            Ui.WriteText(Path.Combine(dir, "LR0_项目集规范族.txt"), string.Join(Environment.NewLine, _lrStates.ToTextLines()));

            // 状态转换图（与界面上看到的一致）
            var size = new Size(1100, 800);
            using (var bmp = new Bitmap(size.Width, size.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    _lrGraph.RenderTo(g, size);
                }
                bmp.Save(Path.Combine(dir, "LR0_项目集规范族状态转换图.png"),
                    System.Drawing.Imaging.ImageFormat.Png);
            }

            _status.Text = "结果已写入：" + dir;
            MessageBox.Show("全部结果已写入程序目录下的 output 文件夹：\r\n" + dir,
                "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ------------------------------ 帮助 ------------------------------

    private static void ShowGrammarHelp()
    {
        MessageBox.Show(
            "文法书写说明（程序自动推导四元组 G = (Vn, Vt, P, S)）：\r\n\r\n" +
            "1) 每行写一条产生式，箭头可用 → 或 ->，例如：\r\n" +
            "      E→E+T|T\r\n" +
            "      T→T*F|F\r\n" +
            "      F→(E)|i\r\n\r\n" +
            "2) 同一左部用 | 分隔多个候选式。\r\n\r\n" +
            "3) 只出现在右部、从未做过左部的符号一律按终结符处理，\r\n" +
            "   所以终结符不需要声明（如上面的 i、+、*、(、)）。\r\n\r\n" +
            "4) 空串可写作 ε 或 @，例如 E′→+TE′|ε。\r\n\r\n" +
            "5) 非终结符建议用单个大写字母或带撇号的符号（E、E′、T、F…）。\r\n" +
            "   若要使用 if、then 这样的多字符终结符，请在该产生式的符号之间加空格，\r\n" +
            "   例如：S→if E then S else S。\r\n\r\n" +
            "6) 开始符默认为第一条产生式的左部。\r\n\r\n" +
            "7) 以 // 开头的内容视为注释，可忽略。\r\n\r\n" +
            "8) 程序不需要手写任何分析表：\r\n" +
            "   LL(1) 会自动消除左递归、提取左公因子，再由 SELECT 集生成分析表；\r\n" +
            "   LR 会自动求 LR(0) 项目集规范族，生成 LR(0) 与 SLR(1) 两张分析表。",
            "文法书写说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "ParseLab 语法分析实验系统\r\n《编译原理》实验二\r\n\r\n" +
            "已实现内容：\r\n" +
            "  基础实验：对给定文法手工/自动构造 LL(1) 分析表，按\r\n" +
            "     “步骤|分析栈|当前输入a|剩余输入串|所用产生式”输出分析过程表，\r\n" +
            "     并给出分析结果；同时输出文法四元组 (Vn, Vt, P, S)。\r\n" +
            "  探索实验（一）：任意文法 → 自动消除左递归、提取左公因子 →\r\n" +
            "     求 FIRST/FOLLOW/SELECT → 自动生成 LL(1) 分析表并完成分析。\r\n" +
            "  探索实验（二）：任意文法 → 自动求 LR(0) 项目集规范族 →\r\n" +
            "     自动生成 LR(0) 与 SLR(1) 两张分析表，自动检出并说明冲突，\r\n" +
            "     并可绘出项目集规范族的状态转换图。\r\n\r\n" +
            "运行环境：Windows 10/11 x64，单文件发布，无需安装任何运行环境。",
            "关于 ParseLab", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
