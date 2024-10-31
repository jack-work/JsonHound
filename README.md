# Description
Your json is escaped, perhaps at multiple levels (e.g. \"{\\\"test\\\": \\\"{...}\\\"}\"); and you want to track it down and catch it and bring it back home to where it belongs (or in other words remove all the escaped characters and render a valid json object).  
This program can do it for you.

## Installation
No installation script is provided, it must be built locally.  This can be done as a self contained publish to a directory of your choice.

## Usage
Arguments are defined by consecutive name value pairs.  They are not position specific.
- filePath: Description: file from which json may be read.  If not provided, application will 
prompt for json input from console.  e.g. filePath C:\test
- format: Description: comma separated list defining the format of the output.  Currently only indent is supported.  Default System.Text.Json format used if none specified. e.g. format indent.
- outFile: Description: file to which the output will be written.  If none is specified then the output will be written to console. e.g. outFile C:\\pcf.json.

Program supports absolute or relative file paths.  Arguments can be provided in any order.

Full argument example: 
outFile C:\escapedJson.json format indent outFile pcf.json");
