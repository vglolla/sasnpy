import sys
import tempfile
import atexit

def import_package(package):
	try:
		return __import__(package)
	except ImportError:
		return None

def entryhandler():
	pass
		
def exithandler():
	pass
	
def run_script(file):
	if sys.version_info >= (3, 0):
		exec(open(file).read())
	else:
		execfile(file)

sasnpy_image_count = 0		
def plot_to_disk():
	try:
		global sasnpy_image_count
		import matplotlib.pyplot as plt
		imagepath = sasnpy_tempdir_plots + "/image_" + str(sasnpy_image_count) + ".png"
		sasnpy_image_count = sasnpy_image_count + 1
		print("{{IMAGE||" + imagepath + "}}")
		plt.savefig(imagepath)
	except Exception as ex:
		pass
		
def capture_pyplots():
	try:
		import matplotlib.pyplot as plt
		plt.show = plot_to_disk
	except:
		pass
		
atexit.register(exithandler)		
capture_pyplots()
entryhandler()	