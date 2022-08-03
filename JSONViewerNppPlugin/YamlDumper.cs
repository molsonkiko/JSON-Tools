﻿/*
Reads a JSON document and outputs YAML that can be serialized back to 
equivalent (or very nearly equivalent) JSON.
*/
using System.Text;
using System.Text.RegularExpressions;

namespace JSON_Viewer.JSONViewerNppPlugin
{
    public class YamlDumper
    {
        // NonAscii functions from https://stackoverflow.com/questions/1615559/convert-a-unicode-string-to-an-escaped-ascii-string
        //static string EncodeNonAsciiCharacters(string value)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (char c in value)
        //    {
        //        if (c > 127)
        //        {
        //            // This character is too big for ASCII
        //            string encodedValue = "\\u" + ((int)c).ToString("x4");
        //            sb.Append(encodedValue);
        //        }
        //        else
        //        {
        //            sb.Append(c);
        //        }
        //    }
        //    return sb.ToString();
        //}

        //static string DecodeEncodedNonAsciiCharacters(string value)
        //{
        //    return Regex.Replace(
        //        value,
        //        @"\\u(?<Value>[a-zA-Z0-9]{4})",
        //        m => {
        //            return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
        //        });
        //}

        private Boolean IsIterable(JNode x)
        {
            return (x == null) ? false : x is JObject || x is JArray;
        }

        private Boolean StartsOrEndsWith(string x, string suf_or_pref)
        {
            return (x.Length > 0) && (suf_or_pref.Contains(x[0]) || suf_or_pref.Contains(x[x.Length - 1]));
        }

        private string EscapeBackslash(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append(@"\\t"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\'': sb.Append(@"\'"); break;
                    case '\f': sb.Append(@"\\f"); break;
                    case '\b': sb.Append(@"\\b"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string EscapeBackslashKey(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append(@"\\t"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\'': sb.Append("''"); break;
                    case '\f': sb.Append(@"\\f"); break;
                    case '\b': sb.Append(@"\\b"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string YamlKeyRepr(string k)
        {
            if (double.TryParse(k, out double d))
            {
                // k is a string representing a number; we need to enquote it so that
                // a YAML parser will recognize that it is not actually a number.
                return "'" + k + "'";
            }
            Regex forbidden_key_chars = new Regex(@"[\t :]");
            if (forbidden_key_chars.IsMatch(k))
            {
                // '\t', ' ', and ':' are all illegal inside a YAML key. We will escape those out
                return EscapeBackslashKey(k);
            }
            return k;
        }

        private string YamlValRepr(JNode v)
        {
            if (v.type == Dtype.NULL)
            {
                return "null";
            }
            object val = v.value;
            string strv = val.ToString();
            if (v.type == Dtype.INT || v.type == Dtype.BOOL)
            {
                return strv;
            }
            if (double.TryParse(strv, out double d))
            {
                // k is a number
                switch (d)
                {
                    case double.PositiveInfinity: return ".inf";
                    case double.NegativeInfinity: return "-.inf";
                }
                if (double.IsNaN(d))
                {
                    return ".nan";
                }
                if (v.type == Dtype.STR)
                {
                    // enquote numstrings to prevent confusion
                    return "'" + strv + "'";
                }
                if (v.type == Dtype.FLOAT && double.Parse(strv) % 1 == 0)
                {
                    // ensure that floats equal to ints are rendered as floats
                    return strv + ".0";
                }
                return strv;
            }
            if (StartsOrEndsWith(strv, " "))
            {
                return '"' + strv + '"';
            }
            Regex backslash = new Regex("([\\\\:\"'\r\t\n\f\b])");
            if (backslash.IsMatch(strv))
            {
                //Console.WriteLine("has backslash");
                return EscapeBackslash(strv);
            }
            return strv;
        }

        private void BuildYaml(StringBuilder sb, JNode tok, int depth, int indent)
        {
            // start the line with depth * indent blank spaces
            string indent_space = new string(' ', indent * depth);
            if (tok is JObject)
            {
                var o = (JObject)tok;
                if (o.children.Count == 0)
                {
                    sb.Append(indent_space + "{}\n");
                    return;
                }
                foreach (string k in o.children.Keys)
                {
                    JNode v = o.children[k];
                    // Console.WriteLine("k = " + k);
                    if (IsIterable(v))
                    {
                        sb.Append(String.Format("{0}{1}:\n", indent_space, YamlKeyRepr(k)));
                        BuildYaml(sb, v, depth + 1, indent);
                    }
                    else
                    {
                        sb.Append(String.Format("{0}{1}: {2}\n",
                                             indent_space,
                                             YamlKeyRepr(k),
                                             YamlValRepr(v)));
                    }
                }
            }
            else if (tok is JArray)
            {
                JArray a = (JArray)tok;
                if (a.children.Count == 0)
                {
                    sb.Append(indent_space + "[]\n");
                    return;
                }
                foreach (JNode child in a.children)
                {
                    if (IsIterable(child))
                    {
                        sb.Append(String.Format("{0}-\n", indent_space));
                        BuildYaml(sb, child, depth + 1, indent);
                    }
                    else
                    {
                        sb.Append(String.Format("{0}- {1}\n",
                            indent_space, YamlValRepr(child)));
                    }
                }
            }
            else
            {
                sb.Append(String.Format("{0}\n", YamlValRepr(tok)));
            }
        }
        public string Dump(JNode json, int indent)
        {
            //Console.WriteLine(obj);
            var sb = new StringBuilder();
            BuildYaml(sb, json, 0, indent);
            return sb.ToString();
        }

        public string Dump(JNode json)
        {
            return Dump(json, 4);
        }
    }

    public class YamlDumperTester
    {
        public static int MyUnitTest(Tuple<string, string, string>[] testcases)
        {
            JsonParser jsonParser = new JsonParser();
            int tests_failed = 0;
            YamlDumper yamlDumper = new YamlDumper();
            for (int ii = 0; ii < testcases.Length; ii++)
            {
                var input = testcases[ii].Item1;
                JNode json = jsonParser.Parse(input);
                var correct = testcases[ii].Item2;
                var description = testcases[ii].Item3;
                var result = yamlDumper.Dump(json, 2);
                if (correct != result)
                {
                    Console.WriteLine(String.Format(@"Test {0} ({1}) failed:
Expected
{2}
Got
{3}",
                                      ii + 1, description, correct, result));
                    tests_failed++;
                }
            }
            Console.WriteLine("Failed " + tests_failed + " tests.");
            Console.WriteLine("Passed " + (testcases.Length - tests_failed) + " tests.");
            return tests_failed;
        }

        public static void Test()
        {
            Tuple<string, string, string>[] tests = {
				// space at end of key
				Tuple.Create("{\"adogDOG! \": \"dog\"}", "\"adogDOG! \": dog\n",
                            "space at end of key"),
                Tuple.Create("{\" adogDOG!\": \"dog\"}", "\" adogDOG!\": dog\n",
                             "space at start of key"),
				// space inside key
				Tuple.Create("{\"a dog DOG!\": \"dog\"}", "\"a dog DOG!\": dog\n",
                            "space inside key"),
				// stringified nums as keys
				Tuple.Create("{\"9\": 9}", "'9': 9\n", "stringified num as key"),
				//
				Tuple.Create("{\"9\": \"9\"}", "'9': '9'\n", "stringified num as val"),
                Tuple.Create("{\"9a\": \"9a\", \"a9.2\": \"a9.2\"}", "9a: 9a\na9.2: a9.2\n",
                             "partially stringified nums as vals"),
                Tuple.Create("{\"a\\\"b'\": \"bub\\\"ar\"}", "a\"b': \"bub\\\"ar\"\n",
                            "singlequotes and doublequotes inside key"),
                Tuple.Create("{\"a\": \"big\\nbad\\ndog\"}", "a: \"big\\nbad\\ndog\"\n",
                            "values containing newlines"),
                Tuple.Create("{\"a\": \" big \"}", "a: \" big \"\n",
                            "leading or ending space in dict value"),
                Tuple.Create("[\" big \"]", "- \" big \"\n",
                            "leading or ending space in array value"),
                Tuple.Create("\"a \"", "\"a \"\n", "scalar string"),
                Tuple.Create("9", "9\n", "scalar int"),
                Tuple.Create("-940.3", "-940.3\n", "scalar float"),
                Tuple.Create("[true, false]", "- True\n- False\n", "scalar bools"),
                Tuple.Create("[null, Infinity, -Infinity, NaN]",
                             "- \n- .inf\n- -.inf\n- .nan\n",
                             "null, +/-infinity, NaN"),
                // in the below case, there's actually a bit of an error;
                // it is better to dump the float 2.0 as '2.0', but this algorithm dumps it
                // as an integer.
                // So there's some room for improvement here
                Tuple.Create("{\"a\": [[1, 2.0], { \"3\": [\"5\"]}], \"2\": 6}",
                             "a:\n  -\n    - 1\n    - 2.0\n  -\n    '3':\n      - '5'\n'2': 6\n",
                             "nested iterables"),
                Tuple.Create("{\"a\": \"a: b\"}", "a: \"a: b\"\n", "value contains colon"),
                Tuple.Create("{\"a: b\": \"a\"}", "\"a: b\": a\n", "key contains colon"),
                Tuple.Create("{\"a\": \"RT @blah: MondayMo\\\"r\'ing\"}",
                            "a: \'RT @blah: MondayMo\"r\'\'ing\'\n",
                            "Value contains quotes and colon"),
                Tuple.Create("{\"a\": \"a\\n\'big\'\\ndog\"}", "a: \"a\\n\'big\'\\ndog\"\n",
                             "Value contains quotes and newline"),
                Tuple.Create("{\"a\": \"RT @blah: MondayMo\\nring\"}",
                             "a: \"RT @blah: MondayMo\\nring\"\n",
                             "value contains newline and colon"),
                Tuple.Create("{\"\\\"a: 'b'\": \"a\"}", "\'\"a: \'\'b\'\'\': a\n",
                            "key contains quotes and colon")
            };
            MyUnitTest(tests);
        }
    }
}
