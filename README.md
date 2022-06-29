# JSON-Tools
Miscellaneous tools for querying of JSON in C#.
At some point I hope to make a Notepad++ extension that incorporates all of these features.
Includes:
1. a JSON parser that includes the line number of each JSON node (see JsonParser.cs, JNode.cs). 5-10x slower than Python's standard library JSON parser.
2. The JNode objects produced by JsonParser can be pretty-printed or compactly printed. This can also change the line numbers of each child JNode in-place to reflect the new format.
2. A YAML dumper that dumps valid equivalent YAML for *most* (but *not all*) JSON (see YamlDumper.cs). Most likely to have trouble with keys that contain colons or double quotes, and also values that contain newlines.
3. The JSON parser can also be used as a linter to identify the following errors in JSON:
    * string literals terminated by newlines
    * string literals enclosed by ' instead of "
    * unterminated string literals (lint shows location of starting quote)
    * invalidly escaped characters in strings
    * numbers with two decimal points
    * multiple consecutive commas in an array or object
    * commas before first or after last element in array or object
    * two array or object elements not separated by a comma
    * no colon between key and value in object
    * non-string key in object
    * wrong char closing object or array
4. RemesPath query language for pandas-style querying of JSON. Similar in concept to [JMESPath](https://jmespath.org/), but with added functionality including regular expressions and recursive search. Query execution speed appears to be comparable to pandas, perhaps a couple times slower.
    * I plan to add RemesPath examples at some point in the future.
    * See the language specification.
    * See RemesPath.cs, RemesPathFunctions.cs, RemesPathLexer.cs
5. Function (see JsonTabularize.BuildTable) for converting JSON into tabular (arrays of objects) form, and outputting JSON in this form as a delimiter-separated-variables (e.g., CSV, TSV) file 
(see JsonTabularize.TableToCsv).