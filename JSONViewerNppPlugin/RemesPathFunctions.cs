/*
A library of built-in functions for the RemesPath query language.
*/
using System.Text;
using System.Text.RegularExpressions;

namespace JSON_Viewer.JSONViewerNppPlugin
{
    /// <summary>
    /// Binary operators, e.g., +, -, *, ==
    /// </summary>
    public class Binop
    {
        private Func<JNode, JNode, JNode> Function { get; }
        public float precedence;
        public string name;

        public Binop(Func<JNode, JNode, JNode> function, float precedence, string name)
        {
            Function = function;
            this.precedence = precedence;
            this.name = name;
        }

        public override string ToString() 
        { 
            return $"Binop(\"{this.name}\")";
        }

        public JNode Call(JNode left, JNode right)
        {
            if (left is CurJson && right is CurJson)
            {
                JNode ResolvedBinop(JNode json)
                {
                    return Function(((CurJson)left).function(json), ((CurJson)right).function(json));
                }
                return new CurJson(Dtype.UNKNOWN, ResolvedBinop);
            }
            if (right is CurJson)
            {
                JNode ResolvedBinop(JNode json)
                {
                    return Function(left, ((CurJson)right).function(json));
                }
                return new CurJson(Dtype.UNKNOWN, ResolvedBinop);
            }
            if (left is CurJson)
            {
                JNode ResolvedBinop(JNode json)
                {
                    return Function(((CurJson)left).function(json), right);
                }
                return new CurJson(Dtype.UNKNOWN, ResolvedBinop);
            }
            return Function(left, right);
        }

        public static JNode Add(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) + Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (atype == Dtype.FLOAT)
            {
                return new JNode(Convert.ToDouble(aval) + Convert.ToDouble(bval), Dtype.FLOAT, 0);
            }
            return new JNode(Convert.ToString(aval) + Convert.ToString(bval), Dtype.STR, 0);
        }

        public static JNode Sub(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) - Convert.ToInt64(bval), Dtype.INT, 0);
            }
            return new JNode(Convert.ToDouble(aval) - Convert.ToDouble(bval), Dtype.FLOAT, 0);
        }

        public static JNode Mul(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) * Convert.ToInt64(bval), Dtype.INT, 0);
            }
            return new JNode(Convert.ToDouble(aval) * Convert.ToDouble(bval), Dtype.FLOAT, 0);
        }

        public static JNode Divide(JNode a, JNode b)
        {
            return new JNode(Convert.ToDouble(a.value) / Convert.ToDouble(b.value), Dtype.FLOAT, 0);
        }

        public static JNode FloorDivide(JNode a, JNode b)
        {
            return new JNode(Math.Floor(Convert.ToDouble(a.value) / Convert.ToDouble(b.value)), Dtype.INT, 0);
        }

        public static JNode Pow(JNode a, JNode b)
        {
            return new JNode(Math.Pow(Convert.ToDouble(a.value), Convert.ToDouble(b.value)), Dtype.FLOAT, 0);
        }

        /// <summary>
        /// -a.value**b.value
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static JNode NegPow(JNode a, JNode b)
        {
            return new JNode(-Math.Pow(Convert.ToDouble(a.value), Convert.ToDouble(b.value)), Dtype.FLOAT, 0);
        }

        public static JNode Mod(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) % Convert.ToInt64(bval), Dtype.INT, 0);
            }
            return new JNode(Convert.ToDouble(aval) % Convert.ToDouble(bval), Dtype.FLOAT, 0);
        }

        public static JNode BitWiseOR(JNode a, JNode b)
        {
            if (a.type == Dtype.INT || b.type == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(a.value) | Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            return new JNode(Convert.ToBoolean(a.value) || Convert.ToBoolean(b.value), Dtype.BOOL, 0);
        }

        public static JNode BitWiseXOR(JNode a, JNode b)
        {
            if (a.type == Dtype.INT || b.type == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(a.value) ^ Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            return new JNode((bool)a.value ^ (bool)b.value, Dtype.BOOL, 0);
        }

        public static JNode BitWiseAND(JNode a, JNode b)
        {
            if (a.type == Dtype.INT || b.type == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(a.value) & Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            return new JNode((bool)a.value && (bool)b.value, Dtype.BOOL, 0);
        }

        /// <summary>
        /// Returns a boolean JNode with value true if node's string value contains the pattern sub.value.<br></br>
        /// E.g. HasPattern(JNode("abc"), JNode("ab+")) -> JNode(true)
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sub"></param>
        /// <returns></returns>
        public static JNode HasPattern(JNode node, JNode sub)
        {
            string s = (string)node.value;
            if (sub.type == Dtype.STR)
            {
                return new JNode(Regex.IsMatch(s, (string)sub.value), Dtype.BOOL, 0);
            }
            return new JNode((((JRegex)sub).regex).IsMatch(s), Dtype.BOOL, 0);
        }

        public static JNode IsIn(JNode a, JNode b)
        {
            if (b is JArray)
            {
                foreach (JNode node in ((JArray)b).children)
                {
                    if (node.value.CompareTo(a.value) == 0)
                    {
                        return new JNode(true, Dtype.BOOL, 0);
                    }
                }
                return new JNode(false, Dtype.BOOL, 0);
            }
            foreach (string key in ((JObject)b).children.Keys)
            {
                if (key == (string)a.value)
                {
                    return new JNode(true, Dtype.BOOL, 0);
                }
            }
            return new JNode(false, Dtype.BOOL, 0);
        }

        public static JNode LessThan(JNode a, JNode b)
        {
            return new JNode(a.LessThan(b), Dtype.BOOL, 0);
        }

        public static JNode GreaterThan(JNode a, JNode b)
        {
            return new JNode(a.GreaterThan(b), Dtype.BOOL, 0);
        }

        public static JNode GreaterThanOrEqual(JNode a, JNode b)
        {
            return new JNode(a.GreaterEquals(b), Dtype.BOOL, 0);
        }

        public static JNode LessThanOrEqual(JNode a, JNode b)
        {
            return new JNode(a.LessEquals(b), Dtype.BOOL, 0);
        }

        public static JNode IsEqual(JNode a, JNode b)
        {
            return new JNode(a.Equals(b), Dtype.BOOL, 0);
        }

        public static JNode IsNotEqual(JNode a, JNode b)
        {
            return new JNode(!a.Equals(b), Dtype.BOOL, 0);
        }

        /// <summary>
        /// Not strictly a binop, because it has no return value.<br></br>
        /// Implements the -= operator for two JNodes, both with type = Dtype.INT or Dtype.FLOAT,<br></br>
        /// So MinusEquals(JNode(3), JNode(0.5))<br></br>
        /// converts the first JNode's type from Dtype.INT to Dtype.FLOAT and sets its value to 2.5.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void MinusEquals(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (btype == Dtype.FLOAT && atype != Dtype.FLOAT)
            {
                a.value = Convert.ToDouble(aval) - Convert.ToDouble(bval);
                a.type = Dtype.FLOAT;
            }
            else if (atype == Dtype.FLOAT)
            {
                a.value = Convert.ToDouble(aval) - Convert.ToDouble(bval);
            }
            else
            {
                a.value = Convert.ToInt64(aval) - Convert.ToInt64(bval);
            }
        }

        /// <summary>
        /// See documentation for Binop.MinusEquals, except this implements +=
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void PlusEquals(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (btype == Dtype.FLOAT && atype != Dtype.FLOAT)
            {
                a.value = Convert.ToDouble(aval) + Convert.ToDouble(bval);
                a.type = Dtype.FLOAT;
            }
            else if (atype == Dtype.FLOAT)
            {
                a.value = Convert.ToDouble(aval) + Convert.ToDouble(bval);
            }
            else
            {
                a.value = Convert.ToInt64(aval) + Convert.ToInt64(bval);
            }
        }

        /// <summary>
        /// See documentation for MinusEquals, except this implements *=
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void TimesEquals(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (btype == Dtype.FLOAT && atype != Dtype.FLOAT)
            {
                a.value = Convert.ToDouble(aval) * Convert.ToDouble(bval);
                a.type = Dtype.FLOAT;
            }
            else if (atype == Dtype.FLOAT)
            {
                a.value = Convert.ToDouble(aval) * Convert.ToDouble(bval);
            }
            else
            {
                a.value = Convert.ToInt64(aval) * Convert.ToInt64(bval);
            }
        }

        /// <summary>
        /// Implements in-place exponentiation of a JNode by another JNode.<br></br>
        /// This always changes the type of JNode a to Dtype.FLOAT.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void PowEquals(JNode a, JNode b)
        {
            object? aval = a.value; object? bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            a.value = Math.Pow(Convert.ToDouble(aval), Convert.ToDouble(bval));
            a.type = Dtype.FLOAT;
        }

        public static Dictionary<string, Binop> BINOPS = new Dictionary<string, Binop>
        {
            ["&"] = new Binop(BitWiseAND, 0, "&"),
            ["|"] = new Binop(BitWiseOR, 0, "|"),
            ["^"] = new Binop(BitWiseXOR, 0, "^"),
            ["in"] = new Binop(IsIn, 1, "in"),
            ["=~"] = new Binop(HasPattern, 1, "=~"),
            ["=="] = new Binop(IsEqual, 1, "=="),
            ["!="] = new Binop(IsNotEqual, 1, "!="),
            ["<"] = new Binop(LessThan, 1, "<"),
            [">"] = new Binop(GreaterThan, 1, ">"),
            [">="] = new Binop(GreaterThanOrEqual, 1, ">="),
            ["<="] = new Binop(LessThanOrEqual, 1, "<="),
            ["+"] = new Binop(Add, 2, "+"),
            ["-"] = new Binop(Sub, 2, "-"),
            ["//"] = new Binop(FloorDivide, 3, "//"),
            ["%"] =  new Binop(Mod, 3, "%"),
            ["*"] = new Binop(Mul, 3, "*"),
            ["/"] = new Binop(Divide, 3, "/"),
            // precedence of unary minus (e.g., 2 * -5) is between division's precedence
            // exponentiation's precedence
            ["**"] = new Binop(Pow, 5, "**"),
        };

        public static HashSet<string> BOOLEAN_BINOPS = new HashSet<string> { "==", ">", "<", "=~", "!=", ">=", "<=", "in" };

        public static HashSet<string> BITWISE_BINOPS = new HashSet<string> { "^", "&", "|" };

        public static HashSet<string> FLOAT_RETURNING_BINOPS = new HashSet<string> { "/", "**" };

        public static HashSet<string> POLYMORPHIC_BINOPS = new HashSet<string> { "%", "+", "-", "*" };
    }


    public class BinopWithArgs
    {
        public Binop binop;
        public object left;
        public object right;

        public BinopWithArgs(Binop binop, object left, object right)
        {
            this.binop = binop;
            this.left = left;
            this.right = right;
        }

        public JNode Call()
        {
            if (left is BinopWithArgs)
            {
                left = ((BinopWithArgs)left).Call();
            }
            if (right is BinopWithArgs)
            {
                right = ((BinopWithArgs)right).Call();
            }
            return binop.Call((JNode)left, (JNode)right);
        }
    }


    /// <summary>
    /// functions with arguments in parens, e.g. mean(x), index(arr, elt), sort_by(arr, key, reverse)
    /// </summary>
    public class ArgFunction
    {
        private Func<JNode[], JNode> Function { get; }
        private Dtype[] Input_types;
        public string name;
        public Dtype type;
        public Byte max_args;
        public Byte min_args;
        public bool is_vectorized;

        public ArgFunction(Func<JNode[], JNode> function,
            string name,
            Dtype type,
            Byte min_args,
            Byte max_args,
            bool is_vectorized,
            Dtype[] input_types)
        {
            Function = function;
            this.name = name;
            this.type = type;
            this.max_args = max_args;
            this.min_args = min_args;
            this.is_vectorized = is_vectorized;
            this.Input_types = input_types;
        }

        public Dtype[] input_types()
        {
            var intypes = new Dtype[Input_types.Length];
            for (int i = 0; i < intypes.Length; i++)
            {
                intypes[i] = Input_types[i];
            }
            return intypes;
        }

        public JNode Call(JNode[] args)
        {
            return Function(args);
        }
        
        public override string ToString()
        {
            return $"ArgFunction({name}, {type})";
        }

        #region NON_VECTORIZED_ARG_FUNCTIONS

        /// <summary>
        /// Assuming first arg is a dictionary or list, return the number of elements it contains.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Len(JNode[] args)
        {
            var itbl = args[0];
            if (itbl is JArray)
            {
                return new JNode(Convert.ToInt64(((JArray)itbl).Length), Dtype.INT, 0);
            }
            return new JNode(Convert.ToInt64(((JObject)itbl).Length), Dtype.INT, 0);
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the sum, cast to a double.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Sum(JNode[] args)
        {
            var itbl = (JArray)args[0];
            double tot = 0;
            foreach (JNode child in itbl.children)
            {
                tot += Convert.ToDouble(child.value);
            }
            return new JNode(tot, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the arithmetic mean of that list.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Mean(JNode[] args)
        {
            var itbl = (JArray)args[0];
            double tot = 0;
            foreach (JNode child in itbl.children)
            {
                tot += Convert.ToDouble(child.value);
            }
            return new JNode(tot / itbl.Length, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// "Flattens" nested lists by adding all the elements of lists at depth 1 to a single
        /// list.<br></br>
        /// Example: Flatten({{1,2},3,{4,{5}}}) = {1,2,3,4,{5}}<br></br> 
        /// (except input is an array with one List<object?> and output is a List<object?>)<br></br>
        /// In the above example, everything at depth 0 is still at depth 0, and everything else
        /// has its depth reduced by 1.
        /// </summary>
        /// <param name="args">an array containng a single List<object?></param>
        /// <returns>List<object?> containing the flattened result</returns>
        public static JNode Flatten(JNode[] args)
        {
            var itbl = (JArray)args[0];
            var iterations = (long?)args[1].value;
            JArray flat;
            if (iterations is null || iterations == 1)
            {
                flat = new JArray(0, new List<JNode>());
                foreach (JNode child in itbl.children)
                {
                    if (child is JArray)
                    {
                        foreach (JNode grandchild in ((JArray)child).children)
                        {
                            flat.children.Add(grandchild);
                        }
                    }
                    else
                    {
                        flat.children.Add(child);
                    }
                }
                return flat;
            }
            flat = itbl;
            JNode jnull = new JNode(null, Dtype.NULL, 0);
            for (int ii = 0; ii < iterations; ii++)
            {
                flat = (JArray)Flatten(new JNode[] { flat, jnull });
            }
            return flat;
        }

        /// <summary>
        /// first arg should be List&lt;object?&gt;, second arg should be object?,
        /// optional third arg should be bool.<br></br>
        /// If second arg (elt) is in first arg (itbl), return the index in itbl where
        /// elt first occurs.<br></br>
        /// If a third arg (reverse) is true, then instead return the index
        /// of the final occurence of elt in itbl.<br></br>
        /// If elt does not occur in itbl, throw a KeyNotFoundException.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public static JNode Index(JNode[] args)
        {
            var itbl = (JArray)args[0];
            var elt = args[1];
            var reverse = args[2].value;
            
            if (reverse != null && (bool)reverse == true)
            {
                for (int ii = itbl.Length - 1; ii >= 0; ii--)
                {
                    if (itbl.children[ii].CompareTo(elt) == 0) { return new JNode(Convert.ToInt64(ii), Dtype.INT, 0); }
                }
                throw new KeyNotFoundException($"Element {elt} not found in the array {itbl}");
            }
            for (int ii = 0; ii < itbl.Length; ii++)
            {
                if (itbl.children[ii].CompareTo(elt) == 0) { return new JNode(Convert.ToInt64(ii), Dtype.INT, 0); }
            }
            throw new KeyNotFoundException($"Element {elt} not found in the array {itbl}");
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the largest number in the list,
        /// cast to a double.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Max(JNode[] args)
        {
            var itbl = (JArray)args[0];
            JNode biggest = new JNode(double.NegativeInfinity, Dtype.FLOAT, 0);
            foreach (JNode child in itbl.children)
            {
                if (Convert.ToDouble(child.value) > Convert.ToDouble(biggest.value)) { biggest = child; }
            }
            return biggest;
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the smallest number in the list,
        /// cast to a double.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Min(JNode[] args)
        {
            var itbl = (JArray)args[0];
            JNode smallest = new JNode(double.PositiveInfinity, Dtype.FLOAT, 0);
            foreach (JNode child in itbl.children)
            {
                if (Convert.ToDouble(child.value) < Convert.ToDouble(smallest.value)) { smallest = child; }
            }
            return smallest;
        }


        public static JNode Sorted(JNode[] args)
        {
            var sorted = new JArray(0, new List<JNode>());
            sorted.children.AddRange(((JArray)args[0]).children);
            var reverse = args[1].value;
            sorted.children.Sort();
            if (reverse != null && (bool)reverse)
            {
                sorted.children.Reverse();
            }
            return sorted;
        }

        public static JNode SortBy(JNode[] args)
        {
            var arr = (JArray)args[0];
            var sorted = new JArray(0, new List<JNode>());
            var key = args[1].value;
            var reverse = args[2].value;
            if (key is string)
            {
                string kstr = (string)key;
                foreach (JNode elt in arr.children.OrderBy(x => ((JObject)x).children[kstr]))
                {
                    sorted.children.Add(elt);
                }
            }
            else
            {
                int kint = Convert.ToInt32(key);
                foreach (JNode elt in arr.children.OrderBy(x => ((JArray)x).children[kint]))
                {
                    sorted.children.Add(elt);
                }
            }
            if (reverse != null && (bool)reverse)
            {
                sorted.children.Reverse();
            }
            return sorted;
        }

        public static JNode MaxBy(JNode[] args)
        {
            var itbl = (JArray)args[0];
            var key = args[1].value;
            if (key is string)
            {
                string keystr = (string)key;
                return itbl.children.MaxBy(x => ((JObject)x).children[keystr]);
            }
            int kint = Convert.ToInt32(key);
            return itbl.children.MaxBy(x => ((JArray)x).children[kint]);
        }

        public static JNode MinBy(JNode[] args)
        {
            var itbl = (JArray)args[0];
            var key = args[1].value;
            if (key is string)
            {
                string keystr = (string)key;
                return itbl.children.MinBy(x => ((JObject)x).children[keystr]);
            }
            int kint = Convert.ToInt32(key);
            return itbl.children.MinBy(x => ((JArray)x).children[kint]);
        }

        /// <summary>
        /// Three args, one required.<br></br>
        /// If only first arg provided: return all integers in [0, args[0])<br></br>
        /// If first and second arg provided: return all integers in [args[0], args[1])<br></br>
        /// If all three args provided: return all integers from args[0] to args[1],
        ///     incrementing by args[2] each time.<br></br>
        /// EXAMPLES:<br></br>
        /// IRange(3) -> List&lt;long&gt;({0, 1, 2})<br></br>
        /// IRange(3, 7) -> List&lt;long&gt;({3, 4, 5, 6})<br></br>
        /// IRange(10, 4, -2) -> List&lt;long&gt;({10, 8, 6})
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode IRange(JNode[] args)
        {
            var start = (long?)args[0].value; 
            var stop = (long?)args[1].value;
            var step = (long?)args[2].value;
            var nums = new JArray(0, new List<JNode>());
            if (stop == null)
            {
                for (long ii = 0; ii < start; ii++)
                {
                    nums.children.Add(new JNode(ii, Dtype.INT, 0));
                }
            }
            else if (step == null)
            {
                if (start < stop)
                {
                    for (long ii = start.Value; ii < stop; ii++)
                    {
                        nums.children.Add(new JNode(ii, Dtype.INT, 0));
                    }
                }
            }
            else
            {
                if (start > stop && step < 0)
                {
                    for (long ii = start.Value; ii > stop; ii -= step.Value)
                    {
                        nums.children.Add(new JNode(ii, Dtype.INT, 0));
                    }
                }
                else if (start < stop && step > 0)
                {
                    for (long ii = start.Value; ii < stop; ii += step.Value)
                    {
                        nums.children.Add(new JNode(ii, Dtype.INT, 0));
                    }
                }
            }
            return nums;
        }

        public static JNode Values(JNode[] args)
        {
            var vals = new JArray(0, new List<JNode>());
            vals.children.AddRange(((JObject)args[0]).children.Values);
            return vals;
        }

        public static JNode Keys(JNode[] args)
        {
            var ks = new JArray(0, new List<JNode>());
            foreach (string s in ((JObject)args[0]).children.Keys)
            {
                ks.children.Add(new JNode(s, Dtype.STR, 0));
            }
            return ks;
        }

        public static JNode Unique(JNode[] args)
        {
            var itbl = (JArray)args[0];
            var is_sorted = args[1].value;
            var uniq = new HashSet<object?>();
            foreach (JNode val in itbl.children)
            {
                uniq.Add(val.value);
            }
            var uniq_list = new List<JNode>();
            foreach (object? val in uniq)
            {
                uniq_list.Add(ObjectsToJNode(val));
            }
            if (is_sorted != null && (bool)is_sorted)
            {
                uniq_list.Sort();
            }
            return new JArray(0, uniq_list);
        }

        /// <summary>
        /// The first arg (itbl) must be a list containing only numbers.<br></br>
        /// The second arg (qtile) to be a double in [0,1].<br></br>
        /// Returns the qtile^th quantile of itbl, as a double.<br></br>
        /// So Quantile(x, 0.5) returns the median, Quantile(x, 0.75) returns the 75th percentile, and so on.<br></br>
        /// Uses linear interpolation if the index found is not an integer.<br></br>
        /// For example, suppose that the 60th percentile is at index 6.6, and elements 6 and 7 are 8 and 10.<br></br>
        /// Then the returned value is 0.6*10 + 0.4*8 = 9.2
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Quantile(JNode[] args)
        {
            var sorted = new List<double>();
            foreach (JNode node in ((JArray)args[0]).children)
            {
                sorted.Add(Convert.ToDouble(node.value));
            }
            double quantile = Convert.ToDouble(args[1].value);
            sorted.Sort();
            if (sorted.Count == 0)
            {
                throw new RemesPathException("Cannot find quantiles of an empty array");
            }
            if (sorted.Count == 1)
            {
                return new JNode(sorted[0], Dtype.FLOAT, 0);
            }
            double ind = quantile * (sorted.Count - 1);
            int lower_ind = Convert.ToInt32(Math.Floor(ind));
            double weighted_avg;
            double lower_val = sorted[lower_ind];
            if (ind != lower_ind)
            {
                
                double upper_val = sorted[lower_ind + 1];
                double frac_upper = ind - lower_ind;
                weighted_avg = upper_val * frac_upper + lower_val * (1 - frac_upper);
            }
            else
            {
                weighted_avg = lower_val;
            }
            return new JNode(weighted_avg, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// args[0] should be a list of objects.<br></br>
        /// Finds an array of sub-arrays, where each sub-array is an element-count pair, where the count is of that element in args[0].<br></br>
        /// EXAMPLES:
        /// ValueCounts(JArray({1, "a", 2, "a", 1})) ->
        /// JArray(JArray({"a", 2}), JArray({1, 2}), JArray({2, 1}))
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode ValueCounts(JNode[] args)
        {
            var itbl = (JArray)args[0];
            var uniqs = new Dictionary<object, long>();
            foreach (JNode elt in itbl.children)
            {
                object? val = elt.value;
                if (val == null)
                {
                    throw new RemesPathException("Can't count occurrences of objects with null values");
                }
                uniqs.TryAdd(val, 0);
                uniqs[val]++;
            }
            var uniq_arr = new JArray(0, new List<JNode>());
            foreach ((object elt, long ct) in uniqs)
            {
                JArray elt_ct = new JArray(0, new List<JNode>());
                elt_ct.children.Add(ObjectsToJNode(elt));
                elt_ct.children.Add(new JNode(ct, Dtype.INT, 0));
                uniq_arr.children.Add(elt_ct);
            }
            return uniq_arr;
        }

        public static JNode StringJoin(JNode[] args)
        {
            string sep = (string)args[0].value;
            var itbl = (JArray)args[1];
            var sb = new StringBuilder();
            sb.Append((string)itbl.children[0].value);
            for (int ii = 1; ii < itbl.Length; ii++)
            {
                sb.Append(sep);
                sb.Append((string)itbl.children[ii].value);
            }
            return new JNode(sb.ToString(), Dtype.STR, 0);
        }

        /// <summary>
        /// First arg (itbl) must be a JArray containing only other JArrays or only other JObjects.<br></br>
        /// Second arg (key) must be a JNode with a long value or a JNode with a string value.<br></br>
        /// Returns a new JObject where each entry in itbl is grouped into a separate JArray under the stringified form
        /// of the value associated with key/index key in itbl.<br></br>
        /// EXAMPLE<br></br>
        /// GroupBy([{"foo": 1, "bar": "a"}, {"foo": 2, "bar": "b"}, {"foo": 3, "bar": "a"}], "bar") returns:<br></br>
        /// {"a": [{"foo": 1, "bar": "a"}, {"foo": 3, "bar": "a"}], "b": [{"foo": 2, "bar": "b"}]}
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static JNode GroupBy(JNode[] args)
        {
            var itbl = (JArray)args[0];
            object? key = args[1].value;
            if (!(key is string || key is long))
            {
                throw new ArgumentException("The GroupBy function can only group by string keys or int indices");
            }
            var gb = new Dictionary<string, JNode>();
            string vstr;
            if (key is long)
            {
                int ikey = Convert.ToInt32(key);
                foreach (JNode node in itbl.children)
                {
                    JArray subobj = (JArray)node;
                    JNode val = subobj.children[ikey];
                    vstr = val.type == Dtype.STR ? (string)val.value : val.ToString();
                    if (!gb.TryAdd(vstr, new JArray(0, new List<JNode> { subobj })))
                    {
                        ((JArray)gb[vstr]).children.Add(subobj);
                    }
                }
            }
            else
            {
                string skey = (string)key;
                foreach (JNode node in itbl.children)
                {
                    JObject subobj = (JObject)node;
                    JNode val = subobj.children[skey];
                    vstr = val.type == Dtype.STR ? (string)val.value : val.ToString();
                    if (!gb.TryAdd(vstr, new JArray(0, new List<JNode> { subobj })))
                    {
                        ((JArray)gb[vstr]).children.Add(subobj);
                    }
                }
            }
            return new JObject(0, gb);
        }

        ///// <summary>
        ///// Like GroupBy, the first argument is a JArray containing only JObjects or only JArrays, and the second arg is a string or int.<br></br>
        ///// The third argument is a function of the current JSON, e.g., sum(@[:].foo).
        ///// Returns a JObject mapping each distinct stringified value at the key^th index/key of each subobject in the original iterable
        ///// to the specified aggregation function of all of those iterables.<br></br>
        ///// EXAMPLE<br></br>
        ///// AggBy([{"foo": 1, "bar": "a"}, {"foo": 2, "bar": "b"}, {"foo": 3, "bar": "a"}], "bar", sum(@[:].foo)) returns <br></br>
        ///// {"a": 4.0, "b": 2.0}
        ///// </summary>
        ///// <param name="args"></param>
        ///// <returns></returns>
        //public static JNode AggBy(JNode[] args)
        //{
        //    JObject gb = (JObject)GroupBy(args.Slice(":2").ToArray());
        //    CurJson fun = (CurJson)args[2];
        //    var aggs = new Dictionary<string, JNode>();
        //    foreach ((string key, JNode subitbl) in gb.children)
        //    {
        //        aggs[key] = fun.function(subitbl);
        //    }
        //    return new JObject(0, aggs);
        //}

        #endregion
        #region VECTORIZED_ARG_FUNCTIONS
        /// <summary>
        /// Length of a string
        /// </summary>
        /// <param name="node">string</param>
        public static JNode StrLen(JNode node)
        {
            if (node.type != Dtype.STR)
            {
                throw new RemesPathException("StrLen only works for strings");
            }
            return new JNode(Convert.ToInt64(((string)node.value).Length), Dtype.INT, 0);
        }

        /// <summary>
        /// Returns a string made by joining one string to itself n times.<br></br>
        /// Thus StrMul("ab", 3) -> "ababab"
        /// </summary>
        /// <param name="node">string</param>
        /// <param name="n">number of times to repeat s</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static JNode StrMul(JNode node, JNode n)
        {
            string s = (string)node.value;
            var sb = new StringBuilder();
            for (int i = 0; i < Convert.ToInt32(n.value); i++)
            {
                sb.Append(s);
            }
            return new JNode(sb.ToString(), Dtype.STR, 0);
        }

        /// <summary>
        /// Return a JNode of type = Dtype.INT with value equal to the number of ocurrences of 
        /// pattern or substring sub in node.value.<br></br>
        /// So StrCount(JNode("ababa", Dtype.STR, 0), Regex("a?ba")) -> JNode(2, Dtype.INT, 0)
        /// because "a?ba" matches "aba" starting at position 0 and "ba" starting at position 3.
        /// </summary>
        /// <param name="node">a string in which to find pattern/substring sub</param>
        /// <param name="sub">a substring or Regex pattern</param>
        public static JNode StrCount(JNode node, JNode sub)
        {
            string s = (string)node.value;
            int ct;
            if (sub.type == Dtype.REGEX)
            {
                ct = (((JRegex)sub).regex).Matches(s).Count;
                
            }
            else
            {
                ct = Regex.Matches(s, (string)sub.value).Count;
            }
            return new JNode(Convert.ToInt64(ct), Dtype.INT, 0);
        }

        /// <summary>
        /// Get a List<object?> containing all non-overlapping occurrences of regex pattern pat in
        /// string node
        /// </summary>
        /// <param name="node">string</param>
        /// <param name="sub">substring or Regex pattern to be found within node</param>
        /// <returns></returns>
        public static JNode StrFind(JNode node, JNode pat)
        {
            Regex rex = (pat as JRegex).regex;
            MatchCollection results = rex.Matches((string)node.value);
            var result_list = new List<JNode>();
            foreach (Match match in results)
            {
                result_list.Add(new JNode(match.Value, Dtype.STR, 0));
            }
            return new JArray(0, result_list);
        }

        public static JNode StrSplit(JNode node, JNode sep)
        {
            string s = (string)node.value;
            string[] parts = (sep.type == Dtype.STR) ? Regex.Split(s, (string)sep.value) : ((JRegex)sep).regex.Split(s);
            var out_nodes = new List<JNode>();
            foreach (string part in parts)
            {
                out_nodes.Add(new JNode(part, Dtype.STR, 0));
            }
            return new JArray(0, out_nodes);
        }

        public static JNode StrLower(JNode node)
        {
            return new JNode(((string)node.value).ToLower(), Dtype.STR, 0);
        }

        public static JNode StrUpper(JNode node)
        {
            return new JNode(((string)node.value).ToUpper(), Dtype.STR, 0);
        }

        public static JNode StrStrip(JNode node)
        {
            return new JNode(((string)node.value).Trim(), Dtype.STR, 0);
        }

        public static JNode StrSlice(JNode node, JNode slicer_or_int)
        {
            string s = (string)node.value;
            if (slicer_or_int is JSlicer)
            {
                return new JNode(s.Slice(((JSlicer)slicer_or_int).slicer), Dtype.STR, 0);
            }
            int index = Convert.ToInt32(slicer_or_int.value);
            // allow negative indices for consistency with slicing syntax
            index = index < s.Length ? index : s.Length + index;
            return new JNode(s.Substring(index, 1), Dtype.STR, 0);            
        }

        /// <summary>
        /// Replaces all instances of string to_replace with string repl in the string value
        /// of JNode node
        /// </summary>
         /// <returns>new JNode of type = Dtype.STR with all replacements made</returns>
        public static JNode StrSub(JNode node, JNode to_replace, JNode repl)
        {
            string val = (string)node.value;
            if (to_replace.type == Dtype.STR)
            {
                return new JNode(Regex.Replace(val, (string)to_replace.value, (string)repl.value), Dtype.STR, 0);
            }
            return new JNode(((JRegex)to_replace).regex.Replace(val, (string)repl.value), Dtype.STR, 0);
        }

        /// <summary>
        /// returns true is x is string
        /// </summary>
        public static JNode IsStr(JNode x)
        {
            return new JNode(x.type == Dtype.STR, Dtype.BOOL, 0);
        }

        /// <summary>
        /// returns true is x is long, double, or bool
        /// </summary>
        public static JNode IsNum(JNode x)
        {
            return new JNode((x.type & (Dtype.INT | Dtype.FLOAT | Dtype.BOOL)) != 0, Dtype.BOOL, 0);
        }

        /// <summary>
        /// returns true if x is JObject or JArray
        /// </summary>
        public static JNode IsExpr(JNode x)
        {
            return new JNode(x is JArray || x is JObject, Dtype.BOOL, 0);
        }

        public static JNode IfElse(JNode condition, JNode if_true, JNode if_false)
        {
            return (bool)condition.value ? if_true : if_false;
        }

        public static JNode Log(JNode val, JNode Base)
        {
            double num = Convert.ToDouble(val.value);
            if (Base.value == null)
            {
                return new JNode(Math.Log(num), Dtype.FLOAT, 0);
            }
            return new JNode(Math.Log(num, Convert.ToDouble(Base.value)), Dtype.FLOAT, 0);
        }

        public static JNode Log2(JNode val)
        {
            return new JNode(Math.Log2(Convert.ToDouble(val.value)), Dtype.FLOAT, 0);
        }

        public static JNode Abs(JNode val)
        {
            if (val.type == Dtype.INT)
            {
                return new JNode(Math.Abs(Convert.ToInt64(val.value)), Dtype.INT, 0);
            }
            else if (val.type == Dtype.FLOAT)
            {
                return new JNode(Math.Abs(Convert.ToDouble(val.value)), Dtype.FLOAT, 0);
            }
            throw new ArgumentException("Abs can only be called on ints and floats");
        }

        public static JNode IsNa(JNode val)
        {
            return new JNode(double.IsNaN(Convert.ToDouble(val.value)), Dtype.BOOL, 0);
        }

        public static JNode ToStr(JNode val)
        {
            if (val.type == Dtype.STR)
            {
                return new JNode(val.value, Dtype.STR, 0);
            }
            return new JNode(val.ToString(), Dtype.STR, 0);
        }

        public static JNode ToFloat(JNode val)
        {
            if (val.type == Dtype.STR)
            {
                return new JNode(double.Parse((string)val.value), Dtype.FLOAT, 0);
            }
            return new JNode(Convert.ToDouble(val.value), Dtype.FLOAT, 0);
        }

        public static JNode ToInt(JNode val)
        {
            if (val.type == Dtype.STR)
            {
                return new JNode(long.Parse((string)val.value), Dtype.INT, 0);
            }
            return new JNode(Convert.ToInt64(val.value), Dtype.INT, 0);
        }

        /// <summary>
        /// If val is an int/long, return val because rounding does nothing to an int/long.<br></br>
        /// If val is a double:<br></br>
        ///     - if sigfigs is null, return val rounded to the nearest long.<br></br>
        ///     - else return val rounded to nearest double with sigfigs decimal places<br></br>
        /// If val's value is any other type, throw an ArgumentException
        /// </summary>
        public static JNode Round(JNode val, JNode sigfigs)
        {
            if (val.type == Dtype.INT)
            {
                return new JNode(val.value, Dtype.INT, 0);
            }
            else if (val.type == Dtype.FLOAT)
            {
                if (sigfigs == null)
                {
                    return new JNode(Convert.ToInt64(Math.Round(Convert.ToDouble(val))), Dtype.INT, 0);
                }
                return new JNode(Math.Round(Convert.ToDouble(val), Convert.ToInt32(sigfigs.value)), Dtype.FLOAT, 0);
            }
            throw new ArgumentException("Cannot round non-float, non-integer numbers");            
        }

        public static JNode Not(JNode val)
        {
            return new JNode(!Convert.ToBoolean(val.value), Dtype.BOOL, 0);
        }

        public static JNode Uminus(JNode val)
        {
            if (val.type == Dtype.INT)
            {
                return new JNode(-Convert.ToInt64(val.value), Dtype.INT, 0);
            }
            if (val.type == Dtype.FLOAT)
            {
                return new JNode(-Convert.ToDouble(val.value), Dtype.FLOAT, 0);
            }
            throw new RemesPathException("Unary '-' can only be applied to ints and floats");
        }

        #endregion

        public static JNode ObjectsToJNode(object? obj)
        {
            if (obj == null)
            {
                return new JNode(null, Dtype.NULL, 0);
            }
            if (obj is long)
            {
                return new JNode(Convert.ToInt64(obj), Dtype.INT, 0);
            }
            if (obj is double)
            {
                return new JNode((double)obj, Dtype.FLOAT, 0);
            }
            if (obj is string)
            {
                return new JNode((string)obj, Dtype.STR, 0);
            }
            if (obj is bool)
            {
                return new JNode((bool)obj, Dtype.BOOL, 0);
            }
            if (obj is List<object?>)
            {
                var nodes = new List<JNode>();
                foreach (object? child in (List<object?>)obj)
                {
                    nodes.Add(ObjectsToJNode(child));
                }
                return new JArray(0, nodes);
            }
            else if (obj is Dictionary<string, object?>)
            {
                var nodes = new Dictionary<string, JNode>();
                foreach ((string key, object? val) in (Dictionary<string, object?>)obj)
                {
                    nodes[key] = ObjectsToJNode(val);
                }
                return new JObject(0, nodes);
            }
            throw new ArgumentException("Cannot convert any objects to JNode except null, long, double, bool, string, List<object?>, or Dictionary<string, object?");
        }

        /// <summary>
        /// Recursively extract the values from a JNode, converting JArrays into lists of objects,
        /// JObjects into Dictionaries mapping strings to objects, and everything else to its value.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static object? JNodeToObjects(JNode node)
        {
            // if it's not an obj, arr, or unknown, just return its value
            if ((node.type & Dtype.ITERABLE) == 0)
            {
                return node.value;
            }
            if (node.type == Dtype.UNKNOWN)
            {
                return (CurJson)node;
            }
            if (node.type == Dtype.OBJ)
            {
                var dic = new Dictionary<string, object?>();
                foreach ((string key, JNode val) in ((JObject)node).children)
                {
                    dic[key] = JNodeToObjects(val);
                }
                return dic;
            }
            var arr = new List<object?>();
            foreach (JNode val in ((JArray)node).children)
            {
                arr.Add(JNodeToObjects(val));
            }
            return arr;
        }

        /// <summary>
        /// Vectorize an ArgFunction with 1 to 5 arguments.<br></br>
        /// The function returned by VectorizeArgFunction will take an array of object? as input,
        /// the first argument of which is either a valid scalar input (e.g., a string for StrStrip)<br></br>
        /// Throws an exception if you try to vectorize a function with 0 args or more than 5 args.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="max_args"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Func<JNode[], JNode> VectorizeArgFunc(object func, int max_args)
        {
            if (max_args == 1)
            {
                var the_fun = (Func<JNode, JNode>)func;
                JNode Vectorized(JNode[] args)
                {
                    return the_fun(args[0]);
                }
                return Vectorized;
            }
            if (max_args == 2)
            {
                var the_fun = (Func<JNode, JNode, JNode>)func;
                JNode Vectorized(JNode[] args)
                {
                    return the_fun(args[0], args[1]);
                }
                return Vectorized;
            }
            if (max_args == 3)
            {
                var the_fun = (Func<JNode, JNode, JNode, JNode>)func;
                JNode Vectorized(JNode[] args)
                {
                    return the_fun(args[0], args[1], args[2]);
                }
                return Vectorized;
            }
            if (max_args == 4)
            {
                var the_fun = (Func<JNode, JNode, JNode, JNode, JNode>)func;
                JNode Vectorized(JNode[] args)
                {
                    return the_fun(args[0], args[1], args[2], args[3]);
                }
                return Vectorized;
            }
            if (max_args == 5)
            {
                var the_fun = (Func<JNode, JNode, JNode, JNode, JNode, JNode>)func;
                JNode Vectorized(JNode[] args)
                {
                    return the_fun(args[0], args[1], args[2], args[3], args[4]);
                }
                return Vectorized;
            }
            if (max_args == 6)
            {
                var the_fun = (Func<JNode, JNode, JNode, JNode, JNode, JNode, JNode>)func;
                JNode Vectorized(JNode[] args)
                {
                    return the_fun(args[0], args[1], args[2], args[3], args[4], args[5]);
                }
                return Vectorized;
            }
            throw new NotImplementedException("Cannot vectorize functions with more than 6 arguments");
        }

        public static Dictionary<string, ArgFunction> FUNCTIONS =
        new Dictionary<string, ArgFunction>
        {
            // non-vectorized functions
            ["avg"] = new ArgFunction(ArgFunction.Mean, "avg", Dtype.FLOAT, 1, 1, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN }),
            ["flatten"] = new ArgFunction(ArgFunction.Flatten, "flatten", Dtype.ARR, 1, 2, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.INT }),
            ["index"] = new ArgFunction(ArgFunction.Index, "index", Dtype.INT, 2, 3, false, new Dtype[] {Dtype.ITERABLE, Dtype.SCALAR, Dtype.BOOL}),
            ["irange"] = new ArgFunction(ArgFunction.IRange, "irange", Dtype.ARR, 1, 3, false, new Dtype[] {Dtype.INT, Dtype.INT, Dtype.INT}),
            ["keys"] = new ArgFunction(ArgFunction.Keys, "keys", Dtype.ARR, 1, 1, false, new Dtype[] {Dtype.OBJ | Dtype.UNKNOWN}),
            ["len"] = new ArgFunction(ArgFunction.Len, "len", Dtype.INT, 1, 1, false, new Dtype[] {Dtype.ITERABLE}),
            ["max"] = new ArgFunction(ArgFunction.Max, "max", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["max_by"] = new ArgFunction(ArgFunction.MaxBy, "max_by", Dtype.ARR_OR_OBJ, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT}),
            ["mean"] = new ArgFunction(ArgFunction.Mean, "mean", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["min"] = new ArgFunction(ArgFunction.Min, "min", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["min_by"] = new ArgFunction(ArgFunction.MinBy, "min_by", Dtype.ARR_OR_OBJ, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT}),
            ["quantile"] = new ArgFunction(ArgFunction.Quantile, "quantile", Dtype.FLOAT, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.FLOAT}),
            ["s_join"] = new ArgFunction(ArgFunction.StringJoin, "s_join", Dtype.STR, 2, 2, false, new Dtype[] {Dtype.STR, Dtype.ARR | Dtype.UNKNOWN}),
            ["sort_by"] = new ArgFunction(ArgFunction.SortBy, "sort_by", Dtype.ARR, 2, 3, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT, Dtype.BOOL }),
            ["sorted"] = new ArgFunction(ArgFunction.Sorted, "sorted", Dtype.ARR, 1, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.BOOL}),
            ["sum"] = new ArgFunction(ArgFunction.Sum, "sum", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["unique"] = new ArgFunction(ArgFunction.Unique, "unique", Dtype.ARR, 1, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.BOOL}),
            ["value_counts"] = new ArgFunction(ArgFunction.ValueCounts, "value_counts",Dtype.ARR_OR_OBJ, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["values"] = new ArgFunction(ArgFunction.Values, "values", Dtype.ARR, 1, 1, false, new Dtype[] {Dtype.OBJ | Dtype.UNKNOWN}),
            ["group_by"] = new ArgFunction(ArgFunction.GroupBy, "group_by", Dtype.OBJ, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT}),
            //["agg_by"] = new ArgFunction(ArgFunction.AggBy, "agg_by", Dtype.OBJ, 3, 3, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT, Dtype.ITERABLE | Dtype.SCALAR }),
            // vectorized functions
            ["abs"] = new ArgFunction(VectorizeArgFunc(ArgFunction.Abs, 1), "abs", Dtype.FLOAT_OR_INT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["float"] = new ArgFunction(VectorizeArgFunc(ArgFunction.ToFloat, 1), "float", Dtype.FLOAT, 1, 1, true, new Dtype[] { Dtype.SCALAR | Dtype.ITERABLE}),
            ["ifelse"] = new ArgFunction(VectorizeArgFunc(ArgFunction.IfElse, 3), "ifelse", Dtype.UNKNOWN, 3, 3, true, new Dtype[] {Dtype.SCALAR | Dtype.ITERABLE, Dtype.ITERABLE | Dtype.SCALAR, Dtype.ITERABLE | Dtype.SCALAR}),
            ["int"] = new ArgFunction(VectorizeArgFunc(ArgFunction.ToInt, 1), "int", Dtype.INT, 1, 1, true, new Dtype[] {Dtype.SCALAR | Dtype.ITERABLE}),
            ["is_expr"] = new ArgFunction(VectorizeArgFunc(ArgFunction.IsExpr, 1), "is_expr", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.SCALAR | Dtype.ITERABLE}),
            ["is_num"] = new ArgFunction(VectorizeArgFunc(ArgFunction.IsNum, 1), "is_num", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.SCALAR | Dtype.ITERABLE}),
            ["is_str"] = new ArgFunction(VectorizeArgFunc(ArgFunction.IsStr, 1), "is_str", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.SCALAR | Dtype.ITERABLE}),
            ["isna"] = new ArgFunction(VectorizeArgFunc(ArgFunction.IsNa, 1), "isna", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.SCALAR | Dtype.ITERABLE}),
            ["log"] = new ArgFunction(VectorizeArgFunc(ArgFunction.Log, 2), "log", Dtype.FLOAT, 1, 2, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE, Dtype.FLOAT_OR_INT}),
            ["log2"] = new ArgFunction(VectorizeArgFunc(ArgFunction.Log2, 1), "log2", Dtype.FLOAT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["__UMINUS__"] = new ArgFunction(VectorizeArgFunc(Uminus, 1), "-", Dtype.FLOAT_OR_INT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["not"] = new ArgFunction(VectorizeArgFunc(ArgFunction.Not, 1), "not", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.BOOL | Dtype.ITERABLE}),
            ["round"] = new ArgFunction(VectorizeArgFunc(ArgFunction.Round, 2), "round", Dtype.FLOAT_OR_INT, 1, 2, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE, Dtype.INT}),
            ["s_count"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrCount, 2), "s_count", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX}),
            ["s_find"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrFind, 2), "s_find", Dtype.ARR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.REGEX}),
            ["s_len"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrLen, 1), "s_len", Dtype.INT, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_lower"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrLower, 1), "s_lower", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_mul"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrMul, 2), "s_mul", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.INT}),
            ["s_slice"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrSlice, 2), "s_slice", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.INT_OR_SLICE}),
            ["s_split"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrSplit, 2), "s_split", Dtype.ARR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX}),
            ["s_strip"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrStrip, 1), "s_strip", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_sub"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrSub, 3), "s_sub", Dtype.STR, 3, 3, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX, Dtype.STR}),
            ["s_upper"] = new ArgFunction(VectorizeArgFunc(ArgFunction.StrUpper, 1), "s_upper", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["str"] = new ArgFunction(VectorizeArgFunc(ArgFunction.ToStr, 1), "str", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.SCALAR | Dtype.ITERABLE})
        };
    }


    public class ArgFunctionWithArgs
    {
        public ArgFunction function;
        public JNode[] args;

        public ArgFunctionWithArgs(ArgFunction function, JNode[] args)
        {
            this.function = function;
            this.args = args;
        }

        public JNode Call()
        {
            return function.Call(args);
        }
    }


    /// <summary>
    /// A stand-in for an object that is a function of the user's input.
    /// This can take on any value, including a scalar (e.g., len(@) is CurJson of type Dtype.INT).
    /// The Function field must take any object or null as input and return an object of the type reflected
    /// in the CurJson's type field.
    /// So for example, a CurJson node standing in for len(@) would be initialized as follows:
    /// CurJson(Dtype.INT, obj => obj.Length)
    /// </summary>
    public class CurJson : JNode
    {
        public Func<JNode, JNode> function;
        public CurJson(Dtype type, Func<JNode, JNode> function) : base(null, type, 0)
        {
            this.function = function;
        }

        /// <summary>
        /// A CurJson node that simply stands in for the current json itself (represented by @)
        /// Its function is the identity function.
        /// </summary>
        public CurJson() : base(null, Dtype.UNKNOWN, 0)
        {
            function = Identity;
        }

        public JNode Identity(JNode obj)
        {
            return obj;
        }
    }

    class BinopTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            JNode jtrue = jsonParser.Parse("true"); JNode jfalse = jsonParser.Parse("false");
            var testcases = new (JNode x, JNode y, Binop bop, JNode desired, string msg)[]
            {
                (jsonParser.Parse("1"), jsonParser.Parse("3"), Binop.BINOPS["-"], jsonParser.Parse("-2"), "subtraction of ints"),
                (jsonParser.Parse("2.5"), jsonParser.Parse("5"), Binop.BINOPS["/"], jsonParser.Parse("0.5"), "division of float by int"),
                (jsonParser.Parse("\"a\""), jsonParser.Parse("\"b\""), Binop.BINOPS["+"], jsonParser.Parse("\"ab\""), "addition of strings"),
                (jsonParser.Parse("3"), jsonParser.Parse("4"), Binop.BINOPS[">="], jfalse, "comparison ge"),
                (jsonParser.Parse("7"), jsonParser.Parse("9"), Binop.BINOPS["<"], jtrue, "comparison lt"),
                (jsonParser.Parse("\"abc\""), jsonParser.Parse("\"ab+\""), Binop.BINOPS["=~"], jtrue, "has_pattern"),
                (jsonParser.Parse("\"a\""), jsonParser.Parse("[\"a\", \"b\"]"), Binop.BINOPS["in"], jtrue, "string in list"),
                (jsonParser.Parse("\"d\""), jsonParser.Parse("[\"b\", \"c\"]"), Binop.BINOPS["in"], jfalse, "string not in list"),
                (jsonParser.Parse("\"b\""), jsonParser.Parse("{\"b\": 1, \"a\": 2}"), Binop.BINOPS["in"], jtrue, "string in dict keys"),
                (jsonParser.Parse("\"c\""), jsonParser.Parse("{\"b\": 1, \"a\": 2}"), Binop.BINOPS["in"], jfalse, "string in dict keys"),
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((JNode x, JNode y, Binop bop, JNode desired, string msg) in testcases)
            {
                JNode output = bop.Call(x, y);
                string str_desired = desired.ToString();
                string str_output = output.ToString();
                if (str_desired != str_output)
                {

                    tests_failed++;
                    Console.WriteLine(String.Format("Test {0} (input \"{1}({2}, {3})\", {4}) failed:\n" +
                                                    "Expected\n{5}\nGot\n{6}",
                                                    ii+1, bop, x.ToString(), y.ToString(), msg, str_desired, str_output));
                }
                ii++;
            }
            ii = testcases.Length;
            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }
    }

    class ArgFunctionTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            JNode jtrue = jsonParser.Parse("true");
            JNode jfalse = jsonParser.Parse("false");
            var testcases = new (JNode[] args, ArgFunction f, JNode desired)[]
            {
                (new JNode[]{jsonParser.Parse("[1,2]")}, ArgFunction.FUNCTIONS["len"], new JNode(Convert.ToInt64(2), Dtype.INT, 0)),
                (new JNode[]{jsonParser.Parse("[1,2]"), jtrue}, ArgFunction.FUNCTIONS["sorted"], jsonParser.Parse("[2,1]")),
                (new JNode[]{jsonParser.Parse("[[1,2], [4, 1]]"), new JNode(Convert.ToInt64(1), Dtype.INT, 0), jfalse }, ArgFunction.FUNCTIONS["sort_by"], jsonParser.Parse("[[4, 1], [1, 2]]")),
                (new JNode[]{jsonParser.Parse("[1, 3, 2]")}, ArgFunction.FUNCTIONS["mean"], new JNode(2.0, Dtype.FLOAT, 0)),
                //(new JNode[]{jsonParser.Parse("[{\"a\": 1, \"b\": 2}, {\"a\": 3, \"b\": 1}]"), new JNode("b", Dtype.STR, 0)}, ArgFunction.FUNCTIONS["min_by"], jsonParser.Parse("{\"a\": 3, \"b\": 1}")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]")}, ArgFunction.FUNCTIONS["s_len"], jsonParser.Parse("[2, 3, 0]")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]"), new JNode("a", Dtype.STR, 0), new JNode("z", Dtype.STR, 0)}, ArgFunction.FUNCTIONS["s_sub"], jsonParser.Parse("[\"zb\", \"bcz\", \"\"]")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]"), new JRegex(new Regex(@"a+")), new JNode("z", Dtype.STR, 0)}, ArgFunction.FUNCTIONS["s_sub"], jsonParser.Parse("[\"zb\", \"bcz\", \"\"]")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]"), new JSlicer(new int?[] {1, null, -1})}, ArgFunction.FUNCTIONS["s_slice"], jsonParser.Parse("[\"ba\", \"cb\", \"\"]")),
                //(new JNode[]{jsonParser.Parse("{\"a\": \"2\", \"b\": \"1.5\"}")}, ArgFunction.FUNCTIONS["float"], jsonParser.Parse("{\"a\": 2.0, \"b\": 1.5}")),
                //(new JNode[]{jsonParser.Parse("{\"a\": \"a\", \"b\": \"b\"}"), new JNode(3, Dtype.INT, 0)}, ArgFunction.FUNCTIONS["s_mul"], jsonParser.Parse("{\"a\": \"aaa\", \"b\": \"bbb\"}"))
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((JNode[] args, ArgFunction f, JNode desired) in testcases)
            {
                JNode output = f.Call(args);
                var sb = new StringBuilder();
                sb.Append('{');
                int argnum = 0;
                while (argnum < args.Length)
                {
                    sb.Append(args[argnum++].ToString());
                    if (argnum < (args.Length - 1)) { sb.Append(", "); }
                }
                sb.Append('}');
                string argstrings = sb.ToString();
                string str_desired = desired.ToString();
                string str_output = output.ToString();
                if (str_desired != str_output)
                {

                    tests_failed++;
                    Console.WriteLine(String.Format("Test {0} (input \"{1}({2}) failed:\nExpected\n{3}\nGot\n{4}",
                                                    ii+1, f, argstrings, str_desired, str_output));
                }
                ii++;
            }
            ii = testcases.Length;
            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }
    }
}