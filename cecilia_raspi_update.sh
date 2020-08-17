#!/usr/bin/env bash

# cecilia_raspi_update.sh
# Version: 1.0
# Last Changed 16/08/2020:
    # 16/08/2020 - Initial script

# Updates a docker image of Cecilia to the latest image.
# MUST BE AN IMAGE MADE BY cecilia_raspi_install.sh

# Stop and remove container
docker stop cecilia
docker rm cecilia

# Pull image update
docker pull mgrime/cecilia:arm

# Create new container
docker create --name cecilia -v $HOME/cecilia_config:/App/Config --restart=always mgrime/cecilia:arm

# Start
docker start cecilia

exit
