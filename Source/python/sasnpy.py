# /* --------------------------------------------------------------------------------- 
 # * MIT License
 # * 
 # * Copyright(c) 2019; Venu Gopal Lolla
 # * 
 # * Permission is hereby granted, free of charge, to any person obtaining a copy
 # * of this software and associated documentation files (the "Software"), to deal
 # * in the Software without restriction, including without limitation the rights
 # * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 # * copies of the Software, and to permit persons to whom the Software is
 # * furnished to do so, subject to the following conditions:
 # * 
 # * The above copyright notice and this permission notice shall be included in all
 # * copies or substantial portions of the Software.
 # * 
 # * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 # * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 # * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 # * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 # * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 # * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 # * SOFTWARE.
 # *  
# --------------------------------------------------------------------------------- */

# sasnpy module : is automatically loaded by Python through sasnpy.dll
# manages script execution, data transfer, and plot capture

import sys
import tempfile
import atexit
import csv
import xml.etree.ElementTree as ET

_sasnpy_symbol_map = {}
_sasnpy_globals = {}
_sasnpy_locals = {}

sasnpy_image_count = 0

# function that attempts to load a package
def import_package(package):
	try:
		return __import__(package)
	except ImportError:
		return None

# entry handler: executed on module load
def entry_handler():
	reroute_pyplot_show()
		
# exit handler : executed at interpreter termination
def exit_handler():
	pass

# create sentinel file to indicate completion of a task
def create_sentinel_file(sentinel_file):
	open(sentinel_file, 'wb', buffering = 0).close()

# function to execute user's Python script
def execute_script(script_file, sentinel_file):
	try:
		if sys.version_info >= (3, 0):
			exec(open(script_file).read(), _sasnpy_locals, _sasnpy_locals)
		else:
			execfile(script_file, _sasnpy_locals, _sasnpy_locals)
	finally:
		create_sentinel_file(sentinel_file)
			
# Custom handler for pyplot's show
# captures images to disk and introduces
# a placeholder in the output stream
def pyplot_show_handler():
	try:
		global sasnpy_image_count
		import matplotlib.pyplot as plt
		sasnpy_tempdir_plots = _sasnpy_symbol_map["tempdir_plots"]
		imagepath = sasnpy_tempdir_plots + "/image_" + str(sasnpy_image_count) + ".png"
		sasnpy_image_count = sasnpy_image_count + 1
		print("{{IMAGE||" + imagepath + "}}")
		plt.savefig(imagepath)
	except Exception as ex:
		pass

# install custom handler for pyplot
# set AGG backend to write to file 
# instead of plotting to a window 		
def reroute_pyplot_show():
	try:
		import matplotlib
		matplotlib.use('agg')
		import matplotlib.pyplot as plt
		plt.show = pyplot_show_handler
	except:
		pass

# read CSV file as a list of lists
# used when the pandas package is not installed		
def read_csv_file_as_list(table_file):
	table = []
	try:
		with open(table_file) as csv_file:
			csv_reader = csv.reader(csv_file, delimiter=',')
			for row in csv_reader:
				table.append(row)
	except:
		table = None
	return table
		
# reads a CSV file and makes it available in the user script's local namespace		
def set_input_table(table_name, table_file, sentinel_file):
	try:
		pd = import_package("pandas")
		if not pd is None:
			table = pd.read_csv(table_file)
		else:
			table = read_csv_file_as_list(table_file)
		_sasnpy_locals[table_name] = table
			
	finally:
		create_sentinel_file(sentinel_file)

# reads an XML file and induces a scalar value in the user script's local namespace	
def set_input_scalar(scalar_file, sentinel_file):
	try:
		
		xmlRoot = ET.parse(scalar_file).getroot()
		scalar_name = xmlRoot.get('name')
		scalar_type = xmlRoot.get('type')
		scalar_value = xmlRoot.text
		
		value = scalar_value
		if scalar_type == "int":
			value = int(scalar_value)
		elif scalar_type == "float":
			value = float(scalar_value)

		_sasnpy_locals[scalar_name] = value
			
	finally:
		create_sentinel_file(sentinel_file)

# write CSV file from a list of lists
# used when the pandas package is not installed				
def write_list_as_csv_file(table, table_file):
	try:
		with open(table_file, mode='w') as csv_file:
			csv_writer = csv.writer(csv_file, delimiter=',', quotechar='"', quoting=csv.QUOTE_MINIMAL)
			for row in table:
				csv_writer.writerow(row)
	finally:
		pass

# reads a table from the user script's local namespace and writes it to a CSV file
def get_output_table(table_name, table_file, sentinel_file):
	try:
		if table_name in _sasnpy_locals:
			table = _sasnpy_locals[table_name]
			pd = import_package("pandas")
			if (not pd is None) and isinstance(table, pd.DataFrame):
				table.to_csv(table_file)
			else:
				write_list_as_csv_file(table, table_file)
			
	finally:
		create_sentinel_file(sentinel_file)

# reads a scalar value in the user script's local namespace	and writes it to an XML file
def get_output_scalar(scalar_name, scalar_file, sentinel_file):
	try:
		if scalar_name in _sasnpy_locals:
			value = _sasnpy_locals[scalar_name]
			xmlEl = ET.Element('object')
			xmlEl.set('name', scalar_name)
			xmlEl.set('type', type(value).__name__)
			xmlEl.text = str(value)
			xmlTr = ET.ElementTree(xmlEl)
			xmlTr.write(scalar_file)
	finally:
		create_sentinel_file(sentinel_file)
		
#register exit handler
atexit.register(exit_handler)		

# invoke entry handler
entry_handler()	