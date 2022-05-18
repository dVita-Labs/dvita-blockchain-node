##
## Copyright (c) 2021 - Team11. All rights reserved.
##

all: build

build: restore
	dotnet publish -c Release -o ./dist

restore:
	dotnet restore

test:
	cd ../tests/Neo.ConsoleService.Tests && dotnet test --nologo --no-restore --verbosity normal 

start: plugins
	dotnet dist/neo-cli.dll

plugins:
	-@mkdir ./dist/Plugins
	-@cp -a ../../neo-modules/dist/* ./dist/Plugins
	-@sed -i 's/\/nxa-node-data\///g' ./dist/config.json

align-project: neo-cli.csproj
	dotnet remove $< reference Neo
	dotnet add $< reference ../../neo/src/neo/neo.cspro

clean:
	-@dotnet clean 2>&1 > /dev/null
	-@rm -rf ./dist
	-@rm -rf ./bin
	-@rm -rf ./obj
	-@rm -rf ../Neo.ConsoleService/bin
	-@rm -rf ../Neo.ConsoleService/obj
	-@rm -rf ../tests/Neo.ConsoleService.Tests/obj

.PHONY: all build restore test start plugins align-project clean

.SILENT: clean
