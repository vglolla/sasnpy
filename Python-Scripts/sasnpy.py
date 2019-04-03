import sys
import tempfile
import atexit

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
	
def execute_script(script_file, sentinel_file):
	try:
		if sys.version_info >= (3, 0):
			exec(open(script_file).read(), _sasnpy_globals, _sasnpy_locals)
		else:
			execfile(script_file)
	finally:
		open(sentinel_file, 'a').close()
			

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
	
def set_input_scalar():
	pass
	
def get_output_table():
	pass

def get_output_scalar():
	pass
		
atexit.register(exit_handler)		
entry_handler()	