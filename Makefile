##
## Copyright (c) 2021 - Team11. All rights reserved.
##

all:
	make -C neo-cli $@

build:
	make -C neo-cli $@

restore:
	make -C neo-cli $@

test:
	make -C neo-cli $@

start:
	make -C neo-cli $@

align-project:
	make -C neo-cli $@

clean:
	make -C neo-cli $@

.PHONY: all build restore test start align-project clean

.SILENT: clean
