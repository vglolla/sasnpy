import sys
import tempfile
import atexit
import xml.etree.ElementTree as ET

_sasnpy_symbol_map = {}
_sasnpy_globals = {}
_sasnpy_locals = {}

sasnpy_image_count = 0

def import_package(package):
	try:
		return __import__(package)
	except ImportError:
		return None

def entry_handler():
	reroute_pyplot_show()
		
def exit_handler():
	pass

def create_sentinel_file(sentinel_file):
	open(sentinel_file, 'wb', buffering = 0).close()
	
def execute_script(script_file, sentinel_file):
	try:
		if sys.version_info >= (3, 0):
			exec(open(script_file).read(), _sasnpy_globals, _sasnpy_locals)
		else:
			execfile(script_file, _sasnpy_globals, _sasnpy_locals)
	finally:
		create_sentinel_file(sentinel_file)
			

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

		
def reroute_pyplot_show():
	try:
		import matplotlib
		matplotlib.use('agg')
		import matplotlib.pyplot as plt
		plt.show = pyplot_show_handler
	except:
		pass
		
def set_input_table():
	pass
	
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
	
def get_output_table():
	pass

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
		
		
atexit.register(exit_handler)		
entry_handler()	