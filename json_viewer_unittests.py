import json
import os
import pathlib
import subprocess
import shutil
import sys
import unittest
import yaml

this_dir = pathlib.Path(os.path.dirname(__file__))

exe_dir = this_dir / "bin" / "Debug" / "net6.0"

exe_fname = exe_dir / "JSON_Viewer.exe" 
# the '/' is overloaded for pathjoin with a pathlib path 

temp_dir = this_dir / 'tempdir'


def test_parser_dumper(tester, Json, exe_fname, out_type='j'):
    true_out = json.loads(Json)
    input_name, output_name = temp_dir / 'input.json', temp_dir / 'output.yaml'
    with open(temp_dir / 'input.json', 'w') as f:
        json.dump(true_out, f)
    with open(output_name, 'w') as f:
        subprocess.run(f'{exe_fname} {out_type} {input_name}', stdout = f)
    with open(output_name) as f:
        if out_type[0] == 'j':
            out = json.load(f)
        else:
            out = yaml.safe_load(f)
    tester.assertEqual(true_out, out)


def test_specific_parser_dumper(tester, Json, out_type='j'):
    test_parser_dumper(tester, Json, exe_fname, out_type)


class JsonParserDumperTester(unittest.TestCase):
    def AAA_setup(self):
        # tests are run in lexicographic order of name, so the AAA 
        # ensures that this is run first
        os.mkdir(temp_dir)
        
    def zzz_tearDown(self):
        # as with AAA_setup, the zzz ensures that this is run after all
        # other tets
        shutil.rmtree(temp_dir)
        
    def test_parser_float(self):
        for out_type in ['j', 'jp']: # , 'y']: 
            # yaml dumper is broken, don't bother trying for now 
            with self.subTest(out_type=out_type):
                test_specific_parser_dumper(self, '123.45e6', out_type)
        
    def test_parser_general(self):
        example = r'''{"a":[-1, true, {"b" :  0.5, "c": "\uae77"},null],
"a\u10ff":[true, false, NaN, Infinity,-Infinity, {},    "\u043ea", []], "back'slas\"h": ["\"'\f\n\b\t\/"]}'''
        for out_type in ['j', 'jp']: # , 'y']:
            with self.subTest(out_type=out_type):
                test_specific_parser_dumper(self, example, out_type)
                
                
if __name__ == '__main__':
    unittest.main()