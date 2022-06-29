# TODO

- Learn how to make a Notepad++ plugin.
- Create the basic GUI.
- Add remespath querying to the GUI. Ideally also add a build-your-own remespath tool for the most common cases (e.g. searching for `foo.bar.baz`, toggling recursive search).
- add recursive search to remespath
- Enable querying/formatting of only selected text 
- For making tabularization tools:
	1. Add the tabularization GUI:
		- Add button for converting data to table
		- Add export options
		- Enable tabularization of only selected text
- Make a BSON parser
- add support for dates, datetimes, times
- Add linting GUI

# PARTIALLY DONE
- Remove bug in determination of "required" keys for JsonSchema
- Make sure linter works
- Fix bugs in YamlDumper.cs:
	- fails when key contains quotes and colon
	- fails when value contains quotes and newline
	- fails when value contains quotes and colon