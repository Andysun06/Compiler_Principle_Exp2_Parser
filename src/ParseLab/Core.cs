using System.Text;

namespace ParseLab.Core;

/// <summary>文法中用到的特殊符号常量。</summary>
public static class Symbols
{
    public const string Epsilon = "ε";   // 空串
    public const string End = "#";       // 输入串结束符（也用作分析表的行/列标记）
    public const string Arrow = "→";
}

// =====================================================================
//                          产生式与文法（四元组）
// =====================================================================

/// <summary>一条产生式 A → X1X2…Xn（右部为空表示 A → ε）。</summary>
public sealed class Production
{
    public int Index { get; set; }
    public string Left { get; set; } = "";
    public List<string> Right { get; set; } = new();

    public bool IsEpsilon => Right.Count == 0;
    public string RightText => IsEpsilon ? Symbols.Epsilon : string.Join("", Right);
    public string Text => $"{Left}{Symbols.Arrow}{RightText}";

    public Production Clone() => new()
    {
        Index = Index,
        Left = Left,
        Right = new List<string>(Right)
    };
}

/// <summary>
/// 文法 G = (Vn, Vt, P, S)，即“四元组”。
/// 输入时按行书写产生式（如 E→E+T|T），程序自动推导出 Vn、Vt、P、S。
/// </summary>
public sealed class Grammar
{
    public List<string> NonTerminals { get; set; } = new();
    public List<string> Terminals { get; set; } = new();
    public List<Production> Productions { get; set; } = new();
    public string Start { get; set; } = "";

    /// <summary>用于显示的四元组描述。</summary>
    public string Quadruple()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Vn = {{ {string.Join(", ", NonTerminals)} }}");
        sb.AppendLine($"Vt = {{ {string.Join(", ", Terminals)} }}");
        sb.AppendLine($"S  = {Start}");
        sb.Append("P  = { ");
        sb.Append(string.Join(" ； ", Productions.Select(p => p.Text)));
        sb.Append(" }");
        return sb.ToString();
    }

    public IEnumerable<Production> ProductionsOf(string nt)
        => Productions.Where(p => p.Left == nt);

    public bool IsNonTerminal(string s) => NonTerminals.Contains(s);

    public Grammar Clone() => new()
    {
        NonTerminals = new List<string>(NonTerminals),
        Terminals = new List<string>(Terminals),
        Productions = Productions.Select(p => p.Clone()).ToList(),
        Start = Start
    };

    public void Renumber()
    {
        for (int i = 0; i < Productions.Count; i++) Productions[i].Index = i;
    }

    /// <summary>把产生式按非终结符在 NonTerminals 中的顺序整理，便于阅读（稳定排序）。</summary>
    public void SortProductions()
    {
        Productions = Productions
            .Select((p, i) => (p, i))
            .OrderBy(x => NonTerminals.IndexOf(x.p.Left) is var k && k >= 0 ? k : int.MaxValue)
            .ThenBy(x => x.i)
            .Select(x => x.p)
            .ToList();
        Renumber();
    }

    /// <summary>
    /// 终结符排序：标识符一类的字母/数字符号排最前，其余按运算符、括号、分隔符的常规顺序排列，
    /// 使分析表的列次序与教材（i、+、*、(、)、#）一致。
    /// </summary>
    private const string PunctOrder = "+-*/%^<>=!&|.,;:()[]{}";

    public void SortTerminals()
    {
        int PunctIndex(string t)
        {
            if (t.Length != 1) return 100;
            int k = PunctOrder.IndexOf(t[0]);
            return k >= 0 ? k : 90;
        }

        Terminals = Terminals
            .OrderBy(t => t.Length > 0 && char.IsLetterOrDigit(t[0]) ? 0 : 1)
            .ThenBy(PunctIndex)
            .ThenBy(t => t, StringComparer.Ordinal)
            .ToList();
    }

    // ------------------------------------------------------------------
    //                            解析
    // ------------------------------------------------------------------

    /// <summary>从文本解析文法。每行一条产生式，右部可用 | 分隔多个候选式。</summary>
    public static Grammar Parse(string text, out List<string> errors)
    {
        errors = new List<string>();
        var g = new Grammar();

        var raw = new List<(string Left, List<string> Alts, bool Spaced)>();
        foreach (string rawLine in (text ?? "").Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine;
            int cm = line.IndexOf("//", StringComparison.Ordinal);
            if (cm >= 0) line = line[..cm];
            line = line.Trim();
            if (line.Length == 0) continue;

            int arrow = line.IndexOf("->", StringComparison.Ordinal);
            int arrowLen = 2;
            if (arrow < 0) { arrow = line.IndexOf(Symbols.Arrow, StringComparison.Ordinal); arrowLen = 1; }
            if (arrow <= 0)
            {
                errors.Add($"无法识别的产生式（缺少 → 或 ->）：{line}");
                continue;
            }

            string lhs = line[..arrow].Trim();
            string rhsAll = line[(arrow + arrowLen)..];
            if (lhs.Length == 0)
            {
                errors.Add($"产生式左部为空：{line}");
                continue;
            }
            // 只要该行的右部出现过空白，就整行按“空白分隔”切分符号。
            // 这样 E→E+T|T 这类紧凑写法仍按单字符切分，
            // 而 factor→( expr )|id|num 中的 id、num 能整体识别为一个终结符。
            bool spaced = rhsAll.Any(char.IsWhiteSpace);
            raw.Add((lhs, rhsAll.Split('|').Select(s => s.Trim()).ToList(), spaced));
        }

        // 第一遍：收集非终结符（所有产生式的左部）
        foreach (var (lhs, _, _) in raw)
            if (!g.NonTerminals.Contains(lhs)) g.NonTerminals.Add(lhs);

        if (g.NonTerminals.Count == 0)
        {
            errors.Add("文法为空：请至少输入一条形如 E→E+T|T 的产生式。");
            return g;
        }

        // 第二遍：切分右部
        foreach (var (lhs, alts, spaced) in raw)
            foreach (string alt in alts)
                g.Productions.Add(new Production { Left = lhs, Right = Tokenize(alt, g.NonTerminals, spaced) });

        g.Start = g.NonTerminals[0];

        // 终结符 = 出现在右部、且不是非终结符的符号
        foreach (var p in g.Productions)
            foreach (string s in p.Right)
                if (!g.NonTerminals.Contains(s) && !g.Terminals.Contains(s)) g.Terminals.Add(s);

        g.Renumber();
        g.SortTerminals();
        foreach (string s in g.Terminals)
            if (s.Length == 1 && char.IsUpper(s[0]) && s != Symbols.End)
                errors.Add($"提示：符号 {s} 只出现在产生式右部、从未作为左部出现，" +
                           $"程序把它当作终结符处理了。如果它应该是非终结符，请补上它的产生式。");

        return g;
    }

    /// <summary>
    /// 把一条候选式切成符号序列。
    /// forceSpaced 为 true（或该串自身含空白）时按空白切分，便于书写 if、id、num 这类多字符符号；
    /// 否则按“优先匹配已声明的非终结符、否则单字符”切分，并吸收紧跟的撇号（如 E'）。
    /// 输入串的切分也用本方法（此时沿用“是否含空白”的判断）。
    /// </summary>
    public static List<string> Tokenize(string rhs, List<string> nonTerminals)
        => Tokenize(rhs, nonTerminals, false);

    public static List<string> Tokenize(string rhs, List<string> nonTerminals, bool forceSpaced)
    {
        var tokens = new List<string>();
        string s = (rhs ?? "").Trim();
        if (s.Length == 0) return tokens;

        if (forceSpaced || s.Any(char.IsWhiteSpace))
        {
            foreach (string t in s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                if (!IsEpsilonToken(t)) tokens.Add(t);
            return tokens;
        }

        int i = 0;
        while (i < s.Length)
        {
            string? matched = null;
            foreach (string nt in nonTerminals)
                if (nt.Length > 0 && i + nt.Length <= s.Length &&
                    string.CompareOrdinal(s, i, nt, 0, nt.Length) == 0 &&
                    (matched is null || nt.Length > matched.Length))
                    matched = nt;

            string sym;
            if (matched is not null) { sym = matched; i += matched.Length; }
            else { sym = s[i].ToString(); i++; }

            while (i < s.Length && s[i] == '\'') { sym += "'"; i++; }   // 吸收撇号

            if (!IsEpsilonToken(sym)) tokens.Add(sym);
        }
        return tokens;
    }

    private static bool IsEpsilonToken(string t)
        => t is "ε" or "epsilon" or "EPSILON" or "Epsilon" or "@" or "空" or "空串";
}

// =====================================================================
//                文法变换：消除左递归 + 提取左公因子
// =====================================================================

/// <summary>
/// LL(1) 分析要求文法无左递归、无回溯。本类把任意文法改造成 LL(1) 友好的形式，
/// 并记录每一步变换过程，便于在报告中说明。
/// </summary>
public static class GrammarTransform
{
    /// <summary>消除左递归、提取左公因子，返回新文法与变换说明。</summary>
    public static (Grammar Grammar, List<string> Steps) ToLl1Friendly(Grammar input)
    {
        var g = input.Clone();
        var steps = new List<string>();
        var parentOf = new Dictionary<string, string>();     // 新非终结符 → 它是由谁引入的
        var originalOrder = new List<string>(g.NonTerminals);

        EliminateLeftRecursion(g, steps, parentOf);
        LeftFactor(g, steps, parentOf);

        // 重排非终结符顺序，让 E' 紧跟在 E 之后，读起来与教材一致
        var ordered = new List<string>();
        foreach (string nt in originalOrder)
        {
            ordered.Add(nt);
            ordered.AddRange(g.NonTerminals.Where(x => parentOf.TryGetValue(x, out var p) && p == nt));
        }
        foreach (string nt in g.NonTerminals)
            if (!ordered.Contains(nt)) ordered.Add(nt);
        g.NonTerminals = ordered;

        // 终结符重新推导
        g.Terminals = new List<string>();
        foreach (var p in g.Productions)
            foreach (string s in p.Right)
                if (!g.NonTerminals.Contains(s) && !g.Terminals.Contains(s)) g.Terminals.Add(s);

        g.SortProductions();
        g.SortTerminals();
        return (g, steps);
    }

    /// <summary>生成一个不与现有符号重名的新非终结符，如 E → E′ → E″。</summary>
    private static string MakeFresh(Grammar g, string baseName)
    {
        string name = baseName + "′";
        while (g.NonTerminals.Contains(name)) name += "′";
        return name;
    }

    /// <summary>
    /// 消除左递归（含间接左递归）。
    /// 做法：把非终结符编号 A1…An，对每个 Ai 先用 A1…Ai-1 的候选式代入，
    /// 再消除 Ai 自身的直接左递归（引入 Ai′ 把递归改写成右递归）。
    /// </summary>
    private static void EliminateLeftRecursion(Grammar g, List<string> steps, Dictionary<string, string> parentOf)
    {
        var order = new List<string>(g.NonTerminals);

        for (int i = 0; i < order.Count; i++)
        {
            string ai = order[i];

            // ① 用前面的 Aj 替换形如 Ai → Aj γ 的候选式
            for (int j = 0; j < i; j++)
            {
                string aj = order[j];
                foreach (var p in g.ProductionsOf(ai).Where(p => p.Right.Count > 0 && p.Right[0] == aj).ToList())
                {
                    var gamma = p.Right.Skip(1).ToList();
                    g.Productions.Remove(p);
                    foreach (var q in g.ProductionsOf(aj).ToList())
                    {
                        var nr = new List<string>(q.Right);
                        nr.AddRange(gamma);
                        g.Productions.Add(new Production { Left = ai, Right = nr });
                    }
                    steps.Add($"{ai} → {aj}… 属于间接左递归：用 {aj} 的候选式代入 {ai}");
                }
            }

            // ② 消除 Ai 的直接左递归
            var all = g.ProductionsOf(ai).ToList();
            var direct = all.Where(p => p.Right.Count > 0 && p.Right[0] == ai).ToList();
            if (direct.Count == 0) continue;

            var beta = all.Where(p => !(p.Right.Count > 0 && p.Right[0] == ai)).ToList();
            string newNt = MakeFresh(g, ai);
            g.NonTerminals.Add(newNt);
            parentOf[newNt] = ai;

            foreach (var p in all) g.Productions.Remove(p);

            if (beta.Count == 0)
                g.Productions.Add(new Production { Left = ai, Right = new List<string> { newNt } });
            else
                foreach (var b in beta)
                {
                    var nr = new List<string>(b.Right) { newNt };
                    g.Productions.Add(new Production { Left = ai, Right = nr });
                }

            foreach (var d in direct)
            {
                var nr = new List<string>(d.Right.Skip(1)) { newNt };
                g.Productions.Add(new Production { Left = newNt, Right = nr });
            }
            g.Productions.Add(new Production { Left = newNt, Right = new List<string>() });   // ε

            steps.Add($"{ai} 存在直接左递归：引入 {newNt}，改写为 {ai}→…{newNt}、{newNt}→…{newNt} | ε");
        }
    }

    /// <summary>提取左公因子：把 A → αβ1 | αβ2 改写成 A → αA′、A′ → β1 | β2。</summary>
    private static void LeftFactor(Grammar g, List<string> steps, Dictionary<string, string> parentOf)
    {
        int guard = 0;
        while (guard++ < 100)
        {
            bool changed = false;

            foreach (string a in g.NonTerminals.ToList())
            {
                var prods = g.ProductionsOf(a).Where(p => p.Right.Count > 0).ToList();
                var groups = prods.GroupBy(p => p.Right[0]).Where(x => x.Count() > 1).ToList();

                foreach (var grp in groups)
                {
                    var list = grp.ToList();
                    int minLen = list.Min(p => p.Right.Count);
                    int common = 0;
                    while (common < minLen)
                    {
                        string s = list[0].Right[common];
                        if (list.All(p => p.Right[common] == s)) common++;
                        else break;
                    }
                    if (common == 0) continue;

                    var prefix = list[0].Right.Take(common).ToList();
                    string newNt = MakeFresh(g, a);
                    g.NonTerminals.Add(newNt);
                    parentOf[newNt] = a;

                    foreach (var p in list) g.Productions.Remove(p);

                    var head = new List<string>(prefix) { newNt };
                    g.Productions.Add(new Production { Left = a, Right = head });
                    foreach (var p in list)
                        g.Productions.Add(new Production { Left = newNt, Right = p.Right.Skip(common).ToList() });

                    steps.Add($"{a} 的候选式有公共前缀 {string.Join("", prefix)}：提取为 {a}→{string.Join("", prefix)}{newNt}");
                    changed = true;
                    break;
                }
                if (changed) break;
            }

            if (!changed) break;
        }
    }
}

// =====================================================================
//                        FIRST / FOLLOW / SELECT 集
// =====================================================================

/// <summary>文法的 FIRST、FOLLOW、SELECT 集。</summary>
public sealed class GrammarSets
{
    public Grammar Grammar { get; }
    public Dictionary<string, SortedSet<string>> First { get; } = new();
    public Dictionary<string, SortedSet<string>> Follow { get; } = new();
    public Dictionary<int, SortedSet<string>> Select { get; } = new();

    public GrammarSets(Grammar g)
    {
        Grammar = g;
        ComputeFirst();
        ComputeFollow();
        ComputeSelect();
    }

    /// <summary>符号串的 FIRST 集（含 ε 的情形也一并返回）。</summary>
    public SortedSet<string> FirstOf(IEnumerable<string> symbols)
    {
        var result = new SortedSet<string>();
        foreach (string s in symbols)
        {
            if (Grammar.Terminals.Contains(s)) { result.Add(s); return result; }
            if (!First.TryGetValue(s, out var f)) { result.Add(s); return result; }

            foreach (string x in f) if (x != Symbols.Epsilon) result.Add(x);
            if (!f.Contains(Symbols.Epsilon)) return result;      // 该符号不能推出 ε，后续不再看
        }
        result.Add(Symbols.Epsilon);                              // 所有符号都可空
        return result;
    }

    private void ComputeFirst()
    {
        foreach (string t in Grammar.Terminals) First[t] = new SortedSet<string> { t };
        foreach (string nt in Grammar.NonTerminals) First[nt] = new SortedSet<string>();

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var p in Grammar.Productions)
            {
                var f = FirstOf(p.Right);
                foreach (string x in f)
                    if (First[p.Left].Add(x)) changed = true;
            }
        }
    }

    private void ComputeFollow()
    {
        foreach (string nt in Grammar.NonTerminals) Follow[nt] = new SortedSet<string>();
        if (Grammar.NonTerminals.Contains(Grammar.Start)) Follow[Grammar.Start].Add(Symbols.End);

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var p in Grammar.Productions)
                for (int i = 0; i < p.Right.Count; i++)
                {
                    string b = p.Right[i];
                    if (!Grammar.NonTerminals.Contains(b)) continue;

                    var beta = FirstOf(p.Right.Skip(i + 1));
                    foreach (string x in beta)
                        if (x != Symbols.Epsilon && Follow[b].Add(x)) changed = true;

                    if (beta.Contains(Symbols.Epsilon))
                        foreach (string x in Follow[p.Left].ToList())     // 先快照，避免边遍历边修改
                            if (Follow[b].Add(x)) changed = true;
                }
        }
    }

    private void ComputeSelect()
    {
        foreach (var p in Grammar.Productions)
        {
            var first = FirstOf(p.Right);
            var sel = new SortedSet<string>(first.Where(x => x != Symbols.Epsilon));
            if (first.Contains(Symbols.Epsilon))
                foreach (string x in Follow[p.Left]) sel.Add(x);
            Select[p.Index] = sel;
        }
    }
}

// =====================================================================
//                            分析表 / 分析过程
// =====================================================================

/// <summary>任意二维分析表（行=状态或非终结符，列=终结符/非终结符），空串表示该格为空。</summary>
public sealed class ParseTable
{
    public string Title { get; set; } = "";
    public List<string> Columns { get; } = new();
    public List<string> Rows { get; } = new();
    public List<string[]> Cells { get; } = new();
}

/// <summary>LL(1) 分析过程中的一行。</summary>
public sealed class AnalysisStep
{
    public int Index { get; set; }
    public string Stack { get; set; } = "";
    public string CurrentInput { get; set; } = "";
    public string RemainingInput { get; set; } = "";
    public string Production { get; set; } = "";
}

/// <summary>LL(1) 分析的完整结果。</summary>
public sealed class Ll1Result
{
    public Grammar Original { get; set; } = new();
    public Grammar Transformed { get; set; } = new();
    public List<string> TransformSteps { get; } = new();
    public GrammarSets Sets { get; set; } = null!;
    public ParseTable Table { get; set; } = new();

    /// <summary>分析表单元格 (非终结符, 终结符) → 产生式文本，供分析过程查表使用。</summary>
    public Dictionary<(string, string), string> TableDict { get; set; } = new();

    public List<string> Conflicts { get; } = new();
    public List<AnalysisStep> Process { get; } = new();
    public bool Accepted { get; set; }
    public string Result { get; set; } = "";
    public string Input { get; set; } = "";

    public bool IsLl1 => Conflicts.Count == 0;
}

/// <summary>LL(1) 语法分析器：自动生成分析表，并模拟进栈/出栈完成分析过程。</summary>
public static class Ll1Analyzer
{
    public static Ll1Result Build(Grammar input, string inputString)
    {
        var result = new Ll1Result { Original = input, Input = inputString };

        (var transformed, var steps) = GrammarTransform.ToLl1Friendly(input);
        result.Transformed = transformed;
        result.TransformSteps.AddRange(steps);

        result.Sets = new GrammarSets(transformed);
        BuildTable(result);
        Analyze(result);
        return result;
    }

    /// <summary>依据 SELECT 集填表：SELECT(A→α) 中的每个终结符 a，都在 M[A, a] 填入该产生式。</summary>
    private static void BuildTable(Ll1Result r)
    {
        var g = r.Transformed;
        var terminals = g.Terminals.Concat(new[] { Symbols.End }).Distinct().ToList();

        var table = new ParseTable { Title = "LL(1) 语法分析表" };
        table.Columns.AddRange(terminals);
        foreach (string nt in g.NonTerminals) table.Rows.Add(nt);

        // 单元格内容：产生式文本；同时用字典保存产生式对象，便于分析时使用
        var cells = new Dictionary<(string, string), string>();
        foreach (string nt in g.NonTerminals)
        {
            var row = new string[terminals.Count];
            foreach (var p in g.ProductionsOf(nt))
                foreach (string a in r.Sets.Select[p.Index])
                {
                    int col = terminals.IndexOf(a);
                    if (col < 0) continue;
                    var key = (nt, a);
                    if (cells.TryGetValue(key, out string? old))
                    {
                        if (old != p.Text)
                            r.Conflicts.Add($"M[{nt}, {a}] 冲突：“{old}” 与 “{p.Text}” 都要求填入，该文法是 LL(1) 文法吗？");
                    }
                    else
                    {
                        cells[key] = p.Text;
                        row[col] = p.Text;
                    }
                }
            table.Cells.Add(row);
        }

        r.Table = table;
        r.TableDict = cells;
    }

    /// <summary>模拟分析过程：自左向右扫描输入串，依据栈顶符号与当前输入字符查表。</summary>
    private static void Analyze(Ll1Result r)
    {
        var g = r.Transformed;
        var tokens = Grammar.Tokenize(r.Input, g.Terminals);
        if (tokens.Count == 0)
        {
            r.Result = "输入串为空。";
            return;
        }
        tokens.Add(Symbols.End);

        var stack = new List<string> { Symbols.End, g.Start };   // 下标 0 为栈底
        int pos = 0;
        int step = 0;

        while (true)
        {
            if (++step > 2000)
            {
                r.Result = "分析步数过多，已中止（可能文法存在环）。";
                break;
            }

            string top = stack[^1];
            string cur = tokens[pos];

            var s = new AnalysisStep
            {
                Index = step,
                Stack = string.Join("", stack),
                CurrentInput = cur,
                RemainingInput = string.Join("", tokens.Skip(pos))
            };

            // 1) 栈顶与当前输入都是结束符 → 分析成功
            if (top == Symbols.End && cur == Symbols.End)
            {
                s.Production = "分析成功";
                r.Process.Add(s);
                r.Accepted = true;
                r.Result = $"分析成功：输入串 {r.Input} 是文法 {g.Start} 的句子。";
                return;
            }

            // 2) 栈顶是终结符 → 与当前输入匹配则出栈
            if (g.Terminals.Contains(top) || top == Symbols.End)
            {
                if (top == cur)
                {
                    stack.RemoveAt(stack.Count - 1);
                    pos++;
                    s.Production = "";
                    r.Process.Add(s);
                    continue;
                }
                s.Production = "出错";
                r.Process.Add(s);
                r.Accepted = false;
                r.Result = $"分析失败：栈顶终结符 {top} 与当前输入 {cur} 不匹配（第 {step} 步）。";
                return;
            }

            // 3) 栈顶是非终结符 → 查表，用产生式右部替换
            if (r.TableDict.TryGetValue((top, cur), out string? prodText))
            {
                var prod = g.Productions.First(p => p.Left == top && p.Text == prodText);
                stack.RemoveAt(stack.Count - 1);
                for (int i = prod.Right.Count - 1; i >= 0; i--) stack.Add(prod.Right[i]);  // 逆序压栈
                s.Production = prod.Text;
                r.Process.Add(s);
                continue;
            }

            s.Production = "出错";
            r.Process.Add(s);
            r.Accepted = false;
            r.Result = $"分析失败：M[{top}, {cur}] 为空，无法继续推导（第 {step} 步）。";
            return;
        }
    }
}

// =====================================================================
//                     LR(0) / SLR(1) 语法分析
// =====================================================================

/// <summary>LR 项目 [A → α·β]，用“产生式编号 + 圆点位置”唯一确定。</summary>
public sealed class LrItem : IEquatable<LrItem>, IComparable<LrItem>
{
    public int Production { get; }
    public int Dot { get; }
    public LrItem(int production, int dot) { Production = production; Dot = dot; }

    public bool Equals(LrItem? other) => other is not null && Production == other.Production && Dot == other.Dot;
    public override bool Equals(object? obj) => Equals(obj as LrItem);
    public override int GetHashCode() => Production * 1000 + Dot;
    public int CompareTo(LrItem? other) => other is null ? 1 : (Production, Dot).CompareTo((other.Production, other.Dot));
}

/// <summary>LR 项目集规范族中的一个状态（一个项目集）。</summary>
public sealed class LrState
{
    public int Id { get; set; }
    public List<LrItem> Items { get; set; } = new();
    public string Key { get; set; } = "";

    /// <summary>把一个项目渲染成 A→α·β 的形式。</summary>
    public static string RenderItem(Grammar g, LrItem it)
    {
        var p = g.Productions[it.Production];
        var rhs = new List<string>(p.Right);
        rhs.Insert(Math.Clamp(it.Dot, 0, rhs.Count), "·");
        return $"{p.Left}{Symbols.Arrow}{string.Join("", rhs)}";
    }

    public string Render(Grammar g) => string.Join("  ", Items.Select(it => RenderItem(g, it)));
}

/// <summary>LR 分析过程中的一行。</summary>
public sealed class LrStep
{
    public int Index { get; set; }
    public string StateStack { get; set; } = "";
    public string SymbolStack { get; set; } = "";
    public string CurrentInput { get; set; } = "";
    public string RemainingInput { get; set; } = "";
    public string Action { get; set; } = "";
}

/// <summary>LR(0) / SLR(1) 分析的完整结果。</summary>
public sealed class LrResult
{
    public Grammar Input { get; set; } = new();
    public Grammar Augmented { get; set; } = new();
    public string AugmentedStart { get; set; } = "";
    public List<LrState> States { get; } = new();
    public GrammarSets Sets { get; set; } = null!;

    public Dictionary<(int, string), string> Lr0Action { get; } = new();
    public Dictionary<(int, string), string> SlrAction { get; } = new();
    public Dictionary<(int, string), int> Goto { get; } = new();

    /// <summary>项目集之间的全部转移：GOTO(状态, 符号) = 后继状态（与是否冲突无关）。</summary>
    public Dictionary<(int, string), int> Transitions { get; } = new();

    public ParseTable Lr0Table { get; set; } = new();
    public ParseTable SlrTable { get; set; } = new();
    public List<string> Lr0Conflicts { get; } = new();
    public List<string> SlrConflicts { get; } = new();

    public List<LrStep> Process { get; } = new();
    public bool Accepted { get; set; }
    public string Result { get; set; } = "";
    public string InputString { get; set; } = "";
    public bool UseSlr { get; set; }

    public ParseTable TableFor(bool slr) => slr ? SlrTable : Lr0Table;
    public Dictionary<(int, string), string> ActionFor(bool slr) => slr ? SlrAction : Lr0Action;
    public List<string> ConflictsFor(bool slr) => slr ? SlrConflicts : Lr0Conflicts;
}

/// <summary>
/// LR(0) / SLR(1) 语法分析器。
/// 两张表共用同一套 LR(0) 项目集规范族：
///   · LR(0)  对每个完整项目在所有终结符上都填“归约”，容易产生冲突；
///   · SLR(1) 只用 FOLLOW(A) 中的终结符填归约，冲突通常可以消解。
/// </summary>
public static class LrAnalyzer
{
    public static LrResult Build(Grammar input)
    {
        var r = new LrResult { Input = input };

        // ① 拓广文法：加入 S′ → S
        var g = input.Clone();
        string newStart = "S′";
        while (g.NonTerminals.Contains(newStart)) newStart += "′";
        g.NonTerminals.Insert(0, newStart);
        g.Productions.Insert(0, new Production { Left = newStart, Right = new List<string> { g.Start } });
        g.Start = newStart;
        g.SortProductions();
        r.Augmented = g;
        r.AugmentedStart = newStart;

        // ② 求 FIRST / FOLLOW（SLR(1) 需要 FOLLOW）
        r.Sets = new GrammarSets(g);

        BuildCanonicalCollection(r);

        var allTerminals = g.Terminals.Concat(new[] { Symbols.End }).Distinct().ToList();
        BuildTables(r, allTerminals, slr: false);
        BuildTables(r, allTerminals, slr: true);
        return r;
    }

    // ------------------------------------------------------------------
    //              LR(0) 项目集规范族（closure / goto / BFS）
    // ------------------------------------------------------------------

    private static string KeyOf(IEnumerable<LrItem> items)
        => string.Join(",", items.OrderBy(i => i.Production).ThenBy(i => i.Dot).Select(i => $"{i.Production}.{i.Dot}"));

    /// <summary>CLOSURE(I)：对每个 A→α·Bβ，把 B 的所有产生式 B→·γ 加进来。</summary>
    private static List<LrItem> Closure(Grammar g, IEnumerable<LrItem> seed)
    {
        var set = new SortedSet<LrItem>(seed);
        var queue = new Queue<LrItem>(set);
        while (queue.Count > 0)
        {
            var it = queue.Dequeue();
            var p = g.Productions[it.Production];
            if (it.Dot >= p.Right.Count) continue;
            string b = p.Right[it.Dot];
            if (!g.NonTerminals.Contains(b)) continue;
            foreach (var q in g.ProductionsOf(b))
                if (set.Add(new LrItem(q.Index, 0))) queue.Enqueue(new LrItem(q.Index, 0));
        }
        return set.ToList();
    }

    /// <summary>GOTO(I, X)：把圆点越过 X 的项目求闭包。</summary>
    private static List<LrItem> Goto(Grammar g, List<LrItem> items, string x)
    {
        var moved = new List<LrItem>();
        foreach (var it in items)
        {
            var p = g.Productions[it.Production];
            if (it.Dot < p.Right.Count && p.Right[it.Dot] == x) moved.Add(new LrItem(it.Production, it.Dot + 1));
        }
        return moved.Count == 0 ? new List<LrItem>() : Closure(g, moved);
    }

    private static void BuildCanonicalCollection(LrResult r)
    {
        var g = r.Augmented;
        var index = new Dictionary<string, int>();
        var queue = new Queue<LrState>();

        LrState Add(IEnumerable<LrItem> items)
        {
            var list = items.ToList();
            string key = KeyOf(list);
            if (index.TryGetValue(key, out int exist)) return r.States[exist];

            var st = new LrState { Id = r.States.Count, Items = list, Key = key };
            r.States.Add(st);
            index[key] = st.Id;
            queue.Enqueue(st);
            return st;
        }

        Add(Closure(g, new[] { new LrItem(0, 0) }));      // 初态：CLOSURE({ S′→·S })

        var symbols = g.Terminals.Concat(g.NonTerminals).Distinct().ToList();
        while (queue.Count > 0)
        {
            var st = queue.Dequeue();
            foreach (string x in symbols)
            {
                var next = Goto(g, st.Items, x);
                if (next.Count == 0) continue;
                r.Transitions[(st.Id, x)] = Add(next).Id;
            }
        }
    }

    // ------------------------------------------------------------------
    //                   构造 ACTION / GOTO 分析表
    // ------------------------------------------------------------------

    private static void BuildTables(LrResult r, List<string> allTerminals, bool slr)
    {
        var g = r.Augmented;
        var action = slr ? r.SlrAction : r.Lr0Action;
        var conflicts = slr ? r.SlrConflicts : r.Lr0Conflicts;

        // 先算好所有非终结符的 GOTO（移进项目里圆点后是非终结符的那些）
        foreach (var st in r.States)
            foreach (var it in st.Items)
            {
                var p = g.Productions[it.Production];
                if (it.Dot >= p.Right.Count) continue;
                string x = p.Right[it.Dot];
                if (g.NonTerminals.Contains(x))
                {
                    var to = r.States.FirstOrDefault(s => s.Key == KeyOf(Goto(g, st.Items, x)));
                    if (to is not null) r.Goto[(st.Id, x)] = to.Id;
                }
            }

        void Put(int state, string symbol, string value)
        {
            var key = (state, symbol);
            if (action.TryGetValue(key, out string? old))
            {
                if (old != value) conflicts.Add(Describe(state, symbol, old, value));
                return;                                  // 冲突时保留先填的动作（移进优先）
            }
            action[key] = value;
        }

        foreach (var st in r.States)
            foreach (var it in st.Items)
            {
                var p = g.Productions[it.Production];

                // 圆点后面还有符号 → 移进 / 转移
                if (it.Dot < p.Right.Count)
                {
                    string x = p.Right[it.Dot];
                    if (g.Terminals.Contains(x))
                    {
                        var to = r.States.FirstOrDefault(s => s.Key == KeyOf(Goto(g, st.Items, x)));
                        if (to is not null) Put(st.Id, x, "s" + to.Id);
                    }
                    continue;
                }

                // 完整项目 [A → α·] → 归约（或接受）
                if (p.Left == r.AugmentedStart)
                {
                    Put(st.Id, Symbols.End, "acc");
                    continue;
                }

                if (slr)
                {
                    foreach (string a in r.Sets.Follow[p.Left]) Put(st.Id, a, "r" + it.Production);
                }
                else
                {
                    foreach (string a in allTerminals) Put(st.Id, a, "r" + it.Production);
                }
            }

        // 组织成可显示的二维表：列 = 终结符(含 #) + 非终结符（GOTO 部分）
        var nonTerminalCols = g.NonTerminals.Where(n => n != r.AugmentedStart).ToList();
        var table = new ParseTable
        {
            Title = slr ? "SLR(1) 语法分析表（ACTION + GOTO）" : "LR(0) 语法分析表（ACTION + GOTO）"
        };
        table.Columns.AddRange(allTerminals);
        table.Columns.AddRange(nonTerminalCols);

        foreach (var st in r.States)
        {
            table.Rows.Add(st.Id.ToString());
            var row = new string[table.Columns.Count];
            for (int c = 0; c < table.Columns.Count; c++)
            {
                string sym = table.Columns[c];
                if (action.TryGetValue((st.Id, sym), out string? v)) row[c] = v;
                else if (r.Goto.TryGetValue((st.Id, sym), out int gt)) row[c] = gt.ToString();
            }
            table.Cells.Add(row);
        }

        if (slr) r.SlrTable = table; else r.Lr0Table = table;
    }

    private static string Describe(int state, string symbol, string oldValue, string newValue)
    {
        bool IsR(string v) => v.StartsWith("r");
        bool IsS(string v) => v.StartsWith("s");

        string kind =
            (IsS(oldValue) && IsR(newValue)) || (IsR(oldValue) && IsS(newValue)) ? "移进-归约冲突"
            : IsR(oldValue) && IsR(newValue) ? "归约-归约冲突"
            : "冲突";

        return $"状态 {state}，输入 {symbol}：{kind}（{oldValue} 与 {newValue}）";
    }

    // ------------------------------------------------------------------
    //                            分析过程
    // ------------------------------------------------------------------

    /// <summary>用指定的分析表（LR(0) 或 SLR(1)）分析输入串。</summary>
    public static void Analyze(LrResult r, bool slr, string inputString)
    {
        r.UseSlr = slr;
        r.InputString = inputString;
        r.Process.Clear();
        r.Accepted = false;

        var g = r.Augmented;
        var action = r.ActionFor(slr);
        var tokens = Grammar.Tokenize(inputString, g.Terminals);
        if (tokens.Count == 0) { r.Result = "输入串为空。"; return; }
        tokens.Add(Symbols.End);

        var states = new List<int> { 0 };
        var symbols = new List<string> { Symbols.End };
        int pos = 0, step = 0;

        while (true)
        {
            if (++step > 5000) { r.Result = "分析步数过多，已中止。"; return; }

            int s = states[^1];
            string a = tokens[pos];

            var st = new LrStep
            {
                Index = step,
                StateStack = string.Join(" ", states),
                SymbolStack = string.Join("", symbols),
                CurrentInput = a,
                RemainingInput = string.Join("", tokens.Skip(pos))
            };

            if (!action.TryGetValue((s, a), out string? act))
            {
                st.Action = "出错";
                r.Process.Add(st);
                r.Result = $"分析失败：状态 {s} 遇到输入 {a} 时分析表为空（第 {step} 步）。";
                return;
            }

            if (act == "acc")
            {
                st.Action = "接受（acc）";
                r.Process.Add(st);
                r.Accepted = true;
                r.Result = $"分析成功：输入串 {inputString} 是文法的句子。";
                return;
            }

            if (act[0] == 's')
            {
                int to = int.Parse(act[1..]);
                st.Action = $"移进 {a}，转到状态 {to}";
                r.Process.Add(st);
                states.Add(to);
                symbols.Add(a);
                pos++;
                continue;
            }

            int pi = int.Parse(act[1..]);
            var p = g.Productions[pi];
            st.Action = $"归约 {p.Text}";
            r.Process.Add(st);

            if (states.Count - 1 < p.Right.Count)
            {
                r.Result = $"分析失败：归约 {p.Text} 时状态栈长度不足。";
                return;
            }
            for (int k = 0; k < p.Right.Count; k++)
            {
                states.RemoveAt(states.Count - 1);
                symbols.RemoveAt(symbols.Count - 1);
            }

            if (!r.Goto.TryGetValue((states[^1], p.Left), out int gt))
            {
                r.Result = $"分析失败：GOTO[{states[^1]}, {p.Left}] 为空。";
                return;
            }
            states.Add(gt);
            symbols.Add(p.Left);
        }
    }
}
