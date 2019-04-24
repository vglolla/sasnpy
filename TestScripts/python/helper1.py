import sys
sys.path.append('C:/GHRepositories/sasnpy/SASnPy/bin/x64/Debug')
sys.path.append('C:/GHRepositories/sasnpy/Python-Scripts')
import sasnpy

sasnpy._sasnpy_symbol_map['tempdir_datain'] = 'C:/Users/svglolla/AppData/Local/Temp/SASnPyDebug/DataIn'
sasnpy._sasnpy_symbol_map['tempdir_dataout'] = 'C:/Users/svglolla/AppData/Local/Temp/SASnPyDebug/DataOut'
sasnpy._sasnpy_symbol_map['tempdir_displaycontent'] = 'C:/Users/svglolla/AppData/Local/Temp/SASnPyDebug/DisplayContent'
sasnpy._sasnpy_symbol_map['tempdir_plots'] = 'C:/Users/svglolla/AppData/Local/Temp/SASnPyDebug/DisplayContent/Plots'

sasnpy.execute_script('C:/GHRepositories/sasnpy/TestScripts/pySample1.py', 'C:/Users/svglolla/AppData/Local/Temp/SASnPyDebug/booyah1.txt')
sasnpy.execute_script('C:/GHRepositories/sasnpy/TestScripts/pySample2.py', 'C:/Users/svglolla/AppData/Local/Temp/SASnPyDebug/booyah2.txt')
sasnpy.execute_script('C:/GHRepositories/sasnpy/TestScripts/pySample3.py', 'C:/Users/svglolla/AppData/Local/Temp/SASnPyDebug/booyah3.txt')