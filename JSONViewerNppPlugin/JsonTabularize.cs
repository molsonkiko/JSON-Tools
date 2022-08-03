/*
Utilities for converting JSON with a "tabular" layout 
(broadly speaking, arrays of arrays, objects with keys mapping to parallel arrays, and arrays of same-format objects)
into arrays of same-format objects.
Uses an algorithm to flatten nested JSON.
*/
using System.Text;

namespace JSON_Viewer.JSONViewerNppPlugin
{
	public class JsonTabularizer
	{
		public JsonTabularizer()
		{
		}

		/// <summary>
		/// Determines whether a JSON schema represents a scalar (<b>"scal"</b>),<br></br>
		/// an array of arrays of scalars (<b>"sqr"</b>, e.g. [[1, 2], [3, 4]])<br></br>
		/// an array containing only objects with scalar values (<b>"rec"</b>, e.g. [{"a": 1, "b": "a"}, {"a": 2, "b": "b"}])<br></br>
		/// an object or array with only scalar values (<b>"row"</b>, e.g. [1, 2] or {"a": 1})<br></br>
		/// or something else (<b>"bad"</b>, e.g. [{"a": [1]}, [1, 2]]) 
		/// </summary>
		/// <param name="schema"></param>
		/// <returns></returns>
		public string ClassifySchema(Dictionary<string, object> schema)
        {
			Dtype tipe = (Dtype)schema["type"];
			if ((tipe & Dtype.SCALAR) != 0)
            {
				return "scal";
            }
			if (tipe == Dtype.ARR)
            {
				// it's tabular if it contains only arrays of scalars
				// or only objects where all values are scalar
				var items = (Dictionary<string, object>)schema["items"];
				bool item_has_type = items.TryGetValue("type", out object item_types);
				if (!item_has_type)
                {
					// this is because it's an "anyOf" type, where it can have some non-scalars and some scalars as values.
					// we can't tabularize it so we call it "bad"
					return "bad";
                }
				if (item_types is List<object>)
                {
					// it's an array with a mixture of element types
					foreach (object t in (List<object>)item_types)
					{
						if (((Dtype)t & Dtype.SCALAR) == 0)
						{
							// disallow arrays of arrays of non-scalars
							return "bad";
						}
					}
					// a list of scalars is not a tabular schema, but a "row".
					// rows have desirable properties, though,
					// because we can flatten them into keys of the parent object
					return "row";
                }
				Dtype item_dtype = (Dtype)item_types;
				if ((item_dtype & Dtype.SCALAR) != 0)
                {
					// it's an array where all elements have the same (scalar) type
					return "row";
                }
				if (item_dtype == Dtype.ARR)
                {
					// it's an array of arrays
					Dictionary<string, object> subitems = (Dictionary<string, object>)items["items"];
					bool subarr_has_type = subitems.TryGetValue("type", out object subarr_types);
					if (!subarr_has_type)
                    {
						// this is an array containing mixed arrays - bad!
						return "bad";
                    }
					if (subarr_types is List<object>)
                    {
						foreach (object v in (List<object>)subarr_types)
                        {
							if (((Dtype)v & Dtype.SCALAR) == 0)
                            {
								// this is an array of mixed scalar/non-scalar arrays, no good!
								return "bad";
                            }
                        }
						// an array of arrays of mixed-type, e.g., [[1, "2"], [3, "4"]]
						return "sqr";
                    }
					if (subarr_types is Dtype && ((Dtype)subarr_types & Dtype.SCALAR) != 0)
                    {
						// an array of arrays of homogeneous scalars (e.g., [[1, 2], [3, 4]])
						return "sqr";
                    }
					// an array of arrays of non-scalars
					return "bad";
                }
				var subprops = (Dictionary<string, object>)items["properties"];
				foreach (object prop in subprops.Values)
                {
					var dprop = (Dictionary<string, object>)prop;
					bool has_subtipe = dprop.TryGetValue("type", out object subtipe);
					if (!has_subtipe) 
					{ 
						return "bad"; // it's a dict with a mixture of non-scalar types - bad! 
					}
					if (!(subtipe is List<object> || (subtipe is Dtype && ((Dtype)subtipe & Dtype.SCALAR) != 0)))
                    {
						// it's a dict containing non-scalar values
						return "bad";
                    }
                }
				// an array of dicts with only scalar values
				return "rec";
            }
			if (!schema.ContainsKey("properties"))
            {
				return "bad";
            }
			// it's an object
			string cls = "row";
			var props = (Dictionary<string, object>)schema["properties"];
			foreach (object prop in props.Values)
            {

				var dprop = (Dictionary<string, object>)prop;
				bool has_subtipe = dprop.TryGetValue("type", out object subtipe);
				if (!has_subtipe)
				{
					return "bad"; // it's a dict with a mixture of non-scalar types - bad! 
				}
				if (subtipe is Dtype && (Dtype)subtipe == Dtype.OBJ)
                {
					Dictionary<string, object> subprops = (Dictionary<string, object>)dprop["properties"];
					foreach (object subprop in subprops.Values)
                    {
						Dictionary<string, object> dsubprop = (Dictionary<string, object>)subprop;
						bool subprop_has_type = dsubprop.TryGetValue("type", out object subproptipe);
						if (!subprop_has_type)
                        {
							// this key of the dictionary has a mixture of nonscalar types
							return "bad";
                        }
						if (!(subproptipe is List<object> || (subtipe is Dtype && ((Dtype)subproptipe & Dtype.SCALAR) != 0)))
                        {
							// this key of the dictionary has one nonscalar type
							return "bad";
                        }
						continue;
					}
                }
				if (subtipe is Dtype && (Dtype)subtipe != Dtype.ARR)
                {
					// it's OK for a table to have some scalar values and some list values.
					// the scalars values are just copy-pasted into each row for that table
					continue;
                }
				bool subarr_has_type = ((Dictionary<string, object>)dprop["items"]).TryGetValue("type", out object subarr_tipe);
				if (!subarr_has_type || (subarr_tipe is Dtype && ((Dtype)subarr_tipe & Dtype.SCALAR) == 0))
                {
					return "bad";
                }
				// it must be a subarray filled with scalars, so now the presumptive type is "tab"
				// the "tab" type represents a dict where some keys map to scalars and others map to lists.
				// e.g. {"a": [1, 2, 3], "b": ["a", "b", "c"], "c": 3.0} is a tab.
				cls = "tab";
			}
			// by now we've determined that the dict either contains all scalars (or dicts with all scalars)
			// in which case it is a "row"
			// OR it contains a mixture of scalars and all-scalar arrays, in which case it is a "tab".
			return cls;
        }


		private void FindTabsInSchemaHelper(Dictionary<string, object> schema, List<object> path, Dictionary<string, string> tab_paths)
        {
			string cls = ClassifySchema(schema);
			if (cls == "scal" || cls == "row")
            {
				// scalars and rows can't have tables as children
				return;
            }
			if (cls == "sqr" || cls == "tab" || cls == "rec")
            {
				JArray patharr = new JArray(0, new List<JNode>());
				foreach (object node in path)
                {
					if (node is double && double.IsInfinity((double)node))
                    {
						patharr.children.Add(new JNode((double)node, Dtype.FLOAT, 0));
                    }
                    else
                    {
						patharr.children.Add(new JNode((string)node, Dtype.STR, 0));
                    }
                }
				tab_paths[patharr.ToString()] = cls;
				return;
            }
			// by process of elimination, it's bad, but maybe it contains tables
			object tipe = schema["type"];
			if (tipe is Dtype && (Dtype)tipe == Dtype.ARR)
            {
				// it's an array; we'll search recursively at each index for tables
				var items = (Dictionary<string, object>)schema["items"];
				List<object> newpath = new List<object>(path);
				newpath.Add(double.PositiveInfinity);
				if (items.TryGetValue("anyOf", out object anyof))
                {
					List<object> danyof = (List<object>)anyof;
					foreach (object subschema in danyof)
                    {
						FindTabsInSchemaHelper((Dictionary<string, object>)subschema, newpath, tab_paths);
                    }
                }
                else
                {
					FindTabsInSchemaHelper(items, newpath, tab_paths);
                }
				return;
            }
			// it's a dict; we'll search recursively beneath each key for tables
			var props = (Dictionary<string, object>)schema["properties"];
			foreach ((string k, object v) in props)
            {
				List<object> newpath = new List<object>(path);
				newpath.Add(k);
				FindTabsInSchemaHelper((Dictionary<string, object>)props[k], newpath, tab_paths); 
            }
        }

		public Dictionary<string, string> FindTabsInSchema(Dictionary<string, object> schema)
        {
			var tab_paths = new Dictionary<string, string>();
			FindTabsInSchemaHelper(schema, new List<object>(), tab_paths);
			return tab_paths;
        }

		/// <summary>
		/// hanging_row: a dict where some values are scalars and others are
		/// dicts with only scalar values.
		/// Returns: a new dict where the off-hanging dict at key k has been merged into
		/// the original by adding &lt;k&gt;&lt;key_sep&gt;&lt;key in hanger&gt; = &lt;val in hanger&gt;
		/// for each key, val in hanger.
		/// </summary>
		/// <param name="hanging_row"></param>
		/// <param name="key_sep"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private Dictionary<string, JNode> ResolveHang(Dictionary<string, JNode> hanging_row, char key_sep = '.')
        {
			var result = new Dictionary<string, JNode>();
			foreach ((string k, JNode v) in hanging_row)
            {
				if (v is JObject)
                {
					var dv = (JObject)v;
					foreach ((string subk, JNode subv) in dv.children)
                    {
						string new_key = $"{k}{key_sep}{subk}";
						if (result.ContainsKey(new_key))
                        {
							throw new ArgumentException($"Attempted to create hanging row with synthetic key {new_key}, but {new_key} was already a key in {hanging_row}");
                        }
						result[new_key] = subv;
                    }
                }
                else
                {
					result[k] = v;
                }
            }
			return result;
        }

		private string FormatKey(string key, string super_key, char key_sep = '.')
        {
			return super_key.Length == 0 ? key : $"{super_key}{key_sep}{key}";
        }

		private void AnyTableToRecord(JNode obj, 
									   string cls, 
									   Dictionary<string, JNode> rest_of_row, 
									   List<JNode> result, 
									   List<string> super_keylist, 
									   char key_sep)
        {
			string super_key = string.Join(key_sep, super_keylist);
			Func<string, string> keyformat = (string x) => FormatKey(x, super_key, key_sep);
			if (cls == "rec")
            {
				JArray aobj = (JArray)obj;
				// arrays of flat objects - this is what we're aiming for
				foreach (object rec in aobj.children)
                {
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
					foreach ((string k, JNode v) in ((JObject)rec).children)
                    {
						newrec[keyformat(k)] = v;
                    }
					foreach ((string k, JNode v) in rest_of_row)
                    {
						newrec[k] = v;
                    }
					result.Add(new JObject(0, newrec));
                }
            }
			else if (cls == "tab")
            {
				// a dict mapping some number of keys to scalars and the rest to arrays
				// some keys may be mapped to hanging all-scalar dicts and not scalars
				JObject oobj = (JObject)obj;
				Dictionary<string, JArray> arr_dict = new Dictionary<string, JArray>();
				Dictionary<string, JNode> not_arr_dict = new Dictionary<string, JNode>();
				int len_arr = 0;
				foreach ((string k, JNode v) in oobj.children)
                {
					if (v is JArray)
                    {
						JArray varr = (JArray)v;
						arr_dict[k] = varr;
						if (varr.Length > len_arr) { len_arr = varr.Length; }
                    }
                    else
                    {
						not_arr_dict[keyformat(k)] = v;
                    }
                }
				not_arr_dict = ResolveHang(not_arr_dict, key_sep);
				for (int ii = 0; ii < len_arr; ii++)
                {
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
					foreach ((string k, JArray v) in arr_dict)
                    {
						// if one array is longer than others, the missing values from the short rows are filled by
						// empty strings
						newrec[keyformat(k)] = ii >= v.Length ? new JNode("", Dtype.STR, 0) : v.children[ii];
                    }
					foreach ((string k, JNode v) in not_arr_dict) { newrec[k] = v; }
					foreach ((string k, JNode v) in rest_of_row) { newrec[k] = v; }
					result.Add(new JObject(0, newrec));
                }
            }
			else if (cls == "sqr")
            {
				// an array of arrays of scalars
				JArray aobj = (JArray)obj;
				foreach (JNode arr in aobj.children)
                {
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
					foreach ((string k, JNode v) in rest_of_row)
                    {
						newrec[k] = v;
                    }
					List<JNode> children = ((JArray)arr).children;
					for (int ii = 0; ii < children.Count; ii++)
                    {
						newrec[keyformat($"col{ii+1}")] = children[ii];
                    }
					result.Add(new JObject(0, newrec));
                }
            }
        }

		private void BuildTableHelper(JNode obj, 
									string cls, 
									int depth, 
									JArray tab_path, 
									List<JNode> result, 
									Dictionary<string, JNode> rest_of_row,
									List<string> super_keylist, 
									char key_sep)
        {
			List<string> new_super_keylist = new List<string>(super_keylist);
			Dictionary<string, JNode> new_rest_of_row = new Dictionary<string, JNode>(rest_of_row);
			if (depth == tab_path.Length)
            {
				AnyTableToRecord(obj, cls, new_rest_of_row, result, new_super_keylist, key_sep);
				return;
            }
            else
            {
				if (obj is JArray)
                {
					foreach (JNode subobj in ((JArray)obj).children)
                    {
						BuildTableHelper(subobj, cls, depth + 1, tab_path, result, new_rest_of_row, new_super_keylist, key_sep);
                    }
                }
                else
                {
					string new_key = (string)tab_path.children[depth].value;
					string super_key = string.Join(key_sep, new_super_keylist);
					Dictionary<string, JNode> children = ((JObject)obj).children;
					foreach ((string k, JNode v) in children)
					{
						if (k == new_key) continue;
						string newk = FormatKey(k, super_key, key_sep);
						if (v.type == Dtype.OBJ)
						{
							foreach ((string subk, JNode subv) in ((JObject)v).children)
							{
								// add hanging scalar dicts to rest_of_row
								if ((subv.type & Dtype.ARR_OR_OBJ) == 0)
								{
									new_rest_of_row[$"{newk}{key_sep}{subk}"] = subv;
								}
							}
						}
						else if (v.type != Dtype.ARR)
						{
							new_rest_of_row[newk] = v;
						}
					}
					new_super_keylist.Add(new_key);
					BuildTableHelper(children[new_key], cls, depth + 1, tab_path, result, new_rest_of_row, new_super_keylist, key_sep);
                }
            }
        }

		public JArray BuildTable(JNode obj, Dictionary<string, object> schema, char key_sep = '.')
        {
			JsonParser jsonParser = new JsonParser();
			Dictionary<string, string> tab_paths = FindTabsInSchema(schema);
			if (tab_paths.Count == 0)
            {
				return new JArray(0, new List<JNode>());
            }
			if (tab_paths.Count > 1)
            {
				throw new ArgumentException($"This JSON contains {tab_paths.Count} possible tables, and BuildTable doesn't know how to proceed.");
            }
			List<JNode> result = new List<JNode>();
			JArray path = new JArray(0, new List<JNode>());
			string cls = "";
			foreach ((string pathstr, string clsname) in tab_paths)
            {
				(path, _, _) = jsonParser.ParseArray(pathstr, 0, 0);
				cls = clsname;
            }
			BuildTableHelper(obj, cls, 0, path, result, new Dictionary<string, JNode>(), new List<string>(), key_sep);
			return new JArray(0, result);
        }

		public string TableToCsv(JArray table, char delim = ',', char quote_char = '"', string[]? header = null)
        {
			// allow the user to supply their own column order. If they don't, just alphabetically sort colnames
			if (header == null)
            {
				header = ((JObject)table.children[0]).children.Keys.ToArray();
				Array.Sort(header);
			}
			string squote_char = new string(quote_char, 1);
			Func<string, string> escape_quotes = (string s) => s.Replace(squote_char, $"\\{squote_char}");
			StringBuilder sb = new StringBuilder();
			for (int ii = 0; ii < header.Length; ii++)
            {
				string col = header[ii];
				if (col.Contains(quote_char))
                {
					sb.Append(quote_char + escape_quotes(col) + quote_char);
                }
				else if (col.Contains(delim))
                {
					sb.Append(quote_char + col + quote_char);
                }
                else
                {
					sb.Append(col);
                }
				if (ii < header.Length - 1) sb.Append(delim);
            }
			sb.Append('\n');
			foreach (JNode row in table.children)
            {
				JObject orow = (JObject)row;
				for (int ii = 0; ii < header.Length; ii++)
                {
					string col = header[ii];
					JNode val = orow.children[col];
					string sval = val.type == Dtype.STR ? (string)val.value : val.ToString();
                    if (val.type == Dtype.STR && sval.Contains(quote_char))
                    {
						// strings that don't contain the delimiter or quote character don't need enclosing quotes
						sb.Append(quote_char + escape_quotes(sval) + quote_char);
					}
					else if (val.type == Dtype.STR && sval.Contains(delim))
                    {
						sb.Append(quote_char + sval + quote_char);
                    }
					else if ((val.type & Dtype.DATE_OR_DATETIME) != 0)
                    {
						// dates and datetimes are automatically enquoted, but we don't want the quotation marks in the csv
						sb.Append(sval.Slice("1:-1"));
                    }
					//else if (val.type == Dtype.BOOL)
					//{
					//	sb.Append((bool)val.value ? "1" : "0");
					//}
					else
                    {
						// everything else looks best with the normal JNode ToString as well
						sb.Append(sval);
                    }
					if (ii < header.Length - 1) sb.Append(delim);
                }
				sb.Append('\n');
            }
			return sb.ToString();
        }
	}


	public class JsonTabularizerTester
    {
		public static void Test()
        {
			var testcases = new (string inp, string desired_out, char key_sep)[]
			{
				(
				"[" +
					"{" +
						"\"a\": 1," +
						"\"b\": \"foo\"," +
						"\"c\": [" +
							"{" +
								"\"d\": 1," +
								"\"e\": \"a\"" +
							"}," +
							"{" +
								"\"d\": 2," +
								"\"e\": \"b\"" +
							"}" +
						"]" +
					"}," +
					"{" +
						"\"a\": 2," +
						"\"b\": \"bar\"," +
						"\"c\": [" +
							"{" +
								"\"d\": 3," +
								"\"e\": \"c\"" +
							"}," +
							"{" +
								"\"d\": 4," +
								"\"e\": \"d\"" +
							"}" +
						"]" +
					"}" +
				"]", // a list of dicts mapping to records (each record is a list of dicts)
				"[" +
					"{\"a\": 1, \"b\": \"foo\", \"c.d\": 1, \"c.e\": \"a\"}," +
					"{\"a\": 1, \"b\": \"foo\", \"c.d\": 2, \"c.e\": \"b\"}," +
					"{\"a\": 2, \"b\": \"bar\", \"c.d\": 3, \"c.e\": \"c\"}," +
					"{\"a\": 2, \"b\": \"bar\", \"c.d\": 4, \"c.e\": \"d\"}" +
				"]",
				'.'
				),
				(
				"{\"6\": 7," +
 "\"9\": \"ball\"," +
 "\"9a\": 2," +
 "\"a\": false," +
 "\"b\": \"3\"," +
 "\"blutentharst\": [\"DOOM BOOM\", true]," +
 "\"jub\": {\"status\": \"jubar\"," +
		 "\"uy\": [1, 2, NaN]," +
		 "\"yu\": [[6, {\"m8\": 9, \"y\": \"b\"}], null]}}", // bad irregular JSON that can't be tabularized
				"[]", // no table possible
				'.'
				),
				(
				"[" +
					"{" +
						"\"name\": \"foo\"," +
						"\"players\": [" +
							"{\"name\": \"alice\", \"hits\": [1, 2], \"at-bats\": [3, 4]}," +
							"{\"name\": \"bob\", \"hits\": [2], \"at-bats\": [3]}" +
						"]" +
					"}," +
					"{" +
						"\"name\": \"bar\"," +
						"\"players\": [" +
							"{\"name\": \"carol\", \"hits\": [1], \"at-bats\": [2, 3]}" +
						"]" +
					"}" +
				"]", // list of tabs with some missing values
				"[" +
					"{" +
						"\"name\": \"foo\", " +
						"\"players.name\": \"alice\"," +
						"\"players.hits\": 1," +
						"\"players.at-bats\": 3" +
					"}," +
					"{" +
						"\"name\": \"foo\"," +
						"\"players.name\": \"alice\"," +
						"\"players.hits\": 2," +
						"\"players.at-bats\": 4" +
					"}," +
					"{" +
						"\"name\": \"foo\"," +
						"\"players.name\": \"bob\"," +
						"\"players.hits\": 2," +
						"\"players.at-bats\": 3" +
					"}," +
					"{" +
						"\"name\": \"bar\"," +
						"\"players.name\": \"carol\"," +
						"\"players.hits\": 1," +
						"\"players.at-bats\": 2" +
					"}," +
					"{" +
						"\"name\": \"bar\"," +
						"\"players.name\": \"carol\"," +
						"\"players.hits\": \"\"," +
						"\"players.at-bats\": 3" +
					"}" +
				"]",
				'.'
				),
				(
				"{\"leagues\": [" +
					"{" +
					"\"league\": \"American\"," +
					"\"teams\": [" +
							"{" +
								"\"name\": \"foo\"," +
								"\"players\": [" +
									"{\"name\": \"alice\", \"hits\": [1], \"at-bats\": [3]}" +
								"]" +
							"}," +
							"{" +
								"\"name\": \"bar\"," +
								"\"players\": [" +
									"{\"name\": \"carol\", \"hits\": [1], \"at-bats\": [2]}" +
								"]" +
							"}" +
						"]" +
					"}," +
					"{" +
					"\"league\": \"National\"," +
					"\"teams\": [" +
							"{" +
								"\"name\": \"baz\"," +
								"\"players\": [" +
									"{\"name\": \"bob\", \"hits\": [2], \"at-bats\": [3]}" +
								"]" +
							"}" +
						"]" +
					"}" +
				"]}", // deeply nested tab
				"[" +
					"{" +
						"\"leagues.league\": \"American\"," +
						"\"leagues.teams.name\": \"foo\"," +
						"\"leagues.teams.players.name\": \"alice\"," +
						"\"leagues.teams.players.hits\": 1," +
						"\"leagues.teams.players.at-bats\": 3" +
					"}," +
					"{" +
						"\"leagues.league\": \"American\"," +
						"\"leagues.teams.name\": \"bar\"," +
						"\"leagues.teams.players.name\": \"carol\"," +
						"\"leagues.teams.players.hits\": 1," +
						"\"leagues.teams.players.at-bats\": 2" +
					"}," +
					"{" +
						"\"leagues.league\": \"National\"," +
						"\"leagues.teams.name\": \"baz\"," +
						"\"leagues.teams.players.name\": \"bob\"," +
						"\"leagues.teams.players.hits\": 2," +
						"\"leagues.teams.players.at-bats\": 3" +
					"}" +
				"]",
				'.'
				),
				(
				"[" +
					"[1, 2, \"a\"]," +
					"[2, 3, \"b\"]" +
				"]", // list of lists with mixed scalar types
				"[" +
					"{\"col1\": 1, \"col2\": 2, \"col3\": \"a\"}," +
					"{\"col1\": 2, \"col2\": 3, \"col3\": \"b\"}" +
				"]",
				'.'
				),
				(
				"{\"a\": [" +
						"{" +
							"\"name\": \"blah\"," +
							"\"rows\": [" +
								"[1, 2, \"a\"]," +
								"[1, 2, \"a\"]" +
							"]" +
						"}" +
					"]" +
				"}", // deeply nested list of lists with mixed scalar types
				"[" +
					"{" +
						"\"a.name\": \"blah\", " +
						"\"a.rows.col1\": 1, " +
						"\"a.rows.col2\": 2, " +
						"\"a.rows.col3\": \"a\"" +
					"}," +
					"{" +
						"\"a.name\": \"blah\", " +
						"\"a.rows.col1\": 1, " +
						"\"a.rows.col2\": 2, " +
						"\"a.rows.col3\": \"a\"" +
					"}" +
				"]",
				'.'
				),
				(
				"[" +
					"{\"a\": 1, \"b\": \"a\"}," +
					"{\"a\": \"2\", \"b\": \"b\"}," +
					"{\"a\": 3., \"b\": \"c\"}" +
				"]", // record with mixed scalar types
				"[" +
					"{\"a\": 1, \"b\": \"a\"}," +
					"{\"a\": \"2\", \"b\": \"b\"}," +
					"{\"a\": 3., \"b\": \"c\"}" +
				"]",
				'.'
				),
				(
				"{" +
					"\"f\": {\"g\": 1}," +
					"\"g\": 2.0," +
					"\"a\": {" +
						"\"a\": [1, 2]," +
						"\"b\": [\"a\", \"b\"]," +
						"\"c\": {\"d\": 2, \"e\": \"a\"}," +
						"\"d\": \"b\"" +
					"}" +
				"}", // deep nested tab with hanging dict
				"[" +
					"{" +
						"\"a.a\": 1," +
						"\"a.b\": \"a\"," +
						"\"a.c.d\": 2," +
						"\"a.c.e\": \"a\"," +
						"\"a.d\": \"b\"," +
						"\"f.g\": 1," +
						"\"g\": 2.0" +
					"}," +
					"{" +
						"\"a.a\": 2," +
						"\"a.b\": \"b\"," +
						"\"a.c.d\": 2," +
						"\"a.c.e\": \"a\"," +
						"\"a.d\": \"b\"," +
						"\"f.g\": 1," +
						"\"g\": 2.0" +
					"}" +
				"]",
				'.'
				),
				(
				"{" +
					"\"a\": {\"b\": 2, \"c\": \"b\"}," +
					"\"d\": [" +
						"[1, 2]," +
						"[3, 4]" +
					"]" +
				"}", // deep nested all-same-type sqr with hanging dict
				"[" +
					"{" +
					"\"a.b\": 2," +
					"\"a.c\": \"b\"," +
					"\"d.col1\": 1," +
					"\"d.col2\": 2" +
					"}," +
					"{" +
					"\"a.b\": 2," +
					"\"a.c\": \"b\"," +
					"\"d.col1\": 3," +
					"\"d.col2\": 4" +
					"}" +
				"]",
				'.'
				),
				(
				"[" +
					"{\"a\": 1, \"b\": 2}," +
					"{\"a\": 3, \"b\": 4}" +
				"]", // all-same-type tab
				"[" +
					"{\"a\": 1, \"b\": 2}," +
					"{\"a\": 3, \"b\": 4}" +
				"]",
				'.'
				),
				(
				"{\"stuff\": [" +
					"{" +
						"\"name\": \"blah\"," +
						"\"substuff\": [" +
							"{" +
								"\"a\": 1," +
								"\"b\": \"foo\"," +
								"\"c\": [" +
									"{\"d\": 1, \"e\": \"a\"}" +
								"]" +
							"}," +
							"{" +
								"\"a\": 2," +
								"\"b\": \"bar\"," +
								"\"c\": [" +
									"{\"d\": 3, \"e\": \"c\"}" +
								"]" +
							"}" +
						"]" +
					"}," +
					"{" +
						"\"name\": \"wuz\"," +
						"\"substuff\": [" +
							"{" +
								"\"a\": 3, " +
								"\"b\": \"baz\", " +
								"\"c\": [" +
									"{\"d\": 4, \"e\": \"f\"}" +
								"]" +
							"}" +
						"]" +
					"}" +
				"]}",
				"[" +
					"{" +
						"\"stuff_name\": \"blah\"," +
						"\"stuff_substuff_a\": 1," +
						"\"stuff_substuff_b\": \"foo\"," +
						"\"stuff_substuff_c_d\": 1," +
						"\"stuff_substuff_c_e\": \"a\"" +
					"}," +
					"{" +
						"\"stuff_name\": \"blah\"," +
						"\"stuff_substuff_a\": 2," +
						"\"stuff_substuff_b\": \"bar\"," +
						"\"stuff_substuff_c_d\": 3," +
						"\"stuff_substuff_c_e\": \"c\"" +
					"}," +
					"{" +
						"\"stuff_name\": \"wuz\"," +
						"\"stuff_substuff_a\": 3," +
						"\"stuff_substuff_b\": \"baz\"," +
						"\"stuff_substuff_c_d\": 4," +
						"\"stuff_substuff_c_e\": \"f\"" +
					"}" +
				"]",
				'_'
				),
				(
				"[" +
					"{" +
						"\"a\": 1," +
						"\"b\": \"foo\"," +
						"\"c\": [" +
							"{\"d\": 1, \"e\": \"a\"}" +
						"]" +
					"}," +
					"{" +
						"\"a\": 2," +
						"\"b\": \"bar\"," +
						"\"c\": [" +
							"{\"d\": 3, \"e\": \"c\"}" +
						"]" +
					"}" +
				"]",
				"[" +
					"{\"a\": 1, \"b\": \"foo\", \"c/d\": 1, \"c/e\": \"a\"}," +
					"{\"a\": 2, \"b\": \"bar\", \"c/d\": 3, \"c/e\": \"c\"}" +
				"]",
				'/'
				),
				(
				"{\"a\": [[1, 2], [3, 4]], \"b\": \"a\"}",
				"[" +
					"{" +
						"\"a+col1\": 1," +
						"\"a+col2\": 2," +
						"\"b\": \"a\"" +
					"}," +
					"{" +
						"\"a+col1\": 3," +
						"\"a+col2\": 4," +
						"\"b\": \"a\"" +
					"}" +
				"]",
				'+'
				)
			};
			JsonParser jsonParser = new JsonParser();
			JsonTabularizer tabularizer = new JsonTabularizer();
			JsonSchemaMaker schema_maker = new JsonSchemaMaker();
			int tests_failed = 0;
			int ii = 0;
			foreach ((string inp, string desired_out, char key_sep) in testcases)
            {
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = schema_maker.BuildSchema(jinp);
				//Console.WriteLine($"schema for {inp}:\n{schema_maker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode(null, Dtype.NULL, 0);
				string base_message = $"Expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
                {
					result = tabularizer.BuildTable(jinp, schema, key_sep);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch (Exception ex)
                    {
						tests_failed++;
						Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
                {
					tests_failed++;
					Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
                }
            }

			JsonParser fancyParser = new JsonParser(true);

			var csv_testcases = new (string inp, string desired_out, char delim, char quote_char, string[]? header)[]
			{
				("[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\n1,a\n2,b\n", ',', '"', null),
				("[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a\tb\n1\ta\n2\tb\n", '\t', '"', null),
				("[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\n1,a\n2,b\n", ',', '\'', null),
				("[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a\tb\n1\ta\n2\tb\n", '\t', '\'', null),
				("[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "b,a\na,1\nb,2\n", ',', '"', new string[]{"b", "a"}),
				("[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "b\ta\na\t1\nb\t2\n", '\t', '"', new string[]{"b", "a"}),
				("[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\n1,\"a,b\"\n2,c\n", ',', '"', null),
				("[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\n1,'a,b'\n2,c\n", ',', '\'', null),
				("[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", "\"a,b\",b\n1,a\n2,b\n", ',', '"', null),
				("[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", "'a,b',b\n1,a\n2,b\n", ',', '\'', null),
				("[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", "b,\"a,b\"\na,1\nb,2\n", ',', '"', new string[]{"b", "a,b"}),
				("[{\"a\": 1}, {\"a\": 2}, {\"a\": 3}]", "a\n1\n2\n3\n", ',', '"', null),
				("[{\"a\": 1, \"b\": 2, \"c\": 3}, {\"a\": 2, \"b\": 3, \"c\": 4}, {\"a\": 3, \"b\": 4, \"c\": 5}]", "a|b|c\n1|2|3\n2|3|4\n3|4|5\n", '|', '"', null),
				("[{\"date\": \"1999-01-03\", \"cost\": 100.5, \"num\": 13}, {\"date\": \"2000-03-15\", \"cost\": 157.0, \"num\": 17}]",
				"cost,date,num\n100.5,1999-01-03,13\n157.0,2000-03-15,17\n", ',', '"', null),
				("[{\"name\": \"\\\"Dr. Blutentharst\\\"\", \"phone number\": \"420-997-1043\"},{\"name\": \"\\\"Fjordlak the Deranged\\\"\", \"phone number\": \"blo-od4-blud\"}]",
				"name,phone number\n\"\\\"Dr. Blutentharst\\\"\",420-997-1043\n\"\\\"Fjordlak the Deranged\\\"\",blo-od4-blud\n",
				',', '"', null),
			};
			foreach ((string inp, string desired_out, char delim, char quote_char, string[]? header) in csv_testcases)
			{
				ii++;
				JNode table = jsonParser.Parse(inp);
				string result = "";
				string head_str = header == null ? "null" : '[' + string.Join(',', header) + ']';
				string base_message = $"Expected TableToCsv({inp}, '{delim}', '{quote_char}', {head_str})\nto return\n{desired_out}\n";
				try
				{
					result = tabularizer.TableToCsv((JArray)table, delim, quote_char, header);
					//Console.WriteLine(table);
					//Console.WriteLine(result);
					try
					{
						if (!desired_out.Equals(result))
						{
							tests_failed++;
							Console.WriteLine($"{base_message}Instead returned\n{result}");
						}
					}
					catch (Exception ex)
					{
						tests_failed++;
						Console.WriteLine($"{base_message}Instead returned\n{result}\nand threw exception\n{ex}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
				}
			}

			Console.WriteLine($"Failed {tests_failed} tests.");
			Console.WriteLine($"Passed {ii - tests_failed} tests.");
		}
    }
}